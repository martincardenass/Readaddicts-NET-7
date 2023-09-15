using Microsoft.EntityFrameworkCore;
using PostAPI.Dto;
using PostAPI.Interfaces;
using PostAPI.Models;

namespace PostAPI.Repositories
{
    public class MessageRepository : IMessage
    {
        private readonly AppDbContext _context;
        private readonly IToken _tokenService;

        public MessageRepository(AppDbContext context, IToken tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<List<MessageView>> GetConversation(int page, int pageSize, string receiver, string sender)
        {
            int msgsToSkip = (page - 1) * pageSize;
            // * Extract the IDs using the usernames
            var receiverId = await MessageJoinQuery()
                .Where(u => u.Receiver_Username == receiver)
                .Select(u => u.Receiver_User_Id)
                .FirstOrDefaultAsync();

            var senderId = await MessageJoinQuery()
                .Where(u => u.Sender_Username == sender)
                .Select(u => u.Sender_User_Id)
                .FirstOrDefaultAsync();

            var messages = await MessageJoinQuery()
                .Where(r => (r.Sender == senderId && r.Receiver == receiverId) || (r.Sender == receiverId && r.Receiver == senderId))
                .OrderByDescending(m => m.Timestamp)
                .Skip(msgsToSkip)
                .Take(pageSize)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return messages;
        }

        public async Task<MessageView?> GetMessageById(int messageId)
        {
            var userId = await _context.Messages
                .Where(m => m.Message_Id == messageId)
                .Select(u => u.Receiver)
                .FirstOrDefaultAsync();

            if (await IsUserReceiver(userId) && await _tokenService.IsUserAuthorized())
            {
                var message = await _context.Messages.FindAsync(messageId); // * Get message from the context so we can edit it

                if(message != null && !message.Is_Read)
                {
                    message.Is_Read = true; // * Set the massage read when we open it
                    _ = await _context.SaveChangesAsync();
                }

                return await MessageJoinQuery().FirstOrDefaultAsync(m => m.Message_Id == messageId); // * Return the messageView
            }
            else return null;
        }

        public async Task<List<MessageView>> GetUserMessages(string username)
        {
            int userId = await _context.Users
                .Where(u => u.Username == username)
                .Select(u => u.User_Id)
                .FirstOrDefaultAsync();

            if (await IsUserReceiver(userId) && await _tokenService.IsUserAuthorized())
                return await MessageJoinQuery().Where(u => u.Receiver == userId).ToListAsync();
            else return null;
        }

        public async Task<List<UserLimitedDto>> GetUsersThatHaveMessagedMe()
        {
            // * Id receiver, so the user logged in.
            var (id, _, username) = await _tokenService.DecodeHS512Token();

            // * List of sender who have messaged the loggedin user, exclusing ofc the loggedin user
            var sender = await _context.Messages
                .Where(user => user.Receiver == id)
                .Where(user => user.Sender != id) // * Filter out your ID from the response
                .GroupBy(group => group.Sender)
                .Select(group => group.Key) // * Unique sender key
                .ToListAsync();

            var users = _context.Users
                .Where(user => sender.Contains(user.User_Id))
                .Select(u => new
                {
                    u.User_Id,
                    u.Username,
                    u.Profile_Picture
                })
                // * Order based on the most recent message timestamp for each conversation
                .OrderByDescending(user => _context.Messages
                    .Where(msg => msg.Sender == user.User_Id && msg.Receiver == id || msg.Sender == id && msg.Receiver == user.User_Id)
                    .OrderByDescending(msg => msg.Timestamp)
                    .Select(msg => msg.Timestamp)
                    .FirstOrDefault())
                .ToList();

            var msgs = users
                .Select(u => new UserLimitedDto
                {
                    User_Id = u.User_Id,
                    Username = u.Username,
                    Profile_Picture = u.Profile_Picture
                })
                .ToList();

            return msgs;
        }

        public async Task<bool> IsUserReceiver(int userId)
        {
            // * is the logged in user trying to get their own messages?
            var (id, _, _) = await _tokenService.DecodeHS512Token();

            var receiver = await _context.Messages
                .Where(u => u.Receiver == userId)
                .Select(u => u.Receiver)
                .FirstOrDefaultAsync();

            return id == receiver;
        }

        public IQueryable<MessageView> MessageJoinQuery()
        {
            return
                _context.Messages
                .GroupJoin(
                    _context.Users,
                    message => message.Sender, // * Join to get the info of who sent the message
                    sender => sender.User_Id,
                    (message, sender) => new { message, sender })
                .SelectMany(
                    joinResult => joinResult.sender.DefaultIfEmpty(),
                    (joinResult, sender) => new { joinResult.message, sender })
                .GroupJoin(
                    _context.Users,
                    message => message.message.Receiver,
                    receiver => receiver.User_Id,
                    (message, receiver) => new { message, receiver })
                .SelectMany(
                    joinResult => joinResult.receiver.DefaultIfEmpty(),
                    (joinResult, receiver) => new { joinResult.message, receiver }
                    )
                .Select(result => new MessageView
                {
                    // * Sender
                    Sender_User_Id = result.message.sender.User_Id,
                    Sender_Username = result.message.sender.Username,
                    Sender_Profile_Picture = result.message.sender.Profile_Picture,
                    // * Receiver
                    Receiver_User_Id = result.receiver.User_Id,
                    Receiver_Username = result.receiver.Username,
                    Receiver_Profile_Picture = result.receiver.Profile_Picture,
                    // * Actual messages
                    Message_Id = result.message.message.Message_Id,
                    Sender = result.message.message.Sender,
                    Receiver = result.message.message.Receiver,
                    Content = result.message.message.Content,
                    Timestamp = result.message.message.Timestamp,
                    Is_Read = result.message.message.Is_Read
                });
        }

        public async Task<MessageView> SendMessage(string receiver, Message message)
        {
            var (id, _, _) = await _tokenService.DecodeHS512Token();

            // * Workaround to get the id using the username string
            var receiverId = await _context.Users
                .Where(u => u.Username == receiver)
                .Select(u => u.User_Id)
                .FirstOrDefaultAsync();

            bool receiverExists = await _context.Users.AnyAsync(u => u.User_Id == receiverId);

            if (id != 0 && receiverExists)
            {
                var newMessage = new Message()
                {
                    Sender = id,
                    Receiver = receiverId,
                    Content = message.Content,
                    Timestamp = DateTime.UtcNow,
                };

                _context.Add(newMessage);

                // * Get the user of whoever its sending a message
                var userLogged = await _context.Users.Where(u => u.User_Id == id).FirstOrDefaultAsync();

                if(userLogged != null)
                {
                    userLogged.Last_Login = DateTime.UtcNow; // * Update their last seen
                    _ = await _context.SaveChangesAsync();
                }

                _ = await _context.SaveChangesAsync();

                var user = await _context.Users
                    .FindAsync(id);

                var messageToReturn = new MessageView()
                {
                    Sender_User_Id = id,
                    Sender_Username = user.Username,
                    Sender_Profile_Picture = user.Profile_Picture,
                    Message_Id = newMessage.Message_Id,
                    Content = newMessage.Content,
                    Timestamp = newMessage.Timestamp
                };
                return messageToReturn; // * Hardcoding true to avoid weird error
            }

            else return null;
        }
    }
}

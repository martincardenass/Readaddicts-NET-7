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

        public async Task<List<MessageView>> GetConversation(string receiver, string sender)
        {
            // * Extract the IDs using the usernames
            var receiverId = await MessageJoinQuery()
                .Where(u => u.Receiver_Username == receiver)
                .Select(u => u.Receiver_User_Id)
                .FirstOrDefaultAsync();

            var senderId = await MessageJoinQuery()
                .Where(u => u.Sender_Username == sender)
                .Select(u => u.Sender_User_Id)
                .FirstOrDefaultAsync();

            return await MessageJoinQuery()
                .Where(r => (r.Sender == senderId && r.Receiver == receiverId) || (r.Sender == receiverId && r.Receiver == senderId))
                .ToListAsync();
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
            var (_, _, username) = await _tokenService.DecodeHS512Token();
            // * Select and return the properties that userlimited dto has extracted from the MessageView

            var users = await MessageJoinQuery()
                .Where(u => u.Receiver_Username == username)
                .ToListAsync();

            return users
                .GroupBy(u => u.Sender_User_Id)
                .Select(g => g.First())
                .Select(u => new UserLimitedDto
                {
                    User_Id =u.Sender_User_Id,
                    Username = u.Sender_Username,
                    Profile_Picture = u.Sender_Profile_Picture
                })
                .ToList();
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

        public async Task<bool> SendMessage(string receiver, Message message)
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

                return await _context.SaveChangesAsync() > 0;
            }

            else return false;
        }
    }
}

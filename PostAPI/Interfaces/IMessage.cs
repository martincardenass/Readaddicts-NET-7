using PostAPI.Dto;
using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface IMessage
    {
        IQueryable<MessageView> MessageJoinQuery();
        Task<List<MessageView>> GetUserMessages(string username);
        Task<MessageView> GetMessageById(int messageId);
        Task<List<MessageView>> GetConversation(int page, int pageSize, string receiver, string sender);
        Task<List<UserLimitedDto>> GetUsersThatHaveMessagedMe();
        Task<bool> IsUserReceiver(int userId);
        Task<MessageView> SendMessage(string receiver, Message message);
    }
}

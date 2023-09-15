using PostAPI.Dto;
using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface IUser
    {
        Task<List<User>> GetUsers();
        Task<(List<User>, List<ReaderTier>)> ShowcaseUsers();
        Task<string> CreateUser(User user, IFormFile file);
        Task<User> UpdateUser(UserUpdateDto user, IFormFile file);
        Task<bool> DeleteUser(int userId);
        Task<User> GetUser(string username);
        Task<UserView> GetUserView(string username);
        Task<UserLimitedDto> GetUserLimited(string username);
        Task<User> GetUserById(int userId);
        Task<User> GetUserByUsername(string name);
        Task<bool> UserExists(string username);
        Task<bool> UserIdExists(int userId);
        Task<bool> EmailExists(string email);
        Task<string> GetHashedPassword(string username);
        Task<(string token, UserLimitedDto userLimited)> LoginUser(UserDto user);
        Task<List<Group>> GetUserGroups(string username);
    }
}

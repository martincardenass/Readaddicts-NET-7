using PostAPI.Dto;
using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface IUser
    {
        Task<List<User>> GetUsers();
        Task<string> CreateUser(User user, IFormFile file);
        Task<User> UpdateUser(UserUpdateDto user, IFormFile file);
        Task<bool> DeleteUser(int userId);
        Task<User> GetUser(string username);
        Task<UserLimitedDto> GetUserLimited(string username);
        Task<User> GetUserById(int userId);
        Task<bool> UserExists(string username);
        Task<bool> UserIdExists(int userId);
        Task<bool> EmailExists(string email);
        Task<string> GetHashedPassword(string username);
    }
}

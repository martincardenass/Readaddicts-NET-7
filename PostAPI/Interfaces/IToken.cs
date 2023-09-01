using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface IToken
    {
        Task<(int id, string role)> DecodeHS512Token();
        string JwtTokenGenerator(User user);
        Task<bool> IsUserAuthorized();
    }
}

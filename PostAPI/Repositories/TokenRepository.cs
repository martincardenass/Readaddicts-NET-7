using PostAPI.Interfaces;

namespace PostAPI.Repositories
{
    public class TokenRepository : IToken
    {
        private readonly IUser _userService;

        public TokenRepository(IUser userService)
        {
            _userService = userService;
        }
        public async Task<int> ExtractIdFromToken()
        {
            var token = await _userService.GetToken();
            if (token == null) return 0;

            var (id, _) = await _userService.DecodeHS512(token);
            return id;
        }
    }
}

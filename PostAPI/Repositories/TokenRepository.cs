using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PostAPI.Interfaces;
using PostAPI.Models;
using PostAPI.OptionsSetup;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PostAPI.Repositories
{
    public class TokenRepository : IToken
    {
        private readonly IHttpContextAccessor _http;
        private readonly IOptions<JwtOptions> _options;

        public TokenRepository(IHttpContextAccessor http, IOptions<JwtOptions> options)
        {
            _http = http;
            _options = options;
        }

        public Task<(int id, string role, string username)> DecodeHS512Token()
        {
            string token = _http.HttpContext.Request.Headers.Authorization.ToString()
                .Replace("Bearer", "").Trim();

            if (token == null) return null;

            JwtSecurityTokenHandler tokenHandler = new();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            string? idString = jwtToken.Claims.FirstOrDefault(i => i.Type == "nameid")?.Value;
            _ = int.TryParse(idString, out int id);

            string? role = jwtToken.Claims.FirstOrDefault(r => r.Type == "role")?.Value;

            string? username = jwtToken.Claims.FirstOrDefault(u => u.Type == "unique_name")?.Value;

            return Task.FromResult((id, role, username));
        }

        public async Task<bool> IsUserAuthorized()
        {
            var (id, role, _) = await DecodeHS512Token();

            if (id != 0 || role == "Admin") return true;
            else return false;
        }

        public string JwtTokenGenerator(User user)
        {
            // * Set up the JWT payload
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.User_Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Value.Issuer,
                Audience = _options.Value.Audience,
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Value.SecretKey)), SecurityAlgorithms.HmacSha512Signature)
            };

            // * Generate the JWT Token
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}

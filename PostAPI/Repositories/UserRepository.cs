using PostAPI.Interfaces;
using PostAPI.Models;
using Microsoft.EntityFrameworkCore;
using PostAPI.Dto;

namespace PostAPI.Repositories
{
    public class UserRepository : IUser
    {
        private readonly AppDbContext _context;
        private readonly IImage _imageService;
        private readonly IToken _tokenService;
        private readonly ITier _tierService;

        public UserRepository(AppDbContext context, IImage imageService, IToken tokenService, ITier tierService)
        {
            _context = context;
            _imageService = imageService;
            _tokenService = tokenService;
            _tierService = tierService;
        }

        public async Task<string> CreateUser(User user, IFormFile file)
        {
            string salt = BCrypt.Net.BCrypt.GenerateSalt();
            string hashedPw = BCrypt.Net.BCrypt.HashPassword(user.Password, salt);

            // * Get the value of the Profile_Picture field from the payload. Upload to Cloudinary...
            string? profilePictureUrl = await _imageService.UploadProfilePicture(file); // * This should add the string URL

            var newUser = new User()
            {
                Username = user.Username.ToLower(), // * Required
                First_Name = user.First_Name,
                Last_Name = user.Last_Name,
                Created = DateTime.Now,
                Email = user.Email.ToLower(), // * Required
                Role = "user", // * Required
                Password = hashedPw, // * Required
                Gender = user.Gender,
                Birthday = user.Birthday,
                Profile_Picture = profilePictureUrl, // * And save the URL on the database
                Status = user.Status,
                Last_Login = user.Last_Login,
                Tier_Id = user.Tier_Id
            };

            _context.Add(newUser);
            _ = await _context.SaveChangesAsync() > 0;

            var userToLogin = await GetUser(newUser.Username);
            var token = _tokenService.JwtTokenGenerator(userToLogin);

            return token; // * Return the token when we create the user so we can login after creating the user
        }

        public async Task<bool> DeleteUser(int userId)
        {
            var (id, _, _) = await _tokenService.DecodeHS512Token();

            var user = await GetUserById(userId);
            if (user == null) return false;

            if (id == userId && await _tokenService.IsUserAuthorized())
            {
                _context.Users.Remove(user);
                return await _context.SaveChangesAsync() > 0;
            }
            else return false;
        }

        public async Task<bool> EmailExists(string email)
        {
            return await _context.Users.AnyAsync(e => e.Email == email);
        }

        public async Task<string> GetHashedPassword(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(p => p.Username == username);
            return user?.Password;
        }

        public async Task<User> GetUser(string username)
        {
            return await _context.Users.Where(u => u.Username == username).FirstOrDefaultAsync();
        }

        public async Task<User> GetUserById(int userId)
        {
            var (id, _, _) = await _tokenService.DecodeHS512Token();
            if (id == userId && await _tokenService.IsUserAuthorized())
                return await _context.Users.Where(u => u.User_Id == userId).FirstOrDefaultAsync();

            else return null;
        }

        public async Task<User> GetUserByUsername(string name)
        {
            var (_, _, username) = await _tokenService.DecodeHS512Token();

            if (username == name && await _tokenService.IsUserAuthorized())
                return await _context.Users.Where(u => u.Username == name).FirstOrDefaultAsync();

            else return null;
        }

        public async Task<List<Group>> GetUserGroups(string username)
        {
            // * Get the userId from the username
            var userId = await _context.Users
                .Where(u => u.Username == username)
                .Select(u => u.User_Id)
                .FirstOrDefaultAsync();

            // * Get the groups Ids that the user is registered on
            var groupIds = await _context.GroupsRelations
                .Where(g => g.User_Id == userId)
                .Select(g => g.Group_Id)
                .ToListAsync();

            // * Returns a list of all the groups
            var groups = await _context.Groups
                .Where(g => groupIds.Contains(g.Group_Id))
                .ToListAsync();

            return groups;
        }

        public async Task<UserLimitedDto> GetUserLimited(string username)
        {
            // * A limited version of the user to store an object in the localstorage
            return await _context.Users
                .Where(user => user.Username == username)
                .Select(user => new UserLimitedDto
                {
                    User_Id = user.User_Id,
                    Username = user.Username,
                    Profile_Picture = user.Profile_Picture
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<User>> GetUsers()
        {
            return await _context.Users.OrderBy(user => user.User_Id).ToListAsync();
        }

        public async Task<UserView> GetUserView(string username)
        {
            var user = await _context.Users
                .GroupJoin(
                    _context.ReaderTiers,
                    user => user.Tier_Id,
                    tier => tier.Tier_Id,
                    (user, tier) => new { user, tier })
                .SelectMany(
                    joinResult => joinResult.tier.DefaultIfEmpty(),
                    (joinResult, tier) => new { joinResult.user, tier })
                .Select(result => new UserView
                {
                    User_Id = result.user.User_Id,
                    Username = result.user.Username,
                    First_Name = result.user.First_Name,
                    Last_Name = result.user.Last_Name,
                    Created = result.user.Created,
                    Email = result.user.Email,
                    Role = result.user.Role,
                    Gender = result.user.Gender,
                    Profile_Picture = result.user.Profile_Picture,
                    Bio = result.user.Bio,
                    Status = result.user.Status,
                    Last_Login = result.user.Last_Login,
                    Tier_Id = result.user.Tier_Id,
                    Tier_Name = result.tier.Tier_Name
                })
                .Where(u => u.Username == username)
                .FirstOrDefaultAsync();

            return user;
        }

        public async Task<(string token, UserLimitedDto userLimited)> LoginUser(UserDto user)
        {
            var userLogin = await GetUser(user.Username);

            var userLimited = await GetUserLimited(user.Username); // * This is what we will return to the user

            var token = _tokenService.JwtTokenGenerator(userLogin);

            if(token != null && userLogin != null)
            {
                userLogin.Last_Login = DateTime.UtcNow;
                _ = await _context.SaveChangesAsync();
            }

            return (token, userLimited);
        }

        public async Task<(List<User>, List<ReaderTier>)> ShowcaseUsers()
        {
            // * Loading the whole users into memory. Might need to do pagination
            var users = await _context.Users
                .OrderBy(user => user.User_Id)
                .ToListAsync();

            var usersDto = new List<User>();

            var tiers = await _tierService.GetReaderTiers();

            // * O(n) can be avoided here? I dont think so
            foreach (var user in users)
            {
                var newUsers = new User
                {
                    User_Id = user.User_Id,
                    Username = user.Username,
                    First_Name = user.First_Name,
                    Last_Name = user.Last_Name,
                    Created = user.Created,
                    Role = user.Role,
                    Gender = user.Gender,
                    Birthday = user.Birthday,
                    Profile_Picture = user.Profile_Picture,
                    Bio = user.Bio,
                    Status = user.Status,
                    Tier_Id = user.Tier_Id
                };
                
                usersDto.Add(newUsers);
            }

            return (usersDto, tiers);
        }

        public async Task<User> UpdateUser(UserUpdateDto user, IFormFile? file)
        {
            var (id, _, _) = await _tokenService.DecodeHS512Token();
            string? hashedPw = null;

            if (!string.IsNullOrEmpty(user.Password))
            {
                string salt = BCrypt.Net.BCrypt.GenerateSalt();
                hashedPw = BCrypt.Net.BCrypt.HashPassword(user.Password, salt);
            }
            
            string? profilePictureUrl = await _imageService.UploadProfilePicture(file);

            var existingUser = _context.Users.Find(id);

            if (existingUser != null)
            {
                // * Might use AutoMapper for this
                existingUser.First_Name = user.First_Name ?? existingUser.First_Name;
                existingUser.Last_Name = user.Last_Name ?? existingUser.Last_Name;
                existingUser.Email = user.Email ?? existingUser.Email;
                existingUser.Password = hashedPw ?? existingUser.Password;
                existingUser.Gender = user.Gender ?? existingUser.Gender;
                existingUser.Birthday = user.Birthday ?? existingUser.Birthday;
                existingUser.Profile_Picture = profilePictureUrl ?? existingUser.Profile_Picture;
                existingUser.Bio = user.Bio ?? existingUser.Bio;
                existingUser.Status = user.Status ?? existingUser.Status;

                _context.Update(existingUser);

                _ = await _context.SaveChangesAsync() > 0;

                return existingUser;
            }
            else return null;
        }

        public async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> UserIdExists(int userId)
        {
            return await _context.Users.AnyAsync(i => i.User_Id == userId);
        }
    }
}

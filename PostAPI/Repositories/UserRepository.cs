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

        public UserRepository(AppDbContext context, IImage imageService, IToken tokenService)
        {
            _context = context;
            _imageService = imageService;
            _tokenService = tokenService;
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
                Role = user.Role.ToLower(), // * Required
                Password = hashedPw, // * Required
                Gender = user.Gender,
                Birthday = user.Birthday,
                Profile_Picture = profilePictureUrl, // * And save the URL on the database
                Status = user.Status,
                Last_Login = user.Last_Login,
            };

            _context.Add(newUser);
            _ = await _context.SaveChangesAsync() > 0;

            var userToLogin = await GetUser(newUser.Username);
            var token = _tokenService.JwtTokenGenerator(userToLogin);

            return token; // * Return the token when we create the user so we can login after creating the user
        }

        public async Task<bool> DeleteUser(int userId)
        {
            var (id, _) = await _tokenService.DecodeHS512Token();

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
            var (id, _) = await _tokenService.DecodeHS512Token();
            if (id == userId && await _tokenService.IsUserAuthorized())
                return await _context.Users.Where(u => u.User_Id == userId).FirstOrDefaultAsync();

            else return null;
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

        public async Task<User> UpdateUser(UserUpdateDto user, IFormFile? file)
        {
            var (id, _) = await _tokenService.DecodeHS512Token();
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostAPI.Dto;
using PostAPI.Interfaces;
using PostAPI.Models;

namespace PostAPI.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUser _userService;
        private readonly IPost _postService;
        private readonly IComment _commentService;
        private readonly IToken _tokenService;

        public UserController(IUser userService, IPost postService, IComment commentService, IToken tokenService)
        {
            _userService = userService;
            _postService = postService;
            _commentService = commentService;
            _tokenService = tokenService;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<User>))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userService.GetUsers();
            var usersDto = new List<User>();

            foreach(var user in users)
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
                    Status = user.Status
                };

                usersDto.Add(newUsers);
            }

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(usersDto);
        }

        [HttpPost("Validator/UsernameExists/{username}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ValidateIfUsernameExists(string username)
        {
            bool exists = await _userService.UserExists(username);

            if (!exists) return Ok(new { NotOk = "Username does not exist"});

            return Ok(new { Ok = "Username exists"});
        }

        [HttpPost("Validator/EmailExists/{email}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ValidateIfEmailExists(string email)
        {
            bool exists = await _userService.EmailExists(email);

            if (!exists) return Ok(new { NotOk = "Email does not exist" });

            return Ok(new { Ok = "Email exists"});
        }


        [HttpGet("id/{userId}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200, Type = typeof(User))]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetUserById(int userId)
        {
            var user = await _userService.GetUserById(userId);
            bool exists = await _userService.UserIdExists(userId);

            if (user == null) return Unauthorized();

            if (!exists)
                return NotFound("User does not exist");
                
            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(user);
        }

        [HttpGet("username/{username}")]
        [ProducesResponseType(200, Type = typeof(User))]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var user = await _userService.GetUser(username);

            bool exists = await _userService.UserExists(username);

            if (!exists)
                return NotFound($"The user {username} does not exist");

            var userDto = new User
            {
                User_Id = user.User_Id,
                Username = user.Username,
                First_Name = user.First_Name,
                Last_Name = user.Last_Name,
                Created = user.Created,
                Email = user.Email,
                Role = user.Role,
                Gender = user.Gender,
                Profile_Picture = user.Profile_Picture,
                Bio = user.Bio,
                Status = user.Status,
                Last_Login = user.Last_Login
            };

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(userDto);
        }

        [HttpGet("{username}/posts")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<Post>))]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPostsByUserId(string username)
        {
            var posts = await _postService.GetUserPostsByUsername(username);

            bool userExists = await _userService.UserExists(username);

            if (!userExists)
                return NotFound($"User with username {username} does not exist");

            if(posts.Count == 0) return NotFound($"Seems like {char.ToUpper(username[0]) + username[1..]} has not posted anything yet.");

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(posts);
        }

        [HttpGet("{username}/comments")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<CommentView>))]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetCommentsByUserId(string username)
        {
            var comments = await _commentService.GetCommentsByUsername(username);

            bool userExists = await _userService.UserExists(username);

            if (!userExists)
                return NotFound($"User with username {username} does not exist");

            if (comments.Count == 0) return NotFound($"Seems like {char.ToUpper(username[0]) + username[1..]} has not commented anything yet.");

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(comments);
        }

        [HttpPost("signup")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateUser([FromForm] User user, [FromForm] IFormFile? imageFile)
        {
            var validator = new UserValidator(_userService);
            var validationResult = await validator.ValidateAsync(user);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(error => new ValidationError
                {
                    Field = error.PropertyName,
                    Error = error.ErrorMessage
                }).ToList();

                return BadRequest(errors);
            };

            string token = await _userService.CreateUser(user, imageFile);

            // * Get the userLimitedDto info (id, username and picture)
            var userLimited = await _userService.GetUserLimited(user.Username);

            if (token == "")
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(new { Token = token, User = userLimited });
        }

        [HttpPost("login")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Login([FromBody] UserDto user)
        {
            bool exists = await _userService.UserExists(user.Username);

            if (!exists)
                return BadRequest("This username does not exist");

            var hashedPassword = await _userService.GetHashedPassword(user.Username);

            if (!BCrypt.Net.BCrypt.Verify(user.Password, hashedPassword))
                return BadRequest("Wrong password");

            if(!ModelState.IsValid) return BadRequest(ModelState);

            var userLogin = await _userService.GetUser(user.Username);

            var userLimited = await _userService.GetUserLimited(user.Username);

            var token = _tokenService.JwtTokenGenerator(userLogin);

            return Ok(new { Token = token, User = userLimited });
        }

        [HttpPatch("update")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpdateUser([FromForm] UserUpdateDto user, [FromForm] IFormFile? imageFile)
        {
            var validator = new UserUpdateDtoValidator(_userService);
            var validatorResult = await validator.ValidateAsync(user);

            if (!validatorResult.IsValid)
            {
                var errors = validatorResult.Errors.Select(error => new ValidationError
                {
                    Field = error.PropertyName,
                    Error = error.ErrorMessage
                }).ToList();

                return BadRequest(errors);
            }

            if (!ModelState.IsValid)
                return BadRequest();

            var updated = await _userService.UpdateUser(user, imageFile);

            if (updated == null)
            {
                return BadRequest("No field changes");
            }

            return Ok(updated);
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            bool exists = await _userService.UserIdExists(id);

            if (!exists)
                return NotFound("This user does not exist or has already been deleted");

            var user = await _userService.GetUserById(id);

            bool deleted = await _userService.DeleteUser(id);

            if (!deleted)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok("User deleted");
        }
    }
}

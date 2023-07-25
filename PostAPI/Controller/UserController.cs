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

        public UserController(IUser userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<User>))]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userService.GetUsers();

            return Ok(users);
        }

        [HttpPost("signup")]
        public async Task<IActionResult> CreateUser(User user)
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

            bool created = await _userService.CreateUser(user);

            if (!created)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok($"User: {user.Username} was created");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserDto user)
        {
            bool exists = await _userService.UserExists(user.Username);

            if (!exists)
                return BadRequest($"{user.Username} does not exist");

            var hashedPassword = await _userService.GetHashedPassword(user.Username);

            if (!BCrypt.Net.BCrypt.Verify(user.Password, hashedPassword))
                return BadRequest("Wrong password");

            if(!ModelState.IsValid) return BadRequest(ModelState);

            var userLogin = await _userService.GetUser(user.Username);

            var token = _userService.JwtTokenGenerator(userLogin);

            return Ok(token);
        }

        [HttpPatch("update")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(400)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateUser([FromBody] UserUpdateDto user)
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

            var updated = await _userService.UpdateUser(user);

            if (!updated)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            return Ok("User updated");
        }
        [HttpDelete("delete/{id}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(400)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            bool exists = await _userService.UserIdExists(id);

            if (!exists)
                return NotFound("This user does not exist or has already been deleted");

            var user = await _userService.GetUserById(id);

            bool deleted = await _userService.DeleteUser(user);

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

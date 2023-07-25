using FluentValidation;
using PostAPI.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PostAPI.Models
{
    public class User
    {
        [Key]
        public int User_Id { get; set; }
        public string? Username { get; set; } // * Required
        public string? First_Name { get; set; }
        public string? Last_Name { get; set; }
        public DateTime? Created { get; set; }
        public string? Email { get; set; } // * Required
        public string? Role { get; set; } // * Required
        public string? Password { get; set; } // * Required
        public string? Gender { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Profile_Picture { get; set; }
        public string? Bio { get; set; }
        public string? Status { get; set; }
        public DateTime? Last_Login { get; set; }
    }

    public class UserValidator : AbstractValidator<User>
    {
        private readonly IUser _userService;
        public UserValidator(IUser userService)
        {
            _userService = userService;
            RuleFor(x => x.Username).NotEmpty().MaximumLength(16).MinimumLength(4)
                .MustAsync(
                async( Username, cancellation ) =>
                {
                    bool exists = await _userService.UserExists(Username);
                    return !exists;
                }
                ).WithMessage("This username already exists");
            RuleFor(x => x.First_Name).MaximumLength(64);
            RuleFor(x => x.Last_Name).MaximumLength(64);
            RuleFor(x => x.Email).NotEmpty().MaximumLength(128).EmailAddress()
                .MustAsync(
                async ( Email, cancellation ) =>
                {
                    bool exists = await _userService.EmailExists(Email);
                    return !exists;
                }
                ).WithMessage("This email is already registered");
            RuleFor(x => x.Role).NotEmpty();
            RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
            RuleFor(x => x.Gender).MaximumLength(20);
            RuleFor(x => x.Profile_Picture).MaximumLength(255);
            RuleFor(x => x.Bio).MaximumLength(255);
            RuleFor(x => x.Status).MaximumLength(20);   
        }
    }
}

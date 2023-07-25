using FluentValidation;
using PostAPI.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PostAPI.Dto
{
    // * This stores only values that can be updated
    public class UserUpdateDto
    {
        [Key]
        public string? First_Name { get; set; }
        public string? Last_Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Profile_Picture { get; set; }
        public string? Bio { get; set; }
        public string? Status { get; set; }
    }

    public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
    {
        private readonly IUser _userService;

        public UserUpdateDtoValidator(IUser userService)
        {
            _userService = userService;
            RuleFor(x => x.First_Name).MaximumLength(64);
            RuleFor(x => x.Last_Name).MaximumLength(64);
            RuleFor(x => x.Email).MaximumLength(128).EmailAddress()
                .MustAsync(
                async ( Email, cancellation ) =>
                {
                    bool exists = await _userService.EmailExists(Email);
                    return !exists;
                }
                ).WithMessage("This email is already registered");
            RuleFor(x => x.Password).MaximumLength(128);
            RuleFor(x => x.Gender).MaximumLength(20);
            RuleFor(x => x.Profile_Picture).MaximumLength(255);
            RuleFor(x => x.Bio).MaximumLength(255);
            RuleFor(x => x.Status).MaximumLength(20);
        }
    }
}

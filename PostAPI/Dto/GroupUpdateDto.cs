using FluentValidation;

namespace PostAPI.Dto
{
    public class GroupUpdateDto
    {
        // * Only group values that can be updated
        public string? Group_Name { get; set; }
        public string? Group_Description { get; set; }
        public string? Group_Picture { get; set; }
    }

    public class GroupUpdateDtoValidator : AbstractValidator<GroupUpdateDto>
    {
        public GroupUpdateDtoValidator()
        {
            RuleFor(x => x.Group_Name).MaximumLength(255).MinimumLength(4);
            RuleFor(x => x.Group_Description).MaximumLength(255).MinimumLength(4);
            RuleFor(x => x.Group_Picture).MaximumLength(255);
        }
    }
}

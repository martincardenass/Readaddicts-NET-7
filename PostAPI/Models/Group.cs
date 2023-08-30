using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace PostAPI.Models
{
    public class Group
    {
        [Key]
        public int Group_Id { get; set; }
        public string Group_Name { get; set; }
        public string? Group_Description { get; set; }
        public int Group_Owner { get; set; }
        public string? Group_Picture { get; set; }
    }

    public class GroupValidator : AbstractValidator<Group>
    {
        public GroupValidator()
        {
            RuleFor(x => x.Group_Name).NotEmpty().MaximumLength(255).MinimumLength(4);
            RuleFor(x => x.Group_Description).NotEmpty().MaximumLength(255).MinimumLength(4);
        }
    }
}

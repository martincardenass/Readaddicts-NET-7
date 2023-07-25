using FluentValidation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostAPI.Models
{
    public class Comment
    {
        [Key]
        public int Comment_Id { get; set; }
        [ForeignKey("Users")]
        public int? User_Id { get; set; } // Comments can be anonymus. Thats why its nullable ?
        [ForeignKey("Posts")]
        public int Post_Id { get; set; }
        public string Content { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public bool Anonymous { get; set; }
    }

    public class CommentValidator : AbstractValidator<Comment>
    {
        public CommentValidator()
        {
            RuleFor(x => x.Content).NotEmpty().MaximumLength(255).MinimumLength(8);
        }
    }
}

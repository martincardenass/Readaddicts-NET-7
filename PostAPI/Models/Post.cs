using FluentValidation;
using PostAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostAPI.Models
{
    public class Post
    {
        [Key]
        public int Post_Id { get; set; }
        [ForeignKey("Users")]
        public int User_Id { get; set; }
        public DateTime Created { get; set; }
        public string Content { get; set; }
        public DateTime Modified { get; set; }
    }
}

public class PostValidator : AbstractValidator<Post>
{
    public PostValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(255).MinimumLength(8);
    }
}
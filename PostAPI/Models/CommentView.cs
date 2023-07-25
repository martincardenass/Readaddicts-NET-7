using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostAPI.Models
{
    public class CommentView
    {
        [Key]
        public int Comment_Id { get; set; }
        [ForeignKey("Users")]
        public int? User_Id { get; set; }
        [ForeignKey("Posts")]
        public int Post_Id { get; set; }
        public string Content { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public bool Anonymous { get; set; }
        public string Author { get; set; }
    }
}

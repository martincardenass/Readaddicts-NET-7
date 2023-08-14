using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostAPI.Models
{
    public class PostView
    {
        [Key]
        public int Post_Id { get; set; }
        [ForeignKey("Users")]
        public int User_Id { get; set; }
        public string Author { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string Content { get; set; }
        public string? Profile_Picture { get; set; }
        public string? First_Name { get; set; }
        public string? Last_Name { get; set; }
        public int? Comments { get; set; }
    }
}

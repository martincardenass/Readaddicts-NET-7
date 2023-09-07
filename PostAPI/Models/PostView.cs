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
        public int? Group_Id { get; set; }
        public ICollection<Image> Images { get; set; }
        public Group Group { get; set; }
        // * Used to track if user its allowed to see the post
        // * Ex: if not a group member Allowed will be falsy
        public bool Allowed { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace PostAPI.Models
{
    public class PostView
    {
        [Key]
        public int Post_Id { get; set; }
        public string Author { get; set; }
        public DateTime Created { get; set; }
        public string Content { get; set; }
    }
}

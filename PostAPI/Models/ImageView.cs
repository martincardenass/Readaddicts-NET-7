using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PostAPI.Models
{
    public class ImageView
    {
        [Key]
        public int Image_Id { get; set; }
        [ForeignKey("Posts")]
        public int Post_Id { get; set; }
        [ForeignKey("Users")]
        public int User_Id { get; set; }
        public string Image_Url { get; set; }
        public string Author { get; set; }
    }
}

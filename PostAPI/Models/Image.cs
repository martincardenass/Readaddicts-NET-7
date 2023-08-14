using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostAPI.Models
{
    public class Image
    {
        [Key]
        public int Image_Id { get; set; }
        [ForeignKey("Posts")]
        public int Post_Id { get; set; }
        [ForeignKey("Users")]
        public int User_Id { get; set; }
        public string Image_Url { get; set; }
    }
}

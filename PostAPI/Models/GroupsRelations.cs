using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostAPI.Models
{
    public class GroupsRelations
    {
        [Key]
        public int User_Group_Id { get; set; }
        [ForeignKey("Users")]
        public int User_Id { get; set; }
        [ForeignKey("Groups")]
        public int Group_Id { get; set; }
    }
}

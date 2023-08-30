using System.ComponentModel.DataAnnotations;

namespace PostAPI.Models
{
    public class GroupView
    {
        [Key]
        public int Group_Id { get; set; }
        public string Group_Name { get; set; }
        public string Group_Description { get; set; }
        public int Group_Owner { get; set; }
        public string Group_Picture { get; set; }
        public ICollection<User?> Members { get; set;}
        public int? Members_Count { get; set; }
        public User Owner { get; set; }

    }
}

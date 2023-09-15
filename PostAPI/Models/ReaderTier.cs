using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostAPI.Models
{
    public class ReaderTier
    {
        [Key]
        public int Tier_Id { get; set; }
        public string Tier_Name { get; set; }
        public string Tier_Description { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostAPI.Models
{
    public class Message
    {
        [Key]
        public int Message_Id { get; set; }
        [ForeignKey("Users")]
        public int Sender { get; set; }
        [ForeignKey("Users")]
        public int Receiver { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Is_Read { get; set; }
    }
}

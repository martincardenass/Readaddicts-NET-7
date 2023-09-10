namespace PostAPI.Models
{
    public class MessageView
    {
        public int Sender_User_Id { get; set; }
        public string Sender_Username { get; set; }
        public string Sender_Profile_Picture { get; set; }
        public int Receiver_User_Id { get; set; }
        public string Receiver_Username { get; set; }
        public string Receiver_Profile_Picture { get; set; }
        public int Message_Id { get; set; }
        public int Sender { get; set; }
        public int Receiver { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Is_Read { get; set; }
    }
}

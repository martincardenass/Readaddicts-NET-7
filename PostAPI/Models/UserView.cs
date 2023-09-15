namespace PostAPI.Models
{
    public class UserView
    {
        public int User_Id { get; set; }
        public string? Username { get; set; } // * Required
        public string? First_Name { get; set; }
        public string? Last_Name { get; set; }
        public DateTime? Created { get; set; }
        public string? Email { get; set; } // * Required
        public string? Role { get; set; } // * Required
        //public string? Password { get; set; } // * Required
        public string? Gender { get; set; }
        //public DateTime? Birthday { get; set; }
        public string? Profile_Picture { get; set; }
        public string? Bio { get; set; }
        public string? Status { get; set; }
        public DateTime? Last_Login { get; set; }
        public int Tier_Id { get; set; }
        public string Tier_Name { get; set; }
    }
}

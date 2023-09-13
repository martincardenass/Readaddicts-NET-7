using PostAPI.Models;

namespace PostAPI.Dto
{
    public class UserLimitedDto
    {
        public int User_Id { get; set; }
        public string? Username { get; set; } // * Required
        public string? Profile_Picture { get; set; }
    }
}

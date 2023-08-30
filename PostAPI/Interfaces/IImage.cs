using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface IImage
    {
        Task<List<ImageView>> GetImagesByPostId(int postId);
        Task<bool> PostHasImages(int postId);
        Task<string?> UploadImage(IFormFile file);
        Task<string> UploadProfilePicture(IFormFile file);
    }
}

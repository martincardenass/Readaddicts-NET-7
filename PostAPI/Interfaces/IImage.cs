using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface IImage
    {
        Task<List<ImageView>> GetImagesByPostId(int postId);
        Task<bool> PostHasImages(int postId);
        Task<string?> UploadImage(IFormFile file);
        Task<bool> AddImageToPost(IFormFile file, int postId);
    }
}

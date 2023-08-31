using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.EntityFrameworkCore;
using PostAPI.Interfaces;
using PostAPI.Models;

namespace PostAPI.Repositories
{
    public class ImageRepository : IImage
    {
        private readonly AppDbContext _context;
        private readonly Cloudinary _cloudinary;

        public ImageRepository(AppDbContext context, Cloudinary cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        public async Task<List<ImageView>> GetImagesByPostId(int postId)
        {
            return await _context.Images.Join(
                _context.Users,
                images => images.User_Id,
                user => user.User_Id,
                (image, user) => new ImageView
                {
                    Image_Id = image.Image_Id,
                    User_Id = image.User_Id,
                    Post_Id = image.Post_Id,
                    Image_Url = image.Image_Url,
                    Author = user.Username
                })
                .Where(i => i.Post_Id == postId).ToListAsync();
        }

        public async Task<List<Image>> GetImagesByPostIdNotView(int postId)
        {
            return await _context.Images
                .Where(i => i.Post_Id == postId)
                .ToListAsync();
        }

        public async Task<bool> PostHasImages(int postId)
        {
            return await _context.Images.AnyAsync(i => i.Post_Id == postId);
        }

        public async Task<string?> UploadImage(IFormFile file)  
        {
            if(file != null && file.Length > 0)
            {
                await using var stream = file.OpenReadStream();

                var uploadParams = new ImageUploadParams
                {

                    File = new FileDescription(file.FileName, stream), //  * Get the uploade image and set the name to the image file name
                    UseFilename = true, UniqueFilename = true, Overwrite = true
                };
                
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    // * Return an error message if an exception happens
                    throw new Exception(uploadResult.Error.Message);
                }

                return uploadResult.SecureUrl.ToString();
            }
            return null;
        }

        public async Task<string> UploadProfilePicture(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                // * Dispose the resources being used here
                await using var stream = file.OpenReadStream();

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Transformation = new Transformation().Width(300).Height(300).Crop("fill"),
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = true
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    // * Return an error message if an exception happens
                    throw new Exception(uploadResult.Error.Message);
                }

                return uploadResult.SecureUrl.ToString();
            }
            return null; // * If no file is provided
        }
    }
}

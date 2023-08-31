using Microsoft.EntityFrameworkCore;
using PostAPI.Interfaces;
using PostAPI.Models;

namespace PostAPI.Repositories
{
    public class PostRepository : IPost
    {
        private readonly AppDbContext _context;
        private readonly IUser _userService;
        private readonly IToken _tokenService;
        private readonly IComment _commentService;
        private readonly IImage _imageService;

        public PostRepository(AppDbContext context, IUser userService, IToken tokenService, IComment commentService, IImage imageService)
        {
            _context = context;
            _userService = userService;
            _tokenService = tokenService;
            _commentService = commentService;
            _imageService = imageService;
        }

        public async Task<bool> IdExists(int id)
        {  
            return await _context.Posts
                .AnyAsync(i => i.Post_Id == id);
        }

        public async Task<int> CreatePost(List<IFormFile> files, Post post, int? groupId)
        {
            int userId = await _tokenService.ExtractIdFromToken();

            // * Check if the group exists
            var findGroup = await _context.Groups.FindAsync(groupId);// * Return 0 will cause 500 server error

            // * Get the relations of the same groupId
            var relation = await _context.GroupsRelations.Where(g => g.Group_Id == groupId && g.User_Id == userId).FirstOrDefaultAsync();

            if (findGroup != null && relation.User_Id == userId)
            {
                var newPostWithGroup = new Post()
                {
                    User_Id = userId,
                    Created = DateTime.UtcNow,
                    Content = post.Content,
                    Group_Id = groupId    
                };

                _context.Add(newPostWithGroup);

                _ = await _context.SaveChangesAsync() > 0; // ** Add the new post and add the changes.

                await AddImagesToPost(files, newPostWithGroup.Post_Id);

                return newPostWithGroup.Post_Id;
            }

            // * If no groupId is provided, groupId will be equal to null; meaning the post does not have a group. Then just create a normal post
            else
            {
                var newPostWithoutGroup = new Post()
                {
                    User_Id = userId,
                    Created = DateTime.UtcNow,
                    Content = post.Content,
                    Group_Id = null // * No group
                };

                _context.Add(newPostWithoutGroup);

                _ = await _context.SaveChangesAsync() > 0;

                await AddImagesToPost(files, newPostWithoutGroup.Post_Id);

                return newPostWithoutGroup.Post_Id;
            }
        }

        public async Task<bool> DeletePost(Post post)
        {
            var toDelete = await _context.Posts.FindAsync(post.Post_Id);

            var comparasion = await CompareTokenPostId(post.Post_Id);
            var admin = await _userService.CheckAdminStatus();

            if (comparasion == true || admin)
            {
                var comments = await _commentService.GetComments(post.Post_Id);

                if(comments.Count > 0)
                {
                    var childComments = comments
                        .Where(comment => comment.Parent_Comment_Id.HasValue)
                        .OrderByDescending(comment => comment.Parent_Comment_Id)
                        .ToList();

                    
                    foreach (var comment in childComments)
                    {
                        _context.Remove(comment);
                        _ = await _context.SaveChangesAsync();
                    }

                    var parentComments = comments
                        .Except(childComments)
                        .ToList();

                    _context.RemoveRange(parentComments);
                }

                var images = await _imageService.GetImagesByPostIdNotView(post.Post_Id);

                if (images.Count > 0)
                    _context.RemoveRange(images);

                _context.Posts.Remove(toDelete);

                return await _context.SaveChangesAsync() > 0;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> UpdatePost(List<IFormFile> files, int id, Post post)
        {
            var comparasion = await CompareTokenPostId(id);
            var admin = await _userService.CheckAdminStatus();

            if (comparasion || admin)
            {
                var existingPost = await _context.Posts.FindAsync(id);

                if (existingPost == null)
                    return false;

                existingPost.Content = post.Content;
                existingPost.Modified = DateTime.UtcNow;

                _context.Update(existingPost);
                _ = await _context.SaveChangesAsync() > 0;

                await AddImagesToPost(files, id); // * Will update post with the provied images, if any

                _ = await _context.SaveChangesAsync() > 0;

                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> CompareTokenPostId(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);

            var idFromToken = await _tokenService.ExtractIdFromToken();
            var idFromPost = post.User_Id;

            return idFromToken == idFromPost;
        }

        public async Task<PostView> GetPostViewById(int id)
        {
            return await PostJoinQuery()
                .FirstOrDefaultAsync(p => p.Post_Id == id);
        }

        public async Task<List<PostView>> GetPosts(int page, int pageSize)
        {
            int postsToSkip = (page - 1) * pageSize;
            return await PostJoinQuery()
                .Where(post => post.Group_Id == null)
                .OrderByDescending(p => p.Created)
                .Skip(postsToSkip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Post> GetPostById(int id)
        {
            return await _context.Posts
                .FirstOrDefaultAsync(p => p.Post_Id == id);
        }

        public async Task<List<PostView>> GetUserPostsByUsername(string username)
        {
            return await PostJoinQuery()
                .OrderByDescending(p => p.Created)
                .Where(p => p.Author == username)
                .ToListAsync();
        }

        public IQueryable<PostView> PostJoinQuery()
        {
            return _context.Posts
                .GroupJoin(
                _context.Users,
                post => post.User_Id,
                user => user.User_Id,
                (post, user) => new { post, user })
                .SelectMany(joinResult => joinResult.user.DefaultIfEmpty(),
                (joinResult, user) => new { joinResult.post, user })
                .GroupJoin(
                _context.Comments,
                post => post.post.Post_Id,
                comment => comment.Post_Id,
                (post, comment) => new { post, CommentCount = comment.Count() })
                .Select(result => new PostView
                {
                    User_Id = result.post.post.Post_Id,
                    Post_Id = result.post.post.Post_Id,
                    Author = result.post.user.Username,
                    First_Name = result.post.user.First_Name,
                    Last_Name = result.post.user.Last_Name,
                    Created = result.post.post.Created,
                    Modified = result.post.post.Modified,
                    Content = result.post.post.Content,
                    Profile_Picture = result != null ? result.post.user.Profile_Picture : "No picture",
                    Comments = result.CommentCount,
                    Group_Id = result.post.post.Group_Id,
                    Images = _context.Images.Where(image => image.Post_Id == result.post.post.Post_Id).ToList()
                });
        }

        public async Task AddImagesToPost(List<IFormFile> files, int posttId)
        {
            if(files != null && files.Any(file => file.Length > 0))
            {
                foreach (var file in files) // * Iterate over every uploaded image and add it to the image table
                {
                    _ = await AddImageToPost(file, posttId);
                }
            }
        }

        public async Task<bool> AddImageToPost(IFormFile file, int postId)
        {
            string? imageUrl = await _imageService.UploadImage(file);

            var Id = await _tokenService.ExtractIdFromToken();

            var newImage = new Image
            {
                Post_Id = postId,
                User_Id = Id,
                Image_Url = imageUrl
            };

            _context.Add(newImage);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<PostView>> GetPostsByGroupId(int groupId)
        {
            return await PostJoinQuery()
                .Where(g => g.Group_Id == groupId)
                .ToListAsync();
        }

        public async Task DeleteRecursiveComments(Comment comment)
        {
            var childComments = _context.Comments
                .Where(c => c.Parent_Comment_Id == comment.Parent_Comment_Id)
                .ToList();

            foreach (var child in childComments)
            {
                DeleteRecursiveComments(child);
            }
        }
    }
}

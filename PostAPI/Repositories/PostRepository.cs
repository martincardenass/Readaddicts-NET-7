using Microsoft.EntityFrameworkCore;
using PostAPI.Interfaces;
using PostAPI.Models;

namespace PostAPI.Repositories
{
    public class PostRepository : IPost
    {
        private readonly AppDbContext _context;
        private readonly IToken _tokenService;
        private readonly IComment _commentService;
        private readonly IImage _imageService;

        public PostRepository(AppDbContext context, IToken tokenService, IComment commentService, IImage imageService)
        {
            _context = context;
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
            var(id, _, _) = await _tokenService.DecodeHS512Token();

            // * Check if the group exists
            var findGroup = await _context.Groups.FindAsync(groupId);// * Return 0 will cause 500 server error

            // * Get the relations of the same groupId
            var relation = await _context.GroupsRelations.Where(g => g.Group_Id == groupId && g.User_Id == id).FirstOrDefaultAsync();

            if (findGroup != null && relation.User_Id == id)
            {
                var newPostWithGroup = new Post()
                {
                    User_Id = id,
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
                    User_Id = id,
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
            var postToDelete = await _context.Posts.FindAsync(post.Post_Id);
            var (id, _, _) = await _tokenService.DecodeHS512Token();

            if (postToDelete.User_Id == id && await _tokenService.IsUserAuthorized())
            {
                var comments = await _commentService.GetComments(post.Post_Id);

                if (comments.Count > 0)
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

                _context.Posts.Remove(postToDelete);

                return await _context.SaveChangesAsync() > 0;
            }
            else return false;
        }

        public async Task<bool> UpdatePost(List<IFormFile> files, int postId, Post post)
        {
            var (id, _, _) = await _tokenService.DecodeHS512Token();
            var existingPost = await _context.Posts.FindAsync(postId);

            if (existingPost.User_Id == id && await _tokenService.IsUserAuthorized())
            {
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
            else return false;
        }

        public async Task<PostView?> GetPostViewById(int postId)
        {
            // * Load only the group_Id into memory. This way we dont save the whole post into the memory if the user its not authenticated to see the post
            var group_Id = await _context.Posts
                .Where(p => p.Post_Id == postId)
                .Select(p => p.Group_Id)
                .FirstOrDefaultAsync();

            if (!group_Id.HasValue)
                return await PostJoinQuery()
                    .FirstOrDefaultAsync(p => p.Post_Id == postId);

            // * If groupId , its a group post and needs authentication to see it
            if (group_Id.HasValue)
            {
                if (await CheckIfUserIsAGroupMember(group_Id.Value) && await _tokenService.IsUserAuthorized())
                    return await PostJoinQuery()
                        .FirstOrDefaultAsync(p => p.Post_Id == postId);

                else return new PostView
                {
                    Content = "Sorry, you are not allowed to see this post at this moment.",
                    Group = await _context.Groups
                        .Where(g => g.Group_Id == group_Id)
                        .FirstOrDefaultAsync(),
                    Allowed = false // * Not allowed to see the post. Used to handle conditional rendering in the frontend
                };
            }
            else return null;
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

        public async Task<List<PostView>> GetUserPostsByUsername(int page, int pageSize, string username)
        {
            int postsToSkip = (page - 1) * pageSize;
            return await PostJoinQuery()
                .Where(p => p.Author == username)
                .OrderByDescending(p => p.Created)
                .Skip(postsToSkip)
                .Take(pageSize)
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
                    Group = _context.Groups.FirstOrDefault(g => g.Group_Id == result.post.post.Group_Id),
                    Images = _context.Images.Where(image => image.Post_Id == result.post.post.Post_Id).ToList(),
                    Allowed = true // * Every method that uses PostJoinQuery its allowed to see the content of the post
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

            var (id, _, _) = await _tokenService.DecodeHS512Token();

            var newImage = new Image
            {
                Post_Id = postId,
                User_Id = id,
                Image_Url = imageUrl
            };

            _context.Add(newImage);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task DeleteRecursiveComments(Comment comment)
        {
            var childComments = await _context.Comments
                .Where(c => c.Parent_Comment_Id == comment.Parent_Comment_Id)
                .ToListAsync();

            foreach (var child in childComments)
            {
                await DeleteRecursiveComments(child);
            }
        }

        public async Task<bool> CheckIfUserIsAGroupMember(int groupId)
        {
            // * Extract userId from the auth headers (for the logged user)
            var (id, _, _) = await _tokenService.DecodeHS512Token();
            // * Create a list of all the relations table of the desired group. This will throw all of the members IDs that belong to the desired group
            var groupRelationships = await _context.GroupsRelations.Where(g => g.Group_Id == groupId).ToListAsync();
            // * Then we check if at least any of those user IDs match the userId that was extracted from the token, if they do it mens the user its a member of the desired group
            return groupRelationships.Any(user => user.User_Id == id);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
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

        public async Task<int> CreatePost(List<IFormFile> files, Post post)
        {
            int userId = await _tokenService.ExtractIdFromToken();

            var newPost = new Post()
            {
                User_Id = userId,
                Created = DateTime.UtcNow,
                Content = post.Content
            };

            _context.Add(newPost);
             
            _ = await _context.SaveChangesAsync() > 0; // ** Add the new post and add the changes.
        
            await AddImagesToPost(files, newPost.Post_Id);

            return newPost.Post_Id;
        }

        public async Task<bool> DeletePost(Post post)
        {
            var toDelete = await _context.Posts.FindAsync(post.Post_Id);

            var comparasion = await CompareTokenPostId(post.Post_Id);
            var admin = await _userService.CheckAdminStatus();

            var comments = await _commentService.GetComments(post.Post_Id);

            if (comparasion == true || admin)
            {
                _context.Posts.Remove(toDelete);
                // * This will delete all the comments of the post
                if(comments.Count > 0)
                {
                    foreach (var comment in comments)
                    {
                        _context.Comments.Remove(comment);
                    }
                }

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
            // * Pagination logic & left join of table to get profile picture and first and last name per post

            int postsToSkip = (page - 1) * pageSize;
            return await PostJoinQuery()
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
            return _context.Posts.GroupJoin(
                _context.Users,
                post => post.User_Id,
                user => user.User_Id,
                (posts, users) => new { posts, users })
                .SelectMany(
                x => x.users.DefaultIfEmpty(),
                (post, user) => new PostView
                {
                    User_Id = post.posts.User_Id,
                    Post_Id = post.posts.Post_Id,
                    Author = user.Username,
                    First_Name = user.First_Name,
                    Last_Name = user.Last_Name,
                    Created = post.posts.Created,
                    Modified = post.posts.Modified,
                    Content = post.posts.Content,
                    Profile_Picture = user != null ? user.Profile_Picture : "No picture",
                })
                .GroupJoin(
                _context.Comments,
                post => post.Post_Id,
                comment => comment.Post_Id,
                (post, comment) => new
                {
                    PostView = post,
                    Comments = comment.Count()
                })
                .Select(result => new PostView
                {
                    User_Id = result.PostView.User_Id,
                    Post_Id = result.PostView.Post_Id,
                    Author = result.PostView.Author,
                    First_Name = result.PostView.First_Name,
                    Last_Name = result.PostView.Last_Name,
                    Created = result.PostView.Created,
                    Modified = result.PostView.Modified,
                    Content = result.PostView.Content,
                    Profile_Picture = result != null ? result.PostView.Profile_Picture : "No picture",
                    Comments = result.Comments
                });
        }

        public async Task AddImagesToPost(List<IFormFile> files, int posttId)
        {
            if(files != null && files.Any(file => file.Length > 0))
            {
                foreach (var file in files) // * Iterate over every uploaded image and add it to the image table
                {
                    _ = await _imageService.AddImageToPost(file, posttId);
                }
            }
        }
    }
}

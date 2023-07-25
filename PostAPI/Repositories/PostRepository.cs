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

        public PostRepository(AppDbContext context, IUser userService, IToken tokenService)
        {
            _context = context;
            _userService = userService;
            _tokenService = tokenService;
        }

        public async Task<bool> IdExists(int id)
        {
            return await _context.Posts.AnyAsync(i => i.Post_Id == id);
        }

        public async Task<List<PostView>> GetPosts()
        {
            return await _context.PostsView.OrderBy(p => p.Post_Id).ToListAsync();
        }

        public async Task<bool> CreatePost(Post post)
        {
            int userId = await _tokenService.ExtractIdFromToken();

            var newPost = new Post()
            {
                User_Id = userId,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow,
                Content = post.Content
            };

            _context.Add(newPost);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeletePost(Post post)
        {
            var toDelete = await _context.Posts.FindAsync(post.Post_Id);

            var comparasion = await CompareTokenPostId(post.Post_Id);
            var admin = await _userService.CheckAdminStatus();

            if(comparasion == true || admin)
            {
                _context.Posts.Remove(toDelete);
                return await _context.SaveChangesAsync() > 0;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> UpdatePost(int id, Post post)
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
                return await _context.SaveChangesAsync() > 0;
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

        public async Task<Post> GetById(int id)
        {
            return await _context.Posts.FirstOrDefaultAsync(p => p.Post_Id == id);
        }
    }
}

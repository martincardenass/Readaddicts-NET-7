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

        public async Task<PostView> GetPostViewById(int id)
        {
            var post = await _context.PostsView.GroupJoin(
                _context.Users,
                post => post.Author,
                user => user.Username,
                (posts, users) => new { posts, users })
                .SelectMany(
                x => x.users.DefaultIfEmpty(),
                (post, user) => new PostView
                {
                    Post_Id = post.posts.Post_Id,
                    Author = post.posts.Author,
                    First_Name = user.First_Name,
                    Last_Name = user.Last_Name,
                    Created = post.posts.Created,
                    Content = post.posts.Content,
                    Profile_Picture = user != null ? user.Profile_Picture : "No picture",
                }
                ).FirstOrDefaultAsync(p => p.Post_Id == id);

            return post;
        }

        public async Task<List<PostView>> GetPosts(int page, int pageSize)
        {
            // * Some pagination logic and left join of tables to get the profile picture and first and last name per post
            int postsToSkip = (page - 1) * pageSize;
            var posts = await _context.PostsView.GroupJoin(
                _context.Users,
                post => post.Author,
                user => user.Username,
                (posts, users) => new { posts, users })
                .SelectMany(
                x => x.users.DefaultIfEmpty(),
                (post, user) => new PostView
                {
                    Post_Id = post.posts.Post_Id,
                    Author = post.posts.Author,
                    First_Name = user.First_Name,
                    Last_Name = user.Last_Name,
                    Created = post.posts.Created,
                    Content = post.posts.Content,
                    Profile_Picture = user != null ? user.Profile_Picture : "No picture",
                }
                ).Skip(postsToSkip).Take(pageSize).ToListAsync();
            return posts;
        }

        public async Task<Post> GetPostById(int id)
        {
            return await _context.Posts.FirstOrDefaultAsync(p => p.Post_Id == id);
        }
    }
}

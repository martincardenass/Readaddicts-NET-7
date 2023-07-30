using Microsoft.EntityFrameworkCore;
using PostAPI.Interfaces;
using PostAPI.Models;

namespace PostAPI.Repositories
{
    public class CommentRepository : IComment
    {
        private readonly AppDbContext _context;
        private readonly IToken _tokenService;
        private readonly IHttpContextAccessor _http;
        private readonly IUser _userService;

        public CommentRepository(AppDbContext context, IToken tokenService, IHttpContextAccessor http, IUser userService)
        {
            _context = context;
            _tokenService = tokenService;
            _http = http;
            _userService = userService;
        }

        public async Task<bool> CommentIdExists(int commentId)
        {
            return await _context.Comments.AnyAsync(i => i.Comment_Id == commentId);
        }

        public async Task<bool> ComparedTokenCommentId(int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);

            var idFromToken = await _tokenService.ExtractIdFromToken();
            var idFromComment = comment.User_Id;

            return idFromToken == idFromComment;
        }

        public async Task<bool> CreateComment(int postId, int parentCommentId, Comment comment)
        {
            // * If no token string is provided: comment will be anonymous
            string token = _http.HttpContext.Request.Headers.Authorization.ToString();
            int? userId = null;

            if (!string.IsNullOrEmpty(token))
                userId = await _tokenService.ExtractIdFromToken();

            var newComment = new Comment()
            {
                User_Id = userId, // * If UserId field has value
                Parent_Comment_Id = parentCommentId == 0 ? null : parentCommentId,
                Anonymous = userId == null,
                Post_Id = postId,
                Content = comment.Content,
                Created = DateTime.UtcNow,
                // Modified = null // Not necessary on comment creation
            };

            _context.Add(newComment);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteComment(Comment comment)
        {
            var toDelete = await _context.Comments.FindAsync(comment.Comment_Id);

            var comparasion = await ComparedTokenCommentId(comment.Comment_Id);
            var admin = await _userService.CheckAdminStatus();

            if(comparasion || admin)
            {
                _context.Comments.Remove(toDelete);
                return await _context.SaveChangesAsync() > 0;
            }
            else
            {
                return false;
            }
        }

        public async Task<Comment> GetCommentById(int commentId)
        {
            return await _context.Comments.FirstOrDefaultAsync(c => c.Comment_Id == commentId);
        }

        public async Task<List<CommentView>> GetCommentsByPostId(int postId)
        {
            return await _context.Comments.GroupJoin( // * This join can also be done with a view
                _context.Users,
                comment => comment.User_Id,
                user => user.User_Id,
                (comment, users) => new { comment, users })
                .SelectMany(
                x => x.users.DefaultIfEmpty(),
                (comment, user) => new CommentView
                {
                    Comment_Id = comment.comment.Comment_Id,
                    User_Id = comment.comment.User_Id,
                    Post_Id = comment.comment.Post_Id,
                    Parent_Comment_Id = comment.comment.Parent_Comment_Id,
                    Content = comment.comment.Content,
                    Created = comment.comment.Created,
                    Modified = comment.comment.Modified,
                    Anonymous = comment.comment.Anonymous,
                    Author = user != null ? user.Username : "Anonymous",
                    Profile_Picture = user.Profile_Picture
                }
                )
                .Where(c => c.Post_Id == postId)
                .ToListAsync();
        }

        public async Task<List<CommentView>> GetCommentsByUserId(int userId)
        {
            return await _context.Comments.GroupJoin( // * This join can also be done with a view
                _context.Users,
                comment => comment.User_Id,
                user => user.User_Id,
                (comment, users) => new { comment, users })
                .SelectMany(
                x => x.users.DefaultIfEmpty(),
                (comment, user) => new CommentView
                {
                    Comment_Id = comment.comment.Comment_Id,
                    User_Id = comment.comment.User_Id,
                    Post_Id = comment.comment.Post_Id,
                    Parent_Comment_Id = comment.comment.Parent_Comment_Id,
                    Content = comment.comment.Content,
                    Created = comment.comment.Created,
                    Modified = comment.comment.Modified,
                    Anonymous = comment.comment.Anonymous,
                    Author = user != null ? user.Username : "Anonymous",
                    Profile_Picture = user.Profile_Picture
                }
                )
                .Where(c => c.User_Id == userId)
                .ToListAsync();
        }

        public async Task<bool> UpdateComment(int commentId, Comment comment)
        {
            var comparasion = await ComparedTokenCommentId(commentId);

            if(comparasion)
            {
                var existingComment = await _context.Comments.FindAsync(commentId);

                if (existingComment == null)
                    return false;

                existingComment.Content = comment.Content;
                existingComment.Modified = DateTime.UtcNow;

                _context.Update(existingComment);
                return await _context.SaveChangesAsync() > 0;
            }
            else
            {
                return false;
            }
        }
    }
}

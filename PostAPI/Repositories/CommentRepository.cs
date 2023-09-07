using Microsoft.EntityFrameworkCore;
using PostAPI.Interfaces;
using PostAPI.Models;

namespace PostAPI.Repositories
{
    public class CommentRepository : IComment
    {
        private readonly AppDbContext _context;
        private readonly IToken _tokenService;

        public CommentRepository(AppDbContext context, IToken tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public IQueryable<CommentView> CommentJoinQuery()
        {
            var comments = _context.Comments.GroupJoin( // * This join can also be done with a view
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
                    Profile_Picture = user.Profile_Picture,
                })
                .GroupJoin(
                _context.Comments,
                c => c.Comment_Id,
                reply => reply.Parent_Comment_Id,
                (c, replies) => new
                {
                    CommentView = c,
                    Replies = replies.Count()
                })
                .Select(result => new CommentView
                {
                    Comment_Id = result.CommentView.Comment_Id,
                    User_Id = result.CommentView.User_Id,
                    Post_Id = result.CommentView.Post_Id,
                    Parent_Comment_Id = result.CommentView.Parent_Comment_Id,
                    Content = result.CommentView.Content,
                    Created = result.CommentView.Created,
                    Modified = result.CommentView.Modified,
                    Anonymous = result.CommentView.Anonymous,
                    Author = result != null ? result.CommentView.Author : "Anonymous",
                    Profile_Picture = result.CommentView.Profile_Picture,
                    Replies = result.Replies,
                });

            return comments;
        }

        public async Task<bool> CommentIdExists(int commentId)
        {
            return await _context.Comments.AnyAsync(i => i.Comment_Id == commentId);
        }

        public async Task<int> CreateComment(int postId, int parentCommentId, Comment comment)
        {
            var (id, _) = await _tokenService.DecodeHS512Token();

            var newComment = new Comment()
            {
                User_Id = id, // * If UserId field has value
                Parent_Comment_Id = parentCommentId == 0 ? null : parentCommentId,
                Anonymous = id == null,
                Post_Id = postId,
                Content = comment.Content,
                Created = DateTime.UtcNow,
            };

            _context.Add(newComment);
            _ = await _context.SaveChangesAsync() > 0;

            return newComment.Comment_Id;
        }

        public async Task<bool> DeleteComment(Comment comment)
        {
            var commentToDelete = await _context.Comments.FindAsync(comment.Comment_Id);
            if (commentToDelete == null) return false;

            var (id, _) = await _tokenService.DecodeHS512Token();

            if (id == comment.User_Id && await _tokenService.IsUserAuthorized())
            {
                _context.Comments.Remove(commentToDelete);
                return await _context.SaveChangesAsync() > 0;
            }
            else return false;
        }

        public async Task<List<CommentView>> GetChildCommentsById(int commentId, List<CommentView> allComments)
        {
            var comments = await CommentJoinQuery()
                .Where(comment => comment.Parent_Comment_Id == commentId)
                .ToListAsync();

            var childComments = new List<CommentView>();

            foreach(var comment in comments)
            {
                var childCommentView = new CommentView
                {
                    Comment_Id = comment.Comment_Id,
                    User_Id = comment.User_Id,
                    Post_Id = comment.Post_Id,
                    Parent_Comment_Id = comment.Parent_Comment_Id,
                    Content = comment.Content,
                    Created = comment.Created,
                    Modified = comment.Modified,
                    Anonymous = comment.Anonymous,
                    Author = comment != null ? comment.Author : "Anonymous",
                    Profile_Picture = comment.Profile_Picture,
                    Replies = comment.Replies
                };

                var recursive = await GetChildCommentsById(comment.Comment_Id, allComments);
                childCommentView.ChildComments = recursive;

                childComments.Add(childCommentView);
            }

            return childComments;
        }

        public async Task<Comment> GetCommentById(int commentId)
        {
            return await _context.Comments.FirstOrDefaultAsync(c => c.Comment_Id == commentId);
        }

        public async Task<List<Comment>> GetComments(int postId)
        {
            // * Will return a list of all the comments of a post. This is to delete them when we delete a post. We cannot use the one after this one because its a VIEW
            return await _context.Comments.Where(p => p.Post_Id == postId).ToListAsync();
        }

        public async Task<List<CommentView>> GetCommentsByPostId(int postId)
        {
            // * Get the parent comments only
            var parentComments = await CommentJoinQuery().Where(c => c.Post_Id == postId && c.Parent_Comment_Id == null).ToListAsync();

            return parentComments; // * Returning the parent Comments only.
        }

        public async Task<List<CommentView>> GetCommentsByUsername(int page, int pageSize, string username)
        {
            int postsToSkip = (page - 1) * pageSize;
            var comments = await
                _context.Comments
                .GroupJoin(
                    _context.Users,
                    comment => comment.User_Id,
                    user => user.User_Id,
                    (comment, user) => new { comment, user })
                .SelectMany(result => result.user.DefaultIfEmpty(), (comment, user) => new CommentView
                {
                    Comment_Id = comment.comment.Comment_Id,
                    User_Id = comment.comment.User_Id,
                    Post_Id = comment.comment.Post_Id,
                    Parent_Comment_Id = comment.comment.Parent_Comment_Id,
                    Content = comment.comment.Content,
                    Created = comment.comment.Created,
                    Modified = comment.comment.Modified,
                    Anonymous = comment.comment.Anonymous,
                    Author = user == null ? "Anonymous" : user.Username,
                    Profile_Picture = user.Profile_Picture
                })
                .Where(comment => comment.Parent_Comment_Id == null)
                .OrderByDescending(p => p.Created).Where(c => c.Author == username)
                .Skip(postsToSkip)
                .Take(pageSize)
                .ToListAsync();
                

            var additionalComments = new List<CommentView>();

            foreach (var comment in comments)
            {
                var childComments = await RecursiveComments(comment, comments);
                additionalComments.AddRange(childComments);
            }

            return additionalComments;
        }

        public async Task<List<CommentView>> GetCommentViewById(int commentId)
        {
            var comment = await CommentJoinQuery()
                .FirstOrDefaultAsync(i => i.Comment_Id == commentId);

            if (comment == null) return null;

            var allComments = await CommentJoinQuery().ToListAsync();

            var childComments = new List<CommentView>();

            var commentWithReplies = new CommentView
            {
                Comment_Id = comment.Comment_Id,
                User_Id = comment.User_Id,
                Post_Id = comment.Post_Id,
                Parent_Comment_Id = comment.Parent_Comment_Id,
                Content = comment.Content,
                Created = comment.Created,
                Modified = comment.Modified,
                Anonymous = comment.Anonymous,
                Author = comment.Author,
                Profile_Picture = comment.Profile_Picture
            };

            return await RecursiveComments(comment, allComments);
        }


        public async Task<bool> UpdateComment(int commentId, Comment comment)
        {
            var (id, _) = await _tokenService.DecodeHS512Token();

            var commentToUpdate = await _context.Comments.FindAsync(commentId);
            if (commentToUpdate == null) return false;

            if (id == commentToUpdate.User_Id && await _tokenService.IsUserAuthorized())
            {
                var existingComment = await _context.Comments.FindAsync(commentId);

                if (existingComment == null)
                    return false;

                existingComment.Content = comment.Content;
                existingComment.Modified = DateTime.UtcNow;

                _context.Update(existingComment);
                return await _context.SaveChangesAsync() > 0;
            }
            else return false;
        }

        public async Task<List<CommentView>> RecursiveComments(CommentView parentComment, List<CommentView> comments)
        {
            var replies = await GetChildCommentsById(parentComment.Comment_Id, comments);

            var childComments = comments
                .Where(x => x.Comment_Id == parentComment.Comment_Id)
                .GroupJoin(
                _context.Comments,
                c => c.Comment_Id,
                reply => reply.Parent_Comment_Id,
                (x, r) => new CommentView
                {
                    Comment_Id = x.Comment_Id,
                    User_Id = x.User_Id,
                    Post_Id = x.Post_Id,
                    Parent_Comment_Id = x.Parent_Comment_Id,
                    Content = x.Content,
                    Created = x.Created,
                    Modified = x.Modified,
                    Anonymous = x.Anonymous,
                    Author = x.Author,
                    Profile_Picture = x.Profile_Picture,
                    Replies = r.Count()
                }
                )
                .Select(x => new CommentView
                {
                    Comment_Id = x.Comment_Id,
                    User_Id = x.User_Id,
                    Post_Id = x.Post_Id,
                    Parent_Comment_Id = x.Parent_Comment_Id,
                    Content = x.Content,
                    Created = x.Created,
                    Modified = x.Modified,
                    Anonymous = x.Anonymous,
                    Author = x.Author,
                    Profile_Picture = x.Profile_Picture,
                    Replies = x.Replies,
                    ChildComments = replies
                })
                .ToList();

            return childComments;
        }
    }
}

using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface IComment
    {
        Task<bool> CommentIdExists(int commentId);
        Task<Comment> GetCommentById(int commentId);
        Task<List<CommentView>> GetCommentsByPostId(int postId); // * This isnt actually the view
        Task<List<CommentView>> GetCommentsByUsername(string username);
        Task<bool> ComparedTokenCommentId(int commentId);
        Task<bool> CreateComment(int postId, int parentCommentId, Comment comment);
        Task<bool> UpdateComment(int commentId, Comment comment);
        Task<bool> DeleteComment(Comment comment);
    }
}

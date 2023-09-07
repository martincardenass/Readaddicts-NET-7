using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface IComment
    {
        IQueryable<CommentView> CommentJoinQuery();
        Task<bool> CommentIdExists(int commentId);
        Task<Comment> GetCommentById(int commentId);
        Task<List<CommentView>> GetCommentViewById(int commentId);
        Task<List<CommentView>> GetChildCommentsById(int commentId, List<CommentView> allComments);
        Task<List<CommentView>> GetCommentsByPostId(int postId); // * This isnt actually the view
        Task<List<Comment>> GetComments(int postId); // * This is to delete the comments when deleting the post
        Task<List<CommentView>> GetCommentsByUsername(int page, int pageSize, string username);
        Task<int> CreateComment(int postId, int parentCommentId, Comment comment);
        Task<bool> UpdateComment(int commentId, Comment comment);
        Task<bool> DeleteComment(Comment comment);
    }
}

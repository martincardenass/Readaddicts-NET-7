using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface IPost
    {
        Task DeleteRecursiveComments(Comment comment);
        Task AddImagesToPost(List<IFormFile> files, int posttId);
        IQueryable<PostView> PostJoinQuery();
        Task<List<PostView>> GetPosts(int page, int pageSize);
        Task<bool> IdExists(int id);
        Task<PostView> GetPostViewById(int postId);
        Task<List<PostView>> GetUserPostsByUsername(int page, int pageSize, string username);
        Task<Post> GetPostById(int id);
        Task<int> CreatePost(List<IFormFile> files, Post post, int? groupId);
        Task<bool> CheckIfUserIsAGroupMember(int groupId);
        Task<bool> DeletePost(Post post);
        Task<bool> UpdatePost(List<IFormFile> files, int postId, Post post);
        Task<bool> AddImageToPost(IFormFile file, int postId);
    }
}

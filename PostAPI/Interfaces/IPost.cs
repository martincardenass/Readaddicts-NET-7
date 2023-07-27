using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface IPost
    {
        Task<List<PostView>> GetPosts(int page, int pageSize);
        Task<bool> IdExists(int id);
        Task<Post> GetPostById(int id);
        Task<bool> CompareTokenPostId(int  postId);
        Task<bool> CreatePost(Post post);
        Task<bool> DeletePost(Post post);
        Task<bool> UpdatePost(int id, Post post);
    }
}

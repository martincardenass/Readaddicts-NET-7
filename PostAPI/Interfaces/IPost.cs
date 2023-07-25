using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface IPost
    {
        Task<List<PostView>> GetPosts();
        Task<bool> IdExists(int id);
        Task<Post> GetById(int id);
        Task<bool> CompareTokenPostId(int  postId);
        Task<bool> CreatePost(Post post);
        Task<bool> DeletePost(Post post);
        Task<bool> UpdatePost(int id, Post post);
    }
}

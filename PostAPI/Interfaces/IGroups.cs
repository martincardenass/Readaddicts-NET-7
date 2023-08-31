using PostAPI.Dto;
using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface IGroups
    {
        IQueryable<GroupView> GroupJoinQuery();
        Task<List<GroupView>> GetGroups();
        Task<Group> CreateGroup(Group group, IFormFile file);
        Task<bool> UpdateGroup(int groupId, GroupUpdateDto group, IFormFile file);
        Task<bool> DeleteGroup(Group group);
        Task<bool> CompareUserTokenWithGroupId(int groupId);
        Task<bool> JoinGroup(int groupId);
        Task<bool> LeaveGroup(int groupId);
        Task<GroupView> GetGroupById(int groupId);
        Task<Group> GetGroupByIdNotView(int groupId);
        Task<bool> GroupExists(string groupName);
    }
}

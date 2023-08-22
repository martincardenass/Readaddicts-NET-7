using PostAPI.Dto;
using PostAPI.Models;

namespace PostAPI.Interfaces
{
    public interface IGroups
    {
        IQueryable<GroupView> GroupJoinQuery();
        Task<List<GroupView>> GetGroups();
        Task<bool> CreateGroup(Group group);
        Task<bool> UpdateGroup(int groupId, GroupUpdateDto group);
        Task<bool> DeleteGroup(Group group);
        Task<bool> CompareUserTokenWithGroupId(int groupId);
        Task<bool> JoinGroup(int groupId);
        Task<GroupView> GetGroupById(int groupId);
        Task<Group> GetGroupByIdNotView(int groupId);
    }
}

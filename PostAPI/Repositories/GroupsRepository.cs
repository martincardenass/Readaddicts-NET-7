﻿using Microsoft.EntityFrameworkCore;
using PostAPI.Dto;
using PostAPI.Interfaces;
using PostAPI.Models;

namespace PostAPI.Repositories
{
    public class GroupsRepository : IGroups
    {
        private readonly AppDbContext _context;
        private readonly IToken _tokenService;
        private readonly IImage _imageService;

        public GroupsRepository(AppDbContext context, IToken tokenService, IImage imageService)
        {
            _context = context;
            _tokenService = tokenService;
            _imageService = imageService;
        }

        public async Task<bool> CompareUserTokenWithGroupId(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);

            var idFromToken = await _tokenService.ExtractIdFromToken(); // * Get ID from authorization headers
            var idFromGroup = group.Group_Owner; // * Group Owner ID

            return idFromToken == idFromGroup;
        }

        public async Task<bool> CreateGroup(Group group)
        {
            int userId = await _tokenService.ExtractIdFromToken();

            var newGroup = new Group()
            {
                Group_Name = group.Group_Name,
                Group_Description = group.Group_Description,
                Group_Owner = userId // * User ID from token is the group owner
            };

            _context.Add(newGroup); // * Save the new group
            _ = await _context.SaveChangesAsync() > 0;

            var relation = new GroupsRelations() // * Add the logged in user as a member of the group
            {
                User_Id = userId,
                Group_Id = newGroup.Group_Id
            };

            _context.Add(relation);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteGroup(Group group)
        {
            if(await CompareUserTokenWithGroupId(group.Group_Id))
            {
                var groupToDelete = await _context.Groups.FindAsync(group.Group_Id);

                // * Get all the relations so we can delete them as well
                var relations = await _context.GroupsRelations.Where(g => g.Group_Id == group.Group_Id).ToListAsync();

                if(relations.Count > 0)
                {
                    foreach(var relation in relations)
                    {
                        // * Iterate and delete every relation
                        _context.GroupsRelations.Remove(relation);
                        _ = await _context.SaveChangesAsync() > 0;
                    }
                }

                _context.Groups.Remove(group);

                return await _context.SaveChangesAsync() > 0;
            }
            else
            {
                return false;
            }
        }

        public async Task<GroupView> GetGroupById(int groupId)
        {
            return await GroupJoinQuery().FirstOrDefaultAsync(g => g.Group_Id == groupId);
        }

        public async Task<Group> GetGroupByIdNotView(int groupId)
        {
            return await _context.Groups.FirstOrDefaultAsync(g => g.Group_Id == groupId);
        }

        public async Task<List<GroupView>> GetGroups()
        {
            return await GroupJoinQuery().OrderBy(g => g.Group_Id).ToListAsync();
        }

        public IQueryable<GroupView> GroupJoinQuery()
        {
            // * Joins the relations table with the users table and creates a Members property with all the members
            var groups =
                _context.Groups
                .GroupJoin(
                    _context.GroupsRelations,
                    group => group.Group_Id,
                    relation => relation.Group_Id,
                    (group, relation) => new { group, relation })
                .SelectMany(
                    grouped => grouped.relation.DefaultIfEmpty(),
                    (grouped, relation) => new { grouped.group, Relations = relation })
                .GroupJoin(
                    _context.Users,
                    grouped => grouped.Relations.User_Id,
                    user => user.User_Id,
                    (grouped, users) => new
                    {
                        Group = grouped.group,
                        Members = users.ToList(),
                        Owner = users.Where(u => u.User_Id == grouped.Relations.User_Id).FirstOrDefault()
                    })
                .GroupBy(grouped => new // * Group the results by a composite key
                {
                    grouped.Group.Group_Id,
                    grouped.Group.Group_Name,
                    grouped.Group.Group_Description
                })
                .Select(grouped => new GroupView
                {
                    Group_Id = grouped.Key.Group_Id,
                    Group_Name = grouped.Key.Group_Name,
                    Group_Description = grouped.Key.Group_Description,
                    Owner = grouped.Select(g => g.Owner).FirstOrDefault(),
                    Members = grouped.SelectMany(g => g.Members).ToList()
                });

            return groups;
        }

        public async Task<bool> JoinGroup(int groupId)
        {
            int userId = await _tokenService.ExtractIdFromToken();

            var relation = new GroupsRelations() // * Add the user from the token as a new member for the group
            {
                User_Id = userId,
                Group_Id = groupId
            };

            _context.Add(relation);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateGroup(int groupId, GroupUpdateDto group)
        {
            if (await CompareUserTokenWithGroupId(groupId))
            {
                var groupToUpdate = await _context.Groups.FindAsync(groupId);

                if (groupToUpdate != null)
                {
                    groupToUpdate.Group_Name = group.Group_Name ?? groupToUpdate.Group_Name;
                    groupToUpdate.Group_Description = group.Group_Description ?? groupToUpdate.Group_Description;

                    _context.Update(groupToUpdate);
                    return await _context.SaveChangesAsync() > 0;
                }
            }
            return false;
        }
    }
}
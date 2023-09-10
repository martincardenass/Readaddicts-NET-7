using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostAPI.Dto;
using PostAPI.Interfaces;
using PostAPI.Models;

namespace PostAPI.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupsController : ControllerBase
    {
        private readonly IGroups _groupService;

        public GroupsController(IGroups groupService)
        {
            _groupService = groupService;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<GroupView>))]
        public async Task<IActionResult> GetGroups()
        {
            var groups = await _groupService.GetGroups();

            return Ok(groups);
        }

        [HttpGet("{groupId}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<GroupView>))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetGroupById(int groupId)
        {
            var group = await _groupService.GetGroupById(groupId);

            if (group == null) return NotFound();

            return Ok(group);
        }

        [HttpGet("Posts/{groupId}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<PostView>))]
        public async Task<IActionResult> GetPostsByGroupId(int groupId)
        {
            var posts = await _groupService.GetPostsByGroupId(groupId);

            return Ok(posts);
        }

        [HttpPost("Validator/GroupExists/{groupName}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ValidateIfGroupExists(string groupName)
        {
            bool exists = await _groupService.GroupExists(groupName);

            if (!exists) return Ok(new { NotOk = "Group does not exist" });

            return Ok(new { OK = "Group exists" });
        }


        [HttpPost("create")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateGroup([FromForm] Group group, [FromForm] IFormFile? imageFile)
        {
            var validator = new GroupValidator();
            var validationResult = await validator.ValidateAsync(group);

            if(!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(error => new ValidationError
                {
                    Field = error.PropertyName,
                    Error = error.ErrorMessage
                }).ToList();

                return BadRequest(errors);
            }
            var newlyCreatedGroup = await _groupService.CreateGroup(group, imageFile);

            if (newlyCreatedGroup == null)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(newlyCreatedGroup);
        }

        [HttpPost("Join/{groupId}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> JoinGroup(int groupId)
        {
            bool joined = await _groupService.JoinGroup(groupId);

            if (!joined) return Conflict(new { NotOk = "User is already a member" });

            return Ok(new { Ok = "Joined Group" });
        }

        [HttpDelete("Leave/{groupId}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            bool left = await _groupService.LeaveGroup(groupId);

            if (!left) return Conflict(new { NotOk = "User already left" });

            return Ok(new { Ok = "User left the group " });
        }

        [HttpPatch("Update/{groupId}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateGroup(int groupId, [FromForm] GroupUpdateDto group, [FromForm] IFormFile? imageFile)
        {
            var validator = new GroupUpdateDtoValidator();
            var validatorResult = await validator.ValidateAsync(group);

            if (!validatorResult.IsValid)
            {
                var errors = validatorResult.Errors.Select(error => new ValidationError
                {
                    Field = error.PropertyName,
                    Error = error.ErrorMessage
                }).ToList();

                return BadRequest(errors);
            }

            var findGroup = await _groupService.GetGroupById(groupId);

            if (findGroup == null) return NotFound();

            var updated = await _groupService.UpdateGroup(groupId, group, imageFile);

            if (!updated)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(new { Ok = "Group updated" });
        }

        [HttpDelete("Delete/{groupId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            var findGroup = await _groupService.GetGroupByIdNotView(groupId);

            if (findGroup == null) return NotFound();

            bool deleted = await _groupService.DeleteGroup(findGroup);

            if(!deleted)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            return Ok(new { Ok = "Group deleted success" });
        }
    }
}

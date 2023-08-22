using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostAPI.Dto;
using PostAPI.Interfaces;
using PostAPI.Models;
using System.Collections;

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
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetGroups()
        {
            var groups = await _groupService.GetGroups();

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(groups);
        }

        [HttpGet("{groupId}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<GroupView>))]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetGroupById(int groupId)
        {
            var group = await _groupService.GetGroupById(groupId);

            if (group == null) return NotFound();

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(group);
        }


        [HttpPost("create")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateGroup(Group group)
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
            bool created = await _groupService.CreateGroup(group);

            if (!created)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(group);
        }

        [HttpPost("Join/{groupId}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> JoinGroup(int groupId)
        {
            bool joined = await _groupService.JoinGroup(groupId);

            if (!joined)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }

        [HttpPatch("Update/{groupId}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateGroup(int groupId, GroupUpdateDto group)
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

            var updated = await _groupService.UpdateGroup(groupId, group);

            if (!updated)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest();

            return NoContent();
        }

        [HttpDelete("Delete/{groupId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
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

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return NoContent();
        }
    }
}

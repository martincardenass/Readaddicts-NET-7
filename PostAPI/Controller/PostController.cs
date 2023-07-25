using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostAPI.Interfaces;
using PostAPI.Models;

namespace PostAPI.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly IPost _postService;

        public PostController(IPost postService)
        {
            _postService = postService;
        }

        [HttpGet("allposts")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<PostView>))]
        public async Task<IEnumerable<PostView>> GetPosts() => await _postService.GetPosts();

        [HttpPost]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreatePost(Post post)
        {
            var validator = new PostValidator();
            var validationResult = await validator.ValidateAsync(post);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(error => new ValidationError
                {
                    Field = error.PropertyName,
                    Error = error.ErrorMessage
                }).ToList();

                return BadRequest(errors);
            };

            bool posted = await _postService.CreatePost(post);

            if(!posted)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok("Post created");
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(400)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePostAsync(int id)
        {
            bool exists = await _postService.IdExists(id);

            if(!exists)
                return NotFound("The post does not exist or has already been deleted");

            var post = await _postService.GetById(id);
            bool deleted = await _postService.DeletePost(post);

            if(!deleted)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok("Post deleted");
        }

        [HttpPatch("update/{id}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(400)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdatePostAsync(int id, [FromBody] Post post)
        {
            bool exists = await _postService.IdExists(id);

            if (!exists)
                return NotFound("The post does not exist");

            var validator = new PostValidator();
            var validatorResult = validator.Validate(post);

            if (!validatorResult.IsValid)
            {
                var errors = validatorResult.Errors.Select(error => new ValidationError
                {
                    Field = error.PropertyName,
                    Error = error.ErrorMessage
                }).ToList();

                return BadRequest(errors);
            }
            
            var updated = await _postService.UpdatePost(id, post);

            if (!updated)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok("Post updated");
        }
    }
}

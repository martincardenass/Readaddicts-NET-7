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
        private readonly IImage _imageService;

        public PostController(IPost postService, IImage imageService)
        {
            _postService = postService;
            _imageService = imageService;
        }

        [HttpGet("allposts")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<PostView>))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetPosts(int page, int pageSize)
        {
            var posts = await _postService.GetPosts(page, pageSize);

            // * Not returning any response when we reach a page that has no posts. Just return an empty array[] (200 response tho).
            // * It was causing issues with the appending in the frontend. There might be still a solution but this works just fine (by now!)

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(posts);
        }


        [HttpGet("{id}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<PostView>))]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetPostViewById(int id)
        {
            var post = await _postService.GetPostViewById(id);

            bool exists = await _postService.IdExists(id);
            if(!exists) return NotFound($"The post with ID {id} does not exist");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(post);
        }

        [HttpGet("{id}/images")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ImageView>))]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetImagesByPostId(int id)
        {
            var images = await _imageService.GetImagesByPostId(id);

            bool hasImages = await _imageService.PostHasImages(id);

            if (!hasImages) return NotFound("This post has no images");

            return Ok(images);
        }

        [HttpPost]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreatePost([FromForm] List<IFormFile?> files, [FromForm] Post post)
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

            int newPostId = await _postService.CreatePost(files, post);

            if(newPostId == 0)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok($"{newPostId}");
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> DeletePostAsync(int id)
        {
            bool exists = await _postService.IdExists(id);

            if(!exists)
                return NotFound("The post does not exist or has already been deleted");

            var post = await _postService.GetPostById(id);
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
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdatePostAsync([FromForm] List<IFormFile?> files, int id, [FromForm] Post post)
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
            
            var updated = await _postService.UpdatePost(files, id, post);

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

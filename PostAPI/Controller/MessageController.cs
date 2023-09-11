using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostAPI.Dto;
using PostAPI.Interfaces;
using PostAPI.Models;

namespace PostAPI.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IMessage _messageService;

        public MessageController(IMessage messageService)
        {
            _messageService = messageService;
        }

        [HttpGet("messages/{username}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<MessageView>))]
        public async Task<IActionResult> GetUserMessages(string username)
        {
            var messages = await _messageService.GetUserMessages(username);

            return Ok(messages);
        }

        [HttpGet("message/{messageId}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200, Type = typeof(MessageView))]
        public async Task<IActionResult> GetMessageById(int messageId)
        {
            var message = await _messageService.GetMessageById(messageId);

            return Ok(message);
        }

        [HttpGet("messages/conversation")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<MessageView>))]
        public async Task<IActionResult> GetConversation(int page, int pageSize, string receiver, string sender)
        {
            var messages = await _messageService.GetConversation(page, pageSize, receiver, sender);

            return Ok(messages);
        }

        [HttpGet("messages/users")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<UserLimitedDto>))]
        public async Task<IActionResult> GetUsersThatHaveMessagedMe()
        {
            var users = await _messageService.GetUsersThatHaveMessagedMe();
           
            return Ok(users);
        }

        [HttpPost("message/send/{receiver}")]
        [Authorize(Policy = "UserAllowed")]
        [ProducesResponseType(500)]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        public async Task<IActionResult> SendMessage(string receiver, [FromForm] Message message)
        {
            var newMessage = await _messageService.SendMessage(receiver, message);

            if(newMessage == null)
            {
                ModelState.AddModelError("", "Something went wrong");
                return StatusCode(500, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(newMessage);
        }
    }
}

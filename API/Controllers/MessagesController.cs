using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : BaseApiController
    {
        private readonly IUserRepository userRepository;
        private readonly IMessageRepository messageRepository;
        private readonly IMapper mapper;

        public MessagesController(IUserRepository userRepository, IMessageRepository messageRepository, IMapper mapper)
        {
            this.messageRepository = messageRepository;
            this.mapper = mapper;
            this.userRepository = userRepository;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDTO>> CreateMessage(CreateMessageDTO createMessageDTO) {
            var username = User.GetUsername();
            if (username == createMessageDTO.RecipientUsername.ToLower()) {
                return BadRequest("You cannot send messages to yourself");
            }
            var sender = await userRepository.GetUserByUsernameAsync(username);
            var recipient = await userRepository.GetUserByUsernameAsync(createMessageDTO.RecipientUsername);
            if (recipient == null) {
                return NotFound();
            }
            var message = new Message {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDTO.Content
            };
            messageRepository.AddMessage(message);
            if (await messageRepository.SaveAllAsync()) {
                return Ok(mapper.Map<MessageDTO>(message));
            }
            return BadRequest("Failed to send message");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessagesForUser([FromQuery] MessageParams messageParams) {
            messageParams.Username = User.GetUsername();
            var messages = await messageRepository.GetMessagesForUser(messageParams);
            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);
            return messages;
        }

        [HttpGet("thread/{username}")]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessageThread(string username) {
            var currentUsername = User.GetUsername();
            return Ok(await messageRepository.GetMessageThread(currentUsername, username));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id) {
            var currentUsername = User.GetUsername();
            var message = await messageRepository.GetMessage(id);
            if (message.Sender.UserName != currentUsername && message.Recipient.UserName != currentUsername) {
                return Unauthorized();
            }
            if (message.Sender.UserName == currentUsername) {
                message.SenderDeleted = true;
            }
            if (message.Recipient.UserName == currentUsername) {
                message.RecipientDeleted = true;
            }
            if (message.SenderDeleted && message.RecipientDeleted) {
                messageRepository.DeleteMessage(message);
            }
            if (await messageRepository.SaveAllAsync())  {
                return Ok();
            }
            return BadRequest("Problem deleting the message");
        }
    }
}
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
        private readonly IMapper mapper;
        private readonly IUnitOfWork unitOfWork;

        public MessagesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDTO>> CreateMessage(CreateMessageDTO createMessageDTO) {
            var username = User.GetUsername();
            if (username == createMessageDTO.RecipientUsername.ToLower()) {
                return BadRequest("You cannot send messages to yourself");
            }
            var sender = await unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            var recipient = await unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDTO.RecipientUsername);
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
            unitOfWork.MessageRepository.AddMessage(message);
            if (await unitOfWork.Complete()) {
                return Ok(mapper.Map<MessageDTO>(message));
            }
            return BadRequest("Failed to send message");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessagesForUser([FromQuery] MessageParams messageParams) {
            messageParams.Username = User.GetUsername();
            var messages = await unitOfWork.MessageRepository.GetMessagesForUser(messageParams);
            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);
            return messages;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id) {
            var currentUsername = User.GetUsername();
            var message = await unitOfWork.MessageRepository.GetMessage(id);
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
                unitOfWork.MessageRepository.DeleteMessage(message);
            }
            if (await unitOfWork.Complete())  {
                return Ok();
            }
            return BadRequest("Problem deleting the message");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IMessageRepository messageRepository;
        private readonly IMapper mapper;
        private readonly IUserRepository userRepository;
        private readonly IHubContext<PresenceHub> presenceHub;
        private readonly PresenceTracker tracker;

        public MessageHub(IMessageRepository messageRepository, IMapper mapper, 
            IUserRepository userRepository, IHubContext<PresenceHub> presenceHub, PresenceTracker tracker)
        {
            this.messageRepository = messageRepository;
            this.mapper = mapper;
            this.userRepository = userRepository;
            this.presenceHub = presenceHub;
            this.tracker = tracker;
        }

        public override async Task OnConnectedAsync() {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await AddToGroup(groupName);
            var group = await AddToGroup(groupName);
            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);
            var messages = await messageRepository.GetMessageThread(Context.User.GetUsername(), otherUser);
            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
        }

        public async Task SendMessage(CreateMessageDTO createMessageDTO) {
            var username = Context.User.GetUsername();
            if (username == createMessageDTO.RecipientUsername.ToLower()) {
                throw new HubException("You cannot send messages to yourself");
            }
            var sender = await userRepository.GetUserByUsernameAsync(username);
            var recipient = await userRepository.GetUserByUsernameAsync(createMessageDTO.RecipientUsername);
            if (recipient == null) {
                throw new HubException("Not found user");
            }
            var message = new Message {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDTO.Content
            };
            var groupName = GetGroupName(sender.UserName, recipient.UserName);
            var group = await messageRepository.GetMessageGroup(groupName);
            if (group.Connections.Any(x => x.Username == recipient.UserName)) {
                message.DateRead = DateTime.UtcNow;
            } else {
                var connections = await tracker.GetConnectionsForUser(recipient.UserName);
                if (connections != null) {
                    await presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", 
                        new { username = sender.UserName, knownAs = sender.KnownAs });
                }
            }
            messageRepository.AddMessage(message);
            if (await messageRepository.SaveAllAsync()) {
                await Clients.Group(groupName).SendAsync("NewMessage", mapper.Map<MessageDTO>(message));
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception) {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
            await base.OnDisconnectedAsync(exception);
        }

        private async Task<Group> AddToGroup(string groupName) {
            var group = await messageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());
            if (group == null) {
                group = new Group(groupName);
                messageRepository.AddGroup(group);
            }
            group.Connections.Add(connection);
            if (await messageRepository.SaveAllAsync()) {
                return group;
            } else {
                throw new HubException("Failed to join group");
            }
        }

        private async Task<Group> RemoveFromMessageGroup() {
            var group = await messageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            messageRepository.RemoveConnection(connection);
            if (await messageRepository.SaveAllAsync()) {
                return group;
            } else {
                throw new HubException("Failed to remove from group");
            }
        }

        private string GetGroupName(string caller, string other) {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}
using System.Collections.Concurrent;
using API.Data;
using API.Dtos;
using API.Extensions;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Hubs;

[Authorize]
public class ChatHub(UserManager<AppUser> userManager, ChatDbContext chatDbContext) : Hub
{
    public ConcurrentDictionary<string, OnlineUserDto> OnlineUsers = new();

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var receiverId = httpContext?.Request.Query["senderId"].ToString();
        var userName = Context.User!.Identity!.Name!;
        var currentUser = await userManager.FindByNameAsync(userName);
        var connectionId = Context.ConnectionId;

        if (OnlineUsers.ContainsKey(userName))
        {
            OnlineUsers[userName].ConnectionId = connectionId;
        }
        else
        {
            var user = new OnlineUserDto
            {
                ConnectionId = connectionId,
                UserName = userName,
                ProfileImage = currentUser!.ProfileImage,
                FullName = currentUser!.FullName,
            };
            OnlineUsers.TryAdd(userName, user);
            await Clients.AllExcept(connectionId).SendAsync("Notify", currentUser);
        }

        if (!string.IsNullOrEmpty(receiverId))
        {
            await LoadMessages(receiverId);
        }

        await Clients.All.SendAsync("OnlineUsers", await GetAllUsers());
    }

    private async Task<IEnumerable<OnlineUserDto>> GetAllUsers()
    {
        var userName = Context.User!.GetUserName();

        var onlineUsersSet = new HashSet<string>(OnlineUsers.Keys);

        var users = await userManager.Users.Select(u => new OnlineUserDto
        {
            FullName = u.FullName,
            Id = u.Id,
            ProfileImage = u.ProfileImage,
            UserName = u.UserName,
            IsOnline = onlineUsersSet.Contains(u.UserName),
            UnreadCount = chatDbContext.Messages.Count(m => m.ReceiverId == userName && m.SenderId == u.Id && !m.IsRead)
        }).OrderByDescending(u => u.IsOnline).ToListAsync();

        return users;
    }

    public async Task SendMessage(MessageRequestDto message)
    {
        var senderId = Context.User!.Identity!.Name;
        var recipientId = message.ReceiverId;
        var newMessage = new Message
        {
            Content = message.Content,
            IsRead = message.IsRead,
            CreatedAt = message.CreatedDate,
            Sender = await userManager.FindByNameAsync(senderId!),
            Receiver = await userManager.FindByNameAsync(recipientId!),
        };
        await chatDbContext.Messages.AddAsync(newMessage);
        await chatDbContext.SaveChangesAsync();

        await Clients.User(recipientId).SendAsync("ReceiveNewMessage", newMessage);
    }

    public async Task NotifyTyping(string recipientUserName)
    {
        var senderUserName = Context.User!.Identity!.Name;
        if(string.IsNullOrEmpty(senderUserName)) 
            return;

        var connectionId = OnlineUsers.Values.FirstOrDefault(u => u.UserName == recipientUserName)?.ConnectionId;
        if(!string.IsNullOrEmpty(connectionId)) 
            await Clients.Client(connectionId).SendAsync("NotifyTypingToUser", senderUserName);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userName = Context.User!.Identity!.Name;
        OnlineUsers.TryRemove(userName!, out _);
        await Clients.All.SendAsync("OnlineUsers", OnlineUsers);
    }

    public async Task LoadMessages(string recipientId, int pageNumber = 1)
    {
        int pageSize = 10;

        var username = Context.User!.Identity!.Name;
        var currentUser = await userManager.FindByNameAsync(username!);
        if(currentUser is null) return;

        List<MessageResponseDto> messages = await chatDbContext.Messages
        .Where(m => m.ReceiverId == currentUser!.Id && m.SenderId == recipientId ||
                m.SenderId == currentUser!.Id && m.ReceiverId == recipientId)
        .OrderByDescending(m => m.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .OrderBy(m => m.CreatedAt)
        .Select(m => new MessageResponseDto
        {
            Id = m.Id,
            Content = m.Content,
            CreatedDate = m.CreatedAt,
            ReceiverId = m.ReceiverId,
            SenderId = m.SenderId
        }).ToListAsync();

        foreach(var msg in messages)
        {
            var message = await chatDbContext.Messages.FirstOrDefaultAsync(m => m.Id == msg.Id);
            if(message is not null && message.ReceiverId == currentUser.Id)
            {
                message.IsRead = true;
            }
        }
        await chatDbContext.SaveChangesAsync();

        await Clients.User(currentUser.Id).SendAsync("ReceiveMessageList", messages);
    }
}

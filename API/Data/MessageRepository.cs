﻿using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository(DataContext context, IMapper mapper) : IMessageRepository
    {
        public void AddGroup(Group group)
        {
            context.Groups.Add(group);
        }

        public void AddMessage(Message message)
        {
            context.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            context.Messages.Remove(message);
        }

        public async Task<Connection?> GetConnection(string connectionId)
        {
            return await context.Connections.FindAsync(connectionId);
        }

        public async Task<Group?> GetGroupForConnection(string connectionId)
        {
            return await context
                .Groups.Include(x => x.Connections)
                .Where(x => x.Connections.Any(c => c.ConnectionId == connectionId))
                .FirstOrDefaultAsync();
        }

        public async Task<Message?> GetMessage(int id)
        {
            return await context.Messages.FindAsync(id);
        }

        public async Task<Group?> GetMessageGroup(string groupName)
        {
            return await context
                .Groups.Include(x => x.Connections)
                .FirstOrDefaultAsync(x => x.Name == groupName);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = context.Messages.OrderBy(m => m.MessageSent).AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(u =>
                    u.RecipientUsername == messageParams.Username && u.RecipientDeleted == false
                ),
                "Outbox" => query.Where(u =>
                    u.SenderUsername == messageParams.Username && u.SenderDeleted == false
                ),
                _ => query.Where(u =>
                    u.RecipientUsername == messageParams.Username
                    && u.DateRead == null
                    && u.RecipientDeleted == false
                ),
            };

            var messages = query.ProjectTo<MessageDto>(mapper.ConfigurationProvider);

            return await PagedList<MessageDto>.CreateAsync(
                messages,
                messageParams.PageNumber,
                messageParams.PageSize
            );
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(
            string currentUsername,
            string recipientUsername
        )
        {
            var query = context
                .Messages.Include(u => u.Sender)
                .ThenInclude(p => p.Photos)
                .Include(u => u.Recipient)
                .ThenInclude(p => p.Photos)
                .Where(m =>
                    m.RecipientUsername == currentUsername
                        && m.RecipientDeleted == false
                        && m.SenderUsername == recipientUsername
                    || m.SenderUsername == currentUsername
                        && m.SenderDeleted == false
                        && m.RecipientUsername == recipientUsername
                )
                .OrderBy(m => m.MessageSent)
                .AsQueryable();

            var unreadMessages = query
                .Where(m => m.DateRead == null && m.RecipientUsername == currentUsername)
                .ToList();

            if (unreadMessages.Count != 0)
            {
                unreadMessages.ForEach(m => m.DateRead = DateTime.UtcNow);
            }

            return await query.ProjectTo<MessageDto>(mapper.ConfigurationProvider).ToListAsync();
        }

        public void RemoveConnection(Connection connection)
        {
            context.Connections.Remove(connection);
        }
    }
}

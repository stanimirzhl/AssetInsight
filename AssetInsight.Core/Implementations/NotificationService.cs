using AssetInsight.Core.Interfaces;
using AssetInsight.Data;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using AssetInsight.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Implementations
{
	public class NotificationService : INotificationService
	{
		private readonly IRepository<Notification> repository;
		private readonly IHubContext<NotificationHub> hubContext;

		public NotificationService(IRepository<Notification> repository, IHubContext<NotificationHub> hubContext)
		{
			this.repository = repository;
			this.hubContext = hubContext;
		}

		public async Task<List<Notification>> GetLatestAsync(string userId)
		{
			return await repository
				.All()
				.Where(n => n.ReceiverId == userId)
				.OrderByDescending(n => n.CreatedAt)
				.Take(10)
				.ToListAsync();
		}

		public async Task MarkAsReadAsync(int id, string userId)
		{
			var note = await repository.All().FirstOrDefaultAsync(n => n.Id == id && n.ReceiverId == userId);
			if (note != null)
			{
				note.IsRead = true;
				await repository.SaveChangesAsync();
			}
		}

		public async Task MarkAllAsReadAsync(string userId)
		{
			var unread = await repository.All()
				.Where(n => n.ReceiverId == userId && !n.IsRead)
				.ToListAsync();

			unread.ForEach(n => n.IsRead = true);
			await repository.SaveChangesAsync();
		}

		public async Task<int> GetUnreadCountAsync(string userId)
		{
			return await repository
				.All()
				.CountAsync(n => n.ReceiverId == userId && !n.IsRead);
		}

		public async Task CreateNotification(string userId, string message, string url)
		{
			var notification = new Notification
			{
				ReceiverId = userId,
				Message = message,
				TargetUrl = url
			};

			await repository.AddAsync(notification);

			await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", message, url);
		}
	}
}

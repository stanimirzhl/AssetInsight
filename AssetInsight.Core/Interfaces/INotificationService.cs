using AssetInsight.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface INotificationService
	{
		Task CreateNotification(string userId, string message, string url);
		Task<List<Notification>> GetLatestAsync(string userId);
		Task<int> GetUnreadCountAsync(string userId);
		Task MarkAsReadAsync(int id, string userId);
		Task MarkAllAsReadAsync(string userId);
	}
}

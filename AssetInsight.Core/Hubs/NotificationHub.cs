using AssetInsight.Core.Trackers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AssetInsight.Hubs
{
	[Authorize]
	public class NotificationHub : Hub
	{
		private readonly PresenceTracker _tracker;

		public NotificationHub(PresenceTracker tracker)
		{
			_tracker = tracker;
		}

		public override async Task OnConnectedAsync()
		{
			var userId = Context.UserIdentifier;
			if (userId != null)
			{
				await _tracker.UserConnected(userId, Context.ConnectionId);

				var count = await _tracker.GetOnlineUsersCount();
				await Clients.All.SendAsync("UpdateOnlineCount", count);
			}

			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			var userId = Context.UserIdentifier;
			if (userId != null)
			{
				await _tracker.UserDisconnected(userId, Context.ConnectionId);

				var count = await _tracker.GetOnlineUsersCount();
				await Clients.All.SendAsync("UpdateOnlineCount", count);
			}

			await base.OnDisconnectedAsync(exception);
		}
	}
}

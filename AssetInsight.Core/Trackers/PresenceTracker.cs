using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Trackers
{
	public class PresenceTracker
	{
		private readonly ConcurrentDictionary<string, List<string>> _onlineUsers = new();

		public Task<bool> UserConnected(string userId, string connectionId)
		{
			bool isNewUser = false;
			lock (_onlineUsers)
			{
				if (!_onlineUsers.ContainsKey(userId))
				{
					_onlineUsers[userId] = new List<string>();
					isNewUser = true;
				}
				_onlineUsers[userId].Add(connectionId);
			}
			return Task.FromResult(isNewUser);
		}

		public Task<bool> UserDisconnected(string userId, string connectionId)
		{
			bool isOffline = false;
			lock (_onlineUsers)
			{
				if (!_onlineUsers.ContainsKey(userId)) return Task.FromResult(isOffline);

				_onlineUsers[userId].Remove(connectionId);

				if (_onlineUsers[userId].Count == 0)
				{
					_onlineUsers.TryRemove(userId, out _);
					isOffline = true;
				}
			}
			return Task.FromResult(isOffline);
		}

		public Task<int> GetOnlineUsersCount()
		{
			return Task.FromResult(_onlineUsers.Count);
		}
	}
}

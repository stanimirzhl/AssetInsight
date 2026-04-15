using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface IFollowService
	{
		Task<bool> ToggleFollowAsync(string followerId, string followeeId);
		Task<List<string>> GetFollowersIdsAsync(string userId);
		Task<bool> IsFollowing(string currentUserId, string targetUserId);
	}
}

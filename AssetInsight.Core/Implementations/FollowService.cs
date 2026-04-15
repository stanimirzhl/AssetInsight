using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Implementations
{
	public class FollowService : IFollowService
	{
		private readonly IRepository<Follow> repository;

		public FollowService(IRepository<Follow> repository)
		{
			this.repository = repository;
		}

		public async Task<bool> ToggleFollowAsync(string followerId, string followeeId)
		{
			bool isExisting = repository.All().Any(f => f.FollowerId == followerId && f.FollowedUserId == followeeId);

			if (isExisting)
			{
				var follow = await repository.All().FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedUserId == followeeId);
				await repository.DeleteAsync(follow.Id);
				return false;
			}
			else
			{
				await repository.AddAsync(new Follow { FollowerId = followerId, FollowedUserId = followeeId });
				return true;	
			}
		}

		public async Task<bool> IsFollowing(string currentUserId, string targetUserId)
		{
			return await repository.All()
				.AnyAsync(f => f.FollowerId == currentUserId && f.FollowedUserId == targetUserId);
		}

		public async Task<List<string>> GetFollowersIdsAsync(string userId)
		{
			return await repository.All()
				.Where(f => f.FollowedUserId == userId)
				.Select(f => f.FollowerId)
				.ToListAsync();
		}
	}
}

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
	public class PostReactionService : IPostReactionService
	{
		private readonly IRepository<PostReaction> repository;

		public PostReactionService(IRepository<PostReaction> repository)
		{
			this.repository = repository;
		}

		public async Task<int> GetPostReactionScoreAsync(Guid postId)
		{
			return await repository.All()
				.Where(r => r.PostId == postId)
				.SumAsync(r => r.IsUpVote ? 1 : -1);
		}

		public async Task<(int score, string status)> ToggleReactionAsync(Guid postId, string userId, bool isUpVote)
		{
			var existing = await repository.All()
				.FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);

			string status = "none";

			if (existing != null)
			{
				if (existing.IsUpVote == isUpVote)
				{
					await repository.DeleteAsync(existing.Id);
				}
				else
				{
					existing.IsUpVote = isUpVote;
					status = isUpVote ? "upvoted" : "downvoted";
				}
			}
			else
			{
				await repository.AddAsync(new PostReaction
				{
					PostId = postId,
					UserId = userId,
					IsUpVote = isUpVote
				});
				status = isUpVote ? "upvoted" : "downvoted";
			}

			await repository.SaveChangesAsync();

			var newTotal = await repository.All()
				.Where(r => r.PostId == postId)
				.SumAsync(r => r.IsUpVote ? 1 : -1);

			return (newTotal, status);
		}
	}
}

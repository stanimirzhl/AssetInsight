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
	public class CommentReactionService : ICommentReactionService
	{
		private readonly IRepository<CommentReaction> repository;

		public CommentReactionService(IRepository<CommentReaction> repository)
		{
			this.repository = repository;
		}

		public async Task<int> GetCommentReactionScoreAsync(Guid commentId)
		{
			return await repository.All()
				.Where(r => r.CommentId == commentId)
				.SumAsync(r => r.IsUpVote ? 1 : -1);
		}

		public async Task<(int score, string status)> ToggleReactionAsync(Guid commentId, string userId, bool isUpVote)
		{
			var existing = await repository.All()
				.FirstOrDefaultAsync(r => r.CommentId == commentId && r.UserId == userId);
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
				await repository.AddAsync(new CommentReaction
				{
					CommentId = commentId,
					UserId = userId,
					IsUpVote = isUpVote
				});
				status = isUpVote ? "upvoted" : "downvoted";
			}
			await repository.SaveChangesAsync();
			int newScore = await GetCommentReactionScoreAsync(commentId);
			return (newScore, status);
		}
	}
}

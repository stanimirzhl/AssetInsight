using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface ICommentReactionService
	{
		Task<int> GetCommentReactionScoreAsync(Guid commentId);

		Task<(int score, string status)> ToggleReactionAsync(Guid commentId, string userId, bool isUpVote);
	}
}

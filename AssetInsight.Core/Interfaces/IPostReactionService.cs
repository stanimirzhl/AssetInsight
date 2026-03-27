using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface IPostReactionService
	{
		Task<(int score, string status)> ToggleReactionAsync(Guid postId, string userId, bool isUpVote);

		Task<int> GetPostReactionScoreAsync(Guid postId);
	}
}

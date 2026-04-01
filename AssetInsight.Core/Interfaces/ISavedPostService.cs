using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface ISavedPostService
	{
		Task<bool> ToggleSavePost(Guid postId, string userId);

		Task<bool> HasUserSavedPost(Guid postId, string userId);
	}
}

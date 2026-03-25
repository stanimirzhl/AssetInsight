using AssetInsight.Core.DTOs.Comment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface ICommentService
	{
		Task<PagingModel<CommentDto>> GetRootCommentsPaginated(Guid postId, int pageIndex);

		Task<List<CommentDto>> GetRepliesByParentId(Guid parentId);
	}
}

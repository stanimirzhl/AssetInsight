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
		Task<PagingModel<CommentDto>> GetRootCommentsPaginated(Guid postId, int pageIndex, string userId, string sortBy = "best");

		Task<List<CommentDto>> GetRepliesByParentId(Guid parentId, string userId/*, int skip, int take*/);

		Task<PagingModel<CommentDto>> GetPagedCommentsByUserAsync(string userId, int pageIndex, int pageSize);

		Task<CommentDto> AddAsync(Guid postId, string content, string? authorId, Guid? parentCommentId = null);

		Task<CommentDto> GetByIdAsync(Guid commentId);

		Task EditAsync(Guid postId, Guid commentId, string content);

		Task DeleteAsync(Guid commentId, Guid postId);
	}
}

using AssetInsight.Core.DTOs.Post;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface IPostService
	{
		Task<PagingModel<PostDto>> GetAllPagedPostsAsync(int pageIndex, int pageSize, string tag);

		Task<PagingModel<PostDto>> GetAllPagedPostsByUserNameAsync(string userName, int pageIndex, int pageSize, string sortBy);

		Task<PagingModel<PostDto>> GetSavedPostsPagedAsync(string userId, int pageIndex, int pageSize, string sortBy);

		Task<PagingModel<PostDto>> GetUpvotedPostsPagedAsync(string userId, int pageIndex, int pageSize, string sortBy);

		Task<PagingModel<PostDto>> GetDownvotedPostsPagedAsync(string userId, int pageIndex, int pageSize, string sortBy);

		Task<Guid> AddAsync(PostDto postDto);

		Task<PostDto> GetByIdAsync(Guid id);

		Task EditAsync(PostDto postDto);

		Task DeleteAsync(Guid id);

		Task<bool> IsAuthor(Guid postId, string userId);

		Task<(bool Success, bool IsLocked)> ToggleLockAsync(Guid postId);
	}
}

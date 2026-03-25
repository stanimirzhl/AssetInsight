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
		Task<PagingModel<PostDto>> GetAllPagedPostsAsync(int pageIndex, int pageSize);

		Task<Guid> AddAsync(PostDto postDto);

		Task<PostDto> GetByIdAsync(Guid id);

		Task EditAsync(PostDto postDto);

		Task DeleteAsync(Guid id);
	}
}

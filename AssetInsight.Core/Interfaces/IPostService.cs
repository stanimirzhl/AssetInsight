using AssetInsight.Core.DTOs.Tag;
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
	}
}

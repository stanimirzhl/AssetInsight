using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface IPostTagService
	{
		Task AddAsync(Guid postId, List<Guid> tagIds);

		Task<List<Guid>> GetAllTagIdsByPostIdAsync(Guid postId);

		Task DeleteAsync(List<Guid> tagIds);
	}
}

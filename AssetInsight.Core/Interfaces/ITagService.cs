using AssetInsight.Core.DTOs.Tag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface ITagService
	{
		Task<List<Guid>> ExtractAndAddTagsIfAny(string content);

		Task<List<TagDto>> GetAllTagsbyPostId(Guid postId);

		Task<IEnumerable<TagDto>> GetTrendingTagsAsync(int count = 5);
	}
}

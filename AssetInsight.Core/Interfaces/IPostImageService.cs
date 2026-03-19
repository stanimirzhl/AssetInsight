using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface IPostImageService
	{
		Task AddAsync(List<(string, string)> imageUrls, Guid postId);
	}
}

using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Implementations
{
	public class PostImageService : IPostImageService
	{
		private readonly IRepository<PostImage> repository;

		public PostImageService(IRepository<PostImage> repository)
		{
			this.repository = repository;
		}

		public async Task AddAsync(List<(string, string)> imageUrls, Guid postId)
		{
			foreach ((string url, string publicId) in imageUrls)
			{
				PostImage postImage = new PostImage
				{
					ImgUrl = url,
					PublicId = publicId,
					PostId = postId
				};
				await repository.AddAsync(postImage);
			}
		}
	}
}

using AssetInsight.Core.DTOs.Post_Image;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
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

		public async Task DeleteAsync(Guid postId, List<int> imageIds)
		{
			foreach (int id in imageIds)
			{
				PostImage? image = await repository.GetByIdAsync(id);
				if (image != null && image.PostId == postId)
				{
					await repository.DeleteAsync(id);
				}
			}
		}

		public async Task<List<PostImageDto>> GetAllByPostIdAsync(Guid postId)
		{
			return await repository.AllAsReadOnly()
				.Where(pi => pi.PostId == postId)
				.Select(x => new PostImageDto
				{
					Id = x.Id,
					ImgUrl = x.ImgUrl,
					PublicId = x.PublicId,
				})
				.ToListAsync();
		}
	}
}

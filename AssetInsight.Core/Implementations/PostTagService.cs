using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Implementations
{
	public class PostTagService : IPostTagService
	{
		private readonly IRepository<PostTag> repository;

		public PostTagService(IRepository<PostTag> tagRepository)
		{
			this.repository = tagRepository;
		}

		public async Task AddAsync(Guid postId, List<Guid> tagIds)
		{
			foreach (Guid tagId in tagIds)
			{
				PostTag postTag = new PostTag()
				{
					PostId = postId,
					TagId = tagId
				};

				await repository.AddAsync(postTag);
			}

			await repository.SaveChangesAsync();
		}

		public async Task<List<Guid>> GetAllTagIdsByPostIdAsync(Guid postId)
		{
			return await repository.AllAsReadOnly()
				.Where(x => x.PostId == postId)
				.Select(x => x.TagId)
				.ToListAsync();
		}

		public async Task DeleteAsync(List<Guid> tagIds)
		{
			IQueryable<PostTag> postTags = repository
				.All()
				.Where(x => tagIds.Contains(x.TagId));

			await repository.RemoveRange(postTags);
		}
	}
}

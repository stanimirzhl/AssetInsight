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
	}
}

using AssetInsight.Core.DTOs.Tag;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Implementations
{
	public class PostService : IPostService
	{
		private readonly AssetInsightDbContext context;

		public PostService(AssetInsightDbContext context)
		{
			this.context = context;
		}

		public async Task<PagingModel<PostDto>> GetAllPagedPostsAsync(int pageIndex, int pageSize)
		{
			IQueryable<PostDto> query = context.Posts
				.Include(x => x.Author)
				.Include(x => x.Comments)
				.Include(x => x.Reactions)
			.Select(p => new PostDto
			{
				Id = p.Id,
				Title = p.Title,
				Content = p.Content,
				AuthorName = p.Author == null ? "[deleted]" : p.Author.UserName,
				CreatedAt = p.CreatedAt,
				EditedAt = p.EditedAt,
				IsLocked = p.IsLocked,
				CommentsCount = p.Comments.Count,
				ReactionsCount = p.Reactions.Count
			})
			.OrderByDescending(p => p.CreatedAt);

			return await PagingModel<PostDto>.CreateAsync(query, pageIndex, pageSize);
		}
	}
}

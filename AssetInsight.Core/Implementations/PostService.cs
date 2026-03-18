using AssetInsight.Core.DTOs.Tag;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data;
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
	public class PostService : IPostService
	{
		private readonly IRepository<Post> context;

		public PostService(IRepository<Post> context)
		{
			this.context = context;
		}

		public async Task<Guid> AddAsync(PostDto postDto)
		{
			Post post = new Post
			{
				Title = postDto.Title,
				Content = postDto.Content,
				AuthorId = postDto.AuthorId,
			};

			await context.AddAsync(post);

			return post.Id;
		}

		public async Task<PagingModel<PostDto>> GetAllPagedPostsAsync(int pageIndex, int pageSize)
		{
			IQueryable<PostDto> query = context.AllAsReadOnly()
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

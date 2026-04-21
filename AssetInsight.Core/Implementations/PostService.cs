using AssetInsight.Core.DTOs.Post;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Implementations
{
	public class PostService : IPostService
	{
		private readonly IRepository<Post> repository;

		public PostService(IRepository<Post> context)
		{
			this.repository = context;
		}

		public async Task<Guid> AddAsync(PostDto postDto)
		{
			Post post = new Post
			{
				Title = postDto.Title,
				Content = postDto.Content,
				AuthorId = postDto.AuthorId,
			};

			await repository.AddAsync(post);

			return post.Id;
		}

		public async Task<PostDto> GetByIdAsync(Guid id)
		{
			return await repository.AllAsReadOnly()
			.Where(p => p.Id == id)
			.Select(p => new PostDto
			{
				Id = p.Id,
				Title = p.Title,
				Content = p.Content,
				AuthorId = p.AuthorId,
				AuthorName = p.Author == null ? "[deleted]" : p.Author.UserName,
				CreatedAt = p.CreatedAt,
				EditedAt = p.EditedAt,
				IsLocked = p.IsLocked,
			})
			.FirstOrDefaultAsync() ?? throw new NoEntityException($"No entity found with id: {id}!");
		}

		public async Task EditAsync(PostDto postDto)
		{
			Post? post = await repository.GetByIdAsync(postDto.Id);

			post.Title = postDto.Title;
			post.Content = postDto.Content;
			post.EditedAt = DateTime.Now;

			await repository.SaveChangesAsync();
		}

		public async Task DeleteAsync(Guid id)
		{
			Post post = await repository.GetByIdAsync(id)
				?? throw new NoEntityException($"No entity found with id: {id}!");

			await repository.DeleteAsync(post.Id);
			await repository.SaveChangesAsync();
		}

		public async Task<bool> IsAuthor(Guid postId, string userId)
		{
			return await repository.AllAsReadOnly()
				.AnyAsync(p => p.Id == postId && p.AuthorId == userId);
		}

		public async Task<PagingModel<PostDto>> GetAllPagedPostsAsync(int pageIndex, int pageSize, string tag)
		{
			IQueryable<PostDto> query = repository.AllAsReadOnly()
				.Include(x => x.Author)
				.Include(x => x.Comments)
				.Include(x => x.Reactions)
				.Include(x => x.PostTags)
				 .ThenInclude(x => x.Tag)
			.Where(p => string.IsNullOrEmpty(tag)
				|| p.PostTags.Any(pt => pt.Tag.Name == tag))
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

		public async Task<PagingModel<PostDto>> GetAllPagedPostsByUserNameAsync(string userName, int pageIndex, int pageSize, string sortBy)
		{
			IQueryable<PostDto> query = repository.AllAsReadOnly()
				.Include(x => x.Author)
				.Include(x => x.Comments)
				.Include(x => x.Reactions)
			.Where(p => p.Author != null && p.Author.UserName == userName)
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

			query = sortBy == "top"
					? query.OrderByDescending(p => p.ReactionsCount).ThenByDescending(p => p.CreatedAt)
				: query.OrderByDescending(p => p.CreatedAt);

			return await PagingModel<PostDto>.CreateAsync(query, pageIndex, pageSize);
		}

		public async Task<PagingModel<PostDto>> GetSavedPostsPagedAsync(string userId, int pageIndex, int pageSize, string sortBy)
		{
			IQueryable<PostDto> query = repository.AllAsReadOnly()
				.Include(x => x.Author)
				.Include(x => x.Comments)
				.Include(x => x.Reactions)
				.Include(x => x.SavedPosts)
			.Where(p => p.SavedPosts.Any(sp => sp.UserId == userId))
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

			query = sortBy == "top"
					? query.OrderByDescending(p => p.ReactionsCount).ThenByDescending(p => p.CreatedAt)
				: query.OrderByDescending(p => p.CreatedAt);

			return await PagingModel<PostDto>.CreateAsync(query, pageIndex, pageSize);
		}

		public async Task<PagingModel<PostDto>> GetUpvotedPostsPagedAsync(string userId, int pageIndex, int pageSize, string sortBy)
		{
			IQueryable<PostDto> query = repository.AllAsReadOnly()
				.Include(x => x.Author)
				.Include(x => x.Comments)
				.Include(x => x.Reactions)
			.Where(p => p.Reactions.Any(pr => pr.UserId == userId && pr.IsUpVote))
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

			query = sortBy == "top"
					? query.OrderByDescending(p => p.ReactionsCount).ThenByDescending(p => p.CreatedAt)
				: query.OrderByDescending(p => p.CreatedAt);

			return await PagingModel<PostDto>.CreateAsync(query, pageIndex, pageSize);
		}

		public async Task<PagingModel<PostDto>> GetDownvotedPostsPagedAsync(string userId, int pageIndex, int pageSize, string sortBy)
		{
			IQueryable<PostDto> query = repository.AllAsReadOnly()
				.Include(x => x.Author)
				.Include(x => x.Comments)
				.Include(x => x.Reactions)
			.Where(p => p.Reactions.Any(pr => pr.UserId == userId && !pr.IsUpVote))
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

			query = sortBy == "top"
					? query.OrderByDescending(p => p.ReactionsCount).ThenByDescending(p => p.CreatedAt)
				: query.OrderByDescending(p => p.CreatedAt);

			return await PagingModel<PostDto>.CreateAsync(query, pageIndex, pageSize);
		}
	}
}

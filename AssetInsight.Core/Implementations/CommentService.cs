using AssetInsight.Core.DTOs.Comment;
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
	public class CommentService : ICommentService
	{
		private readonly IRepository<Comment> repository;

		public CommentService(IRepository<Comment> repository)
		{
			this.repository = repository;
		}

		public async Task<PagingModel<CommentDto>> GetRootCommentsPaginated(Guid postId, int pageIndex, string userId)
		{
			IQueryable<Comment> comments = repository.All()
				.Where(x => x.PostId == postId && x.ParentCommentId == null)
				.Include(x => x.Author)
				.Include(x => x.Replies)
				.Include(x => x.Reactions)
				.OrderByDescending(c => c.CreatedAt);

			IQueryable<CommentDto> commentDtos = comments.Select(x => new CommentDto()
			{
				Id = x.Id,
				Content = x.Content,
				CreatedOn = x.CreatedAt,
				AuthorName = x.Author == null ? "[deleted]" : x.Author.UserName,
				UpvoteCount = x.Reactions.Count(r => r.IsUpVote) - x.Reactions.Count(r => !r.IsUpVote),

				UserVote = x.Reactions
				.Where(r => r.UserId == userId)
				.Select(r => (bool?)r.IsUpVote)
				.FirstOrDefault(),

				ReplyCount = x.Replies.Count
			});

			return await PagingModel<CommentDto>.CreateAsync(commentDtos, pageIndex, 5);
		}

		public async Task<List<CommentDto>> GetRepliesByParentId(Guid parentId, string userId)
		{
			return await repository.All()
					.Where(c => c.ParentCommentId == parentId)
					.Include(c => c.Replies)
						.ThenInclude(r => r.Replies)
					.Select(c => new CommentDto
					{
						Id = c.Id,
						AuthorName = c.Author == null ? "[deleted]" : c.Author.UserName,
						Content = c.Content,
						CreatedOn = c.CreatedAt,
						ReplyCount = c.Replies.Count,
						ParentCommentId = c.ParentCommentId,
						UpvoteCount = c.Reactions.Count(r => r.IsUpVote) - c.Reactions.Count(r => !r.IsUpVote),

						UserVote = c.Reactions
						.Where(r => r.UserId == userId)
						.Select(r => (bool?)r.IsUpVote)
						.FirstOrDefault(),

						Replies = c.Replies.Select(r => new CommentDto
						{
							Id = r.Id,
							AuthorName = r.Author == null ? "[deleted]" : r.Author.UserName,
							Content = r.Content,
							CreatedOn = r.CreatedAt,
							ReplyCount = r.Replies.Count,
							ParentCommentId = r.ParentCommentId,
							UpvoteCount = r.Reactions.Count(vote => vote.IsUpVote) - r.Reactions.Count(vote => !vote.IsUpVote),
							UserVote = r.Reactions
							.Where(r => r.UserId == userId)
							.Select(r => (bool?)r.IsUpVote)
							.FirstOrDefault()
						})
						.ToList()
					})
					.ToListAsync();
		}

		public async Task<CommentDto> AddAsync(Guid postId, string content, string? authorId, Guid? parentCommentId = null)
		{
			var comment = new Comment
			{
				Content = content,
				AuthorId = authorId,
				PostId = postId,
				ParentCommentId = parentCommentId,
			};
			await repository.AddAsync(comment);

			return new CommentDto
			{
				Id = comment.Id,
				Content = comment.Content,
				CreatedOn = comment.CreatedAt,
				AuthorName = comment.Author == null ? "[deleted]" : comment.Author.UserName,
				ReplyCount = 0,
				ParentCommentId = comment.ParentCommentId,
				Replies = new List<CommentDto>()
			};
		}
	}
}

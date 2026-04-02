using AssetInsight.Core.DTOs.Comment;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetInsight.Core.Implementations
{
	public class CommentService : ICommentService
	{
		private readonly IRepository<Comment> repository;

		public CommentService(IRepository<Comment> repository)
		{
			this.repository = repository;
		}

		public async Task<PagingModel<CommentDto>> GetRootCommentsPaginated(Guid postId, int pageIndex, string userId, string sortBy = "best")
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

				ReplyCount = x.Replies.Count,
				IsDeleted = x.IsDeleted
			});

			commentDtos = sortBy == "newest"
					? commentDtos.OrderByDescending(c => c.CreatedOn)
				: commentDtos.OrderByDescending(c => c.UpvoteCount).ThenByDescending(c => c.CreatedOn);

			return await PagingModel<CommentDto>.CreateAsync(commentDtos, pageIndex, 5);
		}

		public async Task<PagingModel<CommentDto>> GetPagedCommentsByUserAsync(string userId, int pageIndex, int pageSize)
		{
			IQueryable<CommentDto> query = repository.All()
				.Where(c => c.AuthorId == userId)
				.Include(c => c.Author)
				.Include(c => c.ParentComment)
				 .ThenInclude(pc => pc.Author)
				.Include(c => c.Reactions)
				.Select(c => new CommentDto
				{
					Id = c.Id,
					Content = c.Content,
					CreatedOn = c.CreatedAt,
					ParentCommentAuthorName = c.ParentComment != null ? (c.ParentComment.Author == null ? "[deleted]" : c.ParentComment.Author.UserName) : null,
					AuthorName = c.Author == null ? "[deleted]" : c.Author.UserName,
					UpvoteCount = c.Reactions.Count(r => r.IsUpVote) - c.Reactions.Count(r => !r.IsUpVote),
					UserVote = c.Reactions
						.Where(r => r.UserId == userId)
						.Select(r => (bool?)r.IsUpVote)
						.FirstOrDefault(),
					IsDeleted = c.IsDeleted
				})
				.OrderByDescending(c => c.CreatedOn);
			return await PagingModel<CommentDto>.CreateAsync(query, pageIndex, pageSize);
		}

		public async Task<List<CommentDto>> GetRepliesByParentId(Guid parentId, string userId/*, int skip, int take*/)
		{
			return await repository.All()
					.Where(c => c.ParentCommentId == parentId)
					.Include(c => c.Replies)
						.ThenInclude(r => r.Replies)
					.Include(x => x.Author)
					/*.Skip(skip)
					.Take(take)*/
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

						IsAuthor = c.AuthorId == userId,
						IsDeleted = c.IsDeleted,

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
							.FirstOrDefault(),
							IsAuthor = r.AuthorId == userId,
							IsDeleted = r.IsDeleted
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
				IsAuthor = comment.AuthorId == authorId,
				//Replies = new List<CommentDto>()
			};
		}

		public async Task EditAsync(Guid postId, Guid commentId, string newContent)
		{
			var comment = await repository.GetByIdAsync(commentId) ?? throw new NoEntityException($"No entity found with id: {commentId}");

			if(comment.PostId != postId)
				throw new InvalidOperationException("Comment does not belong to the specified post.");

			comment.Content = newContent;
			comment.EditedAt = DateTime.Now;

			await repository.SaveChangesAsync();
		}

		public async Task DeleteAsync(Guid postId, Guid commentId)
		{
			var comment = await repository.GetByIdAsync(commentId) ?? throw new NoEntityException($"No entity found with id: {commentId}");
			if (comment.PostId != postId)
				throw new InvalidOperationException("Comment does not belong to the specified post.");
			//comment.Content = "[deleted]";
			comment.IsDeleted = true;
			//comment.AuthorId = null;
			await repository.SaveChangesAsync();
		}

		public async Task<CommentDto> GetByIdAsync(Guid commentId)
		{
			var comment = await repository.All()
				.Where(c => c.Id == commentId)
				.Include(c => c.Author)
				.Include(c => c.Reactions)
				.FirstOrDefaultAsync() ?? throw new NoEntityException($"No entity found with id: {commentId}");


			return new CommentDto
			{
				Id = comment.Id,
				Content = comment.Content,
				CreatedOn = comment.CreatedAt,
				AuthorName = comment.Author == null ? "[deleted]" : comment.Author.UserName,
				AuthorId = comment.AuthorId,
			};
		}
	}
}

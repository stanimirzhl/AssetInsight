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

		public async Task<PagingModel<CommentDto>> GetRootCommentsPaginated(Guid postId, int pageIndex)
		{
			IQueryable<Comment> comments = repository.All()
				.Where(x => x.PostId == postId && x.ParentCommentId == null)
				.Include(x => x.Author)
				.Include(x => x.Replies) 
				.OrderByDescending(c => c.CreatedAt);

			IQueryable<CommentDto> commentDtos = comments.Select(x => new CommentDto()
			{
				Id = x.Id,
				Content = x.Content,
				CreatedOn = x.CreatedAt,
				AuthorName = x.Author == null ? "[deleted]" : x.Author.UserName,
				ReplyCount = x.Replies.Count
			});

			return await PagingModel<CommentDto>.CreateAsync(commentDtos, pageIndex, 5);
		}

		public async Task<List<CommentDto>> GetRepliesByParentId(Guid parentId)
		{
			return await repository.All()
				.Where(x => x.ParentCommentId == parentId)
				.Include(x => x.Author)
				.OrderBy(x => x.CreatedAt)
				.Select(x => new CommentDto
				{
					Id = x.Id,
					Content = x.Content,
					CreatedOn = x.CreatedAt,
					AuthorName = x.Author == null ? "[deleted]" : x.Author.UserName,
					ParentCommentId = x.ParentCommentId
				}).ToListAsync();
		}
	}
}

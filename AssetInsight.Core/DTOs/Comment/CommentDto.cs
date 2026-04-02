namespace AssetInsight.Core.DTOs.Comment
{
	public class CommentDto
	{
		public Guid Id { get; set; }
		public string Content { get; set; } = null!;
		public string AuthorName { get; set; } = null!;
		public string? AuthorId { get; set; }
		public bool IsAuthor { get; set; } = false;
		public DateTime CreatedOn { get; set; }
		public int ReplyCount { get; set; }
		public Guid? ParentCommentId { get; set; }
		public string? ParentCommentAuthorName { get; set; }
		public int UpvoteCount { get; set; }
		public bool? UserVote { get; set; }
		public bool IsDeleted { get; set; } = false;

		public IEnumerable<CommentDto> Replies { get; set; } = new List<CommentDto>();
	}
}
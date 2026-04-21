using AssetInsight.Core;
using AssetInsight.Core.DTOs.Comment;
using AssetInsight.Core.DTOs.Post_Image;
using AssetInsight.Core.DTOs.Tag;
using AssetInsight.Models.Comment;

namespace AssetInsight.Models.Post
{
	public class PostDetailsViewModel
	{
		public Guid Id { get; set; }
		public string Title { get; set; } = null!;
		public string Content { get; set; } = null!;
		public string? AuthorName { get; set; }
		public DateTime CreatedAt { get; set; }
		public bool IsLocked { get; set; }
		public int UpvoteCount { get; set; }
		public bool? UserVote { get; set; }
		public bool UserHasSaved { get; set; }

		public List<TagDto> Tags { get; set; } = new();
		public List<PostImageDto> ImgUrls { get; set; } = new();
		public PagingModel<CommentDto> Comments { get; set; }
		public IEnumerable<TagDto> TrendingTags { get; set; } = new List<TagDto>();
	}
}

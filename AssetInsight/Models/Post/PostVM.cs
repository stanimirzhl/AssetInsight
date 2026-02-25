using AssetInsight.Models.Tag;

namespace AssetInsight.Models.Post
{
	public class PostVM
	{
		public Guid Id { get; set; }

		public string Title { get; set; }

		public string Content { get; set; }

		public string AuthorUserName { get; set; }

		public int ReactionsCount { get; set; }

		public DateTime CreatedAt { get; set; }

		public DateTime? EditedAt { get; set; }

		public bool IsLocked { get; set; }

		public int CommentsCount { get; set; }

		public List<TagVM> Tags { get; set; } = new List<TagVM>();
	}
}

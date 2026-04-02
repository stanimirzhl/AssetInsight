namespace AssetInsight.Models.Comment
{
	public class CommentViewModel
	{
		public Guid Id { get; set; }
		public string Author { get; set; } = null!;
		public string Content { get; set; } = null!;
		public DateTime CreatedAt { get; set; }
		public List<CommentViewModel> Replies { get; set; } = new();
	}
}

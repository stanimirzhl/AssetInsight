using AssetInsight.Core;
using AssetInsight.Core.DTOs.Comment;
using AssetInsight.Models.Post;

namespace AssetInsight.Models.Account
{
	public class UserPostsViewModel
	{
		public PagingModel<PostVM> Posts { get; set; }

		public PagingModel<CommentDto> Comments { get; set; }

		public PagingModel<PostVM> Saved { get; set; }

		public PagingModel<PostVM> UpVoted { get; set; }

		public PagingModel<PostVM> DownVoted { get; set; }

		public string Section { get; set; }

		public bool IsUser { get; set; }

		public string SortBy { get; set; } = "newest";

		public bool IsFollowing { get; set; }
	}
}

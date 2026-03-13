using System.ComponentModel.DataAnnotations;
using static AssetInsight.Data.Constants.DataConstants.PostConstants;

namespace AssetInsight.Models.Post
{
	public class PostFormModel
	{
		[Required]
		[StringLength(PostTitleMaxLength, MinimumLength = PostTitleMinLength)]
		public string Title { get; set; }

		[Required]
		[StringLength(PostContentMaxLength, MinimumLength = PostTitleMinLength)]
		public string Content { get; set; }

		public List<IFormFile> Images { get; set; } = new List<IFormFile>();
	}
}

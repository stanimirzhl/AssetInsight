using System.ComponentModel.DataAnnotations;
using static AssetInsight.Data.Constants.DataConstants.PostConstants;

namespace AssetInsight.Models.Post
{
	public class PostFormModel
	{
		[Required(ErrorMessageResourceName = "Title_Required", ErrorMessageResourceType = typeof(Resources.Models.Post.PostFormModel))]
		[StringLength(PostTitleMaxLength, MinimumLength = PostTitleMinLength, ErrorMessageResourceName = "Title_StringLength", 
			ErrorMessageResourceType = typeof(Resources.Models.Post.PostFormModel))]
		[Display(Name = "Title",
			ResourceType = typeof(Resources.Models.Post.PostFormModel))]
		public string Title { get; set; }

		[Required(ErrorMessageResourceName = "Content_Required", ErrorMessageResourceType = typeof(Resources.Models.Post.PostFormModel))]
		[StringLength(PostContentMaxLength, MinimumLength = PostContentMinLength, ErrorMessageResourceName = "Content_StringLength",
			ErrorMessageResourceType = typeof(Resources.Models.Post.PostFormModel))]
		[Display(Name = "Content",
			ResourceType = typeof(Resources.Models.Post.PostFormModel))]
		public string Content { get; set; }

		public List<IFormFile> Images { get; set; } = new List<IFormFile>();
	}
}

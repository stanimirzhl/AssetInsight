using AssetInsight.Core;
using AssetInsight.Core.DTOs.Tag;
using AssetInsight.Core.Interfaces;
using AssetInsight.Models.Post;
using Microsoft.AspNetCore.Mvc;

namespace AssetInsight.Controllers
{
	public class PostController : Controller
	{
		private readonly IPostService postService;

		public PostController(IPostService postService)
		{
			this.postService = postService;
		}

		public async Task<IActionResult> Index()
		{
			PagingModel<PostDto> pagedPostDtos = await postService.GetAllPagedPostsAsync(1, 10);

			PagingModel<PostVM> pagedVMs = pagedPostDtos.Map(dto => new PostVM
			{
				Id = dto.Id,
				Title = dto.Title,
				Content = dto.Content,
				AuthorUserName = dto.AuthorName,
				CreatedAt = dto.CreatedAt,
				EditedAt = dto.EditedAt,
				IsLocked = dto.IsLocked,
				CommentsCount = dto.CommentsCount, 
				ReactionsCount = dto.ReactionsCount,
				//Tags = new List<TagVM>() // Placeholder, you would need to get this from the service
			});

			return View(pagedVMs);
		}
	}
}

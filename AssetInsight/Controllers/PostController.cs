using AssetInsight.Core;
using AssetInsight.Core.DTOs.Tag;
using AssetInsight.Core.Interfaces;
using AssetInsight.Models.Post;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

		//[Authorize]
		public async Task<IActionResult> Create()
		{
			return View();
		}

		[HttpPost]
		//[Authorize]
		public async Task<IActionResult> Create(PostFormModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			if (model.Images.Any())
			{
				model.Images.RemoveAll(x => !x.ContentType.Contains("image/"));


			}

			PostDto postDto = new PostDto
			{
				Title = model.Title,
				Content = model.Content,
				AuthorId = User.FindFirstValue(ClaimTypes.NameIdentifier),
			};

			Guid postId = await postService.AddAsync(postDto);


			return View(model);
		}
	}
}

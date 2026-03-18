using AssetInsight.Core;
using AssetInsight.Core.DTOs.Post;
using AssetInsight.Core.DTOs.Tag;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Models;
using AssetInsight.Models.Post;
using CloudinaryDotNet;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetInsight.Controllers
{
	public class PostController : Controller
	{
		private readonly IPostService postService;
		private readonly ITagService tagService;
		private readonly IPostTagService postTagService;

		public PostController(IPostService postService,
			ITagService tagService,
			IPostTagService postTagService)
		{
			this.postService = postService;
			this.tagService = tagService;
			this.postTagService = postTagService;
		}

		public async Task<IActionResult> Index(string? tag = null)
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
			});

			foreach (PostVM post in pagedVMs.Items)
			{
				post.Tags = await tagService.GetAllTagsbyPostId(post.Id);
			}

			if (!string.IsNullOrEmpty(tag))
			{
				pagedVMs.Items = pagedVMs.Items.Where(p => p.Tags.Any(t => t.Name == tag)).ToList();
			}

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

			List<Guid> tagIds = await tagService.ExtractAndAddTagsIfAny(postDto.Content);

			if(tagIds.Count != 0)
			{
				await postTagService.AddAsync(postId, tagIds);
			}

			return RedirectToAction(nameof(Index));
		}
	}
}

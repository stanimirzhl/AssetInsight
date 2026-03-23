using AssetInsight.Core;
using AssetInsight.Core.DTOs.Image_Error_Dto;
using AssetInsight.Core.DTOs.Images_To_Be_Deleted_Dto;
using AssetInsight.Core.DTOs.Post;
using AssetInsight.Core.DTOs.Post_Image;
using AssetInsight.Core.DTOs.Tag;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Models;
using AssetInsight.Models.Post;
using CloudinaryDotNet;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace AssetInsight.Controllers
{
	public class PostController : Controller
	{
		private readonly ILogger<PostController> logger;
		private readonly IPostService postService;
		private readonly ITagService tagService;
		private readonly IPostTagService postTagService;
		private readonly IImageService imageService;
		private readonly IPostImageService postImageService;

		public PostController(IPostService postService,
			ITagService tagService,
			IPostTagService postTagService,
			ILogger<PostController> logger,
			IImageService imageService,
			IPostImageService postImageService)
		{
			this.postService = postService;
			this.tagService = tagService;
			this.postTagService = postTagService;
			this.logger = logger;
			this.imageService = imageService;
			this.postImageService = postImageService;
		}

		public async Task<IActionResult> Index(int page = 1, string? tag = null)
		{
			PagingModel<PostDto> pagedPostDtos = await postService.GetAllPagedPostsAsync(page, 5);

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

			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
			{
				return PartialView("_PostPartial", pagedVMs.Items);
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

			try
			{
				PostDto postDto = new PostDto
				{
					Title = model.Title,
					Content = model.Content,
					AuthorId = User.FindFirstValue(ClaimTypes.NameIdentifier),
				};

				Guid postId = await postService.AddAsync(postDto);

				List<Guid> tagIds = await tagService.ExtractAndAddTagsIfAny(postDto.Content);

				if (tagIds.Count != 0)
				{
					await postTagService.AddAsync(postId, tagIds);
				}

				if (model.Images.Count != 0)
				{
					model.Images.RemoveAll(x => !x.ContentType.Contains("image/"));

					(List<(string, string)> uploadedResults, List<ErrorImageDto> errorImages) =
						await imageService.UploadPhotosAsync(model.Images, postId);

					if (uploadedResults.Count != 0)
					{
						await postImageService.AddAsync(uploadedResults, postId);
					}

					if (errorImages.Count != 0)
					{
						foreach (ErrorImageDto error in errorImages)
						{
							logger.LogError(error.Exception, error.ToString());
						}
					}
				}

			}
			catch (Exception ex)
			{
				logger.LogError(ex, ex.Message);
			}

			return RedirectToAction(nameof(Index));
		}

		public async Task<IActionResult> Edit(Guid? id)
		{
			PostFormModel model;
			try
			{
				string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

				PostDto postDto = await postService.GetByIdAsync(id.Value);

				if(postDto.AuthorId != null && postDto.AuthorId != userId)
				{
					return Unauthorized();
				}

				model = new PostFormModel
				{
					Id = postDto.Id,
					Title = postDto.Title,
					Content = postDto.Content,
					ExistingImages = await postImageService.GetAllByPostIdAsync(postDto.Id),
				};

			}
			catch (Exception ex)
			{
				logger.LogError(ex, ex.Message);
				return NotFound();
			}

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Edit(Guid id, PostFormModel model)
		{
			if (!ModelState.IsValid)
			{
				model.ExistingImages = await postImageService.GetAllByPostIdAsync(id);
				return View(model);
			}

			if(string.IsNullOrEmpty(model.DeletedImagesJson))
			{
				return BadRequest();
			}

			List<ImageDeleteDto> toBeDeletedDtos = JsonConvert.DeserializeObject<List<ImageDeleteDto>>(model.DeletedImagesJson);
			/*if (toBeDeletedDtos.Count != 0)
				{
					foreach (ImageDeleteDto image in toBeDeletedDtos)
					{
						try
						{
							await imageService.DeletePhotoAsync(image.PublicId);
							await postImageService.DeleteAsync(image.PostImageId);
						}
						catch (Exception ex)
						{
							logger.LogError(ex, ex.Message);
						}
					}
			}*/

			return View(model);
				try
				{
					string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
					PostDto postDto = await postService.GetByIdAsync(id);
					if (postDto.AuthorId != null && postDto.AuthorId != userId)
					{
						return Unauthorized();
					}
					postDto.Title = model.Title;
					postDto.Content = model.Content;
					//await postService.UpdateAsync(postDto);
					List<Guid> tagIds = await tagService.ExtractAndAddTagsIfAny(postDto.Content);
					if (tagIds.Count != 0)
					{
						//await postTagService.UpdateAsync(postDto.Id, tagIds);
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex, ex.Message);
				}
				return RedirectToAction(nameof(Index));
		}
	}
}

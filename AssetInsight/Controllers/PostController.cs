using AssetInsight.Core;
using AssetInsight.Core.DTOs.Image_Error_Dto;
using AssetInsight.Core.DTOs.Images_To_Be_Deleted_Dto;
using AssetInsight.Core.DTOs.Post;
using AssetInsight.Core.DTOs.Post_Image;
using AssetInsight.Core.DTOs.Tag;
using AssetInsight.Core.Implementations;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Models;
using AssetInsight.Models.Account;
using AssetInsight.Models.Post;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Data;
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
		private readonly ICommentService commentService;
		private readonly IPostReactionService postReactionService;
		private readonly ISavedPostService savedPostService;
		private readonly UserManager<User> userManager;
		private readonly IFollowService followService;
		private readonly INotificationService notificationService;

		public PostController(IPostService postService,
			ITagService tagService,
			IPostTagService postTagService,
			ILogger<PostController> logger,
			IImageService imageService,
			IPostImageService postImageService,
			ICommentService commentService,
			IPostReactionService postReactionService,
			ISavedPostService savedPostService,
			UserManager<User> userManager,
			IFollowService followService,
			INotificationService notificationService)
		{
			this.postService = postService;
			this.tagService = tagService;
			this.postTagService = postTagService;
			this.logger = logger;
			this.imageService = imageService;
			this.postImageService = postImageService;
			this.commentService = commentService;
			this.postReactionService = postReactionService;
			this.savedPostService = savedPostService;
			this.userManager = userManager;
			this.followService = followService;
			 this.notificationService = notificationService;
		}

		public async Task<IActionResult> Index(int page = 1, string? tag = null)
		{
			PagingModel<PostDto> pagedPostDtos = await postService.GetAllPagedPostsAsync(page, 5, tag);

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

			/*if (!string.IsNullOrEmpty(tag))
			{
				pagedVMs.Items = pagedVMs.Items.Where(p => p.Tags.Any(t => t.Name == tag)).ToList();
			}*/

			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
			{
				if (pagedVMs.Items.Count() == 0)
				{
					return NoContent();
				}
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
			TryValidateTitleAndContent(model);

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

				var followers = await followService.GetFollowersIdsAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
				foreach (var followerId in followers)
				{
					await notificationService.CreateNotification(
						followerId,
						$"New post from {User.Identity.Name}!",
						$"/Post/Details/{postId}");
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

				if (postDto.AuthorId != null && postDto.AuthorId != userId)
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
			catch (NoEntityException ex)
			{
				logger.LogError(ex, ex.Message);
				return NotFound();
			}

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Edit(Guid id, PostFormModel model)
		{
			if (id != model.Id)
			{
				return BadRequest();
			}

			TryValidateTitleAndContent(model);

			if (!ModelState.IsValid)
			{
				model.ExistingImages = await postImageService.GetAllByPostIdAsync(id);
				return View(model);
			}

			if (string.IsNullOrEmpty(model.DeletedImagesJson))
			{
				return BadRequest();
			}

			try
			{
				await postService.GetByIdAsync(id);
			}
			catch (NoEntityException ex)
			{
				logger.LogError(ex, ex.Message);
				return NotFound();
			}

			HashSet<Guid> originalTagIds = [.. await postTagService.GetAllTagIdsByPostIdAsync(id)];
			HashSet<Guid> newTagIds = [.. await tagService.ExtractAndAddTagsIfAny(model.Content)];

			List<Guid> tagsToAdd = newTagIds.Except(originalTagIds).ToList();
			List<Guid> tagsToRemove = originalTagIds.Except(newTagIds).ToList();

			await postTagService.AddAsync(id, tagsToAdd);
			await postTagService.DeleteAsync(tagsToRemove);

			List<ImageDeleteDto> toBeDeletedDtos;

			try
			{
				toBeDeletedDtos = JsonConvert.DeserializeObject<List<ImageDeleteDto>>(model.DeletedImagesJson)
								?? [];
			}
			catch
			{
				toBeDeletedDtos = new List<ImageDeleteDto>();
			}

			if (toBeDeletedDtos.Count != 0)
			{
				try
				{
					await imageService.DeleteAsync(id, [.. toBeDeletedDtos.Select(x => x.PublicId)]);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, ex.Message);
				}

				try
				{
					await postImageService.DeleteAsync(id, [.. toBeDeletedDtos.Select(x => x.Id)]);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, ex.Message);
				}
			}

			await postService.EditAsync(new PostDto
			{
				Id = id,
				Title = model.Title,
				Content = model.Content,
			});

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		public async Task<IActionResult> Delete(Guid id)
		{
			try
			{
				await postService.GetByIdAsync(id);

				try
				{
					await imageService.DeleteAsync(id, [.. (await postImageService.GetAllByPostIdAsync(id)).Select(x => x.PublicId)]);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, ex.Message);
				}

				await postService.DeleteAsync(id);
			}
			catch (NoEntityException ex)
			{
				logger.LogError(ex, ex.Message);
				return NotFound();
			}

			return Ok();
		}

		public async Task<IActionResult> Details(Guid? id)
		{
			PostDetailsViewModel model;
			try
			{
				string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

				PostDto postDto = await postService.GetByIdAsync(id.Value);
				model = new PostDetailsViewModel
				{
					Id = postDto.Id,
					Title = postDto.Title,
					Content = postDto.Content,
					AuthorName = postDto.AuthorName,
					CreatedAt = postDto.CreatedAt,
					IsLocked = postDto.IsLocked,
					UpvoteCount = await postReactionService.GetPostReactionScoreAsync(postDto.Id),
					Tags = await tagService.GetAllTagsbyPostId(postDto.Id),
					ImgUrls = await postImageService.GetAllByPostIdAsync(postDto.Id),
					Comments = await commentService.GetRootCommentsPaginated(postDto.Id, 1, userId),
					UserHasSaved = await savedPostService.HasUserSavedPost(postDto.Id, userId),
				};
			}
			catch (NoEntityException ex)
			{
				logger.LogError(ex, ex.Message);
				return NotFound();
			}
			return View(model);
		}

		[HttpGet]
		[Route("u/{userName}")]
		public async Task<IActionResult> UserPosts(string userName, string section = "posts", string sortBy = "top", int page = 1)
		{
			if (string.IsNullOrEmpty(userName) || userName == "[deleted]")
			{
				return BadRequest();
			}

			string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			User user = await userManager.FindByNameAsync(userName);

			var viewModel = new UserPostsViewModel { Section = section.ToLower() };

			if (user == null)
			{
				return BadRequest();
			}

			if (currentUserId is not null)
			{
				bool following = await followService.IsFollowing(currentUserId, user.Id);
				viewModel.IsFollowing = following;
			}

			if (viewModel.Section == "saved" || viewModel.Section == "upvoted" || viewModel.Section == "downvoted")
			{
				if (user.Id != currentUserId) return Forbid();
			}

			viewModel.IsUser = user.Id == currentUserId;

			viewModel.SortBy = sortBy.ToLower() == "newest" ? "newest" : "top";

			switch (viewModel.Section)
			{
				case "comments":
					viewModel.Comments = await commentService.GetPagedCommentsByUserAsync(user.Id, page, 5, viewModel.SortBy);
					break;

				case "saved":
					var savedDtos = await postService.GetSavedPostsPagedAsync(user.Id, page, 5, viewModel.SortBy);
					viewModel.Saved = await MapToPostVM(savedDtos);
					break;

				case "upvoted":
					var upvotedDtos = await postService.GetUpvotedPostsPagedAsync(user.Id, page, 5, viewModel.SortBy);
					viewModel.UpVoted = await MapToPostVM(upvotedDtos);
					break;

				case "downvoted":
					var downvotedDtos = await postService.GetDownvotedPostsPagedAsync(user.Id, page, 5, viewModel.SortBy);
					viewModel.DownVoted = await MapToPostVM(downvotedDtos);
					break;

				default:
					var postDtos = await postService.GetAllPagedPostsByUserNameAsync(userName, page, 5, viewModel.SortBy);
					viewModel.Posts = await MapToPostVM(postDtos);
					break;
			}

			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
			{
				return viewModel.Section switch
				{
					"comments" => PartialView("_UserCommentPartial", viewModel.Comments.Items),
					"posts" => PartialView("_PostPartial", viewModel.Posts.Items),
					"saved" => PartialView("PostPartial", viewModel.Saved.Items),
					"upvoted" => PartialView("_PostPartial", viewModel.UpVoted.Items),
					"downvoted" => PartialView("_PostPartial", viewModel.DownVoted.Items),
					_ => NoContent()
				};
			}

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ToggleSave(Guid postId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId is null)
			{
				var returnUrl = Url.Action("Details", "Post", new { id = postId });
				var loginUrl = Url.Page("/Account/Login", null, new { area = "Identity", ReturnUrl = returnUrl });
				return Json(new { loginUrl });
			}

			try
			{
				await postService.GetByIdAsync(postId);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, ex.Message);
				return NotFound();
			}

			bool isSaved = await savedPostService.ToggleSavePost(postId, userId);

			return Json(new
			{
				saved = isSaved,
				message = isSaved ? "Post saved!" : "Post removed from saved."
			});
		}

		private async Task<PagingModel<PostVM>> MapToPostVM(PagingModel<PostDto> source)
		{
			var pagedVMs = source.Map(dto => new PostVM
			{
				Id = dto.Id,
				Title = dto.Title,
				Content = dto.Content,
				AuthorUserName = dto.AuthorName,
				CreatedAt = dto.CreatedAt,
				CommentsCount = dto.CommentsCount,
				ReactionsCount = dto.ReactionsCount,
				IsLocked = dto.IsLocked,
				EditedAt = dto.EditedAt,
			});

			foreach (var post in pagedVMs.Items)
			{
				post.Tags = await tagService.GetAllTagsbyPostId(post.Id);
			}
			return pagedVMs;
		}

		private void TryValidateTitleAndContent(PostFormModel model)
		{
			model.Title = model.Title.Trim();
			model.Content = model.Content.Trim();
			ModelState.Clear();
			TryValidateModel(model);
		}
	}
}

using AssetInsight.Core.Implementations;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AssetInsight.Controllers
{
	public class PostReactionController : Controller
	{
		private readonly IPostReactionService postReactService;
		private readonly INotificationService notificationService;
		private readonly IPostService postService;

		public PostReactionController(IPostReactionService postReactService,
			INotificationService notificationService,
			IPostService postService)
		{
			this.postReactService = postReactService;
			this.notificationService = notificationService;
			this.postService = postService;
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> React(Guid postId, bool isUpVote)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId is null)
			{
				var returnUrl = Url.Action("Details", "Post", new { id = postId });
				var loginUrl = Url.Page("/Account/Login", null, new { area = "Identity", ReturnUrl = returnUrl });
				return Json(new { loginUrl });
			}

			string actionText = isUpVote ? "upvoted" : "downvoted";
			string notificationMessage = $"{User.Identity.Name} {actionText} your post!";

			var post = await postService.GetByIdAsync(postId);
			var postAuthorId = post?.AuthorId;

			if (postAuthorId != null && postAuthorId != userId)
			{
				await notificationService.CreateNotification(
					postAuthorId,
					notificationMessage,
					$"/Post/Details/{postId}");
			}

			var (score, status) = await postReactService.ToggleReactionAsync(postId, userId, isUpVote);
			return Ok(new { score, status });
		}
	}
}

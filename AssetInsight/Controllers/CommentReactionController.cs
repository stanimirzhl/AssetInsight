using AssetInsight.Core.Implementations;
using AssetInsight.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetInsight.Controllers
{
	public class CommentReactionController : Controller
	{
		private readonly ILogger<CommentReactionController> logger;
		private readonly ICommentReactionService commentReactionService;

		public CommentReactionController(ILogger<CommentReactionController> logger,
			ICommentReactionService commentReactionService)
		{
			this.logger = logger;
			this.commentReactionService = commentReactionService;
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> React(Guid postId, Guid commentId, bool isUpVote)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (string.IsNullOrEmpty(userId))
			{
				var returnUrl = Url.Action("Details", "Post", new { id = postId });
				var loginUrl = Url.Page("/Account/Login", null, new { area = "Identity", ReturnUrl = returnUrl });
				return Json(new { loginUrl });
			}

			var (newScore, status) = await commentReactionService.ToggleReactionAsync(commentId, userId, isUpVote);

			return Json(new { score = newScore, status = status });
		}
	}
}

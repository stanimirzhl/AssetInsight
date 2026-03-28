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

		public PostReactionController(IPostReactionService postReactService)
		{
			this.postReactService = postReactService;
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

			var (score, status) = await postReactService.ToggleReactionAsync(postId, userId, isUpVote);
			return Ok(new { score, status });
		}
	}
}

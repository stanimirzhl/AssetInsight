using AssetInsight.Core.Implementations;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetInsight.Controllers
{
	public class FollowController : Controller
	{
		private readonly IFollowService followService;
		private readonly UserManager<User> userManager;
		private readonly INotificationService notificationService;

		public FollowController(IFollowService followService, UserManager<User> userManager,
			INotificationService notificationService)
		{
			this.followService = followService;
			this.userManager = userManager;
			this.notificationService = notificationService;
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Follow(string userName)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId is null)
			{
				var returnUrl = Url.Action("UserPosts", "Post", new { userName = userName });
				var loginUrl = Url.Page("/Account/Login", null, new { area = "Identity", ReturnUrl = returnUrl });
				return Json(new { loginUrl });
			}

			User user = await userManager.FindByNameAsync(userName);
			if (user is null)
			{
				return NotFound();
			}

			var isFollowing = await followService.ToggleFollowAsync(userId, user.Id);

			if (isFollowing)
			{
				await notificationService.CreateNotification(
				user.Id,
				$"{User.Identity.Name} started following you!",
				$"/u/{User.Identity.Name}");
			}

			return Json(new
			{
				isFollowing,
				message = isFollowing ? "Now following user" : "Unfollowed user"
			});
		}
	}
}

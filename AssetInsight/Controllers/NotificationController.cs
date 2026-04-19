using AssetInsight.Core.Implementations;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Models;
using AssetInsight.Models.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetInsight.Controllers
{
	[Authorize]
	public class NotificationController : Controller
	{
		private readonly ILogger<NotificationController> logger;
		private readonly INotificationService notificationService;
		private readonly UserManager<User> userManager;

		public NotificationController(ILogger<NotificationController> logger, INotificationService notificationService,
			UserManager<User> userManager)
		{
			this.logger = logger;
			this.notificationService = notificationService;
			this.userManager = userManager;
		}

		[HttpGet]
		public async Task<IActionResult> GetUnreadCount()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (userId == null)
				return Json(0);

			var count = await notificationService.GetUnreadCountAsync(userId);

			return Json(count);
		}

		//[Authorize]
		public async Task<IActionResult> GetFullNotificationsPartial()
		{
			var userId = userManager.GetUserId(User);
			var notifications = await notificationService.GetAllByIdAsync(userId);

			return PartialView("_NotificationFullListPartial", notifications
				.Select(x => new NotificationVM
				{
					Id = x.Id,
					ReceiverId = x.ReceiverId,
					Message = x.Message,
					TargetUrl = x.TargetUrl,
					IsRead = x.IsRead,
					CreatedAt = x.CreatedAt
				}).ToList());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> MarkAsRead(int? id)
		{
			var userId = userManager.GetUserId(User);

			if (id.HasValue)
			{
				await notificationService.MarkAsReadAsync(id.Value, userId);
			}
			else
			{
				await notificationService.MarkAllAsReadAsync(userId);
			}

			return Ok();
		}

		public IActionResult Refresh()
		{
			return ViewComponent("Notification");
		}
	}
}

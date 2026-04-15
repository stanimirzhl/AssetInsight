using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Models;
using AssetInsight.Models.Notification;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AssetInsight.ViewComponents
{
	public class NotificationViewComponent : ViewComponent
	{
		private readonly INotificationService notificationService;
		private readonly UserManager<User> userManager;

		public NotificationViewComponent(
			INotificationService notificationService,
			UserManager<User> userManager)
		{
			this.notificationService = notificationService;
			this.userManager = userManager;
		}

		public async Task<IViewComponentResult> InvokeAsync()
		{
			var userId = userManager.GetUserId(UserClaimsPrincipal);

			if (userId == null)
			{
				return View("_NotificationPartial", new List<NotificationVM>());
			}

			var notifications = await notificationService.GetLatestAsync(userId);

			return View("_NotificationPartial", notifications.Select(x => new NotificationVM
			{
				Id = x.Id,
				CreatedAt = x.CreatedAt,
				IsRead = x.IsRead,
				Message = x.Message,
				ReceiverId = x.ReceiverId,
				TargetUrl = x.TargetUrl
			}).ToList());
		}
	}
}

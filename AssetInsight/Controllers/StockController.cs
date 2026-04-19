using AssetInsight.Core.Implementations;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AssetInsight.Controllers
{
	public class StockController : Controller
	{
		private readonly IStockService stockService;
		private readonly IWatchListService watchListService;
		private readonly INotificationService notificationService;

		public StockController(IStockService stockService,
			IWatchListService watchListService,
			INotificationService notificationService)
		{
			this.stockService = stockService;
			this.watchListService = watchListService;
			this.notificationService = notificationService;
		}

		[HttpGet]
		public async Task<IActionResult> Details(string symbol = "AAPL", string range = "1mo")
		{
			try
			{
				var viewModel = await stockService.GetStockHistoryAsync(symbol, range);

				viewModel.IsFollowing = User.Identity.IsAuthenticated
					? await watchListService.IsFollowing(User.FindFirstValue(ClaimTypes.NameIdentifier), symbol)
					: false;

				return View(viewModel);
			}
			catch (Exception)
			{
				TempData["ErrorMessage"] = $"Could not find market data for '{symbol}'.";
				return RedirectToAction("Index", "Home");
			}
		}

		[HttpPost]
		[Authorize]
		public async Task<IActionResult> ToggleFollow([FromBody] string symbol)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			bool isFollowing = await watchListService.ToggleWatchList(userId, symbol);

			if (isFollowing)
			{
				string message = $"You are now tracking {symbol}. We will alert you of major price changes.";
				string url = $"/Stock/Details?symbol={symbol}";

				await notificationService.CreateNotification(userId, message, url);

			}

			return Json(new { success = true, isFollowing = isFollowing });
		}
	}
}

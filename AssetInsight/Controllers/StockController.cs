using AssetInsight.Core.Implementations;
using AssetInsight.Core.Interfaces;
using AssetInsight.Core.StrategyEngine.JSON_Options;
using AssetInsight.Core.StrategyEngine.Nodes;
using AssetInsight.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

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
			if (string.IsNullOrWhiteSpace(symbol))
			{
				TempData["Error"] = "Please enter a valid stock ticker.";
				return RedirectToAction("Details", "Stock");
			}

			try
			{
				var chartTask = stockService.GetStockHistoryAsync(symbol, range);
				var newsTask = stockService.GetCompanyNewsAsync(symbol);

				await Task.WhenAll(chartTask, newsTask);

				var viewModel = chartTask.Result;
				viewModel.CompanyNews = newsTask.Result;

				viewModel.IsFollowing = User.Identity.IsAuthenticated
					? await watchListService.IsFollowing(User.FindFirstValue(ClaimTypes.NameIdentifier), symbol)
					: false;

				return View(viewModel);
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Could not find market data for '{symbol}'. Make sure the ticker is correct.";
				return RedirectToAction("Details", "Stock");
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

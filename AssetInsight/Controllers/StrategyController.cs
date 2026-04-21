using AssetInsight.Core.DTOs.TradingStrategy;
using AssetInsight.Core.Interfaces;
using AssetInsight.Models.TradingStrategy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetInsight.Controllers
{
	[Authorize]
	public class StrategyController : Controller
	{
		private readonly IStrategyService strategyService;

		public StrategyController(IStrategyService strategyService)
		{
			this.strategyService = strategyService;
		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var allStrategies = await strategyService.GetAllUserStrategiesAsync(userId!);

			return View(allStrategies);
		}

		[HttpGet]
		public IActionResult Create() => View();

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(StrategyFormModel model)
		{
			if (!ModelState.IsValid) return View(model);

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			try
			{
				var dto = new StrategyDto { Name = model.Name, DefinitionJson = model.DefinitionJson };

				if (User.IsInRole("Admin"))
				{
					await strategyService.CreateCustomStrategyAsync(dto, null);
				}
				else
				{
					await strategyService.CreateCustomStrategyAsync(dto, userId!);
				}

				TempData["Success"] = "Strategy created successfully!";
				return RedirectToAction("Index", "Backtest");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", ex.Message);
				return View(model);
			}
		}

		[HttpGet]
		[Authorize(Roles = "Admin, User")]
		public async Task<IActionResult> Edit(int id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			try
			{
				var strategy = await strategyService.GetStrategyByIdAsync(id);
				if (strategy.UserId != userId) return Unauthorized();

				var model = new StrategyFormModel
				{
					Id = strategy.Id,
					Name = strategy.Name,
					DefinitionJson = strategy.DefinitionJson
				};
				return View(model);
			}
			catch (Exception ex)
			{
				TempData["Error"] = ex.Message;
				return RedirectToAction("Index", "Backtest");
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin, User")]
		public async Task<IActionResult> Edit(int id, StrategyFormModel model)
		{
			if (!ModelState.IsValid) return View(model);

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			try
			{
				var dto = new StrategyDto { Name = model.Name, DefinitionJson = model.DefinitionJson };
				await strategyService.UpdateCustomStrategyAsync(id, dto, userId!);

				TempData["Success"] = "Strategy updated successfully!";
				return RedirectToAction("Index", "Backtest");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", ex.Message);
				return View(model);
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin, User")]
		public async Task<IActionResult> Delete(int id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			try
			{
				await strategyService.DeleteStrategyAsync(id, userId!);
				TempData["Success"] = "Strategy deleted successfully!";
			}
			catch (Exception ex)
			{
				TempData["Error"] = ex.Message;
			}
			return RedirectToAction("Index", "Backtest");
		}
	}
}

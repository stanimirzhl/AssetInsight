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
		public IActionResult Create() => View();

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(StrategyFormModel model)
		{
			if (!ModelState.IsValid) return View(model);

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			try
			{
				var strategy = new StrategyDto
				{
					Name = model.Name,
					DefinitionJson = model.DefinitionJson
				};

				await strategyService.CreateCustomStrategyAsync(strategy, userId!);
				TempData["Success"] = "Strategy created successfully!";
				return RedirectToAction("Index", "Backtest");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", ex.Message);
				return View(model);
			}
		}
	}
}

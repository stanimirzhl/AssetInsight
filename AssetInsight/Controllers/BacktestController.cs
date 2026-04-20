using AssetInsight.Core.Interfaces;
using AssetInsight.Core.StrategyEngine.JSON_Options;
using AssetInsight.Core.StrategyEngine.Nodes;
using AssetInsight.Core.StrategyEngine.Serialization;
using AssetInsight.Data.Models;
using AssetInsight.Models.Backtest;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace AssetInsight.Controllers
{
	public class BacktestController : Controller
	{
		private readonly IStrategyService strategyService;
		private readonly IStockService stockService;
		private readonly IBacktestService backtestService;

		public BacktestController(IStrategyService strategyService, IStockService stockService, IBacktestService backtestService)
		{
			this.strategyService = strategyService;
			this.stockService = stockService;
			this.backtestService = backtestService;
		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var strategies = await strategyService.GetAllUserStrategiesAsync(userId!);

			return View(strategies);
		}

		[HttpPost]
		public async Task<IActionResult> Run(int strategyId, string symbol, decimal initialBalance = 10000)
		{
			var strategy = await strategyService.GetStrategyByIdAsync(strategyId);

			var definition = JsonSerializer.Deserialize<StrategyDefinition>(
				strategy.DefinitionJson,
				new JsonSerializerOptions
				{
					Converters = { new StrategyNodeConverter() },
					PropertyNameCaseInsensitive = true
				});
			var stockData = await stockService.GetStockHistoryAsync(symbol, "1y");

			var result = backtestService.Run(stockData.History.ToList(), definition!, initialBalance);

			var viewModel = new BacktestResultViewModel
			{
				Symbol = symbol,
				StrategyName = strategy.Name,
				InitialBalance = initialBalance,
				FinalBalance = result.FinalBalance,
				TradeLogs = result.Logs
			};

			return View("Result", viewModel);
		}
	}
}

using AssetInsight.Core.DTOs.BackTest;
using AssetInsight.Core.DTOs.Stock;
using AssetInsight.Core.Interfaces;
using AssetInsight.Core.StrategyEngine.Context;
using AssetInsight.Core.StrategyEngine.Nodes;
using Skender.Stock.Indicators;

namespace AssetInsight.Core.Implementations
{
	public class BacktestService : IBacktestService
	{
		public BacktestResult Run(List<ChartDataPoint> history, StrategyDefinition strategy, decimal initialBalance)
		{
			decimal balance = initialBalance;
			decimal sharesOwned = 0;
			var logs = new List<string>();

			var requirements = new List<(string Ind, int Per)>();
			if (strategy.Buy != null) requirements.AddRange(ExtractRequirements(strategy.Buy));
			if (strategy.Sell != null) requirements.AddRange(ExtractRequirements(strategy.Sell));
			requirements = requirements.Distinct().ToList();

			var indicatorCache = PrecomputeIndicators(history, requirements);

			for (int i = 0; i < history.Count; i++)
			{
				var currentPrice = history[i].ClosePrice;
				var context = new IndicatorContext(indicatorCache, i, currentPrice);

				bool shouldBuy = strategy.Buy?.Evaluate(context) ?? false;
				bool shouldSell = strategy.Sell?.Evaluate(context) ?? false;

				if (shouldBuy && balance > currentPrice)
				{
					sharesOwned = Math.Floor(balance / currentPrice);
					balance -= sharesOwned * currentPrice;
					logs.Add($"{history[i].Date:yyyy-MM-dd}: BUY at {currentPrice:F2}");
				}
				else if (shouldSell && sharesOwned > 0)
				{
					balance += sharesOwned * currentPrice;
					logs.Add($"{history[i].Date:yyyy-MM-dd}: SELL at {currentPrice:F2} | Balance: {balance:F2}");
					sharesOwned = 0;
				}
			}

			var finalValue = balance + (sharesOwned * history.Last().ClosePrice);
			return new BacktestResult { FinalBalance = finalValue, Logs = logs };
		}

		private List<(string Ind, int Per)> ExtractRequirements(IStrategyNode node)
		{
			var reqs = new List<(string, int)>();
			if (node is ConditionNode c)
			{
				if (c.Indicator.ToUpper() != "PRICE")
					reqs.Add((c.Indicator.ToUpper(), c.Period));

				if (c.CompareIndicator != null && c.CompareIndicator.ToUpper() != "PRICE")
					reqs.Add((c.CompareIndicator.ToUpper(), c.ComparePeriod!.Value));
			}
			else if (node is GroupNode g)
			{
				foreach (var child in g.Children) reqs.AddRange(ExtractRequirements(child));
			}
			return reqs;
		}

		private Dictionary<string, decimal[]> PrecomputeIndicators(List<ChartDataPoint> history, List<(string Ind, int Per)> reqs)
		{
			var cache = new Dictionary<string, decimal[]>();
			int count = history.Count;

			var quotes = history.Select(h => new Quote
			{
				Date = h.Date,
				Open = h.ClosePrice,
				High = h.ClosePrice,
				Low = h.ClosePrice,
				Close = h.ClosePrice,
				Volume = 0
			}).ToList();

			foreach (var r in reqs)
			{
				var key = $"{r.Ind}_{r.Per}";
				var valuesArray = new decimal[count];

				switch (r.Ind.ToUpper())
				{
					case "SMA":
						var sma = quotes.GetSma(r.Per).ToList();
						for (int i = 0; i < count; i++) valuesArray[i] = (decimal)(sma[i].Sma ?? 0);
						break;

					case "RSI":
						var rsi = quotes.GetRsi(r.Per).ToList();
						for (int i = 0; i < count; i++) valuesArray[i] = (decimal)(rsi[i].Rsi ?? 50);
						break;

					case "EMA":
						var ema = quotes.GetEma(r.Per).ToList();
						for (int i = 0; i < count; i++) valuesArray[i] = (decimal)(ema[i].Ema ?? 0);
						break;

					default:
						throw new Exception($"Indicator {r.Ind} is not supported by the engine yet.");
				}

				cache[key] = valuesArray;
			}

			return cache;
		}
	}
}

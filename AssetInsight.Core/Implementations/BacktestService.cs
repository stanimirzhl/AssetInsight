using AssetInsight.Core.DTOs.BackTest;
using AssetInsight.Core.DTOs.Stock;
using AssetInsight.Core.Interfaces;
using AssetInsight.Core.StrategyEngine.Context;
using AssetInsight.Core.StrategyEngine.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Implementations
{
	public class BacktestService : IBacktestService
	{
		public BacktestResult Run(List<ChartDataPoint> history, StrategyDefinition strategy, decimal initialBalance)
		{
			decimal balance = initialBalance;
			decimal sharesOwned = 0;
			var logs = new List<string>();

			var requirements = new List<(string, int)>();
			requirements.AddRange(ExtractRequirements(strategy.Buy));
			requirements.AddRange(ExtractRequirements(strategy.Sell));
			requirements = requirements.Distinct().ToList();

			var indicatorMatrix = PrecomputeIndicators(history, requirements);

			for (int i = 0; i < history.Count; i++)
			{
				var context = new IndicatorContext(indicatorMatrix[i]);

				bool shouldBuy = strategy.Buy?.Evaluate(context) ?? false;
				bool shouldSell = strategy.Sell?.Evaluate(context) ?? false;

				if (shouldBuy && balance > history[i].ClosePrice)
				{
					sharesOwned = Math.Floor(balance / history[i].ClosePrice);
					balance -= sharesOwned * history[i].ClosePrice;
					logs.Add($"{history[i].Date:yyyy-MM-dd}: BUY at {history[i].ClosePrice}");
				}
				else if (shouldSell && sharesOwned > 0)
				{
					balance += sharesOwned * history[i].ClosePrice;
					logs.Add($"{history[i].Date:yyyy-MM-dd}: SELL at {history[i].ClosePrice} | Balance: {balance:F2}");
					sharesOwned = 0;
				}
			}

			var finalValue = balance + (sharesOwned * history.Last().ClosePrice);
			return new BacktestResult { FinalBalance = finalValue, Logs = logs };
		}

		private List<(string, int)> ExtractRequirements(IStrategyNode node)
		{
			var reqs = new List<(string, int)>();
			if (node is ConditionNode c)
			{
				reqs.Add((c.Indicator.ToUpper(), c.Period));
				if (c.CompareIndicator != null)
					reqs.Add((c.CompareIndicator.ToUpper(), c.ComparePeriod!.Value));
			}
			else if (node is GroupNode g)
			{
				foreach (var child in g.Children) reqs.AddRange(ExtractRequirements(child));
			}
			return reqs;
		}

		private List<Dictionary<string, decimal>> PrecomputeIndicators(List<ChartDataPoint> history, List<(string Ind, int Per)> reqs)
		{
			var result = new List<Dictionary<string, decimal>>();
			for (int i = 0; i < history.Count; i++)
			{
				var dict = new Dictionary<string, decimal>();
				foreach (var r in reqs)
				{
					dict[$"{r.Ind}_{r.Per}"] = r.Ind == "SMA" ? CalculateSma(history, i, r.Per) : CalculateRsi(history, i, r.Per);
				}
				result.Add(dict);
			}
			return result;
		}

		private decimal CalculateSma(List<ChartDataPoint> history, int index, int period)
		{
			if (index + 1 < period) return 0;
			decimal sum = 0;
			for (int i = index - period + 1; i <= index; i++) sum += history[i].ClosePrice;
			return sum / period;
		}

		private decimal CalculateRsi(List<ChartDataPoint> history, int index, int period)
		{
			if (index < period) return 50;
			decimal gain = 0, loss = 0;
			for (int i = index - period + 1; i <= index; i++)
			{
				var change = history[i].ClosePrice - history[i - 1].ClosePrice;
				if (change > 0) gain += change;
				else loss -= change;
			}
			if (loss == 0) return 100;
			var rs = gain / loss;
			return 100 - (100 / (1 + rs));
		}
	}
}

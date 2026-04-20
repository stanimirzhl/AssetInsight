using AssetInsight.Core.DTOs.BackTest;
using AssetInsight.Core.DTOs.Stock;
using AssetInsight.Core.StrategyEngine.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface IBacktestService
	{
		public BacktestResult Run(List<ChartDataPoint> history, StrategyDefinition strategy, decimal initialBalance);
	}
}

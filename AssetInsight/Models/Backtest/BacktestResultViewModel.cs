namespace AssetInsight.Models.Backtest
{
	public class BacktestResultViewModel
	{
		public string Symbol { get; set; }
		public string StrategyName { get; set; }
		public decimal InitialBalance { get; set; }
		public decimal FinalBalance { get; set; }
		public decimal ProfitPercentage => ((FinalBalance - InitialBalance) / InitialBalance) * 100;
		public List<string> TradeLogs { get; set; }
	}
}

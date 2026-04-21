using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.StrategyEngine.Context
{
	public class IndicatorContext
	{
		private readonly Dictionary<string, decimal[]> _indicatorCache;
		private readonly int _currentIndex;
		private readonly decimal _currentPrice;

		public IndicatorContext(Dictionary<string, decimal[]> indicatorCache, int currentIndex, decimal currentPrice)
		{
			_indicatorCache = indicatorCache;
			_currentIndex = currentIndex;
			_currentPrice = currentPrice;
		}

		public decimal GetValue(string indicator, int period)
		{
			if (indicator.ToUpper() == "PRICE")
				return _currentPrice;

			var key = $"{indicator.ToUpper()}_{period}";

			if (!_indicatorCache.TryGetValue(key, out var values))
				throw new Exception($"Missing indicator value: {key}");

			return values[_currentIndex];
		}
	}
}

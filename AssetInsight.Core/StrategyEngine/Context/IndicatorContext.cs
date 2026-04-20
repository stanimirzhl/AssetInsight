using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.StrategyEngine.Context
{
	public class IndicatorContext
	{
		private readonly Dictionary<string, decimal> _values;

		public IndicatorContext(Dictionary<string, decimal> values)
		{
			_values = values;
		}

		public decimal GetValue(string indicator, int period)
		{
			var key = $"{indicator}_{period}";

			if (!_values.TryGetValue(key, out var value))
				throw new Exception($"Missing indicator value: {key}");

			return value;
		}
	}
}

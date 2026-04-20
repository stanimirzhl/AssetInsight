using AssetInsight.Core.StrategyEngine.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.StrategyEngine.Nodes
{
	public class ConditionNode : IStrategyNode
	{
		public string Type => "Condition";
		public string Indicator { get; set; } = null!;
		public int Period { get; set; }
		public string Operator { get; set; } = null!;

		public decimal? Value { get; set; }

		public string? CompareIndicator { get; set; }
		public int? ComparePeriod { get; set; }

		public bool Evaluate(IndicatorContext context)
		{
			var left = context.GetValue(Indicator, Period);

			var right = CompareIndicator != null
				? context.GetValue(CompareIndicator, ComparePeriod!.Value)
				: Value ?? 0;

			return Operator switch
			{
				">" => left > right,
				"<" => left < right,
				">=" => left >= right,
				"<=" => left <= right,
				"==" => left == right,
				_ => throw new Exception($"Invalid operator: {Operator}")
			};
		}
	}
}

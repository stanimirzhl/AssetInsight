using AssetInsight.Core.StrategyEngine.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.StrategyEngine.Nodes
{
	public class GroupNode : IStrategyNode
	{
		public string Type => "Group";

		public string LogicalOperator { get; set; } = "AND";

		public List<IStrategyNode> Children { get; set; } = new();

		public bool Evaluate(IndicatorContext context)
		{
			return LogicalOperator switch
			{
				"AND" => Children.All(c => c.Evaluate(context)),
				"OR" => Children.Any(c => c.Evaluate(context)),
				_ => throw new Exception($"Invalid logical operator: {LogicalOperator}")
			};
		}
	}
}

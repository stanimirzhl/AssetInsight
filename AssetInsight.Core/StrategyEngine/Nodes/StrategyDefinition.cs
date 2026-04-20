using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.StrategyEngine.Nodes
{
	public class StrategyDefinition
	{
		public IStrategyNode? Buy { get; set; } = null!;
		public IStrategyNode? Sell { get; set; } = null!;
	}
}

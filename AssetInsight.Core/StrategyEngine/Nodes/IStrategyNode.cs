using AssetInsight.Core.StrategyEngine.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.StrategyEngine.Nodes
{
	public interface IStrategyNode
	{
		bool Evaluate(IndicatorContext context);
	}
}

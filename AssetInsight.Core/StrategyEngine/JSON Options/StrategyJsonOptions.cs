using AssetInsight.Core.StrategyEngine.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AssetInsight.Core.StrategyEngine.JSON_Options
{
	public static class StrategyJsonOptions
	{
		public static JsonSerializerOptions Default => new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			Converters = { new StrategyNodeConverter() }
		};
	}
}

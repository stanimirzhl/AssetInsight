using System.ComponentModel.DataAnnotations;

namespace AssetInsight.Models.TradingStrategy
{
	public class StrategyFormModel
	{
		[Required]
		[StringLength(100)]
		public string Name { get; set; } = null!;

		[Required]
		public string DefinitionJson { get; set; } = string.Empty;
	}
}

using System.ComponentModel.DataAnnotations;

namespace AssetInsight.Models.TradingStrategy
{
	public class StrategyFormModel
	{
		public int? Id { get; set; }

		[Required]
		[StringLength(100)]
		public string Name { get; set; } = null!;

		[Required]
		public string DefinitionJson { get; set; } = string.Empty;
	}
}

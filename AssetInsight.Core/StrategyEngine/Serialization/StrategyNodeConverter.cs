using AssetInsight.Core.StrategyEngine.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AssetInsight.Core.StrategyEngine.Serialization
{
	public class StrategyNodeConverter : JsonConverter<IStrategyNode>
	{
		public override IStrategyNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
			{
				var root = doc.RootElement;

				if (!root.TryGetProperty("type", out var typeProp))
					return null;

				var type = typeProp.GetString();
				return type switch
				{
					"Group" => JsonSerializer.Deserialize<GroupNode>(root.GetRawText(), options),
					"Condition" => JsonSerializer.Deserialize<ConditionNode>(root.GetRawText(), options),
					_ => throw new JsonException($"Unknown node type: {type}")
				};
			}
		}
		public override void Write(Utf8JsonWriter writer, IStrategyNode value, JsonSerializerOptions options)
		{
			JsonSerializer.Serialize(writer, value, value.GetType(), options);
		}
	}
}

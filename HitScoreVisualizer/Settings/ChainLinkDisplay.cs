using System.Collections.Generic;
using Newtonsoft.Json;

namespace HitScoreVisualizer.Settings
{
	public class ChainLinkDisplay
	{
		internal static ChainLinkDisplay Default { get; } = new("20", new List<float> { 1f, 1f, 1f, 1f });

		// The text to display (if judgment text is enabled).
		[JsonProperty("text")]
		public string Text { get; internal set; }

		// 4 floats, 0-1; red, green, blue, glow (not transparency!)
		// leaving this out should look obviously wrong
		[JsonProperty("color")]
		public List<float> Color { get; internal set; }

		[JsonConstructor]
		public ChainLinkDisplay(string text, List<float> color)
		{
			Text = text;
			Color = color;
		}
	}
}
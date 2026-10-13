///////////////////////////////////////////////////////////////////////////////
//
// (C) 2026 ICE TEA GROUP LLC - ALL RIGHTS RESERVED
//
//
//
// ALL INFORMATION CONTAINED HEREIN IS, AND REMAINS
// THE PROPERTY OF ICE TEA GROUP LLC AND ITS SUPPLIERS, IF ANY.
// THE INTELLECTUAL PROPERTY AND TECHNICAL CONCEPTS CONTAINED
// HEREIN ARE PROPRIETARY TO ICE TEA GROUP LLC AND ITS SUPPLIERS
// AND MAY BE COVERED BY U.S. AND FOREIGN PATENTS, PATENT IN PROCESS, AND
// ARE PROTECTED BY TRADE SECRET OR COPYRIGHT LAW.
//
// DISSEMINATION OF THIS INFORMATION OR REPRODUCTION OF THIS MATERIAL
// IS STRICTLY FORBIDDEN UNLESS PRIOR WRITTEN PERMISSION IS OBTAINED
// FROM ICE TEA GROUP LLC.
//
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Wisej.AI.Html.Dom;

namespace Wisej.AI.Html.Content
{
	/// <summary>
	/// Recognizes the lists written by Microsoft Word and Outlook in HTML: paragraphs with a
	/// <c>mso-list</c> style or a <c>MsoList*</c> class whose marker ("1.", "a)", "·") is typed text
	/// followed by padding, instead of <c>&lt;ol&gt;</c> and <c>&lt;li&gt;</c> elements.
	/// </summary>
	internal static class OfficeList
	{
		/// <summary>Matches the level of the <c>mso-list</c> style: <c>mso-list:l0 level2 lfo1</c>.</summary>
		private static readonly Regex LevelPattern = new Regex(@"mso-list:\s*l\d+\s+level(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		/// <summary>Matches the left margin of a paragraph: <c>margin-left:.5in</c>.</summary>
		private static readonly Regex MarginPattern = new Regex(@"margin-left:\s*(-?[\d.]+)(in|cm|mm|pt|px)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		/// <summary>Matches an ordered marker: <c>1.</c>, <c>a)</c>, <c>iv.</c>, <c>2.1.</c>.</summary>
		private static readonly Regex OrderedPattern = new Regex(@"^[(]?([0-9]{1,4}|[a-zA-Z]{1,6})([.][0-9]{1,4})*[.)]$", RegexOptions.CultureInvariant);

		/// <summary>
		/// Determines whether an element is a list paragraph of Word or Outlook.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> for a list paragraph.</returns>
		public static bool IsListParagraph(HtmlElement element)
		{
			if (!element.IsHtml || (element.LocalName != "p" && element.LocalName != "div"))
				return false;

			var style = element.GetAttribute("style");
			if (style != null && LevelPattern.IsMatch(style))
				return true;

			foreach (var className in element.ClassList)
			{
				if (className.StartsWith("MsoList", StringComparison.OrdinalIgnoreCase))
					return FindMarker(element, out _) != null;
			}

			return false;
		}

		/// <summary>
		/// Returns the nesting level of a list paragraph, from its <c>mso-list</c> style, its class
		/// (<c>MsoListBullet2</c>) or its left margin.
		/// </summary>
		/// <param name="element">The list paragraph.</param>
		/// <returns>The level, from 1.</returns>
		public static int GetLevel(HtmlElement element)
		{
			var style = element.GetAttribute("style") ?? "";

			var level = LevelPattern.Match(style);
			// Word uses at most 9 levels; anything else is clamped.
			if (level.Success)
				return Int32.TryParse(level.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? Math.Max(1, Math.Min(9, value)) : 9;

			foreach (var className in element.ClassList)
			{
				var last = className[className.Length - 1];
				if (className.StartsWith("MsoList", StringComparison.OrdinalIgnoreCase) && last >= '2' && last <= '9' && !className.EndsWith("CxSp", StringComparison.Ordinal))
					return last - '0';
			}

			// Word indents every level by half an inch.
			var margin = MarginPattern.Match(style);
			if (margin.Success && Double.TryParse(margin.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var length))
			{
				var inches = length / UnitsPerInch(margin.Groups[2].Value.ToLowerInvariant());
				return Math.Max(1, Math.Min(9, (int)Math.Round(inches / 0.5)));
			}

			return 1;
		}

		/// <summary>
		/// Returns the number of units of a CSS length per inch.
		/// </summary>
		/// <param name="unit">The lowercase unit: <c>in</c>, <c>cm</c>, <c>mm</c>, <c>pt</c> or <c>px</c>.</param>
		/// <returns>The units per inch.</returns>
		private static double UnitsPerInch(string unit)
		{
			switch (unit)
			{
				case "cm":
					return 2.54;

				case "mm":
					return 25.4;

				case "pt":
					return 72;

				case "px":
					return 96;

				default:
					return 1;
			}
		}

		/// <summary>
		/// Finds the typed marker of a list paragraph: the content of the <c>mso-list:Ignore</c> span, or the
		/// text that precedes the padding span in filtered HTML.
		/// </summary>
		/// <param name="element">The list paragraph.</param>
		/// <param name="nodes">Receives the nodes that make up the marker, to be skipped in the content.</param>
		/// <returns>The marker text, or <c>null</c> when the paragraph has none.</returns>
		public static string FindMarker(HtmlElement element, out List<HtmlNode> nodes)
		{
			nodes = new List<HtmlNode>();

			foreach (var span in element.DescendantElements())
			{
				var style = span.GetAttribute("style");
				if (style != null && style.Replace(" ", "").IndexOf("mso-list:ignore", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					nodes.Add(span);
					return Clean(span.TextContent);
				}
			}

			// filtered HTML: the marker is the leading text, followed by a span of non-breaking spaces.
			var marker = new StringBuilder();
			for (var node = element.FirstChild; node != null; node = node.NextSibling)
			{
				nodes.Add(node);

				if (node is HtmlElement child && IsPadding(child))
				{
					var text = Clean(marker.ToString());
					return text.Length > 0 && text.Length <= 12 ? text : null;
				}

				marker.Append(node.TextContent);
				if (marker.Length > 40)
					break;
			}

			nodes.Clear();
			return null;
		}

		/// <summary>
		/// Converts a typed marker to the marker of the list item.
		/// </summary>
		/// <param name="marker">The typed marker.</param>
		/// <returns>The marker itself for ordered items, <c>-</c> for bullets.</returns>
		public static string ToListMarker(string marker)
		{
			return OrderedPattern.IsMatch(marker) ? marker : "-";
		}

		/// <summary>
		/// Determines whether an element is the padding that Word writes after a list marker.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> for a span made of non-breaking spaces.</returns>
		private static bool IsPadding(HtmlElement element)
		{
			var text = element.TextContent;
			return text.Length > 0 && text.Trim(' ', ' ').Length == 0;
		}

		/// <summary>
		/// Removes the white space around a marker.
		/// </summary>
		/// <param name="text">The marker text.</param>
		/// <returns>The trimmed marker.</returns>
		private static string Clean(string text)
		{
			return text.Replace(' ', ' ').Trim();
		}
	}
}

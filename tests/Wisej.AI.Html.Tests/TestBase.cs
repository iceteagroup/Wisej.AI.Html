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
using System.Text;
using Wisej.AI.Html.Dom;

namespace Wisej.AI.Html.Tests
{
	/// <summary>
	/// Helpers shared by the tests.
	/// </summary>
	public abstract class TestBase
	{
		/// <summary>
		/// Converts HTML to Markdown with test friendly defaults: no title heading.
		/// </summary>
		/// <param name="html">The markup.</param>
		/// <param name="configure">Changes the options before the extraction. Can be <c>null</c>.</param>
		/// <returns>The Markdown text.</returns>
		protected static string Markdown(string html, Action<HtmlExtractorOptions> configure = null)
		{
			return Extract(html, configure).Text;
		}

		/// <summary>
		/// Converts HTML to plain text with test friendly defaults.
		/// </summary>
		/// <param name="html">The markup.</param>
		/// <param name="configure">Changes the options before the extraction. Can be <c>null</c>.</param>
		/// <returns>The plain text.</returns>
		protected static string PlainText(string html, Action<HtmlExtractorOptions> configure = null)
		{
			return Markdown(html, options =>
			{
				options.Format = OutputFormat.PlainText;
				configure?.Invoke(options);
			});
		}

		/// <summary>
		/// Extracts HTML with test friendly defaults: no title heading.
		/// </summary>
		/// <param name="html">The markup.</param>
		/// <param name="configure">Changes the options before the extraction. Can be <c>null</c>.</param>
		/// <returns>The extraction result.</returns>
		protected static HtmlExtractionResult Extract(string html, Action<HtmlExtractorOptions> configure = null)
		{
			var options = new HtmlExtractorOptions { IncludeTitle = false };
			configure?.Invoke(options);

			return new HtmlExtractor(options).ExtractFromString(html);
		}

		/// <summary>
		/// Extracts HTML from bytes, as a file or a web response would provide it.
		/// </summary>
		/// <param name="data">The bytes.</param>
		/// <param name="configure">Changes the options before the extraction. Can be <c>null</c>.</param>
		/// <returns>The extraction result.</returns>
		protected static HtmlExtractionResult Extract(byte[] data, Action<HtmlExtractorOptions> configure = null)
		{
			var options = new HtmlExtractorOptions { IncludeTitle = false };
			configure?.Invoke(options);

			return new HtmlExtractor(options).Extract(data);
		}

		/// <summary>
		/// Writes a document tree in a compact form: elements as <c>name(children)</c>, text in quotes.
		/// </summary>
		/// <param name="node">The node whose children are written.</param>
		/// <returns>The compact tree.</returns>
		protected static string Tree(HtmlNode node)
		{
			var sb = new StringBuilder();

			for (var child = node.FirstChild; child != null; child = child.NextSibling)
			{
				if (sb.Length > 0)
					sb.Append(' ');

				switch (child)
				{
					case HtmlElement element:
						sb.Append(element.LocalName);
						if (element.HasChildNodes)
							sb.Append('(').Append(Tree(element)).Append(')');
						break;

					case HtmlText text:
						sb.Append('"').Append(text.Data).Append('"');
						break;

					case HtmlComment comment:
						sb.Append("<!--").Append(comment.Data).Append("-->");
						break;
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Writes the tree of the body of a document.
		/// </summary>
		/// <param name="html">The markup.</param>
		/// <returns>The compact tree of the body.</returns>
		protected static string BodyTree(string html)
		{
			return Tree(HtmlDocument.Parse(html).Body);
		}
	}
}

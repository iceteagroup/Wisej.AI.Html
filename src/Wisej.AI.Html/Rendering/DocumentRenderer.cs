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
using System.Linq;
using System.Text;
using Wisej.AI.Html.Content;

namespace Wisej.AI.Html.Rendering
{
	/// <summary>
	/// Where inline content is written, which determines how line breaks and special characters are written.
	/// </summary>
	internal enum InlineMode
	{
		/// <summary>A paragraph: line breaks are newlines.</summary>
		Block,

		/// <summary>A single line, such as a heading: line breaks are spaces.</summary>
		Line,

		/// <summary>A pipe table cell: line breaks are <c>&lt;br&gt;</c> and pipes are escaped.</summary>
		Cell,

		/// <summary>An HTML table cell: line breaks are <c>&lt;br&gt;</c>.</summary>
		HtmlCell,
	}

	/// <summary>
	/// The base class of the renderers: splits the content into pages and lays out the blocks; the
	/// derived classes format paragraphs, code, tables and inline content.
	/// </summary>
	internal abstract class DocumentRenderer
	{
		// the content being rendered.
		private readonly DocumentContent _content;

		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentRenderer"/> class.
		/// </summary>
		/// <param name="content">The content.</param>
		/// <param name="options">The extraction options.</param>
		protected DocumentRenderer(DocumentContent content, HtmlExtractorOptions options)
		{
			_content = content;
			this.Options = options;
		}

		#region Properties

		/// <summary>
		/// Gets the extraction options.
		/// </summary>
		protected HtmlExtractorOptions Options { get; }

		/// <summary>
		/// Gets the number of the page being rendered.
		/// </summary>
		protected int CurrentPage { get; private set; }

		/// <summary>
		/// Gets the images rendered on the current page.
		/// </summary>
		protected List<HtmlImage> PageImages { get; private set; }

		/// <summary>
		/// Gets the default format of the page header line.
		/// </summary>
		protected abstract string DefaultPageHeaderFormat { get; }

		#endregion

		#region Render

		/// <summary>
		/// Renders the content.
		/// </summary>
		/// <returns>The extraction result.</returns>
		public HtmlExtractionResult Render()
		{
			var pageBlocks = new List<List<Block>> { new List<Block>() };
			foreach (var block in _content.Blocks)
			{
				if (block is PageBreakBlock)
					pageBlocks.Add(new List<Block>());
				else
					pageBlocks[pageBlocks.Count - 1].Add(block);
			}

			pageBlocks.RemoveAll(p => p.Count == 0);
			if (pageBlocks.Count == 0)
				pageBlocks.Add(new List<Block>());

			var pages = new List<HtmlPage>();
			for (var i = 0; i < pageBlocks.Count; i++)
			{
				this.CurrentPage = i + 1;
				this.PageImages = new List<HtmlImage>();

				var text = RenderBlocks(pageBlocks[i]);
				pages.Add(new HtmlPage(i + 1, text, this.PageImages));
			}

			// images that were not rendered (such as inside an unused caption) are on the first page.
			foreach (var image in _content.Images)
			{
				if (image.PageNumber == 0)
					image.PageNumber = 1;
			}

			return new HtmlExtractionResult(JoinPages(pages), pages, _content.Images, _content.Links, _content.Metadata);
		}

		/// <summary>
		/// Joins the pages into the text of the document, introducing every page with the header line
		/// when the document has more than one page.
		/// </summary>
		/// <param name="pages">The pages.</param>
		/// <returns>The text.</returns>
		private string JoinPages(List<HtmlPage> pages)
		{
			if (pages.Count == 1)
				return pages[0].Text;

			var format = this.Options.PageHeaderFormat ?? this.DefaultPageHeaderFormat;
			var sb = new StringBuilder();

			foreach (var page in pages)
			{
				if (sb.Length > 0)
					sb.Append("\n\n");

				if (format.Length > 0)
					sb.Append(FormatPageHeader(format, page.Number, pages.Count)).Append("\n\n");

				sb.Append(page.Text);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Formats the header line of a page. An invalid composite format is written as is.
		/// </summary>
		/// <param name="format">The composite format.</param>
		/// <param name="number">The page number.</param>
		/// <param name="count">The number of pages.</param>
		/// <returns>The header line.</returns>
		private static string FormatPageHeader(string format, int number, int count)
		{
			try
			{
				return String.Format(CultureInfo.InvariantCulture, format, number, count);
			}
			catch (FormatException)
			{
				return format;
			}
		}

		#endregion

		#region Blocks

		/// <summary>
		/// Renders a sequence of blocks, separated by blank lines.
		/// </summary>
		/// <param name="blocks">The blocks.</param>
		/// <returns>The text.</returns>
		protected string RenderBlocks(List<Block> blocks)
		{
			var sb = new StringBuilder();
			Block previous = null;

			foreach (var block in blocks)
			{
				var text = RenderBlock(block);
				if (String.IsNullOrEmpty(text))
					continue;

				if (sb.Length > 0)
					sb.Append(IsTight(previous, block) ? "\n" : "\n\n");

				sb.Append(text);
				previous = block;
			}

			return sb.ToString();
		}

		/// <summary>
		/// Renders a block.
		/// </summary>
		/// <param name="block">The block.</param>
		/// <returns>The text, or an empty string.</returns>
		protected string RenderBlock(Block block)
		{
			switch (block)
			{
				case ParagraphBlock paragraph:
					return RenderParagraph(paragraph);

				case CodeBlock code:
					return RenderCode(code);

				case QuoteBlock quote:
					return PrefixLines(RenderBlocks(quote.Blocks), "> ", "> ", trimEmpty: true);

				case ListBlock list:
					return RenderList(list);

				case DefinitionBlock definition:
					return RenderDefinition(definition);

				case TableBlock table:
					return RenderTable(table);

				case RuleBlock _:
					return "---";

				case MathBlock math:
					return RenderMath(math);

				default:
					return "";
			}
		}

		/// <summary>
		/// Determines whether two consecutive blocks are written on consecutive lines: a term and its
		/// definitions.
		/// </summary>
		/// <param name="previous">The previous block, or <c>null</c>.</param>
		/// <param name="next">The next block.</param>
		/// <returns><c>true</c> when no blank line separates the blocks.</returns>
		private static bool IsTight(Block previous, Block next)
		{
			return next is DefinitionBlock && (previous is DefinitionBlock || (previous is ParagraphBlock paragraph && paragraph.IsTerm));
		}

		/// <summary>
		/// Renders a list: one item per line, nested content indented under the marker.
		/// </summary>
		/// <param name="list">The list.</param>
		/// <returns>The text.</returns>
		protected virtual string RenderList(ListBlock list)
		{
			var sb = new StringBuilder();

			foreach (var item in list.Items)
			{
				var content = RenderItemBlocks(item.Blocks);
				if (content.Length == 0)
					continue;

				if (sb.Length > 0)
					sb.Append('\n');

				if (item.Marker == null)
				{
					sb.Append(PrefixLines(content, "  ", "  ", trimEmpty: true));
				}
				else
				{
					var indent = new string(' ', item.Marker.Length + 1);
					sb.Append(PrefixLines(content, item.Marker + " ", indent, trimEmpty: true));
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Renders the content of a list item or definition compactly: blocks on consecutive lines, with a
		/// blank line only around tables and code, which Markdown cannot continue without one.
		/// </summary>
		/// <param name="blocks">The blocks.</param>
		/// <returns>The text.</returns>
		protected string RenderItemBlocks(List<Block> blocks)
		{
			var sb = new StringBuilder();
			Block previous = null;

			foreach (var block in blocks)
			{
				var text = RenderBlock(block);
				if (String.IsNullOrEmpty(text))
					continue;

				if (sb.Length > 0)
					sb.Append(NeedsBlankLine(previous, block) ? "\n\n" : "\n");

				sb.Append(text);
				previous = block;
			}

			return sb.ToString();
		}

		/// <summary>
		/// Determines whether two consecutive blocks of a list item or definition need a blank line to be
		/// read as separate blocks by Markdown.
		/// </summary>
		/// <param name="previous">The previous block.</param>
		/// <param name="next">The next block.</param>
		/// <returns><c>true</c> when a blank line separates the blocks.</returns>
		private static bool NeedsBlankLine(Block previous, Block next)
		{
			if (next is TableBlock || next is CodeBlock || previous is TableBlock || previous is CodeBlock)
				return true;

			// "Text\n---" is a heading; a rule needs blank lines around it.
			if (next is RuleBlock || previous is RuleBlock)
				return true;

			// a numbered list that does not start at 1 cannot interrupt a paragraph.
			if (!(next is ListBlock list) || !list.Ordered || list.Items.Count == 0)
				return false;

			var marker = list.Items[0].Marker ?? "";
			return marker.Length > 0 && Char.IsDigit(marker[0]) && marker != "1." && marker != "1)";
		}

		/// <summary>
		/// Renders the definition of a term.
		/// </summary>
		/// <param name="definition">The definition.</param>
		/// <returns>The text.</returns>
		protected abstract string RenderDefinition(DefinitionBlock definition);

		/// <summary>
		/// Renders a paragraph, heading or definition term.
		/// </summary>
		/// <param name="paragraph">The paragraph.</param>
		/// <returns>The text.</returns>
		protected abstract string RenderParagraph(ParagraphBlock paragraph);

		/// <summary>
		/// Renders a code block.
		/// </summary>
		/// <param name="code">The code block.</param>
		/// <returns>The text.</returns>
		protected abstract string RenderCode(CodeBlock code);

		/// <summary>
		/// Renders a table.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns>The text.</returns>
		protected abstract string RenderTable(TableBlock table);

		/// <summary>
		/// Renders a display equation.
		/// </summary>
		/// <param name="math">The equation.</param>
		/// <returns>The text.</returns>
		protected abstract string RenderMath(MathBlock math);

		#endregion

		#region Helpers

		/// <summary>
		/// Records that an image is rendered on the current page.
		/// </summary>
		/// <param name="image">The image.</param>
		protected void PlaceImage(HtmlImage image)
		{
			if (image.PageNumber == 0)
				image.PageNumber = this.CurrentPage;

			if (!this.PageImages.Contains(image))
				this.PageImages.Add(image);
		}

		/// <summary>
		/// Determines whether a cell has no visible content.
		/// </summary>
		/// <param name="cell">The cell.</param>
		/// <returns><c>true</c> for an empty cell.</returns>
		protected static bool IsEmptyCell(TableCell cell)
		{
			return cell == null || cell.Blocks.Count == 0;
		}

		/// <summary>
		/// Returns the formatting shared by all the text of inlines, which is styling of the whole block
		/// rather than emphasis.
		/// </summary>
		/// <param name="inlines">The inlines.</param>
		/// <returns>The common formatting.</returns>
		protected static InlineStyle CommonStyle(IEnumerable<Inline> inlines)
		{
			var common = (InlineStyle)(-1);
			var any = false;

			foreach (var inline in inlines)
			{
				if (inline is TextInline text && text.Text.Trim().Length > 0)
				{
					common &= text.Style;
					any = true;
				}
			}

			return any ? common : InlineStyle.None;
		}

		/// <summary>
		/// Prefixes every line of a text.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="first">The prefix of the first line.</param>
		/// <param name="others">The prefix of the following lines.</param>
		/// <param name="trimEmpty">Whether trailing spaces of the prefix are removed on empty lines.</param>
		/// <returns>The prefixed text.</returns>
		protected static string PrefixLines(string text, string first, string others, bool trimEmpty)
		{
			var lines = text.Split('\n');
			var sb = new StringBuilder(text.Length + lines.Length * others.Length);

			for (var i = 0; i < lines.Length; i++)
			{
				if (i > 0)
					sb.Append('\n');

				var prefix = i == 0 ? first : others;
				if (trimEmpty && lines[i].Length == 0)
					sb.Append(prefix.TrimEnd());
				else
					sb.Append(prefix).Append(lines[i]);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Collapses line breaks and white space runs into single spaces.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>The text on a single line.</returns>
		protected static string Collapse(string text)
		{
			var sb = new StringBuilder(text.Length);
			var space = false;

			foreach (var c in text)
			{
				if (Char.IsWhiteSpace(c))
				{
					space = sb.Length > 0;
					continue;
				}

				if (space)
					sb.Append(' ');

				sb.Append(c);
				space = false;
			}

			return sb.ToString();
		}

		/// <summary>
		/// Returns the images of a paragraph when the paragraph contains nothing else.
		/// </summary>
		/// <param name="paragraph">The paragraph.</param>
		/// <returns>The images, or <c>null</c> when the paragraph has other content.</returns>
		protected static List<ImageInline> OnlyImages(ParagraphBlock paragraph)
		{
			var images = new List<ImageInline>();

			foreach (var inline in paragraph.Inlines)
			{
				switch (inline)
				{
					case ImageInline image:
						images.Add(image);
						break;

					case TextInline text when text.Text.Trim().Length == 0:
					case BreakInline _:
						break;

					default:
						return null;
				}
			}

			return images.Count > 0 ? images : null;
		}

		/// <summary>
		/// Returns the cells of a table as text rows, for formats without table syntax.
		/// </summary>
		/// <param name="grid">The table grid.</param>
		/// <param name="cellText">Formats the content of a cell.</param>
		/// <returns>The text of every cell, by row.</returns>
		protected static List<string[]> CellTexts(TableGrid grid, Func<TableCell, string> cellText)
		{
			var cache = new Dictionary<TableCell, string>();
			return grid.Rows.Select(row => row.Slots.Select(slot =>
			{
				if (slot == null)
					return "";

				if (!cache.TryGetValue(slot.Cell, out var text))
					cache[slot.Cell] = text = cellText(slot.Cell);

				return text;
			}).ToArray()).ToList();
		}

		#endregion
	}
}

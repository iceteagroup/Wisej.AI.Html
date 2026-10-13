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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Wisej.AI.Html.Content;

namespace Wisej.AI.Html.Rendering
{
	/// <summary>
	/// Renders the content as GitHub flavored Markdown.
	/// </summary>
	/// <remarks>
	/// The output favors what language models read best: ATX headings, nested lists with the original
	/// numbering, pipe tables with merged cells resolved (HTML tables when a table nests other tables),
	/// image references with their alternative text, fenced code blocks with their language and LaTeX
	/// equations. Escaping is limited to the characters that would change the meaning of the Markdown,
	/// so the text stays readable.
	/// </remarks>
	internal sealed class MarkdownRenderer : DocumentRenderer
	{
		/// <summary>Matches line starts that Markdown would read as a heading, a quote, a bullet or a rule.</summary>
		private static readonly Regex BlockMarker = new Regex(@"^(\s*)(#{1,6}(?=\s|$)|>|[+\-*](?=\s|$)|=+\s*$|-{2,}\s*$|```|~~~)", RegexOptions.CultureInvariant);

		/// <summary>Matches line starts that Markdown would read as an ordered list item.</summary>
		private static readonly Regex OrderedMarker = new Regex(@"^(\s*\d{1,9})([.)])(?=\s|$)", RegexOptions.CultureInvariant);

		/// <summary>The emphasis markers, in nesting order.</summary>
		private static readonly InlineStyle[] EmphasisOrder = { InlineStyle.Bold, InlineStyle.Italic, InlineStyle.Strike, InlineStyle.Superscript, InlineStyle.Subscript };

		/// <summary>
		/// Initializes a new instance of the <see cref="MarkdownRenderer"/> class.
		/// </summary>
		/// <param name="content">The content.</param>
		/// <param name="options">The extraction options.</param>
		public MarkdownRenderer(DocumentContent content, HtmlExtractorOptions options)
			: base(content, options)
		{
		}

		/// <inheritdoc/>
		protected override string DefaultPageHeaderFormat => "<!-- Page {0} -->";

		#region Blocks

		/// <inheritdoc/>
		protected override string RenderParagraph(ParagraphBlock paragraph)
		{
			var images = OnlyImages(paragraph);
			if (images != null)
				return RenderImages(images);

			if (paragraph.HeadingLevel > 0)
			{
				var heading = Collapse(FormatInlines(paragraph.Inlines, InlineMode.Line, CommonStyle(paragraph.Inlines) & (InlineStyle.Bold | InlineStyle.Italic)));
				return heading.Length == 0 ? "" : new string('#', paragraph.HeadingLevel) + " " + EscapeClosingSequence(heading);
			}

			if (paragraph.IsTerm)
			{
				var term = Collapse(FormatInlines(paragraph.Inlines, InlineMode.Line, InlineStyle.Bold));
				return term.Length == 0 ? "" : "**" + term + "**";
			}

			return EscapeLineStarts(FormatInlines(paragraph.Inlines, InlineMode.Block, InlineStyle.None).Trim());
		}

		/// <summary>
		/// Renders a paragraph made only of images, one per line, each followed by its description.
		/// </summary>
		/// <param name="images">The images.</param>
		/// <returns>The text.</returns>
		private string RenderImages(List<ImageInline> images)
		{
			var sb = new StringBuilder();

			foreach (var inline in images)
			{
				if (sb.Length > 0)
					sb.Append('\n');

				var reference = FormatImage(inline.Image);
				sb.Append(inline.Link != null ? "[" + reference + "](" + FormatUrl(inline.Link) + ")" : reference);

				if (inline.Image.Description != null)
					sb.Append('\n').Append(PrefixLines("**" + inline.Image.FileName + ":** " + inline.Image.Description.Trim(), "> ", "> ", trimEmpty: true));
			}

			return sb.ToString();
		}

		/// <inheritdoc/>
		protected override string RenderDefinition(DefinitionBlock definition)
		{
			var content = RenderItemBlocks(definition.Blocks);
			return content.Length == 0 ? "" : PrefixLines(content, ": ", "  ", trimEmpty: true);
		}

		/// <inheritdoc/>
		protected override string RenderCode(CodeBlock code)
		{
			var fence = new string('`', Math.Max(3, LongestRun(code.Text, '`') + 1));
			return fence + (code.Language ?? "") + "\n" + code.Text + "\n" + fence;
		}

		/// <inheritdoc/>
		protected override string RenderMath(MathBlock math)
		{
			return "$$" + math.Latex + "$$";
		}

		#endregion

		#region Tables

		/// <inheritdoc/>
		protected override string RenderTable(TableBlock table)
		{
			var caption = RenderItemBlocks(table.Caption);
			var grid = new TableGrid(table, IsEmptyCell);
			grid.RemoveDuplicateColumns(cell => FormatCell(cell, InlineMode.Cell));

			if (grid.Rows.Count == 0 || grid.ColumnCount == 0)
				return caption;

			var html = this.Options.Tables == TableFormat.Html || (this.Options.Tables == TableFormat.Auto && HasNestedTable(table));
			if (html)
				return FormatHtmlTable(grid, Collapse(caption));

			var body = FormatPipeTable(grid);
			return caption.Length == 0 ? body : caption + "\n\n" + body;
		}

		/// <summary>
		/// Formats a table as a pipe table. Multiple header rows are combined into one, and a table without
		/// a header uses its first row.
		/// </summary>
		/// <param name="grid">The table grid.</param>
		/// <returns>The pipe table.</returns>
		private string FormatPipeTable(TableGrid grid)
		{
			var texts = CellTexts(grid, cell => FormatCell(cell, InlineMode.Cell));
			var headerCount = Math.Max(1, grid.HeaderRowCount);
			var columns = grid.ColumnCount;
			var sb = new StringBuilder();

			// a group heading spanning columns above the column names: "Group / Name".
			var names = new string[columns];
			for (var c = 0; c < columns; c++)
			{
				var parts = new List<string>();
				for (var r = 0; r < headerCount; r++)
				{
					var text = texts[r][c];
					if (text.Length > 0 && !parts.Contains(text))
						parts.Add(text);
				}

				names[c] = String.Join(" / ", parts);
			}

			sb.Append("| ").Append(String.Join(" | ", names)).Append(" |\n");
			sb.Append('|').Append(String.Concat(Enumerable.Repeat(" --- |", columns)));

			for (var r = headerCount; r < grid.Rows.Count; r++)
			{
				var slots = grid.Rows[r].Slots;
				sb.Append("\n|");

				for (var c = 0; c < columns; c++)
				{
					var slot = slots[c];
					var text = slot == null || (!slot.IsOrigin && this.Options.MergedCells == MergedCellMode.Empty) ? "" : texts[r][c];
					sb.Append(' ').Append(text).Append(" |");
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Formats a table as an HTML table, with spans for merged cells and nested tables.
		/// </summary>
		/// <param name="grid">The table grid.</param>
		/// <param name="caption">The caption, possibly empty.</param>
		/// <returns>The HTML table.</returns>
		private string FormatHtmlTable(TableGrid grid, string caption)
		{
			var sb = new StringBuilder("<table>");
			if (caption.Length > 0)
				sb.Append("\n<caption>").Append(caption).Append("</caption>");

			for (var r = 0; r < grid.Rows.Count; r++)
			{
				var slots = grid.Rows[r].Slots;
				sb.Append("\n<tr>");

				for (var c = 0; c < slots.Length; c++)
				{
					var slot = slots[c];
					if (slot != null && !slot.IsOrigin)
						continue;

					var columnSpan = 1;
					while (slot != null && c + columnSpan < slots.Length && slots[c + columnSpan]?.Cell == slot.Cell && slots[c + columnSpan].IsHorizontalContinuation)
						columnSpan++;

					var rowSpan = 1;
					while (slot != null && r + rowSpan < grid.Rows.Count && grid.Rows[r + rowSpan].Slots[c]?.Cell == slot.Cell)
						rowSpan++;

					var tag = r < grid.HeaderRowCount || (slot?.Cell.IsHeader ?? false) ? "th" : "td";
					sb.Append('<').Append(tag);
					if (columnSpan > 1)
						sb.Append(" colspan=\"").Append(columnSpan).Append('"');
					if (rowSpan > 1)
						sb.Append(" rowspan=\"").Append(rowSpan).Append('"');
					sb.Append('>');

					if (slot != null)
						sb.Append(FormatHtmlCell(slot.Cell));

					sb.Append("</").Append(tag).Append('>');
					c += columnSpan - 1;
				}

				sb.Append("</tr>");
			}

			sb.Append("\n</table>");
			return sb.ToString();
		}

		/// <summary>
		/// Formats the content of a cell of an HTML table, with nested tables as HTML tables.
		/// </summary>
		/// <param name="cell">The cell.</param>
		/// <returns>The cell content.</returns>
		private string FormatHtmlCell(TableCell cell)
		{
			var parts = new List<string>();

			foreach (var block in cell.Blocks)
			{
				if (block is TableBlock nested)
				{
					var grid = new TableGrid(nested, IsEmptyCell);
					if (grid.Rows.Count > 0)
						parts.Add(FormatHtmlTable(grid, Collapse(RenderItemBlocks(nested.Caption))));
				}
				else
				{
					var text = FormatBlockInCell(block, InlineMode.HtmlCell, CellStyle(cell));
					if (text.Length > 0)
						parts.Add(text);
				}
			}

			return String.Join("<br>", parts);
		}

		/// <summary>
		/// Formats the content of a cell of a pipe table on a single line.
		/// </summary>
		/// <param name="cell">The cell.</param>
		/// <param name="mode">Where the content is written.</param>
		/// <returns>The cell content.</returns>
		private string FormatCell(TableCell cell, InlineMode mode)
		{
			var drop = CellStyle(cell);
			var parts = cell.Blocks.Select(b => FormatBlockInCell(b, mode, drop)).Where(t => t.Length > 0);
			return String.Join("<br>", parts);
		}

		/// <summary>
		/// Formats a block of a cell on a single line.
		/// </summary>
		/// <param name="block">The block.</param>
		/// <param name="mode">Where the content is written.</param>
		/// <param name="drop">The formatting dropped because it covers the whole cell.</param>
		/// <returns>The text.</returns>
		private string FormatBlockInCell(Block block, InlineMode mode, InlineStyle drop)
		{
			switch (block)
			{
				case ParagraphBlock paragraph:
					return FormatInlines(paragraph.Inlines, mode, drop).Trim();

				case ListBlock list:
					return String.Join("<br>", list.Items.Select(item =>
					{
						var content = String.Join(" ", item.Blocks.Select(b => FormatBlockInCell(b, mode, drop)).Where(t => t.Length > 0));
						return item.Marker == null ? content : (item.Marker == "-" ? "• " : item.Marker + " ") + content;
					}));

				case CodeBlock code:
					return String.Join("<br>", code.Text.Split('\n').Select(line => line.Trim().Length == 0 ? "" : CodeSpan(line.TrimEnd(), mode)));

				case QuoteBlock quote:
					return String.Join("<br>", quote.Blocks.Select(b => FormatBlockInCell(b, mode, drop)).Where(t => t.Length > 0));

				case DefinitionBlock definition:
					return String.Join("<br>", definition.Blocks.Select(b => FormatBlockInCell(b, mode, drop)).Where(t => t.Length > 0));

				case MathBlock math:
					return "$" + math.Latex + "$";

				case TableBlock nested:
					// a pipe table cannot nest tables: each row of the nested table becomes a line.
					var grid = new TableGrid(nested, IsEmptyCell);
					var texts = CellTexts(grid, c => FormatCell(c, mode));
					return String.Join("<br>", texts.Select(row => String.Join(" / ", row.Where(t => t.Length > 0))).Where(t => t.Length > 0));

				default:
					return "";
			}
		}

		/// <summary>
		/// Returns the formatting that covers the whole content of a cell, which is styling (bold first
		/// columns, header cells) rather than emphasis.
		/// </summary>
		/// <param name="cell">The cell.</param>
		/// <returns>The formatting to drop.</returns>
		private static InlineStyle CellStyle(TableCell cell)
		{
			var inlines = cell.Blocks.OfType<ParagraphBlock>().SelectMany(p => p.Inlines);
			return CommonStyle(inlines) & (InlineStyle.Bold | InlineStyle.Italic);
		}

		/// <summary>
		/// Determines whether a table contains another table.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns><c>true</c> when a cell contains a table.</returns>
		private static bool HasNestedTable(TableBlock table)
		{
			return table.Rows.Any(r => r.Cells.Any(c => c.Blocks.Any(b => b is TableBlock)));
		}

		#endregion

		#region Inlines

		/// <summary>
		/// Formats inline content, grouping the runs that share a link.
		/// </summary>
		/// <param name="inlines">The inlines.</param>
		/// <param name="mode">Where the content is written.</param>
		/// <param name="drop">The formatting that is not written, because it covers the whole block.</param>
		/// <returns>The Markdown text.</returns>
		private string FormatInlines(List<Inline> inlines, InlineMode mode, InlineStyle drop)
		{
			var sb = new StringBuilder();
			var index = 0;

			while (index < inlines.Count)
			{
				var link = inlines[index].Link;
				var end = index;
				while (end < inlines.Count && inlines[end].Link == link)
					end++;

				var text = FormatEmphasis(inlines, index, end, mode, drop, link != null);

				if (link != null && text.Trim().Length > 0)
				{
					var lead = text.Substring(0, text.Length - text.TrimStart().Length);
					var trail = text.Substring(text.TrimEnd().Length);
					var raw = String.Concat(inlines.Skip(index).Take(end - index).OfType<TextInline>().Select(t => t.Text)).Trim();

					sb.Append(lead);

					// a link whose text is its address is written as the address.
					if (raw == link || "mailto:" + raw == link || (raw.Length > 0 && raw + "/" == link))
					{
						sb.Append(link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ? raw : link);
					}
					else
					{
						// "Wow![link](url)" would be an image.
						if (lead.Length == 0 && sb.Length > 0 && sb[sb.Length - 1] == '!' && (sb.Length == 1 || sb[sb.Length - 2] != '\\'))
							sb.Insert(sb.Length - 1, '\\');

						sb.Append('[').Append(text.Trim()).Append("](").Append(FormatUrl(link)).Append(')');
					}

					sb.Append(trail);
				}
				else
				{
					sb.Append(text);
				}

				index = end;
			}

			return sb.ToString();
		}

		/// <summary>
		/// Formats a range of inlines with emphasis markers, nesting them correctly and keeping white space
		/// outside of the markers.
		/// </summary>
		/// <param name="inlines">The inlines.</param>
		/// <param name="start">The first inline of the range.</param>
		/// <param name="end">The end of the range, exclusive.</param>
		/// <param name="mode">Where the content is written.</param>
		/// <param name="drop">The formatting that is not written.</param>
		/// <param name="inLink">Whether the inlines are the text of a link, where brackets are escaped.</param>
		/// <returns>The formatted text.</returns>
		private string FormatEmphasis(List<Inline> inlines, int start, int end, InlineMode mode, InlineStyle drop, bool inLink)
		{
			var sb = new StringBuilder();
			var open = new List<InlineStyle>();
			var pendingSpace = "";

			for (var i = start; i < end; i++)
			{
				string markup = null;

				switch (inlines[i])
				{
					case BreakInline _:
						markup = mode == InlineMode.Block ? "\n" : mode == InlineMode.Line ? " " : "<br>";
						pendingSpace = "";
						break;

					case ImageInline image:
						markup = FormatImage(image.Image);
						if (image.Image.Description != null)
							markup += " (" + Collapse(image.Image.Description) + ")";
						break;

					case MathInline math:
						markup = "$" + math.Latex + "$";
						break;
				}

				if (markup != null)
				{
					CloseEmphasis(sb, open, 0);
					sb.Append(pendingSpace).Append(markup);
					pendingSpace = "";
					continue;
				}

				var run = (TextInline)inlines[i];
				var style = this.Options.IncludeFormatting ? run.Style & ~drop : InlineStyle.None;
				var text = style.HasFlag(InlineStyle.Code) ? CodeSpan(run.Text, mode) : Escape(run.Text, mode, inLink);
				var core = text.Trim();

				if (core.Length == 0)
				{
					pendingSpace += text;
					continue;
				}

				var lead = text.Substring(0, text.Length - text.TrimStart().Length);
				var trail = text.Substring(text.TrimEnd().Length);

				// close the markers that are not wanted, together with the markers nested inside them.
				var firstUnwanted = open.FindIndex(e => !style.HasFlag(e));
				if (firstUnwanted >= 0)
					CloseEmphasis(sb, open, firstUnwanted);

				sb.Append(pendingSpace).Append(lead);

				foreach (var emphasis in EmphasisOrder)
				{
					if (style.HasFlag(emphasis) && !open.Contains(emphasis))
					{
						sb.Append(OpenMarker(emphasis));
						open.Add(emphasis);
					}
				}

				sb.Append(core);
				pendingSpace = trail;
			}

			CloseEmphasis(sb, open, 0);
			sb.Append(pendingSpace);

			return sb.ToString();
		}

		/// <summary>
		/// Closes the open emphasis markers from the innermost down to <paramref name="index"/>.
		/// </summary>
		/// <param name="sb">The output.</param>
		/// <param name="open">The open markers, outermost first.</param>
		/// <param name="index">The index of the outermost marker to close.</param>
		private static void CloseEmphasis(StringBuilder sb, List<InlineStyle> open, int index)
		{
			for (var i = open.Count - 1; i >= index; i--)
			{
				sb.Append(CloseMarker(open[i]));
				open.RemoveAt(i);
			}
		}

		/// <summary>
		/// Returns the opening marker of an emphasis.
		/// </summary>
		/// <param name="emphasis">The emphasis.</param>
		/// <returns>The marker.</returns>
		private static string OpenMarker(InlineStyle emphasis)
		{
			switch (emphasis)
			{
				case InlineStyle.Bold:
					return "**";

				case InlineStyle.Italic:
					return "*";

				case InlineStyle.Strike:
					return "~~";

				case InlineStyle.Superscript:
					return "<sup>";

				default:
					return "<sub>";
			}
		}

		/// <summary>
		/// Returns the closing marker of an emphasis.
		/// </summary>
		/// <param name="emphasis">The emphasis.</param>
		/// <returns>The marker.</returns>
		private static string CloseMarker(InlineStyle emphasis)
		{
			switch (emphasis)
			{
				case InlineStyle.Superscript:
					return "</sup>";

				case InlineStyle.Subscript:
					return "</sub>";

				default:
					return OpenMarker(emphasis);
			}
		}

		/// <summary>
		/// Formats an image reference: <c>![alt](image1.png)</c>.
		/// </summary>
		/// <param name="image">The image.</param>
		/// <returns>The reference.</returns>
		private string FormatImage(HtmlImage image)
		{
			PlaceImage(image);

			var alt = (image.AltText ?? image.Title ?? image.Caption ?? "").Replace('\n', ' ').Replace("[", "(").Replace("]", ")");
			return "![" + Collapse(alt) + "](" + FormatUrl(image.Reference) + ")";
		}

		#endregion

		#region Escaping

		/// <summary>
		/// Escapes the characters that Markdown would interpret.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="mode">Where the text is written.</param>
		/// <param name="inLink">Whether the text is the text of a link, where brackets are always escaped.</param>
		/// <returns>The escaped text.</returns>
		private static string Escape(string text, InlineMode mode, bool inLink = false)
		{
			var sb = new StringBuilder(text.Length + 8);

			// computed once, so that escaping stays linear.
			var lastLinkSyntax = text.LastIndexOf("](", StringComparison.Ordinal);
			var pairedTildes = text.IndexOf('~') != text.LastIndexOf('~');

			for (var i = 0; i < text.Length; i++)
			{
				var c = text[i];
				var previous = i > 0 ? text[i - 1] : ' ';
				var next = i + 1 < text.Length ? text[i + 1] : ' ';

				switch (c)
				{
					case '\\':
					case '*':
					case '`':
						sb.Append('\\').Append(c);
						break;

					case '_':
						// an underscore inside a word (snake_case) cannot start emphasis.
						if (Char.IsLetterOrDigit(previous) && Char.IsLetterOrDigit(next))
							sb.Append(c);
						else
							sb.Append("\\_");
						break;

					case '~':
						// GitHub reads "~x~" as strikethrough.
						sb.Append(pairedTildes ? "\\~" : "~");
						break;

					case '<':
						sb.Append(Char.IsLetter(next) || next == '/' || next == '!' || next == '?' ? "&lt;" : "<");
						break;

					case '&':
						// "&amp;" written in the text must not be read as a character reference.
						sb.Append(IsReferenceLike(text, i) ? "&amp;" : "&");
						break;

					case '[':
						// "[text](url)" written in the text must not become a link; link text must stay balanced.
						sb.Append(inLink || i < lastLinkSyntax ? "\\[" : "[");
						break;

					case ']':
						sb.Append(inLink ? "\\]" : "]");
						break;

					case '|':
						sb.Append(mode == InlineMode.Cell ? "\\|" : "|");
						break;

					case '\n':
						sb.Append(mode == InlineMode.Block ? "\n" : mode == InlineMode.Line ? " " : "<br>");
						break;

					default:
						sb.Append(c);
						break;
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Determines whether an ampersand starts something Markdown reads as a character reference.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="index">The position of the ampersand.</param>
		/// <returns><c>true</c> for <c>&amp;name;</c> and <c>&amp;#123;</c>.</returns>
		private static bool IsReferenceLike(string text, int index)
		{
			var i = index + 1;
			if (i < text.Length && text[i] == '#')
				i++;

			var start = i;
			while (i < text.Length && i - start < 32 && Char.IsLetterOrDigit(text[i]))
				i++;

			return i > start && i < text.Length && text[i] == ';';
		}

		/// <summary>
		/// Escapes the start of every line that Markdown would read as a heading, a quote, a list item, a
		/// rule or a code fence.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>The escaped text.</returns>
		private static string EscapeLineStarts(string text)
		{
			var lines = text.Split('\n');
			for (var i = 0; i < lines.Length; i++)
			{
				lines[i] = BlockMarker.Replace(lines[i], @"$1\$2", 1);
				lines[i] = OrderedMarker.Replace(lines[i], @"$1\$2", 1);
			}

			return String.Join("\n", lines);
		}

		/// <summary>
		/// Escapes a run of <c>#</c> at the end of a heading, which Markdown would remove as the optional
		/// closing sequence ("Section #" would become "Section").
		/// </summary>
		/// <param name="heading">The text of the heading.</param>
		/// <returns>The escaped text.</returns>
		private static string EscapeClosingSequence(string heading)
		{
			var start = heading.Length;
			while (start > 0 && heading[start - 1] == '#')
				start--;

			if (start == heading.Length || (start > 0 && heading[start - 1] != ' '))
				return heading;

			return heading.Substring(0, start) + "\\" + heading.Substring(start);
		}

		/// <summary>
		/// Formats a code span, choosing a fence longer than any backtick run in the text.
		/// </summary>
		/// <param name="text">The code.</param>
		/// <param name="mode">Where the code is written.</param>
		/// <returns>The code span.</returns>
		private static string CodeSpan(string text, InlineMode mode)
		{
			var core = text.Trim();
			if (core.Length == 0)
				return text;

			if (mode == InlineMode.Cell)
				core = core.Replace("|", "\\|");

			core = core.Replace("\n", mode == InlineMode.Block || mode == InlineMode.Line ? " " : "<br>");

			var fence = new string('`', LongestRun(core, '`') + 1);
			var padding = core.StartsWith("`", StringComparison.Ordinal) || core.EndsWith("`", StringComparison.Ordinal) ? " " : "";
			var lead = text.Substring(0, text.Length - text.TrimStart().Length);
			var trail = text.Substring(text.TrimEnd().Length);

			return lead + fence + padding + core + padding + fence + trail;
		}

		/// <summary>
		/// Formats the target of a link or image, encoding the characters that would end it.
		/// </summary>
		/// <param name="url">The target.</param>
		/// <returns>The formatted target.</returns>
		private static string FormatUrl(string url)
		{
			return url.Trim().Replace(" ", "%20").Replace("(", "%28").Replace(")", "%29").Replace("<", "%3C").Replace(">", "%3E");
		}

		/// <summary>
		/// Returns the length of the longest run of a character.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="c">The character.</param>
		/// <returns>The length of the longest run.</returns>
		private static int LongestRun(string text, char c)
		{
			var longest = 0;
			var run = 0;

			foreach (var ch in text)
			{
				run = ch == c ? run + 1 : 0;
				longest = Math.Max(longest, run);
			}

			return longest;
		}

		#endregion
	}
}

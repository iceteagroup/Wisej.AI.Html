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
using Wisej.AI.Html.Content;

namespace Wisej.AI.Html.Rendering
{
	/// <summary>
	/// Renders the content as plain text: no markup, lists keep their labels, tables are rows of
	/// pipe-separated cells and images are written as <c>[Image: alternative text]</c>.
	/// </summary>
	internal sealed class PlainTextRenderer : DocumentRenderer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PlainTextRenderer"/> class.
		/// </summary>
		/// <param name="content">The content.</param>
		/// <param name="options">The extraction options.</param>
		public PlainTextRenderer(DocumentContent content, HtmlExtractorOptions options)
			: base(content, options)
		{
		}

		/// <inheritdoc/>
		protected override string DefaultPageHeaderFormat => "--- Page {0} ---";

		/// <inheritdoc/>
		protected override string RenderParagraph(ParagraphBlock paragraph)
		{
			var mode = paragraph.HeadingLevel > 0 || paragraph.IsTerm ? InlineMode.Line : InlineMode.Block;
			var text = FormatInlines(paragraph.Inlines, mode).Trim();

			return mode == InlineMode.Line ? Collapse(text) : text;
		}

		/// <inheritdoc/>
		protected override string RenderDefinition(DefinitionBlock definition)
		{
			var content = RenderItemBlocks(definition.Blocks);
			return content.Length == 0 ? "" : PrefixLines(content, "  ", "  ", trimEmpty: true);
		}

		/// <inheritdoc/>
		protected override string RenderCode(CodeBlock code)
		{
			return code.Text;
		}

		/// <inheritdoc/>
		protected override string RenderMath(MathBlock math)
		{
			return math.Latex;
		}

		/// <inheritdoc/>
		protected override string RenderTable(TableBlock table)
		{
			var caption = RenderItemBlocks(table.Caption);
			var grid = new TableGrid(table, IsEmptyCell);
			grid.RemoveDuplicateColumns(FormatCell);
			var texts = CellTexts(grid, FormatCell);

			var sb = new StringBuilder(caption);
			for (var r = 0; r < grid.Rows.Count; r++)
			{
				var slots = grid.Rows[r].Slots;
				var cells = new List<string>();

				for (var c = 0; c < slots.Length; c++)
				{
					var slot = slots[c];
					cells.Add(slot == null || (!slot.IsOrigin && this.Options.MergedCells == MergedCellMode.Empty) ? "" : texts[r][c]);
				}

				if (sb.Length > 0)
					sb.Append('\n');

				sb.Append(String.Join(" | ", cells));
			}

			return sb.ToString();
		}

		/// <summary>
		/// Formats the content of a cell on a single line.
		/// </summary>
		/// <param name="cell">The cell.</param>
		/// <returns>The cell text.</returns>
		private string FormatCell(TableCell cell)
		{
			var parts = new List<string>();

			foreach (var block in cell.Blocks)
			{
				switch (block)
				{
					case ParagraphBlock paragraph:
						parts.Add(Collapse(FormatInlines(paragraph.Inlines, InlineMode.Line)));
						break;

					case TableBlock nested:
						var grid = new TableGrid(nested, IsEmptyCell);
						parts.AddRange(CellTexts(grid, FormatCell).Select(row => String.Join(" / ", row.Where(t => t.Length > 0))));
						break;

					default:
						parts.Add(Collapse(RenderBlock(block)));
						break;
				}
			}

			return String.Join(" ", parts.Where(p => p.Length > 0)).Replace("|", "/");
		}

		/// <summary>
		/// Formats inline content as plain text.
		/// </summary>
		/// <param name="inlines">The inlines.</param>
		/// <param name="mode">Where the content is written.</param>
		/// <returns>The text.</returns>
		private string FormatInlines(List<Inline> inlines, InlineMode mode)
		{
			var sb = new StringBuilder();

			foreach (var inline in inlines)
			{
				switch (inline)
				{
					case TextInline text:
						sb.Append(text.Text);
						break;

					case BreakInline _:
						sb.Append(mode == InlineMode.Block ? "\n" : " ");
						break;

					case MathInline math:
						sb.Append(math.Latex);
						break;

					case ImageInline image:
						PlaceImage(image.Image);

						var alt = image.Image.AltText ?? image.Image.Title ?? image.Image.Caption ?? image.Image.FileName;
						sb.Append("[Image: ").Append(Collapse(alt)).Append(']');

						if (image.Image.Description != null)
							sb.Append(" (").Append(Collapse(image.Image.Description)).Append(')');
						break;
				}
			}

			return sb.ToString();
		}
	}
}

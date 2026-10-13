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
using Wisej.AI.Html.Dom;

namespace Wisej.AI.Html.Content
{
	/// <summary>
	/// The rules of tables: row groups, cells with their spans, captions, and the detection of the
	/// tables used for page layout, which are written as regular content.
	/// </summary>
	internal sealed partial class ContentBuilder
	{
		/// <summary>The maximum number of columns a cell can span (HTML standard, 4.9.11).</summary>
		private const int MaxColumnSpan = 1000;

		/// <summary>The maximum number of rows a cell can span (HTML standard, 4.9.11).</summary>
		private const int MaxRowSpan = 65534;

		/// <summary>
		/// A row of the table with the group it belongs to.
		/// </summary>
		private sealed class SourceRow
		{
			/// <summary>The row element.</summary>
			public HtmlElement Element;

			/// <summary>The computed style of the row.</summary>
			public ComputedStyle Style;

			/// <summary>The index of the row group.</summary>
			public int Group;

			/// <summary>Whether the row belongs to the table header.</summary>
			public bool IsHeader;
		}

		/// <summary>
		/// Adds a table, or its content when it is a layout table.
		/// </summary>
		/// <param name="element">The table element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void Table(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			container.FlushParagraph();

			var rows = CollectRows(element, style, out var caption);

			if (_options.UnwrapLayoutTables && IsLayoutTable(element, rows, caption))
			{
				UnwrapTable(rows, caption, container, link);
				return;
			}

			var table = new TableBlock();

			if (caption != null)
			{
				var captionStyle = ComputeStyle(caption, style);
				var inner = new BlockContainer(table.Caption, allowPageBreaks: false);
				WalkChildren(caption, inner, captionStyle, link);
				inner.FlushParagraph();
			}

			foreach (var source in rows)
			{
				var row = new TableRow { IsHeader = source.IsHeader };

				foreach (var cellElement in source.Element.Children)
				{
					if (!cellElement.Is("td") && !cellElement.Is("th"))
						continue;

					var cellStyle = ComputeStyle(cellElement, source.Style);
					if (!IsRendered(cellElement, cellStyle))
						continue;

					var cell = new TableCell
					{
						IsHeader = cellElement.Is("th"),
						ColumnSpan = Clamp(ParseInt(cellElement.GetAttribute("colspan"), 1), 1, MaxColumnSpan),
						RowSpan = Clamp(ParseInt(cellElement.GetAttribute("rowspan"), 1), 0, MaxRowSpan),
						Group = source.Group,
					};

					var inner = new BlockContainer(cell.Blocks, allowPageBreaks: false);

					_depth++;
					WalkChildren(cellElement, inner, cellStyle, link);
					_depth--;

					inner.FlushParagraph();
					row.Cells.Add(cell);
				}

				if (row.Cells.Count > 0)
					table.Rows.Add(row);
			}

			if (table.Rows.Count > 0)
			{
				container.Add(table);
				return;
			}

			// a table without rows keeps its caption.
			foreach (var block in table.Caption)
				container.Add(block);
		}

		/// <summary>
		/// Collects the rows of a table: header groups first, then body rows, then footer groups, as browsers lay them out.
		/// </summary>
		/// <param name="table">The table element.</param>
		/// <param name="style">The computed style of the table.</param>
		/// <param name="caption">Receives the caption element, or <c>null</c>.</param>
		/// <returns>The rendered rows.</returns>
		private List<SourceRow> CollectRows(HtmlElement table, ComputedStyle style, out HtmlElement caption)
		{
			var header = new List<SourceRow>();
			var body = new List<SourceRow>();
			var footer = new List<SourceRow>();
			var group = 0;
			var implicitGroup = -1;
			caption = null;

			foreach (var child in table.Children)
			{
				if (!child.IsHtml)
					continue;

				var childStyle = ComputeStyle(child, style);
				if (!IsRendered(child, childStyle))
					continue;

				switch (child.LocalName)
				{
					case "caption":
						caption = caption ?? child;
						break;

					case "thead":
						AddRows(child, childStyle, ++group, header, isHeader: true);
						implicitGroup = -1;
						break;

					case "tbody":
						AddRows(child, childStyle, ++group, body, isHeader: false);
						implicitGroup = -1;
						break;

					case "tfoot":
						AddRows(child, childStyle, ++group, footer, isHeader: false);
						implicitGroup = -1;
						break;

					case "tr":
						// consecutive rows outside of a section form one group.
						if (implicitGroup < 0)
							implicitGroup = ++group;

						body.Add(new SourceRow { Element = child, Style = childStyle, Group = implicitGroup });
						break;
				}
			}

			header.AddRange(body);
			header.AddRange(footer);
			return header;
		}

		/// <summary>
		/// Adds the rendered rows of a row group.
		/// </summary>
		/// <param name="section">The row group element.</param>
		/// <param name="style">The computed style of the group.</param>
		/// <param name="group">The index of the group.</param>
		/// <param name="rows">Receives the rows.</param>
		/// <param name="isHeader">Whether the group is the table header.</param>
		private void AddRows(HtmlElement section, ComputedStyle style, int group, List<SourceRow> rows, bool isHeader)
		{
			foreach (var row in section.Children)
			{
				if (!row.Is("tr"))
					continue;

				var rowStyle = ComputeStyle(row, style);
				if (IsRendered(row, rowStyle))
					rows.Add(new SourceRow { Element = row, Style = rowStyle, Group = group, IsHeader = isHeader });
			}
		}

		/// <summary>
		/// Determines whether a table is used for page layout rather than to present data, following the
		/// heuristics of browsers' accessibility layers.
		/// </summary>
		/// <param name="table">The table element.</param>
		/// <param name="rows">The rendered rows.</param>
		/// <param name="caption">The caption element, or <c>null</c>.</param>
		/// <returns><c>true</c> for a layout table.</returns>
		private static bool IsLayoutTable(HtmlElement table, List<SourceRow> rows, HtmlElement caption)
		{
			switch (Role(table))
			{
				case "presentation":
				case "none":
					return true;

				case "table":
				case "grid":
				case "treegrid":
					return false;
			}

			// explicit data table structure.
			if (caption != null || rows.Any(r => r.IsHeader) || table.HasAttribute("summary"))
				return false;

			if (table.Children.Any(c => c.Is("colgroup") || c.Is("col") || c.Is("tfoot")))
				return false;

			foreach (var row in rows)
			{
				foreach (var cell in row.Element.Children)
				{
					if (cell.Is("th") || cell.HasAttribute("headers") || cell.HasAttribute("scope") || cell.HasAttribute("abbr"))
						return false;
				}
			}

			// a table without header cells that contains tables arranges them.
			var columns = 0;
			foreach (var row in rows)
			{
				var count = 0;
				foreach (var cell in row.Element.Children)
				{
					if (cell.DescendantElements().Any(e => e.Is("table")))
						return true;

					if (cell.Is("td"))
						count += Clamp(ParseInt(cell.GetAttribute("colspan"), 1), 1, MaxColumnSpan);
				}

				columns = Math.Max(columns, count);
			}

			// a single row or column arranges content, it does not tabulate it.
			return rows.Count <= 1 || columns <= 1;
		}

		/// <summary>
		/// Writes the content of a layout table as regular content, cell after cell.
		/// </summary>
		/// <param name="rows">The rendered rows.</param>
		/// <param name="caption">The caption element, or <c>null</c>.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void UnwrapTable(List<SourceRow> rows, HtmlElement caption, BlockContainer container, string link)
		{
			if (caption != null)
				Walk(caption, container, rows.Count > 0 ? rows[0].Style : ComputedStyle.Root, link);

			foreach (var row in rows)
			{
				foreach (var cell in row.Element.Children)
				{
					if (!cell.Is("td") && !cell.Is("th"))
						continue;

					var cellStyle = ComputeStyle(cell, row.Style);
					if (!IsRendered(cell, cellStyle))
						continue;

					container.FlushParagraph();

					_depth++;
					WalkChildren(cell, container, cellStyle, link);
					_depth--;

					container.FlushParagraph();
				}
			}
		}

		/// <summary>
		/// Clamps a value to a range.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="min">The minimum.</param>
		/// <param name="max">The maximum.</param>
		/// <returns>The clamped value.</returns>
		private static int Clamp(int value, int min, int max)
		{
			return value < min ? min : value > max ? max : value;
		}
	}
}

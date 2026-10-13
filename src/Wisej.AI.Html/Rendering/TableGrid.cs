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
using Wisej.AI.Html.Content;

namespace Wisej.AI.Html.Rendering
{
	/// <summary>
	/// The grid of a table: every row has one slot per column, and merged cells occupy several slots,
	/// following the table processing model of the HTML standard (4.9.12).
	/// </summary>
	internal sealed class TableGrid
	{
		/// <summary>The maximum number of columns of the grid.</summary>
		private const int MaxColumns = 1000;

		/// <summary>The maximum number of slots of the grid, which bounds the memory used by hostile spans.</summary>
		private const int MaxSlots = 1000000;

		/// <summary>
		/// A slot of the grid.
		/// </summary>
		internal sealed class Slot
		{
			/// <summary>Gets or sets the cell that covers the slot.</summary>
			public TableCell Cell { get; set; }

			/// <summary>Gets or sets whether the slot is the top left corner of the cell.</summary>
			public bool IsOrigin { get; set; }

			/// <summary>Gets or sets whether the slot continues a cell from the column on its left.</summary>
			public bool IsHorizontalContinuation { get; set; }

			/// <summary>Gets or sets whether the slot continues a cell from the row above.</summary>
			public bool IsVerticalContinuation { get; set; }
		}

		/// <summary>
		/// A row of the grid.
		/// </summary>
		internal sealed class GridRow
		{
			/// <summary>Gets or sets the slots, one per column; empty slots are <c>null</c>.</summary>
			public Slot[] Slots { get; set; }

			/// <summary>Gets or sets whether the row belongs to the header.</summary>
			public bool IsHeader { get; set; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TableGrid"/> class.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="isEmpty">Determines whether a cell has no visible content.</param>
		public TableGrid(TableBlock table, Func<TableCell, bool> isEmpty)
		{
			var rows = Build(table);
			RemoveEmpty(rows, isEmpty);

			this.Rows = rows;
			this.ColumnCount = rows.Count == 0 ? 0 : rows[0].Slots.Length;
			this.HeaderRowCount = CountHeaderRows(rows);
		}

		/// <summary>
		/// Gets the rows.
		/// </summary>
		public List<GridRow> Rows { get; }

		/// <summary>
		/// Gets the number of columns.
		/// </summary>
		public int ColumnCount { get; private set; }

		/// <summary>
		/// Gets the number of leading rows that form the header: the rows of <c>&lt;thead&gt;</c>, or else the
		/// leading rows made of header cells only.
		/// </summary>
		public int HeaderRowCount { get; }

		/// <summary>
		/// Removes the columns that repeat the column on their left in every row, such as the copies that
		/// responsive pages render for small screens and hide with style sheets.
		/// </summary>
		/// <param name="text">Returns the text of a cell.</param>
		public void RemoveDuplicateColumns(Func<TableCell, string> text)
		{
			if (this.Rows.Count == 0 || this.ColumnCount < 2)
				return;

			var cache = new Dictionary<TableCell, string>();
			string Text(Slot slot)
			{
				if (slot == null)
					return "";

				if (!cache.TryGetValue(slot.Cell, out var value))
					cache[slot.Cell] = value = text(slot.Cell);

				return value;
			}

			var keep = new List<int> { 0 };
			for (var c = 1; c < this.ColumnCount; c++)
			{
				var previous = keep[keep.Count - 1];
				var duplicate = this.Rows.All(r => Text(r.Slots[c]) == Text(r.Slots[previous]))
					&& this.Rows.Any(r => Text(r.Slots[c]).Length > 0);

				if (!duplicate)
					keep.Add(c);
			}

			if (keep.Count == this.ColumnCount)
				return;

			foreach (var row in this.Rows)
				row.Slots = Select(row.Slots, keep);

			this.ColumnCount = keep.Count;
		}

		#region Implementation

		/// <summary>
		/// Places the cells in the grid.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns>The rows of the grid.</returns>
		private static List<GridRow> Build(TableBlock table)
		{
			var grid = new List<List<Slot>>();

			// rows beyond the slot budget are dropped: a million cells is far beyond any useful context.
			var widest = 1;
			foreach (var row in table.Rows)
				widest = Math.Max(widest, (int)Math.Min(MaxColumns, row.Cells.Sum(c => (long)c.ColumnSpan)));

			var rowCount = Math.Min(table.Rows.Count, Math.Max(1, MaxSlots / widest));

			// the last row of every group bounds the row spans.
			var groupEnd = new Dictionary<int, int>();
			for (var r = 0; r < rowCount; r++)
			{
				foreach (var cell in table.Rows[r].Cells)
					groupEnd[cell.Group] = r;
			}

			for (var r = 0; r < rowCount; r++)
				grid.Add(new List<Slot>());

			var columns = 0;

			for (var r = 0; r < rowCount; r++)
			{
				var column = 0;

				foreach (var cell in table.Rows[r].Cells)
				{
					while (column < grid[r].Count && grid[r][column] != null)
						column++;

					if (column >= MaxColumns)
						break;

					var lastRow = groupEnd.TryGetValue(cell.Group, out var end) ? end : rowCount - 1;
					var rowSpan = cell.RowSpan == 0 ? lastRow - r + 1 : Math.Min(cell.RowSpan, lastRow - r + 1);
					var columnSpan = Math.Min(cell.ColumnSpan, MaxColumns - column);
					rowSpan = Math.Max(1, rowSpan);

					for (var y = 0; y < rowSpan; y++)
					{
						var row = grid[r + y];
						while (row.Count < column + columnSpan)
							row.Add(null);

						for (var x = 0; x < columnSpan; x++)
						{
							// overlapping spans: the first cell keeps the slot.
							if (row[column + x] != null)
								continue;

							row[column + x] = new Slot
							{
								Cell = cell,
								IsOrigin = x == 0 && y == 0,
								IsHorizontalContinuation = x > 0,
								IsVerticalContinuation = y > 0,
							};
						}
					}

					column += columnSpan;
					columns = Math.Max(columns, column);
				}
			}

			var result = new List<GridRow>(rowCount);
			for (var r = 0; r < rowCount; r++)
			{
				var slots = new Slot[columns];
				for (var c = 0; c < grid[r].Count && c < columns; c++)
					slots[c] = grid[r][c];

				result.Add(new GridRow { Slots = slots, IsHeader = table.Rows[r].IsHeader });
			}

			return result;
		}

		/// <summary>
		/// Removes the rows and columns that have no content, which tables used for spacing contain.
		/// </summary>
		/// <param name="rows">The rows.</param>
		/// <param name="isEmpty">Determines whether a cell has no visible content.</param>
		private static void RemoveEmpty(List<GridRow> rows, Func<TableCell, bool> isEmpty)
		{
			rows.RemoveAll(r => r.Slots.All(s => s == null || isEmpty(s.Cell)));

			if (rows.Count == 0)
				return;

			var keep = new List<int>();
			for (var c = 0; c < rows[0].Slots.Length; c++)
			{
				// a column is kept when a cell starts in it and has content.
				if (rows.Any(r => r.Slots[c] != null && !r.Slots[c].IsHorizontalContinuation && !isEmpty(r.Slots[c].Cell)))
					keep.Add(c);
			}

			if (keep.Count == rows[0].Slots.Length)
				return;

			foreach (var row in rows)
				row.Slots = Select(row.Slots, keep);
		}

		/// <summary>
		/// Selects columns of a row. The first kept slot of a cell becomes its horizontal origin.
		/// </summary>
		/// <param name="slots">The slots of the row.</param>
		/// <param name="columns">The indexes of the columns to keep.</param>
		/// <returns>The kept slots.</returns>
		private static Slot[] Select(Slot[] slots, List<int> columns)
		{
			var result = new Slot[columns.Count];

			for (var i = 0; i < columns.Count; i++)
			{
				var slot = slots[columns[i]];
				if (slot != null && slot.IsHorizontalContinuation && (i == 0 || result[i - 1]?.Cell != slot.Cell))
					slot = new Slot { Cell = slot.Cell, IsOrigin = !slot.IsVerticalContinuation, IsVerticalContinuation = slot.IsVerticalContinuation };

				result[i] = slot;
			}

			return result;
		}

		/// <summary>
		/// Counts the header rows.
		/// </summary>
		/// <param name="rows">The rows.</param>
		/// <returns>The number of leading header rows.</returns>
		private static int CountHeaderRows(List<GridRow> rows)
		{
			var declared = 0;
			while (declared < rows.Count && rows[declared].IsHeader)
				declared++;

			if (declared > 0)
				return declared;

			var implicitCount = 0;
			while (implicitCount < rows.Count && rows[implicitCount].Slots.All(s => s == null || s.Cell.IsHeader))
				implicitCount++;

			// a table made only of header cells has no header row.
			return implicitCount == rows.Count ? 0 : implicitCount;
		}

		#endregion
	}
}

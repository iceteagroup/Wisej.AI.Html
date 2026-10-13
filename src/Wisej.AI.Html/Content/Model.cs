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

namespace Wisej.AI.Html.Content
{
	#region Inlines

	/// <summary>
	/// The character formatting of a run of text.
	/// </summary>
	[Flags]
	internal enum InlineStyle
	{
		/// <summary>No formatting.</summary>
		None = 0,

		/// <summary>Strong emphasis.</summary>
		Bold = 1,

		/// <summary>Emphasis.</summary>
		Italic = 2,

		/// <summary>Strikethrough.</summary>
		Strike = 4,

		/// <summary>Superscript.</summary>
		Superscript = 8,

		/// <summary>Subscript.</summary>
		Subscript = 16,

		/// <summary>Code, set in a monospace font.</summary>
		Code = 32,
	}

	/// <summary>
	/// A piece of inline content.
	/// </summary>
	internal abstract class Inline
	{
		/// <summary>
		/// Gets or sets the target of the link that contains the inline, or <c>null</c>.
		/// </summary>
		public string Link { get; set; }
	}

	/// <summary>
	/// A run of text with uniform formatting. White space is already collapsed.
	/// </summary>
	internal sealed class TextInline : Inline
	{
		/// <summary>Gets or sets the text.</summary>
		public string Text { get; set; }

		/// <summary>Gets or sets the formatting.</summary>
		public InlineStyle Style { get; set; }
	}

	/// <summary>
	/// A forced line break (<c>&lt;br&gt;</c>).
	/// </summary>
	internal sealed class BreakInline : Inline
	{
	}

	/// <summary>
	/// An image.
	/// </summary>
	internal sealed class ImageInline : Inline
	{
		/// <summary>Gets or sets the image.</summary>
		public HtmlImage Image { get; set; }
	}

	/// <summary>
	/// An equation, as LaTeX.
	/// </summary>
	internal sealed class MathInline : Inline
	{
		/// <summary>Gets or sets the LaTeX source.</summary>
		public string Latex { get; set; }
	}

	#endregion

	#region Blocks

	/// <summary>
	/// A block of content.
	/// </summary>
	internal abstract class Block
	{
	}

	/// <summary>
	/// A paragraph, heading or definition term.
	/// </summary>
	internal sealed class ParagraphBlock : Block
	{
		/// <summary>Gets the inline content.</summary>
		public List<Inline> Inlines { get; } = new List<Inline>();

		/// <summary>Gets or sets the heading level, 1 to 6, or 0 for a regular paragraph.</summary>
		public int HeadingLevel { get; set; }

		/// <summary>Gets or sets whether the paragraph is a definition term (<c>&lt;dt&gt;</c>).</summary>
		public bool IsTerm { get; set; }
	}

	/// <summary>
	/// Preformatted text or source code.
	/// </summary>
	internal sealed class CodeBlock : Block
	{
		/// <summary>Gets or sets the text, with its white space.</summary>
		public string Text { get; set; }

		/// <summary>Gets or sets the language, such as <c>csharp</c>, or <c>null</c>.</summary>
		public string Language { get; set; }
	}

	/// <summary>
	/// A block quotation.
	/// </summary>
	internal sealed class QuoteBlock : Block
	{
		/// <summary>Gets the content.</summary>
		public List<Block> Blocks { get; } = new List<Block>();
	}

	/// <summary>
	/// The definition of a term (<c>&lt;dd&gt;</c>).
	/// </summary>
	internal sealed class DefinitionBlock : Block
	{
		/// <summary>Gets the content.</summary>
		public List<Block> Blocks { get; } = new List<Block>();
	}

	/// <summary>
	/// An ordered or unordered list.
	/// </summary>
	internal sealed class ListBlock : Block
	{
		/// <summary>Gets or sets whether the list is ordered.</summary>
		public bool Ordered { get; set; }

		/// <summary>Gets the items.</summary>
		public List<ListItem> Items { get; } = new List<ListItem>();
	}

	/// <summary>
	/// An item of a list.
	/// </summary>
	internal sealed class ListItem
	{
		/// <summary>Gets or sets the marker, such as <c>-</c>, <c>3.</c> or <c>b)</c>.</summary>
		public string Marker { get; set; }

		/// <summary>Gets the content.</summary>
		public List<Block> Blocks { get; } = new List<Block>();
	}

	/// <summary>
	/// A table.
	/// </summary>
	internal sealed class TableBlock : Block
	{
		/// <summary>Gets the rows, header rows first and footer rows last.</summary>
		public List<TableRow> Rows { get; } = new List<TableRow>();

		/// <summary>Gets the caption, possibly empty.</summary>
		public List<Block> Caption { get; } = new List<Block>();
	}

	/// <summary>
	/// A row of a table.
	/// </summary>
	internal sealed class TableRow
	{
		/// <summary>Gets the cells.</summary>
		public List<TableCell> Cells { get; } = new List<TableCell>();

		/// <summary>Gets or sets whether the row belongs to the table header (<c>&lt;thead&gt;</c>).</summary>
		public bool IsHeader { get; set; }
	}

	/// <summary>
	/// A cell of a table.
	/// </summary>
	internal sealed class TableCell
	{
		/// <summary>Gets the content.</summary>
		public List<Block> Blocks { get; } = new List<Block>();

		/// <summary>Gets or sets the number of columns spanned.</summary>
		public int ColumnSpan { get; set; } = 1;

		/// <summary>Gets or sets the number of rows spanned; 0 spans to the end of the row group.</summary>
		public int RowSpan { get; set; } = 1;

		/// <summary>Gets or sets whether the cell is a header cell (<c>&lt;th&gt;</c>).</summary>
		public bool IsHeader { get; set; }

		/// <summary>Gets or sets the index of the row group of the cell, which bounds its row span.</summary>
		public int Group { get; set; }
	}

	/// <summary>
	/// A thematic break (<c>&lt;hr&gt;</c>).
	/// </summary>
	internal sealed class RuleBlock : Block
	{
	}

	/// <summary>
	/// A display equation, as LaTeX.
	/// </summary>
	internal sealed class MathBlock : Block
	{
		/// <summary>Gets or sets the LaTeX source.</summary>
		public string Latex { get; set; }
	}

	/// <summary>
	/// A print page break.
	/// </summary>
	internal sealed class PageBreakBlock : Block
	{
	}

	#endregion

	/// <summary>
	/// The content of a document, ready to be rendered.
	/// </summary>
	internal sealed class DocumentContent
	{
		/// <summary>Gets the blocks, with <see cref="PageBreakBlock"/> markers between pages.</summary>
		public List<Block> Blocks { get; } = new List<Block>();

		/// <summary>Gets the images, in reading order.</summary>
		public List<HtmlImage> Images { get; } = new List<HtmlImage>();

		/// <summary>Gets the links, in reading order, without duplicates.</summary>
		public List<HtmlLink> Links { get; } = new List<HtmlLink>();

		/// <summary>Gets or sets the document properties.</summary>
		public HtmlMetadata Metadata { get; set; }
	}
}

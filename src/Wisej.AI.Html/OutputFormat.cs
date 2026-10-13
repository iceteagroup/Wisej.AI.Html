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

namespace Wisej.AI.Html
{
	/// <summary>
	/// Text format produced by the <see cref="HtmlExtractor"/>.
	/// </summary>
	public enum OutputFormat
	{
		/// <summary>
		/// GitHub flavored Markdown: headings, emphasis, lists, pipe tables (or HTML tables when the
		/// structure requires it), links, images, code blocks and LaTeX equations. The best format for
		/// language models, which read Markdown natively.
		/// </summary>
		Markdown,

		/// <summary>
		/// Plain text without markup. Lists keep their labels and tables are written as rows of
		/// pipe-separated cells.
		/// </summary>
		PlainText,
	}

	/// <summary>
	/// Controls how tables are written in Markdown.
	/// </summary>
	public enum TableFormat
	{
		/// <summary>
		/// Pipe tables for regular tables, HTML tables when a table contains nested tables, which a pipe
		/// table cannot represent.
		/// </summary>
		Auto,

		/// <summary>Always pipe tables. Nested tables are flattened into their cells.</summary>
		Markdown,

		/// <summary>Always HTML tables, with <c>colspan</c> and <c>rowspan</c> for merged cells.</summary>
		Html,
	}

	/// <summary>
	/// Controls how cells merged with <c>colspan</c> and <c>rowspan</c> are written in pipe tables.
	/// </summary>
	public enum MergedCellMode
	{
		/// <summary>
		/// The content of a merged cell is repeated in every cell it covers, so that every row is
		/// self-contained, which is what retrieval and language models need.
		/// </summary>
		Repeat,

		/// <summary>The content is written in the first covered cell only; the others are empty.</summary>
		Empty,
	}
}

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

using System.Collections.Generic;

namespace Wisej.AI.Html
{
	/// <summary>
	/// The result of the extraction of an HTML document.
	/// </summary>
	public sealed class HtmlExtractionResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlExtractionResult"/> class.
		/// </summary>
		/// <param name="text">The text of the whole document.</param>
		/// <param name="pages">The pages of the document.</param>
		/// <param name="images">The images of the document.</param>
		/// <param name="links">The hyperlinks of the document.</param>
		/// <param name="metadata">The document properties.</param>
		internal HtmlExtractionResult(
			string text,
			IReadOnlyList<HtmlPage> pages,
			IReadOnlyList<HtmlImage> images,
			IReadOnlyList<HtmlLink> links,
			HtmlMetadata metadata)
		{
			this.Text = text;
			this.Pages = pages;
			this.Images = images;
			this.Links = links;
			this.Metadata = metadata;
		}

		/// <summary>
		/// Gets the text of the whole document. When the document has more than one page, every page is
		/// introduced by a header line (see <see cref="HtmlExtractorOptions.PageHeaderFormat"/>).
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Gets the pages of the document. A document without print page breaks has a single page.
		/// </summary>
		public IReadOnlyList<HtmlPage> Pages { get; }

		/// <summary>
		/// Gets the number of pages.
		/// </summary>
		public int PageCount => this.Pages.Count;

		/// <summary>
		/// Gets every image placed in the extracted content, in reading order.
		/// </summary>
		public IReadOnlyList<HtmlImage> Images { get; }

		/// <summary>
		/// Gets the hyperlinks of the extracted content, in reading order, without duplicates.
		/// </summary>
		public IReadOnlyList<HtmlLink> Links { get; }

		/// <summary>
		/// Gets the document properties.
		/// </summary>
		public HtmlMetadata Metadata { get; }

		/// <summary>
		/// Returns the text of the whole document.
		/// </summary>
		/// <returns>The value of <see cref="Text"/>.</returns>
		public override string ToString() => this.Text;
	}
}

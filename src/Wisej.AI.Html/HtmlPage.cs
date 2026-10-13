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
	/// A page of the document.
	/// </summary>
	/// <remarks>
	/// HTML has no pages: a document is a single page unless it declares print page breaks with the CSS
	/// <c>break-before</c>, <c>break-after</c>, <c>page-break-before</c> or <c>page-break-after</c>
	/// properties, as reports and documents exported to HTML by Word and other applications do.
	/// </remarks>
	public sealed class HtmlPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlPage"/> class.
		/// </summary>
		/// <param name="number">The 1-based page number.</param>
		/// <param name="text">The text of the page.</param>
		/// <param name="images">The images displayed on the page.</param>
		internal HtmlPage(int number, string text, IReadOnlyList<HtmlImage> images)
		{
			this.Number = number;
			this.Text = text;
			this.Images = images;
		}

		/// <summary>
		/// Gets the 1-based page number.
		/// </summary>
		public int Number { get; }

		/// <summary>
		/// Gets the text of the page, without the page header line.
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Gets the images displayed on the page.
		/// </summary>
		public IReadOnlyList<HtmlImage> Images { get; }

		/// <summary>
		/// Returns the text of the page.
		/// </summary>
		/// <returns>The value of <see cref="Text"/>.</returns>
		public override string ToString() => this.Text;
	}

	/// <summary>
	/// A hyperlink of the document.
	/// </summary>
	public sealed class HtmlLink
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlLink"/> class.
		/// </summary>
		/// <param name="url">The absolute or relative target.</param>
		/// <param name="text">The text of the link.</param>
		internal HtmlLink(string url, string text)
		{
			this.Url = url;
			this.Text = text;
		}

		/// <summary>
		/// Gets the target of the link, resolved against the base URL when one is known.
		/// </summary>
		public string Url { get; }

		/// <summary>
		/// Gets the text of the link, or an empty string for links that only contain images.
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Returns the text and the target of the link.
		/// </summary>
		/// <returns>A string describing the link.</returns>
		public override string ToString() => $"{this.Text} ({this.Url})";
	}
}

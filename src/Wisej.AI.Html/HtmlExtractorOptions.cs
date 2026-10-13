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

namespace Wisej.AI.Html
{
	/// <summary>
	/// Options that control what the <see cref="HtmlExtractor"/> extracts and how it is formatted.
	/// </summary>
	/// <remarks>
	/// The defaults produce Markdown tuned for language model context and retrieval: every piece of
	/// information of the document is kept (headings, emphasis, tables, list numbers, image alternative
	/// text, links, code, math, form values) while noise is removed (scripts, styles, hidden content,
	/// navigation, decorative images and inline image data).
	/// </remarks>
	public sealed class HtmlExtractorOptions
	{
		#region Output

		/// <summary>
		/// Gets or sets the format of the extracted text. The default is <see cref="OutputFormat.Markdown"/>.
		/// </summary>
		public OutputFormat Format { get; set; } = OutputFormat.Markdown;

		/// <summary>
		/// Gets or sets the composite format of the line that introduces every page in
		/// <see cref="HtmlExtractionResult.Text"/> when the document has more than one page, where
		/// <c>{0}</c> is the page number and <c>{1}</c> the total number of pages.
		/// </summary>
		/// <remarks>
		/// When <c>null</c> (the default), Markdown output uses <c>&lt;!-- Page {0} --&gt;</c> and plain text
		/// output uses <c>--- Page {0} ---</c>. Set it to an empty string to concatenate the pages without separators.
		/// </remarks>
		public string PageHeaderFormat { get; set; }

		/// <summary>
		/// Gets or sets whether character formatting (bold, italic, strikethrough, superscript, subscript and
		/// code) is written as Markdown. The default is <c>true</c>.
		/// </summary>
		public bool IncludeFormatting { get; set; } = true;

		/// <summary>
		/// Gets or sets whether hyperlinks are written as Markdown links. The default is <c>true</c>.
		/// </summary>
		/// <remarks>
		/// Links to <c>javascript:</c> URLs and to anchors in the same document are always written as plain text.
		/// </remarks>
		public bool IncludeHyperlinks { get; set; } = true;

		/// <summary>
		/// Gets or sets whether the document title is written as a level 1 heading at the start of the text,
		/// when the content does not already start with it. The default is <c>true</c>.
		/// </summary>
		public bool IncludeTitle { get; set; } = true;

		#endregion

		#region Content

		/// <summary>
		/// Gets or sets whether only the main content of a web page is extracted, when the page identifies it
		/// with a <c>&lt;main&gt;</c> element, the <c>main</c> role or a single <c>&lt;article&gt;</c>.
		/// The default is <c>true</c>.
		/// </summary>
		public bool MainContentOnly { get; set; } = true;

		/// <summary>
		/// Gets or sets whether the site chrome of web pages is removed: navigation menus, page banners and
		/// footers, skip links and cookie banners. The default is <c>true</c>.
		/// </summary>
		public bool RemoveBoilerplate { get; set; } = true;

		/// <summary>
		/// Gets or sets whether content that is not rendered is included: elements hidden with the
		/// <c>hidden</c> attribute, <c>aria-hidden</c>, <c>display: none</c>, <c>visibility: hidden</c> or the
		/// screen reader only pattern. The default is <c>false</c>.
		/// </summary>
		public bool IncludeHiddenContent { get; set; }

		/// <summary>
		/// Gets or sets whether the values of form controls are included: check boxes as <c>[x]</c> and
		/// <c>[ ]</c>, the value of text fields, the selected option of lists and the text of text areas.
		/// The default is <c>true</c>.
		/// </summary>
		public bool IncludeForms { get; set; } = true;

		/// <summary>
		/// Gets or sets whether MathML equations are included as LaTeX. The default is <c>true</c>.
		/// </summary>
		public bool IncludeMath { get; set; } = true;

		/// <summary>
		/// Gets or sets whether the content of <c>&lt;noscript&gt;</c> elements is included. The default is <c>false</c>.
		/// </summary>
		public bool IncludeNoScript { get; set; }

		/// <summary>
		/// Gets or sets whether the author style sheets (<c>&lt;style&gt;</c> elements, linked style sheets of
		/// MHTML archives and <c>style</c> attributes) are applied to determine hidden content, emphasis,
		/// code fonts and page breaks. The default is <c>true</c>.
		/// </summary>
		public bool ApplyStyleSheets { get; set; } = true;

		/// <summary>
		/// Gets or sets the URL of the document, used to resolve relative links and images.
		/// The default is <c>null</c>, which uses the <c>&lt;base href&gt;</c> of the document, if any.
		/// </summary>
		public Uri BaseUri { get; set; }

		/// <summary>
		/// Gets or sets the encoding of documents read from bytes, overriding the detection.
		/// The default is <c>null</c>, which detects the encoding as browsers do.
		/// </summary>
		public Encoding Encoding { get; set; }

		#endregion

		#region Tables

		/// <summary>
		/// Gets or sets how tables are written in Markdown. The default is <see cref="TableFormat.Auto"/>.
		/// </summary>
		public TableFormat Tables { get; set; } = TableFormat.Auto;

		/// <summary>
		/// Gets or sets how merged cells are written in pipe tables. The default is <see cref="MergedCellMode.Repeat"/>.
		/// </summary>
		public MergedCellMode MergedCells { get; set; } = MergedCellMode.Repeat;

		/// <summary>
		/// Gets or sets whether tables used for page layout (marked as presentational, made of a single row or
		/// column, or containing other tables) are written as regular content instead of tables.
		/// The default is <c>true</c>.
		/// </summary>
		public bool UnwrapLayoutTables { get; set; } = true;

		#endregion

		#region Images

		/// <summary>
		/// Gets or sets whether images are referenced in the text and listed in <see cref="HtmlExtractionResult.Images"/>.
		/// The default is <c>true</c>.
		/// </summary>
		public bool IncludeImages { get; set; } = true;

		/// <summary>
		/// Gets or sets whether the bytes of embedded images are loaded into <see cref="HtmlImage.Data"/>.
		/// The default is <c>true</c>.
		/// </summary>
		public bool IncludeImageData { get; set; } = true;

		/// <summary>
		/// Gets or sets whether decorative images are skipped: images with an empty <c>alt</c>, the
		/// <c>presentation</c> or <c>none</c> role or <c>aria-hidden</c>, tracking pixels, and inline SVG icons
		/// without a title or text. The default is <c>true</c>.
		/// </summary>
		public bool SkipDecorativeImages { get; set; } = true;

		/// <summary>
		/// Gets or sets the minimum declared width and height, in CSS pixels, of an image to be included.
		/// Images whose declared size is smaller are skipped; images without a declared size are always included.
		/// The default is <c>0</c>.
		/// </summary>
		public double MinimumImageSize { get; set; }

		/// <summary>
		/// Gets or sets an optional callback that describes an image, for example with a vision model or OCR.
		/// The returned text is written after the image reference. Return <c>null</c> to add nothing.
		/// </summary>
		/// <remarks>
		/// The callback is invoked once per distinct picture: placements of the same picture reuse the description.
		/// <see cref="HtmlImage.Data"/> is always loaded for the callback, regardless of <see cref="IncludeImageData"/>;
		/// it is <c>null</c> for external images, which the callback can download from <see cref="HtmlImage.Url"/>.
		/// </remarks>
		public Func<HtmlImage, string> ImageDescriber { get; set; }

		#endregion

		/// <summary>
		/// Creates a copy of these options.
		/// </summary>
		/// <returns>A new <see cref="HtmlExtractorOptions"/> with the same values.</returns>
		public HtmlExtractorOptions Clone()
		{
			return (HtmlExtractorOptions)MemberwiseClone();
		}
	}
}

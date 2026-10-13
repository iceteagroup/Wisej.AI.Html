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
	/// An image placed in the document: an <c>&lt;img&gt;</c>, a <c>&lt;picture&gt;</c>, an
	/// <c>&lt;input type="image"&gt;</c> or an inline <c>&lt;svg&gt;</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Images embedded in the document (<c>data:</c> URIs, inline SVG and the resources of MHTML web
	/// archives) carry their bytes in <see cref="Data"/> and are referenced in the text by a generated
	/// <see cref="FileName"/>, such as <c>image3.png</c>, so that a base64 payload never ends up in the
	/// language model context. Images linked to external files are referenced by their absolute
	/// <see cref="Url"/>.
	/// </para>
	/// <para>
	/// Every placement produces an <see cref="HtmlImage"/>; placements of the same picture share the same
	/// <see cref="FileName"/> and <see cref="Data"/>.
	/// </para>
	/// </remarks>
	public sealed class HtmlImage
	{
		/// <summary>
		/// Gets the file name of an embedded image, such as <c>image3.png</c>, or the last segment of the
		/// URL of an external image.
		/// </summary>
		public string FileName { get; internal set; }

		/// <summary>
		/// Gets the absolute URL of the image, or <c>null</c> for images embedded as <c>data:</c> URIs or inline SVG.
		/// </summary>
		public string Url { get; internal set; }

		/// <summary>
		/// Gets the MIME type of the image, such as <c>image/png</c>, or <c>null</c> when unknown.
		/// </summary>
		public string ContentType { get; internal set; }

		/// <summary>
		/// Gets the bytes of an embedded image, or <c>null</c> for external images and when
		/// <see cref="HtmlExtractorOptions.IncludeImageData"/> is <c>false</c>.
		/// </summary>
		public byte[] Data { get; internal set; }

		/// <summary>
		/// Gets the alternative text written by the author (<c>alt</c>, <c>aria-label</c> or the SVG <c>&lt;title&gt;</c>), or <c>null</c>.
		/// </summary>
		public string AltText { get; internal set; }

		/// <summary>
		/// Gets the <c>title</c> of the image, or <c>null</c>.
		/// </summary>
		public string Title { get; internal set; }

		/// <summary>
		/// Gets the caption of the image, from the <c>&lt;figcaption&gt;</c> of its figure, or <c>null</c>.
		/// </summary>
		public string Caption { get; internal set; }

		/// <summary>
		/// Gets the width declared by the document, in CSS pixels, or 0 when unknown.
		/// </summary>
		public double Width { get; internal set; }

		/// <summary>
		/// Gets the height declared by the document, in CSS pixels, or 0 when unknown.
		/// </summary>
		public double Height { get; internal set; }

		/// <summary>
		/// Gets the number of the page that displays the image.
		/// </summary>
		public int PageNumber { get; internal set; }

		/// <summary>
		/// Gets the description produced by <see cref="HtmlExtractorOptions.ImageDescriber"/>, or <c>null</c>.
		/// </summary>
		public string Description { get; internal set; }

		/// <summary>
		/// Gets the target of the link that contains the image, or <c>null</c>.
		/// </summary>
		public string Link { get; internal set; }

		/// <summary>
		/// Gets the key that identifies the picture: the URL or the hash of the embedded data.
		/// </summary>
		internal string Key { get; set; }

		/// <summary>
		/// Gets the reference written in the text: the file name of embedded images, the URL of external ones.
		/// </summary>
		internal string Reference => this.Data != null || this.Url == null ? this.FileName : this.Url;

		/// <summary>
		/// Returns the reference and the alternative text of the image.
		/// </summary>
		/// <returns>A string describing the image.</returns>
		public override string ToString()
		{
			var reference = this.Url ?? this.FileName;
			return this.AltText == null ? reference : $"{reference} ({this.AltText})";
		}
	}
}

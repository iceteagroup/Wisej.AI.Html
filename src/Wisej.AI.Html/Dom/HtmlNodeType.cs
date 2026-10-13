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

namespace Wisej.AI.Html.Dom
{
	/// <summary>
	/// The type of an <see cref="HtmlNode"/>. The values match the DOM <c>Node.nodeType</c> constants.
	/// </summary>
	public enum HtmlNodeType
	{
		/// <summary>An element, such as <c>&lt;p&gt;</c>.</summary>
		Element = 1,

		/// <summary>A run of text.</summary>
		Text = 3,

		/// <summary>A comment.</summary>
		Comment = 8,

		/// <summary>The document.</summary>
		Document = 9,

		/// <summary>The document type declaration (<c>&lt;!DOCTYPE html&gt;</c>).</summary>
		DocumentType = 10,

		/// <summary>A document fragment, such as the content of a <c>&lt;template&gt;</c>.</summary>
		DocumentFragment = 11,
	}

	/// <summary>
	/// The rendering mode selected by the document type declaration.
	/// </summary>
	public enum HtmlQuirksMode
	{
		/// <summary>Standards mode.</summary>
		NoQuirks,

		/// <summary>Almost standards mode.</summary>
		LimitedQuirks,

		/// <summary>Quirks mode, used by documents without a document type declaration or with a legacy one.</summary>
		Quirks,
	}

	/// <summary>
	/// The namespaces of the elements and attributes created by the HTML parser.
	/// </summary>
	public static class HtmlNamespaces
	{
		/// <summary>The HTML namespace.</summary>
		public const string Html = "http://www.w3.org/1999/xhtml";

		/// <summary>The SVG namespace.</summary>
		public const string Svg = "http://www.w3.org/2000/svg";

		/// <summary>The MathML namespace.</summary>
		public const string MathML = "http://www.w3.org/1998/Math/MathML";

		/// <summary>The XLink namespace, used by attributes such as <c>xlink:href</c> in SVG and MathML.</summary>
		public const string XLink = "http://www.w3.org/1999/xlink";

		/// <summary>The XML namespace, used by attributes such as <c>xml:lang</c>.</summary>
		public const string Xml = "http://www.w3.org/XML/1998/namespace";

		/// <summary>The XMLNS namespace, used by namespace declarations.</summary>
		public const string Xmlns = "http://www.w3.org/2000/xmlns/";
	}
}

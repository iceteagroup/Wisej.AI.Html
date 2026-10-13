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

using System.Text;

namespace Wisej.AI.Html.Dom
{
	/// <summary>
	/// Options of the HTML parser.
	/// </summary>
	public sealed class HtmlParserOptions
	{
		/// <summary>
		/// Gets or sets whether the parser behaves as if scripting were enabled. The default is <c>false</c>.
		/// </summary>
		/// <remarks>
		/// With scripting enabled the content of <c>&lt;noscript&gt;</c> is parsed as raw text, as browsers
		/// with JavaScript enabled do; with scripting disabled it is parsed as markup.
		/// </remarks>
		public bool ScriptingEnabled { get; set; }

		/// <summary>
		/// Gets or sets the encoding of a document loaded from bytes, overriding the detection.
		/// The default is <c>null</c>, which detects the encoding.
		/// </summary>
		/// <remarks>
		/// When <c>null</c>, the encoding is determined as browsers do: from the byte order mark, then
		/// <see cref="TransportEncoding"/>, then the <c>&lt;meta charset&gt;</c> declaration, then the
		/// content itself (UTF-8 when the bytes are valid UTF-8, otherwise windows-1252).
		/// A byte order mark always takes precedence.
		/// </remarks>
		public Encoding Encoding { get; set; }

		/// <summary>
		/// Gets or sets the name of the encoding declared by the transport layer, such as the <c>charset</c>
		/// parameter of an HTTP <c>Content-Type</c> header or of a MIME part. The default is <c>null</c>.
		/// </summary>
		public string TransportEncoding { get; set; }

		/// <summary>
		/// Gets or sets the maximum depth of the element tree. The default is <c>512</c>, the same limit used by browsers.
		/// </summary>
		/// <remarks>
		/// Elements nested deeper are inserted as siblings of their parent, which keeps every algorithm
		/// working on the tree safe from stack overflows on hostile or broken markup.
		/// </remarks>
		public int MaximumDepth { get; set; } = 512;

		/// <summary>
		/// Creates a copy of these options.
		/// </summary>
		/// <returns>A new <see cref="HtmlParserOptions"/> with the same values.</returns>
		public HtmlParserOptions Clone()
		{
			return (HtmlParserOptions)MemberwiseClone();
		}
	}
}

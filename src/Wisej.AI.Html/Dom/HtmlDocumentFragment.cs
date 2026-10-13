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
	/// A lightweight container of nodes that is not part of a document tree: the result of
	/// <see cref="HtmlDocument.ParseFragment(string, string, HtmlParserOptions)"/> or the content of a
	/// <c>&lt;template&gt;</c> element.
	/// </summary>
	/// <remarks>
	/// Inserting a fragment into a node moves its children, not the fragment itself.
	/// </remarks>
	public sealed class HtmlDocumentFragment : HtmlNode
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlDocumentFragment"/> class.
		/// </summary>
		/// <param name="ownerDocument">The document that owns the fragment.</param>
		internal HtmlDocumentFragment(HtmlDocument ownerDocument)
			: base(ownerDocument)
		{
		}

		/// <inheritdoc/>
		public override HtmlNodeType NodeType => HtmlNodeType.DocumentFragment;

		/// <inheritdoc/>
		public override string NodeName => "#document-fragment";
	}
}

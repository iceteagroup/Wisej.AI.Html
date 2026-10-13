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
	/// The document type declaration (<c>&lt;!DOCTYPE html&gt;</c>).
	/// </summary>
	public sealed class HtmlDocumentType : HtmlNode
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlDocumentType"/> class.
		/// </summary>
		/// <param name="ownerDocument">The document that owns the node.</param>
		/// <param name="name">The name of the document type, usually <c>html</c>.</param>
		/// <param name="publicId">The public identifier.</param>
		/// <param name="systemId">The system identifier.</param>
		internal HtmlDocumentType(HtmlDocument ownerDocument, string name, string publicId, string systemId)
			: base(ownerDocument)
		{
			this.Name = name ?? "";
			this.PublicId = publicId ?? "";
			this.SystemId = systemId ?? "";
		}

		/// <inheritdoc/>
		public override HtmlNodeType NodeType => HtmlNodeType.DocumentType;

		/// <inheritdoc/>
		public override string NodeName => this.Name;

		/// <summary>
		/// Gets the name of the document type, usually <c>html</c>.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the public identifier, such as <c>-//W3C//DTD HTML 4.01//EN</c>, or an empty string.
		/// </summary>
		public string PublicId { get; }

		/// <summary>
		/// Gets the system identifier, or an empty string.
		/// </summary>
		public string SystemId { get; }

		/// <inheritdoc/>
		public override string TextContent => null;

		/// <inheritdoc/>
		internal override bool CanHaveChildren => false;
	}
}

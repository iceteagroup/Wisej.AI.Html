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
	/// A comment (<c>&lt;!-- ... --&gt;</c>).
	/// </summary>
	public sealed class HtmlComment : HtmlNode
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlComment"/> class.
		/// </summary>
		/// <param name="ownerDocument">The document that owns the node.</param>
		/// <param name="data">The text of the comment.</param>
		internal HtmlComment(HtmlDocument ownerDocument, string data)
			: base(ownerDocument)
		{
			this.Data = data ?? "";
		}

		/// <inheritdoc/>
		public override HtmlNodeType NodeType => HtmlNodeType.Comment;

		/// <inheritdoc/>
		public override string NodeName => "#comment";

		/// <summary>
		/// Gets or sets the text of the comment, without the delimiters.
		/// </summary>
		public string Data { get; set; }

		/// <inheritdoc/>
		public override string TextContent => this.Data;

		/// <inheritdoc/>
		internal override bool CanHaveChildren => false;
	}
}

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
	/// A run of text. Character references are already decoded.
	/// </summary>
	public sealed class HtmlText : HtmlNode
	{
		// the text, or null while the parser is appending to the builder.
		private string _data;

		// collects consecutive character tokens without quadratic string concatenation.
		private StringBuilder _builder;

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlText"/> class.
		/// </summary>
		/// <param name="ownerDocument">The document that owns the node.</param>
		/// <param name="data">The text.</param>
		internal HtmlText(HtmlDocument ownerDocument, string data)
			: base(ownerDocument)
		{
			_data = data ?? "";
		}

		/// <inheritdoc/>
		public override HtmlNodeType NodeType => HtmlNodeType.Text;

		/// <inheritdoc/>
		public override string NodeName => "#text";

		/// <summary>
		/// Gets or sets the text.
		/// </summary>
		public string Data
		{
			get
			{
				if (_builder != null)
				{
					_data = _builder.ToString();
					_builder = null;
				}

				return _data;
			}
			set
			{
				_builder = null;
				_data = value ?? "";
			}
		}

		/// <inheritdoc/>
		public override string TextContent => this.Data;

		/// <inheritdoc/>
		internal override bool CanHaveChildren => false;

		/// <summary>
		/// Appends text, as the tree builder does when consecutive character tokens are inserted.
		/// </summary>
		/// <param name="text">The text to append.</param>
		internal void Append(string text)
		{
			if (_builder == null)
			{
				_builder = new StringBuilder(_data, _data.Length + text.Length + 16);
				_data = null;
			}

			_builder.Append(text);
		}

		/// <summary>
		/// Returns the text, for debugging.
		/// </summary>
		/// <returns>The text in quotes.</returns>
		public override string ToString()
		{
			return "\"" + this.Data + "\"";
		}
	}
}

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

namespace Wisej.AI.Html.Parsing
{
	/// <summary>
	/// The type of a token produced by the <see cref="HtmlTokenizer"/>.
	/// </summary>
	internal enum HtmlTokenType
	{
		/// <summary>A document type declaration.</summary>
		Doctype,

		/// <summary>A start tag.</summary>
		StartTag,

		/// <summary>An end tag.</summary>
		EndTag,

		/// <summary>A comment.</summary>
		Comment,

		/// <summary>A run of characters.</summary>
		Characters,

		/// <summary>The end of the input.</summary>
		EndOfFile,
	}

	/// <summary>
	/// An attribute of a tag token.
	/// </summary>
	internal struct HtmlTokenAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlTokenAttribute"/> struct.
		/// </summary>
		/// <param name="name">The lowercase name.</param>
		/// <param name="value">The decoded value.</param>
		public HtmlTokenAttribute(string name, string value)
		{
			this.Name = name;
			this.Value = value;
		}

		/// <summary>The lowercase name of the attribute.</summary>
		public string Name;

		/// <summary>The value of the attribute, with character references decoded.</summary>
		public string Value;
	}

	/// <summary>
	/// A token produced by the <see cref="HtmlTokenizer"/> (HTML standard, 13.2.5).
	/// </summary>
	/// <remarks>
	/// Consecutive characters are delivered as a single <see cref="HtmlTokenType.Characters"/> token; the tree
	/// builder splits them where the insertion mode treats white space differently.
	/// </remarks>
	internal sealed class HtmlToken
	{
		/// <summary>An empty attribute list shared by the tokens without attributes.</summary>
		private static readonly List<HtmlTokenAttribute> NoAttributes = new List<HtmlTokenAttribute>();

		/// <summary>
		/// Gets or sets the type of the token.
		/// </summary>
		public HtmlTokenType Type { get; set; }

		/// <summary>
		/// Gets or sets the lowercase name of a tag or the name of a document type.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the characters of a character token or the text of a comment.
		/// </summary>
		public string Data { get; set; }

		/// <summary>
		/// Gets or sets the attributes of a tag, in source order, without duplicates.
		/// </summary>
		public List<HtmlTokenAttribute> Attributes { get; set; } = NoAttributes;

		/// <summary>
		/// Gets or sets whether a tag ends with <c>/&gt;</c>.
		/// </summary>
		public bool SelfClosing { get; set; }

		/// <summary>
		/// Gets or sets the public identifier of a document type, or <c>null</c> when missing.
		/// </summary>
		public string PublicId { get; set; }

		/// <summary>
		/// Gets or sets the system identifier of a document type, or <c>null</c> when missing.
		/// </summary>
		public string SystemId { get; set; }

		/// <summary>
		/// Gets or sets whether a malformed document type forces quirks mode.
		/// </summary>
		public bool ForceQuirks { get; set; }

		/// <summary>
		/// Returns the value of an attribute of a tag.
		/// </summary>
		/// <param name="name">The lowercase name of the attribute.</param>
		/// <returns>The value, or <c>null</c> when the tag does not have the attribute.</returns>
		public string GetAttribute(string name)
		{
			var attributes = this.Attributes;
			for (var i = 0; i < attributes.Count; i++)
			{
				if (attributes[i].Name == name)
					return attributes[i].Value;
			}

			return null;
		}

		/// <summary>
		/// Creates a character token.
		/// </summary>
		/// <param name="data">The characters.</param>
		/// <returns>The token.</returns>
		public static HtmlToken Characters(string data)
		{
			return new HtmlToken { Type = HtmlTokenType.Characters, Data = data };
		}

		/// <summary>
		/// Creates a start tag token without attributes, used when the tree builder fakes a tag.
		/// </summary>
		/// <param name="name">The tag name.</param>
		/// <returns>The token.</returns>
		public static HtmlToken StartTag(string name)
		{
			return new HtmlToken { Type = HtmlTokenType.StartTag, Name = name };
		}

		/// <summary>
		/// Creates an end tag token, used when the tree builder fakes a tag.
		/// </summary>
		/// <param name="name">The tag name.</param>
		/// <returns>The token.</returns>
		public static HtmlToken EndTag(string name)
		{
			return new HtmlToken { Type = HtmlTokenType.EndTag, Name = name };
		}

		/// <summary>
		/// Returns a description of the token, for debugging.
		/// </summary>
		/// <returns>A string describing the token.</returns>
		public override string ToString()
		{
			switch (this.Type)
			{
				case HtmlTokenType.StartTag:
					return "<" + this.Name + ">";
				case HtmlTokenType.EndTag:
					return "</" + this.Name + ">";
				case HtmlTokenType.Characters:
					return "\"" + this.Data + "\"";
				case HtmlTokenType.Comment:
					return "<!--" + this.Data + "-->";
				case HtmlTokenType.Doctype:
					return "<!DOCTYPE " + this.Name + ">";
				default:
					return "EOF";
			}
		}
	}

	/// <summary>
	/// Receives the tokens produced by the <see cref="HtmlTokenizer"/>.
	/// </summary>
	internal interface IHtmlTokenSink
	{
		/// <summary>
		/// Gets whether the adjusted current node is an element outside the HTML namespace, where
		/// <c>&lt;![CDATA[</c> starts a CDATA section instead of a bogus comment.
		/// </summary>
		bool AllowCdata { get; }

		/// <summary>
		/// Processes a token.
		/// </summary>
		/// <param name="token">The token.</param>
		void ProcessToken(HtmlToken token);
	}
}

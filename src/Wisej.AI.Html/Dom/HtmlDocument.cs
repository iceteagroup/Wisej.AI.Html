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
using System.IO;
using System.Text;
using Wisej.AI.Html.Parsing;

namespace Wisej.AI.Html.Dom
{
	/// <summary>
	/// An HTML document, parsed with the algorithm of the HTML standard.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parser implements the tokenization and tree construction rules of the WHATWG HTML standard,
	/// so every input, however broken, produces the same tree as a modern browser: misnested formatting
	/// elements are repaired with the adoption agency algorithm, content misplaced in tables is foster
	/// parented, optional tags are implied, SVG and MathML get their namespaces and the 2,231 named
	/// character references are decoded.
	/// </para>
	/// <para>
	/// Documents loaded from bytes detect their encoding as browsers do (byte order mark, transport
	/// encoding, <c>&lt;meta charset&gt;</c> prescan, content sniffing).
	/// </para>
	/// <para>
	/// Example:
	/// <code><![CDATA[
	/// var document = HtmlDocument.Parse("<p>Hello <b>world</p>");
	///
	/// foreach (var cell in document.QuerySelectorAll("table.prices td"))
	///     Console.WriteLine(cell.TextContent);
	/// ]]></code>
	/// </para>
	/// </remarks>
	public sealed class HtmlDocument : HtmlNode
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlDocument"/> class, empty.
		/// </summary>
		public HtmlDocument()
			: base(null)
		{
			this.OwnerDocument = this;
		}

		#region Properties

		/// <inheritdoc/>
		public override HtmlNodeType NodeType => HtmlNodeType.Document;

		/// <inheritdoc/>
		public override string NodeName => "#document";

		/// <inheritdoc/>
		public override string TextContent => null;

		/// <summary>
		/// Gets the document type declaration, or <c>null</c>.
		/// </summary>
		public HtmlDocumentType Doctype
		{
			get
			{
				for (var child = this.FirstChild; child != null; child = child.NextSibling)
				{
					if (child is HtmlDocumentType doctype)
						return doctype;
				}

				return null;
			}
		}

		/// <summary>
		/// Gets the root element of the document, normally <c>&lt;html&gt;</c>.
		/// </summary>
		public HtmlElement DocumentElement
		{
			get
			{
				for (var child = this.FirstChild; child != null; child = child.NextSibling)
				{
					if (child is HtmlElement element)
						return element;
				}

				return null;
			}
		}

		/// <summary>
		/// Gets the <c>&lt;head&gt;</c> element, or <c>null</c>.
		/// </summary>
		public HtmlElement Head => FindChild(this.DocumentElement, "head");

		/// <summary>
		/// Gets the <c>&lt;body&gt;</c> or <c>&lt;frameset&gt;</c> element, or <c>null</c>.
		/// </summary>
		public HtmlElement Body
		{
			get
			{
				var root = this.DocumentElement;
				if (root == null || !root.Is("html"))
					return null;

				foreach (var child in root.Children)
				{
					if (child.Is("body") || child.Is("frameset"))
						return child;
				}

				return null;
			}
		}

		/// <summary>
		/// Gets the title of the document: the text of the first <c>&lt;title&gt;</c> element with its
		/// white space collapsed, or an empty string.
		/// </summary>
		public string Title
		{
			get
			{
				foreach (var element in DescendantElements())
				{
					if (element.Is("title"))
						return CollapseWhitespace(element.TextContent);
				}

				return "";
			}
		}

		/// <summary>
		/// Gets the rendering mode selected by the document type declaration.
		/// </summary>
		public HtmlQuirksMode QuirksMode { get; internal set; }

		/// <summary>
		/// Gets the encoding used to decode the document, or <c>null</c> when it was parsed from a string.
		/// </summary>
		public Encoding Encoding { get; internal set; }

		#endregion

		#region Parsing

		/// <summary>
		/// Parses an HTML document from a string.
		/// </summary>
		/// <param name="html">The markup.</param>
		/// <param name="options">The parser options, or <c>null</c> for the defaults.</param>
		/// <returns>The document.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="html"/> is <c>null</c>.</exception>
		public static HtmlDocument Parse(string html, HtmlParserOptions options = null)
		{
			if (html == null)
				throw new ArgumentNullException(nameof(html));

			var document = new HtmlDocument();
			new HtmlTreeBuilder(document, options ?? new HtmlParserOptions()).ParseDocument(html);

			return document;
		}

		/// <summary>
		/// Parses an HTML document from a stream of bytes, detecting the encoding.
		/// </summary>
		/// <param name="stream">The stream. It is read from its current position to the end and is not closed.</param>
		/// <param name="options">The parser options, or <c>null</c> for the defaults.</param>
		/// <returns>The document.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
		public static HtmlDocument Load(Stream stream, HtmlParserOptions options = null)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			using (var buffer = new MemoryStream())
			{
				stream.CopyTo(buffer);
				return Load(buffer.ToArray(), options);
			}
		}

		/// <summary>
		/// Parses an HTML document from a file, detecting the encoding.
		/// </summary>
		/// <param name="path">The path of the file.</param>
		/// <param name="options">The parser options, or <c>null</c> for the defaults.</param>
		/// <returns>The document.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <c>null</c>.</exception>
		public static HtmlDocument Load(string path, HtmlParserOptions options = null)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			return Load(File.ReadAllBytes(path), options);
		}

		/// <summary>
		/// Parses an HTML document from bytes, detecting the encoding.
		/// </summary>
		/// <param name="data">The bytes of the document.</param>
		/// <param name="options">The parser options, or <c>null</c> for the defaults.</param>
		/// <returns>The document.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <c>null</c>.</exception>
		public static HtmlDocument Load(byte[] data, HtmlParserOptions options = null)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			options = options ?? new HtmlParserOptions();

			var encoding = EncodingSniffer.Detect(data, options.Encoding, options.TransportEncoding, out var bomLength);
			var html = encoding.GetString(data, bomLength, data.Length - bomLength);

			var document = Parse(html, options);
			document.Encoding = encoding;

			return document;
		}

		/// <summary>
		/// Parses a fragment of HTML as the content of an element, as the <c>innerHTML</c> setter does.
		/// </summary>
		/// <param name="html">The markup.</param>
		/// <param name="context">The name of the HTML element that would contain the fragment. The default is <c>body</c>.</param>
		/// <param name="options">The parser options, or <c>null</c> for the defaults.</param>
		/// <returns>A fragment containing the parsed nodes.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="html"/> or <paramref name="context"/> is <c>null</c>.</exception>
		/// <remarks>
		/// The context changes how the markup is parsed: <c>&lt;td&gt;</c> is ignored in a <c>body</c>
		/// context but creates a cell in a <c>tr</c> context, and the content of a <c>textarea</c> context is text.
		/// </remarks>
		public static HtmlDocumentFragment ParseFragment(string html, string context = "body", HtmlParserOptions options = null)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			return ParseFragment(html, context.ToLowerInvariant(), HtmlNamespaces.Html, options);
		}

		/// <summary>
		/// Parses a fragment of HTML as the content of an element in any namespace.
		/// </summary>
		/// <param name="html">The markup.</param>
		/// <param name="contextName">The local name of the context element.</param>
		/// <param name="contextNamespace">The namespace of the context element.</param>
		/// <param name="options">The parser options, or <c>null</c> for the defaults.</param>
		/// <returns>A fragment containing the parsed nodes.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="html"/> is <c>null</c>.</exception>
		internal static HtmlDocumentFragment ParseFragment(string html, string contextName, string contextNamespace, HtmlParserOptions options)
		{
			if (html == null)
				throw new ArgumentNullException(nameof(html));

			var document = new HtmlDocument();
			var context = new HtmlElement(document, contextName, contextNamespace);

			return new HtmlTreeBuilder(document, options ?? new HtmlParserOptions()).ParseFragment(html, context);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Returns the first element with the specified <c>id</c>.
		/// </summary>
		/// <param name="id">The identifier, compared case sensitively.</param>
		/// <returns>The element, or <c>null</c>.</returns>
		public HtmlElement GetElementById(string id)
		{
			if (String.IsNullOrEmpty(id))
				return null;

			foreach (var element in DescendantElements())
			{
				if (element.GetAttribute("id") == id)
					return element;
			}

			return null;
		}

		/// <summary>
		/// Creates an HTML element owned by this document.
		/// </summary>
		/// <param name="name">The name of the element; it is lowercased.</param>
		/// <returns>The new element, not attached to the tree.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <c>null</c>.</exception>
		public HtmlElement CreateElement(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			return new HtmlElement(this, name.ToLowerInvariant(), HtmlNamespaces.Html);
		}

		/// <summary>
		/// Creates a text node owned by this document.
		/// </summary>
		/// <param name="data">The text.</param>
		/// <returns>The new node, not attached to the tree.</returns>
		public HtmlText CreateTextNode(string data)
		{
			return new HtmlText(this, data);
		}

		/// <summary>
		/// Creates a comment owned by this document.
		/// </summary>
		/// <param name="data">The text of the comment.</param>
		/// <returns>The new node, not attached to the tree.</returns>
		public HtmlComment CreateComment(string data)
		{
			return new HtmlComment(this, data);
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Returns the first child element of a parent with the specified HTML name.
		/// </summary>
		/// <param name="parent">The parent element. Can be <c>null</c>.</param>
		/// <param name="name">The lowercase element name.</param>
		/// <returns>The element, or <c>null</c>.</returns>
		private static HtmlElement FindChild(HtmlElement parent, string name)
		{
			if (parent == null)
				return null;

			foreach (var child in parent.Children)
			{
				if (child.Is(name))
					return child;
			}

			return null;
		}

		/// <summary>
		/// Strips and collapses ASCII white space, as the DOM does for the document title.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>The collapsed text.</returns>
		internal static string CollapseWhitespace(string text)
		{
			var sb = new StringBuilder(text.Length);
			var space = false;

			foreach (var c in text)
			{
				if (c == ' ' || c == '\t' || c == '\n' || c == '\f' || c == '\r')
				{
					space = sb.Length > 0;
					continue;
				}

				if (space)
					sb.Append(' ');

				sb.Append(c);
				space = false;
			}

			return sb.ToString();
		}

		#endregion
	}
}

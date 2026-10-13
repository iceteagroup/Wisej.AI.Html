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
using System.Collections.Generic;
using Wisej.AI.Html.Css;

namespace Wisej.AI.Html.Dom
{
	/// <summary>
	/// An element of an HTML document, in the HTML, SVG or MathML namespace.
	/// </summary>
	public sealed class HtmlElement : HtmlNode
	{
		// the classes parsed from the class attribute, cached until the attribute changes.
		private string[] _classList;
		private string _classSource;

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlElement"/> class.
		/// </summary>
		/// <param name="ownerDocument">The document that owns the element.</param>
		/// <param name="localName">The local name of the element.</param>
		/// <param name="namespaceUri">The namespace of the element.</param>
		internal HtmlElement(HtmlDocument ownerDocument, string localName, string namespaceUri)
			: base(ownerDocument)
		{
			this.LocalName = localName;
			this.NamespaceUri = namespaceUri;
			this.Attributes = new HtmlAttributeCollection();

			if (localName == "template" && namespaceUri == HtmlNamespaces.Html)
				this.Content = new HtmlDocumentFragment(ownerDocument);
		}

		#region Properties

		/// <inheritdoc/>
		public override HtmlNodeType NodeType => HtmlNodeType.Element;

		/// <inheritdoc/>
		public override string NodeName => this.LocalName;

		/// <summary>
		/// Gets the name of the element. HTML element names are lowercase; SVG and MathML element names
		/// keep their canonical case, such as <c>foreignObject</c>.
		/// </summary>
		public string Name => this.LocalName;

		/// <summary>
		/// Gets the local name of the element.
		/// </summary>
		public string LocalName { get; }

		/// <summary>
		/// Gets the namespace of the element: <see cref="HtmlNamespaces.Html"/>, <see cref="HtmlNamespaces.Svg"/> or <see cref="HtmlNamespaces.MathML"/>.
		/// </summary>
		public string NamespaceUri { get; }

		/// <summary>
		/// Gets the attributes of the element.
		/// </summary>
		public HtmlAttributeCollection Attributes { get; }

		/// <summary>
		/// Gets the value of the <c>id</c> attribute, or an empty string.
		/// </summary>
		public string Id => GetAttribute("id") ?? "";

		/// <summary>
		/// Gets the value of the <c>class</c> attribute, or an empty string.
		/// </summary>
		public string ClassName => GetAttribute("class") ?? "";

		/// <summary>
		/// Gets the classes listed in the <c>class</c> attribute.
		/// </summary>
		public IReadOnlyList<string> ClassList
		{
			get
			{
				var source = this.ClassName;
				if (_classList == null || !ReferenceEquals(source, _classSource))
				{
					_classSource = source;
					_classList = source.Length == 0
						? Array.Empty<string>()
						: source.Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);
				}

				return _classList;
			}
		}

		/// <summary>
		/// Gets the content of a <c>&lt;template&gt;</c> element, which is not part of the document tree,
		/// or <c>null</c> for every other element.
		/// </summary>
		public HtmlDocumentFragment Content { get; }

		/// <summary>
		/// Gets whether the element is in the HTML namespace.
		/// </summary>
		public bool IsHtml => this.NamespaceUri == HtmlNamespaces.Html;

		/// <inheritdoc/>
		public override string TextContent
		{
			get
			{
				// fast path for the common leaf element containing a single text node.
				if (this.FirstChild != null && this.FirstChild == this.LastChild && this.FirstChild is HtmlText text)
					return text.Data;

				return base.TextContent;
			}
		}

		/// <summary>
		/// The characters that separate the tokens of the <c>class</c> attribute.
		/// </summary>
		internal static readonly char[] Whitespace = { ' ', '\t', '\n', '\f', '\r' };

		#endregion

		#region Attributes

		/// <summary>
		/// Returns the value of an attribute.
		/// </summary>
		/// <param name="name">The qualified name of the attribute.</param>
		/// <returns>The value of the attribute, or <c>null</c> when the element does not have it.</returns>
		public string GetAttribute(string name)
		{
			return this.Attributes.Find(name)?.Value;
		}

		/// <summary>
		/// Determines whether the element has an attribute.
		/// </summary>
		/// <param name="name">The qualified name of the attribute.</param>
		/// <returns><c>true</c> when the attribute exists, even with an empty value.</returns>
		public bool HasAttribute(string name)
		{
			return this.Attributes.Find(name) != null;
		}

		/// <summary>
		/// Sets the value of an attribute, adding the attribute when it does not exist.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <c>null</c>.</exception>
		public void SetAttribute(string name, string value)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var attribute = this.Attributes.Find(name);
			if (attribute != null)
				attribute.Value = value;
			else
				this.Attributes.Add(new HtmlAttribute(this.IsHtml ? name.ToLowerInvariant() : name, value));
		}

		/// <summary>
		/// Removes an attribute.
		/// </summary>
		/// <param name="name">The qualified name of the attribute.</param>
		/// <returns><c>true</c> when the attribute was removed, <c>false</c> when the element did not have it.</returns>
		public bool RemoveAttribute(string name)
		{
			var attribute = this.Attributes.Find(name);
			if (attribute == null)
				return false;

			this.Attributes.Remove(attribute);
			return true;
		}

		/// <summary>
		/// Determines whether the <c>class</c> attribute contains a class.
		/// </summary>
		/// <param name="className">The class name, compared case sensitively.</param>
		/// <returns><c>true</c> when the element has the class.</returns>
		public bool HasClass(string className)
		{
			var list = this.ClassList;
			for (var i = 0; i < list.Count; i++)
			{
				if (list[i] == className)
					return true;
			}

			return false;
		}

		#endregion

		#region Selectors

		/// <summary>
		/// Determines whether the element matches a CSS selector.
		/// </summary>
		/// <param name="selectors">A CSS selector list.</param>
		/// <returns><c>true</c> when the element matches at least one of the selectors.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="selectors"/> is <c>null</c>.</exception>
		/// <exception cref="FormatException">Thrown when <paramref name="selectors"/> is not a valid or supported selector.</exception>
		public bool Matches(string selectors)
		{
			return SelectorParser.Parse(selectors).Matches(this);
		}

		/// <summary>
		/// Returns the closest inclusive ancestor of the element that matches a CSS selector.
		/// </summary>
		/// <param name="selectors">A CSS selector list.</param>
		/// <returns>The element itself or its nearest ancestor that matches, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="selectors"/> is <c>null</c>.</exception>
		/// <exception cref="FormatException">Thrown when <paramref name="selectors"/> is not a valid or supported selector.</exception>
		public HtmlElement Closest(string selectors)
		{
			var list = SelectorParser.Parse(selectors);

			for (var element = this; element != null; element = element.ParentElement)
			{
				if (list.Matches(element))
					return element;
			}

			return null;
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Determines whether the element is the HTML element with the specified name.
		/// </summary>
		/// <param name="localName">The lowercase local name.</param>
		/// <returns><c>true</c> when the element is in the HTML namespace and has the specified name.</returns>
		internal bool Is(string localName)
		{
			return this.LocalName == localName && this.NamespaceUri == HtmlNamespaces.Html;
		}

		/// <summary>
		/// Creates a copy of the element without its children, as required by the tree builder when it
		/// reconstructs formatting elements.
		/// </summary>
		/// <returns>The new element.</returns>
		internal HtmlElement CloneWithoutChildren()
		{
			var clone = new HtmlElement(this.OwnerDocument, this.LocalName, this.NamespaceUri);

			foreach (var attribute in this.Attributes)
				clone.Attributes.Add(new HtmlAttribute(attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, attribute.Value));

			return clone;
		}

		#endregion

		/// <summary>
		/// Returns the start tag of the element, for debugging.
		/// </summary>
		/// <returns>A string such as <c>&lt;div class="x"&gt;</c>.</returns>
		public override string ToString()
		{
			var text = "<" + this.LocalName;
			foreach (var attribute in this.Attributes)
				text += " " + attribute;

			return text + ">";
		}
	}
}

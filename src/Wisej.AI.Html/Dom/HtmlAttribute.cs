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
using System.Collections;
using System.Collections.Generic;

namespace Wisej.AI.Html.Dom
{
	/// <summary>
	/// An attribute of an <see cref="HtmlElement"/>.
	/// </summary>
	public sealed class HtmlAttribute
	{
		// the value, never null.
		private string _value;

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlAttribute"/> class in no namespace.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <c>null</c>.</exception>
		public HtmlAttribute(string name, string value)
			: this(null, name, null, value)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlAttribute"/> class.
		/// </summary>
		/// <param name="prefix">The namespace prefix, such as <c>xlink</c>, or <c>null</c>.</param>
		/// <param name="localName">The local name of the attribute.</param>
		/// <param name="namespaceUri">The namespace of the attribute, or <c>null</c>.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="localName"/> is <c>null</c>.</exception>
		public HtmlAttribute(string prefix, string localName, string namespaceUri, string value)
		{
			this.LocalName = localName ?? throw new ArgumentNullException(nameof(localName));
			this.Prefix = prefix;
			this.NamespaceUri = namespaceUri;
			this.Value = value ?? "";
		}

		/// <summary>
		/// Gets the qualified name of the attribute, such as <c>href</c> or <c>xlink:href</c>.
		/// </summary>
		public string Name => this.Prefix == null ? this.LocalName : this.Prefix + ":" + this.LocalName;

		/// <summary>
		/// Gets the local name of the attribute.
		/// </summary>
		public string LocalName { get; }

		/// <summary>
		/// Gets the namespace prefix of the attribute, or <c>null</c>.
		/// </summary>
		public string Prefix { get; }

		/// <summary>
		/// Gets the namespace of the attribute, or <c>null</c> for attributes in no namespace (all HTML attributes).
		/// </summary>
		public string NamespaceUri { get; }

		/// <summary>
		/// Gets or sets the value of the attribute. Never <c>null</c>.
		/// </summary>
		public string Value
		{
			get => _value;
			set => _value = value ?? "";
		}

		/// <summary>
		/// Returns the attribute as <c>name="value"</c>.
		/// </summary>
		/// <returns>A string representing the attribute.</returns>
		public override string ToString()
		{
			return this.Name + "=\"" + this.Value + "\"";
		}
	}

	/// <summary>
	/// The attributes of an <see cref="HtmlElement"/>, in source order.
	/// </summary>
	/// <remarks>
	/// Attribute names created by the HTML parser are lowercase; lookups by name are ordinal and also
	/// match lowercase names when the requested name contains uppercase letters, as in the DOM.
	/// </remarks>
	public sealed class HtmlAttributeCollection : IReadOnlyList<HtmlAttribute>
	{
		// the attributes, in source order.
		private readonly List<HtmlAttribute> _list = new List<HtmlAttribute>();

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlAttributeCollection"/> class.
		/// </summary>
		internal HtmlAttributeCollection()
		{
		}

		/// <summary>
		/// Gets the number of attributes.
		/// </summary>
		public int Count => _list.Count;

		/// <summary>
		/// Gets the attribute at the specified index.
		/// </summary>
		/// <param name="index">The index of the attribute.</param>
		/// <returns>The attribute.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is out of range.</exception>
		public HtmlAttribute this[int index] => _list[index];

		/// <summary>
		/// Gets the attribute with the specified qualified name, or <c>null</c>.
		/// </summary>
		/// <param name="name">The qualified name of the attribute.</param>
		/// <returns>The attribute, or <c>null</c> when the element does not have it.</returns>
		public HtmlAttribute this[string name] => Find(name);

		/// <summary>
		/// Determines whether the collection contains an attribute with the specified qualified name.
		/// </summary>
		/// <param name="name">The qualified name of the attribute.</param>
		/// <returns><c>true</c> when the attribute exists.</returns>
		public bool Contains(string name) => Find(name) != null;

		/// <summary>
		/// Returns an enumerator over the attributes.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public IEnumerator<HtmlAttribute> GetEnumerator() => _list.GetEnumerator();

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

		/// <summary>
		/// Finds an attribute by qualified name.
		/// </summary>
		/// <param name="name">The qualified name.</param>
		/// <returns>The attribute, or <c>null</c>.</returns>
		internal HtmlAttribute Find(string name)
		{
			if (name == null)
				return null;

			// the parser lowercases the names, so the common lookup is a direct ordinal match.
			for (var i = 0; i < _list.Count; i++)
			{
				if (_list[i].Name == name)
					return _list[i];
			}

			for (var i = 0; i < _list.Count; i++)
			{
				if (String.Equals(_list[i].Name, name, StringComparison.OrdinalIgnoreCase))
					return _list[i];
			}

			return null;
		}

		/// <summary>
		/// Adds an attribute at the end of the collection.
		/// </summary>
		/// <param name="attribute">The attribute.</param>
		internal void Add(HtmlAttribute attribute) => _list.Add(attribute);

		/// <summary>
		/// Removes an attribute.
		/// </summary>
		/// <param name="attribute">The attribute.</param>
		internal void Remove(HtmlAttribute attribute) => _list.Remove(attribute);
	}
}

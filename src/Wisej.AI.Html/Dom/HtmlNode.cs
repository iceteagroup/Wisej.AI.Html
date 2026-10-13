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
using System.Text;
using Wisej.AI.Html.Css;

namespace Wisej.AI.Html.Dom
{
	/// <summary>
	/// The base class of every node of an HTML document tree.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The tree follows the DOM model produced by browsers: nodes are linked to their parent and siblings,
	/// so navigation and insertion are constant time operations.
	/// </para>
	/// <para>
	/// Nodes are not thread safe. A tree can be read concurrently by multiple threads as long as no thread modifies it.
	/// </para>
	/// </remarks>
	public abstract class HtmlNode
	{
		// the cached array returned by ChildNodes, cleared when the children change.
		private HtmlNode[] _childNodes;

		// the 1-based positions of the child elements, among all and among their type, and the totals,
		// computed for the structural selectors and cleared when the children change.
		private Dictionary<HtmlElement, (int Index, int TypeIndex)> _positions;
		private Dictionary<(string, string), int> _typeCounts;
		private int _elementCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlNode"/> class.
		/// </summary>
		/// <param name="ownerDocument">The document that owns the node, or <c>null</c> for a document.</param>
		internal HtmlNode(HtmlDocument ownerDocument)
		{
			this.OwnerDocument = ownerDocument;
		}

		#region Properties

		/// <summary>
		/// Gets the type of the node.
		/// </summary>
		public abstract HtmlNodeType NodeType { get; }

		/// <summary>
		/// Gets the name of the node: the qualified name of an element, <c>#text</c>, <c>#comment</c>,
		/// <c>#document</c>, <c>#document-fragment</c> or the name of a document type.
		/// </summary>
		public abstract string NodeName { get; }

		/// <summary>
		/// Gets the document that owns the node. A document returns itself.
		/// </summary>
		public HtmlDocument OwnerDocument { get; internal set; }

		/// <summary>
		/// Gets the parent of the node, or <c>null</c> when the node is not attached to a tree.
		/// </summary>
		public HtmlNode ParentNode { get; private set; }

		/// <summary>
		/// Gets the parent of the node when it is an element, otherwise <c>null</c>.
		/// </summary>
		public HtmlElement ParentElement => this.ParentNode as HtmlElement;

		/// <summary>
		/// Gets the first child of the node, or <c>null</c>.
		/// </summary>
		public HtmlNode FirstChild { get; private set; }

		/// <summary>
		/// Gets the last child of the node, or <c>null</c>.
		/// </summary>
		public HtmlNode LastChild { get; private set; }

		/// <summary>
		/// Gets the node immediately following this node in its parent, or <c>null</c>.
		/// </summary>
		public HtmlNode NextSibling { get; private set; }

		/// <summary>
		/// Gets the node immediately preceding this node in its parent, or <c>null</c>.
		/// </summary>
		public HtmlNode PreviousSibling { get; private set; }

		/// <summary>
		/// Gets whether the node has children.
		/// </summary>
		public bool HasChildNodes => this.FirstChild != null;

		/// <summary>
		/// Gets the number of children of the node.
		/// </summary>
		public int ChildCount { get; private set; }

		/// <summary>
		/// Gets the children of the node, in document order.
		/// </summary>
		/// <remarks>
		/// The list is a snapshot: it does not change when the tree is modified afterwards.
		/// </remarks>
		public IReadOnlyList<HtmlNode> ChildNodes
		{
			get
			{
				if (_childNodes == null)
				{
					var array = new HtmlNode[this.ChildCount];
					var i = 0;
					for (var child = this.FirstChild; child != null; child = child.NextSibling)
						array[i++] = child;

					_childNodes = array;
				}

				return _childNodes;
			}
		}

		/// <summary>
		/// Gets the child elements of the node, in document order.
		/// </summary>
		public IEnumerable<HtmlElement> Children
		{
			get
			{
				for (var child = this.FirstChild; child != null; child = child.NextSibling)
				{
					if (child is HtmlElement element)
						yield return element;
				}
			}
		}

		/// <summary>
		/// Gets the text of the node: the data of a text or comment node, or the concatenation of all the
		/// descendant text nodes of an element, a document or a fragment.
		/// </summary>
		/// <remarks>
		/// The text is returned as written in the source, without the white space processing applied by
		/// browsers when the content is rendered. Use <see cref="HtmlExtractor"/> to obtain the rendered text.
		/// </remarks>
		public virtual string TextContent
		{
			get
			{
				var sb = new StringBuilder();
				foreach (var node in Descendants())
				{
					if (node is HtmlText text)
						sb.Append(text.Data);
				}

				return sb.ToString();
			}
		}

		/// <summary>
		/// Gets the HTML serialization of the node and its descendants.
		/// </summary>
		public string OuterHtml
		{
			get
			{
				var sb = new StringBuilder();
				HtmlSerializer.SerializeNode(this, sb);
				return sb.ToString();
			}
		}

		/// <summary>
		/// Gets the HTML serialization of the children of the node.
		/// </summary>
		public string InnerHtml
		{
			get
			{
				var sb = new StringBuilder();
				HtmlSerializer.SerializeChildren(this, sb);
				return sb.ToString();
			}
		}

		/// <summary>
		/// Gets whether the node can have children.
		/// </summary>
		internal virtual bool CanHaveChildren => true;

		#endregion

		#region Navigation

		/// <summary>
		/// Returns the descendants of the node in document order (depth first, pre-order).
		/// </summary>
		/// <returns>The descendant nodes.</returns>
		/// <remarks>
		/// The traversal is iterative, so it works on trees of any depth. The content of <c>&lt;template&gt;</c>
		/// elements is not part of the tree and is not returned.
		/// </remarks>
		public IEnumerable<HtmlNode> Descendants()
		{
			var node = this.FirstChild;
			while (node != null)
			{
				yield return node;
				node = NextInTree(node);
			}
		}

		/// <summary>
		/// Returns the descendant elements of the node in document order.
		/// </summary>
		/// <returns>The descendant elements.</returns>
		public IEnumerable<HtmlElement> DescendantElements()
		{
			foreach (var node in Descendants())
			{
				if (node is HtmlElement element)
					yield return element;
			}
		}

		/// <summary>
		/// Returns the ancestors of the node, starting with its parent.
		/// </summary>
		/// <returns>The ancestor nodes.</returns>
		public IEnumerable<HtmlNode> Ancestors()
		{
			for (var node = this.ParentNode; node != null; node = node.ParentNode)
				yield return node;
		}

		/// <summary>
		/// Returns the node that follows <paramref name="node"/> in a pre-order traversal of this subtree.
		/// </summary>
		/// <param name="node">The current node, a descendant of this node.</param>
		/// <returns>The next node, or <c>null</c> at the end of the subtree.</returns>
		internal HtmlNode NextInTree(HtmlNode node)
		{
			if (node.FirstChild != null)
				return node.FirstChild;

			return NextSkippingChildren(node);
		}

		/// <summary>
		/// Returns the node that follows <paramref name="node"/> in a pre-order traversal of this subtree,
		/// skipping the descendants of <paramref name="node"/>.
		/// </summary>
		/// <param name="node">The current node, a descendant of this node.</param>
		/// <returns>The next node, or <c>null</c> at the end of the subtree.</returns>
		internal HtmlNode NextSkippingChildren(HtmlNode node)
		{
			while (node != null && node != this)
			{
				if (node.NextSibling != null)
					return node.NextSibling;

				node = node.ParentNode;
			}

			return null;
		}

		/// <summary>
		/// Returns the descendant elements with the specified name.
		/// </summary>
		/// <param name="name">The qualified name of the elements, compared case insensitively for HTML elements, or <c>*</c> for all elements.</param>
		/// <returns>The matching elements, in document order.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <c>null</c>.</exception>
		public IReadOnlyList<HtmlElement> GetElementsByTagName(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var lower = name.ToLowerInvariant();
			var result = new List<HtmlElement>();

			foreach (var element in DescendantElements())
			{
				if (name == "*"
					|| (element.NamespaceUri == HtmlNamespaces.Html && element.Name == lower)
					|| (element.NamespaceUri != HtmlNamespaces.Html && element.Name == name))
				{
					result.Add(element);
				}
			}

			return result;
		}

		/// <summary>
		/// Returns the first descendant element that matches a CSS selector.
		/// </summary>
		/// <param name="selectors">A CSS selector list, such as <c>"table.data td:first-child"</c>.</param>
		/// <returns>The first matching element in document order, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="selectors"/> is <c>null</c>.</exception>
		/// <exception cref="FormatException">Thrown when <paramref name="selectors"/> is not a valid or supported selector.</exception>
		/// <remarks>
		/// Type, universal, id, class and attribute selectors, the descendant, child, next sibling and
		/// subsequent sibling combinators, and the structural pseudo-classes (<c>:first-child</c>,
		/// <c>:nth-child()</c>, <c>:not()</c>, <c>:is()</c>, <c>:has()</c>, ...) are supported.
		/// </remarks>
		public HtmlElement QuerySelector(string selectors)
		{
			var list = SelectorParser.Parse(selectors);

			foreach (var element in DescendantElements())
			{
				if (list.Matches(element))
					return element;
			}

			return null;
		}

		/// <summary>
		/// Returns all the descendant elements that match a CSS selector.
		/// </summary>
		/// <param name="selectors">A CSS selector list, such as <c>"h1, h2, h3"</c>.</param>
		/// <returns>The matching elements, in document order.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="selectors"/> is <c>null</c>.</exception>
		/// <exception cref="FormatException">Thrown when <paramref name="selectors"/> is not a valid or supported selector.</exception>
		/// <remarks>See <see cref="QuerySelector(string)"/> for the supported selectors.</remarks>
		public IReadOnlyList<HtmlElement> QuerySelectorAll(string selectors)
		{
			var list = SelectorParser.Parse(selectors);
			var result = new List<HtmlElement>();

			foreach (var element in DescendantElements())
			{
				if (list.Matches(element))
					result.Add(element);
			}

			return result;
		}

		/// <summary>
		/// Returns the 1-based position of a child element among the child elements of this node.
		/// </summary>
		/// <param name="child">The child element.</param>
		/// <param name="ofType">Whether only the elements with the same name and namespace are counted.</param>
		/// <param name="fromEnd">Whether the position is counted from the last child.</param>
		/// <returns>The position.</returns>
		internal int GetElementPosition(HtmlElement child, bool ofType, bool fromEnd)
		{
			if (_positions == null)
			{
				_positions = new Dictionary<HtmlElement, (int, int)>();
				_typeCounts = new Dictionary<(string, string), int>();
				_elementCount = 0;

				for (var node = this.FirstChild; node != null; node = node.NextSibling)
				{
					if (!(node is HtmlElement element))
						continue;

					var type = (element.LocalName, element.NamespaceUri);
					_typeCounts.TryGetValue(type, out var count);
					_typeCounts[type] = ++count;
					_positions[element] = (++_elementCount, count);
				}
			}

			if (!_positions.TryGetValue(child, out var position))
				return 1;

			if (!ofType)
				return fromEnd ? _elementCount - position.Index + 1 : position.Index;

			return fromEnd ? _typeCounts[(child.LocalName, child.NamespaceUri)] - position.TypeIndex + 1 : position.TypeIndex;
		}

		#endregion

		#region Mutation

		/// <summary>
		/// Appends a node to the children of this node. A node that is already in a tree is moved.
		/// </summary>
		/// <param name="child">The node to append.</param>
		/// <returns>The appended node.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="child"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when this node cannot have children, or <paramref name="child"/> is a document or an ancestor of this node.</exception>
		public HtmlNode AppendChild(HtmlNode child)
		{
			return InsertBefore(child, null);
		}

		/// <summary>
		/// Inserts a node before a child of this node. A node that is already in a tree is moved.
		/// </summary>
		/// <param name="child">The node to insert.</param>
		/// <param name="reference">The child before which <paramref name="child"/> is inserted, or <c>null</c> to append it.</param>
		/// <returns>The inserted node.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="child"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="reference"/> is not a child of this node.</exception>
		/// <exception cref="InvalidOperationException">Thrown when this node cannot have children, or <paramref name="child"/> is a document or an ancestor of this node.</exception>
		public HtmlNode InsertBefore(HtmlNode child, HtmlNode reference)
		{
			if (child == null)
				throw new ArgumentNullException(nameof(child));
			if (!this.CanHaveChildren)
				throw new InvalidOperationException($"A {this.NodeType} node cannot have children.");
			if (child is HtmlDocument)
				throw new InvalidOperationException("A document cannot be inserted into another node.");
			if (reference != null && reference.ParentNode != this)
				throw new ArgumentException("The reference node is not a child of this node.", nameof(reference));

			for (var node = this; node != null; node = node.ParentNode)
			{
				if (node == child)
					throw new InvalidOperationException("A node cannot be inserted into itself or into one of its descendants.");
			}

			if (child == reference)
				return child;

			// a fragment inserts its children instead of itself.
			if (child is HtmlDocumentFragment fragment)
			{
				while (fragment.FirstChild != null)
					InsertBefore(fragment.FirstChild, reference);

				return child;
			}

			child.ParentNode?.Unlink(child);
			Link(child, reference);

			return child;
		}

		/// <summary>
		/// Removes a child of this node.
		/// </summary>
		/// <param name="child">The child to remove.</param>
		/// <returns>The removed node.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="child"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="child"/> is not a child of this node.</exception>
		public HtmlNode RemoveChild(HtmlNode child)
		{
			if (child == null)
				throw new ArgumentNullException(nameof(child));
			if (child.ParentNode != this)
				throw new ArgumentException("The node is not a child of this node.", nameof(child));

			Unlink(child);
			return child;
		}

		/// <summary>
		/// Removes the node from its parent. Does nothing when the node has no parent.
		/// </summary>
		public void Remove()
		{
			this.ParentNode?.Unlink(this);
		}

		/// <summary>
		/// Links a detached node into the children of this node, without validation.
		/// </summary>
		/// <param name="child">The detached node.</param>
		/// <param name="reference">The child before which the node is inserted, or <c>null</c> to append it.</param>
		internal void Link(HtmlNode child, HtmlNode reference)
		{
			child.ParentNode = this;

			if (reference == null)
			{
				child.PreviousSibling = this.LastChild;
				child.NextSibling = null;

				if (this.LastChild != null)
					this.LastChild.NextSibling = child;
				else
					this.FirstChild = child;

				this.LastChild = child;
			}
			else
			{
				child.PreviousSibling = reference.PreviousSibling;
				child.NextSibling = reference;

				if (reference.PreviousSibling != null)
					reference.PreviousSibling.NextSibling = child;
				else
					this.FirstChild = child;

				reference.PreviousSibling = child;
			}

			this.ChildCount++;
			_childNodes = null;
			_positions = null;
		}

		/// <summary>
		/// Unlinks a child of this node, without validation.
		/// </summary>
		/// <param name="child">The child to remove.</param>
		internal void Unlink(HtmlNode child)
		{
			if (child.PreviousSibling != null)
				child.PreviousSibling.NextSibling = child.NextSibling;
			else
				this.FirstChild = child.NextSibling;

			if (child.NextSibling != null)
				child.NextSibling.PreviousSibling = child.PreviousSibling;
			else
				this.LastChild = child.PreviousSibling;

			child.ParentNode = null;
			child.PreviousSibling = null;
			child.NextSibling = null;

			this.ChildCount--;
			_childNodes = null;
			_positions = null;
		}

		#endregion

		/// <summary>
		/// Returns a short description of the node, for debugging.
		/// </summary>
		/// <returns>The name of the node.</returns>
		public override string ToString()
		{
			return this.NodeName;
		}
	}
}

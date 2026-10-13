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
using Wisej.AI.Html.Dom;

namespace Wisej.AI.Html.Parsing
{
	/// <summary>
	/// Builds the document tree from the tokens, following the tree construction algorithm of the
	/// HTML standard (13.2.6).
	/// </summary>
	/// <remarks>
	/// <para>
	/// This file contains the shared machinery: the stack of open elements, the list of active formatting
	/// elements, the insertion of nodes (with foster parenting), the scope checks, the adoption agency
	/// algorithm and the reset of the insertion mode. The insertion modes are implemented in the other
	/// parts of the class.
	/// </para>
	/// <para>
	/// Scripts are never executed; the parser behaves as a browser that does not run them, with the
	/// scripting flag taken from <see cref="HtmlParserOptions.ScriptingEnabled"/>.
	/// </para>
	/// </remarks>
	internal sealed partial class HtmlTreeBuilder : IHtmlTokenSink
	{
		#region Nested types

		/// <summary>
		/// The insertion modes (13.2.4.1).
		/// </summary>
		private enum InsertionMode
		{
			Initial,
			BeforeHtml,
			BeforeHead,
			InHead,
			InHeadNoscript,
			AfterHead,
			InBody,
			Text,
			InTable,
			InTableText,
			InCaption,
			InColumnGroup,
			InTableBody,
			InRow,
			InCell,
			InTemplate,
			AfterBody,
			InFrameset,
			AfterFrameset,
			AfterAfterBody,
			AfterAfterFrameset,
		}

		/// <summary>
		/// The kinds of element scope (13.2.4.2).
		/// </summary>
		private enum Scope
		{
			Default,
			ListItem,
			Button,
			Table,
			Select,
		}

		#endregion

		#region Fields

		// the document being built and the parser options.
		private readonly HtmlDocument _document;
		private readonly HtmlParserOptions _options;
		private readonly bool _scripting;

		// the tokenizer feeding this builder.
		private HtmlTokenizer _tokenizer;

		// the current and the original insertion mode (used by the text and in table text modes).
		private InsertionMode _mode;
		private InsertionMode _originalMode;

		// the stack of open elements, the current node last.
		private readonly List<HtmlElement> _stack = new List<HtmlElement>();

		// the list of active formatting elements; null entries are markers.
		private readonly List<HtmlElement> _formatting = new List<HtmlElement>();

		// the stack of template insertion modes.
		private readonly List<InsertionMode> _templateModes = new List<InsertionMode>();

		// the head and form element pointers.
		private HtmlElement _head;
		private HtmlElement _form;

		// the frameset-ok flag: cleared by content that cannot be replaced by a frameset.
		private bool _framesetOk = true;

		// whether nodes inserted in a table context are foster parented before the table.
		private bool _fosterParenting;

		// the context element of the fragment parsing algorithm, or null when parsing a document.
		private HtmlElement _context;

		// the character tokens collected in the "in table text" mode.
		private readonly List<string> _pendingTableText = new List<string>();

		// set after <pre>, <listing> and <textarea>: a newline immediately following the start tag is dropped.
		private bool _skipNextNewline;

		// set when a <selectedcontent> element exists: popped options are then mirrored into it.
		private bool _hasSelectedContent;

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlTreeBuilder"/> class.
		/// </summary>
		/// <param name="document">The empty document that receives the nodes.</param>
		/// <param name="options">The parser options.</param>
		public HtmlTreeBuilder(HtmlDocument document, HtmlParserOptions options)
		{
			_document = document;
			_options = options;
			_scripting = options.ScriptingEnabled;
		}

		#region Entry points

		/// <summary>
		/// Parses a complete document.
		/// </summary>
		/// <param name="input">The markup.</param>
		public void ParseDocument(string input)
		{
			_tokenizer = new HtmlTokenizer(input, this);
			_mode = InsertionMode.Initial;
			_tokenizer.Run();

			// stop parsing: pop every open element (13.2.7).
			while (_stack.Count > 0)
				Pop();
		}

		/// <summary>
		/// Parses a fragment with the HTML fragment parsing algorithm (13.4).
		/// </summary>
		/// <param name="input">The markup.</param>
		/// <param name="context">The context element.</param>
		/// <returns>The parsed nodes.</returns>
		public HtmlDocumentFragment ParseFragment(string input, HtmlElement context)
		{
			_context = context;
			_tokenizer = new HtmlTokenizer(input, this);

			if (context.IsHtml)
			{
				switch (context.LocalName)
				{
					case "title":
					case "textarea":
						_tokenizer.State = TokenizerState.Rcdata;
						break;

					case "style":
					case "xmp":
					case "iframe":
					case "noembed":
					case "noframes":
						_tokenizer.State = TokenizerState.Rawtext;
						break;

					case "script":
						_tokenizer.State = TokenizerState.ScriptData;
						break;

					case "noscript":
						if (_scripting)
							_tokenizer.State = TokenizerState.Rawtext;
						break;

					case "plaintext":
						_tokenizer.State = TokenizerState.Plaintext;
						break;
				}
			}

			var root = new HtmlElement(_document, "html", HtmlNamespaces.Html);
			_document.Link(root, null);
			_stack.Add(root);

			if (context.Is("template"))
				_templateModes.Add(InsertionMode.InTemplate);

			ResetInsertionMode();

			for (var node = (HtmlNode)context; node != null; node = node.ParentNode)
			{
				if (node is HtmlElement element && element.Is("form"))
				{
					_form = element;
					break;
				}
			}

			_tokenizer.Run();

			// stop parsing: pop every open element (13.2.7).
			while (_stack.Count > 0)
				Pop();

			var fragment = new HtmlDocumentFragment(_document);
			while (root.FirstChild != null)
			{
				var child = root.FirstChild;
				root.Unlink(child);
				fragment.Link(child, null);
			}

			return fragment;
		}

		#endregion

		#region Dispatcher

		/// <inheritdoc/>
		public bool AllowCdata
		{
			get
			{
				var node = this.AdjustedCurrentNode;
				return node != null && !node.IsHtml;
			}
		}

		/// <inheritdoc/>
		public void ProcessToken(HtmlToken token)
		{
			if (_skipNextNewline)
			{
				_skipNextNewline = false;

				if (token.Type == HtmlTokenType.Characters && token.Data.Length > 0 && token.Data[0] == '\n')
				{
					if (token.Data.Length == 1)
						return;

					token = HtmlToken.Characters(token.Data.Substring(1));
				}
			}

			Dispatch(token);
		}

		/// <summary>
		/// Routes a token to the current insertion mode or to the rules for foreign content (13.2.6).
		/// </summary>
		/// <param name="token">The token.</param>
		private void Dispatch(HtmlToken token)
		{
			var node = this.AdjustedCurrentNode;

			if (node == null
				|| node.IsHtml
				|| token.Type == HtmlTokenType.EndOfFile
				|| (ElementCategories.IsMathMLTextIntegrationPoint(node)
					&& (token.Type == HtmlTokenType.Characters || (token.Type == HtmlTokenType.StartTag && token.Name != "mglyph" && token.Name != "malignmark")))
				|| (node.NamespaceUri == HtmlNamespaces.MathML && node.LocalName == "annotation-xml" && token.Type == HtmlTokenType.StartTag && token.Name == "svg")
				|| (ElementCategories.IsHtmlIntegrationPoint(node) && (token.Type == HtmlTokenType.StartTag || token.Type == HtmlTokenType.Characters)))
			{
				Process(_mode, token);
			}
			else
			{
				ProcessForeignContent(token);
			}
		}

		/// <summary>
		/// Processes a token with the rules of an insertion mode.
		/// </summary>
		/// <param name="mode">The insertion mode whose rules apply.</param>
		/// <param name="token">The token.</param>
		private void Process(InsertionMode mode, HtmlToken token)
		{
			switch (mode)
			{
				case InsertionMode.Initial: InitialMode(token); break;
				case InsertionMode.BeforeHtml: BeforeHtmlMode(token); break;
				case InsertionMode.BeforeHead: BeforeHeadMode(token); break;
				case InsertionMode.InHead: InHeadMode(token); break;
				case InsertionMode.InHeadNoscript: InHeadNoscriptMode(token); break;
				case InsertionMode.AfterHead: AfterHeadMode(token); break;
				case InsertionMode.InBody: InBodyMode(token); break;
				case InsertionMode.Text: TextMode(token); break;
				case InsertionMode.InTable: InTableMode(token); break;
				case InsertionMode.InTableText: InTableTextMode(token); break;
				case InsertionMode.InCaption: InCaptionMode(token); break;
				case InsertionMode.InColumnGroup: InColumnGroupMode(token); break;
				case InsertionMode.InTableBody: InTableBodyMode(token); break;
				case InsertionMode.InRow: InRowMode(token); break;
				case InsertionMode.InCell: InCellMode(token); break;
				case InsertionMode.InTemplate: InTemplateMode(token); break;
				case InsertionMode.AfterBody: AfterBodyMode(token); break;
				case InsertionMode.InFrameset: InFramesetMode(token); break;
				case InsertionMode.AfterFrameset: AfterFramesetMode(token); break;
				case InsertionMode.AfterAfterBody: AfterAfterBodyMode(token); break;
				case InsertionMode.AfterAfterFrameset: AfterAfterFramesetMode(token); break;
			}
		}

		/// <summary>
		/// Switches to a new insertion mode and reprocesses the token.
		/// </summary>
		/// <param name="mode">The new insertion mode.</param>
		/// <param name="token">The token.</param>
		private void Reprocess(InsertionMode mode, HtmlToken token)
		{
			_mode = mode;
			Dispatch(token);
		}

		#endregion

		#region Stack of open elements

		/// <summary>
		/// Gets the current node, the bottommost element of the stack of open elements.
		/// </summary>
		private HtmlElement CurrentNode => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

		/// <summary>
		/// Gets the adjusted current node: the context element when parsing a fragment and the stack only
		/// contains the root element, otherwise the current node.
		/// </summary>
		private HtmlElement AdjustedCurrentNode => _context != null && _stack.Count == 1 ? _context : this.CurrentNode;

		/// <summary>
		/// Pops the current node.
		/// </summary>
		private void Pop()
		{
			var element = _stack[_stack.Count - 1];
			_stack.RemoveAt(_stack.Count - 1);

			if (_hasSelectedContent && element.Is("option"))
				MirrorSelectedOption(element);
		}

		/// <summary>
		/// Copies the content of the selected option of a customizable select into its
		/// <c>&lt;selectedcontent&gt;</c> element, when the option is popped ("maybe clone an option into selectedcontent").
		/// </summary>
		/// <param name="option">The popped option.</param>
		private static void MirrorSelectedOption(HtmlElement option)
		{
			HtmlElement select = null;
			for (var node = option.ParentElement; node != null && select == null; node = node.ParentElement)
			{
				if (node.Is("select"))
					select = node;
			}

			if (select == null)
				return;

			HtmlElement selectedContent = null;
			HtmlElement lastSelected = null;
			HtmlElement firstEnabled = null;

			foreach (var element in select.DescendantElements())
			{
				if (element.Is("selectedcontent"))
				{
					selectedContent = selectedContent ?? element;
				}
				else if (element.Is("option"))
				{
					if (element.HasAttribute("selected"))
						lastSelected = element;
					if (firstEnabled == null && !element.HasAttribute("disabled"))
						firstEnabled = element;
				}
			}

			// the selected option is the last one with the selected attribute, or the first enabled one.
			if (selectedContent == null || (lastSelected ?? firstEnabled) != option)
				return;

			while (selectedContent.FirstChild != null)
				selectedContent.Unlink(selectedContent.FirstChild);

			for (var child = option.FirstChild; child != null; child = child.NextSibling)
			{
				var copy = DeepClone(child);
				if (copy != null)
					selectedContent.Link(copy, null);
			}
		}

		/// <summary>
		/// Creates a deep copy of a node.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns>The copy.</returns>
		private static HtmlNode DeepClone(HtmlNode node)
		{
			switch (node)
			{
				case HtmlText text:
					return new HtmlText(text.OwnerDocument, text.Data);

				case HtmlComment comment:
					return new HtmlComment(comment.OwnerDocument, comment.Data);

				case HtmlElement element:
					var clone = element.CloneWithoutChildren();
					for (var child = element.FirstChild; child != null; child = child.NextSibling)
					{
						var copy = DeepClone(child);
						if (copy != null)
							clone.Link(copy, null);
					}

					return clone;

				default:
					return null;
			}
		}

		/// <summary>
		/// Pops elements until an HTML element with the specified name has been popped.
		/// </summary>
		/// <param name="name">The tag name.</param>
		private void PopUntil(string name)
		{
			while (_stack.Count > 0)
			{
				var element = this.CurrentNode;
				Pop();

				if (element.Is(name))
					return;
			}
		}

		/// <summary>
		/// Pops elements until an HTML element with one of the specified names has been popped.
		/// </summary>
		/// <param name="names">The tag names.</param>
		private void PopUntilOneOf(params string[] names)
		{
			while (_stack.Count > 0)
			{
				var element = this.CurrentNode;
				Pop();

				if (element.IsHtml && Array.IndexOf(names, element.LocalName) >= 0)
					return;
			}
		}

		/// <summary>
		/// Pops elements until a specific element has been popped.
		/// </summary>
		/// <param name="element">The element.</param>
		private void PopUntil(HtmlElement element)
		{
			while (_stack.Count > 0)
			{
				var current = this.CurrentNode;
				Pop();

				if (current == element)
					return;
			}
		}

		/// <summary>
		/// Determines whether the current node is the HTML element with the specified name.
		/// </summary>
		/// <param name="name">The tag name.</param>
		/// <returns><c>true</c> when the current node matches.</returns>
		private bool CurrentIs(string name)
		{
			var current = this.CurrentNode;
			return current != null && current.Is(name);
		}

		/// <summary>
		/// Determines whether the stack contains an HTML element with the specified name.
		/// </summary>
		/// <param name="name">The tag name.</param>
		/// <returns><c>true</c> when such an element is open.</returns>
		private bool StackContains(string name)
		{
			for (var i = _stack.Count - 1; i >= 0; i--)
			{
				if (_stack[i].Is(name))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Determines whether the stack has an HTML element with the specified name in a scope.
		/// </summary>
		/// <param name="name">The tag name.</param>
		/// <param name="scope">The kind of scope.</param>
		/// <returns><c>true</c> when the element is in scope.</returns>
		private bool HasInScope(string name, Scope scope = Scope.Default)
		{
			for (var i = _stack.Count - 1; i >= 0; i--)
			{
				var node = _stack[i];
				if (node.Is(name))
					return true;

				if (IsScopeBoundary(node, scope))
					return false;
			}

			return false;
		}

		/// <summary>
		/// Determines whether the stack has an HTML element with one of the specified names in a scope.
		/// </summary>
		/// <param name="names">The tag names.</param>
		/// <param name="scope">The kind of scope.</param>
		/// <returns><c>true</c> when one of the elements is in scope.</returns>
		private bool HasAnyInScope(string[] names, Scope scope = Scope.Default)
		{
			for (var i = _stack.Count - 1; i >= 0; i--)
			{
				var node = _stack[i];
				if (node.IsHtml && Array.IndexOf(names, node.LocalName) >= 0)
					return true;

				if (IsScopeBoundary(node, scope))
					return false;
			}

			return false;
		}

		/// <summary>
		/// Determines whether the stack has a specific element in the default scope.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> when the element is in scope.</returns>
		private bool HasElementInScope(HtmlElement element)
		{
			for (var i = _stack.Count - 1; i >= 0; i--)
			{
				var node = _stack[i];
				if (node == element)
					return true;

				if (IsScopeBoundary(node, Scope.Default))
					return false;
			}

			return false;
		}

		/// <summary>
		/// Determines whether an element ends a scope.
		/// </summary>
		/// <param name="node">The element.</param>
		/// <param name="scope">The kind of scope.</param>
		/// <returns><c>true</c> when the element is a boundary of the scope.</returns>
		private static bool IsScopeBoundary(HtmlElement node, Scope scope)
		{
			switch (scope)
			{
				case Scope.ListItem:
					return ElementCategories.IsDefaultScopeBoundary(node) || node.Is("ol") || node.Is("ul");

				case Scope.Button:
					return ElementCategories.IsDefaultScopeBoundary(node) || node.Is("button");

				case Scope.Table:
					return node.Is("html") || node.Is("table") || node.Is("template");

				case Scope.Select:
					return !(node.Is("optgroup") || node.Is("option"));

				default:
					return ElementCategories.IsDefaultScopeBoundary(node);
			}
		}

		/// <summary>
		/// Pops the elements that have an implied end tag (13.2.6.3).
		/// </summary>
		/// <param name="except">The name of an element that is not closed, or <c>null</c>.</param>
		private void GenerateImpliedEndTags(string except = null)
		{
			while (true)
			{
				var current = this.CurrentNode;
				if (current == null || !ElementCategories.HasImpliedEndTag(current, thoroughly: false) || current.LocalName == except)
					return;

				Pop();
			}
		}

		/// <summary>
		/// Pops the elements that have an implied end tag, including the table elements (13.2.6.3).
		/// </summary>
		private void GenerateAllImpliedEndTagsThoroughly()
		{
			while (this.CurrentNode != null && ElementCategories.HasImpliedEndTag(this.CurrentNode, thoroughly: true))
				Pop();
		}

		/// <summary>
		/// Closes a <c>p</c> element (13.2.6.4.7).
		/// </summary>
		private void ClosePElement()
		{
			GenerateImpliedEndTags("p");
			PopUntil("p");
		}

		/// <summary>
		/// Closes the <c>p</c> element in button scope, if any.
		/// </summary>
		private void ClosePElementInButtonScope()
		{
			if (HasInScope("p", Scope.Button))
				ClosePElement();
		}

		#endregion

		#region Insertion

		/// <summary>
		/// Returns the appropriate place for inserting a node (13.2.6.1), applying foster parenting.
		/// </summary>
		/// <param name="overrideTarget">The target, or <c>null</c> for the current node.</param>
		/// <param name="parent">Receives the parent of the new node.</param>
		/// <param name="before">Receives the node before which the new node is inserted, or <c>null</c> to append.</param>
		private void GetInsertionPlace(HtmlElement overrideTarget, out HtmlNode parent, out HtmlNode before)
		{
			var target = overrideTarget ?? this.CurrentNode;
			before = null;

			if (_fosterParenting && target.IsHtml
				&& (target.LocalName == "table" || target.LocalName == "tbody" || target.LocalName == "tfoot" || target.LocalName == "thead" || target.LocalName == "tr"))
			{
				var lastTemplate = -1;
				var lastTable = -1;
				for (var i = _stack.Count - 1; i >= 0 && (lastTemplate < 0 || lastTable < 0); i--)
				{
					if (lastTemplate < 0 && _stack[i].Is("template"))
						lastTemplate = i;
					if (lastTable < 0 && _stack[i].Is("table"))
						lastTable = i;
				}

				if (lastTemplate >= 0 && (lastTable < 0 || lastTemplate > lastTable))
				{
					parent = _stack[lastTemplate].Content;
					return;
				}

				if (lastTable < 0)
				{
					// fragment case.
					parent = _stack[0];
					return;
				}

				var table = _stack[lastTable];
				if (table.ParentNode != null)
				{
					parent = table.ParentNode;
					before = table;
					return;
				}

				target = _stack[lastTable - 1];
			}

			parent = target;

			if (target.Content != null)
				parent = target.Content;
		}

		/// <summary>
		/// Creates an element for a token (13.2.6.1).
		/// </summary>
		/// <param name="token">The start tag.</param>
		/// <param name="namespaceUri">The namespace of the element.</param>
		/// <returns>The new element.</returns>
		private HtmlElement CreateElement(HtmlToken token, string namespaceUri)
		{
			var element = new HtmlElement(_document, token.Name, namespaceUri);

			foreach (var attribute in token.Attributes)
			{
				if (namespaceUri == HtmlNamespaces.Html)
					element.Attributes.Add(new HtmlAttribute(null, attribute.Name, null, attribute.Value));
				else
					element.Attributes.Add(CreateForeignAttribute(attribute.Name, attribute.Value, namespaceUri));
			}

			return element;
		}

		/// <summary>
		/// Inserts an element at the appropriate place and pushes it onto the stack of open elements.
		/// </summary>
		/// <param name="element">The new element.</param>
		private void InsertElement(HtmlElement element)
		{
			GetInsertionPlace(null, out var parent, out var before);

			// browsers limit the depth of the tree: deeper elements become siblings of their parent.
			if (_stack.Count >= _options.MaximumDepth && parent.ParentNode != null && !(parent.ParentNode is HtmlDocument))
			{
				before = null;
				parent = parent.ParentNode;
			}

			if (!(parent is HtmlDocument) || _document.DocumentElement == null)
				Move(element, parent, before);

			_stack.Add(element);

			if (element.Is("selectedcontent"))
				_hasSelectedContent = true;
		}

		/// <summary>
		/// Inserts an HTML element for a token.
		/// </summary>
		/// <param name="token">The start tag.</param>
		/// <returns>The new element.</returns>
		private HtmlElement InsertHtmlElement(HtmlToken token)
		{
			var element = CreateElement(token, HtmlNamespaces.Html);
			InsertElement(element);
			return element;
		}

		/// <summary>
		/// Inserts an HTML element without attributes, for a tag implied by the markup.
		/// </summary>
		/// <param name="name">The tag name.</param>
		/// <returns>The new element.</returns>
		private HtmlElement InsertHtmlElement(string name)
		{
			return InsertHtmlElement(HtmlToken.StartTag(name));
		}

		/// <summary>
		/// Inserts characters at the appropriate place, merging them with a preceding text node.
		/// </summary>
		/// <param name="data">The characters.</param>
		private void InsertCharacters(string data)
		{
			if (data.Length == 0)
				return;

			GetInsertionPlace(null, out var parent, out var before);

			// text is never inserted directly into the document.
			if (parent is HtmlDocument)
				return;

			var previous = before == null ? parent.LastChild : before.PreviousSibling;
			if (previous is HtmlText text)
			{
				text.Append(data);
				return;
			}

			parent.Link(new HtmlText(_document, data), before);
		}

		/// <summary>
		/// Inserts a comment at the appropriate place, or as the last child of a specific node.
		/// </summary>
		/// <param name="token">The comment token.</param>
		/// <param name="parent">The node that receives the comment, or <c>null</c> for the appropriate place.</param>
		private void InsertComment(HtmlToken token, HtmlNode parent = null)
		{
			HtmlNode before = null;
			if (parent == null)
				GetInsertionPlace(null, out parent, out before);

			parent.Link(new HtmlComment(_document, token.Data), before);
		}

		/// <summary>
		/// Moves a node to a new position, detaching it from its current parent. A move that would
		/// create a cycle (possible only when the depth limit flattened the tree) is ignored.
		/// </summary>
		/// <param name="node">The node to move.</param>
		/// <param name="parent">The new parent.</param>
		/// <param name="before">The node before which the node is inserted, or <c>null</c> to append.</param>
		private static void Move(HtmlNode node, HtmlNode parent, HtmlNode before)
		{
			for (var ancestor = parent; ancestor != null; ancestor = ancestor.ParentNode)
			{
				if (ancestor == node)
					return;
			}

			if (before == node)
				return;

			node.ParentNode?.Unlink(node);
			parent.Link(node, before);
		}

		/// <summary>
		/// Applies the generic raw text or RCDATA element parsing algorithm (13.2.6.2).
		/// </summary>
		/// <param name="token">The start tag.</param>
		/// <param name="state">The tokenizer state: RAWTEXT or RCDATA.</param>
		private void ParseRawText(HtmlToken token, TokenizerState state)
		{
			InsertHtmlElement(token);
			_tokenizer.State = state;
			_originalMode = _mode;
			_mode = InsertionMode.Text;
		}

		#endregion

		#region Active formatting elements

		/// <summary>
		/// Pushes an element onto the list of active formatting elements, applying the Noah's Ark clause:
		/// at most three identical elements after the last marker.
		/// </summary>
		/// <param name="element">The formatting element.</param>
		private void PushFormatting(HtmlElement element)
		{
			var count = 0;
			var earliest = -1;

			for (var i = _formatting.Count - 1; i >= 0; i--)
			{
				var entry = _formatting[i];
				if (entry == null)
					break;

				if (IsSameFormatting(entry, element))
				{
					count++;
					earliest = i;
				}
			}

			if (count >= 3)
				_formatting.RemoveAt(earliest);

			_formatting.Add(element);
		}

		/// <summary>
		/// Determines whether two formatting elements have the same name, namespace and attributes.
		/// </summary>
		/// <param name="a">The first element.</param>
		/// <param name="b">The second element.</param>
		/// <returns><c>true</c> when the elements are equivalent.</returns>
		private static bool IsSameFormatting(HtmlElement a, HtmlElement b)
		{
			if (a.LocalName != b.LocalName || a.NamespaceUri != b.NamespaceUri || a.Attributes.Count != b.Attributes.Count)
				return false;

			foreach (var attribute in a.Attributes)
			{
				var other = b.Attributes.Find(attribute.Name);
				if (other == null || other.Value != attribute.Value || other.NamespaceUri != attribute.NamespaceUri)
					return false;
			}

			return true;
		}

		/// <summary>
		/// Reopens the formatting elements that were implicitly closed (13.2.4.3), so that
		/// "&lt;b&gt;1&lt;p&gt;2" makes "2" bold as well.
		/// </summary>
		private void ReconstructFormatting()
		{
			if (_formatting.Count == 0)
				return;

			var last = _formatting[_formatting.Count - 1];
			if (last == null || _stack.Contains(last))
				return;

			// rewind to the first entry after a marker or an open element.
			var index = _formatting.Count - 1;
			while (index > 0)
			{
				var previous = _formatting[index - 1];
				if (previous == null || _stack.Contains(previous))
					break;

				index--;
			}

			// advance and create.
			for (; index < _formatting.Count; index++)
			{
				var clone = _formatting[index].CloneWithoutChildren();
				InsertElement(clone);
				_formatting[index] = clone;
			}
		}

		/// <summary>
		/// Removes the entries of the list of active formatting elements up to and including the last marker.
		/// </summary>
		private void ClearFormattingToLastMarker()
		{
			while (_formatting.Count > 0)
			{
				var entry = _formatting[_formatting.Count - 1];
				_formatting.RemoveAt(_formatting.Count - 1);

				if (entry == null)
					return;
			}
		}

		/// <summary>
		/// Finds the last element with a name in the list of active formatting elements, after the last marker.
		/// </summary>
		/// <param name="name">The tag name.</param>
		/// <returns>The index of the element, or -1.</returns>
		private int FindFormatting(string name)
		{
			for (var i = _formatting.Count - 1; i >= 0; i--)
			{
				var entry = _formatting[i];
				if (entry == null)
					return -1;

				if (entry.Is(name))
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Runs the adoption agency algorithm (13.2.6.4.7), which repairs misnested formatting elements
		/// such as "&lt;b&gt;1&lt;i&gt;2&lt;/b&gt;3&lt;/i&gt;".
		/// </summary>
		/// <param name="subject">The tag name of the end tag.</param>
		/// <returns><c>false</c> when the token must be handled as "any other end tag".</returns>
		private bool AdoptionAgency(string subject)
		{
			var current = this.CurrentNode;
			if (current.Is(subject) && !_formatting.Contains(current))
			{
				Pop();
				return true;
			}

			for (var outer = 0; outer < 8; outer++)
			{
				var formattingIndex = FindFormatting(subject);
				if (formattingIndex < 0)
					return false;

				var formattingElement = _formatting[formattingIndex];
				var stackIndex = _stack.LastIndexOf(formattingElement);

				if (stackIndex < 0)
				{
					_formatting.RemoveAt(formattingIndex);
					return true;
				}

				if (!HasElementInScope(formattingElement))
					return true;

				// the furthest block is the topmost special element below the formatting element.
				HtmlElement furthestBlock = null;
				var furthestIndex = -1;
				for (var i = stackIndex + 1; i < _stack.Count; i++)
				{
					if (ElementCategories.IsSpecial(_stack[i]))
					{
						furthestBlock = _stack[i];
						furthestIndex = i;
						break;
					}
				}

				if (furthestBlock == null)
				{
					while (_stack.Count > stackIndex)
						Pop();

					_formatting.Remove(formattingElement);
					return true;
				}

				var commonAncestor = _stack[stackIndex - 1];

				// the bookmark is the index before which the new element is inserted.
				var bookmark = formattingIndex;
				var lastNode = furthestBlock;
				var nodeIndex = furthestIndex;

				for (var inner = 1; ; inner++)
				{
					nodeIndex--;
					var node = _stack[nodeIndex];
					if (node == formattingElement)
						break;

					var nodeFormattingIndex = _formatting.IndexOf(node);
					if (inner > 3 && nodeFormattingIndex >= 0)
					{
						_formatting.RemoveAt(nodeFormattingIndex);
						if (nodeFormattingIndex < bookmark)
							bookmark--;

						nodeFormattingIndex = -1;
					}

					if (nodeFormattingIndex < 0)
					{
						_stack.RemoveAt(nodeIndex);
						continue;
					}

					var clone = node.CloneWithoutChildren();
					_formatting[nodeFormattingIndex] = clone;
					_stack[nodeIndex] = clone;

					if (lastNode == furthestBlock)
						bookmark = nodeFormattingIndex + 1;

					Move(lastNode, clone, null);
					lastNode = clone;
				}

				GetInsertionPlace(commonAncestor, out var parent, out var before);
				Move(lastNode, parent, before);

				var newElement = formattingElement.CloneWithoutChildren();
				while (furthestBlock.FirstChild != null)
				{
					var child = furthestBlock.FirstChild;
					furthestBlock.Unlink(child);
					newElement.Link(child, null);
				}

				furthestBlock.Link(newElement, null);

				var oldIndex = _formatting.IndexOf(formattingElement);
				_formatting.RemoveAt(oldIndex);
				if (oldIndex < bookmark)
					bookmark--;

				_formatting.Insert(Math.Min(bookmark, _formatting.Count), newElement);

				_stack.Remove(formattingElement);
				_stack.Insert(_stack.IndexOf(furthestBlock) + 1, newElement);
			}

			return true;
		}

		#endregion

		#region Insertion mode reset

		/// <summary>
		/// Resets the insertion mode appropriately (13.2.4.1), from the elements on the stack.
		/// </summary>
		private void ResetInsertionMode()
		{
			for (var i = _stack.Count - 1; i >= 0; i--)
			{
				var node = _stack[i];
				var last = i == 0;

				if (last && _context != null)
					node = _context;

				if (node.IsHtml)
				{
					switch (node.LocalName)
					{
						case "td":
						case "th":
							if (!last)
							{
								_mode = InsertionMode.InCell;
								return;
							}
							break;

						case "tr":
							_mode = InsertionMode.InRow;
							return;

						case "tbody":
						case "thead":
						case "tfoot":
							_mode = InsertionMode.InTableBody;
							return;

						case "caption":
							_mode = InsertionMode.InCaption;
							return;

						case "colgroup":
							_mode = InsertionMode.InColumnGroup;
							return;

						case "table":
							_mode = InsertionMode.InTable;
							return;

						case "template":
							_mode = _templateModes[_templateModes.Count - 1];
							return;

						case "head":
							if (!last)
							{
								_mode = InsertionMode.InHead;
								return;
							}
							break;

						case "body":
							_mode = InsertionMode.InBody;
							return;

						case "frameset":
							_mode = InsertionMode.InFrameset;
							return;

						case "html":
							_mode = _head == null ? InsertionMode.BeforeHead : InsertionMode.AfterHead;
							return;
					}
				}

				if (last)
				{
					_mode = InsertionMode.InBody;
					return;
				}
			}
		}

		#endregion

		#region Character helpers

		/// <summary>
		/// Determines whether a character is ASCII white space for the tree builder (tab, LF, FF, CR, space).
		/// </summary>
		/// <param name="c">The character.</param>
		/// <returns><c>true</c> for white space.</returns>
		private static bool IsWhitespace(char c)
		{
			return c == ' ' || c == '\n' || c == '\t' || c == '\f' || c == '\r';
		}

		/// <summary>
		/// Returns the number of white space characters at the start of a string.
		/// </summary>
		/// <param name="data">The string.</param>
		/// <returns>The length of the leading white space.</returns>
		private static int LeadingWhitespace(string data)
		{
			var i = 0;
			while (i < data.Length && IsWhitespace(data[i]))
				i++;

			return i;
		}

		/// <summary>
		/// Determines whether a string contains only white space.
		/// </summary>
		/// <param name="data">The string.</param>
		/// <returns><c>true</c> when every character is white space.</returns>
		private static bool IsAllWhitespace(string data)
		{
			return LeadingWhitespace(data) == data.Length;
		}

		/// <summary>
		/// Returns the white space characters of a string, dropping every other character.
		/// </summary>
		/// <param name="data">The string.</param>
		/// <returns>The white space characters.</returns>
		private static string KeepWhitespace(string data)
		{
			if (IsAllWhitespace(data))
				return data;

			var chars = new char[data.Length];
			var length = 0;
			foreach (var c in data)
			{
				if (IsWhitespace(c))
					chars[length++] = c;
			}

			return new string(chars, 0, length);
		}

		/// <summary>
		/// Splits a character token in its leading white space and the rest.
		/// </summary>
		/// <param name="token">The character token.</param>
		/// <param name="whitespace">Receives the leading white space, possibly empty.</param>
		/// <returns>A token with the remaining characters, or <c>null</c> when the token is all white space.</returns>
		private static HtmlToken SplitWhitespace(HtmlToken token, out string whitespace)
		{
			var count = LeadingWhitespace(token.Data);
			whitespace = token.Data.Substring(0, count);

			if (count == token.Data.Length)
				return null;

			return count == 0 ? token : HtmlToken.Characters(token.Data.Substring(count));
		}

		#endregion
	}
}

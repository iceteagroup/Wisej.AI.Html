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
using Wisej.AI.Html.Dom;

namespace Wisej.AI.Html.Content
{
	/// <summary>
	/// Builds the content model of a document by walking its tree with the computed styles, the way a
	/// browser renders it: block boundaries become paragraphs, white space collapses, hidden content is
	/// skipped and every element with a meaning (headings, lists, tables, images, code, quotes, math,
	/// forms, links) becomes the corresponding block or inline.
	/// </summary>
	/// <remarks>
	/// The element specific rules are in the other parts of the class: <c>ContentBuilder.Elements.cs</c>
	/// and <c>ContentBuilder.Tables.cs</c>.
	/// </remarks>
	internal sealed partial class ContentBuilder
	{
		/// <summary>
		/// The maximum depth of the recursive walk. Deeper content is written as plain text by an iterative
		/// walk, so that hostile or broken markup cannot exhaust the stack of a thread with a small stack
		/// (1 MB on thread pool threads), which would terminate the process.
		/// </summary>
		private const int MaxDepth = 160;

		// the document and the extraction settings.
		private readonly HtmlDocument _document;
		private readonly HtmlExtractorOptions _options;
		private readonly ResourceResolver _resources;
		private readonly StyleResolver _styles;

		// the result being built.
		private readonly DocumentContent _content = new DocumentContent();
		private readonly HashSet<string> _linkUrls = new HashSet<string>(StringComparer.Ordinal);

		// the depth of the walk and the caption of the enclosing figure.
		private int _depth;
		private string _figureCaption;

		// the ids of the popup menus opened by aria-haspopup controls, which are closed in the document.
		private readonly HashSet<string> _popupIds = new HashSet<string>(StringComparer.Ordinal);

		// nodes already written in another form, such as the typed markers of Office lists.
		private readonly HashSet<HtmlNode> _skippedNodes = new HashSet<HtmlNode>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ContentBuilder"/> class.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <param name="options">The extraction options.</param>
		/// <param name="resources">Resolves URLs and embedded resources.</param>
		public ContentBuilder(HtmlDocument document, HtmlExtractorOptions options, ResourceResolver resources)
		{
			_document = document;
			_options = options;
			_resources = resources;

			if (options.ApplyStyleSheets)
				_styles = new StyleResolver(document, resources.LoadText);
		}

		#region Build

		/// <summary>
		/// Builds the content of the document.
		/// </summary>
		/// <returns>The content.</returns>
		public DocumentContent Build()
		{
			_content.Metadata = MetadataReader.Read(_document, _resources);
			CollectPopups();

			var body = _document.Body ?? _document.DocumentElement;
			if (body != null)
			{
				var root = _options.MainContentOnly ? FindMainContent(body) ?? body : body;
				var style = ComputeAncestorStyles(root);

				if (style.Display != Display.None || _options.IncludeHiddenContent)
				{
					var container = new BlockContainer(_content.Blocks, allowPageBreaks: true);
					WalkChildren(root, container, style, null);
					container.FlushParagraph();
				}
			}

			TrimPageBreaks(_content.Blocks);
			AddTitle();

			return _content;
		}

		/// <summary>
		/// Finds the main content of a web page: the <c>&lt;main&gt;</c> element or <c>main</c> role, or the
		/// single <c>&lt;article&gt;</c>, when it holds a significant part of the text.
		/// </summary>
		/// <param name="body">The body.</param>
		/// <returns>The main content element, or <c>null</c> to extract the whole body.</returns>
		private HtmlElement FindMainContent(HtmlElement body)
		{
			HtmlElement main = null;
			HtmlElement article = null;
			var articles = 0;

			foreach (var element in body.DescendantElements())
			{
				if (!element.IsHtml || element.HasAttribute("hidden"))
					continue;

				if (main == null && (element.LocalName == "main" || Role(element) == "main"))
				{
					main = element;
				}
				else if (element.LocalName == "article" && !HasAncestor(element, "article"))
				{
					article = article ?? element;
					articles++;
				}
			}

			var bodyLength = TextLength(body);

			if (main != null && TextLength(main) >= Math.Min(200, bodyLength * 0.2))
				return main;

			if (articles == 1 && TextLength(article) >= bodyLength * 0.3)
				return article;

			return null;
		}

		/// <summary>
		/// Collects the popup menus and listboxes controlled by <c>aria-haspopup</c> elements: they are
		/// closed until the user opens them, and their items are user interface, not content.
		/// </summary>
		private void CollectPopups()
		{
			foreach (var element in _document.DescendantElements())
			{
				var popup = element.GetAttribute("aria-haspopup");
				var controls = element.GetAttribute("aria-controls");

				if (popup == null || controls == null || popup.Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
					continue;

				foreach (var id in controls.Split(HtmlElement.Whitespace, StringSplitOptions.RemoveEmptyEntries))
					_popupIds.Add(id);
			}
		}

		/// <summary>
		/// Computes the style of an element from the root of the document down.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns>The computed style of the element.</returns>
		private ComputedStyle ComputeAncestorStyles(HtmlElement element)
		{
			var chain = new List<HtmlElement>();
			for (var node = element; node != null; node = node.ParentElement)
				chain.Add(node);

			var style = ComputedStyle.Root;
			for (var i = chain.Count - 1; i >= 0; i--)
				style = ComputeStyle(chain[i], style);

			return style;
		}

		/// <summary>
		/// Computes the style of an element.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="parent">The style of the parent.</param>
		/// <returns>The computed style.</returns>
		private ComputedStyle ComputeStyle(HtmlElement element, ComputedStyle parent)
		{
			return ComputedStyle.Compute(element, parent, _styles?.Resolve(element));
		}

		/// <summary>
		/// Adds the document title as a heading when the content does not start with it.
		/// </summary>
		private void AddTitle()
		{
			var title = _content.Metadata.Title;
			if (!_options.IncludeTitle || String.IsNullOrWhiteSpace(title))
				return;

			var normalizedTitle = Normalize(title);

			for (var i = 0; i < _content.Blocks.Count && i < 5; i++)
			{
				if (_content.Blocks[i] is ParagraphBlock paragraph && paragraph.HeadingLevel > 0)
				{
					var heading = Normalize(PlainText(paragraph.Inlines));
					if (heading.Length > 0 && (normalizedTitle.Contains(heading) || heading.Contains(normalizedTitle)))
						return;
				}
			}

			var block = new ParagraphBlock { HeadingLevel = 1 };
			block.Inlines.Add(new TextInline { Text = title.Trim() });
			_content.Blocks.Insert(0, block);
		}

		/// <summary>
		/// Removes the page breaks at the start and at the end of the content.
		/// </summary>
		/// <param name="blocks">The blocks.</param>
		private static void TrimPageBreaks(List<Block> blocks)
		{
			while (blocks.Count > 0 && blocks[blocks.Count - 1] is PageBreakBlock)
				blocks.RemoveAt(blocks.Count - 1);

			while (blocks.Count > 0 && blocks[0] is PageBreakBlock)
				blocks.RemoveAt(0);
		}

		#endregion

		#region Walk

		/// <summary>
		/// Walks the children of a node.
		/// </summary>
		/// <param name="parent">The node.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the node.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void WalkChildren(HtmlNode parent, BlockContainer container, ComputedStyle style, string link)
		{
			var collapsed = false;

			for (var child = parent.FirstChild; child != null; child = child.NextSibling)
			{
				if (child is HtmlElement element)
				{
					// the "checkbox hack": an unchecked check box with the button role opens the content that
					// follows it, a dropdown menu that is closed in the document.
					if (!_options.IncludeHiddenContent && IsToggle(element))
					{
						collapsed = !element.HasAttribute("checked");
						continue;
					}

					if (collapsed && !element.Is("label"))
						continue;

					// Word and Outlook write lists as paragraphs with a typed marker.
					if (OfficeList.IsListParagraph(element))
					{
						child = OfficeListParagraphs(element, container, style, link);
						continue;
					}
				}

				Walk(child, container, style, link);
			}
		}

		/// <summary>
		/// Determines whether an element is a check box used as a toggle button by a CSS only dropdown.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> for <c>&lt;input type="checkbox" role="button"&gt;</c>.</returns>
		private static bool IsToggle(HtmlElement element)
		{
			return element.Is("input")
				&& String.Equals(element.GetAttribute("type"), "checkbox", StringComparison.OrdinalIgnoreCase)
				&& Role(element) == "button";
		}

		/// <summary>
		/// Walks a node.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="parentStyle">The computed style of the parent.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void Walk(HtmlNode node, BlockContainer container, ComputedStyle parentStyle, string link)
		{
			if (_skippedNodes.Count > 0 && _skippedNodes.Contains(node))
				return;

			if (node is HtmlText text)
			{
				if (parentStyle.IsTextVisible || _options.IncludeHiddenContent)
					container.Inline.AddText(text.Data, parentStyle.Format, link, parentStyle.WhiteSpace);

				return;
			}

			if (!(node is HtmlElement element))
				return;

			if (_depth >= MaxDepth)
			{
				WalkFlat(element, container, parentStyle, link);
				return;
			}

			var style = ComputeStyle(element, parentStyle);

			if (_options.RemoveBoilerplate && IsBoilerplate(element))
				return;

			if (!IsRendered(element, style))
			{
				WalkHiddenMath(element, container, style, link);
				return;
			}

			// hidden content that is included keeps the boundaries of its blocks.
			if (style.Display == Display.None)
				style.Display = Display.Block;

			_depth++;
			try
			{
				if (style.BreakBefore)
					container.AddPageBreak();

				if (element.IsHtml)
					WalkHtmlElement(element, container, style, link);
				else
					WalkForeignElement(element, container, style, link);

				if (style.BreakAfter)
					container.AddPageBreak();
			}
			finally
			{
				_depth--;
			}
		}

		/// <summary>
		/// Adds the text of an element nested beyond <see cref="MaxDepth"/>, without recursion: the visible
		/// text is kept, block elements and line breaks separate it, the structure is not.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the parent of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void WalkFlat(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			var stack = new Stack<(HtmlNode Node, ComputedStyle Style)>();
			stack.Push((element, style));

			while (stack.Count > 0)
			{
				var (node, parentStyle) = stack.Pop();

				if (node is HtmlText text)
				{
					if (parentStyle.IsTextVisible || _options.IncludeHiddenContent)
						container.Inline.AddText(text.Data, parentStyle.Format, link, parentStyle.WhiteSpace);

					continue;
				}

				if (!(node is HtmlElement child) || !child.IsHtml)
					continue;

				var childStyle = ComputeStyle(child, parentStyle);
				if (!IsRendered(child, childStyle))
					continue;

				if (child.Is("br") || childStyle.Display == Display.Block || childStyle.Display == Display.ListItem)
					container.Inline.AddSpace();

				// children are pushed in reverse to be visited in document order.
				for (var last = child.LastChild; last != null; last = last.PreviousSibling)
					stack.Push((last, childStyle));
			}
		}

		/// <summary>
		/// Adds the MathML equations of a hidden element. MediaWiki, KaTeX and MathJax render an equation
		/// twice, as MathML and as an image or HTML: one of the two is visually hidden and the other is
		/// hidden from accessibility tools with <c>aria-hidden</c>. The MathML is the meaningful one.
		/// </summary>
		/// <param name="element">The hidden element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void WalkHiddenMath(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			if (!_options.IncludeMath || (element.IsHtml && NeverRendered.Contains(element.LocalName)))
				return;

			if (element.NamespaceUri == HtmlNamespaces.MathML && element.LocalName == "math")
			{
				Equation(element, container, style, link);
				return;
			}

			foreach (var math in element.DescendantElements())
			{
				if (math.NamespaceUri == HtmlNamespaces.MathML && math.LocalName == "math" && math.ParentElement?.NamespaceUri != HtmlNamespaces.MathML)
					Equation(math, container, style, link);
			}
		}

		/// <summary>
		/// Walks an HTML element, dispatching to the rule of its kind.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void WalkHtmlElement(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			if (Role(element) == "heading" && !IsHeadingElement(element))
			{
				Heading(element, container, style, link, ParseInt(element.GetAttribute("aria-level"), 2));
				return;
			}

			switch (element.LocalName)
			{
				case "br":
					// Word writes page breaks as <br style="page-break-before: always">.
					if (!(container.AllowPageBreaks && (style.BreakBefore || style.BreakAfter)))
						container.Inline.AddBreak();
					return;

				case "wbr":
					return;

				case "img":
					Image(element, container, style, link);
					return;

				case "input":
					Input(element, container, style, link);
					return;

				case "select":
					Select(element, container, style, link);
					return;

				case "textarea":
					if (_options.IncludeForms)
						container.Inline.AddText(element.TextContent, style.Format, link, WhiteSpace.PreLine);
					return;

				case "a":
					Anchor(element, container, style, link);
					return;

				case "q":
					container.Inline.AddText("“", style.Format, link, WhiteSpace.Normal);
					Default(element, container, style, link);
					container.Inline.AddText("”", style.Format, link, WhiteSpace.Normal);
					return;

				case "ruby":
					Ruby(element, container, style, link);
					return;

				case "pre":
				case "listing":
				case "xmp":
				case "plaintext":
					Preformatted(element, container, style);
					return;

				case "h1":
				case "h2":
				case "h3":
				case "h4":
				case "h5":
				case "h6":
					Heading(element, container, style, link, element.LocalName[1] - '0');
					return;

				case "ul":
				case "ol":
				case "menu":
				case "dir":
					List(element, container, style, link);
					return;

				case "dl":
					container.FlushParagraph();
					DefinitionList(element, container, style, link);
					container.FlushParagraph();
					return;

				case "blockquote":
					Quote(element, container, style, link);
					return;

				case "table":
					Table(element, container, style, link);
					return;

				case "hr":
					container.Add(new RuleBlock());
					return;

				case "figure":
					Figure(element, container, style, link);
					return;

				case "summary":
				case "legend":
					InlineParagraph(element, container, style.With(InlineStyle.Bold), link, null);
					return;

				case "noscript":
					if (_options.IncludeNoScript)
						Default(element, container, style, link);
					return;

				case "iframe":
				case "frame":
				case "embed":
				case "video":
				case "audio":
				case "canvas":
				case "map":
				case "progress":
				case "meter":
				case "datalist":
				case "option":
				case "optgroup":
					return;

				case "button":
					// buttons are user interface, except the disclosure buttons of accordions, placed in
					// headings, whose label is the title of the section.
					if (IsInHeading(element))
						Default(element, container, style, link);
					return;

				default:
					Default(element, container, style, link);
					return;
			}
		}

		/// <summary>
		/// Walks an element by its display type: blocks break the flow, inline and contents elements do not.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void Default(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			switch (style.Display)
			{
				case Display.Block:
				case Display.ListItem:
					container.FlushParagraph();
					WalkChildren(element, container, style, link);
					container.FlushParagraph();
					break;

				default:
					WalkChildren(element, container, style, link);
					break;
			}
		}

		/// <summary>
		/// Walks an element outside the HTML namespace: an inline SVG image or a MathML equation.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void WalkForeignElement(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			if (element.NamespaceUri == HtmlNamespaces.Svg && element.LocalName == "svg")
				Svg(element, container, style, link);
			else if (element.NamespaceUri == HtmlNamespaces.MathML && element.LocalName == "math")
				Equation(element, container, style, link);
			else
				WalkChildren(element, container, style, link);
		}

		#endregion

		#region Visibility

		/// <summary>
		/// The elements that never produce content, even when hidden content is included.
		/// </summary>
		private static readonly HashSet<string> NeverRendered = new HashSet<string>(StringComparer.Ordinal)
		{
			"script", "style", "template", "head", "title", "meta", "link", "base", "basefont", "param", "source",
			"track", "area", "datalist", "noembed", "noframes", "col", "colgroup", "rp", "slot",
		};

		/// <summary>
		/// Determines whether an element is rendered.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <returns><c>false</c> for hidden elements, unless hidden content is included.</returns>
		private bool IsRendered(HtmlElement element, ComputedStyle style)
		{
			if (element.IsHtml && NeverRendered.Contains(element.LocalName))
				return false;

			if (_options.IncludeHiddenContent)
				return true;

			if (style.Display == Display.None || style.Clipped)
				return false;

			var ariaHidden = element.GetAttribute("aria-hidden");
			if (ariaHidden != null && ariaHidden.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
				return false;

			var id = element.GetAttribute("id");
			if (id != null && _popupIds.Contains(id))
				return false;

			return !HasClass(element, HiddenClasses);
		}

		/// <summary>
		/// The classes of the CSS frameworks that hide content visually ("screen reader only").
		/// </summary>
		private static readonly HashSet<string> HiddenClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"sr-only", "visually-hidden", "visuallyhidden", "screen-reader-text", "screen-reader-only", "screenreader-only",
			"a11y-hidden", "u-hidden-visually", "element-invisible", "hidden-accessible", "offscreen", "assistive-text",
		};

		/// <summary>
		/// The classes that mark site chrome: content hidden when printing, edit links, navigation boxes and breadcrumbs.
		/// </summary>
		private static readonly HashSet<string> BoilerplateClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"noprint", "no-print", "d-print-none", "hidden-print", "print-hidden", "print-hide", "hide-print", "not-printable",
			"mw-editsection", "mw-jump-link", "navbox", "vertical-navbox", "catlinks", "skip-link", "skip-links", "skiplink",
			"breadcrumb", "breadcrumbs",
		};

		/// <summary>
		/// Determines whether an element has one of the classes of a set.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="classes">The classes.</param>
		/// <returns><c>true</c> when the element has one of the classes.</returns>
		private static bool HasClass(HtmlElement element, HashSet<string> classes)
		{
			var list = element.ClassList;
			for (var i = 0; i < list.Count; i++)
			{
				if (classes.Contains(list[i]))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Determines whether an element is inside a heading or a summary.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> when an ancestor is a heading.</returns>
		private static bool IsInHeading(HtmlElement element)
		{
			for (var ancestor = element.ParentElement; ancestor != null; ancestor = ancestor.ParentElement)
			{
				if (ancestor.IsHtml && (IsHeadingElement(ancestor) || ancestor.LocalName == "summary" || Role(ancestor) == "heading"))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Determines whether an element is site chrome: navigation, page banner or footer, sidebar, skip link or cookie banner.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> when the element is removed.</returns>
		private static bool IsBoilerplate(HtmlElement element)
		{
			if (!element.IsHtml)
				return false;

			if (HasClass(element, BoilerplateClasses))
				return true;

			switch (Role(element))
			{
				case "navigation":
				case "banner":
				case "contentinfo":
				case "search":
				case "menubar":
				case "toolbar":
					return true;

				case "complementary":
					return IsPageLevel(element);
			}

			switch (element.LocalName)
			{
				case "nav":
					return true;

				case "header":
					// a page header holding the title of the page is content.
					return IsPageLevel(element) && element.QuerySelector("h1") == null;

				case "footer":
				case "aside":
					return IsPageLevel(element);

				case "a":
					var href = element.GetAttribute("href");
					if (href != null && href.StartsWith("#", StringComparison.Ordinal))
					{
						var text = element.TextContent;
						return text.Length < 60 && text.IndexOf("skip", StringComparison.OrdinalIgnoreCase) >= 0;
					}

					return false;
			}

			return IsCookieBanner(element);
		}

		/// <summary>
		/// Determines whether an element is outside of any sectioning content, so it belongs to the page.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> for page level elements.</returns>
		private static bool IsPageLevel(HtmlElement element)
		{
			for (var ancestor = element.ParentElement; ancestor != null; ancestor = ancestor.ParentElement)
			{
				if (!ancestor.IsHtml)
					continue;

				switch (ancestor.LocalName)
				{
					case "article":
					case "main":
					case "section":
					case "aside":
					case "nav":
					case "blockquote":
					case "details":
					case "dialog":
					case "fieldset":
					case "figure":
					case "td":
						return false;
				}

				var role = Role(ancestor);
				if (role == "main" || role == "article")
					return false;
			}

			return true;
		}

		/// <summary>
		/// Determines whether an element is a cookie consent banner, identified by its id or classes.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> for a cookie banner.</returns>
		private static bool IsCookieBanner(HtmlElement element)
		{
			var id = element.GetAttribute("id");
			var className = element.GetAttribute("class");
			if (id == null && className == null)
				return false;

			var names = ((id ?? "") + " " + (className ?? "")).ToLowerInvariant();
			if (names.IndexOf("cookie", StringComparison.Ordinal) < 0 && names.IndexOf("consent", StringComparison.Ordinal) < 0 && names.IndexOf("gdpr", StringComparison.Ordinal) < 0)
				return false;

			// a large element is a page wrapper that only mentions cookies in a class name.
			return TextLength(element, 3000) < 3000;
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Returns the first token of the <c>role</c> attribute, lowercased.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns>The role, or <c>null</c>.</returns>
		private static string Role(HtmlElement element)
		{
			var role = element.GetAttribute("role");
			if (String.IsNullOrWhiteSpace(role))
				return null;

			var tokens = role.Trim().ToLowerInvariant().Split(HtmlElement.Whitespace, StringSplitOptions.RemoveEmptyEntries);
			return tokens.Length > 0 ? tokens[0] : null;
		}

		/// <summary>
		/// Determines whether an element is one of the heading elements.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> for <c>h1</c> to <c>h6</c>.</returns>
		private static bool IsHeadingElement(HtmlElement element)
		{
			var name = element.LocalName;
			return name.Length == 2 && name[0] == 'h' && name[1] >= '1' && name[1] <= '6';
		}

		/// <summary>
		/// Determines whether an element has an HTML ancestor with the specified name.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="name">The ancestor name.</param>
		/// <returns><c>true</c> when such an ancestor exists.</returns>
		private static bool HasAncestor(HtmlElement element, string name)
		{
			for (var ancestor = element.ParentElement; ancestor != null; ancestor = ancestor.ParentElement)
			{
				if (ancestor.Is(name))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Returns the number of non-white space characters of the text of an element.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="limit">The count at which the measure stops, as the caller only needs to know it is reached.</param>
		/// <returns>The length, at most <paramref name="limit"/>.</returns>
		private static int TextLength(HtmlElement element, int limit = Int32.MaxValue)
		{
			var length = 0;
			foreach (var node in element.Descendants())
			{
				if (node is HtmlText text && !(text.ParentNode is HtmlElement parent && (parent.Is("script") || parent.Is("style"))))
				{
					foreach (var c in text.Data)
					{
						if (!Char.IsWhiteSpace(c))
							length++;
					}

					if (length >= limit)
						return limit;
				}
			}

			return length;
		}

		/// <summary>
		/// Parses an integer attribute.
		/// </summary>
		/// <param name="value">The attribute value. Can be <c>null</c>.</param>
		/// <param name="defaultValue">The value returned when the attribute is missing or invalid.</param>
		/// <returns>The integer.</returns>
		private static int ParseInt(string value, int defaultValue)
		{
			if (value == null)
				return defaultValue;

			// HTML integer parsing: leading white space, optional sign, digits, anything after is ignored.
			value = value.Trim();
			var end = 0;
			if (end < value.Length && (value[end] == '-' || value[end] == '+'))
				end++;

			while (end < value.Length && Char.IsDigit(value[end]) && end < 10)
				end++;

			return Int32.TryParse(value.Substring(0, end), out var result) ? result : defaultValue;
		}

		/// <summary>
		/// Returns the plain text of inlines.
		/// </summary>
		/// <param name="inlines">The inlines.</param>
		/// <returns>The text.</returns>
		internal static string PlainText(IEnumerable<Inline> inlines)
		{
			var sb = new StringBuilder();
			foreach (var inline in inlines)
			{
				if (inline is TextInline text)
					sb.Append(text.Text);
				else if (inline is BreakInline)
					sb.Append(' ');
			}

			return sb.ToString().Trim();
		}

		/// <summary>
		/// Normalizes text for comparisons: lowercase letters and digits separated by single spaces.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>The normalized text.</returns>
		private static string Normalize(string text)
		{
			var sb = new StringBuilder(text.Length);
			var space = false;

			foreach (var c in text)
			{
				if (Char.IsLetterOrDigit(c))
				{
					if (space && sb.Length > 0)
						sb.Append(' ');

					sb.Append(Char.ToLowerInvariant(c));
					space = false;
				}
				else
				{
					space = true;
				}
			}

			return sb.ToString();
		}

		#endregion
	}
}

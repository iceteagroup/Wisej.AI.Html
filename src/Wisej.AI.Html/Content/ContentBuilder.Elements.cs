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
using System.Globalization;
using System.Linq;
using System.Text;
using Wisej.AI.Html.Dom;

namespace Wisej.AI.Html.Content
{
	/// <summary>
	/// The rules of the elements with a meaning: headings, lists, code, quotations, figures, links,
	/// forms, images, SVG and MathML.
	/// </summary>
	internal sealed partial class ContentBuilder
	{
		#region Headings and paragraphs

		/// <summary>
		/// Adds a heading.
		/// </summary>
		/// <param name="element">The heading element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		/// <param name="level">The heading level.</param>
		private void Heading(HtmlElement element, BlockContainer container, ComputedStyle style, string link, int level)
		{
			InlineParagraph(element, container, style, link, paragraph => paragraph.HeadingLevel = Math.Max(1, Math.Min(6, level)));
		}

		/// <summary>
		/// Adds an element as a single paragraph, joining the paragraphs of its content. Blocks that cannot
		/// be part of a paragraph (tables, lists, code) follow it.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		/// <param name="configure">Configures the paragraph, such as its heading level. Can be <c>null</c>.</param>
		private void InlineParagraph(HtmlElement element, BlockContainer container, ComputedStyle style, string link, Action<ParagraphBlock> configure)
		{
			container.FlushParagraph();

			var inner = new BlockContainer(new List<Block>(), allowPageBreaks: false);
			WalkChildren(element, inner, style, link);
			inner.FlushParagraph();

			var paragraph = new ParagraphBlock();
			var others = new List<Block>();

			foreach (var block in inner.Blocks)
			{
				if (block is ParagraphBlock part)
				{
					if (paragraph.Inlines.Count > 0)
						paragraph.Inlines.Add(new TextInline { Text = " ", Style = InlineStyle.None });

					paragraph.Inlines.AddRange(part.Inlines);
				}
				else
				{
					others.Add(block);
				}
			}

			configure?.Invoke(paragraph);

			if (paragraph.Inlines.Count > 0)
				container.Add(paragraph);

			foreach (var block in others)
				container.Add(block);
		}

		/// <summary>
		/// Adds a block quotation.
		/// </summary>
		/// <param name="element">The quotation element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void Quote(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			container.FlushParagraph();

			var quote = new QuoteBlock();
			var inner = new BlockContainer(quote.Blocks, allowPageBreaks: false);
			WalkChildren(element, inner, style, link);
			inner.FlushParagraph();

			if (quote.Blocks.Count > 0)
				container.Add(quote);
		}

		/// <summary>
		/// Adds a figure. The caption is attached to the images of the figure and written as a paragraph.
		/// </summary>
		/// <param name="element">The figure element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void Figure(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			var previous = _figureCaption;
			var caption = element.Children.FirstOrDefault(c => c.Is("figcaption"));
			_figureCaption = caption == null ? null : HtmlDocument.CollapseWhitespace(caption.TextContent);

			try
			{
				Default(element, container, style, link);
			}
			finally
			{
				_figureCaption = previous;
			}
		}

		#endregion

		#region Code

		/// <summary>
		/// Adds a preformatted element as a code block, keeping its white space.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		private void Preformatted(HtmlElement element, BlockContainer container, ComputedStyle style)
		{
			var sb = new StringBuilder();
			CollectPreformatted(element, style, sb);

			// trailing white space and leading blank lines carry no content.
			var text = sb.ToString().TrimEnd();
			while (text.StartsWith("\n", StringComparison.Ordinal))
				text = text.Substring(1);

			if (text.Trim().Length == 0)
				return;

			container.Add(new CodeBlock { Text = text, Language = DetectLanguage(element) });
		}

		/// <summary>
		/// Collects the text of a preformatted element: text is copied as is, line breaks and block
		/// boundaries (one line per element in some highlighters) become newlines.
		/// </summary>
		/// <param name="parent">The element.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="sb">The output.</param>
		private void CollectPreformatted(HtmlNode parent, ComputedStyle style, StringBuilder sb)
		{
			for (var child = parent.FirstChild; child != null; child = child.NextSibling)
			{
				if (child is HtmlText text)
				{
					if (style.IsTextVisible || _options.IncludeHiddenContent)
						sb.Append(text.Data.Replace("\r", ""));

					continue;
				}

				if (!(child is HtmlElement element))
					continue;

				// beyond the depth limit the text is copied without recursion.
				if (_depth >= MaxDepth)
				{
					sb.Append(element.TextContent);
					continue;
				}

				var childStyle = ComputeStyle(element, style);
				if (!IsRendered(element, childStyle))
					continue;

				if (element.Is("br"))
				{
					sb.Append('\n');
					continue;
				}

				var block = childStyle.Display == Display.Block || childStyle.Display == Display.ListItem;
				if (block && sb.Length > 0 && sb[sb.Length - 1] != '\n')
					sb.Append('\n');

				_depth++;
				CollectPreformatted(element, childStyle, sb);
				_depth--;

				if (block && sb.Length > 0 && sb[sb.Length - 1] != '\n')
					sb.Append('\n');
			}
		}

		/// <summary>
		/// Detects the language of a code block from the conventions of syntax highlighters:
		/// <c>language-x</c> and <c>lang-x</c> classes (Prism, highlight.js, Markdown renderers),
		/// <c>highlight-source-x</c> (GitHub), <c>brush: x</c> (SyntaxHighlighter) and <c>data-lang</c>.
		/// </summary>
		/// <param name="pre">The preformatted element.</param>
		/// <returns>The language, or <c>null</c>.</returns>
		private static string DetectLanguage(HtmlElement pre)
		{
			var candidates = new List<HtmlElement> { pre };
			if (pre.ParentElement != null)
				candidates.Add(pre.ParentElement);

			var code = pre.Children.FirstOrDefault(c => c.Is("code"));
			if (code != null)
				candidates.Insert(0, code);

			foreach (var element in candidates)
			{
				var data = element.GetAttribute("data-lang") ?? element.GetAttribute("data-language");
				if (!String.IsNullOrWhiteSpace(data))
					return data.Trim().ToLowerInvariant();

				foreach (var name in element.ClassList)
				{
					var lower = name.ToLowerInvariant();
					foreach (var prefix in new[] { "language-", "lang-", "highlight-source-", "highlight-", "sourcecode-" })
					{
						if (lower.StartsWith(prefix, StringComparison.Ordinal) && lower.Length > prefix.Length)
							return lower.Substring(prefix.Length);
					}
				}

				var className = element.ClassName;
				var brush = className.IndexOf("brush:", StringComparison.OrdinalIgnoreCase);
				if (brush >= 0)
				{
					var value = className.Substring(brush + 6).TrimStart();
					var end = value.IndexOfAny(new[] { ';', ' ' });
					return (end < 0 ? value : value.Substring(0, end)).ToLowerInvariant();
				}
			}

			return null;
		}

		#endregion

		#region Lists

		/// <summary>
		/// Adds an ordered or unordered list, computing the item labels as browsers display them.
		/// </summary>
		/// <param name="element">The list element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void List(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			container.FlushParagraph();

			var ordered = element.Is("ol");
			var reversed = ordered && element.HasAttribute("reversed");
			var list = new ListBlock { Ordered = ordered && !ListMarkers.IsBullet(style.ListStyleType) };

			var counter = Clamp(ParseInt(element.GetAttribute("start"), reversed ? element.Children.Count(c => c.Is("li")) : 1), -1000000000, 1000000000);
			ListItem current = null;
			BlockContainer currentContainer = null;

			for (var child = element.FirstChild; child != null; child = child.NextSibling)
			{
				if (child is HtmlElement item)
				{
					var itemStyle = ComputeStyle(item, style);
					if (!IsRendered(item, itemStyle) || (_options.RemoveBoilerplate && IsBoilerplate(item)))
						continue;

					if (item.Is("li") || itemStyle.Display == Display.ListItem)
					{
						if (item.Is("li"))
							counter = Clamp(ParseInt(item.GetAttribute("value"), counter), -1000000000, 1000000000);

						current = new ListItem
						{
							Marker = ListMarkers.IsBullet(itemStyle.ListStyleType) || !ordered ? "-" : ListMarkers.Format(itemStyle.ListStyleType, counter),
						};

						currentContainer = new BlockContainer(current.Blocks, allowPageBreaks: false);

						_depth++;
						WalkChildren(item, currentContainer, itemStyle, link);
						_depth--;

						currentContainer.FlushParagraph();
						if (current.Blocks.Count > 0)
							list.Items.Add(current);

						counter += reversed ? -1 : 1;
						continue;
					}
				}
				else if (!(child is HtmlText text) || text.Data.Trim().Length == 0)
				{
					continue;
				}

				// content outside of the items, such as a list nested directly in a list, belongs to the previous item.
				if (current == null)
				{
					current = new ListItem { Marker = null };
					currentContainer = new BlockContainer(current.Blocks, allowPageBreaks: false);
					list.Items.Add(current);
				}
				else if (!list.Items.Contains(current))
				{
					list.Items.Add(current);
				}

				currentContainer = new BlockContainer(current.Blocks, allowPageBreaks: false);
				Walk(child, currentContainer, style, link);
				currentContainer.FlushParagraph();
			}

			list.Items.RemoveAll(i => i.Blocks.Count == 0);
			if (list.Items.Count > 0)
				container.Add(list);
		}

		/// <summary>
		/// Adds the terms and definitions of a definition list.
		/// </summary>
		/// <param name="element">The list element, or a <c>&lt;div&gt;</c> grouping a term and its definitions.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void DefinitionList(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			for (var child = element.FirstChild; child != null; child = child.NextSibling)
			{
				var item = child as HtmlElement;
				if (item == null || !(item.Is("dt") || item.Is("dd") || item.Is("div")) || _depth >= MaxDepth)
				{
					Walk(child, container, style, link);
					continue;
				}

				var itemStyle = ComputeStyle(item, style);
				if (!IsRendered(item, itemStyle))
					continue;

				_depth++;
				try
				{
					if (item.Is("dt"))
					{
						InlineParagraph(item, container, itemStyle, link, paragraph => paragraph.IsTerm = true);
					}
					else if (item.Is("dd"))
					{
						var definition = new DefinitionBlock();
						var inner = new BlockContainer(definition.Blocks, allowPageBreaks: false);
						WalkChildren(item, inner, itemStyle, link);
						inner.FlushParagraph();

						if (definition.Blocks.Count > 0)
							container.Add(definition);
					}
					else
					{
						// <div> groups a term with its definitions.
						container.FlushParagraph();
						DefinitionList(item, container, itemStyle, link);
					}
				}
				finally
				{
					_depth--;
				}
			}
		}

		#endregion

		#region Inline elements

		/// <summary>
		/// Adds the content of a hyperlink, with its target.
		/// </summary>
		/// <param name="element">The anchor element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void Anchor(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			var target = link;

			if (_resources.TryResolveLink(element.GetAttribute("href"), out var resolved))
			{
				if (_linkUrls.Add(resolved))
					_content.Links.Add(new HtmlLink(resolved, HtmlDocument.CollapseWhitespace(element.TextContent)));

				if (_options.IncludeHyperlinks)
					target = resolved;
			}

			// links laid out side by side (tags, buttons, menus) are separated in the text.
			if (PreviousElementSibling(element)?.Is("a") == true)
				container.Inline.AddSpace();

			var first = container.Blocks.Count;
			Default(element, container, style, target);

			if (target != null && target != link)
				KeepFirstLink(container.Blocks, first, target);
		}

		/// <summary>
		/// Keeps the target of a link that wraps several blocks (a "card" with a picture, a title and a
		/// summary) only on its main block, the first heading or else the first block, so that the address
		/// is not repeated on every line.
		/// </summary>
		/// <param name="blocks">The blocks of the flow.</param>
		/// <param name="first">The index of the first block of the link.</param>
		/// <param name="target">The target of the link.</param>
		private static void KeepFirstLink(List<Block> blocks, int first, string target)
		{
			var linked = new List<ParagraphBlock>();
			for (var i = first; i < blocks.Count; i++)
			{
				if (blocks[i] is ParagraphBlock paragraph && paragraph.Inlines.Any(inline => inline.Link == target))
					linked.Add(paragraph);
			}

			if (linked.Count < 2)
				return;

			var main = linked.FirstOrDefault(p => p.HeadingLevel > 0) ?? linked[0];
			foreach (var paragraph in linked)
			{
				if (paragraph == main)
					continue;

				foreach (var inline in paragraph.Inlines)
				{
					if (inline.Link == target)
						inline.Link = null;
				}
			}
		}

		/// <summary>
		/// Returns the previous sibling of a node when it is an element, skipping comments but not text.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns>The previous element, or <c>null</c> when text or nothing precedes the node.</returns>
		private static HtmlElement PreviousElementSibling(HtmlNode node)
		{
			for (var previous = node.PreviousSibling; previous != null; previous = previous.PreviousSibling)
			{
				if (previous is HtmlElement element)
					return element;

				if (!(previous is HtmlComment))
					return null;
			}

			return null;
		}

		/// <summary>
		/// Adds a ruby annotation as the base text followed by the annotation in parentheses: 漢字(かんじ).
		/// </summary>
		/// <param name="element">The ruby element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void Ruby(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			for (var child = element.FirstChild; child != null; child = child.NextSibling)
			{
				if (child is HtmlElement annotation && (annotation.Is("rt") || annotation.Is("rtc")))
				{
					var annotationStyle = ComputeStyle(annotation, style);
					if (!IsRendered(annotation, annotationStyle))
						continue;

					container.Inline.AddText("(", style.Format, link, WhiteSpace.Normal);
					WalkChildren(annotation, container, annotationStyle, link);
					container.Inline.AddText(")", style.Format, link, WhiteSpace.Normal);
				}
				else
				{
					Walk(child, container, style, link);
				}
			}
		}

		#endregion

		#region Forms

		/// <summary>
		/// Adds the value of an input control: check boxes and radio buttons as <c>[x]</c> or <c>[ ]</c>,
		/// the value of text fields; buttons, passwords and hidden fields are skipped.
		/// </summary>
		/// <param name="element">The input element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void Input(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			var type = (element.GetAttribute("type") ?? "text").Trim().ToLowerInvariant();

			if (type == "image")
			{
				Image(element, container, style, link);
				return;
			}

			if (!_options.IncludeForms || !style.IsTextVisible)
				return;

			switch (type)
			{
				case "checkbox":
				case "radio":
					container.Inline.AddText(element.HasAttribute("checked") ? "[x] " : "[ ] ", style.Format, link, WhiteSpace.Normal);
					return;

				case "hidden":
				case "password":
				case "file":
				case "submit":
				case "reset":
				case "button":
				case "range":
				case "color":
					return;

				default:
					var value = element.GetAttribute("value");
					if (!String.IsNullOrWhiteSpace(value))
						container.Inline.AddText(value, style.Format, link, WhiteSpace.Normal);
					return;
			}
		}

		/// <summary>
		/// Adds the selected options of a list box or drop down list.
		/// </summary>
		/// <param name="element">The select element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void Select(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			if (!_options.IncludeForms || !style.IsTextVisible)
				return;

			var options = element.DescendantElements().Where(e => e.Is("option")).ToList();
			var selected = options.Where(o => o.HasAttribute("selected")).ToList();

			// a drop down list displays its first option when none is selected.
			if (selected.Count == 0 && !element.HasAttribute("multiple") && options.Count > 0)
				selected.Add(options[0]);

			var text = String.Join(", ", selected.Select(o => o.GetAttribute("label") ?? HtmlDocument.CollapseWhitespace(o.TextContent)).Where(t => t.Length > 0));
			if (text.Length > 0)
				container.Inline.AddText(text, style.Format, link, WhiteSpace.Normal);
		}

		#endregion

		#region Images

		/// <summary>
		/// Adds an image (<c>&lt;img&gt;</c> or <c>&lt;input type="image"&gt;</c>).
		/// </summary>
		/// <param name="element">The image element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void Image(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			if (!_options.IncludeImages || (!style.IsTextVisible && !_options.IncludeHiddenContent))
				return;

			var alt = element.GetAttribute("alt");
			var label = NullIfEmpty(alt) ?? NullIfEmpty(element.GetAttribute("aria-label"));
			var title = NullIfEmpty(element.GetAttribute("title"));

			if (_options.SkipDecorativeImages && IsDecorative(element, alt, title))
				return;

			var width = ParseLength(element.GetAttribute("width"));
			var height = ParseLength(element.GetAttribute("height"));

			// tracking pixels and spacers.
			if (_options.SkipDecorativeImages && width > 0 && height > 0 && width <= 2 && height <= 2)
				return;

			if (_options.MinimumImageSize > 0 && width > 0 && height > 0 && (width < _options.MinimumImageSize || height < _options.MinimumImageSize))
				return;

			var source = _resources.LoadImage(SelectSource(element));
			if (source == null)
			{
				// browsers display the alternative text of a broken image.
				if (label != null)
					container.Inline.AddText(label, style.Format, link, WhiteSpace.Normal);

				return;
			}

			AddImage(container, source, label, title, width, height, link);
		}

		/// <summary>
		/// Adds an inline SVG image. Icons (no title, description or text) are decorative.
		/// </summary>
		/// <param name="element">The svg element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void Svg(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			if (!_options.IncludeImages || (!style.IsTextVisible && !_options.IncludeHiddenContent))
				return;

			var titleElement = element.Children.FirstOrDefault(c => c.LocalName == "title");
			var descElement = element.Children.FirstOrDefault(c => c.LocalName == "desc");

			var label = NullIfEmpty(element.GetAttribute("aria-label"))
				?? NullIfEmpty(titleElement == null ? null : HtmlDocument.CollapseWhitespace(titleElement.TextContent));
			var description = descElement == null ? null : NullIfEmpty(HtmlDocument.CollapseWhitespace(descElement.TextContent));

			var texts = element.DescendantElements()
				.Where(e => e.LocalName == "text")
				.Select(e => HtmlDocument.CollapseWhitespace(e.TextContent))
				.Where(t => t.Length > 0)
				.ToList();

			if (_options.SkipDecorativeImages)
			{
				var role = Role(element);
				if (role == "presentation" || role == "none" || (label == null && description == null && texts.Count == 0))
					return;
			}

			var width = ParseLength(element.GetAttribute("width"));
			var height = ParseLength(element.GetAttribute("height"));

			var alt = label ?? description ?? Truncate(String.Join(", ", texts), 200);
			var markup = SerializeSvg(element);
			var source = _resources.AddEmbedded("image/svg+xml", Encoding.UTF8.GetBytes(markup));

			AddImage(container, source, alt, description != null && description != alt ? description : null, width, height, link);
		}

		/// <summary>
		/// Creates an image placement and adds it to the paragraph being built.
		/// </summary>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="source">The picture.</param>
		/// <param name="alt">The alternative text, or <c>null</c>.</param>
		/// <param name="title">The title, or <c>null</c>.</param>
		/// <param name="width">The declared width, or 0.</param>
		/// <param name="height">The declared height, or 0.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void AddImage(BlockContainer container, ImageSource source, string alt, string title, double width, double height, string link)
		{
			var image = new HtmlImage
			{
				Key = source.Key,
				FileName = source.FileName,
				Url = source.Url,
				ContentType = source.ContentType,
				Data = source.Data,
				AltText = alt,
				Title = title,
				Caption = _figureCaption,
				Width = width,
				Height = height,
				Link = link,
			};

			_content.Images.Add(image);
			container.Inline.AddObject(new ImageInline { Image = image, Link = link });
		}

		/// <summary>
		/// Determines whether an image is decorative: marked as presentational or hidden, or with an empty
		/// alternative text, which the HTML standard defines as "not part of the content".
		/// </summary>
		/// <param name="element">The image element.</param>
		/// <param name="alt">The alt attribute, or <c>null</c>.</param>
		/// <param name="title">The title attribute, or <c>null</c>.</param>
		/// <returns><c>true</c> for decorative images.</returns>
		private static bool IsDecorative(HtmlElement element, string alt, string title)
		{
			var role = Role(element);
			if (role == "presentation" || role == "none")
				return true;

			return alt != null && alt.Trim().Length == 0 && title == null && NullIfEmpty(element.GetAttribute("aria-label")) == null;
		}

		/// <summary>
		/// Selects the source of an image: <c>src</c>, the lazy loading attributes when <c>src</c> is a
		/// placeholder, or the first candidate of <c>srcset</c> (also from the <c>&lt;source&gt;</c> of a picture).
		/// </summary>
		/// <param name="element">The image element.</param>
		/// <returns>The source, or <c>null</c>.</returns>
		private static string SelectSource(HtmlElement element)
		{
			var src = element.GetAttribute("src")?.Trim();

			var placeholder = String.IsNullOrEmpty(src)
				|| (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && src.Length < 400)
				|| src.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0
				|| src.EndsWith("blank.gif", StringComparison.OrdinalIgnoreCase)
				|| src.EndsWith("spacer.gif", StringComparison.OrdinalIgnoreCase);

			if (placeholder)
			{
				foreach (var name in new[] { "data-src", "data-lazy-src", "data-original", "data-lazy", "data-url", "data-srcset" })
				{
					var lazy = element.GetAttribute(name);
					if (!String.IsNullOrWhiteSpace(lazy))
						return name == "data-srcset" ? FirstCandidate(lazy) : lazy;
				}
			}

			if (!String.IsNullOrEmpty(src))
				return src;

			var srcset = element.GetAttribute("srcset");
			if (String.IsNullOrWhiteSpace(srcset) && element.ParentElement != null && element.ParentElement.Is("picture"))
				srcset = element.ParentElement.Children.Where(c => c.Is("source")).Select(c => c.GetAttribute("srcset")).FirstOrDefault(s => !String.IsNullOrWhiteSpace(s));

			return String.IsNullOrWhiteSpace(srcset) ? null : FirstCandidate(srcset);
		}

		/// <summary>
		/// Returns the URL of the first candidate of a <c>srcset</c>.
		/// </summary>
		/// <param name="srcset">The srcset value.</param>
		/// <returns>The URL.</returns>
		private static string FirstCandidate(string srcset)
		{
			var candidate = srcset.Trim();
			var comma = candidate.IndexOf(", ", StringComparison.Ordinal);
			if (comma > 0)
				candidate = candidate.Substring(0, comma);

			var space = candidate.IndexOfAny(new[] { ' ', '\t', '\n' });
			return space > 0 ? candidate.Substring(0, space) : candidate;
		}

		/// <summary>
		/// Serializes an inline SVG as a standalone document, adding the namespace declarations.
		/// </summary>
		/// <param name="svg">The svg element.</param>
		/// <returns>The SVG markup.</returns>
		private static string SerializeSvg(HtmlElement svg)
		{
			var markup = svg.OuterHtml;
			var declarations = "";

			if (!svg.HasAttribute("xmlns"))
				declarations += " xmlns=\"http://www.w3.org/2000/svg\"";

			if (!svg.HasAttribute("xmlns:xlink") && markup.IndexOf("xlink:", StringComparison.Ordinal) >= 0)
				declarations += " xmlns:xlink=\"http://www.w3.org/1999/xlink\"";

			return declarations.Length == 0 ? markup : "<svg" + declarations + markup.Substring(4);
		}

		/// <summary>
		/// Parses a dimension attribute, such as <c>width="120"</c> or <c>width="120px"</c>.
		/// </summary>
		/// <param name="value">The attribute value. Can be <c>null</c>.</param>
		/// <returns>The length in CSS pixels, or 0 when missing or relative.</returns>
		private static double ParseLength(string value)
		{
			if (String.IsNullOrWhiteSpace(value))
				return 0;

			value = value.Trim();
			if (value.EndsWith("%", StringComparison.Ordinal))
				return 0;

			if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
				value = value.Substring(0, value.Length - 2);

			return Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var length) && length > 0 ? length : 0;
		}

		#endregion

		#region Math

		/// <summary>
		/// Adds a MathML equation as LaTeX: display equations as blocks, others inline.
		/// </summary>
		/// <param name="element">The math element.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="style">The computed style of the element.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		private void Equation(HtmlElement element, BlockContainer container, ComputedStyle style, string link)
		{
			if (!_options.IncludeMath)
			{
				container.Inline.AddText(element.TextContent, style.Format, link, WhiteSpace.Normal);
				return;
			}

			var latex = MathMLConverter.ToLatex(element);
			if (latex.Length == 0)
				return;

			if (MathMLConverter.IsDisplay(element))
				container.Add(new MathBlock { Latex = latex });
			else
				container.Inline.AddObject(new MathInline { Latex = latex, Link = link });
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Returns a trimmed string, or <c>null</c> when it is empty.
		/// </summary>
		/// <param name="value">The string. Can be <c>null</c>.</param>
		/// <returns>The trimmed string or <c>null</c>.</returns>
		private static string NullIfEmpty(string value)
		{
			value = value?.Trim();
			return String.IsNullOrEmpty(value) ? null : value;
		}

		/// <summary>
		/// Truncates a text at a word boundary.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="length">The maximum length.</param>
		/// <returns>The text, with an ellipsis when truncated.</returns>
		private static string Truncate(string text, int length)
		{
			if (text.Length <= length)
				return text;

			var cut = text.LastIndexOf(' ', length);
			return text.Substring(0, cut > length / 2 ? cut : length) + "…";
		}

		#endregion
	}
}

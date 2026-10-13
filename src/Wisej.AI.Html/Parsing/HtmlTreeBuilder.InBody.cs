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
using Wisej.AI.Html.Dom;

namespace Wisej.AI.Html.Parsing
{
	/// <summary>
	/// The "in body" insertion mode (HTML standard, 13.2.6.4.7).
	/// </summary>
	internal sealed partial class HtmlTreeBuilder
	{
		/// <summary>The heading element names.</summary>
		private static readonly string[] Headings = { "h1", "h2", "h3", "h4", "h5", "h6" };

		/// <summary>
		/// 13.2.6.4.7 The "in body" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void InBodyMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Characters:
					InBodyCharacters(token.Data);
					return;

				case HtmlTokenType.Comment:
					InsertComment(token);
					return;

				case HtmlTokenType.Doctype:
					return;

				case HtmlTokenType.StartTag:
					InBodyStartTag(token);
					return;

				case HtmlTokenType.EndTag:
					InBodyEndTag(token);
					return;

				case HtmlTokenType.EndOfFile:
					if (_templateModes.Count > 0)
						InTemplateMode(token);
					return;
			}
		}

		/// <summary>
		/// Inserts characters in the "in body" insertion mode: NUL characters are dropped and the active
		/// formatting elements are reconstructed first.
		/// </summary>
		/// <param name="data">The characters.</param>
		private void InBodyCharacters(string data)
		{
			if (data.IndexOf('\0') >= 0)
				data = data.Replace("\0", "");

			if (data.Length == 0)
				return;

			ReconstructFormatting();
			InsertCharacters(data);

			if (!IsAllWhitespace(data))
				_framesetOk = false;
		}

		/// <summary>
		/// Processes a start tag in the "in body" insertion mode.
		/// </summary>
		/// <param name="token">The start tag.</param>
		private void InBodyStartTag(HtmlToken token)
		{
			switch (token.Name)
			{
				case "html":
					if (StackContains("template"))
						return;

					MergeAttributes(_stack[0], token);
					return;

				case "base":
				case "basefont":
				case "bgsound":
				case "link":
				case "meta":
				case "noframes":
				case "script":
				case "style":
				case "template":
				case "title":
					InHeadMode(token);
					return;

				case "body":
					if (_stack.Count == 1 || !_stack[1].Is("body") || StackContains("template"))
						return;

					_framesetOk = false;
					MergeAttributes(_stack[1], token);
					return;

				case "frameset":
					if (_stack.Count == 1 || !_stack[1].Is("body") || !_framesetOk)
						return;

					_stack[1].Remove();
					while (_stack.Count > 1)
						Pop();

					InsertHtmlElement(token);
					_mode = InsertionMode.InFrameset;
					return;

				case "address":
				case "article":
				case "aside":
				case "blockquote":
				case "center":
				case "details":
				case "dialog":
				case "dir":
				case "div":
				case "dl":
				case "fieldset":
				case "figcaption":
				case "figure":
				case "footer":
				case "header":
				case "hgroup":
				case "main":
				case "menu":
				case "nav":
				case "ol":
				case "p":
				case "search":
				case "section":
				case "summary":
				case "ul":
					ClosePElementInButtonScope();
					InsertHtmlElement(token);
					return;

				case "h1":
				case "h2":
				case "h3":
				case "h4":
				case "h5":
				case "h6":
					ClosePElementInButtonScope();

					// headings do not nest: "<h1><h2>" closes the h1.
					if (this.CurrentNode.IsHtml && Array.IndexOf(Headings, this.CurrentNode.LocalName) >= 0)
						Pop();

					InsertHtmlElement(token);
					return;

				case "pre":
				case "listing":
					ClosePElementInButtonScope();
					InsertHtmlElement(token);
					_skipNextNewline = true;
					_framesetOk = false;
					return;

				case "form":
					if (_form != null && !StackContains("template"))
						return;

					ClosePElementInButtonScope();
					var form = InsertHtmlElement(token);
					if (!StackContains("template"))
						_form = form;
					return;

				case "li":
					StartListItem(token, "li");
					return;

				case "dd":
				case "dt":
					StartListItem(token, "dd", "dt");
					return;

				case "plaintext":
					ClosePElementInButtonScope();
					InsertHtmlElement(token);
					_tokenizer.State = TokenizerState.Plaintext;
					return;

				case "button":
					if (HasInScope("button"))
					{
						GenerateImpliedEndTags();
						PopUntil("button");
					}

					ReconstructFormatting();
					InsertHtmlElement(token);
					_framesetOk = false;
					return;

				case "a":
					var index = FindFormatting("a");
					if (index >= 0)
					{
						// "<a>1<a>2": the first anchor is closed.
						var previous = _formatting[index];
						if (!AdoptionAgency("a"))
							AnyOtherEndTag(HtmlToken.EndTag("a"));
						_formatting.Remove(previous);
						_stack.Remove(previous);
					}

					ReconstructFormatting();
					PushFormatting(InsertHtmlElement(token));
					return;

				case "b":
				case "big":
				case "code":
				case "em":
				case "font":
				case "i":
				case "s":
				case "small":
				case "strike":
				case "strong":
				case "tt":
				case "u":
					ReconstructFormatting();
					PushFormatting(InsertHtmlElement(token));
					return;

				case "nobr":
					ReconstructFormatting();
					if (HasInScope("nobr"))
					{
						if (!AdoptionAgency("nobr"))
							AnyOtherEndTag(HtmlToken.EndTag("nobr"));
						ReconstructFormatting();
					}

					PushFormatting(InsertHtmlElement(token));
					return;

				case "applet":
				case "marquee":
				case "object":
					ReconstructFormatting();
					InsertHtmlElement(token);
					_formatting.Add(null);
					_framesetOk = false;
					return;

				case "table":
					if (_document.QuirksMode != HtmlQuirksMode.Quirks)
						ClosePElementInButtonScope();

					InsertHtmlElement(token);
					_framesetOk = false;
					_mode = InsertionMode.InTable;
					return;

				case "area":
				case "br":
				case "embed":
				case "img":
				case "keygen":
				case "wbr":
					ReconstructFormatting();
					InsertHtmlElement(token);
					Pop();
					_framesetOk = false;
					return;

				case "input":
					if (IsFragmentContext("select"))
						return;

					if (HasInScope("select"))
						PopUntil("select");

					ReconstructFormatting();
					InsertHtmlElement(token);
					Pop();

					var type = token.GetAttribute("type");
					if (type == null || !String.Equals(type, "hidden", StringComparison.OrdinalIgnoreCase))
						_framesetOk = false;
					return;

				case "param":
				case "source":
				case "track":
					InsertHtmlElement(token);
					Pop();
					return;

				case "hr":
					if (HasInScope("select"))
					{
						GenerateImpliedEndTags();
					}

					ClosePElementInButtonScope();
					InsertHtmlElement(token);
					Pop();
					_framesetOk = false;
					return;

				case "image":
					// "<image>" is a legacy alias of "<img>".
					token.Name = "img";
					Dispatch(token);
					return;

				case "textarea":
					InsertHtmlElement(token);
					_skipNextNewline = true;
					_tokenizer.State = TokenizerState.Rcdata;
					_originalMode = _mode;
					_framesetOk = false;
					_mode = InsertionMode.Text;
					return;

				case "xmp":
					ClosePElementInButtonScope();
					ReconstructFormatting();
					_framesetOk = false;
					ParseRawText(token, TokenizerState.Rawtext);
					return;

				case "iframe":
					_framesetOk = false;
					ParseRawText(token, TokenizerState.Rawtext);
					return;

				case "noembed":
					ParseRawText(token, TokenizerState.Rawtext);
					return;

				case "noscript":
					if (_scripting)
					{
						ParseRawText(token, TokenizerState.Rawtext);
						return;
					}

					ReconstructFormatting();
					InsertHtmlElement(token);
					return;

				case "select":
					if (IsFragmentContext("select"))
						return;

					// a nested select closes the open one.
					if (HasInScope("select"))
					{
						PopUntil("select");
						return;
					}

					ReconstructFormatting();
					InsertHtmlElement(token);
					_framesetOk = false;
					return;

				case "option":
					if (HasInScope("select"))
					{
						GenerateImpliedEndTags("optgroup");
					}
					else if (CurrentIs("option"))
					{
						Pop();
					}

					ReconstructFormatting();
					InsertHtmlElement(token);
					return;

				case "optgroup":
					if (HasInScope("select"))
					{
						GenerateImpliedEndTags();
					}
					else if (CurrentIs("option"))
					{
						Pop();
					}

					ReconstructFormatting();
					InsertHtmlElement(token);
					return;

				case "rb":
				case "rtc":
					if (HasInScope("ruby"))
						GenerateImpliedEndTags();

					InsertHtmlElement(token);
					return;

				case "rp":
				case "rt":
					if (HasInScope("ruby"))
						GenerateImpliedEndTags("rtc");

					InsertHtmlElement(token);
					return;

				case "math":
					ReconstructFormatting();
					InsertForeignElement(token, HtmlNamespaces.MathML);
					return;

				case "svg":
					ReconstructFormatting();
					InsertForeignElement(token, HtmlNamespaces.Svg);
					return;

				case "caption":
				case "col":
				case "colgroup":
				case "frame":
				case "head":
				case "tbody":
				case "td":
				case "tfoot":
				case "th":
				case "thead":
				case "tr":
					return;

				default:
					ReconstructFormatting();
					InsertHtmlElement(token);
					return;
			}
		}

		/// <summary>
		/// Processes an end tag in the "in body" insertion mode.
		/// </summary>
		/// <param name="token">The end tag.</param>
		private void InBodyEndTag(HtmlToken token)
		{
			switch (token.Name)
			{
				case "template":
					InHeadMode(token);
					return;

				case "body":
					if (HasInScope("body"))
						_mode = InsertionMode.AfterBody;
					return;

				case "html":
					if (HasInScope("body"))
						Reprocess(InsertionMode.AfterBody, token);
					return;

				case "address":
				case "article":
				case "aside":
				case "blockquote":
				case "button":
				case "center":
				case "details":
				case "dialog":
				case "dir":
				case "div":
				case "dl":
				case "fieldset":
				case "figcaption":
				case "figure":
				case "footer":
				case "header":
				case "hgroup":
				case "listing":
				case "main":
				case "menu":
				case "nav":
				case "ol":
				case "pre":
				case "search":
				case "section":
				case "select":
				case "summary":
				case "ul":
					if (!HasInScope(token.Name))
						return;

					GenerateImpliedEndTags();
					PopUntil(token.Name);
					return;

				case "form":
					if (!StackContains("template"))
					{
						var node = _form;
						_form = null;

						if (node == null || !HasElementInScope(node))
							return;

						GenerateImpliedEndTags();
						_stack.Remove(node);
					}
					else
					{
						if (!HasInScope("form"))
							return;

						GenerateImpliedEndTags();
						PopUntil("form");
					}
					return;

				case "p":
					if (!HasInScope("p", Scope.Button))
						InsertHtmlElement("p");

					ClosePElement();
					return;

				case "li":
					if (!HasInScope("li", Scope.ListItem))
						return;

					GenerateImpliedEndTags("li");
					PopUntil("li");
					return;

				case "dd":
				case "dt":
					if (!HasInScope(token.Name))
						return;

					GenerateImpliedEndTags(token.Name);
					PopUntil(token.Name);
					return;

				case "h1":
				case "h2":
				case "h3":
				case "h4":
				case "h5":
				case "h6":
					// any heading closes any open heading: "<h1>x</h2>".
					if (!HasAnyInScope(Headings))
						return;

					GenerateImpliedEndTags();
					PopUntilOneOf(Headings);
					return;

				case "a":
				case "b":
				case "big":
				case "code":
				case "em":
				case "font":
				case "i":
				case "nobr":
				case "s":
				case "small":
				case "strike":
				case "strong":
				case "tt":
				case "u":
					if (!AdoptionAgency(token.Name))
						AnyOtherEndTag(token);
					return;

				case "applet":
				case "marquee":
				case "object":
					if (!HasInScope(token.Name))
						return;

					GenerateImpliedEndTags();
					PopUntil(token.Name);
					ClearFormattingToLastMarker();
					return;

				case "br":
					// "</br>" is treated as "<br>".
					InBodyStartTag(HtmlToken.StartTag("br"));
					return;

				default:
					AnyOtherEndTag(token);
					return;
			}
		}

		/// <summary>
		/// Processes the "any other end tag" rule of the "in body" insertion mode.
		/// </summary>
		/// <param name="token">The end tag.</param>
		private void AnyOtherEndTag(HtmlToken token)
		{
			for (var i = _stack.Count - 1; i >= 0; i--)
			{
				var node = _stack[i];

				if (node.Is(token.Name))
				{
					GenerateImpliedEndTags(token.Name);
					PopUntil(node);
					return;
				}

				// an unrelated end tag cannot close a special element.
				if (ElementCategories.IsSpecial(node))
					return;
			}
		}

		/// <summary>
		/// Starts a list item (<c>li</c>, <c>dd</c> or <c>dt</c>), closing the open list item of the same kind.
		/// </summary>
		/// <param name="token">The start tag.</param>
		/// <param name="names">The names of the list items closed by the new one.</param>
		private void StartListItem(HtmlToken token, params string[] names)
		{
			_framesetOk = false;

			for (var i = _stack.Count - 1; i >= 0; i--)
			{
				var node = _stack[i];

				if (node.IsHtml && Array.IndexOf(names, node.LocalName) >= 0)
				{
					GenerateImpliedEndTags(node.LocalName);
					PopUntil(node.LocalName);
					break;
				}

				if (ElementCategories.IsSpecial(node) && !node.Is("address") && !node.Is("div") && !node.Is("p"))
					break;
			}

			ClosePElementInButtonScope();
			InsertHtmlElement(token);
		}

		/// <summary>
		/// Adds the attributes of a repeated <c>&lt;html&gt;</c> or <c>&lt;body&gt;</c> tag that the element does not have yet.
		/// </summary>
		/// <param name="element">The existing element.</param>
		/// <param name="token">The repeated start tag.</param>
		private static void MergeAttributes(HtmlElement element, HtmlToken token)
		{
			foreach (var attribute in token.Attributes)
			{
				if (!element.HasAttribute(attribute.Name))
					element.Attributes.Add(new HtmlAttribute(null, attribute.Name, null, attribute.Value));
			}
		}

		/// <summary>
		/// Determines whether the parser is parsing a fragment for a context element with the specified name.
		/// </summary>
		/// <param name="name">The tag name.</param>
		/// <returns><c>true</c> in the fragment case with a matching HTML context element.</returns>
		private bool IsFragmentContext(string name)
		{
			return _context != null && _context.Is(name);
		}
	}
}

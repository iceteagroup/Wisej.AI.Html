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
	/// The document level insertion modes: before the body, text, template, after the body and frameset
	/// (HTML standard, 13.2.6.4).
	/// </summary>
	internal sealed partial class HtmlTreeBuilder
	{
		#region Initial and before html

		/// <summary>
		/// 13.2.6.4.1 The "initial" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void InitialMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Characters:
					token = SplitWhitespace(token, out _);
					if (token == null)
						return;
					break;

				case HtmlTokenType.Comment:
					InsertComment(token, _document);
					return;

				case HtmlTokenType.Doctype:
					_document.Link(new HtmlDocumentType(_document, token.Name, token.PublicId, token.SystemId), null);
					_document.QuirksMode = QuirksModes.FromDoctype(token);
					_mode = InsertionMode.BeforeHtml;
					return;
			}

			// a document without a doctype renders in quirks mode.
			_document.QuirksMode = HtmlQuirksMode.Quirks;
			Reprocess(InsertionMode.BeforeHtml, token);
		}

		/// <summary>
		/// 13.2.6.4.2 The "before html" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void BeforeHtmlMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Doctype:
					return;

				case HtmlTokenType.Comment:
					InsertComment(token, _document);
					return;

				case HtmlTokenType.Characters:
					token = SplitWhitespace(token, out _);
					if (token == null)
						return;
					break;

				case HtmlTokenType.StartTag:
					if (token.Name == "html")
					{
						var html = CreateElement(token, HtmlNamespaces.Html);
						_document.Link(html, null);
						_stack.Add(html);
						_mode = InsertionMode.BeforeHead;
						return;
					}
					break;

				case HtmlTokenType.EndTag:
					if (token.Name != "head" && token.Name != "body" && token.Name != "html" && token.Name != "br")
						return;
					break;
			}

			var root = new HtmlElement(_document, "html", HtmlNamespaces.Html);
			_document.Link(root, null);
			_stack.Add(root);
			Reprocess(InsertionMode.BeforeHead, token);
		}

		#endregion

		#region Head

		/// <summary>
		/// 13.2.6.4.3 The "before head" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void BeforeHeadMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Characters:
					token = SplitWhitespace(token, out _);
					if (token == null)
						return;
					break;

				case HtmlTokenType.Comment:
					InsertComment(token);
					return;

				case HtmlTokenType.Doctype:
					return;

				case HtmlTokenType.StartTag:
					if (token.Name == "html")
					{
						InBodyMode(token);
						return;
					}

					if (token.Name == "head")
					{
						_head = InsertHtmlElement(token);
						_mode = InsertionMode.InHead;
						return;
					}
					break;

				case HtmlTokenType.EndTag:
					if (token.Name != "head" && token.Name != "body" && token.Name != "html" && token.Name != "br")
						return;
					break;
			}

			_head = InsertHtmlElement("head");
			Reprocess(InsertionMode.InHead, token);
		}

		/// <summary>
		/// 13.2.6.4.4 The "in head" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void InHeadMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Characters:
					token = SplitWhitespace(token, out var whitespace);
					InsertCharacters(whitespace);
					if (token == null)
						return;
					break;

				case HtmlTokenType.Comment:
					InsertComment(token);
					return;

				case HtmlTokenType.Doctype:
					return;

				case HtmlTokenType.StartTag:
					switch (token.Name)
					{
						case "html":
							InBodyMode(token);
							return;

						case "base":
						case "basefont":
						case "bgsound":
						case "link":
						case "meta":
							InsertHtmlElement(token);
							Pop();
							return;

						case "title":
							ParseRawText(token, TokenizerState.Rcdata);
							return;

						case "noscript":
							if (_scripting)
							{
								ParseRawText(token, TokenizerState.Rawtext);
							}
							else
							{
								InsertHtmlElement(token);
								_mode = InsertionMode.InHeadNoscript;
							}
							return;

						case "noframes":
						case "style":
							ParseRawText(token, TokenizerState.Rawtext);
							return;

						case "script":
							InsertHtmlElement(token);
							_tokenizer.State = TokenizerState.ScriptData;
							_originalMode = _mode;
							_mode = InsertionMode.Text;
							return;

						case "template":
							InsertHtmlElement(token);
							_formatting.Add(null);
							_framesetOk = false;
							_mode = InsertionMode.InTemplate;
							_templateModes.Add(InsertionMode.InTemplate);
							return;

						case "head":
							return;
					}
					break;

				case HtmlTokenType.EndTag:
					switch (token.Name)
					{
						case "head":
							Pop();
							_mode = InsertionMode.AfterHead;
							return;

						case "body":
						case "html":
						case "br":
							break;

						case "template":
							if (!StackContains("template"))
								return;

							GenerateAllImpliedEndTagsThoroughly();
							PopUntil("template");
							ClearFormattingToLastMarker();
							_templateModes.RemoveAt(_templateModes.Count - 1);
							ResetInsertionMode();
							return;

						default:
							return;
					}
					break;
			}

			Pop();
			Reprocess(InsertionMode.AfterHead, token);
		}

		/// <summary>
		/// 13.2.6.4.5 The "in head noscript" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void InHeadNoscriptMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Doctype:
					return;

				case HtmlTokenType.Comment:
					InHeadMode(token);
					return;

				case HtmlTokenType.Characters:
					token = SplitWhitespace(token, out var whitespace);
					if (whitespace.Length > 0)
						InHeadMode(HtmlToken.Characters(whitespace));
					if (token == null)
						return;
					break;

				case HtmlTokenType.StartTag:
					switch (token.Name)
					{
						case "html":
							InBodyMode(token);
							return;

						case "basefont":
						case "bgsound":
						case "link":
						case "meta":
						case "noframes":
						case "style":
							InHeadMode(token);
							return;

						case "head":
						case "noscript":
							return;
					}
					break;

				case HtmlTokenType.EndTag:
					if (token.Name == "noscript")
					{
						Pop();
						_mode = InsertionMode.InHead;
						return;
					}

					if (token.Name != "br")
						return;
					break;
			}

			Pop();
			Reprocess(InsertionMode.InHead, token);
		}

		/// <summary>
		/// 13.2.6.4.6 The "after head" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void AfterHeadMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Characters:
					token = SplitWhitespace(token, out var whitespace);
					InsertCharacters(whitespace);
					if (token == null)
						return;
					break;

				case HtmlTokenType.Comment:
					InsertComment(token);
					return;

				case HtmlTokenType.Doctype:
					return;

				case HtmlTokenType.StartTag:
					switch (token.Name)
					{
						case "html":
							InBodyMode(token);
							return;

						case "body":
							InsertHtmlElement(token);
							_framesetOk = false;
							_mode = InsertionMode.InBody;
							return;

						case "frameset":
							InsertHtmlElement(token);
							_mode = InsertionMode.InFrameset;
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
							// head content after </head> still goes into the head.
							_stack.Add(_head);
							InHeadMode(token);
							_stack.Remove(_head);
							return;

						case "head":
							return;
					}
					break;

				case HtmlTokenType.EndTag:
					if (token.Name == "template")
					{
						InHeadMode(token);
						return;
					}

					if (token.Name != "body" && token.Name != "html" && token.Name != "br")
						return;
					break;
			}

			InsertHtmlElement("body");
			Reprocess(InsertionMode.InBody, token);
		}

		#endregion

		#region Text and template

		/// <summary>
		/// 13.2.6.4.8 The "text" insertion mode, used inside raw text and RCDATA elements.
		/// </summary>
		/// <param name="token">The token.</param>
		private void TextMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Characters:
					InsertCharacters(token.Data);
					return;

				case HtmlTokenType.EndOfFile:
					Pop();
					Reprocess(_originalMode, token);
					return;

				case HtmlTokenType.EndTag:
					Pop();
					_mode = _originalMode;
					return;
			}
		}

		/// <summary>
		/// 13.2.6.4.18 The "in template" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void InTemplateMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Characters:
				case HtmlTokenType.Comment:
				case HtmlTokenType.Doctype:
					InBodyMode(token);
					return;

				case HtmlTokenType.StartTag:
					switch (token.Name)
					{
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

						case "caption":
						case "colgroup":
						case "tbody":
						case "tfoot":
						case "thead":
							SwitchTemplateMode(InsertionMode.InTable, token);
							return;

						case "col":
							SwitchTemplateMode(InsertionMode.InColumnGroup, token);
							return;

						case "tr":
							SwitchTemplateMode(InsertionMode.InTableBody, token);
							return;

						case "td":
						case "th":
							SwitchTemplateMode(InsertionMode.InRow, token);
							return;

						default:
							SwitchTemplateMode(InsertionMode.InBody, token);
							return;
					}

				case HtmlTokenType.EndTag:
					if (token.Name == "template")
						InHeadMode(token);
					return;

				case HtmlTokenType.EndOfFile:
					if (!StackContains("template"))
						return;

					PopUntil("template");
					ClearFormattingToLastMarker();
					_templateModes.RemoveAt(_templateModes.Count - 1);
					ResetInsertionMode();
					Dispatch(token);
					return;
			}
		}

		/// <summary>
		/// Replaces the current template insertion mode and reprocesses the token in the new mode.
		/// </summary>
		/// <param name="mode">The new template insertion mode.</param>
		/// <param name="token">The token.</param>
		private void SwitchTemplateMode(InsertionMode mode, HtmlToken token)
		{
			_templateModes[_templateModes.Count - 1] = mode;
			Reprocess(mode, token);
		}

		#endregion

		#region After body and frameset

		/// <summary>
		/// 13.2.6.4.19 The "after body" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void AfterBodyMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Characters:
					token = SplitWhitespace(token, out var whitespace);
					if (whitespace.Length > 0)
						InBodyMode(HtmlToken.Characters(whitespace));
					if (token == null)
						return;
					break;

				case HtmlTokenType.Comment:
					InsertComment(token, _stack[0]);
					return;

				case HtmlTokenType.Doctype:
					return;

				case HtmlTokenType.StartTag:
					if (token.Name == "html")
					{
						InBodyMode(token);
						return;
					}
					break;

				case HtmlTokenType.EndTag:
					if (token.Name == "html")
					{
						if (_context == null)
							_mode = InsertionMode.AfterAfterBody;
						return;
					}
					break;

				case HtmlTokenType.EndOfFile:
					return;
			}

			Reprocess(InsertionMode.InBody, token);
		}

		/// <summary>
		/// 13.2.6.4.20 The "in frameset" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void InFramesetMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Characters:
					InsertCharacters(KeepWhitespace(token.Data));
					return;

				case HtmlTokenType.Comment:
					InsertComment(token);
					return;

				case HtmlTokenType.StartTag:
					switch (token.Name)
					{
						case "html":
							InBodyMode(token);
							return;

						case "frameset":
							InsertHtmlElement(token);
							return;

						case "frame":
							InsertHtmlElement(token);
							Pop();
							return;

						case "noframes":
							InHeadMode(token);
							return;
					}
					return;

				case HtmlTokenType.EndTag:
					if (token.Name == "frameset")
					{
						if (this.CurrentNode == _stack[0])
							return;

						Pop();

						if (_context == null && !CurrentIs("frameset"))
							_mode = InsertionMode.AfterFrameset;
					}
					return;
			}
		}

		/// <summary>
		/// 13.2.6.4.21 The "after frameset" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void AfterFramesetMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Characters:
					InsertCharacters(KeepWhitespace(token.Data));
					return;

				case HtmlTokenType.Comment:
					InsertComment(token);
					return;

				case HtmlTokenType.StartTag:
					if (token.Name == "html")
						InBodyMode(token);
					else if (token.Name == "noframes")
						InHeadMode(token);
					return;

				case HtmlTokenType.EndTag:
					if (token.Name == "html")
						_mode = InsertionMode.AfterAfterFrameset;
					return;
			}
		}

		/// <summary>
		/// 13.2.6.4.22 The "after after body" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void AfterAfterBodyMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Comment:
					InsertComment(token, _document);
					return;

				case HtmlTokenType.Doctype:
					InBodyMode(token);
					return;

				case HtmlTokenType.Characters:
					token = SplitWhitespace(token, out var whitespace);
					if (whitespace.Length > 0)
						InBodyMode(HtmlToken.Characters(whitespace));
					if (token == null)
						return;
					break;

				case HtmlTokenType.StartTag:
					if (token.Name == "html")
					{
						InBodyMode(token);
						return;
					}
					break;

				case HtmlTokenType.EndOfFile:
					return;
			}

			Reprocess(InsertionMode.InBody, token);
		}

		/// <summary>
		/// 13.2.6.4.23 The "after after frameset" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void AfterAfterFramesetMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Comment:
					InsertComment(token, _document);
					return;

				case HtmlTokenType.Doctype:
					InBodyMode(token);
					return;

				case HtmlTokenType.Characters:
					var whitespace = KeepWhitespace(token.Data);
					if (whitespace.Length > 0)
						InBodyMode(HtmlToken.Characters(whitespace));
					return;

				case HtmlTokenType.StartTag:
					if (token.Name == "html")
						InBodyMode(token);
					else if (token.Name == "noframes")
						InHeadMode(token);
					return;
			}
		}

		#endregion
	}

	/// <summary>
	/// Determines the quirks mode from the document type declaration (HTML standard, 13.2.6.4.1).
	/// </summary>
	internal static class QuirksModes
	{
		/// <summary>
		/// The public identifier prefixes that select quirks mode.
		/// </summary>
		private static readonly string[] QuirkyPublicPrefixes =
		{
			"+//silmaril//dtd html pro v0r11 19970101//",
			"-//as//dtd html 3.0 aswedit + extensions//",
			"-//advasoft ltd//dtd html 3.0 aswedit + extensions//",
			"-//ietf//dtd html 2.0 level 1//",
			"-//ietf//dtd html 2.0 level 2//",
			"-//ietf//dtd html 2.0 strict level 1//",
			"-//ietf//dtd html 2.0 strict level 2//",
			"-//ietf//dtd html 2.0 strict//",
			"-//ietf//dtd html 2.0//",
			"-//ietf//dtd html 2.1e//",
			"-//ietf//dtd html 3.0//",
			"-//ietf//dtd html 3.2 final//",
			"-//ietf//dtd html 3.2//",
			"-//ietf//dtd html 3//",
			"-//ietf//dtd html level 0//",
			"-//ietf//dtd html level 1//",
			"-//ietf//dtd html level 2//",
			"-//ietf//dtd html level 3//",
			"-//ietf//dtd html strict level 0//",
			"-//ietf//dtd html strict level 1//",
			"-//ietf//dtd html strict level 2//",
			"-//ietf//dtd html strict level 3//",
			"-//ietf//dtd html strict//",
			"-//ietf//dtd html//",
			"-//metrius//dtd metrius presentational//",
			"-//microsoft//dtd internet explorer 2.0 html strict//",
			"-//microsoft//dtd internet explorer 2.0 html//",
			"-//microsoft//dtd internet explorer 2.0 tables//",
			"-//microsoft//dtd internet explorer 3.0 html strict//",
			"-//microsoft//dtd internet explorer 3.0 html//",
			"-//microsoft//dtd internet explorer 3.0 tables//",
			"-//netscape comm. corp.//dtd html//",
			"-//netscape comm. corp.//dtd strict html//",
			"-//o'reilly and associates//dtd html 2.0//",
			"-//o'reilly and associates//dtd html extended 1.0//",
			"-//o'reilly and associates//dtd html extended relaxed 1.0//",
			"-//sq//dtd html 2.0 hotmetal + extensions//",
			"-//softquad software//dtd hotmetal pro 6.0::19990601::extensions to html 4.0//",
			"-//softquad//dtd hotmetal pro 4.0::19971010::extensions to html 4.0//",
			"-//spyglass//dtd html 2.0 extended//",
			"-//sun microsystems corp.//dtd hotjava html//",
			"-//sun microsystems corp.//dtd hotjava strict html//",
			"-//w3c//dtd html 3 1995-03-24//",
			"-//w3c//dtd html 3.2 draft//",
			"-//w3c//dtd html 3.2 final//",
			"-//w3c//dtd html 3.2//",
			"-//w3c//dtd html 3.2s draft//",
			"-//w3c//dtd html 4.0 frameset//",
			"-//w3c//dtd html 4.0 transitional//",
			"-//w3c//dtd html experimental 19960712//",
			"-//w3c//dtd html experimental 970421//",
			"-//w3c//dtd w3 html//",
			"-//w3o//dtd w3 html 3.0//",
			"-//webtechs//dtd mozilla html 2.0//",
			"-//webtechs//dtd mozilla html//",
		};

		/// <summary>
		/// Returns the quirks mode selected by a document type token.
		/// </summary>
		/// <param name="token">The DOCTYPE token.</param>
		/// <returns>The quirks mode.</returns>
		public static HtmlQuirksMode FromDoctype(HtmlToken token)
		{
			var publicId = token.PublicId?.ToLowerInvariant();
			var systemId = token.SystemId?.ToLowerInvariant();

			if (token.ForceQuirks || token.Name != "html")
				return HtmlQuirksMode.Quirks;

			if (publicId == "-//w3o//dtd w3 html strict 3.0//en//" || publicId == "-/w3c/dtd html 4.0 transitional/en" || publicId == "html")
				return HtmlQuirksMode.Quirks;

			if (systemId == "http://www.ibm.com/data/dtd/v11/ibmxhtml1-transitional.dtd")
				return HtmlQuirksMode.Quirks;

			if (publicId != null)
			{
				foreach (var prefix in QuirkyPublicPrefixes)
				{
					if (publicId.StartsWith(prefix, StringComparison.Ordinal))
						return HtmlQuirksMode.Quirks;
				}

				var html401 = publicId.StartsWith("-//w3c//dtd html 4.01 frameset//", StringComparison.Ordinal)
					|| publicId.StartsWith("-//w3c//dtd html 4.01 transitional//", StringComparison.Ordinal);

				if (html401 && systemId == null)
					return HtmlQuirksMode.Quirks;

				if (html401
					|| publicId.StartsWith("-//w3c//dtd xhtml 1.0 frameset//", StringComparison.Ordinal)
					|| publicId.StartsWith("-//w3c//dtd xhtml 1.0 transitional//", StringComparison.Ordinal))
				{
					return HtmlQuirksMode.LimitedQuirks;
				}
			}

			return HtmlQuirksMode.NoQuirks;
		}
	}
}

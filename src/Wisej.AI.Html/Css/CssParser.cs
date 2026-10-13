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

namespace Wisej.AI.Html.Css
{
	/// <summary>
	/// A property declaration: <c>name: value !important</c>.
	/// </summary>
	internal sealed class CssDeclaration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CssDeclaration"/> class.
		/// </summary>
		/// <param name="name">The lowercase property name.</param>
		/// <param name="value">The value, without <c>!important</c>.</param>
		/// <param name="important">Whether the declaration is important.</param>
		public CssDeclaration(string name, string value, bool important)
		{
			this.Name = name;
			this.Value = value;
			this.Important = important;
		}

		/// <summary>Gets the lowercase property name.</summary>
		public string Name { get; }

		/// <summary>Gets the value.</summary>
		public string Value { get; }

		/// <summary>Gets whether the declaration is important.</summary>
		public bool Important { get; }
	}

	/// <summary>
	/// A style rule: a selector list and its declarations.
	/// </summary>
	internal sealed class CssRule
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CssRule"/> class.
		/// </summary>
		/// <param name="selectors">The selectors.</param>
		/// <param name="declarations">The declarations.</param>
		/// <param name="order">The position of the rule in the cascade.</param>
		public CssRule(SelectorList selectors, List<CssDeclaration> declarations, int order)
		{
			this.Selectors = selectors;
			this.Declarations = declarations;
			this.Order = order;
		}

		/// <summary>Gets the selectors.</summary>
		public SelectorList Selectors { get; }

		/// <summary>Gets the declarations.</summary>
		public List<CssDeclaration> Declarations { get; }

		/// <summary>Gets the position of the rule in the cascade.</summary>
		public int Order { get; }
	}

	/// <summary>
	/// A lenient CSS parser that extracts the style rules relevant to content extraction.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only the declarations of the properties listed in <see cref="RelevantProperties"/> are kept, so
	/// large style sheets made of colors, sizes and animations cost nothing after parsing.
	/// </para>
	/// <para>
	/// <c>@media</c> blocks are applied when they target all media, screen or print without media
	/// features: responsive rules (<c>(max-width: 600px)</c>) describe alternative layouts and are skipped.
	/// <c>@supports</c> and <c>@layer</c> blocks are applied; every other at-rule is skipped.
	/// </para>
	/// </remarks>
	internal sealed class CssParser
	{
		/// <summary>
		/// The properties that influence the extracted content.
		/// </summary>
		public static readonly HashSet<string> RelevantProperties = new HashSet<string>(StringComparer.Ordinal)
		{
			"display", "visibility", "font-weight", "font-style", "font-family", "font", "font-size",
			"text-decoration", "text-decoration-line", "vertical-align", "white-space",
			"page-break-before", "page-break-after", "break-before", "break-after",
			"list-style-type", "list-style", "clip", "clip-path", "mso-hide", "content-visibility",
		};

		/// <summary>The maximum nesting of the applied at-rule blocks.</summary>
		private const int MaxNesting = 32;

		// the style sheet text and the current position.
		private readonly string _text;
		private int _pos;

		// the nesting of the at-rule blocks being parsed.
		private int _nesting;

		// the rules found so far, and the cascade order of the next rule.
		private readonly List<CssRule> _rules;
		private int _order;

		/// <summary>
		/// Initializes a new instance of the <see cref="CssParser"/> class.
		/// </summary>
		/// <param name="text">The style sheet.</param>
		/// <param name="rules">Receives the rules.</param>
		/// <param name="order">The cascade order of the first rule.</param>
		private CssParser(string text, List<CssRule> rules, int order)
		{
			_text = text;
			_rules = rules;
			_order = order;
		}

		/// <summary>
		/// Parses a style sheet and appends its rules to a list.
		/// </summary>
		/// <param name="text">The style sheet.</param>
		/// <param name="rules">Receives the rules, in cascade order.</param>
		public static void ParseStyleSheet(string text, List<CssRule> rules)
		{
			if (String.IsNullOrEmpty(text))
				return;

			var parser = new CssParser(StripComments(text), rules, rules.Count);
			parser.ParseRules(nested: false);
		}

		/// <summary>
		/// Parses a list of declarations, such as the value of a <c>style</c> attribute.
		/// </summary>
		/// <param name="text">The declarations.</param>
		/// <returns>The relevant declarations.</returns>
		public static List<CssDeclaration> ParseDeclarations(string text)
		{
			var result = new List<CssDeclaration>();
			if (String.IsNullOrEmpty(text))
				return result;

			text = StripComments(text);

			var start = 0;
			var depth = 0;
			var quote = '\0';

			for (var i = 0; i <= text.Length; i++)
			{
				var c = i < text.Length ? text[i] : ';';

				if (quote != '\0')
				{
					if (c == '\\')
						i++;
					else if (c == quote)
						quote = '\0';

					continue;
				}

				switch (c)
				{
					case '"':
					case '\'':
						quote = c;
						break;

					case '(':
					case '{':
					case '[':
						depth++;
						break;

					case ')':
					case '}':
					case ']':
						depth = Math.Max(0, depth - 1);
						break;

					case ';':
						if (depth == 0)
						{
							AddDeclaration(text.Substring(start, i - start), result);
							start = i + 1;
						}
						break;
				}
			}

			return result;
		}

		#region Implementation

		/// <summary>
		/// Parses rules until the end of the text or of the enclosing block.
		/// </summary>
		/// <param name="nested">Whether the rules are inside an at-rule block that ends with <c>}</c>.</param>
		private void ParseRules(bool nested)
		{
			while (true)
			{
				SkipWhitespace();
				if (_pos >= _text.Length)
					return;

				var c = _text[_pos];
				if (c == '}')
				{
					_pos++;
					if (nested)
						return;

					continue;
				}

				if (c == '@')
				{
					ParseAtRule();
					continue;
				}

				// "<!--" and "-->" are allowed around style sheets in <style> elements.
				if (_text.Length - _pos >= 4 && String.CompareOrdinal(_text, _pos, "<!--", 0, 4) == 0)
				{
					_pos += 4;
					continue;
				}

				if (_text.Length - _pos >= 3 && String.CompareOrdinal(_text, _pos, "-->", 0, 3) == 0)
				{
					_pos += 3;
					continue;
				}

				var open = FindOutsideStrings('{');
				if (open < 0)
					return;

				var prelude = _text.Substring(_pos, open - _pos).Trim();
				_pos = open + 1;

				var close = FindBlockEnd(_pos);
				var body = _text.Substring(_pos, close - _pos);
				_pos = Math.Min(_text.Length, close + 1);

				AddRule(prelude, body);
			}
		}

		/// <summary>
		/// Parses an at-rule, applying the content of the conditional blocks that apply.
		/// </summary>
		private void ParseAtRule()
		{
			var start = _pos;
			_pos++;
			while (_pos < _text.Length && (Char.IsLetterOrDigit(_text[_pos]) || _text[_pos] == '-'))
				_pos++;

			var name = _text.Substring(start + 1, _pos - start - 1).ToLowerInvariant();

			var semicolon = FindOutsideStrings(';');
			var open = FindOutsideStrings('{');

			// statement at-rules: @import, @charset, @namespace, @layer a, b;
			if (open < 0 || (semicolon >= 0 && semicolon < open))
			{
				_pos = semicolon < 0 ? _text.Length : semicolon + 1;
				return;
			}

			var prelude = _text.Substring(_pos, open - _pos).Trim().ToLowerInvariant();
			_pos = open + 1;

			// nested conditional blocks recurse: deeper blocks are skipped to protect the stack.
			var applies = name == "supports" || name == "layer" || (name == "media" && MediaApplies(prelude));
			if (applies && _nesting < MaxNesting)
			{
				_nesting++;
				ParseRules(nested: true);
				_nesting--;
				return;
			}

			var close = FindBlockEnd(_pos);
			_pos = Math.Min(_text.Length, close + 1);
		}

		/// <summary>
		/// Determines whether a media query list applies to the extraction: all media, screen or print, without features.
		/// </summary>
		/// <param name="query">The lowercase media query list.</param>
		/// <returns><c>true</c> when the rules apply.</returns>
		private static bool MediaApplies(string query)
		{
			if (query.IndexOf('(') >= 0)
				return false;

			foreach (var part in query.Split(','))
			{
				var media = part.Trim();
				if (media.StartsWith("only ", StringComparison.Ordinal))
					media = media.Substring(5).Trim();

				if (media == "all" || media == "screen" || media == "print" || media.Length == 0)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Adds a style rule when its selector is supported and it declares relevant properties.
		/// </summary>
		/// <param name="prelude">The selector text.</param>
		/// <param name="body">The declaration block, without braces.</param>
		private void AddRule(string prelude, string body)
		{
			var order = _order++;

			var declarations = ParseDeclarations(body);
			if (declarations.Count == 0)
				return;

			var selectors = SelectorParser.TryParse(prelude);
			if (selectors == null)
			{
				// a list with one unsupported selector is invalid as a whole; keep the supported ones.
				var supported = new List<ComplexSelector>();
				foreach (var part in SplitSelectorList(prelude))
				{
					var parsed = SelectorParser.TryParse(part);
					if (parsed != null)
						supported.AddRange(parsed.Selectors);
				}

				if (supported.Count == 0)
					return;

				selectors = new SelectorList(supported);
			}

			_rules.Add(new CssRule(selectors, declarations, order));
		}

		/// <summary>
		/// Splits a selector list at the top-level commas.
		/// </summary>
		/// <param name="prelude">The selector list.</param>
		/// <returns>The individual selectors.</returns>
		private static List<string> SplitSelectorList(string prelude)
		{
			var parts = new List<string>();
			var depth = 0;
			var start = 0;

			for (var i = 0; i < prelude.Length; i++)
			{
				var c = prelude[i];
				if (c == '(' || c == '[')
				{
					depth++;
				}
				else if (c == ')' || c == ']')
				{
					depth--;
				}
				else if (c == ',' && depth == 0)
				{
					parts.Add(prelude.Substring(start, i - start));
					start = i + 1;
				}
			}

			parts.Add(prelude.Substring(start));
			return parts;
		}

		/// <summary>
		/// Parses one declaration and adds it when the property is relevant.
		/// </summary>
		/// <param name="text">The declaration text.</param>
		/// <param name="result">Receives the declaration.</param>
		private static void AddDeclaration(string text, List<CssDeclaration> result)
		{
			var colon = text.IndexOf(':');
			if (colon <= 0)
				return;

			var name = text.Substring(0, colon).Trim().ToLowerInvariant();
			if (!RelevantProperties.Contains(name))
				return;

			var value = text.Substring(colon + 1).Trim();
			var important = false;

			var bang = value.LastIndexOf('!');
			if (bang >= 0 && value.Substring(bang + 1).Trim().Equals("important", StringComparison.OrdinalIgnoreCase))
			{
				important = true;
				value = value.Substring(0, bang).Trim();
			}

			if (value.Length > 0)
				result.Add(new CssDeclaration(name, value, important));
		}

		/// <summary>
		/// Finds a character outside strings and parentheses, from the current position.
		/// </summary>
		/// <param name="target">The character.</param>
		/// <returns>The position, or -1.</returns>
		private int FindOutsideStrings(char target)
		{
			var quote = '\0';
			var depth = 0;

			for (var i = _pos; i < _text.Length; i++)
			{
				var c = _text[i];

				if (quote != '\0')
				{
					if (c == '\\')
						i++;
					else if (c == quote)
						quote = '\0';

					continue;
				}

				if (c == '"' || c == '\'')
					quote = c;
				else if (c == '(')
					depth++;
				else if (c == ')')
					depth = Math.Max(0, depth - 1);
				else if (c == target && depth == 0)
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Finds the brace that closes a block, skipping nested blocks and strings.
		/// </summary>
		/// <param name="start">The position after the opening brace.</param>
		/// <returns>The position of the closing brace, or the end of the text.</returns>
		private int FindBlockEnd(int start)
		{
			var depth = 1;
			var quote = '\0';

			for (var i = start; i < _text.Length; i++)
			{
				var c = _text[i];

				if (quote != '\0')
				{
					if (c == '\\')
						i++;
					else if (c == quote)
						quote = '\0';

					continue;
				}

				if (c == '"' || c == '\'')
				{
					quote = c;
				}
				else if (c == '{')
				{
					depth++;
				}
				else if (c == '}')
				{
					depth--;
					if (depth == 0)
						return i;
				}
			}

			return _text.Length;
		}

		/// <summary>
		/// Skips white space.
		/// </summary>
		private void SkipWhitespace()
		{
			while (_pos < _text.Length && Char.IsWhiteSpace(_text[_pos]))
				_pos++;
		}

		/// <summary>
		/// Removes the comments of a style sheet, keeping strings intact.
		/// </summary>
		/// <param name="text">The style sheet.</param>
		/// <returns>The text without comments.</returns>
		private static string StripComments(string text)
		{
			if (text.IndexOf("/*", StringComparison.Ordinal) < 0)
				return text;

			var sb = new StringBuilder(text.Length);
			var quote = '\0';

			for (var i = 0; i < text.Length; i++)
			{
				var c = text[i];

				if (quote != '\0')
				{
					sb.Append(c);
					if (c == '\\' && i + 1 < text.Length)
						sb.Append(text[++i]);
					else if (c == quote)
						quote = '\0';

					continue;
				}

				if (c == '"' || c == '\'')
				{
					quote = c;
					sb.Append(c);
				}
				else if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
				{
					var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
					i = end < 0 ? text.Length : end + 1;
					sb.Append(' ');
				}
				else
				{
					sb.Append(c);
				}
			}

			return sb.ToString();
		}

		#endregion
	}
}

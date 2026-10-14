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
using System.Text;

namespace Wisej.AI.Html.Css
{
	/// <summary>
	/// Parses CSS selector lists (Selectors Level 4) into <see cref="SelectorList"/> instances.
	/// </summary>
	/// <remarks>
	/// Supported: type, universal, id, class and attribute selectors (all operators and the <c>i</c>/<c>s</c>
	/// flags), the four combinators, the structural and state pseudo-classes, and <c>:not()</c>, <c>:is()</c>,
	/// <c>:where()</c> and <c>:has()</c>. Pseudo-elements and namespace prefixes are rejected, so a style sheet
	/// rule that uses them is skipped instead of misapplied.
	///
	/// Syntax errors are recorded instead of thrown: style sheets are full of unsupported selectors and
	/// <see cref="TryParse"/> must reject them without the cost of an exception each.
	/// </remarks>
	internal sealed class SelectorParser
	{
		/// <summary>The maximum nesting of pseudo-classes with a selector argument, such as <c>:not(:is(...))</c>.</summary>
		private const int MaxNesting = 16;

		// the selector text and the current position.
		private readonly string _text;
		private int _pos;

		// the nesting of the pseudo-class arguments being parsed.
		private int _nesting;

		// the first syntax error, or null.
		private string _error;

		/// <summary>
		/// Initializes a new instance of the <see cref="SelectorParser"/> class.
		/// </summary>
		/// <param name="text">The selector text.</param>
		private SelectorParser(string text)
		{
			_text = text;
		}

		/// <summary>
		/// Parses a selector list.
		/// </summary>
		/// <param name="selectors">The selector text.</param>
		/// <returns>The parsed list.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="selectors"/> is <c>null</c>.</exception>
		/// <exception cref="FormatException">Thrown when the text is not a valid or supported selector list.</exception>
		public static SelectorList Parse(string selectors)
		{
			if (selectors == null)
				throw new ArgumentNullException(nameof(selectors));

			var parser = new SelectorParser(selectors);
			var list = parser.ParseAll();
			if (list == null)
				throw new FormatException(parser._error);

			return list;
		}

		/// <summary>
		/// Parses a selector list and returns <c>null</c> instead of throwing on unsupported selectors.
		/// </summary>
		/// <param name="selectors">The selector text.</param>
		/// <returns>The parsed list, or <c>null</c>.</returns>
		public static SelectorList TryParse(string selectors)
		{
			if (selectors == null)
				return null;

			return new SelectorParser(selectors).ParseAll();
		}

		#region Grammar

		/// <summary>
		/// Parses the whole text as a selector list.
		/// </summary>
		/// <returns>The list, or <c>null</c> on a syntax error.</returns>
		private SelectorList ParseAll()
		{
			var list = ParseList(nested: false, relative: false, out _);
			if (list == null)
				return null;

			SkipWhitespace();
			if (_pos < _text.Length)
				return Fail<SelectorList>("unexpected character");

			return list;
		}

		/// <summary>
		/// Parses a comma separated list of complex selectors.
		/// </summary>
		/// <param name="nested">Whether the list is the argument of a pseudo-class and ends at <c>)</c>.</param>
		/// <param name="relative">Whether the selectors are relative (the argument of <c>:has()</c>).</param>
		/// <param name="relativeCombinator">Receives the leading combinator of the first relative selector.</param>
		/// <returns>The list, or <c>null</c> on a syntax error.</returns>
		private SelectorList ParseList(bool nested, bool relative, out char relativeCombinator)
		{
			var selectors = new List<ComplexSelector>();
			relativeCombinator = ' ';

			while (true)
			{
				SkipWhitespace();

				if (relative && _pos < _text.Length && (_text[_pos] == '>' || _text[_pos] == '+' || _text[_pos] == '~'))
				{
					relativeCombinator = _text[_pos++];
					SkipWhitespace();
				}

				var complex = ParseComplex();
				if (complex == null)
					return null;

				selectors.Add(complex);
				SkipWhitespace();

				if (_pos < _text.Length && _text[_pos] == ',')
				{
					_pos++;
					continue;
				}

				if (_pos >= _text.Length || (nested && _text[_pos] == ')'))
					break;

				return Fail<SelectorList>("expected ',' or the end of the selector");
			}

			return new SelectorList(selectors);
		}

		/// <summary>
		/// Parses a complex selector: compound selectors joined by combinators.
		/// </summary>
		/// <returns>The complex selector, or <c>null</c> on a syntax error.</returns>
		private ComplexSelector ParseComplex()
		{
			var first = ParseCompound();
			if (first == null)
				return null;

			var compounds = new List<CompoundSelector> { first };
			var combinators = new List<char>();

			while (true)
			{
				var hadWhitespace = SkipWhitespace();
				if (_pos >= _text.Length)
					break;

				var c = _text[_pos];
				if (c == ',' || c == ')')
					break;

				char combinator;
				if (c == '>' || c == '+' || c == '~')
				{
					combinator = c;
					_pos++;
					SkipWhitespace();
				}
				else if (hadWhitespace)
				{
					combinator = ' ';
				}
				else
				{
					return Fail<ComplexSelector>("unexpected character");
				}

				var compound = ParseCompound();
				if (compound == null)
					return null;

				combinators.Add(combinator);
				compounds.Add(compound);
			}

			return new ComplexSelector(compounds, combinators);
		}

		/// <summary>
		/// Parses a compound selector.
		/// </summary>
		/// <returns>The compound selector, or <c>null</c> on a syntax error.</returns>
		private CompoundSelector ParseCompound()
		{
			var compound = new CompoundSelector();
			var any = false;

			if (_pos < _text.Length && _text[_pos] == '*')
			{
				_pos++;
				any = true;
			}
			else if (_pos < _text.Length && IsNameStart(_text[_pos]))
			{
				var tag = ReadIdentifier();
				if (tag == null)
					return null;

				compound.Tag = tag.ToLowerInvariant();
				any = true;
			}

			if (_pos < _text.Length && _text[_pos] == '|')
				return Fail<CompoundSelector>("namespace prefixes are not supported");

			while (_pos < _text.Length)
			{
				var c = _text[_pos];
				SimpleSelector condition;

				if (c == '#')
				{
					_pos++;
					var id = ReadIdentifier();
					condition = id == null ? null : new IdSelector(id);
				}
				else if (c == '.')
				{
					_pos++;
					var name = ReadIdentifier();
					condition = name == null ? null : new ClassSelector(name);
				}
				else if (c == '[')
				{
					_pos++;
					condition = ParseAttribute();
				}
				else if (c == ':')
				{
					_pos++;
					condition = ParsePseudoClass();
				}
				else
				{
					break;
				}

				if (condition == null)
					return null;

				compound.Conditions.Add(condition);
				any = true;
			}

			if (!any)
				return Fail<CompoundSelector>("expected a selector");

			return compound;
		}

		/// <summary>
		/// Parses an attribute selector after <c>[</c>.
		/// </summary>
		/// <returns>The attribute selector, or <c>null</c> on a syntax error.</returns>
		private SimpleSelector ParseAttribute()
		{
			SkipWhitespace();
			var name = ReadIdentifier()?.ToLowerInvariant();
			if (name == null)
				return null;

			SkipWhitespace();

			if (Peek() == ']')
			{
				_pos++;
				return new AttributeSelector(name, null, null, false);
			}

			string op;
			if (Peek() == '=')
			{
				op = "=";
				_pos++;
			}
			else if (_pos + 1 < _text.Length && "~|^$*".IndexOf(_text[_pos]) >= 0 && _text[_pos + 1] == '=')
			{
				op = _text.Substring(_pos, 2);
				_pos += 2;
			}
			else
			{
				return Fail<SimpleSelector>("invalid attribute selector");
			}

			SkipWhitespace();
			var value = Peek() == '"' || Peek() == '\'' ? ReadString() : ReadIdentifier();
			if (value == null)
				return null;

			SkipWhitespace();

			var ignoreCase = false;
			if (Peek() == 'i' || Peek() == 'I' || Peek() == 's' || Peek() == 'S')
			{
				ignoreCase = Peek() == 'i' || Peek() == 'I';
				_pos++;
				SkipWhitespace();
			}

			if (Peek() != ']')
				return Fail<SimpleSelector>("expected ']'");

			_pos++;
			return new AttributeSelector(name, op, value, ignoreCase);
		}

		/// <summary>
		/// Parses a pseudo-class after <c>:</c>.
		/// </summary>
		/// <returns>The pseudo-class selector, or <c>null</c> on a syntax error.</returns>
		private SimpleSelector ParsePseudoClass()
		{
			if (Peek() == ':')
				return Fail<SimpleSelector>("pseudo-elements are not supported");

			var name = ReadIdentifier()?.ToLowerInvariant();
			if (name == null)
				return null;

			switch (name)
			{
				// legacy pseudo-elements written with a single colon.
				case "before":
				case "after":
				case "first-line":
				case "first-letter":
					return Fail<SimpleSelector>("pseudo-elements are not supported");
			}

			if (Peek() != '(')
			{
				if (!PseudoClassSelector.IsSupported(name) || name.StartsWith("nth-", StringComparison.Ordinal))
					return Fail<SimpleSelector>("unsupported pseudo-class :" + name);

				return new PseudoClassSelector(name);
			}

			_pos++;
			SimpleSelector result;

			// nested pseudo-classes recurse when parsed and matched: bound them to protect the stack.
			if (++_nesting > MaxNesting)
				return Fail<SimpleSelector>("selector nested too deeply");

			switch (name)
			{
				case "not":
				case "is":
				case "where":
				case "matches":
				case "any":
					var arguments = ParseList(nested: true, relative: false, out _);
					if (arguments == null)
						return null;

					result = new LogicalSelector(name == "matches" || name == "any" ? "is" : name, arguments);
					break;

				case "has":
					var list = ParseList(nested: true, relative: true, out var combinator);
					if (list == null)
						return null;

					result = new LogicalSelector(name, list, combinator);
					break;

				case "nth-child":
				case "nth-last-child":
				case "nth-of-type":
				case "nth-last-of-type":
					if (!ParseNth(out var a, out var b))
						return null;

					result = new PseudoClassSelector(name, a, b);
					break;

				default:
					return Fail<SimpleSelector>("unsupported pseudo-class :" + name + "()");
			}

			SkipWhitespace();
			if (Peek() != ')')
				return Fail<SimpleSelector>("expected ')'");

			_pos++;
			_nesting--;
			return result;
		}

		/// <summary>
		/// Parses an <c>an+b</c> argument, including <c>odd</c> and <c>even</c>.
		/// </summary>
		/// <param name="a">Receives the step.</param>
		/// <param name="b">Receives the offset.</param>
		/// <returns><c>true</c> when the expression is valid.</returns>
		private bool ParseNth(out int a, out int b)
		{
			SkipWhitespace();

			var start = _pos;
			while (_pos < _text.Length && _text[_pos] != ')')
				_pos++;

			var expression = _text.Substring(start, _pos - start).Replace(" ", "").ToLowerInvariant();

			if (expression == "odd")
			{
				a = 2;
				b = 1;
				return true;
			}

			if (expression == "even")
			{
				a = 2;
				b = 0;
				return true;
			}

			var n = expression.IndexOf('n');
			if (n < 0)
			{
				a = 0;
				return ParseInteger(expression, out b);
			}

			b = 0;
			var step = expression.Substring(0, n);
			if (step == "" || step == "+")
				a = 1;
			else if (step == "-")
				a = -1;
			else if (!ParseInteger(step, out a))
				return false;

			var offset = expression.Substring(n + 1);
			return offset.Length == 0 || ParseInteger(offset, out b);
		}

		#endregion

		#region Lexing

		/// <summary>
		/// Returns the current character without consuming it.
		/// </summary>
		/// <returns>The character, or <c>'\0'</c> at the end.</returns>
		private char Peek()
		{
			return _pos < _text.Length ? _text[_pos] : '\0';
		}

		/// <summary>
		/// Skips white space and comments.
		/// </summary>
		/// <returns><c>true</c> when white space was skipped.</returns>
		private bool SkipWhitespace()
		{
			var start = _pos;
			while (_pos < _text.Length)
			{
				if (Char.IsWhiteSpace(_text[_pos]))
				{
					_pos++;
				}
				else if (_text[_pos] == '/' && _pos + 1 < _text.Length && _text[_pos + 1] == '*')
				{
					var end = _text.IndexOf("*/", _pos + 2, StringComparison.Ordinal);
					_pos = end < 0 ? _text.Length : end + 2;
				}
				else
				{
					break;
				}
			}

			return _pos > start;
		}

		/// <summary>
		/// Reads a CSS identifier, decoding escapes.
		/// </summary>
		/// <returns>The identifier, or <c>null</c> on a syntax error.</returns>
		private string ReadIdentifier()
		{
			var sb = new StringBuilder();

			while (_pos < _text.Length)
			{
				var c = _text[_pos];
				if (c == '\\')
				{
					_pos++;
					sb.Append(ReadEscape());
				}
				else if (IsNameChar(c))
				{
					sb.Append(c);
					_pos++;
				}
				else
				{
					break;
				}
			}

			if (sb.Length == 0)
				return Fail<string>("expected an identifier");

			return sb.ToString();
		}

		/// <summary>
		/// Reads a quoted string, decoding escapes.
		/// </summary>
		/// <returns>The string, without quotes.</returns>
		private string ReadString()
		{
			var quote = _text[_pos++];
			var sb = new StringBuilder();

			while (_pos < _text.Length && _text[_pos] != quote)
			{
				if (_text[_pos] == '\\')
				{
					_pos++;
					sb.Append(ReadEscape());
				}
				else
				{
					sb.Append(_text[_pos++]);
				}
			}

			_pos++;
			return sb.ToString();
		}

		/// <summary>
		/// Reads an escape sequence after the backslash: up to six hexadecimal digits and an optional space, or one character.
		/// </summary>
		/// <returns>The escaped characters.</returns>
		private string ReadEscape()
		{
			if (_pos >= _text.Length)
				return "�";

			var start = _pos;
			while (_pos < _text.Length && _pos - start < 6 && Uri.IsHexDigit(_text[_pos]))
				_pos++;

			if (_pos == start)
				return _text[_pos++].ToString();

			var code = Int32.Parse(_text.Substring(start, _pos - start), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			if (_pos < _text.Length && Char.IsWhiteSpace(_text[_pos]))
				_pos++;

			return code == 0 || code > 0x10FFFF || (code >= 0xD800 && code <= 0xDFFF) ? "�" : Char.ConvertFromUtf32(code);
		}

		/// <summary>
		/// Determines whether a character can start an identifier.
		/// </summary>
		/// <param name="c">The character.</param>
		/// <returns><c>true</c> for letters, underscore, hyphen, backslash and non-ASCII characters.</returns>
		private static bool IsNameStart(char c)
		{
			return Char.IsLetter(c) || c == '_' || c == '-' || c == '\\' || c > 0x7F;
		}

		/// <summary>
		/// Determines whether a character can continue an identifier.
		/// </summary>
		/// <param name="c">The character.</param>
		/// <returns><c>true</c> for identifier characters.</returns>
		private static bool IsNameChar(char c)
		{
			return Char.IsLetterOrDigit(c) || c == '_' || c == '-' || c > 0x7F;
		}

		/// <summary>
		/// Parses a signed integer of an <c>an+b</c> expression.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="value">Receives the value.</param>
		/// <returns><c>true</c> when the text is a valid integer.</returns>
		private bool ParseInteger(string text, out int value)
		{
			if (Int32.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
				return true;

			Fail<string>("invalid an+b expression");
			return false;
		}

		/// <summary>
		/// Records a syntax error, keeping the first one.
		/// </summary>
		/// <typeparam name="T">The return type of the failing production.</typeparam>
		/// <param name="message">The description of the error.</param>
		/// <returns><c>null</c>, so a production can fail with <c>return Fail&lt;T&gt;(...)</c>.</returns>
		private T Fail<T>(string message) where T : class
		{
			if (_error == null)
				_error = $"Invalid selector \"{_text}\" at position {_pos}: {message}.";

			return null;
		}

		#endregion
	}
}

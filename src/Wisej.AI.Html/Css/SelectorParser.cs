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
	/// <c>:where()</c> and <c>:has()</c>. Pseudo-elements and namespace prefixes are rejected with a
	/// <see cref="FormatException"/>, so a style sheet rule that uses them is skipped instead of misapplied.
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
			var list = parser.ParseList(nested: false, relative: false, out _);

			parser.SkipWhitespace();
			if (parser._pos < parser._text.Length)
				throw parser.Error("unexpected character");

			return list;
		}

		/// <summary>
		/// Parses a selector list and returns <c>null</c> instead of throwing on unsupported selectors.
		/// </summary>
		/// <param name="selectors">The selector text.</param>
		/// <returns>The parsed list, or <c>null</c>.</returns>
		public static SelectorList TryParse(string selectors)
		{
			try
			{
				return Parse(selectors);
			}
			catch (FormatException)
			{
				return null;
			}
		}

		#region Grammar

		/// <summary>
		/// Parses a comma separated list of complex selectors.
		/// </summary>
		/// <param name="nested">Whether the list is the argument of a pseudo-class and ends at <c>)</c>.</param>
		/// <param name="relative">Whether the selectors are relative (the argument of <c>:has()</c>).</param>
		/// <param name="relativeCombinator">Receives the leading combinator of the first relative selector.</param>
		/// <returns>The list.</returns>
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

				selectors.Add(ParseComplex());
				SkipWhitespace();

				if (_pos < _text.Length && _text[_pos] == ',')
				{
					_pos++;
					continue;
				}

				if (_pos >= _text.Length || (nested && _text[_pos] == ')'))
					break;

				throw Error("expected ',' or the end of the selector");
			}

			return new SelectorList(selectors);
		}

		/// <summary>
		/// Parses a complex selector: compound selectors joined by combinators.
		/// </summary>
		/// <returns>The complex selector.</returns>
		private ComplexSelector ParseComplex()
		{
			var compounds = new List<CompoundSelector> { ParseCompound() };
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
					throw Error("unexpected character");
				}

				combinators.Add(combinator);
				compounds.Add(ParseCompound());
			}

			return new ComplexSelector(compounds, combinators);
		}

		/// <summary>
		/// Parses a compound selector.
		/// </summary>
		/// <returns>The compound selector.</returns>
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
				compound.Tag = ReadIdentifier().ToLowerInvariant();
				any = true;
			}

			if (_pos < _text.Length && _text[_pos] == '|')
				throw Error("namespace prefixes are not supported");

			while (_pos < _text.Length)
			{
				var c = _text[_pos];

				if (c == '#')
				{
					_pos++;
					compound.Conditions.Add(new IdSelector(ReadIdentifier()));
				}
				else if (c == '.')
				{
					_pos++;
					compound.Conditions.Add(new ClassSelector(ReadIdentifier()));
				}
				else if (c == '[')
				{
					_pos++;
					compound.Conditions.Add(ParseAttribute());
				}
				else if (c == ':')
				{
					_pos++;
					compound.Conditions.Add(ParsePseudoClass());
				}
				else
				{
					break;
				}

				any = true;
			}

			if (!any)
				throw Error("expected a selector");

			return compound;
		}

		/// <summary>
		/// Parses an attribute selector after <c>[</c>.
		/// </summary>
		/// <returns>The attribute selector.</returns>
		private SimpleSelector ParseAttribute()
		{
			SkipWhitespace();
			var name = ReadIdentifier().ToLowerInvariant();
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
				throw Error("invalid attribute selector");
			}

			SkipWhitespace();
			var value = Peek() == '"' || Peek() == '\'' ? ReadString() : ReadIdentifier();
			SkipWhitespace();

			var ignoreCase = false;
			if (Peek() == 'i' || Peek() == 'I' || Peek() == 's' || Peek() == 'S')
			{
				ignoreCase = Peek() == 'i' || Peek() == 'I';
				_pos++;
				SkipWhitespace();
			}

			if (Peek() != ']')
				throw Error("expected ']'");

			_pos++;
			return new AttributeSelector(name, op, value, ignoreCase);
		}

		/// <summary>
		/// Parses a pseudo-class after <c>:</c>.
		/// </summary>
		/// <returns>The pseudo-class selector.</returns>
		private SimpleSelector ParsePseudoClass()
		{
			if (Peek() == ':')
				throw Error("pseudo-elements are not supported");

			var name = ReadIdentifier().ToLowerInvariant();

			switch (name)
			{
				// legacy pseudo-elements written with a single colon.
				case "before":
				case "after":
				case "first-line":
				case "first-letter":
					throw Error("pseudo-elements are not supported");
			}

			if (Peek() != '(')
			{
				if (!PseudoClassSelector.IsSupported(name) || name.StartsWith("nth-", StringComparison.Ordinal))
					throw Error("unsupported pseudo-class :" + name);

				return new PseudoClassSelector(name);
			}

			_pos++;
			SimpleSelector result;

			// nested pseudo-classes recurse when parsed and matched: bound them to protect the stack.
			if (++_nesting > MaxNesting)
				throw Error("selector nested too deeply");

			switch (name)
			{
				case "not":
				case "is":
				case "where":
				case "matches":
				case "any":
					result = new LogicalSelector(name == "matches" || name == "any" ? "is" : name, ParseList(nested: true, relative: false, out _));
					break;

				case "has":
					var list = ParseList(nested: true, relative: true, out var combinator);
					result = new LogicalSelector(name, list, combinator);
					break;

				case "nth-child":
				case "nth-last-child":
				case "nth-of-type":
				case "nth-last-of-type":
					ParseNth(out var a, out var b);
					result = new PseudoClassSelector(name, a, b);
					break;

				default:
					throw Error("unsupported pseudo-class :" + name + "()");
			}

			SkipWhitespace();
			if (Peek() != ')')
				throw Error("expected ')'");

			_pos++;
			_nesting--;
			return result;
		}

		/// <summary>
		/// Parses an <c>an+b</c> argument, including <c>odd</c> and <c>even</c>.
		/// </summary>
		/// <param name="a">Receives the step.</param>
		/// <param name="b">Receives the offset.</param>
		private void ParseNth(out int a, out int b)
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
				return;
			}

			if (expression == "even")
			{
				a = 2;
				b = 0;
				return;
			}

			var n = expression.IndexOf('n');
			if (n < 0)
			{
				a = 0;
				b = ParseInteger(expression);
				return;
			}

			var step = expression.Substring(0, n);
			a = step == "" || step == "+" ? 1 : step == "-" ? -1 : ParseInteger(step);

			var offset = expression.Substring(n + 1);
			b = offset.Length == 0 ? 0 : ParseInteger(offset);
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
		/// <returns>The identifier.</returns>
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
				throw Error("expected an identifier");

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
		/// <returns>The value.</returns>
		private int ParseInteger(string text)
		{
			if (!Int32.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
				throw Error("invalid an+b expression");

			return value;
		}

		/// <summary>
		/// Creates the exception thrown for a syntax error.
		/// </summary>
		/// <param name="message">The description of the error.</param>
		/// <returns>The exception.</returns>
		private FormatException Error(string message)
		{
			return new FormatException($"Invalid selector \"{_text}\" at position {_pos}: {message}.");
		}

		#endregion
	}
}

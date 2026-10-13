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

namespace Wisej.AI.Html.Parsing
{
	/// <summary>
	/// Decodes named and numeric character references (HTML standard, 13.2.5.72 to 13.2.5.80).
	/// </summary>
	internal static partial class CharacterReferences
	{
		/// <summary>
		/// The length of the longest name in the table, including the semicolon.
		/// </summary>
		private const int MaxNameLength = 32;

		/// <summary>
		/// The named references, by name, built lazily from <see cref="Table"/>.
		/// </summary>
		private static readonly Lazy<Dictionary<string, string>> Named = new Lazy<Dictionary<string, string>>(() =>
		{
			var map = new Dictionary<string, string>(Table.Length / 2, StringComparer.Ordinal);
			for (var i = 0; i < Table.Length; i += 2)
				map[Table[i]] = Table[i + 1];

			return map;
		});

		/// <summary>
		/// The replacements of the numeric references to the C1 control range, which legacy content
		/// uses for windows-1252 characters (13.2.5.80).
		/// </summary>
		private static readonly int[] C1Replacements =
		{
			0x20AC, 0x81, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021, 0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0x8D, 0x017D, 0x8F,
			0x90, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014, 0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0x9D, 0x017E, 0x0178,
		};

		/// <summary>
		/// Finds the longest named character reference at a position of the input.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <param name="start">The position after the ampersand.</param>
		/// <param name="length">Receives the number of characters of the matched name, including the semicolon when present.</param>
		/// <returns>The replacement characters, or <c>null</c> when no reference matches.</returns>
		public static string MatchNamed(string input, int start, out int length)
		{
			length = 0;

			var end = start;
			while (end < input.Length && end - start < MaxNameLength && IsAsciiAlphanumeric(input[end]))
				end++;

			if (end < input.Length && end - start < MaxNameLength && input[end] == ';')
				end++;

			// the table contains every legacy prefix ("amp", "not"), so the longest match wins.
			var map = Named.Value;
			for (var candidate = end - start; candidate > 0; candidate--)
			{
				if (map.TryGetValue(input.Substring(start, candidate), out var value))
				{
					length = candidate;
					return value;
				}
			}

			return null;
		}

		/// <summary>
		/// Returns the replacement of a numeric character reference.
		/// </summary>
		/// <param name="code">The code point, saturated above 0x10FFFF.</param>
		/// <returns>The replacement characters.</returns>
		public static string Numeric(long code)
		{
			if (code == 0 || code > 0x10FFFF || (code >= 0xD800 && code <= 0xDFFF))
				return "�";

			if (code >= 0x80 && code <= 0x9F)
				return Char.ConvertFromUtf32(C1Replacements[code - 0x80]);

			return Char.ConvertFromUtf32((int)code);
		}

		/// <summary>
		/// Decodes the character references of a text, as the tokenizer would in an attribute value.
		/// </summary>
		/// <param name="text">The text. Can be <c>null</c>.</param>
		/// <returns>The decoded text.</returns>
		public static string Decode(string text)
		{
			if (String.IsNullOrEmpty(text) || text.IndexOf('&') < 0)
				return text;

			var sb = new StringBuilder(text.Length);
			var i = 0;

			while (i < text.Length)
			{
				var c = text[i++];
				if (c != '&')
				{
					sb.Append(c);
					continue;
				}

				var value = Consume(text, ref i, inAttribute: true);
				sb.Append(value ?? "&");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Consumes a character reference after an ampersand.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <param name="position">The position after the ampersand; advanced past the reference when one is decoded.</param>
		/// <param name="inAttribute">Whether the reference is in an attribute value, where legacy references followed by <c>=</c> or an alphanumeric are not decoded.</param>
		/// <returns>The replacement characters, or <c>null</c> when the ampersand is literal.</returns>
		public static string Consume(string input, ref int position, bool inAttribute)
		{
			if (position >= input.Length)
				return null;

			var c = input[position];

			if (IsAsciiAlphanumeric(c))
			{
				var value = MatchNamed(input, position, out var length);
				if (value == null)
					return null;

				// historical compatibility: "&copy=1" in a URL is not a reference.
				if (inAttribute && input[position + length - 1] != ';')
				{
					var next = position + length < input.Length ? input[position + length] : '\0';
					if (next == '=' || IsAsciiAlphanumeric(next))
						return null;
				}

				position += length;
				return value;
			}

			if (c != '#')
				return null;

			var i = position + 1;
			var hex = i < input.Length && (input[i] == 'x' || input[i] == 'X');
			if (hex)
				i++;

			long code = 0;
			var digits = 0;

			while (i < input.Length)
			{
				var digit = hex ? HexValue(input[i]) : DecimalValue(input[i]);
				if (digit < 0)
					break;

				// saturate: any value above the Unicode range is replaced the same way.
				if (code <= 0x10FFFF)
					code = code * (hex ? 16 : 10) + digit;

				digits++;
				i++;
			}

			// "&#" and "&#x" without digits are literal text.
			if (digits == 0)
				return null;

			if (i < input.Length && input[i] == ';')
				i++;

			position = i;
			return Numeric(code);
		}

		/// <summary>
		/// Determines whether a character is an ASCII letter or digit.
		/// </summary>
		/// <param name="c">The character.</param>
		/// <returns><c>true</c> for <c>[A-Za-z0-9]</c>.</returns>
		public static bool IsAsciiAlphanumeric(int c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
		}

		/// <summary>
		/// Returns the value of a hexadecimal digit.
		/// </summary>
		/// <param name="c">The character.</param>
		/// <returns>The value, or -1 when the character is not a hexadecimal digit.</returns>
		private static int HexValue(char c)
		{
			if (c >= '0' && c <= '9')
				return c - '0';
			if (c >= 'a' && c <= 'f')
				return c - 'a' + 10;
			if (c >= 'A' && c <= 'F')
				return c - 'A' + 10;

			return -1;
		}

		/// <summary>
		/// Returns the value of a decimal digit.
		/// </summary>
		/// <param name="c">The character.</param>
		/// <returns>The value, or -1 when the character is not a decimal digit.</returns>
		private static int DecimalValue(char c)
		{
			return c >= '0' && c <= '9' ? c - '0' : -1;
		}
	}
}

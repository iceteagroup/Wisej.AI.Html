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
	/// Determines the character encoding of an HTML document from its bytes (HTML standard, 13.2.3).
	/// </summary>
	/// <remarks>
	/// The order follows the standard: byte order mark, user override, transport layer, a prescan of the
	/// first 1024 bytes for <c>&lt;meta charset&gt;</c> or <c>&lt;meta http-equiv="Content-Type"&gt;</c>,
	/// an XML declaration, and finally the content itself: UTF-8 when the bytes are valid UTF-8, otherwise
	/// windows-1252. Labels are resolved with the WHATWG Encoding standard table, so <c>iso-8859-1</c> and
	/// <c>us-ascii</c> decode as windows-1252 exactly as browsers do.
	/// </remarks>
	internal static class EncodingSniffer
	{
		/// <summary>The number of bytes examined by the prescan.</summary>
		private const int PrescanLength = 1024;

		/// <summary>UTF-8 without byte order mark emission, replacing invalid sequences with U+FFFD.</summary>
		private static readonly Encoding Utf8 = new UTF8Encoding(false, false);

		/// <summary>
		/// The encoding labels of the WHATWG Encoding standard, mapped to Windows code pages.
		/// </summary>
		private static readonly Dictionary<string, int> Labels = CreateLabels();

		#region Detection

		/// <summary>
		/// Determines the encoding of a document.
		/// </summary>
		/// <param name="data">The bytes of the document.</param>
		/// <param name="overrideEncoding">An encoding chosen by the caller, or <c>null</c>.</param>
		/// <param name="transportLabel">The charset declared by the transport layer, or <c>null</c>.</param>
		/// <param name="bomLength">Receives the length of the byte order mark to skip.</param>
		/// <returns>The encoding to decode the document with.</returns>
		public static Encoding Detect(byte[] data, Encoding overrideEncoding, string transportLabel, out int bomLength)
		{
			var bom = DetectBom(data, out bomLength);
			if (bom != null)
				return bom;

			if (overrideEncoding != null)
				return overrideEncoding;

			var transport = GetEncoding(transportLabel);
			if (transport != null)
				return transport;

			var declared = GetEncoding(PrescanMeta(data)) ?? GetEncoding(XmlDeclaration(data));
			if (declared != null)
			{
				// a document that declares UTF-16 in ASCII-compatible bytes cannot be UTF-16.
				return declared.CodePage == 1200 || declared.CodePage == 1201 ? Utf8 : declared;
			}

			return IsValidUtf8(data) ? Utf8 : Windows1252Encoding.Instance;
		}

		/// <summary>
		/// Returns the encoding for a label of the WHATWG Encoding standard.
		/// </summary>
		/// <param name="label">The label, such as <c>utf-8</c>, <c>latin1</c> or <c>shift_jis</c>. Can be <c>null</c>.</param>
		/// <returns>The encoding, or <c>null</c> when the label is unknown or the encoding is not available.</returns>
		public static Encoding GetEncoding(string label)
		{
			if (String.IsNullOrWhiteSpace(label))
				return null;

			if (!Labels.TryGetValue(label.Trim().ToLowerInvariant(), out var codePage))
				return null;

			switch (codePage)
			{
				case 65001:
					return Utf8;

				case 1252:
					return Windows1252Encoding.Instance;

				case 1200:
					return new UnicodeEncoding(false, false);

				case 1201:
					return new UnicodeEncoding(true, false);
			}

			try
			{
#if NET
				return CodePagesEncodingProvider.Instance.GetEncoding(codePage) ?? Encoding.GetEncoding(codePage);
#else
				return Encoding.GetEncoding(codePage);
#endif
			}
			catch (ArgumentException)
			{
				return null;
			}
			catch (NotSupportedException)
			{
				return null;
			}
		}

		/// <summary>
		/// Detects a byte order mark.
		/// </summary>
		/// <param name="data">The bytes.</param>
		/// <param name="length">Receives the length of the byte order mark.</param>
		/// <returns>The encoding selected by the byte order mark, or <c>null</c>.</returns>
		private static Encoding DetectBom(byte[] data, out int length)
		{
			if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
			{
				length = 3;
				return Utf8;
			}

			if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
			{
				length = 2;
				return new UnicodeEncoding(true, false);
			}

			if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
			{
				length = 2;
				return new UnicodeEncoding(false, false);
			}

			length = 0;
			return null;
		}

		/// <summary>
		/// Determines whether bytes are valid UTF-8 (ASCII included). A truncated sequence at the end of a
		/// long document is accepted, since the bytes may have been cut.
		/// </summary>
		/// <param name="data">The bytes.</param>
		/// <returns><c>true</c> when the bytes decode as UTF-8 without errors.</returns>
		internal static bool IsValidUtf8(byte[] data)
		{
			var i = 0;
			while (i < data.Length)
			{
				var b = data[i];
				if (b < 0x80)
				{
					i++;
					continue;
				}

				int length;
				int min;
				if (b >= 0xC2 && b <= 0xDF)
				{
					length = 2;
					min = 0x80;
				}
				else if (b >= 0xE0 && b <= 0xEF)
				{
					length = 3;
					min = 0x800;
				}
				else if (b >= 0xF0 && b <= 0xF4)
				{
					length = 4;
					min = 0x10000;
				}
				else
				{
					return false;
				}

				if (i + length > data.Length)
					return true;

				var code = b & (0xFF >> (length + 1));
				for (var k = 1; k < length; k++)
				{
					var next = data[i + k];
					if ((next & 0xC0) != 0x80)
						return false;

					code = (code << 6) | (next & 0x3F);
				}

				if (code < min || code > 0x10FFFF || (code >= 0xD800 && code <= 0xDFFF))
					return false;

				i += length;
			}

			return true;
		}

		#endregion

		#region Prescan

		/// <summary>
		/// Prescans the first bytes of a document for an encoding declaration (13.2.3.2).
		/// </summary>
		/// <param name="data">The bytes.</param>
		/// <returns>The declared label, or <c>null</c>.</returns>
		internal static string PrescanMeta(byte[] data)
		{
			var end = Math.Min(data.Length, PrescanLength);
			var i = 0;

			while (i < end)
			{
				if (Matches(data, i, end, "<!--"))
				{
					// skip the comment; "<!-->" is complete.
					var close = IndexOf(data, i + 2, end, "-->");
					if (close < 0)
						return null;

					i = close + 3;
					continue;
				}

				if (Matches(data, i, end, "<meta") && i + 5 < end && (IsSpace(data[i + 5]) || data[i + 5] == '/'))
				{
					i += 6;

					var label = ReadMeta(data, ref i, end);
					if (label != null)
						return label;

					continue;
				}

				if (data[i] == '<' && i + 1 < end && (IsAsciiLetter(data[i + 1]) || (data[i + 1] == '/' && i + 2 < end && IsAsciiLetter(data[i + 2]))))
				{
					// skip a tag with its attributes, which may contain '>'.
					i++;
					while (i < end && !IsSpace(data[i]) && data[i] != '>')
						i++;

					while (ReadAttribute(data, ref i, end, out _, out _))
					{
					}

					continue;
				}

				if (data[i] == '<' && i + 1 < end && (data[i + 1] == '!' || data[i + 1] == '/' || data[i + 1] == '?'))
				{
					var close = Array.IndexOf(data, (byte)'>', i + 1, end - i - 1);
					if (close < 0)
						return null;

					i = close + 1;
					continue;
				}

				i++;
			}

			return null;
		}

		/// <summary>
		/// Reads the attributes of a <c>&lt;meta&gt;</c> tag and returns the declared encoding, if any.
		/// </summary>
		/// <param name="data">The bytes.</param>
		/// <param name="i">The position after <c>&lt;meta</c>; advanced past the tag.</param>
		/// <param name="end">The end of the prescan.</param>
		/// <returns>The declared label, or <c>null</c>.</returns>
		private static string ReadMeta(byte[] data, ref int i, int end)
		{
			var seen = new HashSet<string>(StringComparer.Ordinal);
			var gotPragma = false;
			bool? needPragma = null;
			string charset = null;

			while (ReadAttribute(data, ref i, end, out var name, out var value))
			{
				if (!seen.Add(name))
					continue;

				switch (name)
				{
					case "http-equiv":
						if (value == "content-type")
							gotPragma = true;
						break;

					case "content":
						if (charset == null)
						{
							var extracted = ExtractCharsetFromContent(value);
							if (extracted != null)
							{
								charset = extracted;
								needPragma = true;
							}
						}
						break;

					case "charset":
						charset = value;
						needPragma = false;
						break;
				}
			}

			if (needPragma == null || (needPragma == true && !gotPragma))
				return null;

			return charset;
		}

		/// <summary>
		/// Reads one attribute in the prescan ("get an attribute", 13.2.3.2).
		/// </summary>
		/// <param name="data">The bytes.</param>
		/// <param name="i">The position; advanced past the attribute.</param>
		/// <param name="end">The end of the prescan.</param>
		/// <param name="name">Receives the lowercase name.</param>
		/// <param name="value">Receives the lowercase value.</param>
		/// <returns><c>false</c> at the end of the tag.</returns>
		private static bool ReadAttribute(byte[] data, ref int i, int end, out string name, out string value)
		{
			name = null;
			value = null;

			while (i < end && (IsSpace(data[i]) || data[i] == '/'))
				i++;

			if (i >= end || data[i] == '>')
			{
				i++;
				return false;
			}

			var nameBuilder = new StringBuilder();
			var valueBuilder = new StringBuilder();

			// the name.
			while (i < end)
			{
				var b = data[i];
				if (b == '=' && nameBuilder.Length > 0)
				{
					i++;
					goto Value;
				}

				if (IsSpace(b))
					break;

				if (b == '/' || b == '>')
				{
					name = nameBuilder.ToString();
					value = "";
					return true;
				}

				nameBuilder.Append(ToLower(b));
				i++;
			}

			while (i < end && IsSpace(data[i]))
				i++;

			if (i >= end || data[i] != '=')
			{
				name = nameBuilder.ToString();
				value = "";
				return true;
			}

			i++;

		Value:
			while (i < end && IsSpace(data[i]))
				i++;

			if (i < end && (data[i] == '"' || data[i] == '\''))
			{
				var quote = data[i++];
				while (i < end && data[i] != quote)
					valueBuilder.Append(ToLower(data[i++]));

				i++;
			}
			else
			{
				while (i < end && !IsSpace(data[i]) && data[i] != '>')
					valueBuilder.Append(ToLower(data[i++]));
			}

			name = nameBuilder.ToString();
			value = valueBuilder.ToString();
			return true;
		}

		/// <summary>
		/// Extracts the charset from the value of a <c>content</c> attribute, such as <c>text/html; charset=utf-8</c>.
		/// </summary>
		/// <param name="content">The lowercase attribute value.</param>
		/// <returns>The label, or <c>null</c>.</returns>
		internal static string ExtractCharsetFromContent(string content)
		{
			var position = 0;
			while (true)
			{
				var index = content.IndexOf("charset", position, StringComparison.Ordinal);
				if (index < 0)
					return null;

				var i = index + 7;
				while (i < content.Length && content[i] <= ' ')
					i++;

				if (i >= content.Length || content[i] != '=')
				{
					position = index + 7;
					continue;
				}

				i++;
				while (i < content.Length && content[i] <= ' ')
					i++;

				if (i >= content.Length)
					return null;

				if (content[i] == '"' || content[i] == '\'')
				{
					var close = content.IndexOf(content[i], i + 1);
					return close < 0 ? null : content.Substring(i + 1, close - i - 1);
				}

				var start = i;
				while (i < content.Length && content[i] > ' ' && content[i] != ';')
					i++;

				return content.Substring(start, i - start);
			}
		}

		/// <summary>
		/// Reads the encoding of an XML declaration at the start of the document, as used by XHTML files.
		/// </summary>
		/// <param name="data">The bytes.</param>
		/// <returns>The label, or <c>null</c>.</returns>
		private static string XmlDeclaration(byte[] data)
		{
			if (!Matches(data, 0, data.Length, "<?xml"))
				return null;

			var end = Array.IndexOf(data, (byte)'>', 0, Math.Min(data.Length, PrescanLength));
			if (end < 0)
				return null;

			var declaration = Encoding.ASCII.GetString(data, 0, end).ToLowerInvariant();
			var index = declaration.IndexOf("encoding", StringComparison.Ordinal);
			if (index < 0)
				return null;

			var i = declaration.IndexOf('=', index);
			if (i < 0)
				return null;

			i++;
			while (i < declaration.Length && declaration[i] == ' ')
				i++;

			if (i >= declaration.Length || (declaration[i] != '"' && declaration[i] != '\''))
				return null;

			var close = declaration.IndexOf(declaration[i], i + 1);
			return close < 0 ? null : declaration.Substring(i + 1, close - i - 1);
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Determines whether the bytes at a position match an ASCII string, ignoring case.
		/// </summary>
		/// <param name="data">The bytes.</param>
		/// <param name="index">The position.</param>
		/// <param name="end">The end of the range.</param>
		/// <param name="value">The lowercase string.</param>
		/// <returns><c>true</c> on a match.</returns>
		private static bool Matches(byte[] data, int index, int end, string value)
		{
			if (index + value.Length > end)
				return false;

			for (var k = 0; k < value.Length; k++)
			{
				if (ToLower(data[index + k]) != value[k])
					return false;
			}

			return true;
		}

		/// <summary>
		/// Finds an ASCII string in a range of bytes.
		/// </summary>
		/// <param name="data">The bytes.</param>
		/// <param name="start">The start of the range.</param>
		/// <param name="end">The end of the range.</param>
		/// <param name="value">The string.</param>
		/// <returns>The position, or -1.</returns>
		private static int IndexOf(byte[] data, int start, int end, string value)
		{
			for (var i = start; i + value.Length <= end; i++)
			{
				if (Matches(data, i, end, value))
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Returns the ASCII lowercase of a byte, as a character.
		/// </summary>
		/// <param name="b">The byte.</param>
		/// <returns>The character.</returns>
		private static char ToLower(byte b)
		{
			return b >= 'A' && b <= 'Z' ? (char)(b + 0x20) : (char)b;
		}

		/// <summary>
		/// Determines whether a byte is white space for the prescan (tab, LF, FF, CR, space).
		/// </summary>
		/// <param name="b">The byte.</param>
		/// <returns><c>true</c> for white space.</returns>
		private static bool IsSpace(byte b)
		{
			return b == 0x09 || b == 0x0A || b == 0x0C || b == 0x0D || b == 0x20;
		}

		/// <summary>
		/// Determines whether a byte is an ASCII letter.
		/// </summary>
		/// <param name="b">The byte.</param>
		/// <returns><c>true</c> for <c>[A-Za-z]</c>.</returns>
		private static bool IsAsciiLetter(byte b)
		{
			return (b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z');
		}

		/// <summary>
		/// Builds the label table of the WHATWG Encoding standard.
		/// </summary>
		/// <returns>The labels mapped to Windows code pages.</returns>
		private static Dictionary<string, int> CreateLabels()
		{
			var map = new Dictionary<string, int>(StringComparer.Ordinal);

			void Add(int codePage, params string[] labels)
			{
				foreach (var label in labels)
					map[label] = codePage;
			}

			Add(65001, "unicode-1-1-utf-8", "unicode11utf8", "unicode20utf8", "utf-8", "utf8", "x-unicode20utf8");
			Add(1252, "ansi_x3.4-1968", "ascii", "cp1252", "cp819", "csisolatin1", "ibm819", "iso-8859-1", "iso-ir-100",
				"iso8859-1", "iso88591", "iso_8859-1", "iso_8859-1:1987", "l1", "latin1", "us-ascii", "windows-1252",
				"x-cp1252", "x-user-defined");
			Add(28592, "csisolatin2", "iso-8859-2", "iso-ir-101", "iso8859-2", "iso88592", "iso_8859-2", "iso_8859-2:1987", "l2", "latin2");
			Add(28593, "csisolatin3", "iso-8859-3", "iso-ir-109", "iso8859-3", "iso88593", "iso_8859-3", "iso_8859-3:1988", "l3", "latin3");
			Add(28594, "csisolatin4", "iso-8859-4", "iso-ir-110", "iso8859-4", "iso88594", "iso_8859-4", "iso_8859-4:1988", "l4", "latin4");
			Add(28595, "csisolatincyrillic", "cyrillic", "iso-8859-5", "iso-ir-144", "iso8859-5", "iso88595", "iso_8859-5", "iso_8859-5:1988");
			Add(28596, "arabic", "asmo-708", "csiso88596e", "csiso88596i", "csisolatinarabic", "ecma-114", "iso-8859-6",
				"iso-8859-6-e", "iso-8859-6-i", "iso-ir-127", "iso8859-6", "iso88596", "iso_8859-6", "iso_8859-6:1987");
			Add(28597, "csisolatingreek", "ecma-118", "elot_928", "greek", "greek8", "iso-8859-7", "iso-ir-126", "iso8859-7",
				"iso88597", "iso_8859-7", "iso_8859-7:1987", "sun_eu_greek");
			Add(28598, "csiso88598e", "csisolatinhebrew", "hebrew", "iso-8859-8", "iso-8859-8-e", "iso-ir-138", "iso8859-8",
				"iso88598", "iso_8859-8", "iso_8859-8:1988", "visual");
			Add(38598, "csiso88598i", "iso-8859-8-i", "logical");
			Add(28603, "iso-8859-13", "iso8859-13", "iso885913");
			Add(28605, "csisolatin9", "iso-8859-15", "iso8859-15", "iso885915", "iso_8859-15", "l9");
			Add(874, "dos-874", "iso-8859-11", "iso8859-11", "iso885911", "tis-620", "windows-874");
			Add(1250, "cp1250", "windows-1250", "x-cp1250");
			Add(1251, "cp1251", "windows-1251", "x-cp1251");
			Add(1253, "cp1253", "windows-1253", "x-cp1253");
			Add(1254, "cp1254", "csisolatin5", "iso-8859-9", "iso-ir-148", "iso8859-9", "iso88599", "iso_8859-9",
				"iso_8859-9:1989", "l5", "latin5", "windows-1254", "x-cp1254");
			Add(1255, "cp1255", "windows-1255", "x-cp1255");
			Add(1256, "cp1256", "windows-1256", "x-cp1256");
			Add(1257, "cp1257", "windows-1257", "x-cp1257");
			Add(1258, "cp1258", "windows-1258", "x-cp1258");
			Add(20866, "cskoi8r", "koi", "koi8", "koi8-r", "koi8_r");
			Add(21866, "koi8-ru", "koi8-u");
			Add(10000, "csmacintosh", "mac", "macintosh", "x-mac-roman");
			Add(866, "866", "cp866", "csibm866", "ibm866");
			Add(936, "chinese", "csgb2312", "csiso58gb231280", "gb2312", "gb_2312", "gb_2312-80", "gbk", "iso-ir-58", "x-gbk");
			Add(54936, "gb18030");
			Add(950, "big5", "big5-hkscs", "cn-big5", "csbig5", "x-x-big5");
			Add(51932, "cseucpkdfmtjapanese", "euc-jp", "x-euc-jp");
			Add(50220, "csiso2022jp", "iso-2022-jp");
			Add(932, "csshiftjis", "ms932", "ms_kanji", "shift-jis", "shift_jis", "sjis", "windows-31j", "x-sjis");
			Add(949, "cseuckr", "csksc56011987", "euc-kr", "iso-ir-149", "korean", "ks_c_5601-1987", "ks_c_5601-1989",
				"ksc5601", "ksc_5601", "windows-949");
			Add(1201, "unicodefffe", "utf-16be");
			Add(1200, "csunicode", "iso-10646-ucs-2", "ucs-2", "unicode", "unicodefeff", "utf-16", "utf-16le");

			return map;
		}

		#endregion
	}

	/// <summary>
	/// The windows-1252 encoding, implemented without code page support so that the default fallback of
	/// the HTML standard works on every platform (including mobile runtimes without code pages).
	/// </summary>
	internal sealed class Windows1252Encoding : Encoding
	{
		/// <summary>The shared instance.</summary>
		public static readonly Windows1252Encoding Instance = new Windows1252Encoding();

		/// <summary>
		/// The characters of the bytes 0x80 to 0x9F; the other bytes map to the same code point.
		/// </summary>
		private static readonly char[] HighTable =
		{
			'€', '\u0081', '‚', 'ƒ', '„', '…', '†', '‡', 'ˆ', '‰', 'Š', '‹', 'Œ', '\u008D', 'Ž', '\u008F',
			'\u0090', '‘', '’', '“', '”', '•', '–', '—', '˜', '™', 'š', '›', 'œ', '\u009D', 'ž', 'Ÿ',
		};

		/// <summary>
		/// Initializes a new instance of the <see cref="Windows1252Encoding"/> class.
		/// </summary>
		private Windows1252Encoding()
			: base(1252)
		{
		}

		/// <inheritdoc/>
		public override string WebName => "windows-1252";

		/// <inheritdoc/>
		public override string EncodingName => "Western European (Windows)";

		/// <inheritdoc/>
		public override int GetByteCount(char[] chars, int index, int count)
		{
			return count;
		}

		/// <inheritdoc/>
		public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
		{
			for (var i = 0; i < charCount; i++)
			{
				var c = chars[charIndex + i];
				byte b;

				if (c < 0x80 || (c >= 0xA0 && c <= 0xFF))
				{
					b = (byte)c;
				}
				else
				{
					var index = Array.IndexOf(HighTable, c);
					b = index >= 0 ? (byte)(0x80 + index) : (byte)'?';
				}

				bytes[byteIndex + i] = b;
			}

			return charCount;
		}

		/// <inheritdoc/>
		public override int GetCharCount(byte[] bytes, int index, int count)
		{
			return count;
		}

		/// <inheritdoc/>
		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
		{
			for (var i = 0; i < byteCount; i++)
			{
				var b = bytes[byteIndex + i];
				chars[charIndex + i] = b >= 0x80 && b <= 0x9F ? HighTable[b - 0x80] : (char)b;
			}

			return byteCount;
		}

		/// <inheritdoc/>
		public override int GetMaxByteCount(int charCount)
		{
			return charCount;
		}

		/// <inheritdoc/>
		public override int GetMaxCharCount(int byteCount)
		{
			return byteCount;
		}
	}
}

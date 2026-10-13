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
using System.IO;
using System.Text;
using Wisej.AI.Html.Content;

namespace Wisej.AI.Html.Mhtml
{
	/// <summary>
	/// A resource stored in an MHTML archive.
	/// </summary>
	internal sealed class MhtmlPart
	{
		/// <summary>Gets or sets the lowercase MIME type, such as <c>image/png</c>.</summary>
		public string ContentType { get; set; }

		/// <summary>Gets or sets the charset parameter of the content type, or <c>null</c>.</summary>
		public string Charset { get; set; }

		/// <summary>Gets or sets the <c>Content-Location</c>, or <c>null</c>.</summary>
		public string Location { get; set; }

		/// <summary>Gets or sets the <c>Content-ID</c> without angle brackets, or <c>null</c>.</summary>
		public string ContentId { get; set; }

		/// <summary>Gets or sets the decoded bytes.</summary>
		public byte[] Data { get; set; }
	}

	/// <summary>
	/// Reads MHTML web archives (RFC 2557): the <c>.mht</c> and <c>.mhtml</c> files saved by browsers and by
	/// Office ("Single File Web Page"), which store a page and its images and style sheets in one MIME message.
	/// </summary>
	internal sealed class MhtmlArchive
	{
		// the resources by location and by content id.
		private readonly Dictionary<string, MhtmlPart> _byLocation = new Dictionary<string, MhtmlPart>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, MhtmlPart> _byContentId = new Dictionary<string, MhtmlPart>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, List<MhtmlPart>> _byFileName = new Dictionary<string, List<MhtmlPart>>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Initializes a new instance of the <see cref="MhtmlArchive"/> class.
		/// </summary>
		private MhtmlArchive()
		{
		}

		/// <summary>
		/// Gets the root HTML part.
		/// </summary>
		public MhtmlPart Root { get; private set; }

		#region Reading

		/// <summary>
		/// Determines whether bytes look like an MHTML archive: a MIME message that starts with headers.
		/// </summary>
		/// <param name="data">The bytes.</param>
		/// <returns><c>true</c> for a MIME message.</returns>
		public static bool IsArchive(byte[] data)
		{
			var head = Latin1(data, 0, Math.Min(data.Length, 4096)).TrimStart();
			if (head.StartsWith("<", StringComparison.Ordinal))
				return false;

			var headerEnd = head.IndexOf("\n\n", StringComparison.Ordinal);
			var crlfEnd = head.IndexOf("\r\n\r\n", StringComparison.Ordinal);
			if (crlfEnd >= 0 && (headerEnd < 0 || crlfEnd < headerEnd))
				headerEnd = crlfEnd;

			var headers = (headerEnd < 0 ? head : head.Substring(0, headerEnd)).ToLowerInvariant();
			return headers.Contains("mime-version:") || headers.Contains("content-type: multipart/") || headers.Contains("content-type:multipart/");
		}

		/// <summary>
		/// Reads an archive.
		/// </summary>
		/// <param name="data">The bytes of the archive.</param>
		/// <returns>The archive, or <c>null</c> when it contains no HTML part.</returns>
		public static MhtmlArchive Read(byte[] data)
		{
			var text = Latin1(data, 0, data.Length);
			var archive = new MhtmlArchive();

			var bodyStart = ReadHeaders(text, 0, out var headers);
			var contentType = Header(headers, "content-type") ?? "text/html";

			if (!MediaType(contentType).StartsWith("multipart/", StringComparison.Ordinal))
			{
				archive.Root = CreatePart(headers, text.Substring(bodyStart));
				return archive.Root.ContentType.Contains("html") ? archive : null;
			}

			var boundary = Parameter(contentType, "boundary");
			var start = Parameter(contentType, "start")?.Trim('<', '>');
			if (String.IsNullOrEmpty(boundary))
				return null;

			MhtmlPart firstHtml = null;

			foreach (var body in SplitParts(text, bodyStart, boundary))
			{
				var partBody = ReadHeaders(body, 0, out var partHeaders);
				var part = CreatePart(partHeaders, body.Substring(partBody));
				archive.Add(part);

				if (start != null && part.ContentId != null && String.Equals(part.ContentId, start, StringComparison.OrdinalIgnoreCase))
					archive.Root = part;
				else if (firstHtml == null && part.ContentType.Contains("html"))
					firstHtml = part;
			}

			// without a start parameter, or when it matches no part, the first HTML part is the page.
			archive.Root = archive.Root ?? firstHtml;
			return archive.Root == null ? null : archive;
		}

		/// <summary>
		/// Finds the resource referenced by a URL.
		/// </summary>
		/// <param name="url">The URL as written in the document.</param>
		/// <param name="resolved">The URL resolved against the base URL of the document, or <c>null</c>.</param>
		/// <returns>The resource, or <c>null</c>.</returns>
		public MhtmlPart Find(string url, string resolved)
		{
			if (String.IsNullOrEmpty(url))
				return null;

			if (url.StartsWith("cid:", StringComparison.OrdinalIgnoreCase) && _byContentId.TryGetValue(Urls.Unescape(url.Substring(4)), out var byId))
				return byId;

			if (resolved != null && _byLocation.TryGetValue(resolved, out var part))
				return part;

			if (_byLocation.TryGetValue(url, out part))
				return part;

			if (resolved != null && _byLocation.TryGetValue(Urls.Unescape(resolved), out part))
				return part;

			// Office writes absolute file:// locations of a folder that the relative references do not match exactly.
			var name = FileName(url);
			if (name != null && _byFileName.TryGetValue(name, out var candidates) && candidates.Count == 1)
				return candidates[0];

			return null;
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Indexes a part.
		/// </summary>
		/// <param name="part">The part.</param>
		private void Add(MhtmlPart part)
		{
			if (part.Location != null)
			{
				_byLocation[part.Location] = part;

				_byLocation[Urls.Unescape(part.Location)] = part;

				var name = FileName(part.Location);
				if (name != null)
				{
					if (!_byFileName.TryGetValue(name, out var list))
						_byFileName[name] = list = new List<MhtmlPart>();

					list.Add(part);
				}
			}

			if (part.ContentId != null)
				_byContentId[part.ContentId] = part;
		}

		/// <summary>
		/// Creates a part from its headers and encoded body.
		/// </summary>
		/// <param name="headers">The headers.</param>
		/// <param name="body">The encoded body, as Latin-1 characters.</param>
		/// <returns>The part.</returns>
		private static MhtmlPart CreatePart(Dictionary<string, string> headers, string body)
		{
			var contentType = Header(headers, "content-type") ?? "text/plain";
			var encoding = (Header(headers, "content-transfer-encoding") ?? "7bit").Trim().ToLowerInvariant();

			byte[] data;
			switch (encoding)
			{
				case "base64":
					data = DecodeBase64(body);
					break;

				case "quoted-printable":
					data = DecodeQuotedPrintable(body);
					break;

				default:
					data = FromLatin1(body);
					break;
			}

			return new MhtmlPart
			{
				ContentType = MediaType(contentType),
				Charset = Parameter(contentType, "charset"),
				Location = Header(headers, "content-location")?.Trim(),
				ContentId = Header(headers, "content-id")?.Trim().Trim('<', '>'),
				Data = data,
			};
		}

		/// <summary>
		/// Reads the headers of a MIME entity, unfolding continuation lines.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="start">The position of the first header.</param>
		/// <param name="headers">Receives the headers, by lowercase name.</param>
		/// <returns>The position of the body.</returns>
		private static int ReadHeaders(string text, int start, out Dictionary<string, string> headers)
		{
			headers = new Dictionary<string, string>(StringComparer.Ordinal);
			var position = start;
			string lastName = null;

			while (position < text.Length)
			{
				var end = text.IndexOf('\n', position);
				if (end < 0)
					end = text.Length;

				var line = text.Substring(position, end - position).TrimEnd('\r');
				position = end + 1;

				if (line.Length == 0)
				{
					// a blank line before any header is skipped.
					if (headers.Count == 0)
						continue;

					break;
				}

				if ((line[0] == ' ' || line[0] == '\t') && lastName != null)
				{
					headers[lastName] += " " + line.Trim();
					continue;
				}

				var colon = line.IndexOf(':');
				if (colon <= 0)
					continue;

				lastName = line.Substring(0, colon).Trim().ToLowerInvariant();
				if (!headers.ContainsKey(lastName))
					headers[lastName] = line.Substring(colon + 1).Trim();
			}

			return Math.Min(position, text.Length);
		}

		/// <summary>
		/// Splits a multipart body at its boundaries.
		/// </summary>
		/// <param name="text">The message.</param>
		/// <param name="start">The position of the body.</param>
		/// <param name="boundary">The boundary.</param>
		/// <returns>The parts, with their headers.</returns>
		private static IEnumerable<string> SplitParts(string text, int start, string boundary)
		{
			var delimiter = "--" + boundary;
			var position = text.IndexOf(delimiter, start, StringComparison.Ordinal);

			while (position >= 0)
			{
				var lineEnd = text.IndexOf('\n', position);
				if (lineEnd < 0)
					yield break;

				// the closing delimiter ends the parts.
				if (String.CompareOrdinal(text, position + delimiter.Length, "--", 0, 2) == 0)
					yield break;

				var next = text.IndexOf("\n" + delimiter, lineEnd, StringComparison.Ordinal);
				var end = next < 0 ? text.Length : next;
				if (end > lineEnd && text[end - 1] == '\r')
					end--;

				yield return text.Substring(lineEnd + 1, Math.Max(0, end - lineEnd - 1));

				position = next < 0 ? -1 : next + 1;
			}
		}

		/// <summary>
		/// Returns a header value.
		/// </summary>
		/// <param name="headers">The headers.</param>
		/// <param name="name">The lowercase name.</param>
		/// <returns>The value, or <c>null</c>.</returns>
		private static string Header(Dictionary<string, string> headers, string name)
		{
			return headers.TryGetValue(name, out var value) ? value : null;
		}

		/// <summary>
		/// Returns the lowercase media type of a content type, without parameters.
		/// </summary>
		/// <param name="contentType">The content type.</param>
		/// <returns>The media type.</returns>
		private static string MediaType(string contentType)
		{
			var semicolon = contentType.IndexOf(';');
			return (semicolon < 0 ? contentType : contentType.Substring(0, semicolon)).Trim().ToLowerInvariant();
		}

		/// <summary>
		/// Returns a parameter of a header value, such as the <c>boundary</c> of a content type.
		/// </summary>
		/// <param name="value">The header value.</param>
		/// <param name="name">The lowercase parameter name.</param>
		/// <returns>The parameter value without quotes, or <c>null</c>.</returns>
		internal static string Parameter(string value, string name)
		{
			foreach (var segment in SplitParameters(value))
			{
				var equals = segment.IndexOf('=');
				if (equals <= 0)
					continue;

				if (!segment.Substring(0, equals).Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
					continue;

				return segment.Substring(equals + 1).Trim().Trim('"');
			}

			return null;
		}

		/// <summary>
		/// Splits a header value at the semicolons outside quotes.
		/// </summary>
		/// <param name="value">The header value.</param>
		/// <returns>The segments.</returns>
		private static IEnumerable<string> SplitParameters(string value)
		{
			var start = 0;
			var quoted = false;

			for (var i = 0; i < value.Length; i++)
			{
				if (value[i] == '"')
				{
					quoted = !quoted;
				}
				else if (value[i] == ';' && !quoted)
				{
					yield return value.Substring(start, i - start);
					start = i + 1;
				}
			}

			yield return value.Substring(start);
		}

		/// <summary>
		/// Returns the last path segment of a URL.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns>The file name, or <c>null</c>.</returns>
		private static string FileName(string url)
		{
			var end = url.IndexOfAny(new[] { '?', '#' });
			var path = (end < 0 ? url : url.Substring(0, end)).Replace('\\', '/');
			var slash = path.LastIndexOf('/');
			var name = slash < 0 ? path : path.Substring(slash + 1);

			name = Urls.Unescape(name);
			return name.Length == 0 ? null : name;
		}

		/// <summary>
		/// Decodes base64 text, ignoring white space and invalid characters.
		/// </summary>
		/// <param name="text">The encoded text.</param>
		/// <returns>The bytes.</returns>
		private static byte[] DecodeBase64(string text)
		{
			var sb = new StringBuilder(text.Length);
			foreach (var c in text)
			{
				if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '+' || c == '/')
					sb.Append(c);
			}

			while (sb.Length % 4 != 0)
				sb.Append('=');

			try
			{
				return Convert.FromBase64String(sb.ToString());
			}
			catch (FormatException)
			{
				return Array.Empty<byte>();
			}
		}

		/// <summary>
		/// Decodes quoted-printable text (RFC 2045, 6.7).
		/// </summary>
		/// <param name="text">The encoded text, as Latin-1 characters.</param>
		/// <returns>The bytes.</returns>
		private static byte[] DecodeQuotedPrintable(string text)
		{
			using (var output = new MemoryStream(text.Length))
			{
				for (var i = 0; i < text.Length; i++)
				{
					var c = text[i];
					if (c != '=')
					{
						output.WriteByte((byte)c);
						continue;
					}

					// soft line break.
					if (i + 1 < text.Length && (text[i + 1] == '\n' || text[i + 1] == '\r'))
					{
						i++;
						if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
							i++;

						continue;
					}

					if (i + 2 < text.Length && Uri.IsHexDigit(text[i + 1]) && Uri.IsHexDigit(text[i + 2]))
					{
						output.WriteByte((byte)Convert.ToInt32(text.Substring(i + 1, 2), 16));
						i += 2;
						continue;
					}

					output.WriteByte((byte)'=');
				}

				return output.ToArray();
			}
		}

		/// <summary>
		/// Maps bytes to characters one to one.
		/// </summary>
		/// <param name="data">The bytes.</param>
		/// <param name="offset">The first byte.</param>
		/// <param name="count">The number of bytes.</param>
		/// <returns>The characters.</returns>
		private static string Latin1(byte[] data, int offset, int count)
		{
			var chars = new char[count];
			for (var i = 0; i < count; i++)
				chars[i] = (char)data[offset + i];

			return new string(chars);
		}

		/// <summary>
		/// Maps characters back to bytes one to one.
		/// </summary>
		/// <param name="text">The characters, all below 256.</param>
		/// <returns>The bytes.</returns>
		private static byte[] FromLatin1(string text)
		{
			var bytes = new byte[text.Length];
			for (var i = 0; i < text.Length; i++)
				bytes[i] = (byte)text[i];

			return bytes;
		}

		#endregion
	}
}

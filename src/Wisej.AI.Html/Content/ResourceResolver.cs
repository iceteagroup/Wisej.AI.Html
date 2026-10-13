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
using System.Security.Cryptography;
using System.Text;
using Wisej.AI.Html.Mhtml;
using Wisej.AI.Html.Parsing;

namespace Wisej.AI.Html.Content
{
	/// <summary>
	/// The picture referenced by one or more images: its bytes or location, shared by every placement.
	/// </summary>
	internal sealed class ImageSource
	{
		/// <summary>Gets or sets the key that identifies the picture.</summary>
		public string Key { get; set; }

		/// <summary>Gets or sets the file name.</summary>
		public string FileName { get; set; }

		/// <summary>Gets or sets the absolute URL, or <c>null</c> for embedded data.</summary>
		public string Url { get; set; }

		/// <summary>Gets or sets the MIME type, or <c>null</c>.</summary>
		public string ContentType { get; set; }

		/// <summary>Gets or sets the bytes of an embedded picture, or <c>null</c>.</summary>
		public byte[] Data { get; set; }
	}

	/// <summary>
	/// Resolves the URLs of a document and loads its embedded resources: <c>data:</c> URIs, inline SVG and the
	/// parts of MHTML archives. Pictures are de-duplicated and receive stable file names.
	/// </summary>
	internal sealed class ResourceResolver
	{
		// the archive of an MHTML document, or null.
		private readonly MhtmlArchive _archive;

		// the pictures already resolved, by key.
		private readonly Dictionary<string, ImageSource> _images = new Dictionary<string, ImageSource>(StringComparer.Ordinal);

		// the number of embedded pictures, used to name them.
		private int _embeddedCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="ResourceResolver"/> class.
		/// </summary>
		/// <param name="baseUri">The base URL of the document, or <c>null</c>.</param>
		/// <param name="archive">The MHTML archive of the document, or <c>null</c>.</param>
		public ResourceResolver(Uri baseUri, MhtmlArchive archive)
		{
			this.BaseUri = baseUri;
			_archive = archive;
		}

		/// <summary>
		/// Gets or sets the base URL of the document, or <c>null</c>.
		/// </summary>
		public Uri BaseUri { get; set; }

		#region URLs

		/// <summary>
		/// Resolves a URL against the base URL.
		/// </summary>
		/// <param name="url">The URL as written in the document. Can be <c>null</c>.</param>
		/// <returns>The absolute URL, the trimmed URL when it cannot be resolved, or <c>null</c> for an empty URL.</returns>
		public string Resolve(string url)
		{
			url = StripNewlines(url?.Trim());
			if (String.IsNullOrEmpty(url))
				return null;

			if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
				return url;

			// without a base URL a relative reference stays as written; parsing it would only cost time.
			if (this.BaseUri == null && !HasScheme(url))
				return url;

			try
			{
				if (Uri.TryCreate(url, UriKind.Absolute, out var absolute) && !IsBareFilePath(url, absolute))
					return absolute.AbsoluteUri;

				if (this.BaseUri != null && Uri.TryCreate(this.BaseUri, url, out var resolved))
					return resolved.AbsoluteUri;
			}
			catch (UriFormatException)
			{
				// malformed URLs are kept as written.
			}

			return url;
		}

		/// <summary>
		/// Determines whether the target of a link is worth keeping: not a script, and not an anchor in the same document.
		/// </summary>
		/// <param name="href">The <c>href</c> attribute. Can be <c>null</c>.</param>
		/// <param name="resolved">Receives the resolved target.</param>
		/// <returns><c>true</c> when the link is kept.</returns>
		public bool TryResolveLink(string href, out string resolved)
		{
			resolved = null;
			href = StripNewlines(href?.Trim());

			if (String.IsNullOrEmpty(href) || href[0] == '#')
				return false;

			if (href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
				|| href.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase)
				|| href.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			resolved = Resolve(href);

			// a link to an anchor of the document itself.
			if (this.BaseUri != null && resolved.StartsWith(this.BaseUri.GetLeftPart(UriPartial.Query) + "#", StringComparison.Ordinal))
				return false;

			return true;
		}

		/// <summary>
		/// Removes the tabs and line breaks of a URL, which the URL standard ignores ("foo\nbar.html").
		/// </summary>
		/// <param name="url">The URL. Can be <c>null</c>.</param>
		/// <returns>The URL without tabs and line breaks.</returns>
		private static string StripNewlines(string url)
		{
			if (url == null || url.IndexOfAny(new[] { '\t', '\n', '\r' }) < 0)
				return url;

			return url.Replace("\t", "").Replace("\n", "").Replace("\r", "");
		}

		/// <summary>
		/// Determines whether a URL starts with a scheme, such as <c>https:</c> or <c>mailto:</c> (RFC 3986, 3.1).
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns><c>true</c> when the URL is absolute.</returns>
		private static bool HasScheme(string url)
		{
			for (var i = 0; i < url.Length; i++)
			{
				var c = url[i];
				if (c == ':')
					return i > 0;

				var valid = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (i > 0 && ((c >= '0' && c <= '9') || c == '+' || c == '-' || c == '.'));
				if (!valid)
					return false;
			}

			return false;
		}

		/// <summary>
		/// Determines whether a string parsed as an absolute URI is actually a relative path, as
		/// <c>Uri</c> reads "C:/x" or "/x" as absolute file URIs on some platforms.
		/// </summary>
		/// <param name="url">The original string.</param>
		/// <param name="uri">The parsed URI.</param>
		/// <returns><c>true</c> for a bare path.</returns>
		private static bool IsBareFilePath(string url, Uri uri)
		{
			return uri.IsFile && !url.StartsWith("file:", StringComparison.OrdinalIgnoreCase);
		}

		#endregion

		#region Resources

		/// <summary>
		/// Loads the text of a linked resource stored in the MHTML archive, such as a style sheet.
		/// </summary>
		/// <param name="url">The URL as written in the document.</param>
		/// <returns>The text, or <c>null</c> when the resource is not available.</returns>
		public string LoadText(string url)
		{
			var part = _archive?.Find(url, Resolve(url));
			if (part == null)
				return null;

			var encoding = EncodingSniffer.GetEncoding(part.Charset) ?? Encoding.UTF8;
			return encoding.GetString(part.Data);
		}

		/// <summary>
		/// Resolves the picture of an image source.
		/// </summary>
		/// <param name="src">The source as written in the document.</param>
		/// <returns>The picture, or <c>null</c> when the source is empty or invalid.</returns>
		public ImageSource LoadImage(string src)
		{
			src = src?.Trim();
			if (String.IsNullOrEmpty(src))
				return null;

			if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
			{
				if (!TryDecodeDataUri(src, out var contentType, out var data) || data.Length == 0)
					return null;

				return Embedded("data:" + Hash(data), contentType, data);
			}

			var resolved = Resolve(src);

			var part = _archive?.Find(src, resolved);
			if (part != null)
				return Embedded("part:" + (part.Location ?? part.ContentId), part.ContentType, part.Data);

			if (_images.TryGetValue("url:" + resolved, out var existing))
				return existing;

			var image = new ImageSource
			{
				Key = "url:" + resolved,
				Url = resolved,
				FileName = UrlFileName(resolved),
				ContentType = ContentTypeFromExtension(resolved),
			};

			_images[image.Key] = image;
			return image;
		}

		/// <summary>
		/// Registers an embedded picture produced by the document itself, such as an inline SVG.
		/// </summary>
		/// <param name="contentType">The MIME type.</param>
		/// <param name="data">The bytes.</param>
		/// <returns>The picture.</returns>
		public ImageSource AddEmbedded(string contentType, byte[] data)
		{
			return Embedded("inline:" + Hash(data), contentType, data);
		}

		/// <summary>
		/// Returns the shared picture for embedded bytes, naming it on first use.
		/// </summary>
		/// <param name="key">The key of the picture.</param>
		/// <param name="contentType">The MIME type.</param>
		/// <param name="data">The bytes.</param>
		/// <returns>The picture.</returns>
		private ImageSource Embedded(string key, string contentType, byte[] data)
		{
			if (_images.TryGetValue(key, out var existing))
				return existing;

			contentType = String.IsNullOrEmpty(contentType) ? SniffContentType(data) : contentType.ToLowerInvariant();

			var image = new ImageSource
			{
				Key = key,
				ContentType = contentType,
				Data = data,
				FileName = "image" + (++_embeddedCount) + "." + Extension(contentType),
			};

			_images[key] = image;
			return image;
		}

		/// <summary>
		/// Decodes a <c>data:</c> URI (RFC 2397).
		/// </summary>
		/// <param name="uri">The URI.</param>
		/// <param name="contentType">Receives the MIME type, or <c>null</c>.</param>
		/// <param name="data">Receives the bytes.</param>
		/// <returns><c>false</c> when the URI is malformed.</returns>
		internal static bool TryDecodeDataUri(string uri, out string contentType, out byte[] data)
		{
			contentType = null;
			data = null;

			var comma = uri.IndexOf(',');
			if (comma < 0)
				return false;

			var header = uri.Substring(5, comma - 5);
			var payload = uri.Substring(comma + 1);
			var parameters = header.Split(';');
			var base64 = false;

			for (var i = 0; i < parameters.Length; i++)
			{
				var parameter = parameters[i].Trim();
				if (i == 0 && parameter.IndexOf('/') > 0)
					contentType = parameter.ToLowerInvariant();
				else if (parameter.Equals("base64", StringComparison.OrdinalIgnoreCase))
					base64 = true;
			}

			try
			{
				if (base64)
				{
					var sb = new StringBuilder(payload.Length);
					foreach (var c in Urls.Unescape(payload))
					{
						if (!Char.IsWhiteSpace(c))
							sb.Append(c == '-' ? '+' : c == '_' ? '/' : c);
					}

					while (sb.Length % 4 != 0)
						sb.Append('=');

					data = Convert.FromBase64String(sb.ToString());
				}
				else
				{
					data = Encoding.UTF8.GetBytes(Urls.Unescape(payload));
				}

				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Returns a short hash of bytes, used to de-duplicate embedded pictures.
		/// </summary>
		/// <param name="data">The bytes.</param>
		/// <returns>The hash, in hexadecimal.</returns>
		private static string Hash(byte[] data)
		{
			using (var sha = SHA256.Create())
			{
				var hash = sha.ComputeHash(data);
				return BitConverter.ToString(hash, 0, 16).Replace("-", "");
			}
		}

		/// <summary>
		/// Returns the last path segment of a URL, unescaped.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns>The file name, or <c>image</c>.</returns>
		private static string UrlFileName(string url)
		{
			var end = url.IndexOfAny(new[] { '?', '#' });
			var path = end < 0 ? url : url.Substring(0, end);
			var name = path.Substring(path.LastIndexOf('/') + 1);

			name = Urls.Unescape(name);
			return name.Length == 0 ? "image" : name;
		}

		/// <summary>
		/// Guesses the MIME type of an image from the extension of its URL.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns>The MIME type, or <c>null</c>.</returns>
		private static string ContentTypeFromExtension(string url)
		{
			var name = UrlFileName(url).ToLowerInvariant();
			var dot = name.LastIndexOf('.');
			if (dot < 0)
				return null;

			switch (name.Substring(dot + 1))
			{
				case "png":
					return "image/png";

				case "jpg":
				case "jpeg":
					return "image/jpeg";

				case "gif":
					return "image/gif";

				case "webp":
					return "image/webp";

				case "svg":
					return "image/svg+xml";

				case "bmp":
					return "image/bmp";

				case "ico":
					return "image/x-icon";

				case "avif":
					return "image/avif";

				case "tif":
				case "tiff":
					return "image/tiff";

				default:
					return null;
			}
		}

		/// <summary>
		/// Returns the file extension of a MIME type.
		/// </summary>
		/// <param name="contentType">The MIME type.</param>
		/// <returns>The extension, without the dot.</returns>
		private static string Extension(string contentType)
		{
			switch (contentType)
			{
				case "image/png":
					return "png";

				case "image/jpeg":
				case "image/jpg":
				case "image/pjpeg":
					return "jpg";

				case "image/gif":
					return "gif";

				case "image/webp":
					return "webp";

				case "image/svg+xml":
					return "svg";

				case "image/bmp":
				case "image/x-ms-bmp":
					return "bmp";

				case "image/x-icon":
				case "image/vnd.microsoft.icon":
					return "ico";

				case "image/avif":
					return "avif";

				case "image/tiff":
					return "tif";

				case "image/x-emf":
				case "image/emf":
					return "emf";

				case "image/x-wmf":
				case "image/wmf":
					return "wmf";

				default:
					return "bin";
			}
		}

		/// <summary>
		/// Identifies the format of image bytes from their signature.
		/// </summary>
		/// <param name="data">The bytes.</param>
		/// <returns>The MIME type, or <c>application/octet-stream</c>.</returns>
		private static string SniffContentType(byte[] data)
		{
			bool Starts(params byte[] signature)
			{
				if (data.Length < signature.Length)
					return false;

				for (var i = 0; i < signature.Length; i++)
				{
					if (data[i] != signature[i])
						return false;
				}

				return true;
			}

			if (Starts(0x89, 0x50, 0x4E, 0x47))
				return "image/png";
			if (Starts(0xFF, 0xD8, 0xFF))
				return "image/jpeg";
			if (Starts(0x47, 0x49, 0x46, 0x38))
				return "image/gif";
			if (Starts(0x42, 0x4D))
				return "image/bmp";
			if (data.Length > 12 && Starts(0x52, 0x49, 0x46, 0x46) && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
				return "image/webp";
			if (Starts(0x3C, 0x73, 0x76, 0x67) || Starts(0x3C, 0x3F, 0x78, 0x6D, 0x6C))
				return "image/svg+xml";

			return "application/octet-stream";
		}

		#endregion
	}
}

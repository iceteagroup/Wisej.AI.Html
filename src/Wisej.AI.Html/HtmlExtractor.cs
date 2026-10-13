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
using Wisej.AI.Html.Content;
using Wisej.AI.Html.Dom;
using Wisej.AI.Html.Mhtml;
using Wisej.AI.Html.Rendering;

namespace Wisej.AI.Html
{
	/// <summary>
	/// Extracts the content of HTML documents (HTML, XHTML and MHTML web archives) as Markdown or plain
	/// text, ready to be used as language model context.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The document is parsed with the algorithm of the HTML standard, so the tree is the one a browser
	/// builds, and rendered the way a browser displays it: the author style sheets are applied to find
	/// hidden content, emphasis, code fonts and page breaks, and white space collapses as in CSS.
	/// </para>
	/// <para>
	/// The output keeps the information of the document: headings, emphasis, links, nested lists with
	/// their real numbers, tables with merged cells and multi-row headers, images (embedded images are
	/// referenced by file name and returned in <see cref="HtmlExtractionResult.Images"/>), code blocks with
	/// their language, quotations, definition lists, MathML equations as LaTeX, form values, and the
	/// document metadata. Scripts, styles, navigation, decorative images and hidden content are removed.
	/// </para>
	/// <para>
	/// Instances are not thread safe but are cheap: create one per thread or per document.
	/// </para>
	/// <para>
	/// Example:
	/// <code><![CDATA[
	/// var result = new HtmlExtractor().Extract("page.html");
	///
	/// Console.WriteLine(result.Metadata.Title);
	/// File.WriteAllText("page.md", result.Text);
	///
	/// foreach (var image in result.Images)
	///     if (image.Data != null)
	///         File.WriteAllBytes(image.FileName, image.Data);
	/// ]]></code>
	/// </para>
	/// </remarks>
	public sealed class HtmlExtractor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlExtractor"/> class with the default options.
		/// </summary>
		public HtmlExtractor()
			: this(new HtmlExtractorOptions())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlExtractor"/> class.
		/// </summary>
		/// <param name="options">The extraction options.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
		public HtmlExtractor(HtmlExtractorOptions options)
		{
			this.Options = options ?? throw new ArgumentNullException(nameof(options));
		}

		/// <summary>
		/// Gets the extraction options.
		/// </summary>
		public HtmlExtractorOptions Options { get; }

		#region Extract

		/// <summary>
		/// Extracts the content of an HTML or MHTML file.
		/// </summary>
		/// <param name="path">The path of the file.</param>
		/// <returns>The extracted content.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <c>null</c>.</exception>
		public HtmlExtractionResult Extract(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			return Extract(File.ReadAllBytes(path));
		}

		/// <summary>
		/// Extracts the content of an HTML or MHTML document from a stream, detecting its encoding.
		/// </summary>
		/// <param name="stream">The stream. It is read from its current position to the end and is not closed.</param>
		/// <returns>The extracted content.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
		public HtmlExtractionResult Extract(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			using (var buffer = new MemoryStream())
			{
				stream.CopyTo(buffer);
				return Extract(buffer.ToArray());
			}
		}

		/// <summary>
		/// Extracts the content of an HTML or MHTML document from bytes, detecting its encoding.
		/// </summary>
		/// <param name="data">The bytes of the document.</param>
		/// <returns>The extracted content.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <c>null</c>.</exception>
		public HtmlExtractionResult Extract(byte[] data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			var parserOptions = new HtmlParserOptions { Encoding = this.Options.Encoding };

			var archive = MhtmlArchive.IsArchive(data) ? MhtmlArchive.Read(data) : null;
			if (archive != null)
			{
				parserOptions.TransportEncoding = archive.Root.Charset;
				return Extract(HtmlDocument.Load(archive.Root.Data, parserOptions), archive);
			}

			return Extract(HtmlDocument.Load(data, parserOptions), null);
		}

		/// <summary>
		/// Extracts the content of an HTML document from a string.
		/// </summary>
		/// <param name="html">The markup.</param>
		/// <returns>The extracted content.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="html"/> is <c>null</c>.</exception>
		public HtmlExtractionResult ExtractFromString(string html)
		{
			if (html == null)
				throw new ArgumentNullException(nameof(html));

			return Extract(HtmlDocument.Parse(html), null);
		}

		/// <summary>
		/// Extracts the content of a parsed document.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <returns>The extracted content.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is <c>null</c>.</exception>
		public HtmlExtractionResult Extract(HtmlDocument document)
		{
			if (document == null)
				throw new ArgumentNullException(nameof(document));

			return Extract(document, null);
		}

		/// <summary>
		/// Converts HTML to Markdown with the default options.
		/// </summary>
		/// <param name="html">The markup.</param>
		/// <returns>The Markdown text.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="html"/> is <c>null</c>.</exception>
		public static string ToMarkdown(string html)
		{
			return new HtmlExtractor().ExtractFromString(html).Text;
		}

		/// <summary>
		/// Converts HTML to plain text with the default options.
		/// </summary>
		/// <param name="html">The markup.</param>
		/// <returns>The plain text.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="html"/> is <c>null</c>.</exception>
		public static string ToText(string html)
		{
			return new HtmlExtractor(new HtmlExtractorOptions { Format = OutputFormat.PlainText }).ExtractFromString(html).Text;
		}

		/// <summary>
		/// Extracts the text of an HTML or MHTML document.
		/// </summary>
		/// <param name="stream">The stream. It is read from its current position to the end and is not closed.</param>
		/// <param name="options">The extraction options, or <c>null</c> for the defaults.</param>
		/// <returns>The text of the document.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
		public static string ExtractText(Stream stream, HtmlExtractorOptions options = null)
		{
			return new HtmlExtractor(options ?? new HtmlExtractorOptions()).Extract(stream).Text;
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Extracts the content of a parsed document.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <param name="archive">The MHTML archive that contains the document, or <c>null</c>.</param>
		/// <returns>The extracted content.</returns>
		private HtmlExtractionResult Extract(HtmlDocument document, MhtmlArchive archive)
		{
			var resources = new ResourceResolver(null, archive);
			resources.BaseUri = ResolveBaseUri(document, archive, resources);

			var content = new ContentBuilder(document, this.Options, resources).Build();

			DescribeImages(content);

			var renderer = this.Options.Format == OutputFormat.PlainText
				? (DocumentRenderer)new PlainTextRenderer(content, this.Options)
				: new MarkdownRenderer(content, this.Options);

			var result = renderer.Render();

			// the bytes were only loaded for the describer.
			if (!this.Options.IncludeImageData)
			{
				foreach (var image in content.Images)
					image.Data = null;
			}

			return result;
		}

		/// <summary>
		/// Determines the base URL of the document: the option, the location of an MHTML archive, and the
		/// <c>&lt;base href&gt;</c> of the document resolved against them.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <param name="archive">The MHTML archive, or <c>null</c>.</param>
		/// <param name="resources">The resolver, used to resolve the base element.</param>
		/// <returns>The base URL, or <c>null</c>.</returns>
		private Uri ResolveBaseUri(HtmlDocument document, MhtmlArchive archive, ResourceResolver resources)
		{
			// a relative base cannot resolve anything.
			var baseUri = this.Options.BaseUri != null && this.Options.BaseUri.IsAbsoluteUri ? this.Options.BaseUri : null;

			if (baseUri == null && archive?.Root.Location != null && Uri.TryCreate(archive.Root.Location, UriKind.Absolute, out var location))
				baseUri = location;

			resources.BaseUri = baseUri;

			foreach (var element in document.DescendantElements())
			{
				if (element.Is("base") && element.HasAttribute("href"))
				{
					var href = resources.Resolve(element.GetAttribute("href"));
					if (href != null && Uri.TryCreate(href, UriKind.Absolute, out var resolved))
						return resolved;

					break;
				}
			}

			return baseUri;
		}

		/// <summary>
		/// Invokes the image describer once for every distinct picture and assigns the description to all its placements.
		/// </summary>
		/// <param name="content">The content.</param>
		private void DescribeImages(DocumentContent content)
		{
			var describer = this.Options.ImageDescriber;
			if (describer == null)
				return;

			var descriptions = new Dictionary<string, string>(StringComparer.Ordinal);

			foreach (var image in content.Images)
			{
				var key = image.Key ?? image.FileName;
				if (!descriptions.TryGetValue(key, out var description))
				{
					description = describer(image);
					descriptions[key] = description;
				}

				image.Description = String.IsNullOrWhiteSpace(description) ? null : description.Trim();
			}
		}

		#endregion
	}
}

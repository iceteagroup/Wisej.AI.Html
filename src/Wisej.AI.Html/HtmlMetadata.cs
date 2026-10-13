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

namespace Wisej.AI.Html
{
	/// <summary>
	/// The properties of an HTML document, read from its <c>&lt;head&gt;</c>: title, <c>&lt;meta&gt;</c>
	/// tags (including Open Graph, Dublin Core and article properties) and links.
	/// </summary>
	public sealed class HtmlMetadata
	{
		/// <summary>
		/// Gets the title: the <c>&lt;title&gt;</c> element, or the Open Graph or Dublin Core title. Can be <c>null</c>.
		/// </summary>
		public string Title { get; internal set; }

		/// <summary>
		/// Gets the description (<c>description</c>, <c>og:description</c> or <c>dc.description</c>), or <c>null</c>.
		/// </summary>
		public string Description { get; internal set; }

		/// <summary>
		/// Gets the keywords, or <c>null</c>.
		/// </summary>
		public string Keywords { get; internal set; }

		/// <summary>
		/// Gets the author (<c>author</c>, <c>article:author</c> or <c>dc.creator</c>), or <c>null</c>.
		/// </summary>
		public string Author { get; internal set; }

		/// <summary>
		/// Gets the name of the web site (<c>og:site_name</c> or <c>application-name</c>), or <c>null</c>.
		/// </summary>
		public string SiteName { get; internal set; }

		/// <summary>
		/// Gets the language of the document, from the <c>lang</c> attribute of the root element or the
		/// <c>content-language</c> pragma, or <c>null</c>.
		/// </summary>
		public string Language { get; internal set; }

		/// <summary>
		/// Gets the canonical URL of the document (<c>&lt;link rel="canonical"&gt;</c> or <c>og:url</c>), or <c>null</c>.
		/// </summary>
		public string CanonicalUrl { get; internal set; }

		/// <summary>
		/// Gets the base URL used to resolve relative links and images, or <c>null</c>.
		/// </summary>
		public string BaseUrl { get; internal set; }

		/// <summary>
		/// Gets the application that generated the document (<c>generator</c>), or <c>null</c>.
		/// </summary>
		public string Generator { get; internal set; }

		/// <summary>
		/// Gets the publication date (<c>article:published_time</c>, <c>dc.date</c>, <c>date</c>, ...), or <c>null</c>.
		/// </summary>
		public DateTimeOffset? Published { get; internal set; }

		/// <summary>
		/// Gets the last modification date (<c>article:modified_time</c>, <c>last-modified</c>, ...), or <c>null</c>.
		/// </summary>
		public DateTimeOffset? Modified { get; internal set; }

		/// <summary>
		/// Gets the name of the character encoding used to decode the document, such as <c>utf-8</c>, or
		/// <c>null</c> when the document was extracted from a string.
		/// </summary>
		public string Encoding { get; internal set; }

		/// <summary>
		/// Gets every <c>&lt;meta&gt;</c> tag with a <c>name</c>, <c>property</c>, <c>itemprop</c> or
		/// <c>http-equiv</c> and a <c>content</c>, by lowercase name. The first occurrence of a name wins.
		/// </summary>
		public IReadOnlyDictionary<string, string> Properties { get; internal set; } = new Dictionary<string, string>();
	}
}

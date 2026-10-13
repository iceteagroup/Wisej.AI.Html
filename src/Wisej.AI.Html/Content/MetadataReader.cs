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
using Wisej.AI.Html.Dom;

namespace Wisej.AI.Html.Content
{
	/// <summary>
	/// Reads the document properties from the title, the <c>&lt;meta&gt;</c> tags and the links of a document.
	/// </summary>
	internal static class MetadataReader
	{
		/// <summary>
		/// Reads the metadata of a document.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <param name="resources">Resolves relative URLs.</param>
		/// <returns>The metadata.</returns>
		public static HtmlMetadata Read(HtmlDocument document, ResourceResolver resources)
		{
			var properties = new Dictionary<string, string>(StringComparer.Ordinal);
			string canonical = null;

			foreach (var element in document.DescendantElements())
			{
				if (element.Is("meta"))
				{
					var content = element.GetAttribute("content");
					var name = element.GetAttribute("name") ?? element.GetAttribute("property") ?? element.GetAttribute("itemprop") ?? element.GetAttribute("http-equiv");

					if (content != null && !String.IsNullOrWhiteSpace(name))
					{
						name = name.Trim().ToLowerInvariant();
						if (!properties.ContainsKey(name))
							properties[name] = HtmlDocument.CollapseWhitespace(content);
					}
				}
				else if (element.Is("link") && canonical == null)
				{
					var rel = element.GetAttribute("rel");
					if (rel != null && rel.Trim().Equals("canonical", StringComparison.OrdinalIgnoreCase))
						canonical = resources.Resolve(element.GetAttribute("href"));
				}
			}

			string First(params string[] names)
			{
				foreach (var name in names)
				{
					if (properties.TryGetValue(name, out var value) && value.Length > 0)
						return value;
				}

				return null;
			}

			var title = document.Title;
			var language = document.DocumentElement?.GetAttribute("lang") ?? document.DocumentElement?.GetAttribute("xml:lang");

			return new HtmlMetadata
			{
				Title = title.Length > 0 ? title : First("og:title", "twitter:title", "dc.title", "dcterms.title", "citation_title"),
				Description = First("description", "og:description", "twitter:description", "dc.description", "dcterms.description"),
				Keywords = First("keywords", "news_keywords"),
				Author = First("author", "article:author", "dc.creator", "dcterms.creator", "citation_author", "twitter:creator"),
				SiteName = First("og:site_name", "application-name"),
				Language = String.IsNullOrWhiteSpace(language) ? First("content-language", "dc.language", "og:locale") : language.Trim(),
				CanonicalUrl = canonical ?? First("og:url"),
				BaseUrl = resources.BaseUri?.AbsoluteUri,
				Generator = First("generator"),
				Published = ParseDate(First("article:published_time", "datepublished", "dc.date", "dcterms.created", "dc.date.issued", "date", "pubdate", "citation_publication_date", "og:published_time")),
				Modified = ParseDate(First("article:modified_time", "datemodified", "og:updated_time", "dcterms.modified", "last-modified", "revised")),
				Encoding = document.Encoding?.WebName,
				Properties = properties,
			};
		}

		/// <summary>
		/// Parses a date written in a <c>&lt;meta&gt;</c> tag.
		/// </summary>
		/// <param name="value">The value. Can be <c>null</c>.</param>
		/// <returns>The date, or <c>null</c> when it cannot be parsed.</returns>
		private static DateTimeOffset? ParseDate(string value)
		{
			if (String.IsNullOrWhiteSpace(value))
				return null;

			return DateTimeOffset.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
				? date
				: (DateTimeOffset?)null;
		}
	}
}

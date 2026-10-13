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
using Wisej.AI.Html.Dom;

namespace Wisej.AI.Html.Parsing
{
	/// <summary>
	/// The element categories used by the tree construction algorithm (HTML standard, 13.2.4.2).
	/// </summary>
	internal static class ElementCategories
	{
		/// <summary>
		/// The HTML elements of the special category.
		/// </summary>
		/// <remarks>
		/// <c>select</c> is deliberately absent: since the customizable select parser changes (2025) it
		/// is parsed in the "in body" insertion mode and formatting end tags may close it.
		/// </remarks>
		private static readonly HashSet<string> SpecialHtml = new HashSet<string>(StringComparer.Ordinal)
		{
			"address", "applet", "area", "article", "aside", "base", "basefont", "bgsound", "blockquote", "body",
			"br", "button", "caption", "center", "col", "colgroup", "dd", "details", "dir", "div", "dl", "dt",
			"embed", "fieldset", "figcaption", "figure", "footer", "form", "frame", "frameset", "h1", "h2", "h3",
			"h4", "h5", "h6", "head", "header", "hgroup", "hr", "html", "iframe", "img", "input", "keygen", "li",
			"link", "listing", "main", "marquee", "menu", "meta", "nav", "noembed", "noframes", "noscript",
			"object", "ol", "p", "param", "plaintext", "pre", "script", "search", "section", "source",
			"style", "summary", "table", "tbody", "td", "template", "textarea", "tfoot", "th", "thead", "title",
			"tr", "track", "ul", "wbr", "xmp",
		};

		/// <summary>
		/// The HTML elements that end the default scope.
		/// </summary>
		private static readonly HashSet<string> DefaultScopeHtml = new HashSet<string>(StringComparer.Ordinal)
		{
			"applet", "caption", "html", "table", "td", "th", "marquee", "object", "template",
		};

		/// <summary>
		/// The elements closed by "generate implied end tags".
		/// </summary>
		private static readonly HashSet<string> ImpliedEndTags = new HashSet<string>(StringComparer.Ordinal)
		{
			"dd", "dt", "li", "optgroup", "option", "p", "rb", "rp", "rt", "rtc",
		};

		/// <summary>
		/// The elements closed by "generate all implied end tags thoroughly".
		/// </summary>
		private static readonly HashSet<string> ThoroughImpliedEndTags = new HashSet<string>(StringComparer.Ordinal)
		{
			"caption", "colgroup", "dd", "dt", "li", "optgroup", "option", "p", "rb", "rp", "rt", "rtc",
			"tbody", "td", "tfoot", "th", "thead", "tr",
		};

		/// <summary>
		/// The formatting elements, which the list of active formatting elements reopens after misnesting.
		/// </summary>
		private static readonly HashSet<string> Formatting = new HashSet<string>(StringComparer.Ordinal)
		{
			"a", "b", "big", "code", "em", "font", "i", "nobr", "s", "small", "strike", "strong", "tt", "u",
		};

		/// <summary>
		/// Determines whether an element is in the special category.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> for the special elements of every namespace.</returns>
		public static bool IsSpecial(HtmlElement element)
		{
			switch (element.NamespaceUri)
			{
				case HtmlNamespaces.Html:
					return SpecialHtml.Contains(element.LocalName);

				case HtmlNamespaces.MathML:
					return IsMathMLTextIntegrationPoint(element) || element.LocalName == "annotation-xml";

				case HtmlNamespaces.Svg:
					return element.LocalName == "foreignObject" || element.LocalName == "desc" || element.LocalName == "title";

				default:
					return false;
			}
		}

		/// <summary>
		/// Determines whether an element ends the default scope.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> when the element is a boundary of the default scope.</returns>
		public static bool IsDefaultScopeBoundary(HtmlElement element)
		{
			switch (element.NamespaceUri)
			{
				case HtmlNamespaces.Html:
					return DefaultScopeHtml.Contains(element.LocalName);

				case HtmlNamespaces.MathML:
					return IsMathMLTextIntegrationPoint(element) || element.LocalName == "annotation-xml";

				case HtmlNamespaces.Svg:
					return element.LocalName == "foreignObject" || element.LocalName == "desc" || element.LocalName == "title";

				default:
					return false;
			}
		}

		/// <summary>
		/// Determines whether an element is closed by "generate implied end tags".
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="thoroughly">Whether the table elements are closed as well.</param>
		/// <returns><c>true</c> when the element has an implied end tag.</returns>
		public static bool HasImpliedEndTag(HtmlElement element, bool thoroughly)
		{
			return element.IsHtml && (thoroughly ? ThoroughImpliedEndTags : ImpliedEndTags).Contains(element.LocalName);
		}

		/// <summary>
		/// Determines whether a tag name is a formatting element.
		/// </summary>
		/// <param name="name">The tag name.</param>
		/// <returns><c>true</c> for the formatting elements.</returns>
		public static bool IsFormatting(string name)
		{
			return Formatting.Contains(name);
		}

		/// <summary>
		/// Determines whether an element is a MathML text integration point (13.2.6, tree construction).
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> for the MathML <c>mi</c>, <c>mo</c>, <c>mn</c>, <c>ms</c> and <c>mtext</c> elements.</returns>
		public static bool IsMathMLTextIntegrationPoint(HtmlElement element)
		{
			if (element.NamespaceUri != HtmlNamespaces.MathML)
				return false;

			switch (element.LocalName)
			{
				case "mi":
				case "mo":
				case "mn":
				case "ms":
				case "mtext":
					return true;

				default:
					return false;
			}
		}

		/// <summary>
		/// Determines whether an element is an HTML integration point, where HTML content resumes inside SVG or MathML.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> for the SVG <c>foreignObject</c>, <c>desc</c> and <c>title</c> elements and for MathML <c>annotation-xml</c> with an HTML encoding.</returns>
		public static bool IsHtmlIntegrationPoint(HtmlElement element)
		{
			if (element.NamespaceUri == HtmlNamespaces.Svg)
				return element.LocalName == "foreignObject" || element.LocalName == "desc" || element.LocalName == "title";

			if (element.NamespaceUri == HtmlNamespaces.MathML && element.LocalName == "annotation-xml")
			{
				var encoding = element.GetAttribute("encoding");
				return encoding != null
					&& (String.Equals(encoding, "text/html", StringComparison.OrdinalIgnoreCase)
						|| String.Equals(encoding, "application/xhtml+xml", StringComparison.OrdinalIgnoreCase));
			}

			return false;
		}
	}
}

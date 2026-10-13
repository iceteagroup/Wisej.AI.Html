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
	/// The rules for parsing tokens in foreign content: SVG and MathML (HTML standard, 13.2.6.5).
	/// </summary>
	internal sealed partial class HtmlTreeBuilder
	{
		#region Adjustment tables

		/// <summary>
		/// The SVG element names that are not lowercase (13.2.6.5, adjust SVG tag names).
		/// </summary>
		private static readonly Dictionary<string, string> SvgTagNames = CreateCaseMap(
			"altGlyph", "altGlyphDef", "altGlyphItem", "animateColor", "animateMotion", "animateTransform", "clipPath",
			"feBlend", "feColorMatrix", "feComponentTransfer", "feComposite", "feConvolveMatrix", "feDiffuseLighting",
			"feDisplacementMap", "feDistantLight", "feDropShadow", "feFlood", "feFuncA", "feFuncB", "feFuncG", "feFuncR",
			"feGaussianBlur", "feImage", "feMerge", "feMergeNode", "feMorphology", "feOffset", "fePointLight",
			"feSpecularLighting", "feSpotLight", "feTile", "feTurbulence", "foreignObject", "glyphRef", "linearGradient",
			"radialGradient", "textPath");

		/// <summary>
		/// The SVG attribute names that are not lowercase (13.2.6.1, adjust SVG attributes).
		/// </summary>
		private static readonly Dictionary<string, string> SvgAttributeNames = CreateCaseMap(
			"attributeName", "attributeType", "baseFrequency", "baseProfile", "calcMode", "clipPathUnits", "diffuseConstant",
			"edgeMode", "filterUnits", "glyphRef", "gradientTransform", "gradientUnits", "kernelMatrix", "kernelUnitLength",
			"keyPoints", "keySplines", "keyTimes", "lengthAdjust", "limitingConeAngle", "markerHeight", "markerUnits",
			"markerWidth", "maskContentUnits", "maskUnits", "numOctaves", "pathLength", "patternContentUnits",
			"patternTransform", "patternUnits", "pointsAtX", "pointsAtY", "pointsAtZ", "preserveAlpha",
			"preserveAspectRatio", "primitiveUnits", "refX", "refY", "repeatCount", "repeatDur", "requiredExtensions",
			"requiredFeatures", "specularConstant", "specularExponent", "spreadMethod", "startOffset", "stdDeviation",
			"stitchTiles", "surfaceScale", "systemLanguage", "tableValues", "targetX", "targetY", "textLength", "viewBox",
			"viewTarget", "xChannelSelector", "yChannelSelector", "zoomAndPan");

		/// <summary>
		/// The HTML start tags that break out of foreign content (13.2.6.5).
		/// </summary>
		private static readonly HashSet<string> BreakoutTags = new HashSet<string>(StringComparer.Ordinal)
		{
			"b", "big", "blockquote", "body", "br", "center", "code", "dd", "div", "dl", "dt", "em", "embed",
			"h1", "h2", "h3", "h4", "h5", "h6", "head", "hr", "i", "img", "li", "listing", "menu", "meta", "nobr",
			"ol", "p", "pre", "ruby", "s", "small", "span", "strong", "strike", "sub", "sup", "table", "tt", "u",
			"ul", "var",
		};

		/// <summary>
		/// Creates a map from the lowercase form of names to their canonical case.
		/// </summary>
		/// <param name="names">The canonical names.</param>
		/// <returns>The map.</returns>
		private static Dictionary<string, string> CreateCaseMap(params string[] names)
		{
			var map = new Dictionary<string, string>(names.Length, StringComparer.Ordinal);
			foreach (var name in names)
				map[name.ToLowerInvariant()] = name;

			return map;
		}

		#endregion

		#region Foreign content

		/// <summary>
		/// 13.2.6.5 The rules for parsing tokens in foreign content.
		/// </summary>
		/// <param name="token">The token.</param>
		private void ProcessForeignContent(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Characters:
					var data = token.Data;
					if (data.IndexOf('\0') >= 0)
					{
						// a replaced NUL does not count as content for the frameset-ok flag.
						if (!IsAllWhitespace(data.Replace("\0", "")))
							_framesetOk = false;

						InsertCharacters(data.Replace('\0', '�'));
						return;
					}

					InsertCharacters(data);

					if (!IsAllWhitespace(data))
						_framesetOk = false;
					return;

				case HtmlTokenType.Comment:
					InsertComment(token);
					return;

				case HtmlTokenType.Doctype:
					return;

				case HtmlTokenType.StartTag:
					if (BreakoutTags.Contains(token.Name)
						|| (token.Name == "font" && (token.GetAttribute("color") != null || token.GetAttribute("face") != null || token.GetAttribute("size") != null)))
					{
						BreakOutOfForeignContent(token);
						return;
					}

					var adjusted = this.AdjustedCurrentNode;
					if (adjusted.NamespaceUri == HtmlNamespaces.Svg && SvgTagNames.TryGetValue(token.Name, out var svgName))
						token.Name = svgName;

					InsertForeignElement(token, adjusted.NamespaceUri);
					return;

				case HtmlTokenType.EndTag:
					if (token.Name == "br" || token.Name == "p")
					{
						BreakOutOfForeignContent(token);
						return;
					}

					ForeignEndTag(token);
					return;
			}
		}

		/// <summary>
		/// Leaves foreign content for an HTML tag: pops the foreign elements and reprocesses the token
		/// with the rules of the current insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void BreakOutOfForeignContent(HtmlToken token)
		{
			while (true)
			{
				var current = this.CurrentNode;
				if (current.IsHtml || ElementCategories.IsMathMLTextIntegrationPoint(current) || ElementCategories.IsHtmlIntegrationPoint(current))
					break;

				Pop();
			}

			Process(_mode, token);
		}

		/// <summary>
		/// Processes an end tag in foreign content.
		/// </summary>
		/// <param name="token">The end tag.</param>
		private void ForeignEndTag(HtmlToken token)
		{
			for (var i = _stack.Count - 1; i > 0; i--)
			{
				var node = _stack[i];

				if (String.Equals(node.LocalName, token.Name, StringComparison.OrdinalIgnoreCase))
				{
					PopUntil(node);
					return;
				}

				// the end tag of an HTML element is processed by the current insertion mode.
				if (_stack[i - 1].IsHtml)
				{
					Process(_mode, token);
					return;
				}
			}
		}

		/// <summary>
		/// Inserts a foreign element for a token, adjusting its attributes (13.2.6.1).
		/// </summary>
		/// <param name="token">The start tag.</param>
		/// <param name="namespaceUri">The namespace of the element.</param>
		private void InsertForeignElement(HtmlToken token, string namespaceUri)
		{
			InsertElement(CreateElement(token, namespaceUri));

			// foreign elements may be self-closing: "<circle/>".
			if (token.SelfClosing)
				Pop();
		}

		/// <summary>
		/// Creates an attribute of a foreign element, adjusting its name for SVG and MathML and its
		/// namespace for the xlink, xml and xmlns attributes.
		/// </summary>
		/// <param name="name">The lowercase attribute name.</param>
		/// <param name="value">The attribute value.</param>
		/// <param name="namespaceUri">The namespace of the element: SVG and MathML adjust different names.</param>
		/// <returns>The attribute.</returns>
		private static HtmlAttribute CreateForeignAttribute(string name, string value, string namespaceUri)
		{
			switch (name)
			{
				case "xlink:actuate":
				case "xlink:arcrole":
				case "xlink:href":
				case "xlink:role":
				case "xlink:show":
				case "xlink:title":
				case "xlink:type":
					return new HtmlAttribute("xlink", name.Substring(6), HtmlNamespaces.XLink, value);

				case "xml:lang":
				case "xml:space":
					return new HtmlAttribute("xml", name.Substring(4), HtmlNamespaces.Xml, value);

				case "xmlns":
					return new HtmlAttribute(null, "xmlns", HtmlNamespaces.Xmlns, value);

				case "xmlns:xlink":
					return new HtmlAttribute("xmlns", "xlink", HtmlNamespaces.Xmlns, value);

				case "definitionurl":
					if (namespaceUri == HtmlNamespaces.MathML)
						return new HtmlAttribute(null, "definitionURL", null, value);
					break;
			}

			if (namespaceUri == HtmlNamespaces.Svg && SvgAttributeNames.TryGetValue(name, out var adjusted))
				return new HtmlAttribute(null, adjusted, null, value);

			return new HtmlAttribute(null, name, null, value);
		}

		#endregion
	}
}

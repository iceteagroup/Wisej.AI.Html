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

using System.Text;

namespace Wisej.AI.Html.Dom
{
	/// <summary>
	/// Serializes nodes to HTML with the fragment serialization algorithm of the HTML standard (13.3).
	/// </summary>
	internal static class HtmlSerializer
	{
		/// <summary>
		/// Serializes a node and its descendants.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="sb">The output.</param>
		public static void SerializeNode(HtmlNode node, StringBuilder sb)
		{
			switch (node)
			{
				case HtmlElement element:
					SerializeElement(element, sb);
					break;

				case HtmlText text:
					SerializeText(text, sb);
					break;

				case HtmlComment comment:
					sb.Append("<!--").Append(comment.Data).Append("-->");
					break;

				case HtmlDocumentType doctype:
					sb.Append("<!DOCTYPE ").Append(doctype.Name).Append('>');
					break;

				default:
					SerializeChildren(node, sb);
					break;
			}
		}

		/// <summary>
		/// Serializes the children of a node; for a <c>&lt;template&gt;</c> element, the children of its content.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="sb">The output.</param>
		public static void SerializeChildren(HtmlNode node, StringBuilder sb)
		{
			if (node is HtmlElement element && element.Content != null)
				node = element.Content;

			for (var child = node.FirstChild; child != null; child = child.NextSibling)
				SerializeNode(child, sb);
		}

		/// <summary>
		/// Serializes an element.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="sb">The output.</param>
		private static void SerializeElement(HtmlElement element, StringBuilder sb)
		{
			sb.Append('<').Append(element.LocalName);

			foreach (var attribute in element.Attributes)
			{
				sb.Append(' ').Append(attribute.Name).Append("=\"");
				Escape(attribute.Value, sb, inAttribute: true);
				sb.Append('"');
			}

			sb.Append('>');

			if (element.IsHtml && IsVoid(element.LocalName))
				return;

			SerializeChildren(element, sb);
			sb.Append("</").Append(element.LocalName).Append('>');
		}

		/// <summary>
		/// Serializes a text node, escaping it unless its parent is a raw text element.
		/// </summary>
		/// <param name="text">The text node.</param>
		/// <param name="sb">The output.</param>
		private static void SerializeText(HtmlText text, StringBuilder sb)
		{
			if (text.ParentNode is HtmlElement parent && parent.IsHtml)
			{
				switch (parent.LocalName)
				{
					case "style":
					case "script":
					case "xmp":
					case "iframe":
					case "noembed":
					case "noframes":
					case "plaintext":
						sb.Append(text.Data);
						return;
				}
			}

			Escape(text.Data, sb, inAttribute: false);
		}

		/// <summary>
		/// Escapes text or an attribute value.
		/// </summary>
		/// <param name="value">The text.</param>
		/// <param name="sb">The output.</param>
		/// <param name="inAttribute">Whether the text is an attribute value.</param>
		private static void Escape(string value, StringBuilder sb, bool inAttribute)
		{
			foreach (var c in value)
			{
				switch (c)
				{
					case '&':
						sb.Append("&amp;");
						break;

					case ' ':
						sb.Append("&nbsp;");
						break;

					case '"':
						sb.Append(inAttribute ? "&quot;" : "\"");
						break;

					case '<':
						sb.Append("&lt;");
						break;

					case '>':
						sb.Append("&gt;");
						break;

					default:
						sb.Append(c);
						break;
				}
			}
		}

		/// <summary>
		/// Determines whether an HTML element is void: it has no end tag and no content.
		/// </summary>
		/// <param name="name">The element name.</param>
		/// <returns><c>true</c> for the void elements.</returns>
		public static bool IsVoid(string name)
		{
			switch (name)
			{
				case "area":
				case "base":
				case "basefont":
				case "bgsound":
				case "br":
				case "col":
				case "embed":
				case "frame":
				case "hr":
				case "img":
				case "input":
				case "keygen":
				case "link":
				case "meta":
				case "param":
				case "source":
				case "track":
				case "wbr":
					return true;

				default:
					return false;
			}
		}
	}
}

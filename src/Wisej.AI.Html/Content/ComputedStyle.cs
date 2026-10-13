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
	/// The display types relevant to content extraction.
	/// </summary>
	internal enum Display
	{
		/// <summary>Not rendered.</summary>
		None,

		/// <summary>Inline content.</summary>
		Inline,

		/// <summary>A block: paragraphs and sections break the text flow.</summary>
		Block,

		/// <summary>A list item.</summary>
		ListItem,

		/// <summary>The element generates no box; its children are rendered in its place.</summary>
		Contents,
	}

	/// <summary>
	/// The handling of white space (the CSS <c>white-space</c> property).
	/// </summary>
	internal enum WhiteSpace
	{
		/// <summary>Sequences of white space collapse into one space; line breaks are spaces.</summary>
		Normal,

		/// <summary>White space and line breaks are preserved.</summary>
		Pre,

		/// <summary>Spaces collapse but line breaks are preserved.</summary>
		PreLine,
	}

	/// <summary>
	/// The computed values of the CSS properties that influence the extracted content.
	/// </summary>
	/// <remarks>
	/// Values are computed in three layers, as browsers do: the default style of HTML elements, the
	/// presentational attributes (<c>hidden</c>, <c>&lt;font face&gt;</c>, <c>type</c> of lists), and the
	/// author cascade from <see cref="Css.StyleResolver"/>. Inherited properties come from the parent.
	/// </remarks>
	internal sealed class ComputedStyle
	{
		/// <summary>The default style of the root.</summary>
		public static readonly ComputedStyle Root = new ComputedStyle { Display = Display.Block };

		/// <summary>The font families that are monospace.</summary>
		private static readonly string[] MonospaceFonts =
		{
			"monospace", "courier", "consolas", "menlo", "monaco", "lucida console", "lucida sans typewriter", "source code",
			"fira code", "fira mono", "jetbrains mono", "roboto mono", "sf mono", "dejavu sans mono", "ubuntu mono",
			"inconsolata", "cascadia", "liberation mono", "andale mono", "ibm plex mono", "noto mono", "ocr a",
		};

		/// <summary>The HTML elements displayed as blocks by default.</summary>
		private static readonly HashSet<string> BlockElements = new HashSet<string>(StringComparer.Ordinal)
		{
			"address", "article", "aside", "blockquote", "body", "center", "dd", "details", "dialog", "dir", "div", "dl", "dt",
			"fieldset", "figcaption", "figure", "footer", "form", "frameset", "h1", "h2", "h3", "h4", "h5", "h6", "header",
			"hgroup", "hr", "html", "legend", "listing", "main", "menu", "nav", "ol", "p", "plaintext", "pre", "search",
			"section", "summary", "ul", "xmp", "table", "caption", "thead", "tbody", "tfoot", "tr", "td", "th", "optgroup",
			"option", "frame", "iframe", "video", "audio", "canvas",
		};

		/// <summary>The HTML elements not rendered by default.</summary>
		private static readonly HashSet<string> HiddenElements = new HashSet<string>(StringComparer.Ordinal)
		{
			"area", "base", "basefont", "datalist", "head", "link", "meta", "noembed", "noframes", "param", "rp", "script",
			"style", "template", "title", "source", "track", "col", "colgroup", "slot",
		};

		#region Properties

		/// <summary>Gets or sets the display type.</summary>
		public Display Display { get; set; } = Display.Inline;

		/// <summary>Gets or sets whether the content is invisible (<c>visibility: hidden</c>, inherited).</summary>
		public bool Invisible { get; set; }

		/// <summary>Gets or sets whether the font size is zero, a common way to hide text (inherited).</summary>
		public bool ZeroFontSize { get; set; }

		/// <summary>Gets or sets whether the element is clipped away, as the "screen reader only" pattern does.</summary>
		public bool Clipped { get; set; }

		/// <summary>Gets or sets the character formatting (inherited).</summary>
		public InlineStyle Format { get; set; }

		/// <summary>Gets or sets the white space handling (inherited).</summary>
		public WhiteSpace WhiteSpace { get; set; }

		/// <summary>Gets or sets the list marker type, such as <c>decimal</c> or <c>lower-roman</c> (inherited).</summary>
		public string ListStyleType { get; set; } = "disc";

		/// <summary>Gets or sets whether a page break precedes the element.</summary>
		public bool BreakBefore { get; set; }

		/// <summary>Gets or sets whether a page break follows the element.</summary>
		public bool BreakAfter { get; set; }

		/// <summary>Gets whether text in this style is rendered.</summary>
		public bool IsTextVisible => !this.Invisible && !this.ZeroFontSize;

		/// <summary>
		/// Returns a copy of the style with additional formatting.
		/// </summary>
		/// <param name="format">The formatting to add.</param>
		/// <returns>The new style.</returns>
		public ComputedStyle With(InlineStyle format)
		{
			var copy = (ComputedStyle)MemberwiseClone();
			copy.Format |= format;
			return copy;
		}

		#endregion

		#region Computation

		/// <summary>
		/// Computes the style of an element.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="parent">The computed style of the parent.</param>
		/// <param name="declared">The declared values of the author cascade, or <c>null</c>.</param>
		/// <returns>The computed style.</returns>
		public static ComputedStyle Compute(HtmlElement element, ComputedStyle parent, Dictionary<string, string> declared)
		{
			var style = new ComputedStyle
			{
				Invisible = parent.Invisible,
				ZeroFontSize = parent.ZeroFontSize,
				Format = parent.Format,
				WhiteSpace = parent.WhiteSpace,
				ListStyleType = parent.ListStyleType,
			};

			if (element.IsHtml)
				ApplyDefaults(element, style);
			else
				style.Display = Display.Inline;

			if (declared != null)
				ApplyDeclared(element, style, declared);

			return style;
		}

		/// <summary>
		/// Applies the default style of HTML elements and their presentational attributes.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="style">The style to update.</param>
		private static void ApplyDefaults(HtmlElement element, ComputedStyle style)
		{
			var name = element.LocalName;

			if (HiddenElements.Contains(name))
				style.Display = Display.None;
			else if (name == "li")
				style.Display = Display.ListItem;
			else if (BlockElements.Contains(name))
				style.Display = Display.Block;
			else
				style.Display = Display.Inline;

			switch (name)
			{
				case "b":
				case "strong":
				case "h1":
				case "h2":
				case "h3":
				case "h4":
				case "h5":
				case "h6":
				case "th":
					style.Format |= InlineStyle.Bold;
					break;

				case "i":
				case "em":
				case "cite":
				case "var":
				case "dfn":
				case "address":
					style.Format |= InlineStyle.Italic;
					break;

				case "s":
				case "strike":
				case "del":
					style.Format |= InlineStyle.Strike;
					break;

				case "sup":
					style.Format = (style.Format & ~InlineStyle.Subscript) | InlineStyle.Superscript;
					break;

				case "sub":
					style.Format = (style.Format & ~InlineStyle.Superscript) | InlineStyle.Subscript;
					break;

				case "code":
				case "kbd":
				case "samp":
				case "tt":
					style.Format |= InlineStyle.Code;
					break;

				case "pre":
				case "listing":
				case "xmp":
				case "plaintext":
					style.Format |= InlineStyle.Code;
					style.WhiteSpace = WhiteSpace.Pre;
					break;

				case "textarea":
					style.WhiteSpace = WhiteSpace.Pre;
					break;

				case "font":
					if (IsMonospace(element.GetAttribute("face")))
						style.Format |= InlineStyle.Code;
					break;

				case "ol":
					style.ListStyleType = ListTypeFromAttribute(element.GetAttribute("type")) ?? "decimal";
					break;

				case "ul":
				case "menu":
				case "dir":
					style.ListStyleType = "disc";
					break;

				case "li":
					var type = ListTypeFromAttribute(element.GetAttribute("type"));
					if (type != null)
						style.ListStyleType = type;
					break;

				case "dialog":
					if (!element.HasAttribute("open"))
						style.Display = Display.None;
					break;

				case "input":
					if (String.Equals(element.GetAttribute("type"), "hidden", StringComparison.OrdinalIgnoreCase))
						style.Display = Display.None;
					break;
			}

			// the hidden attribute hides the element, except "until-found" which hides it until searched.
			var hidden = element.GetAttribute("hidden");
			if (hidden != null && !hidden.Equals("until-found", StringComparison.OrdinalIgnoreCase))
				style.Display = Display.None;
		}

		/// <summary>
		/// Applies the declared values of the author cascade.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="style">The style to update.</param>
		/// <param name="declared">The declared values.</param>
		private static void ApplyDeclared(HtmlElement element, ComputedStyle style, Dictionary<string, string> declared)
		{
			foreach (var pair in declared)
			{
				var value = pair.Value.Trim().ToLowerInvariant();

				switch (pair.Key)
				{
					case "display":
						ApplyDisplay(element, style, value);
						break;

					case "mso-hide":
						if (value == "all")
							style.Display = Display.None;
						break;

					case "content-visibility":
						if (value == "hidden")
							style.Display = Display.None;
						break;

					case "visibility":
						if (value == "hidden" || value == "collapse")
							style.Invisible = true;
						else if (value == "visible")
							style.Invisible = false;
						break;

					case "font-size":
						style.ZeroFontSize = IsZeroLength(value);
						break;

					case "font-weight":
						style.Format = SetFlag(style.Format, InlineStyle.Bold, IsBold(value, style.Format.HasFlag(InlineStyle.Bold)));
						break;

					case "font-style":
						style.Format = SetFlag(style.Format, InlineStyle.Italic, value.StartsWith("italic", StringComparison.Ordinal) || value.StartsWith("oblique", StringComparison.Ordinal));
						break;

					case "font-family":
						if (IsMonospace(value))
							style.Format |= InlineStyle.Code;
						else if (!IsSemanticCode(element) && !value.StartsWith("var(", StringComparison.Ordinal) && value != "inherit")
							style.Format &= ~InlineStyle.Code;
						break;

					case "font":
						ApplyFontShorthand(style, value);
						break;

					case "text-decoration":
					case "text-decoration-line":
						if (value.Contains("line-through"))
							style.Format |= InlineStyle.Strike;
						else if (value == "none")
							style.Format &= ~InlineStyle.Strike;
						break;

					case "vertical-align":
						if (value == "super")
							style.Format = (style.Format & ~InlineStyle.Subscript) | InlineStyle.Superscript;
						else if (value == "sub")
							style.Format = (style.Format & ~InlineStyle.Superscript) | InlineStyle.Subscript;
						else if (value == "baseline")
							style.Format &= ~(InlineStyle.Superscript | InlineStyle.Subscript);
						break;

					case "white-space":
						if (value.StartsWith("pre-line", StringComparison.Ordinal))
							style.WhiteSpace = WhiteSpace.PreLine;
						else if (value.StartsWith("pre", StringComparison.Ordinal) || value == "break-spaces")
							style.WhiteSpace = WhiteSpace.Pre;
						else
							style.WhiteSpace = WhiteSpace.Normal;
						break;

					case "page-break-before":
					case "break-before":
						style.BreakBefore = IsPageBreak(value);
						break;

					case "page-break-after":
					case "break-after":
						style.BreakAfter = IsPageBreak(value);
						break;

					case "list-style-type":
						style.ListStyleType = value;
						break;

					case "list-style":
						foreach (var token in value.Split(' '))
						{
							if (ListMarkers.IsKnownType(token))
								style.ListStyleType = token;
						}
						break;

					case "clip":
						style.Clipped = value.StartsWith("rect(0", StringComparison.Ordinal) && !value.Contains("auto");
						break;

					case "clip-path":
						style.Clipped = value.StartsWith("inset(50%", StringComparison.Ordinal) || value.StartsWith("inset(100%", StringComparison.Ordinal);
						break;
				}
			}
		}

		/// <summary>
		/// Applies the <c>display</c> property. The display of table and list structure is kept, since
		/// it determines the table grid and the list numbering.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="style">The style to update.</param>
		/// <param name="value">The lowercase value.</param>
		private static void ApplyDisplay(HtmlElement element, ComputedStyle style, string value)
		{
			if (value == "none")
			{
				style.Display = Display.None;
				return;
			}

			if (element.IsHtml)
			{
				switch (element.LocalName)
				{
					case "table":
					case "caption":
					case "thead":
					case "tbody":
					case "tfoot":
					case "tr":
					case "td":
					case "th":
					case "li":
					case "html":
					case "body":
						return;
				}
			}

			switch (value)
			{
				case "inline":
				case "inline-block":
				case "inline-flex":
				case "inline-grid":
				case "inline-table":
				case "ruby":
					style.Display = Display.Inline;
					break;

				case "list-item":
					style.Display = Display.ListItem;
					break;

				case "contents":
					style.Display = Display.Contents;
					break;

				default:
					if (value.Length > 0 && !value.StartsWith("var(", StringComparison.Ordinal))
						style.Display = Display.Block;
					break;
			}
		}

		/// <summary>
		/// Applies the font properties included in the <c>font</c> shorthand.
		/// </summary>
		/// <param name="style">The style to update.</param>
		/// <param name="value">The lowercase value.</param>
		private static void ApplyFontShorthand(ComputedStyle style, string value)
		{
			var bold = false;
			var italic = false;

			foreach (var token in value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
			{
				if (token == "italic" || token == "oblique")
					italic = true;
				else if (token == "bold" || token == "bolder" || token == "600" || token == "700" || token == "800" || token == "900")
					bold = true;
			}

			// the shorthand resets the properties it does not mention.
			style.Format = SetFlag(style.Format, InlineStyle.Bold, bold);
			style.Format = SetFlag(style.Format, InlineStyle.Italic, italic);

			if (IsMonospace(value))
				style.Format |= InlineStyle.Code;
		}

		/// <summary>
		/// Determines whether an element marks up code, whatever its font.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> for <c>code</c>, <c>kbd</c>, <c>samp</c>, <c>tt</c> and the preformatted elements.</returns>
		private static bool IsSemanticCode(HtmlElement element)
		{
			if (!element.IsHtml)
				return false;

			switch (element.LocalName)
			{
				case "code":
				case "kbd":
				case "samp":
				case "tt":
				case "pre":
				case "listing":
				case "xmp":
				case "plaintext":
					return true;

				default:
					return false;
			}
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Determines whether a <c>font-weight</c> value is bold.
		/// </summary>
		/// <param name="value">The lowercase value.</param>
		/// <param name="inherited">Whether the inherited weight is bold, for relative values.</param>
		/// <returns><c>true</c> for bold weights.</returns>
		private static bool IsBold(string value, bool inherited)
		{
			switch (value)
			{
				case "bold":
				case "bolder":
					return true;

				case "normal":
				case "lighter":
					return false;

				case "inherit":
				case "unset":
					return inherited;
			}

			return Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight) ? weight >= 600 : inherited;
		}

		/// <summary>
		/// Determines whether a font family list names a monospace font.
		/// </summary>
		/// <param name="family">The font family list. Can be <c>null</c>.</param>
		/// <returns><c>true</c> when the first recognizable family is monospace.</returns>
		internal static bool IsMonospace(string family)
		{
			if (String.IsNullOrEmpty(family))
				return false;

			family = family.ToLowerInvariant();
			foreach (var font in MonospaceFonts)
			{
				if (family.Contains(font))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Determines whether a length is zero.
		/// </summary>
		/// <param name="value">The lowercase value.</param>
		/// <returns><c>true</c> for <c>0</c>, <c>0px</c>, <c>0pt</c>, <c>0em</c>, ...</returns>
		private static bool IsZeroLength(string value)
		{
			var number = value.TrimEnd('p', 'x', 't', 'e', 'm', 'r', '%', ' ');
			return Double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var size) && size == 0;
		}

		/// <summary>
		/// Determines whether a break value forces a page break.
		/// </summary>
		/// <param name="value">The lowercase value.</param>
		/// <returns><c>true</c> for <c>always</c>, <c>page</c>, <c>left</c>, <c>right</c>, <c>recto</c> and <c>verso</c>.</returns>
		private static bool IsPageBreak(string value)
		{
			switch (value)
			{
				case "always":
				case "page":
				case "left":
				case "right":
				case "recto":
				case "verso":
				case "all":
					return true;

				default:
					return false;
			}
		}

		/// <summary>
		/// Converts the <c>type</c> attribute of an ordered list or list item.
		/// </summary>
		/// <param name="type">The attribute value, compared case sensitively. Can be <c>null</c>.</param>
		/// <returns>The list style type, or <c>null</c>.</returns>
		private static string ListTypeFromAttribute(string type)
		{
			switch (type)
			{
				case "1":
					return "decimal";
				case "a":
					return "lower-alpha";
				case "A":
					return "upper-alpha";
				case "i":
					return "lower-roman";
				case "I":
					return "upper-roman";
				case "disc":
				case "circle":
				case "square":
					return type;
				default:
					return null;
			}
		}

		/// <summary>
		/// Sets or clears a formatting flag.
		/// </summary>
		/// <param name="format">The formatting.</param>
		/// <param name="flag">The flag.</param>
		/// <param name="value">Whether the flag is set.</param>
		/// <returns>The new formatting.</returns>
		private static InlineStyle SetFlag(InlineStyle format, InlineStyle flag, bool value)
		{
			return value ? format | flag : format & ~flag;
		}

		#endregion
	}
}

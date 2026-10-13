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
using System.Linq;
using System.Text;
using Wisej.AI.Html.Dom;

namespace Wisej.AI.Html.Content
{
	/// <summary>
	/// Converts MathML (presentation markup) to LaTeX, which language models read fluently.
	/// </summary>
	/// <remarks>
	/// A TeX annotation (<c>&lt;annotation encoding="application/x-tex"&gt;</c>), written by MathJax, KaTeX
	/// and most converters, is used verbatim. Otherwise the presentation elements are translated:
	/// fractions, roots, scripts, under and over scripts, fences, tables and token elements.
	/// </remarks>
	internal static class MathMLConverter
	{
		/// <summary>The maximum nesting depth translated; deeper content is flattened to text.</summary>
		private const int MaxDepth = 64;

		/// <summary>The operators written as LaTeX commands.</summary>
		private static readonly Dictionary<string, string> Operators = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			{ "∑", "\\sum" }, { "∏", "\\prod" }, { "∫", "\\int" }, { "∬", "\\iint" }, { "∮", "\\oint" },
			{ "≤", "\\le" }, { "≥", "\\ge" }, { "≠", "\\ne" }, { "≈", "\\approx" }, { "≡", "\\equiv" },
			{ "×", "\\times" }, { "÷", "\\div" }, { "±", "\\pm" }, { "∓", "\\mp" }, { "⋅", "\\cdot" }, { "·", "\\cdot" },
			{ "∞", "\\infty" }, { "→", "\\to" }, { "←", "\\leftarrow" }, { "⇒", "\\Rightarrow" }, { "⇔", "\\Leftrightarrow" },
			{ "∈", "\\in" }, { "∉", "\\notin" }, { "⊂", "\\subset" }, { "⊆", "\\subseteq" }, { "∪", "\\cup" }, { "∩", "\\cap" },
			{ "∀", "\\forall" }, { "∃", "\\exists" }, { "∂", "\\partial" }, { "∇", "\\nabla" }, { "√", "\\surd" },
			{ "…", "\\ldots" }, { "⋯", "\\cdots" }, { "⁡", "" }, { "⁢", "" }, { "⁣", "," },
		};

		/// <summary>The Greek letters written as LaTeX commands.</summary>
		private static readonly Dictionary<char, string> Letters = new Dictionary<char, string>
		{
			{ 'α', "\\alpha" }, { 'β', "\\beta" }, { 'γ', "\\gamma" }, { 'δ', "\\delta" }, { 'ε', "\\epsilon" }, { 'ζ', "\\zeta" },
			{ 'η', "\\eta" }, { 'θ', "\\theta" }, { 'ι', "\\iota" }, { 'κ', "\\kappa" }, { 'λ', "\\lambda" }, { 'μ', "\\mu" },
			{ 'ν', "\\nu" }, { 'ξ', "\\xi" }, { 'π', "\\pi" }, { 'ρ', "\\rho" }, { 'σ', "\\sigma" }, { 'τ', "\\tau" },
			{ 'υ', "\\upsilon" }, { 'φ', "\\phi" }, { 'χ', "\\chi" }, { 'ψ', "\\psi" }, { 'ω', "\\omega" },
			{ 'Γ', "\\Gamma" }, { 'Δ', "\\Delta" }, { 'Θ', "\\Theta" }, { 'Λ', "\\Lambda" }, { 'Ξ', "\\Xi" }, { 'Π', "\\Pi" },
			{ 'Σ', "\\Sigma" }, { 'Φ', "\\Phi" }, { 'Ψ', "\\Psi" }, { 'Ω', "\\Omega" },
		};

		/// <summary>
		/// Converts a <c>&lt;math&gt;</c> element to LaTeX.
		/// </summary>
		/// <param name="math">The element.</param>
		/// <returns>The LaTeX source, without delimiters.</returns>
		public static string ToLatex(HtmlElement math)
		{
			foreach (var element in math.DescendantElements())
			{
				if (element.LocalName == "annotation")
				{
					var encoding = element.GetAttribute("encoding");
					if (encoding != null && (encoding.Equals("application/x-tex", StringComparison.OrdinalIgnoreCase) || encoding.Equals("TeX", StringComparison.OrdinalIgnoreCase)))
						return Unwrap(element.TextContent.Trim());
				}
			}

			var sb = new StringBuilder();
			Children(math, sb, 0);
			return Normalize(sb.ToString());
		}

		/// <summary>
		/// Determines whether a <c>&lt;math&gt;</c> element is a display equation.
		/// </summary>
		/// <param name="math">The element.</param>
		/// <returns><c>true</c> for <c>display="block"</c> or <c>mode="display"</c>.</returns>
		public static bool IsDisplay(HtmlElement math)
		{
			return String.Equals(math.GetAttribute("display"), "block", StringComparison.OrdinalIgnoreCase)
				|| String.Equals(math.GetAttribute("mode"), "display", StringComparison.OrdinalIgnoreCase);
		}

		#region Implementation

		/// <summary>
		/// Removes the <c>{\displaystyle ...}</c> wrapper that MediaWiki adds around every TeX annotation.
		/// </summary>
		/// <param name="latex">The TeX source.</param>
		/// <returns>The source without the wrapper.</returns>
		private static string Unwrap(string latex)
		{
			foreach (var wrapper in new[] { "{\\displaystyle ", "{\\textstyle " })
			{
				if (!latex.StartsWith(wrapper, StringComparison.Ordinal) || !latex.EndsWith("}", StringComparison.Ordinal))
					continue;

				// the opening brace must be closed by the last one: "{\displaystyle a}+{b}" is not wrapped.
				var depth = 0;
				for (var i = 0; i < latex.Length; i++)
				{
					if (latex[i] == '\\')
					{
						i++;
					}
					else if (latex[i] == '{')
					{
						depth++;
					}
					else if (latex[i] == '}' && --depth == 0)
					{
						return i == latex.Length - 1 ? latex.Substring(wrapper.Length, latex.Length - wrapper.Length - 1).Trim() : latex;
					}
				}
			}

			return latex;
		}

		/// <summary>
		/// Converts the children of an element.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="sb">The output.</param>
		/// <param name="depth">The nesting depth.</param>
		private static void Children(HtmlElement element, StringBuilder sb, int depth)
		{
			foreach (var child in element.Children)
				Element(child, sb, depth + 1);
		}

		/// <summary>
		/// Converts an element.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="sb">The output.</param>
		/// <param name="depth">The nesting depth.</param>
		private static void Element(HtmlElement element, StringBuilder sb, int depth)
		{
			if (depth > MaxDepth)
			{
				sb.Append(element.TextContent);
				return;
			}

			var args = element.Children.ToList();

			switch (element.LocalName)
			{
				case "mi":
					sb.Append(Identifier(element.TextContent.Trim()));
					break;

				case "mn":
					sb.Append(element.TextContent.Trim());
					break;

				case "mo":
					var op = element.TextContent.Trim();
					sb.Append(Operators.TryGetValue(op, out var command) ? (command.Length > 0 && Char.IsLetter(command[command.Length - 1]) ? command + " " : command) : Escape(op));
					break;

				case "mtext":
				case "ms":
					var text = element.TextContent;
					if (text.Trim().Length > 0)
						sb.Append("\\text{").Append(text.Replace("{", "\\{").Replace("}", "\\}")).Append('}');
					break;

				case "mspace":
					// a thin space; "\ " would lose its space in the normalization and escape the next character.
					sb.Append("\\,");
					break;

				case "mfrac":
					sb.Append("\\frac");
					Group(args, 0, sb, depth);
					Group(args, 1, sb, depth);
					break;

				case "msqrt":
					sb.Append("\\sqrt{");
					Children(element, sb, depth);
					sb.Append('}');
					break;

				case "mroot":
					sb.Append("\\sqrt[");
					Arg(args, 1, sb, depth);
					sb.Append(']');
					Group(args, 0, sb, depth);
					break;

				case "msup":
					Base(args, sb, depth);
					sb.Append('^');
					Group(args, 1, sb, depth);
					break;

				case "msub":
					Base(args, sb, depth);
					sb.Append('_');
					Group(args, 1, sb, depth);
					break;

				case "msubsup":
				case "munderover":
					Base(args, sb, depth);
					sb.Append('_');
					Group(args, 1, sb, depth);
					sb.Append('^');
					Group(args, 2, sb, depth);
					break;

				case "munder":
					if (IsLargeOperator(args))
					{
						Base(args, sb, depth);
						sb.Append('_');
						Group(args, 1, sb, depth);
					}
					else
					{
						sb.Append("\\underset");
						Group(args, 1, sb, depth);
						Group(args, 0, sb, depth);
					}
					break;

				case "mover":
					var accent = args.Count > 1 ? args[1].TextContent.Trim() : "";
					var accentCommand = Accent(accent);
					if (accentCommand != null)
					{
						sb.Append(accentCommand);
						Group(args, 0, sb, depth);
					}
					else if (IsLargeOperator(args))
					{
						Base(args, sb, depth);
						sb.Append('^');
						Group(args, 1, sb, depth);
					}
					else
					{
						sb.Append("\\overset");
						Group(args, 1, sb, depth);
						Group(args, 0, sb, depth);
					}
					break;

				case "mfenced":
					var open = element.GetAttribute("open") ?? "(";
					var close = element.GetAttribute("close") ?? ")";
					var separator = (element.GetAttribute("separators") ?? ",").Trim();
					sb.Append("\\left").Append(Fence(open));
					for (var i = 0; i < args.Count; i++)
					{
						if (i > 0 && separator.Length > 0)
							sb.Append(separator[Math.Min(i - 1, separator.Length - 1)]);

						Element(args[i], sb, depth + 1);
					}
					sb.Append("\\right").Append(Fence(close));
					break;

				case "mtable":
					sb.Append("\\begin{matrix}");
					var rows = args.Where(r => r.LocalName == "mtr" || r.LocalName == "mlabeledtr").ToList();
					for (var r = 0; r < rows.Count; r++)
					{
						if (r > 0)
							sb.Append(" \\\\ ");

						var cells = rows[r].Children.Where(c => c.LocalName == "mtd").ToList();
						for (var c = 0; c < cells.Count; c++)
						{
							if (c > 0)
								sb.Append(" & ");

							Children(cells[c], sb, depth + 1);
						}
					}
					sb.Append("\\end{matrix}");
					break;

				case "semantics":
					if (args.Count > 0)
						Element(args[0], sb, depth + 1);
					break;

				case "annotation":
				case "annotation-xml":
				case "mphantom":
				case "mprescripts":
				case "none":
					break;

				case "menclose":
					sb.Append("\\boxed{");
					Children(element, sb, depth);
					sb.Append('}');
					break;

				default:
					// mrow, mstyle, mpadded, merror, maction and unknown elements: their content.
					Children(element, sb, depth);
					break;
			}
		}

		/// <summary>
		/// Writes an argument in braces.
		/// </summary>
		/// <param name="args">The arguments.</param>
		/// <param name="index">The index of the argument.</param>
		/// <param name="sb">The output.</param>
		/// <param name="depth">The nesting depth.</param>
		private static void Group(List<HtmlElement> args, int index, StringBuilder sb, int depth)
		{
			sb.Append('{');
			Arg(args, index, sb, depth);
			sb.Append('}');
		}

		/// <summary>
		/// Writes an argument.
		/// </summary>
		/// <param name="args">The arguments.</param>
		/// <param name="index">The index of the argument.</param>
		/// <param name="sb">The output.</param>
		/// <param name="depth">The nesting depth.</param>
		private static void Arg(List<HtmlElement> args, int index, StringBuilder sb, int depth)
		{
			if (index < args.Count)
				Element(args[index], sb, depth + 1);
		}

		/// <summary>
		/// Writes the base of a script, in braces when it is made of more than one token.
		/// </summary>
		/// <param name="args">The arguments.</param>
		/// <param name="sb">The output.</param>
		/// <param name="depth">The nesting depth.</param>
		private static void Base(List<HtmlElement> args, StringBuilder sb, int depth)
		{
			if (args.Count == 0)
			{
				sb.Append("{}");
				return;
			}

			var inner = new StringBuilder();
			Element(args[0], inner, depth + 1);
			var text = inner.ToString().Trim();

			if (text.Length <= 1 || (text[0] == '\\' && text.IndexOf(' ') < 0 && text.IndexOf('{') < 0))
				sb.Append(text);
			else
				sb.Append('{').Append(text).Append('}');
		}

		/// <summary>
		/// Determines whether the base of an under or over script is a large operator, whose limits are scripts.
		/// </summary>
		/// <param name="args">The arguments.</param>
		/// <returns><c>true</c> for sums, products and integrals.</returns>
		private static bool IsLargeOperator(List<HtmlElement> args)
		{
			if (args.Count == 0 || args[0].LocalName != "mo")
				return false;

			var op = args[0].TextContent.Trim();
			return op == "∑" || op == "∏" || op == "∫" || op == "lim" || op == "max" || op == "min";
		}

		/// <summary>
		/// Returns the LaTeX accent command of an over script.
		/// </summary>
		/// <param name="accent">The accent character.</param>
		/// <returns>The command, or <c>null</c>.</returns>
		private static string Accent(string accent)
		{
			switch (accent)
			{
				case "^":
				case "ˆ":
					return "\\hat";
				case "~":
				case "˜":
					return "\\tilde";
				case "¯":
				case "‾":
				case "_":
					return "\\overline";
				case "→":
					return "\\vec";
				case ".":
				case "˙":
					return "\\dot";
				default:
					return null;
			}
		}

		/// <summary>
		/// Converts an identifier: Greek letters become commands, multi-letter names become upright text.
		/// </summary>
		/// <param name="text">The identifier.</param>
		/// <returns>The LaTeX.</returns>
		private static string Identifier(string text)
		{
			if (text.Length == 1 && Letters.TryGetValue(text[0], out var letter))
				return letter + " ";

			if (text.Length > 1 && text.All(Char.IsLetter))
			{
				switch (text)
				{
					case "sin":
					case "cos":
					case "tan":
					case "log":
					case "ln":
					case "exp":
					case "lim":
					case "max":
					case "min":
					case "det":
						return "\\" + text + " ";

					default:
						return "\\mathrm{" + text + "}";
				}
			}

			return Escape(text);
		}

		/// <summary>
		/// Returns the LaTeX form of a fence character.
		/// </summary>
		/// <param name="fence">The fence.</param>
		/// <returns>The LaTeX delimiter.</returns>
		private static string Fence(string fence)
		{
			switch (fence)
			{
				case "{":
					return "\\{";
				case "}":
					return "\\}";
				case "":
					return ".";
				case "|":
				case "(":
				case ")":
				case "[":
				case "]":
					return fence;
				case "⟨":
					return "\\langle";
				case "⟩":
					return "\\rangle";
				default:
					return fence;
			}
		}

		/// <summary>
		/// Escapes the characters that are special in LaTeX.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>The escaped text.</returns>
		private static string Escape(string text)
		{
			var sb = new StringBuilder(text.Length);
			foreach (var c in text)
			{
				if (c == '{' || c == '}' || c == '%' || c == '#' || c == '&' || c == '$' || c == '_')
					sb.Append('\\');

				if (Letters.TryGetValue(c, out var letter))
					sb.Append(letter).Append(' ');
				else
					sb.Append(c);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Collapses the white space of the generated LaTeX.
		/// </summary>
		/// <param name="latex">The LaTeX.</param>
		/// <returns>The normalized LaTeX.</returns>
		private static string Normalize(string latex)
		{
			var sb = new StringBuilder(latex.Length);
			var space = false;

			foreach (var c in latex)
			{
				if (Char.IsWhiteSpace(c))
				{
					space = sb.Length > 0;
					continue;
				}

				if (space && (Char.IsLetterOrDigit(c) || c == '\\') && sb.Length > 0 && (Char.IsLetter(sb[sb.Length - 1]) || sb[sb.Length - 1] == '\\'))
					sb.Append(' ');

				space = false;
				sb.Append(c);
			}

			return sb.ToString();
		}

		#endregion
	}
}

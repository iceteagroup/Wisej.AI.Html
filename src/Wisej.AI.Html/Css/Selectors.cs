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

namespace Wisej.AI.Html.Css
{
	/// <summary>
	/// A list of complex selectors separated by commas (Selectors Level 4).
	/// </summary>
	internal sealed class SelectorList
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SelectorList"/> class.
		/// </summary>
		/// <param name="selectors">The complex selectors.</param>
		public SelectorList(List<ComplexSelector> selectors)
		{
			this.Selectors = selectors;
		}

		/// <summary>
		/// Gets the complex selectors.
		/// </summary>
		public List<ComplexSelector> Selectors { get; }

		/// <summary>
		/// Determines whether an element matches at least one of the selectors.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> on a match.</returns>
		public bool Matches(HtmlElement element)
		{
			foreach (var selector in this.Selectors)
			{
				if (selector.Matches(element))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Returns the highest specificity of the selectors that match an element.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns>The specificity, or -1 when no selector matches.</returns>
		public int MatchSpecificity(HtmlElement element)
		{
			var result = -1;
			foreach (var selector in this.Selectors)
			{
				if (selector.Specificity > result && selector.Matches(element))
					result = selector.Specificity;
			}

			return result;
		}
	}

	/// <summary>
	/// A sequence of compound selectors joined by combinators, such as <c>div.note &gt; p</c>.
	/// </summary>
	internal sealed class ComplexSelector
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ComplexSelector"/> class.
		/// </summary>
		/// <param name="compounds">The compound selectors, from left to right.</param>
		/// <param name="combinators">The combinators between the compounds: <c>' '</c>, <c>'&gt;'</c>, <c>'+'</c> or <c>'~'</c>.</param>
		public ComplexSelector(List<CompoundSelector> compounds, List<char> combinators)
		{
			this.Compounds = compounds;
			this.Combinators = combinators;

			foreach (var compound in compounds)
				this.Specificity += compound.Specificity;
		}

		/// <summary>
		/// Gets the compound selectors, from left to right.
		/// </summary>
		public List<CompoundSelector> Compounds { get; }

		/// <summary>
		/// Gets the combinators: <c>Combinators[i]</c> joins <c>Compounds[i]</c> and <c>Compounds[i + 1]</c>.
		/// </summary>
		public List<char> Combinators { get; }

		/// <summary>
		/// Gets the specificity, packed as <c>ids * 65536 + classes * 256 + types</c>.
		/// </summary>
		public int Specificity { get; }

		/// <summary>
		/// Gets the rightmost compound, which must match the element itself.
		/// </summary>
		public CompoundSelector Subject => this.Compounds[this.Compounds.Count - 1];

		/// <summary>
		/// Determines whether an element matches the selector.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> on a match.</returns>
		public bool Matches(HtmlElement element)
		{
			HashSet<(HtmlElement, int)> failed = null;
			return MatchFrom(element, this.Compounds.Count - 1, ref failed);
		}

		/// <summary>
		/// Matches the compounds from an index leftwards, starting with an element.
		/// </summary>
		/// <param name="element">The element that must match the compound at <paramref name="index"/>.</param>
		/// <param name="index">The index of the compound.</param>
		/// <param name="failed">
		/// The (element, compound) pairs already known not to match. The result only depends on the pair,
		/// so remembering the failures keeps "div div div div" linear instead of exponential on deep trees.
		/// </param>
		/// <returns><c>true</c> on a match.</returns>
		private bool MatchFrom(HtmlElement element, int index, ref HashSet<(HtmlElement, int)> failed)
		{
			if (failed != null && failed.Contains((element, index)))
				return false;

			if (!this.Compounds[index].Matches(element))
				return false;

			if (index == 0)
				return true;

			var result = false;

			switch (this.Combinators[index - 1])
			{
				case '>':
					var parent = element.ParentElement;
					result = parent != null && MatchFrom(parent, index - 1, ref failed);
					break;

				case '+':
					var previous = PreviousElement(element);
					result = previous != null && MatchFrom(previous, index - 1, ref failed);
					break;

				case '~':
					for (var sibling = PreviousElement(element); sibling != null && !result; sibling = PreviousElement(sibling))
						result = MatchFrom(sibling, index - 1, ref failed);
					break;

				default:
					for (var ancestor = element.ParentElement; ancestor != null && !result; ancestor = ancestor.ParentElement)
						result = MatchFrom(ancestor, index - 1, ref failed);
					break;
			}

			// the first compound is cheap to test and the last is tested once: only the others are remembered.
			if (!result && index < this.Compounds.Count - 1)
			{
				failed = failed ?? new HashSet<(HtmlElement, int)>();
				failed.Add((element, index));
			}

			return result;
		}

		/// <summary>
		/// Returns the previous sibling element.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns>The previous element sibling, or <c>null</c>.</returns>
		internal static HtmlElement PreviousElement(HtmlElement element)
		{
			for (var node = element.PreviousSibling; node != null; node = node.PreviousSibling)
			{
				if (node is HtmlElement previous)
					return previous;
			}

			return null;
		}

		/// <summary>
		/// Returns the next sibling element.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns>The next element sibling, or <c>null</c>.</returns>
		internal static HtmlElement NextElement(HtmlElement element)
		{
			for (var node = element.NextSibling; node != null; node = node.NextSibling)
			{
				if (node is HtmlElement next)
					return next;
			}

			return null;
		}
	}

	/// <summary>
	/// A type selector with simple selectors that all apply to the same element, such as <c>a.external[href]</c>.
	/// </summary>
	internal sealed class CompoundSelector
	{
		/// <summary>
		/// Gets or sets the lowercase type selector, or <c>null</c> for any element.
		/// </summary>
		public string Tag { get; set; }

		/// <summary>
		/// Gets the simple selectors other than the type selector.
		/// </summary>
		public List<SimpleSelector> Conditions { get; } = new List<SimpleSelector>();

		/// <summary>
		/// Gets the specificity of the compound.
		/// </summary>
		public int Specificity
		{
			get
			{
				var result = this.Tag != null ? 1 : 0;
				foreach (var condition in this.Conditions)
					result += condition.Specificity;

				return result;
			}
		}

		/// <summary>
		/// Determines whether an element matches the compound.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> on a match.</returns>
		public bool Matches(HtmlElement element)
		{
			if (this.Tag != null)
			{
				// type selectors are case insensitive for HTML elements only.
				var matches = element.IsHtml
					? element.LocalName == this.Tag
					: String.Equals(element.LocalName, this.Tag, StringComparison.OrdinalIgnoreCase);

				if (!matches)
					return false;
			}

			foreach (var condition in this.Conditions)
			{
				if (!condition.Matches(element))
					return false;
			}

			return true;
		}
	}

	/// <summary>
	/// A simple selector other than a type selector.
	/// </summary>
	internal abstract class SimpleSelector
	{
		/// <summary>
		/// Gets the specificity of the selector.
		/// </summary>
		public abstract int Specificity { get; }

		/// <summary>
		/// Determines whether an element matches the selector.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> on a match.</returns>
		public abstract bool Matches(HtmlElement element);
	}

	/// <summary>
	/// An id selector: <c>#name</c>.
	/// </summary>
	internal sealed class IdSelector : SimpleSelector
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IdSelector"/> class.
		/// </summary>
		/// <param name="id">The identifier.</param>
		public IdSelector(string id)
		{
			this.Id = id;
		}

		/// <summary>Gets the identifier.</summary>
		public string Id { get; }

		/// <inheritdoc/>
		public override int Specificity => 0x10000;

		/// <inheritdoc/>
		public override bool Matches(HtmlElement element) => element.GetAttribute("id") == this.Id;
	}

	/// <summary>
	/// A class selector: <c>.name</c>.
	/// </summary>
	internal sealed class ClassSelector : SimpleSelector
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ClassSelector"/> class.
		/// </summary>
		/// <param name="className">The class name.</param>
		public ClassSelector(string className)
		{
			this.ClassName = className;
		}

		/// <summary>Gets the class name.</summary>
		public string ClassName { get; }

		/// <inheritdoc/>
		public override int Specificity => 0x100;

		/// <inheritdoc/>
		public override bool Matches(HtmlElement element) => element.HasClass(this.ClassName);
	}

	/// <summary>
	/// An attribute selector: <c>[name]</c>, <c>[name=value]</c>, <c>[name~=value]</c>, <c>[name|=value]</c>,
	/// <c>[name^=value]</c>, <c>[name$=value]</c> or <c>[name*=value]</c>, optionally with the <c>i</c> flag.
	/// </summary>
	internal sealed class AttributeSelector : SimpleSelector
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AttributeSelector"/> class.
		/// </summary>
		/// <param name="name">The attribute name.</param>
		/// <param name="op">The operator, or <c>null</c> for a presence test.</param>
		/// <param name="value">The value to compare.</param>
		/// <param name="ignoreCase">Whether the value is compared case insensitively.</param>
		public AttributeSelector(string name, string op, string value, bool ignoreCase)
		{
			this.Name = name;
			this.Operator = op;
			this.Value = value;
			this.IgnoreCase = ignoreCase;
		}

		/// <summary>Gets the attribute name.</summary>
		public string Name { get; }

		/// <summary>Gets the operator, or <c>null</c> for a presence test.</summary>
		public string Operator { get; }

		/// <summary>Gets the value to compare.</summary>
		public string Value { get; }

		/// <summary>Gets whether the comparison ignores case.</summary>
		public bool IgnoreCase { get; }

		/// <inheritdoc/>
		public override int Specificity => 0x100;

		/// <inheritdoc/>
		public override bool Matches(HtmlElement element)
		{
			var actual = element.GetAttribute(this.Name);
			if (actual == null)
				return false;

			if (this.Operator == null)
				return true;

			var comparison = this.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			var value = this.Value;

			switch (this.Operator)
			{
				case "=":
					return String.Equals(actual, value, comparison);

				case "~=":
					if (value.Length == 0 || value.IndexOfAny(HtmlElement.Whitespace) >= 0)
						return false;

					foreach (var token in actual.Split(HtmlElement.Whitespace, StringSplitOptions.RemoveEmptyEntries))
					{
						if (String.Equals(token, value, comparison))
							return true;
					}

					return false;

				case "|=":
					return String.Equals(actual, value, comparison) || actual.StartsWith(value + "-", comparison);

				case "^=":
					return value.Length > 0 && actual.StartsWith(value, comparison);

				case "$=":
					return value.Length > 0 && actual.EndsWith(value, comparison);

				case "*=":
					return value.Length > 0 && actual.IndexOf(value, comparison) >= 0;

				default:
					return false;
			}
		}
	}

	/// <summary>
	/// A structural or state pseudo-class without a selector argument, such as <c>:first-child</c> or <c>:nth-of-type(2n+1)</c>.
	/// </summary>
	internal sealed class PseudoClassSelector : SimpleSelector
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PseudoClassSelector"/> class.
		/// </summary>
		/// <param name="name">The lowercase name of the pseudo-class.</param>
		/// <param name="a">The step of an <c>an+b</c> argument.</param>
		/// <param name="b">The offset of an <c>an+b</c> argument.</param>
		public PseudoClassSelector(string name, int a = 0, int b = 0)
		{
			this.Name = name;
			this.A = a;
			this.B = b;
		}

		/// <summary>Gets the name of the pseudo-class.</summary>
		public string Name { get; }

		/// <summary>Gets the step of the <c>an+b</c> argument.</summary>
		public int A { get; }

		/// <summary>Gets the offset of the <c>an+b</c> argument.</summary>
		public int B { get; }

		/// <inheritdoc/>
		public override int Specificity => 0x100;

		/// <summary>
		/// Determines whether a pseudo-class name is supported.
		/// </summary>
		/// <param name="name">The lowercase name.</param>
		/// <returns><c>true</c> when supported.</returns>
		public static bool IsSupported(string name)
		{
			switch (name)
			{
				case "first-child":
				case "last-child":
				case "only-child":
				case "first-of-type":
				case "last-of-type":
				case "only-of-type":
				case "nth-child":
				case "nth-last-child":
				case "nth-of-type":
				case "nth-last-of-type":
				case "root":
				case "empty":
				case "link":
				case "any-link":
				case "checked":
				case "disabled":
				case "enabled":
				case "required":
				case "optional":
				case "defined":
				case "scope":
					return true;

				default:
					return false;
			}
		}

		/// <inheritdoc/>
		public override bool Matches(HtmlElement element)
		{
			switch (this.Name)
			{
				case "first-child":
					return ComplexSelector.PreviousElement(element) == null && element.ParentNode is HtmlElement;

				case "last-child":
					return ComplexSelector.NextElement(element) == null && element.ParentNode is HtmlElement;

				case "only-child":
					return ComplexSelector.PreviousElement(element) == null && ComplexSelector.NextElement(element) == null && element.ParentNode is HtmlElement;

				case "first-of-type":
					return Position(element, ofType: true, fromEnd: false) == 1;

				case "last-of-type":
					return Position(element, ofType: true, fromEnd: true) == 1;

				case "only-of-type":
					return Position(element, ofType: true, fromEnd: false) == 1 && Position(element, ofType: true, fromEnd: true) == 1;

				case "nth-child":
					return MatchesNth(Position(element, ofType: false, fromEnd: false));

				case "nth-last-child":
					return MatchesNth(Position(element, ofType: false, fromEnd: true));

				case "nth-of-type":
					return MatchesNth(Position(element, ofType: true, fromEnd: false));

				case "nth-last-of-type":
					return MatchesNth(Position(element, ofType: true, fromEnd: true));

				case "root":
				case "scope":
					return element.ParentNode is HtmlDocument;

				case "empty":
					for (var child = element.FirstChild; child != null; child = child.NextSibling)
					{
						if (child is HtmlElement || (child is HtmlText text && text.Data.Length > 0))
							return false;
					}

					return true;

				case "link":
				case "any-link":
					return (element.Is("a") || element.Is("area")) && element.HasAttribute("href");

				case "checked":
					return ((element.Is("input") && element.HasAttribute("checked")) || (element.Is("option") && element.HasAttribute("selected")));

				case "disabled":
					return IsFormControl(element) && element.HasAttribute("disabled");

				case "enabled":
					return IsFormControl(element) && !element.HasAttribute("disabled");

				case "required":
					return IsFormControl(element) && element.HasAttribute("required");

				case "optional":
					return IsFormControl(element) && !element.HasAttribute("required");

				case "defined":
					return true;

				default:
					return false;
			}
		}

		/// <summary>
		/// Determines whether a 1-based position satisfies the <c>an+b</c> argument.
		/// </summary>
		/// <param name="position">The position.</param>
		/// <returns><c>true</c> when some n &gt;= 0 gives a*n+b = position.</returns>
		private bool MatchesNth(int position)
		{
			// long arithmetic: "-n-2147483647" must not overflow.
			if (this.A == 0)
				return position == this.B;

			var n = (long)position - this.B;
			return n % this.A == 0 && n / this.A >= 0;
		}

		/// <summary>
		/// Returns the 1-based position of an element among its sibling elements.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="ofType">Whether only the siblings with the same name are counted.</param>
		/// <param name="fromEnd">Whether the position is counted from the last sibling.</param>
		/// <returns>The position.</returns>
		private static int Position(HtmlElement element, bool ofType, bool fromEnd)
		{
			// the parent caches the positions, so matching every row of a large table stays linear.
			return element.ParentNode?.GetElementPosition(element, ofType, fromEnd) ?? 1;
		}

		/// <summary>
		/// Determines whether an element is a form control that can be disabled or required.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns><c>true</c> for form controls.</returns>
		private static bool IsFormControl(HtmlElement element)
		{
			return element.Is("input") || element.Is("select") || element.Is("textarea") || element.Is("button")
				|| element.Is("option") || element.Is("optgroup") || element.Is("fieldset");
		}
	}

	/// <summary>
	/// A pseudo-class with a selector list argument: <c>:not()</c>, <c>:is()</c>, <c>:where()</c>, <c>:matches()</c> and <c>:has()</c>.
	/// </summary>
	internal sealed class LogicalSelector : SimpleSelector
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LogicalSelector"/> class.
		/// </summary>
		/// <param name="name">The lowercase name of the pseudo-class.</param>
		/// <param name="argument">The selector list argument.</param>
		/// <param name="relativeCombinator">For <c>:has()</c>, the leading combinator of the relative selectors.</param>
		public LogicalSelector(string name, SelectorList argument, char relativeCombinator = ' ')
		{
			this.Name = name;
			this.Argument = argument;
			this.RelativeCombinator = relativeCombinator;
		}

		/// <summary>Gets the name of the pseudo-class.</summary>
		public string Name { get; }

		/// <summary>Gets the selector list argument.</summary>
		public SelectorList Argument { get; }

		/// <summary>Gets the leading combinator of the relative selectors of <c>:has()</c>.</summary>
		public char RelativeCombinator { get; }

		/// <inheritdoc/>
		public override int Specificity
		{
			get
			{
				// :where() has no specificity; the others take the most specific argument.
				if (this.Name == "where")
					return 0;

				var result = 0;
				foreach (var selector in this.Argument.Selectors)
					result = Math.Max(result, selector.Specificity);

				return result;
			}
		}

		/// <inheritdoc/>
		public override bool Matches(HtmlElement element)
		{
			switch (this.Name)
			{
				case "not":
					return !this.Argument.Matches(element);

				case "has":
					return MatchesHas(element);

				default:
					return this.Argument.Matches(element);
			}
		}

		/// <summary>
		/// Determines whether an element has a relative element that matches the argument of <c>:has()</c>.
		/// </summary>
		/// <param name="element">The anchor element.</param>
		/// <returns><c>true</c> on a match.</returns>
		private bool MatchesHas(HtmlElement element)
		{
			switch (this.RelativeCombinator)
			{
				case '>':
					foreach (var child in element.Children)
					{
						if (this.Argument.Matches(child))
							return true;
					}

					return false;

				case '+':
					var next = ComplexSelector.NextElement(element);
					return next != null && this.Argument.Matches(next);

				case '~':
					for (var sibling = ComplexSelector.NextElement(element); sibling != null; sibling = ComplexSelector.NextElement(sibling))
					{
						if (this.Argument.Matches(sibling))
							return true;
					}

					return false;

				default:
					foreach (var descendant in element.DescendantElements())
					{
						if (this.Argument.Matches(descendant))
							return true;
					}

					return false;
			}
		}
	}
}

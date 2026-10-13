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
	/// Applies the cascade of the author style sheets and <c>style</c> attributes of a document, for the
	/// properties relevant to content extraction (see <see cref="CssParser.RelevantProperties"/>).
	/// </summary>
	/// <remarks>
	/// Rules are indexed by the id, class or type of their rightmost compound selector, as browsers do,
	/// so an element is only tested against the rules that can match it.
	/// </remarks>
	internal sealed class StyleResolver
	{
		// the rules, indexed by the key of their subject compound.
		private readonly Dictionary<string, List<RuleEntry>> _byId = new Dictionary<string, List<RuleEntry>>(StringComparer.Ordinal);
		private readonly Dictionary<string, List<RuleEntry>> _byClass = new Dictionary<string, List<RuleEntry>>(StringComparer.Ordinal);
		private readonly Dictionary<string, List<RuleEntry>> _byTag = new Dictionary<string, List<RuleEntry>>(StringComparer.Ordinal);
		private readonly List<RuleEntry> _universal = new List<RuleEntry>();

		/// <summary>
		/// A complex selector of a rule, the unit stored in the index.
		/// </summary>
		private sealed class RuleEntry
		{
			/// <summary>The selector.</summary>
			public ComplexSelector Selector;

			/// <summary>The rule.</summary>
			public CssRule Rule;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StyleResolver"/> class.
		/// </summary>
		/// <param name="document">The document whose <c>&lt;style&gt;</c> elements are applied.</param>
		/// <param name="loadStyleSheet">Returns the text of a linked style sheet from its <c>href</c>, or <c>null</c>. Can be <c>null</c>.</param>
		public StyleResolver(HtmlDocument document, Func<string, string> loadStyleSheet)
		{
			var rules = new List<CssRule>();

			foreach (var element in document.DescendantElements())
			{
				if (element.LocalName == "style")
				{
					var type = element.GetAttribute("type");
					if (type == null || type.Length == 0 || type.Equals("text/css", StringComparison.OrdinalIgnoreCase))
						CssParser.ParseStyleSheet(element.TextContent, rules);
				}
				else if (element.Is("link") && loadStyleSheet != null && IsStyleSheetLink(element))
				{
					CssParser.ParseStyleSheet(loadStyleSheet(element.GetAttribute("href")), rules);
				}
			}

			foreach (var rule in rules)
			{
				foreach (var selector in rule.Selectors.Selectors)
					Index(new RuleEntry { Selector = selector, Rule = rule });
			}

			this.RuleCount = rules.Count;
		}

		/// <summary>
		/// Gets the number of relevant rules found in the style sheets.
		/// </summary>
		public int RuleCount { get; }

		/// <summary>
		/// Returns the declared values of the relevant properties of an element, after the cascade.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns>The winning value of each property, or <c>null</c> when nothing applies.</returns>
		public Dictionary<string, string> Resolve(HtmlElement element)
		{
			Dictionary<string, long> priorities = null;
			Dictionary<string, string> values = null;

			void Apply(List<CssDeclaration> declarations, long basePriority)
			{
				foreach (var declaration in declarations)
				{
					var priority = basePriority | (declaration.Important ? 1L << 62 : 0);

					values = values ?? new Dictionary<string, string>(StringComparer.Ordinal);
					priorities = priorities ?? new Dictionary<string, long>(StringComparer.Ordinal);

					if (!priorities.TryGetValue(declaration.Name, out var current) || priority >= current)
					{
						priorities[declaration.Name] = priority;
						values[declaration.Name] = declaration.Value;
					}
				}
			}

			if (this.RuleCount > 0)
			{
				var id = element.GetAttribute("id");
				if (!String.IsNullOrEmpty(id) && _byId.TryGetValue(id, out var byId))
					ApplyEntries(byId, element, Apply);

				foreach (var className in element.ClassList)
				{
					if (_byClass.TryGetValue(className, out var byClass))
						ApplyEntries(byClass, element, Apply);
				}

				if (_byTag.TryGetValue(element.LocalName.ToLowerInvariant(), out var byTag))
					ApplyEntries(byTag, element, Apply);

				ApplyEntries(_universal, element, Apply);
			}

			// the style attribute wins over the style sheets, except their important declarations.
			var style = element.GetAttribute("style");
			if (!String.IsNullOrEmpty(style))
				Apply(CssParser.ParseDeclarations(style), 1L << 61);

			return values;
		}

		/// <summary>
		/// Applies the indexed selectors that match an element.
		/// </summary>
		/// <param name="entries">The candidate selectors.</param>
		/// <param name="element">The element.</param>
		/// <param name="apply">Applies the declarations of a matching rule with a priority.</param>
		private static void ApplyEntries(List<RuleEntry> entries, HtmlElement element, Action<List<CssDeclaration>, long> apply)
		{
			foreach (var entry in entries)
			{
				if (entry.Selector.Matches(element))
					apply(entry.Rule.Declarations, ((long)entry.Selector.Specificity << 32) | (uint)entry.Rule.Order);
			}
		}

		/// <summary>
		/// Adds a selector to the index.
		/// </summary>
		/// <param name="entry">The selector and its rule.</param>
		private void Index(RuleEntry entry)
		{
			var subject = entry.Selector.Subject;

			foreach (var condition in subject.Conditions)
			{
				if (condition is IdSelector id)
				{
					Add(_byId, id.Id, entry);
					return;
				}
			}

			foreach (var condition in subject.Conditions)
			{
				if (condition is ClassSelector className)
				{
					Add(_byClass, className.ClassName, entry);
					return;
				}
			}

			if (subject.Tag != null)
				Add(_byTag, subject.Tag, entry);
			else
				_universal.Add(entry);
		}

		/// <summary>
		/// Adds an entry to a bucket of the index.
		/// </summary>
		/// <param name="map">The index.</param>
		/// <param name="key">The key.</param>
		/// <param name="entry">The entry.</param>
		private static void Add(Dictionary<string, List<RuleEntry>> map, string key, RuleEntry entry)
		{
			if (!map.TryGetValue(key, out var list))
				map[key] = list = new List<RuleEntry>();

			list.Add(entry);
		}

		/// <summary>
		/// Determines whether a <c>&lt;link&gt;</c> element references a style sheet.
		/// </summary>
		/// <param name="link">The link element.</param>
		/// <returns><c>true</c> for <c>rel="stylesheet"</c> links that are not alternate style sheets.</returns>
		private static bool IsStyleSheetLink(HtmlElement link)
		{
			var rel = link.GetAttribute("rel");
			if (rel == null || String.IsNullOrEmpty(link.GetAttribute("href")))
				return false;

			var tokens = rel.ToLowerInvariant().Split(HtmlElement.Whitespace, StringSplitOptions.RemoveEmptyEntries);
			return Array.IndexOf(tokens, "stylesheet") >= 0 && Array.IndexOf(tokens, "alternate") < 0;
		}
	}
}

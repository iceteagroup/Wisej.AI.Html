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

using System.Collections.Generic;
using Wisej.AI.Html.Dom;

namespace Wisej.AI.Html.Content
{
	/// <summary>
	/// The rules of the content written by Microsoft Office: lists made of paragraphs.
	/// </summary>
	internal sealed partial class ContentBuilder
	{
		/// <summary>
		/// Adds a run of consecutive Office list paragraphs as a nested list.
		/// </summary>
		/// <param name="first">The first list paragraph.</param>
		/// <param name="container">The flow that receives the content.</param>
		/// <param name="parentStyle">The computed style of the parent of the paragraphs.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		/// <returns>The last node of the run, after which the walk continues.</returns>
		private HtmlNode OfficeListParagraphs(HtmlElement first, BlockContainer container, ComputedStyle parentStyle, string link)
		{
			container.FlushParagraph();

			// the open lists, by level, with the last item of each.
			var stack = new List<(int Level, ListBlock List, ListItem Item)>();
			HtmlNode last = first;

			for (var node = (HtmlNode)first; node != null; node = node.NextSibling)
			{
				if ((node is HtmlText text && text.Data.Trim().Length == 0) || node is HtmlComment)
					continue;

				if (!(node is HtmlElement paragraph) || !OfficeList.IsListParagraph(paragraph))
					break;

				last = paragraph;

				var style = ComputeStyle(paragraph, parentStyle);
				if (!IsRendered(paragraph, style))
					continue;

				var marker = OfficeList.FindMarker(paragraph, out var markerNodes);
				var level = OfficeList.GetLevel(paragraph);

				// close the deeper lists, open a nested list for a deeper level.
				while (stack.Count > 0 && stack[stack.Count - 1].Level > level)
					stack.RemoveAt(stack.Count - 1);

				if (stack.Count == 0 || stack[stack.Count - 1].Level < level)
				{
					var list = new ListBlock();

					if (stack.Count == 0)
					{
						container.Add(list);
					}
					else
					{
						var parent = stack[stack.Count - 1];
						if (parent.Item == null)
						{
							parent.Item = new ListItem();
							parent.List.Items.Add(parent.Item);
							stack[stack.Count - 1] = parent;
						}

						parent.Item.Blocks.Add(list);
					}

					stack.Add((level, list, null));
				}

				var item = new ListItem { Marker = marker == null ? "-" : OfficeList.ToListMarker(marker) };
				var current = stack[stack.Count - 1];
				current.List.Ordered |= item.Marker != "-";
				current.List.Items.Add(item);
				current.Item = item;
				stack[stack.Count - 1] = current;

				foreach (var markerNode in markerNodes)
					_skippedNodes.Add(markerNode);

				var inner = new BlockContainer(item.Blocks, allowPageBreaks: false);

				_depth++;
				WalkChildren(paragraph, inner, style, link);
				_depth--;

				inner.FlushParagraph();
			}

			return last;
		}
	}
}

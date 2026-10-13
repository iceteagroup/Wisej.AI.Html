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

namespace Wisej.AI.Html.Content
{
	/// <summary>
	/// Receives the blocks of a flow of content (the document, a list item, a table cell, a quotation)
	/// and the inline content of the paragraph being built.
	/// </summary>
	internal sealed class BlockContainer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BlockContainer"/> class.
		/// </summary>
		/// <param name="blocks">The list that receives the blocks.</param>
		/// <param name="allowPageBreaks">Whether page breaks are recorded in this flow.</param>
		public BlockContainer(List<Block> blocks, bool allowPageBreaks)
		{
			this.Blocks = blocks;
			this.AllowPageBreaks = allowPageBreaks;
			this.Inline = new InlineBuilder();
		}

		/// <summary>
		/// Gets the blocks.
		/// </summary>
		public List<Block> Blocks { get; }

		/// <summary>
		/// Gets the inline content of the paragraph being built.
		/// </summary>
		public InlineBuilder Inline { get; private set; }

		/// <summary>
		/// Gets whether page breaks are recorded: only the top-level flow is split into pages.
		/// </summary>
		public bool AllowPageBreaks { get; }

		/// <summary>
		/// Completes the paragraph being built, adding it when it has content.
		/// </summary>
		public void FlushParagraph()
		{
			if (this.Inline.HasContent)
			{
				var paragraph = new ParagraphBlock();
				paragraph.Inlines.AddRange(this.Inline.Complete());
				this.Blocks.Add(paragraph);
			}

			this.Inline = new InlineBuilder();
		}

		/// <summary>
		/// Adds a block after the paragraph being built.
		/// </summary>
		/// <param name="block">The block.</param>
		public void Add(Block block)
		{
			FlushParagraph();
			this.Blocks.Add(block);
		}

		/// <summary>
		/// Records a page break, when the flow is paginated and the break is not redundant.
		/// </summary>
		public void AddPageBreak()
		{
			if (!this.AllowPageBreaks)
				return;

			FlushParagraph();

			if (this.Blocks.Count > 0 && !(this.Blocks[this.Blocks.Count - 1] is PageBreakBlock))
				this.Blocks.Add(new PageBreakBlock());
		}
	}
}

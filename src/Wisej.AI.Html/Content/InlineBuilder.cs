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
using System.Text;

namespace Wisej.AI.Html.Content
{
	/// <summary>
	/// Collects the inline content of a paragraph, collapsing white space as CSS does (CSS Text 3, 4.1).
	/// </summary>
	/// <remarks>
	/// <para>
	/// White space collapses across element boundaries: "a &lt;b&gt; b&lt;/b&gt;" has one space, and the
	/// spaces at the start and end of lines disappear. A pending space is emitted only when content follows
	/// it, with the formatting of the text that precedes it, so that emphasis markers never enclose
	/// spaces ("**bold** text" rather than "**bold **text").
	/// </para>
	/// <para>
	/// Soft hyphens, zero width spaces and byte order marks are removed. Non-breaking spaces are treated
	/// as regular spaces, which is what text extraction needs.
	/// </para>
	/// </remarks>
	internal sealed class InlineBuilder
	{
		// the completed inlines.
		private readonly List<Inline> _inlines = new List<Inline>();

		// the run being built and its attributes.
		private readonly StringBuilder _run = new StringBuilder();
		private InlineStyle _runStyle;
		private string _runLink;

		// the attributes of the last content emitted, used for pending spaces.
		private InlineStyle _lastStyle;
		private string _lastLink;

		// white space state.
		private bool _pendingSpace;
		private bool _lineStart = true;

		/// <summary>
		/// Gets whether the builder contains visible content (text other than white space, images or equations).
		/// </summary>
		public bool HasContent { get; private set; }

		/// <summary>
		/// Appends text.
		/// </summary>
		/// <param name="text">The text, as written in the document.</param>
		/// <param name="style">The formatting.</param>
		/// <param name="link">The target of the enclosing link, or <c>null</c>.</param>
		/// <param name="whiteSpace">The white space handling.</param>
		public void AddText(string text, InlineStyle style, string link, WhiteSpace whiteSpace)
		{
			foreach (var c in text)
			{
				switch (c)
				{
					case '­':
					case '​':
					case '﻿':
						continue;

					case '\r':
						if (whiteSpace != WhiteSpace.Normal)
							continue;
						break;
				}

				if (whiteSpace == WhiteSpace.Pre)
				{
					if (c == '\n')
					{
						AddBreak();
					}
					else
					{
						EmitPendingSpace();
						Append(c, style, link, isContent: !IsWhitespace(c));
					}

					continue;
				}

				if (c == '\n' && whiteSpace == WhiteSpace.PreLine)
				{
					AddBreak();
					continue;
				}

				if (IsWhitespace(c) || c == ' ')
				{
					if (!_lineStart)
						_pendingSpace = true;

					continue;
				}

				EmitPendingSpace();
				Append(c, style, link, isContent: true);
			}
		}

		/// <summary>
		/// Appends a forced line break. Pending white space before the break is dropped.
		/// </summary>
		public void AddBreak()
		{
			_pendingSpace = false;
			FlushRun();
			_inlines.Add(new BreakInline());
			_lineStart = true;
		}

		/// <summary>
		/// Appends an inline object, such as an image or an equation.
		/// </summary>
		/// <param name="inline">The inline.</param>
		public void AddObject(Inline inline)
		{
			EmitPendingSpace();
			FlushRun();

			_inlines.Add(inline);
			_lastLink = inline.Link;
			_lastStyle = InlineStyle.None;
			_lineStart = false;
			this.HasContent = true;
		}

		/// <summary>
		/// Appends a space if there is content before it, as the boundary between two inline-block elements renders.
		/// </summary>
		public void AddSpace()
		{
			if (!_lineStart)
				_pendingSpace = true;
		}

		/// <summary>
		/// Completes the paragraph and returns its inlines, without leading and trailing line breaks.
		/// </summary>
		/// <returns>The inlines.</returns>
		public List<Inline> Complete()
		{
			FlushRun();

			while (_inlines.Count > 0 && _inlines[_inlines.Count - 1] is BreakInline)
				_inlines.RemoveAt(_inlines.Count - 1);

			while (_inlines.Count > 0 && _inlines[0] is BreakInline)
				_inlines.RemoveAt(0);

			return _inlines;
		}

		#region Implementation

		/// <summary>
		/// Emits the pending space, with the attributes of the preceding content.
		/// </summary>
		private void EmitPendingSpace()
		{
			if (!_pendingSpace)
				return;

			_pendingSpace = false;
			Append(' ', _lastStyle, _lastLink, isContent: false);
		}

		/// <summary>
		/// Appends a character to the current run, starting a new run when the attributes change.
		/// </summary>
		/// <param name="c">The character.</param>
		/// <param name="style">The formatting.</param>
		/// <param name="link">The link target.</param>
		/// <param name="isContent">Whether the character is visible content.</param>
		private void Append(char c, InlineStyle style, string link, bool isContent)
		{
			if (_run.Length > 0 && (style != _runStyle || link != _runLink))
				FlushRun();

			if (_run.Length == 0)
			{
				_runStyle = style;
				_runLink = link;
			}

			_run.Append(c);
			_lastStyle = style;
			_lastLink = link;
			_lineStart = false;

			if (isContent)
				this.HasContent = true;
		}

		/// <summary>
		/// Completes the current run.
		/// </summary>
		private void FlushRun()
		{
			if (_run.Length == 0)
				return;

			_inlines.Add(new TextInline { Text = _run.ToString(), Style = _runStyle, Link = _runLink });
			_run.Clear();
		}

		/// <summary>
		/// Determines whether a character is collapsible white space.
		/// </summary>
		/// <param name="c">The character.</param>
		/// <returns><c>true</c> for space, tab, line feed, form feed and carriage return.</returns>
		private static bool IsWhitespace(char c)
		{
			return c == ' ' || c == '\n' || c == '\t' || c == '\f' || c == '\r';
		}

		#endregion
	}
}

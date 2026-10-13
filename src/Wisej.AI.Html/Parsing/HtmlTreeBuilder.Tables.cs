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

namespace Wisej.AI.Html.Parsing
{
	/// <summary>
	/// The table insertion modes (HTML standard, 13.2.6.4.9 to 13.2.6.4.15).
	/// </summary>
	internal sealed partial class HtmlTreeBuilder
	{
		/// <summary>The table section names.</summary>
		private static readonly string[] TableSections = { "tbody", "thead", "tfoot" };

		/// <summary>The table cell names.</summary>
		private static readonly string[] TableCells = { "td", "th" };

		#region In table

		/// <summary>
		/// 13.2.6.4.9 The "in table" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void InTableMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Characters:
					var current = this.CurrentNode;
					if (current.IsHtml
						&& (current.LocalName == "table" || current.LocalName == "tbody" || current.LocalName == "template"
							|| current.LocalName == "tfoot" || current.LocalName == "thead" || current.LocalName == "tr"))
					{
						_pendingTableText.Clear();
						_originalMode = _mode;
						Reprocess(InsertionMode.InTableText, token);
						return;
					}
					break;

				case HtmlTokenType.Comment:
					InsertComment(token);
					return;

				case HtmlTokenType.Doctype:
					return;

				case HtmlTokenType.StartTag:
					switch (token.Name)
					{
						case "caption":
							ClearStackBackTo("table", "template", "html");
							_formatting.Add(null);
							InsertHtmlElement(token);
							_mode = InsertionMode.InCaption;
							return;

						case "colgroup":
							ClearStackBackTo("table", "template", "html");
							InsertHtmlElement(token);
							_mode = InsertionMode.InColumnGroup;
							return;

						case "col":
							ClearStackBackTo("table", "template", "html");
							InsertHtmlElement("colgroup");
							Reprocess(InsertionMode.InColumnGroup, token);
							return;

						case "tbody":
						case "tfoot":
						case "thead":
							ClearStackBackTo("table", "template", "html");
							InsertHtmlElement(token);
							_mode = InsertionMode.InTableBody;
							return;

						case "td":
						case "th":
						case "tr":
							ClearStackBackTo("table", "template", "html");
							InsertHtmlElement("tbody");
							Reprocess(InsertionMode.InTableBody, token);
							return;

						case "table":
							// "<table><table>" closes the first table.
							if (!HasInScope("table", Scope.Table))
								return;

							PopUntil("table");
							ResetInsertionMode();
							Dispatch(token);
							return;

						case "style":
						case "script":
						case "template":
							InHeadMode(token);
							return;

						case "input":
							var type = token.GetAttribute("type");
							if (type == null || !String.Equals(type, "hidden", StringComparison.OrdinalIgnoreCase))
								break;

							// hidden inputs stay in the table instead of being foster parented.
							InsertHtmlElement(token);
							Pop();
							return;

						case "form":
							if (StackContains("template") || _form != null)
								return;

							_form = InsertHtmlElement(token);
							Pop();
							return;
					}
					break;

				case HtmlTokenType.EndTag:
					switch (token.Name)
					{
						case "table":
							if (!HasInScope("table", Scope.Table))
								return;

							PopUntil("table");
							ResetInsertionMode();
							return;

						case "body":
						case "caption":
						case "col":
						case "colgroup":
						case "html":
						case "tbody":
						case "td":
						case "tfoot":
						case "th":
						case "thead":
						case "tr":
							return;

						case "template":
							InHeadMode(token);
							return;
					}
					break;

				case HtmlTokenType.EndOfFile:
					InBodyMode(token);
					return;
			}

			// content misplaced in a table is moved before the table.
			_fosterParenting = true;
			InBodyMode(token);
			_fosterParenting = false;
		}

		/// <summary>
		/// 13.2.6.4.10 The "in table text" insertion mode, which collects text inside a table to decide
		/// whether it is foster parented.
		/// </summary>
		/// <param name="token">The token.</param>
		private void InTableTextMode(HtmlToken token)
		{
			if (token.Type == HtmlTokenType.Characters)
			{
				var data = token.Data.IndexOf('\0') >= 0 ? token.Data.Replace("\0", "") : token.Data;
				if (data.Length > 0)
					_pendingTableText.Add(data);

				return;
			}

			var text = String.Concat(_pendingTableText);
			_pendingTableText.Clear();

			if (!IsAllWhitespace(text))
			{
				// non-white space text is foster parented, as the "anything else" rule of "in table".
				_fosterParenting = true;
				InBodyCharacters(text);
				_fosterParenting = false;
			}
			else
			{
				InsertCharacters(text);
			}

			Reprocess(_originalMode, token);
		}

		/// <summary>
		/// Pops elements until the current node is one of the specified elements.
		/// </summary>
		/// <param name="names">The names of the elements that stop the clearing.</param>
		private void ClearStackBackTo(params string[] names)
		{
			while (_stack.Count > 0)
			{
				var current = this.CurrentNode;
				if (current.IsHtml && Array.IndexOf(names, current.LocalName) >= 0)
					return;

				Pop();
			}
		}

		#endregion

		#region Caption and column group

		/// <summary>
		/// 13.2.6.4.11 The "in caption" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void InCaptionMode(HtmlToken token)
		{
			if (token.Type == HtmlTokenType.EndTag && token.Name == "caption")
			{
				CloseCaption();
				return;
			}

			var closesCaption =
				(token.Type == HtmlTokenType.StartTag
					&& (token.Name == "caption" || token.Name == "col" || token.Name == "colgroup" || token.Name == "tbody" || token.Name == "td"
						|| token.Name == "tfoot" || token.Name == "th" || token.Name == "thead" || token.Name == "tr"))
				|| (token.Type == HtmlTokenType.EndTag && token.Name == "table");

			if (closesCaption)
			{
				if (CloseCaption())
					Dispatch(token);

				return;
			}

			if (token.Type == HtmlTokenType.EndTag)
			{
				switch (token.Name)
				{
					case "body":
					case "col":
					case "colgroup":
					case "html":
					case "tbody":
					case "td":
					case "tfoot":
					case "th":
					case "thead":
					case "tr":
						return;
				}
			}

			InBodyMode(token);
		}

		/// <summary>
		/// Closes the open caption and returns to the "in table" insertion mode.
		/// </summary>
		/// <returns><c>false</c> when there is no caption in table scope and the token is ignored.</returns>
		private bool CloseCaption()
		{
			if (!HasInScope("caption", Scope.Table))
				return false;

			GenerateImpliedEndTags();
			PopUntil("caption");
			ClearFormattingToLastMarker();
			_mode = InsertionMode.InTable;
			return true;
		}

		/// <summary>
		/// 13.2.6.4.12 The "in column group" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void InColumnGroupMode(HtmlToken token)
		{
			switch (token.Type)
			{
				case HtmlTokenType.Characters:
					token = SplitWhitespace(token, out var whitespace);
					InsertCharacters(whitespace);
					if (token == null)
						return;
					break;

				case HtmlTokenType.Comment:
					InsertComment(token);
					return;

				case HtmlTokenType.Doctype:
					return;

				case HtmlTokenType.StartTag:
					switch (token.Name)
					{
						case "html":
							InBodyMode(token);
							return;

						case "col":
							InsertHtmlElement(token);
							Pop();
							return;

						case "template":
							InHeadMode(token);
							return;
					}
					break;

				case HtmlTokenType.EndTag:
					switch (token.Name)
					{
						case "colgroup":
							if (!CurrentIs("colgroup"))
								return;

							Pop();
							_mode = InsertionMode.InTable;
							return;

						case "col":
							return;

						case "template":
							InHeadMode(token);
							return;
					}
					break;

				case HtmlTokenType.EndOfFile:
					InBodyMode(token);
					return;
			}

			if (!CurrentIs("colgroup"))
				return;

			Pop();
			Reprocess(InsertionMode.InTable, token);
		}

		#endregion

		#region Table body, row and cell

		/// <summary>
		/// 13.2.6.4.13 The "in table body" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void InTableBodyMode(HtmlToken token)
		{
			if (token.Type == HtmlTokenType.StartTag)
			{
				switch (token.Name)
				{
					case "tr":
						ClearStackBackTo("tbody", "tfoot", "thead", "template", "html");
						InsertHtmlElement(token);
						_mode = InsertionMode.InRow;
						return;

					case "th":
					case "td":
						ClearStackBackTo("tbody", "tfoot", "thead", "template", "html");
						InsertHtmlElement("tr");
						Reprocess(InsertionMode.InRow, token);
						return;

					case "caption":
					case "col":
					case "colgroup":
					case "tbody":
					case "tfoot":
					case "thead":
						CloseTableSectionAndReprocess(token);
						return;
				}
			}
			else if (token.Type == HtmlTokenType.EndTag)
			{
				switch (token.Name)
				{
					case "tbody":
					case "tfoot":
					case "thead":
						if (!HasInScope(token.Name, Scope.Table))
							return;

						ClearStackBackTo("tbody", "tfoot", "thead", "template", "html");
						Pop();
						_mode = InsertionMode.InTable;
						return;

					case "table":
						CloseTableSectionAndReprocess(token);
						return;

					case "body":
					case "caption":
					case "col":
					case "colgroup":
					case "html":
					case "td":
					case "th":
					case "tr":
						return;
				}
			}

			InTableMode(token);
		}

		/// <summary>
		/// Closes the open table section and reprocesses the token in the "in table" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void CloseTableSectionAndReprocess(HtmlToken token)
		{
			if (!HasAnyInScope(TableSections, Scope.Table))
				return;

			ClearStackBackTo("tbody", "tfoot", "thead", "template", "html");
			Pop();
			Reprocess(InsertionMode.InTable, token);
		}

		/// <summary>
		/// 13.2.6.4.14 The "in row" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void InRowMode(HtmlToken token)
		{
			if (token.Type == HtmlTokenType.StartTag)
			{
				switch (token.Name)
				{
					case "th":
					case "td":
						ClearStackBackTo("tr", "template", "html");
						InsertHtmlElement(token);
						_mode = InsertionMode.InCell;
						_formatting.Add(null);
						return;

					case "caption":
					case "col":
					case "colgroup":
					case "tbody":
					case "tfoot":
					case "thead":
					case "tr":
						if (CloseRow())
							Dispatch(token);
						return;
				}
			}
			else if (token.Type == HtmlTokenType.EndTag)
			{
				switch (token.Name)
				{
					case "tr":
						CloseRow();
						return;

					case "table":
						if (CloseRow())
							Dispatch(token);
						return;

					case "tbody":
					case "tfoot":
					case "thead":
						if (!HasInScope(token.Name, Scope.Table))
							return;

						if (CloseRow())
							Dispatch(token);
						return;

					case "body":
					case "caption":
					case "col":
					case "colgroup":
					case "html":
					case "td":
					case "th":
						return;
				}
			}

			InTableMode(token);
		}

		/// <summary>
		/// Closes the open row and returns to the "in table body" insertion mode.
		/// </summary>
		/// <returns><c>false</c> when there is no row in table scope and the token is ignored.</returns>
		private bool CloseRow()
		{
			if (!HasInScope("tr", Scope.Table))
				return false;

			ClearStackBackTo("tr", "template", "html");
			Pop();
			_mode = InsertionMode.InTableBody;
			return true;
		}

		/// <summary>
		/// 13.2.6.4.15 The "in cell" insertion mode.
		/// </summary>
		/// <param name="token">The token.</param>
		private void InCellMode(HtmlToken token)
		{
			if (token.Type == HtmlTokenType.EndTag)
			{
				switch (token.Name)
				{
					case "td":
					case "th":
						if (!HasInScope(token.Name, Scope.Table))
							return;

						GenerateImpliedEndTags();
						PopUntil(token.Name);
						ClearFormattingToLastMarker();
						_mode = InsertionMode.InRow;
						return;

					case "body":
					case "caption":
					case "col":
					case "colgroup":
					case "html":
						return;

					case "table":
					case "tbody":
					case "tfoot":
					case "thead":
					case "tr":
						if (!HasInScope(token.Name, Scope.Table))
							return;

						CloseCell();
						Dispatch(token);
						return;
				}
			}
			else if (token.Type == HtmlTokenType.StartTag)
			{
				switch (token.Name)
				{
					case "caption":
					case "col":
					case "colgroup":
					case "tbody":
					case "td":
					case "tfoot":
					case "th":
					case "thead":
					case "tr":
						if (!HasAnyInScope(TableCells, Scope.Table))
							return;

						CloseCell();
						Dispatch(token);
						return;
				}
			}

			InBodyMode(token);
		}

		/// <summary>
		/// Closes the open cell (13.2.6.4.15).
		/// </summary>
		private void CloseCell()
		{
			GenerateImpliedEndTags();
			PopUntilOneOf(TableCells);
			ClearFormattingToLastMarker();
			_mode = InsertionMode.InRow;
		}

		#endregion
	}
}

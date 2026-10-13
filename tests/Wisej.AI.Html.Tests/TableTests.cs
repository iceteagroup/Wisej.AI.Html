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

using Xunit;

namespace Wisej.AI.Html.Tests
{
	/// <summary>
	/// Tests of tables: headers, merged cells, layout tables, nested tables and cell content.
	/// </summary>
	public class TableTests : TestBase
	{
		[Theory]
		[InlineData("<table><tr><th>A</th><th>B</th></tr><tr><td>1</td><td>2</td></tr></table>", "| A | B |\n| --- | --- |\n| 1 | 2 |")]
		[InlineData("<table><thead><tr><td>A</td><td>B</td></tr></thead><tbody><tr><td>1</td><td>2</td></tr></tbody></table>", "| A | B |\n| --- | --- |\n| 1 | 2 |")]
		[InlineData("<table><tr><td>a</td><td>b</td></tr><tr><td>c</td><td>d</td></tr></table>", "| a | b |\n| --- | --- |\n| c | d |")]
		[InlineData("<table><thead><tr><th>A</th></tr></thead><tfoot><tr><td>F</td></tr></tfoot><tbody><tr><td>1</td></tr></tbody></table>", "| A |\n| --- |\n| 1 |\n| F |")]
		public void WritesPipeTables(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		[Fact]
		public void RepeatsMergedCells()
		{
			var html = "<table><tr><th>A</th><th>B</th></tr><tr><td colspan=2>x</td></tr><tr><td rowspan=2>y</td><td>1</td></tr><tr><td>2</td></tr></table>";

			Assert.Equal("| A | B |\n| --- | --- |\n| x | x |\n| y | 1 |\n| y | 2 |", Markdown(html));
			Assert.Equal("| A | B |\n| --- | --- |\n| x |  |\n| y | 1 |\n|  | 2 |", Markdown(html, o => o.MergedCells = MergedCellMode.Empty));
		}

		[Fact]
		public void CombinesHeaderRows()
		{
			var html = "<table><thead><tr><th colspan=2>Group</th><th rowspan=2>C</th></tr><tr><th>A</th><th>B</th></tr></thead><tr><td>1</td><td>2</td><td>3</td></tr></table>";

			Assert.Equal("| Group / A | Group / B | C |\n| --- | --- | --- |\n| 1 | 2 | 3 |", Markdown(html));
		}

		[Fact]
		public void LimitsRowSpansToTheirGroup()
		{
			var html = "<table><thead><tr><th rowspan=5>H</th><th>X</th></tr></thead><tbody><tr><td>a</td><td>b</td></tr></tbody></table>";

			Assert.Equal("| H | X |\n| --- | --- |\n| a | b |", Markdown(html));
		}

		[Fact]
		public void FormatsCellContent()
		{
			var html = "<table><tr><th>A</th><th>B</th><th>C</th></tr>" +
				"<tr><td>a|b<br>c</td><td><ul><li>x</li><li>y</li></ul></td><td><b>whole</b></td></tr>" +
				"<tr><td><b>part</b> of</td><td><pre>code</pre></td><td><img src=i.png alt=I></td></tr></table>";

			Assert.Equal("| A | B | C |\n| --- | --- | --- |\n| a\\|b<br>c | • x<br>• y | whole |\n| **part** of | `code` | ![I](i.png) |", Markdown(html));
		}

		[Fact]
		public void RemovesEmptyRowsAndColumnsAndDuplicateColumns()
		{
			var html = "<table><tr><th>A</th><th></th><th>N</th><th>N</th></tr><tr><td>1</td><td>&nbsp;</td><td>x</td><td>x</td></tr><tr><td></td><td></td><td></td><td></td></tr></table>";

			Assert.Equal("| A | N |\n| --- | --- |\n| 1 | x |", Markdown(html));
		}

		[Fact]
		public void WritesTheCaption()
		{
			Assert.Equal("Cap\n\n| A |\n| --- |\n| 1 |", Markdown("<table><caption>Cap</caption><tr><th>A</th></tr><tr><td>1</td></tr></table>"));
		}

		[Theory]
		[InlineData("<table><tr><td>Left</td><td>Right</td></tr></table>", "Left\n\nRight")]
		[InlineData("<table><tr><td>Top</td></tr><tr><td>Bottom</td></tr></table>", "Top\n\nBottom")]
		[InlineData("<table role=presentation><tr><td>a</td><td>b</td></tr><tr><td>c</td><td>d</td></tr></table>", "a\n\nb\n\nc\n\nd")]
		[InlineData("<table><tr><td>Menu</td><td><table><tr><th>A</th><th>B</th></tr><tr><td>1</td><td>2</td></tr></table></td></tr></table>", "Menu\n\n| A | B |\n| --- | --- |\n| 1 | 2 |")]
		public void UnwrapsLayoutTables(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		[Fact]
		public void KeepsLayoutTablesWhenAsked()
		{
			Assert.Equal("| Left | Right |\n| --- | --- |", Markdown("<table><tr><td>Left</td><td>Right</td></tr></table>", o => o.UnwrapLayoutTables = false));
		}

		[Fact]
		public void WritesNestedDataTablesAsHtml()
		{
			var html = "<table><tr><th>A</th><th>B</th></tr><tr><td>x</td><td><table><tr><th>i</th></tr><tr><td>j</td></tr></table></td></tr></table>";

			Assert.Equal(
				"<table>\n<tr><th>A</th><th>B</th></tr>\n<tr><td>x</td><td><table>\n<tr><th>i</th></tr>\n<tr><td>j</td></tr>\n</table></td></tr>\n</table>",
				Markdown(html, o => o.UnwrapLayoutTables = false));

			Assert.Equal("| A | B |\n| --- | --- |\n| x | i<br>j |", Markdown(html, o =>
			{
				o.UnwrapLayoutTables = false;
				o.Tables = TableFormat.Markdown;
			}));
		}

		[Fact]
		public void WritesHtmlTablesWithSpans()
		{
			var html = "<table><caption>C</caption><tr><th>A</th><th>B</th></tr><tr><td colspan=2>x</td></tr><tr><td rowspan=2>y</td><td>1</td></tr><tr><td>2</td></tr></table>";

			Assert.Equal(
				"<table>\n<caption>C</caption>\n<tr><th>A</th><th>B</th></tr>\n<tr><td colspan=\"2\">x</td></tr>\n<tr><td rowspan=\"2\">y</td><td>1</td></tr>\n<tr><td>2</td></tr>\n</table>",
				Markdown(html, o => o.Tables = TableFormat.Html));
		}

		[Fact]
		public void BoundsTheGridOfHostileTables()
		{
			var html = new System.Text.StringBuilder("<table><tr><th>A</th><th>B</th></tr>");
			for (var i = 0; i < 3000; i++)
				html.Append("<tr><td colspan=1000 rowspan=65534>x</td><td>y</td></tr>");
			html.Append("</table>");

			var text = Markdown(html.ToString());

			Assert.StartsWith("| A | B |", text);
			Assert.True(text.Length < 200000, $"length {text.Length}");
		}

		[Fact]
		public void WritesTablesInPlainText()
		{
			Assert.Equal("A | B\n1 | 2", PlainText("<table><tr><th>A</th><th>B</th></tr><tr><td>1</td><td>2</td></tr></table>"));
		}
	}
}

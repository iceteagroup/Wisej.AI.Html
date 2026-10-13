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
	/// Tests of ordered, unordered and definition lists.
	/// </summary>
	public class ListTests : TestBase
	{
		[Theory]
		[InlineData("<ul><li>a</li><li>b<ul><li>c</li></ul></li></ul>", "- a\n- b\n  - c")]
		[InlineData("<ol><li>a<ol><li>b</li><li>c</li></ol></li><li>d</li></ol>", "1. a\n   1. b\n   2. c\n2. d")]
		[InlineData("<ul><li><p>p1</p><p>p2</p></li><li>q</li></ul>", "- p1\n  p2\n- q")]
		[InlineData("<ul><li>a</li><ul><li>nested directly</li></ul></ul>", "- a\n  - nested directly")]
		[InlineData("<ul><li></li><li>a</li></ul>", "- a")]
		public void WritesNestedLists(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		[Theory]
		[InlineData("<ol start=3><li>a</li><li>b</li></ol>", "3. a\n4. b")]
		[InlineData("<ol type=a><li>x</li><li>y</li></ol>", "a. x\nb. y")]
		[InlineData("<ol type=I><li>x</li><li>y</li><li>z</li><li>w</li></ol>", "I. x\nII. y\nIII. z\nIV. w")]
		[InlineData("<ol reversed><li>a</li><li>b</li><li>c</li></ol>", "3. a\n2. b\n1. c")]
		[InlineData("<ol><li>a</li><li value=10>b</li><li>c</li></ol>", "1. a\n10. b\n11. c")]
		[InlineData("<ol style='list-style-type: lower-roman'><li>a</li><li>b</li></ol>", "i. a\nii. b")]
		[InlineData("<ol style='list-style: none'><li>a</li></ol>", "- a")]
		[InlineData("<ol><li>a</li><li hidden>b</li><li>c</li></ol>", "1. a\n2. c")]
		public void NumbersItemsAsBrowsers(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		[Fact]
		public void WritesTaskLists()
		{
			Assert.Equal("- [x] done\n- [ ] todo", Markdown("<ul><li><input type=checkbox checked> done</li><li><input type=checkbox> todo</li></ul>"));
		}

		[Theory]
		[InlineData("<dl><dt>Term</dt><dd>Definition</dd></dl>", "**Term**\n: Definition")]
		[InlineData("<dl><div><dt>A</dt><dd>1</dd></div><div><dt>B</dt><dd>2</dd><dd>3</dd></div></dl>", "**A**\n: 1\n\n**B**\n: 2\n: 3")]
		[InlineData("<dl><dt><b>Bold</b> term</dt><dd><p>p1</p><p>p2</p></dd></dl>", "**Bold term**\n: p1\n  p2")]
		public void WritesDefinitionLists(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		[Fact]
		public void RecognizesWordLists()
		{
			var html =
				"<p class=MsoNormal>Intro</p>" +
				"<p class=MsoListParagraphCxSpFirst style='text-indent:-.25in;mso-list:l0 level1 lfo1'><![if !supportLists]><span style='font-family:Symbol'><span style='mso-list:Ignore'>·<span style='font:7.0pt \"Times New Roman\"'>&nbsp;&nbsp;&nbsp; </span></span></span><![endif]>Bullet <b>one</b></p>\n" +
				"<p class=MsoListParagraphCxSpMiddle style='margin-left:1.0in;text-indent:-.25in;mso-list:l0 level2 lfo1'><![if !supportLists]><span style='font-family:\"Courier New\"'><span style='mso-list:Ignore'>o<span style='font:7.0pt \"Times New Roman\"'>&nbsp;&nbsp; </span></span></span><![endif]>Nested</p>\n" +
				"<p class=MsoListParagraphCxSpLast style='text-indent:-.25in;mso-list:l0 level1 lfo1'><![if !supportLists]><span style='mso-list:Ignore'>·<span>&nbsp;&nbsp; </span></span><![endif]>Bullet two</p>" +
				"<p class=MsoNormal>Outro</p>";

			Assert.Equal("Intro\n\n- Bullet **one**\n  - Nested\n- Bullet two\n\nOutro", Markdown(html));
		}

		[Fact]
		public void RecognizesFilteredWordLists()
		{
			var html =
				"<p class=MsoListParagraphCxSpFirst style='margin-left:.5in;text-indent:-.25in'>a)<span style='font:7.0pt \"Times New Roman\"'>&nbsp;&nbsp; </span>Alpha</p>" +
				"<p class=MsoListParagraphCxSpLast style='margin-left:1.0in;text-indent:-.25in'>i.<span>&nbsp;&nbsp;</span>Roman</p>" +
				"<p class=MsoListParagraph>Not a list item: no marker</p>";

			Assert.Equal("a) Alpha\n   i. Roman\n\nNot a list item: no marker", Markdown(html));
		}

		[Fact]
		public void WritesListsInPlainText()
		{
			Assert.Equal("- a\n  1. b\n\nTerm\n  Definition", PlainText("<ul><li>a<ol><li>b</li></ol></li></ul><dl><dt>Term</dt><dd>Definition</dd></dl>"));
		}
	}
}

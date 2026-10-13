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
using Xunit;

namespace Wisej.AI.Html.Tests
{
	/// <summary>
	/// Tests of paragraphs, white space, character formatting, headings, links, code and escaping.
	/// </summary>
	public class TextTests : TestBase
	{
		#region Paragraphs and white space

		[Theory]
		[InlineData("<p>  a \n  <b> b </b>  c </p>", "a **b** c")]
		[InlineData("<p>a<span> </span>b</p>", "a b")]
		[InlineData("<p>a <span> b</span></p>", "a b")]
		[InlineData("<p>a&nbsp;&nbsp;b</p>", "a b")]
		[InlineData("<p>soft­hyphen zero​width</p>", "softhyphen zerowidth")]
		[InlineData("<p>a<br>b</p>", "a\nb")]
		[InlineData("<p>a <br> b<br></p>", "a\nb")]
		[InlineData("a<div>b</div>c", "a\n\nb\n\nc")]
		[InlineData("<p><span style='display:block'>a</span><span style='display:block'>b</span></p>", "a\n\nb")]
		[InlineData("<div style='display:inline'>a</div> <div style='display:inline'>b</div>", "a b")]
		[InlineData("<p style='white-space:pre-line'>a\n  b</p>", "a\nb")]
		public void CollapsesWhiteSpaceAsBrowsers(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		#endregion

		#region Formatting

		[Theory]
		[InlineData("<p>Hello <b>bold</b> and <i>italic</i></p>", "Hello **bold** and *italic*")]
		[InlineData("<p><strong>bold <em>both</em></strong> none</p>", "**bold *both*** none")]
		[InlineData("<p><b>bold </b>text</p>", "**bold** text")]
		[InlineData("<p>H<sub>2</sub>O and x<sup>2</sup></p>", "H<sub>2</sub>O and x<sup>2</sup>")]
		[InlineData("<p><del>old</del> <s>gone</s></p>", "~~old gone~~")]
		[InlineData("<p>Use <code>Console.Write</code> or <kbd>Ctrl</kbd></p>", "Use `Console.Write` or `Ctrl`")]
		[InlineData("<p>Use <code>a`b</code></p>", "Use ``a`b``")]
		[InlineData("<p><q>quoted</q></p>", "“quoted”")]
		[InlineData("<p><ruby>漢<rt>kan</rt></ruby></p>", "漢(kan)")]
		public void WritesCharacterFormatting(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		[Fact]
		public void AppliesTheStyleSheets()
		{
			var html = "<style>.b { font-weight: bold } .c { font-family: Consolas, monospace } .i { font-style: italic } .h { display: none }</style>" +
				"<p><span class=b>x</span> <span class=c>y</span> <span class=i>z</span><span class=h>hidden</span></p>";

			Assert.Equal("**x** `y` *z*", Markdown(html));
		}

		[Fact]
		public void HandlesTheGoogleDocsWrapper()
		{
			// Google Docs wraps the clipboard content in <b style="font-weight:normal">.
			var html = "<b style=\"font-weight:normal;\" id=\"docs-internal-guid-1\"><p dir=\"ltr\"><span style=\"font-weight:700\">Bold</span><span style=\"font-weight:400\"> normal </span>" +
				"<span style=\"font-style:italic\">italic</span><span style=\"vertical-align:super\">2</span></p></b>";

			Assert.Equal("**Bold** normal *italic*<sup>2</sup>", Markdown(html));
		}

		[Fact]
		public void OmitsFormattingWhenDisabled()
		{
			Assert.Equal("a b c", Markdown("<p><b>a</b> <i>b</i> <code>c</code></p>", o => o.IncludeFormatting = false));
		}

		#endregion

		#region Headings, quotes, rules, code

		[Theory]
		[InlineData("<h1>Title</h1>", "# Title")]
		[InlineData("<h3>Sub <em>title</em></h3>", "### Sub *title*")]
		[InlineData("<h2><b>All bold</b></h2>", "## All bold")]
		[InlineData("<h2>Two<br>lines</h2>", "## Two lines")]
		[InlineData("<h2>Section #</h2>", "## Section \\#")]
		[InlineData("<h2>C#</h2>", "## C#")]
		[InlineData("<div role=heading aria-level=4>Aria</div>", "#### Aria")]
		public void WritesHeadings(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		[Fact]
		public void WritesQuotationsAndRules()
		{
			Assert.Equal("> a\n>\n> b\n\n---\n\nc", Markdown("<blockquote><p>a</p><p>b</p></blockquote><hr><p>c</p>"));
		}

		[Fact]
		public void WritesCodeBlocksWithTheirLanguage()
		{
			var html = "<pre><code class=\"language-csharp\">var x = 1;\n\n  if (x &lt; 2)\n    x++;\n</code></pre>";

			Assert.Equal("```csharp\nvar x = 1;\n\n  if (x < 2)\n    x++;\n```", Markdown(html));
		}

		[Fact]
		public void WritesHighlightedCodeLineByLine()
		{
			var html = "<div class=\"highlight-source-js\"><pre><span class=k>let</span> a<br><div class=line>b ``` c</div></pre></div>";

			Assert.Equal("````js\nlet a\nb ``` c\n````", Markdown(html));
		}

		#endregion

		#region Links

		[Theory]
		[InlineData("<a href='https://x.com/a b'>text</a>", "[text](https://x.com/a%20b)")]
		[InlineData("<a href='#top'>top</a>", "top")]
		[InlineData("<a href='javascript:void(0)'>js</a>", "js")]
		[InlineData("<a href='https://x.com/'>https://x.com</a>", "https://x.com/")]
		[InlineData("<a href='mailto:a@b.com'>a@b.com</a>", "a@b.com")]
		[InlineData("<a href='/a'> <b>bold</b> link </a>", "[**bold** link](/a)")]
		[InlineData("<a href='/a'>A</a><a href='/b'>B</a>", "[A](/a) [B](/b)")]
		public void WritesLinks(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		[Fact]
		public void ResolvesLinksAgainstTheBaseUrl()
		{
			Assert.Equal("[p](https://example.com/dir/page.html)", Markdown("<a href='page.html'>p</a>", o => o.BaseUri = new Uri("https://example.com/dir/")));
			Assert.Equal("[p](https://cdn.com/x/page.html)", Markdown("<base href='https://cdn.com/x/'><a href='page.html'>p</a>"));
		}

		[Fact]
		public void KeepsTheLinkOfACardOnItsTitle()
		{
			Assert.Equal("## [Title](/x)\n\nSummary\n\nRead more", Markdown("<a href='/x'><h2>Title</h2><p>Summary</p><p>Read more</p></a>"));
		}

		[Fact]
		public void CollectsTheLinks()
		{
			var result = Extract("<a href='https://a.com/'>A</a> <a href='https://a.com/'>again</a> <a href='/b'>B</a>");

			Assert.Equal(2, result.Links.Count);
			Assert.Equal("https://a.com/", result.Links[0].Url);
			Assert.Equal("B", result.Links[1].Text);
		}

		[Fact]
		public void OmitsLinksWhenDisabled()
		{
			Assert.Equal("text", Markdown("<a href='https://x.com'>text</a>", o => o.IncludeHyperlinks = false));
		}

		#endregion

		#region Escaping

		[Theory]
		[InlineData("<p>2 * 3 = 6, snake_case, _x_</p>", "2 \\* 3 = 6, snake_case, \\_x\\_")]
		[InlineData("<p># not a heading</p>", "\\# not a heading")]
		[InlineData("<p>1. not a list</p>", "1\\. not a list")]
		[InlineData("<p>- not a list</p>", "\\- not a list")]
		[InlineData("<p>[a](b) &lt;b&gt; &amp;amp;</p>", "\\[a](b) &lt;b> &amp;amp;")]
		public void EscapesMarkdownSyntax(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		#endregion

		#region Title and plain text

		[Fact]
		public void AddsTheTitleWhenTheContentDoesNotStartWithIt()
		{
			Assert.Equal("# My Page\n\ntext", Markdown("<title>My Page</title><p>text</p>", o => o.IncludeTitle = true));
			Assert.Equal("# Article\n\ntext", Markdown("<title>Article - Site</title><h1>Article</h1><p>text</p>", o => o.IncludeTitle = true));
		}

		[Fact]
		public void WritesPlainText()
		{
			var html = "<h1>Title</h1><p>a <b>b</b> <a href='/x'>c</a><br>d</p><ol><li>x</li><li>y</li></ol><img src=i.png alt=Pic>";

			Assert.Equal("Title\n\na b c\nd\n\n1. x\n2. y\n\n[Image: Pic]", PlainText(html));
		}

		#endregion
	}
}

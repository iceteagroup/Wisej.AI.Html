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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace Wisej.AI.Html.Tests
{
	/// <summary>
	/// Tests of hostile and malformed input: time, stack and exceptions, and of the Markdown edge cases
	/// that would be read differently by a Markdown parser.
	/// </summary>
	public class RobustnessTests : TestBase
	{
		#region Time and stack

		[Fact]
		public void MatchesDescendantSelectorsInLinearTime()
		{
			var html = "<style>section div div div div div div { display: none }</style>" + Repeat("<div>", 400) + "text";

			var watch = Stopwatch.StartNew();
			Assert.Equal("text", Markdown(html));
			Assert.True(watch.ElapsedMilliseconds < 5000, $"{watch.ElapsedMilliseconds} ms");
		}

		[Fact]
		public void MatchesStructuralSelectorsInLinearTime()
		{
			var html = "<style>li:nth-child(odd) { font-weight: bold } li:nth-last-of-type(3) { display: none }</style><ul>" + Repeat("<li>x</li>", 20000) + "</ul>";

			var watch = Stopwatch.StartNew();
			Assert.StartsWith("- **x**\n- x\n", Markdown(html));
			Assert.True(watch.ElapsedMilliseconds < 5000, $"{watch.ElapsedMilliseconds} ms");
		}

		[Theory]
		[InlineData("<style>{0}p {{ display: none }}</style><p>text", "@media all {")]
		[InlineData("<style>p:not({0}a{1}) {{ font-weight: bold }}</style><p>text", ":not(")]
		public void BoundsTheNestingOfStyleSheets(string template, string piece)
		{
			var html = String.Format(template, Repeat(piece, 20000), Repeat(")", 20000));

			string text = null;
			var thread = new Thread(() => text = Markdown(html), 1024 * 1024);
			thread.Start();
			thread.Join();

			Assert.Contains("text", text);
		}

		[Theory]
		[InlineData("<p class=MsoListParagraph style='mso-list:l0 level99999999999 lfo1'>x</p>")]
		[InlineData("<style>p:nth-child(-n-2147483647) { display: none }</style><p>x</p>")]
		[InlineData("<ol start=2147483647><li>x</li><li>y</li></ol>")]
		[InlineData("<img src='https://x.com/a%ZZ.png' alt=x><a href='%E0%A4%A'>x</a>")]
		[InlineData("<a href='foo\nbar'>x</a>")]
		public void NeverThrowsOnHostileValues(string html)
		{
			Assert.NotNull(Markdown(html));
		}

		[Fact]
		public void NeverThrowsOnInvalidOptions()
		{
			var html = "<p>A</p><p style='page-break-before:always'>B</p><a href='x.html'>x</a>";

			Assert.Contains("{x}", Markdown(html, o => o.PageHeaderFormat = "Page {0} of {x}"));
			Assert.Contains("[x](x.html)", Markdown(html, o => o.BaseUri = new Uri("relative/", UriKind.Relative)));
		}

		#endregion

		#region Markdown edge cases

		[Theory]
		[InlineData("<p>Wow!<a href='/a'>link</a></p>", "Wow\\![link](/a)")]
		[InlineData("<p><a href='/a'>a]b[c</a></p>", "[a\\]b\\[c](/a)")]
		[InlineData("<p>a ~x~ b</p>", "a \\~x\\~ b")]
		[InlineData("<p>about ~5 min</p>", "about ~5 min")]
		[InlineData("<p>Title<br>=</p>", "Title\n\\=")]
		[InlineData("<p><a href='foo\nbar.html'>t</a></p>", "[t](foobar.html)")]
		[InlineData("<math><mi>x</mi><mspace/><mo>=</mo><mn>1</mn></math>", "$x\\,=1$")]
		public void EscapesWhatMarkdownWouldMisread(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		[Theory]
		[InlineData("<ul><li>Text<hr>More</li></ul>", "- Text\n\n  ---\n\n  More")]
		[InlineData("<ul><li>Intro<ol start=3><li>three</li></ol></li></ul>", "- Intro\n\n  3. three")]
		[InlineData("<ul><li>Intro<ol><li>one</li></ol></li></ul>", "- Intro\n  1. one")]
		public void SeparatesBlocksInListItems(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		[Fact]
		public void KeepsDataTablesThatNestTables()
		{
			var html = "<table><tr><th>A</th><th>B</th></tr><tr><td>x</td><td><table><tr><td>i</td><td>j</td></tr><tr><td>k</td><td>l</td></tr></table></td></tr></table>";

			Assert.StartsWith("<table>\n<tr><th>A</th><th>B</th></tr>", Markdown(html));
		}

		[Fact]
		public void RecognizesWordListsWithHiddenContent()
		{
			var html = "<p class=MsoListParagraph style='mso-list:l0 level1 lfo1'><span style='mso-list:Ignore'>1.<span>&nbsp;&nbsp;</span></span>Item</p>";

			Assert.Equal("1. Item", Markdown(html, o => o.IncludeHiddenContent = true));
		}

		[Fact]
		public void FallsBackToTheFirstHtmlPartOfAnArchive()
		{
			var mhtml = "MIME-Version: 1.0\r\nContent-Type: multipart/related; boundary=\"B\"; start=\"<missing>\"\r\n\r\n" +
				"--B\r\nContent-Type: text/html\r\n\r\n<p>Page</p>\r\n--B--\r\n";

			Assert.Equal("Page", Extract(Encoding.ASCII.GetBytes(mhtml)).Text);
		}

		#endregion

		/// <summary>
		/// Repeats a string.
		/// </summary>
		/// <param name="value">The string.</param>
		/// <param name="count">The number of repetitions.</param>
		/// <returns>The repeated string.</returns>
		private static string Repeat(string value, int count)
		{
			return String.Concat(Enumerable.Repeat(value, count));
		}
	}
}

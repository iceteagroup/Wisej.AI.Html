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
using System.Linq;
using Wisej.AI.Html.Dom;
using Xunit;

namespace Wisej.AI.Html.Tests
{
	/// <summary>
	/// Tests of the CSS selector engine used by <see cref="HtmlNode.QuerySelectorAll(string)"/> and the style sheets.
	/// </summary>
	public class SelectorTests : TestBase
	{
		/// <summary>The document queried by the tests.</summary>
		private static readonly HtmlDocument Document = HtmlDocument.Parse(
			"<div id=main class='box wide'>" +
			"<p class=first>a</p><p>b</p><span>c</span><p lang=en-US>d</p>" +
			"<ul><li>1</li><li>2</li><li>3</li><li>4</li><li>5</li></ul>" +
			"<a href='https://x.com/file.PDF'>pdf</a><a href='/local'>local</a>" +
			"<figure><img src=x.png></figure><figure><figcaption>none</figcaption></figure>" +
			"</div><p>outside</p>");

		[Theory]
		[InlineData("#main", "div")]
		[InlineData(".box.wide", "div")]
		[InlineData("div > p", "a,b,d")]
		[InlineData("div p:first-child", "a")]
		[InlineData("p + span", "c")]
		[InlineData("p ~ p", "b,d")]
		[InlineData("li:nth-child(2n+1)", "1,3,5")]
		[InlineData("li:nth-child(odd)", "1,3,5")]
		[InlineData("li:nth-last-child(2)", "4")]
		[InlineData("li:not(:first-child):not(:last-child)", "2,3,4")]
		[InlineData("[lang|=en]", "d")]
		[InlineData("a[href^=https]", "pdf")]
		[InlineData("a[href$='.pdf' i]", "pdf")]
		[InlineData("a[href*=loc]", "local")]
		[InlineData(":is(span, .first)", "a,c")]
		[InlineData("figure:has(> img)", "figure")]
		[InlineData("body > p", "outside")]
		[InlineData("P.FIRST, SPAN", "c")]
		public void MatchesElements(string selector, string expected)
		{
			var actual = Document.QuerySelectorAll(selector).Select(e => e.Name == "div" || e.Name == "figure" ? e.Name : e.TextContent);

			Assert.Equal(expected, String.Join(",", actual));
		}

		[Theory]
		[InlineData("p::before")]
		[InlineData("a:hover")]
		[InlineData("div >")]
		[InlineData("[href")]
		[InlineData("ns|p")]
		public void RejectsInvalidOrUnsupportedSelectors(string selector)
		{
			Assert.Throws<FormatException>(() => Document.QuerySelector(selector));
		}

		[Fact]
		public void MatchesAndFindsTheClosestAncestor()
		{
			var item = Document.QuerySelector("li");

			Assert.True(item.Matches("ul > li"));
			Assert.Equal("main", item.Closest("div").Id);
			Assert.Null(item.Closest("table"));
		}
	}
}

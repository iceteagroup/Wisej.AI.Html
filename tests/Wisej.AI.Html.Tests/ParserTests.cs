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
using System.Text;
using Wisej.AI.Html.Dom;
using Xunit;

namespace Wisej.AI.Html.Tests
{
	/// <summary>
	/// Tests of the HTML parser and of the document object model.
	/// </summary>
	public class ParserTests : TestBase
	{
		[Theory]
		[InlineData("<p>One<p>Two", "p(\"One\") p(\"Two\")")]
		[InlineData("<b>1<i>2</b>3</i>", "b(\"1\" i(\"2\")) i(\"3\")")]
		[InlineData("<b>1<p>2</b>3</p>", "b(\"1\") p(b(\"2\") \"3\")")]
		[InlineData("<a href=x>1<a href=y>2", "a(\"1\") a(\"2\")")]
		[InlineData("<table><tr><td>a</td></tr>x</table>", "\"x\" table(tbody(tr(td(\"a\"))))")]
		[InlineData("<ul><li>a<li>b</ul>", "ul(li(\"a\") li(\"b\"))")]
		[InlineData("<dl><dt>a<dd>b<dt>c</dl>", "dl(dt(\"a\") dd(\"b\") dt(\"c\"))")]
		[InlineData("<h1>a<h2>b", "h1(\"a\") h2(\"b\")")]
		[InlineData("<p>a</br>b", "p(\"a\" br \"b\")")]
		[InlineData("<select><option>a<option>b</select>", "select(option(\"a\") option(\"b\"))")]
		public void BuildsTheTreeOfBrowsers(string html, string expected)
		{
			Assert.Equal(expected, BodyTree(html));
		}

		[Fact]
		public void ImpliesTheDocumentStructure()
		{
			var document = HtmlDocument.Parse("<title>T</title>Text");

			Assert.Equal("html", document.DocumentElement.Name);
			Assert.Equal("T", document.Head.TextContent);
			Assert.Equal("Text", document.Body.TextContent);
		}

		[Fact]
		public void DecodesCharacterReferences()
		{
			var document = HtmlDocument.Parse("<p>&amp;&lt;&notin;&copy;&#x1F600;&#128512;&nbsp;&copy2026&#150;&bogus;</p>");

			Assert.Equal("&<∉©😀😀 ©2026–&bogus;", document.Body.TextContent);
		}

		[Fact]
		public void KeepsLegacyReferencesInAttributesLiteral()
		{
			var link = HtmlDocument.Parse("<a href=\"?a=1&copy=2&amp;b=3&lt\">x</a>").QuerySelector("a");

			Assert.Equal("?a=1&copy=2&b=3<", link.GetAttribute("href"));
		}

		[Fact]
		public void NormalizesNewlines()
		{
			Assert.Equal("a\nb\nc", HtmlDocument.Parse("<p>a\r\nb\rc</p>").Body.TextContent);
		}

		[Fact]
		public void DropsTheNewlineAfterPre()
		{
			Assert.Equal("code", HtmlDocument.Parse("<pre>\ncode</pre>").QuerySelector("pre").TextContent);
		}

		[Fact]
		public void ParsesRawTextElements()
		{
			var document = HtmlDocument.Parse("<script>if (a < b && c) { x = '</div>'; }</script><textarea><b>&amp;</b></textarea>");

			Assert.Equal("if (a < b && c) { x = '</div>'; }", document.QuerySelector("script").TextContent);
			Assert.Equal("<b>&</b>", document.QuerySelector("textarea").TextContent);
		}

		[Fact]
		public void AssignsForeignNamespaces()
		{
			var document = HtmlDocument.Parse("<svg viewbox='0 0 1 1'><foreignobject><p>x</p></foreignobject><path xlink:href='#a'/></svg><math><mi>y</mi></math>");

			var svg = document.QuerySelector("svg");
			Assert.Equal(HtmlNamespaces.Svg, svg.NamespaceUri);
			Assert.Equal("0 0 1 1", svg.GetAttribute("viewBox"));

			var foreignObject = svg.Children.First();
			Assert.Equal("foreignObject", foreignObject.LocalName);
			Assert.Equal(HtmlNamespaces.Html, foreignObject.Children.First().NamespaceUri);

			var href = svg.Children.Last().Attributes.First();
			Assert.Equal(HtmlNamespaces.XLink, href.NamespaceUri);
			Assert.Equal("xlink:href", href.Name);

			Assert.Equal(HtmlNamespaces.MathML, document.QuerySelector("mi").NamespaceUri);
		}

		[Fact]
		public void StoresTemplateContentInAFragment()
		{
			var template = HtmlDocument.Parse("<template><p>x</p></template>").QuerySelector("template");

			Assert.False(template.HasChildNodes);
			Assert.Equal("p", ((HtmlElement)template.Content.FirstChild).Name);
		}

		[Fact]
		public void ParsesFragmentsInContext()
		{
			Assert.Equal("td(\"a\")", Tree(HtmlDocument.ParseFragment("<td>a</td>", "tr")));
			Assert.Equal("\"a\"", Tree(HtmlDocument.ParseFragment("<td>a</td>", "body")));
			Assert.Equal("\"<b>x</b>\"", Tree(HtmlDocument.ParseFragment("<b>x</b>", "textarea")));
		}

		[Fact]
		public void DetectsQuirksMode()
		{
			Assert.Equal(HtmlQuirksMode.Quirks, HtmlDocument.Parse("<p>x").QuirksMode);
			Assert.Equal(HtmlQuirksMode.NoQuirks, HtmlDocument.Parse("<!DOCTYPE html><p>x").QuirksMode);
			Assert.Equal(HtmlQuirksMode.LimitedQuirks, HtmlDocument.Parse("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"x\">").QuirksMode);
		}

		[Fact]
		public void LimitsTheDepthOfTheTree()
		{
			var html = new StringBuilder();
			for (var i = 0; i < 5000; i++)
				html.Append("<div>");
			html.Append("deep");

			var document = HtmlDocument.Parse(html.ToString());

			var depth = 0;
			for (HtmlNode node = document.QuerySelectorAll("div").Last(); node != null; node = node.ParentNode)
				depth++;

			Assert.True(depth <= 520, $"depth {depth}");
			Assert.Contains("deep", new HtmlExtractor().Extract(document).Text);
		}

		[Theory]
		[InlineData("<span>")]
		[InlineData("<div><span>")]
		[InlineData("<ul><li>")]
		[InlineData("<blockquote>")]
		[InlineData("<table><tr><td>")]
		[InlineData("<pre><span>")]
		public void ExtractsDeepTreesOnSmallStacks(string open)
		{
			var html = new StringBuilder();
			for (var i = 0; i < 600; i++)
				html.Append(open);
			html.Append("deep text");

			// thread pool and ASP.NET threads have 1 MB stacks: a stack overflow would end the process.
			string text = null;
			var thread = new System.Threading.Thread(() => text = new HtmlExtractor().ExtractFromString(html.ToString()).Text, 1024 * 1024);
			thread.Start();
			thread.Join();

			Assert.Contains("deep text", text);
		}

		[Fact]
		public void ReturnsTheTitle()
		{
			Assert.Equal("A B", HtmlDocument.Parse("<title>  A \n B </title>").Title);
		}

		[Fact]
		public void SerializesNodes()
		{
			var document = HtmlDocument.Parse("<p class='a\"b'>x &amp; <br>y </p><script>a<b</script>");

			Assert.Equal("<p class=\"a&quot;b\">x &amp; <br>y&nbsp;</p>", document.QuerySelector("p").OuterHtml);
			Assert.Equal("a<b", document.QuerySelector("script").InnerHtml);
		}

		[Fact]
		public void ModifiesTheTree()
		{
			var document = HtmlDocument.Parse("<div><p>a</p></div>");
			var div = document.QuerySelector("div");
			var p = document.QuerySelector("p");

			var span = document.CreateElement("SPAN");
			span.SetAttribute("Class", "x");
			span.AppendChild(document.CreateTextNode("b"));
			div.InsertBefore(span, p);

			Assert.Equal("<div><span class=\"x\">b</span><p>a</p></div>", div.OuterHtml);
			Assert.Equal(2, div.ChildCount);
			Assert.Same(span, p.PreviousSibling);

			p.Remove();
			Assert.Equal("<div><span class=\"x\">b</span></div>", div.OuterHtml);

			Assert.Throws<InvalidOperationException>(() => span.AppendChild(div));
			Assert.Throws<InvalidOperationException>(() => span.FirstChild.AppendChild(p));
		}

		[Fact]
		public void FindsElements()
		{
			var document = HtmlDocument.Parse("<div id=a><p>1</p><P>2</P></div>");

			Assert.Equal(2, document.GetElementsByTagName("P").Count);
			Assert.Equal("a", document.GetElementById("a").Id);
			Assert.Equal(new[] { "1", "2" }, document.Body.DescendantElements().Where(e => e.Name == "p").Select(e => e.TextContent));
		}

		[Fact]
		public void NeverFailsOnMalformedInput()
		{
			var random = new Random(42);
			var pieces = new[] { "<", ">", "</", "<!--", "-->", "&", "&#", ";", "<table>", "<td>", "<svg>", "<math>", "<template>", "<b>", "</b>", "<p>", "<select>", "<![CDATA[", "]]>", "\"", "'", "=", " ", "x", "\0", "<script>", "</script>", "<!DOCTYPE", "<frameset>", "<li>", "<a>" };

			for (var iteration = 0; iteration < 300; iteration++)
			{
				var html = new StringBuilder();
				for (var i = 0; i < 200; i++)
					html.Append(pieces[random.Next(pieces.Length)]);

				var document = HtmlDocument.Parse(html.ToString());
				Assert.NotNull(document.DocumentElement);
				Assert.NotNull(new HtmlExtractor().Extract(document).Text);
			}
		}
	}
}

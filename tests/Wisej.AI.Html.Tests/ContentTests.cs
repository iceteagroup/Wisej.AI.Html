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
using Xunit;

namespace Wisej.AI.Html.Tests
{
	/// <summary>
	/// Tests of images, hidden content, site chrome, forms, math, pages, metadata and MHTML archives.
	/// </summary>
	public class ContentTests : TestBase
	{
		/// <summary>A 1x1 PNG image.</summary>
		private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

		#region Images

		[Fact]
		public void ExtractsEmbeddedImages()
		{
			var result = Extract($"<p><img src=\"data:image/png;base64,{Png}\" alt=\"Logo\"></p><img src=\"data:image/png;base64,{Png}\" alt=\"Again\">");

			Assert.Equal("![Logo](image1.png)\n\n![Again](image1.png)", result.Text);
			Assert.Equal(2, result.Images.Count);
			Assert.Equal("image/png", result.Images[0].ContentType);
			Assert.Equal(Convert.FromBase64String(Png), result.Images[0].Data);
			Assert.Null(result.Images[0].Url);
		}

		[Theory]
		[InlineData("<img src='https://x.com/a.png' alt='A'>", "![A](https://x.com/a.png)")]
		[InlineData("<img src='a.png'>", "![](a.png)")]
		[InlineData("<img src='a.png' alt=''>", "")]
		[InlineData("<img src='a.png' alt='A' role='presentation'>", "")]
		[InlineData("<img src='pixel.gif' width=1 height=1 alt='track'>", "")]
		[InlineData("<img alt='Broken image'>", "Broken image")]
		[InlineData("<img src='data:image/gif;base64,R0lGODlhAQABAAAAACw=' data-src='real.jpg' alt='Lazy'>", "![Lazy](real.jpg)")]
		[InlineData("<img srcset='a.jpg 1x, b.jpg 2x' alt='Set'>", "![Set](a.jpg)")]
		[InlineData("<picture><source srcset='p.webp'><img alt='Pic'></picture>", "![Pic](p.webp)")]
		[InlineData("<a href='https://x.com'><img src='a.png' alt='A'></a>", "[![A](a.png)](https://x.com/)")]
		[InlineData("<p>Text <img src='i.png' alt='icon'> more</p>", "Text ![icon](i.png) more")]
		public void WritesImageReferences(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		[Fact]
		public void AttachesFigureCaptions()
		{
			var result = Extract("<figure><img src='a.png' alt='A'><figcaption>The caption</figcaption></figure>");

			Assert.Equal("![A](a.png)\n\nThe caption", result.Text);
			Assert.Equal("The caption", result.Images[0].Caption);
		}

		[Fact]
		public void ExtractsInlineSvgImages()
		{
			var result = Extract("<svg width=100 height=50 viewBox='0 0 100 50'><title>Sales chart</title><rect width=10 height=10/></svg><svg aria-hidden=true><path d='M0'/></svg><svg><path d='M0'/></svg>");

			Assert.Equal("![Sales chart](image1.svg)", result.Text);
			Assert.Equal("image/svg+xml", result.Images[0].ContentType);
			Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\"", Encoding.UTF8.GetString(result.Images[0].Data));
		}

		[Fact]
		public void DescribesImages()
		{
			var calls = 0;
			var result = Extract("<img src='a.png' alt='A'><p>Text <img src='a.png' alt='A'></p>", o => o.ImageDescriber = image =>
			{
				calls++;
				return "A cat on a sofa.";
			});

			Assert.Equal(1, calls);
			Assert.Equal("![A](a.png)\n> **a.png:** A cat on a sofa.\n\nText ![A](a.png) (A cat on a sofa.)", result.Text);
		}

		[Fact]
		public void OmitsImagesWhenDisabled()
		{
			var result = Extract($"<p>a <img src=\"data:image/png;base64,{Png}\" alt=\"Logo\"> b</p>", o => o.IncludeImages = false);

			Assert.Equal("a b", result.Text);
			Assert.Empty(result.Images);

			Assert.Null(Extract($"<img src=\"data:image/png;base64,{Png}\" alt=\"Logo\">", o => o.IncludeImageData = false).Images[0].Data);
		}

		#endregion

		#region Visibility and site chrome

		[Fact]
		public void SkipsHiddenContent()
		{
			var html = "<p hidden>a</p><p style='display:none'>b</p><p aria-hidden='true'>c</p><p class='sr-only'>d</p>" +
				"<p style='position:absolute;clip:rect(0 0 0 0)'>e</p><script>f</script><style>p{}</style><template><p>g</p></template><p>visible</p>";

			Assert.Equal("visible", Markdown(html));
			Assert.Equal("a\n\nb\n\nc\n\nd\n\ne\n\nvisible", Markdown(html, o => o.IncludeHiddenContent = true));
		}

		[Fact]
		public void AppliesVisibilityToDescendants()
		{
			Assert.Equal("b", Markdown("<div style='visibility:hidden'>a<span style='visibility:visible'>b</span></div>"));
			Assert.Equal("b", Markdown("<div style='font-size:0'>a<span style='font-size:12px'>b</span></div>"));
		}

		[Fact]
		public void SkipsNoScriptUnlessAsked()
		{
			// before the body, the content of <noscript> is moved out of it by the parser, as browsers do.
			var html = "<p>a</p><noscript><p>Enable JavaScript</p></noscript><p>text</p>";

			Assert.Equal("a\n\ntext", Markdown(html));
			Assert.Equal("a\n\nEnable JavaScript\n\ntext", Markdown(html, o => o.IncludeNoScript = true));
		}

		[Fact]
		public void RemovesSiteChrome()
		{
			var html = "<header><a href='/'>Logo</a></header><nav><a href='/a'>Menu</a></nav><a href='#main' class=x>Skip to content</a>" +
				"<div><h1>Title</h1><p>The content of the page.</p><div class='cookie-banner'>We use cookies</div>" +
				"<p>Text <button>Copy</button></p><h3><button aria-expanded=false>Question?</button></h3></div>" +
				"<aside>Related</aside><footer>Copyright</footer>";

			Assert.Equal("# Title\n\nThe content of the page.\n\nText\n\n### Question?", Markdown(html));
			Assert.Contains("Copyright", Markdown(html, o => o.RemoveBoilerplate = false));
		}

		[Fact]
		public void ExtractsTheMainContent()
		{
			Assert.Equal("Content of the main element.", Markdown("<div>Sidebar</div><main><p>Content of the main element.</p></main><div>More sidebar</div>"));
			Assert.Equal("Sidebar\n\nContent of the main element.", Markdown("<div>Sidebar</div><main><p>Content of the main element.</p></main>", o => o.MainContentOnly = false));
			Assert.Equal("The article text is the largest part of the page.", Markdown("<div>Menu</div><article><p>The article text is the largest part of the page.</p></article>"));
		}

		[Fact]
		public void SkipsClosedDropdowns()
		{
			var html = "<div><input type=checkbox role=button id=t><label for=t>Languages</label><ul><li>English</li><li>Deutsch</li></ul></div>" +
				"<button aria-haspopup=menu aria-controls=m>More</button><ul id=m><li>Item</li></ul><p>Text</p>";

			Assert.Equal("Languages\n\nText", Markdown(html));
		}

		#endregion

		#region Forms, details, math

		[Fact]
		public void WritesFormValues()
		{
			var html = "<form><label>Name <input value='John'></label> <input type=password value=secret> <select><option>A</option><option selected>B</option></select> " +
				"<textarea>Note</textarea> <input type=radio checked> Yes <input type=submit value=Send></form>";

			Assert.Equal("Name John B Note [x] Yes", Markdown(html));
			Assert.Equal("Name Yes", Markdown(html, o => o.IncludeForms = false));
		}

		[Fact]
		public void WritesDisclosureWidgets()
		{
			Assert.Equal("**More**\n\nHidden text", Markdown("<details><summary>More</summary><p>Hidden text</p></details>"));
			Assert.Equal("**Legend**\n\nField", Markdown("<fieldset><legend>Legend</legend>Field</fieldset>"));
		}

		[Theory]
		[InlineData("<math><mfrac><mi>a</mi><mi>b</mi></mfrac></math>", "$\\frac{a}{b}$")]
		[InlineData("<math display=block><msup><mi>x</mi><mn>2</mn></msup><mo>+</mo><msqrt><mi>y</mi></msqrt></math>", "$$x^{2}+\\sqrt{y}$$")]
		[InlineData("<math><semantics><mi>x</mi><annotation encoding='application/x-tex'>\\alpha</annotation></semantics></math>", "$\\alpha$")]
		[InlineData("<p>where <span style='display:none'><math><semantics><mi>x</mi><annotation encoding='application/x-tex'>{\\displaystyle x}</annotation></semantics></math></span><img src='x.svg' aria-hidden='true' alt='x'> is</p>", "where $x$ is")]
		public void WritesMathAsLatex(string html, string expected)
		{
			Assert.Equal(expected, Markdown(html));
		}

		#endregion

		#region Pages

		[Fact]
		public void SplitsPagesAtPrintBreaks()
		{
			var result = Extract("<p>A</p><p style='page-break-before:always'>B</p><div style='break-after:page'>C</div><p>D</p>");

			Assert.Equal(3, result.PageCount);
			Assert.Equal("A", result.Pages[0].Text);
			Assert.Equal("B\n\nC", result.Pages[1].Text);
			Assert.Equal("<!-- Page 1 -->\n\nA\n\n<!-- Page 2 -->\n\nB\n\nC\n\n<!-- Page 3 -->\n\nD", result.Text);
		}

		[Fact]
		public void RecognizesWordPageBreaks()
		{
			var html = "<style>.brk { page-break-after: always }</style><p>A</p><br clear=all style='mso-special-character:line-break;page-break-before:always'><p>B</p><div class=brk></div><p>C</p>";
			var result = Extract(html, o => o.PageHeaderFormat = "Page {0} of {1}");

			Assert.Equal("Page 1 of 3\n\nA\n\nPage 2 of 3\n\nB\n\nPage 3 of 3\n\nC", result.Text);
		}

		[Fact]
		public void IgnoresPageBreaksInsideTables()
		{
			Assert.Equal(1, Extract("<table><tr><th>A</th></tr><tr><td style='page-break-before:always'>1</td></tr></table>").PageCount);
		}

		#endregion

		#region Metadata

		[Fact]
		public void ReadsTheMetadata()
		{
			var html = "<html lang=en-US><head><title>The title</title>" +
				"<meta name=description content='The description'><meta name=author content='Jane Doe'><meta name=keywords content='a, b'>" +
				"<meta property=og:site_name content=Site><meta property=article:published_time content='2026-01-02T03:04:05Z'>" +
				"<link rel=canonical href='https://site.com/page'><meta name=generator content=Gen></head><body>x</body></html>";

			var metadata = Extract(html).Metadata;

			Assert.Equal("The title", metadata.Title);
			Assert.Equal("The description", metadata.Description);
			Assert.Equal("Jane Doe", metadata.Author);
			Assert.Equal("a, b", metadata.Keywords);
			Assert.Equal("Site", metadata.SiteName);
			Assert.Equal("en-US", metadata.Language);
			Assert.Equal("https://site.com/page", metadata.CanonicalUrl);
			Assert.Equal("Gen", metadata.Generator);
			Assert.Equal(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), metadata.Published);
			Assert.Equal("Jane Doe", metadata.Properties["author"]);
		}

		#endregion

		#region MHTML

		[Fact]
		public void ReadsMhtmlArchives()
		{
			var mhtml = "From: <Saved by Blink>\r\n" +
				"Subject: Test\r\n" +
				"MIME-Version: 1.0\r\n" +
				"Content-Type: multipart/related;\r\n\ttype=\"text/html\";\r\n\tboundary=\"----BOUNDARY\"\r\n" +
				"\r\n" +
				"------BOUNDARY\r\n" +
				"Content-Type: text/html; charset=\"utf-8\"\r\n" +
				"Content-Transfer-Encoding: quoted-printable\r\n" +
				"Content-Location: https://example.com/dir/page.html\r\n" +
				"\r\n" +
				"<html><head><style>.b { font-weight: bold }</style><link rel=3Dstylesheet href=3D\"s.css\"></head><body><p>Caf=C3=A9 <span class=3Db>bold</span> <span class=3Di>it</span></p>=\r\n" +
				"<img src=3D\"img/a.png\" alt=3D\"A\"><a href=3D\"other.html\">link</a></body></html>\r\n" +
				"------BOUNDARY\r\n" +
				"Content-Type: image/png\r\n" +
				"Content-Transfer-Encoding: base64\r\n" +
				"Content-Location: https://example.com/dir/img/a.png\r\n" +
				"\r\n" +
				Png + "\r\n" +
				"------BOUNDARY\r\n" +
				"Content-Type: text/css\r\n" +
				"Content-Location: https://example.com/dir/s.css\r\n" +
				"\r\n" +
				".i { font-style: italic }\r\n" +
				"------BOUNDARY--\r\n";

			var result = Extract(Encoding.ASCII.GetBytes(mhtml));

			Assert.Equal("Café **bold** *it*\n\n![A](image1.png)[link](https://example.com/dir/other.html)", result.Text);
			Assert.Equal(Convert.FromBase64String(Png), result.Images[0].Data);
		}

		#endregion
	}
}

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

using System.Linq;
using System.Text;
using Wisej.AI.Html.Dom;
using Xunit;

namespace Wisej.AI.Html.Tests
{
	/// <summary>
	/// Tests of the detection of the character encoding of documents read from bytes.
	/// </summary>
	public class EncodingTests : TestBase
	{
		/// <summary>The bytes of "café €" in windows-1252.</summary>
		private static readonly byte[] Windows1252Text = { 0x63, 0x61, 0x66, 0xE9, 0x20, 0x80 };

		[Fact]
		public void DetectsUtf8WithoutDeclaration()
		{
			var document = HtmlDocument.Load(Encoding.UTF8.GetBytes("<p>café €</p>"));

			Assert.Equal("café €", document.Body.TextContent);
			Assert.Equal("utf-8", document.Encoding.WebName);
		}

		[Fact]
		public void FallsBackToWindows1252()
		{
			var document = HtmlDocument.Load(Concat("<p>", Windows1252Text, "</p>"));

			Assert.Equal("café €", document.Body.TextContent);
			Assert.Equal("windows-1252", document.Encoding.WebName);
		}

		[Theory]
		[InlineData("<meta charset=\"iso-8859-1\">")]
		[InlineData("<meta charset=windows-1252>")]
		[InlineData("<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=latin1\">")]
		[InlineData("<!-- <meta charset=utf-8> --><meta charset=\"us-ascii\">")]
		public void ReadsTheMetaDeclaration(string meta)
		{
			var document = HtmlDocument.Load(Concat("<html><head>" + meta + "</head><body><p>", Windows1252Text, "</p>"));

			Assert.Equal("café €", document.Body.TextContent);
		}

		[Fact]
		public void HonorsTheByteOrderMark()
		{
			var utf16 = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("<meta charset=iso-8859-1><p>π</p>")).ToArray();

			Assert.Equal("π", HtmlDocument.Load(utf16).Body.TextContent);
		}

		[Fact]
		public void HonorsTheTransportEncoding()
		{
			var options = new HtmlParserOptions { TransportEncoding = "windows-1252" };
			var document = HtmlDocument.Load(Concat("<meta charset=utf-8><p>", Windows1252Text, "</p>"), options);

			Assert.Equal("café €", document.Body.TextContent);
		}

		[Fact]
		public void ReadsTheXmlDeclaration()
		{
			var document = HtmlDocument.Load(Concat("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><html><body><p>", Windows1252Text, "</p>"));

			Assert.Equal("café €", document.Body.TextContent);
		}

		[Fact]
		public void ReportsTheEncodingInTheMetadata()
		{
			var result = Extract(Encoding.UTF8.GetBytes("<p>x</p>"));

			Assert.Equal("utf-8", result.Metadata.Encoding);
		}

		/// <summary>
		/// Concatenates ASCII markup and raw bytes.
		/// </summary>
		/// <param name="before">The markup before the bytes.</param>
		/// <param name="bytes">The bytes.</param>
		/// <param name="after">The markup after the bytes.</param>
		/// <returns>The document bytes.</returns>
		private static byte[] Concat(string before, byte[] bytes, string after)
		{
			return Encoding.ASCII.GetBytes(before).Concat(bytes).Concat(Encoding.ASCII.GetBytes(after)).ToArray();
		}
	}
}

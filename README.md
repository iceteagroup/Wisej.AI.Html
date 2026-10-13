# Wisej.AI.Html

Standards compliant HTML parsing and high fidelity extraction of HTML documents (HTML, XHTML and MHTML web archives) into **Markdown or plain text**, built to feed language model context and retrieval pipelines in [Wisej.AI](https://wisej.com). It replaces HtmlAgilityPack in Wisej.AI and has **no dependencies**.

```csharp
var result = new HtmlExtractor().Extract("page.html");

Console.WriteLine(result.Text);                    // Markdown
Console.WriteLine(result.Metadata.Title);          // title, description, author, language, dates, ...
foreach (var image in result.Images)               // embedded images with their bytes, external ones with their URL
    if (image.Data != null)
        File.WriteAllBytes(image.FileName, image.Data);

string markdown = HtmlExtractor.ToMarkdown("<h1>Hello</h1><p>A <b>bold</b> move.</p>");
```

## What is extracted

| Content | Output |
|---|---|
| Headings | `#` headings from `h1`-`h6` and `role="heading"` |
| Character formatting | `**bold**`, `*italic*`, `~~strike~~`, `<sup>`/`<sub>`, `` `code` ``, from the elements **and the style sheets** (`font-weight`, `font-style`, `font-family`, `vertical-align`, `text-decoration`). Emphasis covering a whole heading or table cell is styling and is dropped. |
| Paragraphs and white space | Block boundaries as paragraphs, `<br>` as line breaks, white space collapsed across elements exactly as CSS does (`white-space: pre`/`pre-line` respected) |
| Lists | Nested lists with the labels browsers display: `start`, `reversed`, `value`, `type` and `list-style-type` (`3.`, `c.`, `iv.`), task lists (`[x]`), definition lists, and the paragraph lists written by Word and Outlook |
| Tables | Pipe tables with `colspan`/`rowspan` resolved (repeated by default), multi-row headers combined (`Group / Column`), `thead`/`tfoot` ordered, empty rows and columns and duplicated columns removed, captions; HTML tables for nested tables; layout tables (single row or column, `role="presentation"`, tables of tables) written as regular content |
| Images | `![alt](image1.png)` for embedded images (`data:` URIs, MHTML parts, inline SVG) whose bytes are returned in `Result.Images`, `![alt](https://...)` for external ones; `srcset`, `<picture>` and lazy loading attributes; figure captions; an optional `ImageDescriber` callback (vision model, OCR) called once per distinct picture. Decorative images, spacers and tracking pixels are skipped. |
| Links | `[text](url)`, resolved against the base URL; script and same-page anchor links as plain text; the link of a "card" kept on its title only; all the links in `Result.Links` |
| Code | Fenced code blocks with the language detected from highlighters (`language-x`, `lang-x`, `highlight-source-x`, `brush:`), one line per highlighter line |
| Quotations, rules, details | `>` quotes, `---`, `<summary>`/`<legend>` as bold paragraphs |
| Equations | MathML as LaTeX (`$\frac{a}{b}$`, `$$...$$`), using the TeX annotation of MediaWiki, KaTeX and MathJax when present |
| Forms | Check boxes as `[x]`/`[ ]`, field values, selected options, text areas; buttons, passwords and hidden fields skipped |
| Ruby, quotes | `漢(kan)`, `“quoted”` |
| Hidden content | Removed: `hidden`, `display: none`, `visibility: hidden`, zero font size, `aria-hidden`, screen-reader-only patterns, closed dropdown menus, `<script>`, `<style>`, `<template>`, `<noscript>` |
| Web page chrome | Removed: navigation, page banners and footers, sidebars, skip links, cookie banners, print-hidden elements, edit links; only `<main>`/the single `<article>` when the page has one |
| Pages | Split at the CSS print page breaks (`break-before`, `page-break-after`, ...), as written by Word and report generators |
| Metadata | Title, description, keywords, author, site name, language, canonical URL, generator, publication and modification dates, every `<meta>` property |
| Encodings | Detected as browsers do: byte order mark, transport charset, `<meta charset>` prescan, XML declaration, UTF-8 validation, windows-1252 fallback |

A Word document saved as a web page, for example, becomes (excerpt):

```markdown
<!-- Page 1 -->

# Quarterly Report

This report covers **revenue** and *costs* for Q3.

## Figures

| Region | Q3 | Q4 |
| --- | --- | --- |
| North | 100 | 120 |
| South | 80 | 90 |
| Total | 180 (both quarters) | 180 (both quarters) |

1. First step
2. Second step

<!-- Page 2 -->

## Appendix

Text on the second page.
```

## The parser

The extraction is built on a complete implementation of the parsing algorithm of the [WHATWG HTML standard](https://html.spec.whatwg.org/multipage/parsing.html): the tokenizer state machine, all the insertion modes of the tree builder (adoption agency, foster parenting, template contents, frameset), foreign content (SVG and MathML with their namespaces and case fixes), the fragment parsing algorithm, the 2025 customizable `<select>` rules, the 2,231 named character references, and the encoding sniffing algorithm. Every input, however broken, produces the same tree as a modern browser.

| Suite ([html5lib-tests](https://github.com/html5lib/html5lib-tests)) | Wisej.AI.Html | HtmlAgilityPack 1.11.67 |
|---|---|---|
| Tokenizer | **7,032 / 7,032** (100%) | n/a |
| Tree construction | **1,792 / 1,792** (100%) | 6 / 1,600 (0.4%) |

It is also faster: parsing 10 real pages (Wikipedia, MDN, Microsoft Learn, GitHub, BBC, the WHATWG specification; 3.3 MB of HTML) takes **79 ms** against 252 ms for HtmlAgilityPack, 3.2 times faster (.NET 8, Release).

The document object model is public:

```csharp
using Wisej.AI.Html.Dom;

var document = HtmlDocument.Load(stream);               // or Parse(string), Load(path), Load(bytes)
Console.WriteLine(document.Title);

foreach (var cell in document.QuerySelectorAll("table.prices td:nth-child(2)"))
    Console.WriteLine(cell.TextContent);

var fragment = HtmlDocument.ParseFragment("<td>a</td><td>b</td>", "tr");
var html = document.Body.OuterHtml;
```

| Type | Members |
|---|---|
| `HtmlDocument` | `Parse`, `Load`, `ParseFragment`, `DocumentElement`, `Head`, `Body`, `Title`, `Doctype`, `QuirksMode`, `Encoding`, `GetElementById`, `CreateElement`, `CreateTextNode`, `CreateComment` |
| `HtmlNode` | `ParentNode`, `FirstChild`, `LastChild`, `NextSibling`, `PreviousSibling`, `ChildNodes`, `Children`, `Descendants()`, `Ancestors()`, `TextContent`, `OuterHtml`, `InnerHtml`, `QuerySelector`, `QuerySelectorAll`, `GetElementsByTagName`, `AppendChild`, `InsertBefore`, `RemoveChild`, `Remove` |
| `HtmlElement` | `Name`, `NamespaceUri`, `Attributes`, `GetAttribute`, `SetAttribute`, `HasAttribute`, `RemoveAttribute`, `Id`, `ClassList`, `HasClass`, `Content` (templates), `Matches`, `Closest` |

Selectors support type, universal, id, class and attribute selectors (all operators and the `i` flag), the four combinators, the structural pseudo-classes (`:first-child`, `:nth-child(2n+1)`, `:nth-of-type()`, `:empty`, `:root`, ...), `:not()`, `:is()`, `:where()` and `:has()`.

### Why not HtmlAgilityPack

HtmlAgilityPack predates the HTML standard parsing algorithm and builds a different tree than browsers: it does not imply `<html>`, `<head>`, `<body>` and `<tbody>`, treats `<p>` as an empty element, does not repair misnested formatting (`<b>1<i>2</b>3</i>`) or misplaced table content, knows 257 of the 2,231 character references, and detects encodings with exceptions. It has no notion of CSS or rendering, so text extracted from it includes hidden content and loses the document structure. Its source was studied for its API; none of its code is used.

## Options

| Option | Default | Description |
|---|---|---|
| `Format` | `Markdown` | `Markdown` or `PlainText` |
| `PageHeaderFormat` | `<!-- Page {0} -->` | Line introducing each page in `Text` when there is more than one; `{0}` page, `{1}` total; empty for none |
| `IncludeTitle` | `true` | Writes the document title as a heading when the content does not start with it |
| `MainContentOnly` | `true` | Extracts only `<main>`, `role="main"` or the single `<article>` of a web page |
| `RemoveBoilerplate` | `true` | Removes navigation, page banners and footers, sidebars, skip links, cookie banners |
| `IncludeHiddenContent` | `false` | Includes content that is not rendered |
| `ApplyStyleSheets` | `true` | Applies `<style>`, the style sheets of MHTML archives and `style` attributes |
| `Tables` | `Auto` | `Auto` (pipe, HTML when nested), `Markdown`, `Html` |
| `MergedCells` | `Repeat` | Repeat merged cell content in every covered cell, or leave them `Empty` |
| `UnwrapLayoutTables` | `true` | Writes layout tables as regular content |
| `IncludeFormatting`, `IncludeHyperlinks`, `IncludeImages`, `IncludeImageData`, `IncludeForms`, `IncludeMath`, `IncludeNoScript` | | Fine grained control of the content |
| `SkipDecorativeImages`, `MinimumImageSize` | `true`, `0` | Filter images that carry no information |
| `ImageDescriber` | `null` | `Func<HtmlImage, string>` that describes an image |
| `BaseUri` | `null` | URL of the document, to resolve relative links and images (`<base href>` is applied too) |
| `Encoding` | `null` | Overrides the detected encoding |

## Architecture

```
src/Wisej.AI.Html
  HtmlExtractor              Public entry point
  Dom/                       Public document object model, serializer
  Parsing/                   Tokenizer, tree builder (one file per group of insertion modes),
                             character references, encoding sniffing
  Css/                       Selector engine, lenient style sheet parser, cascade
  Mhtml/                     MHTML (RFC 2557) archive reader
  Content/                   Computed styles, DOM walk, content model, lists, tables, images,
                             MathML to LaTeX, metadata, Word list recognition
  Rendering/                 Table grid, Markdown and plain text renderers
src/Wisej.AI.Html.Package    Wisej-4-AI-Html NuGet package
tests/Wisej.AI.Html.Tests    Unit tests
```

The library targets **.NET Standard 2.0** and **.NET 8**, so it runs on .NET Framework 4.6.2+ and every modern .NET, on Windows, Linux, macOS, Android and iOS. It has no dependencies, not even for the windows-1252 fallback encoding. The assembly is strong-named with `extension.snk`, so it can be referenced by the strong-named Wisej.AI assembly.

## Building and testing

```powershell
dotnet build Wisej.AI.Html.sln -c Release
dotnet test tests/Wisej.AI.Html.Tests
```

The Release build writes the `Wisej-4-AI-Html` package to `artifacts\` and copies the assemblies to `artifacts\bin\`.

## Limitations

- Scripts are not executed: content rendered by JavaScript after loading is not present (as with any static HTML processing).
- External style sheets are not downloaded; the `<style>` elements, the `style` attributes and the style sheets stored in MHTML archives are applied. Rules that depend on the viewport or on user interaction (`@media (max-width)`, `:hover`) are ignored.
- Layout is not computed: the visual order of flex and grid items, CSS generated content (`::before`) and absolutely positioned text are not taken into account.
- External images are referenced, not downloaded: the `ImageDescriber` receives their URL.

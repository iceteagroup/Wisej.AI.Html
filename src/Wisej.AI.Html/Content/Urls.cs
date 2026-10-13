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

namespace Wisej.AI.Html.Content
{
	/// <summary>
	/// URL helpers that never throw on the malformed URLs found in documents.
	/// </summary>
	internal static class Urls
	{
		/// <summary>
		/// Decodes the percent escapes of a URL, keeping the text as written when the escapes are malformed.
		/// </summary>
		/// <param name="value">The escaped text.</param>
		/// <returns>The unescaped text.</returns>
		public static string Unescape(string value)
		{
			try
			{
				return Uri.UnescapeDataString(value);
			}
			catch (UriFormatException)
			{
				return value;
			}
		}
	}
}

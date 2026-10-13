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

using System.Globalization;
using System.Text;

namespace Wisej.AI.Html.Content
{
	/// <summary>
	/// Formats list markers as browsers render them (CSS Counter Styles).
	/// </summary>
	internal static class ListMarkers
	{
		/// <summary>The lowercase Greek letters used by <c>lower-greek</c>.</summary>
		private const string Greek = "αβγδεζηθικλμνξοπρστυφχψω";

		/// <summary>
		/// Determines whether a token is a list style type supported by the formatter.
		/// </summary>
		/// <param name="type">The lowercase token.</param>
		/// <returns><c>true</c> for a known type.</returns>
		public static bool IsKnownType(string type)
		{
			switch (type)
			{
				case "none":
				case "disc":
				case "circle":
				case "square":
				case "decimal":
				case "decimal-leading-zero":
				case "lower-alpha":
				case "lower-latin":
				case "upper-alpha":
				case "upper-latin":
				case "lower-roman":
				case "upper-roman":
				case "lower-greek":
					return true;

				default:
					return false;
			}
		}

		/// <summary>
		/// Determines whether a list style type is a bullet.
		/// </summary>
		/// <param name="type">The list style type.</param>
		/// <returns><c>true</c> for <c>disc</c>, <c>circle</c>, <c>square</c>, <c>none</c> and unknown types.</returns>
		public static bool IsBullet(string type)
		{
			switch (type)
			{
				case "decimal":
				case "decimal-leading-zero":
				case "lower-alpha":
				case "lower-latin":
				case "upper-alpha":
				case "upper-latin":
				case "lower-roman":
				case "upper-roman":
				case "lower-greek":
					return false;

				default:
					return true;
			}
		}

		/// <summary>
		/// Formats the marker of an ordered list item.
		/// </summary>
		/// <param name="type">The list style type.</param>
		/// <param name="value">The ordinal value of the item.</param>
		/// <returns>The marker, such as <c>3.</c>, <c>c.</c> or <c>iii.</c>.</returns>
		public static string Format(string type, int value)
		{
			string text;

			switch (type)
			{
				case "lower-alpha":
				case "lower-latin":
					text = value > 0 ? Alphabetic(value, "abcdefghijklmnopqrstuvwxyz") : Decimal(value);
					break;

				case "upper-alpha":
				case "upper-latin":
					text = value > 0 ? Alphabetic(value, "ABCDEFGHIJKLMNOPQRSTUVWXYZ") : Decimal(value);
					break;

				case "lower-roman":
					text = value > 0 && value < 4000 ? Roman(value).ToLowerInvariant() : Decimal(value);
					break;

				case "upper-roman":
					text = value > 0 && value < 4000 ? Roman(value) : Decimal(value);
					break;

				case "lower-greek":
					text = value > 0 ? Alphabetic(value, Greek) : Decimal(value);
					break;

				case "decimal-leading-zero":
					text = value >= 0 && value < 10 ? "0" + Decimal(value) : Decimal(value);
					break;

				default:
					text = Decimal(value);
					break;
			}

			return text + ".";
		}

		/// <summary>
		/// Formats a number in decimal.
		/// </summary>
		/// <param name="value">The number.</param>
		/// <returns>The digits.</returns>
		private static string Decimal(int value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Formats a number with an alphabetic system: a, b, ..., z, aa, ab, ...
		/// </summary>
		/// <param name="value">The positive number.</param>
		/// <param name="symbols">The alphabet.</param>
		/// <returns>The letters.</returns>
		private static string Alphabetic(int value, string symbols)
		{
			var sb = new StringBuilder();
			while (value > 0)
			{
				value--;
				sb.Insert(0, symbols[value % symbols.Length]);
				value /= symbols.Length;
			}

			return sb.ToString();
		}

		/// <summary>
		/// Formats a number with uppercase Roman numerals.
		/// </summary>
		/// <param name="value">The number, from 1 to 3999.</param>
		/// <returns>The numerals.</returns>
		private static string Roman(int value)
		{
			int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
			string[] numerals = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

			var sb = new StringBuilder();
			for (var i = 0; i < values.Length; i++)
			{
				while (value >= values[i])
				{
					sb.Append(numerals[i]);
					value -= values[i];
				}
			}

			return sb.ToString();
		}
	}
}

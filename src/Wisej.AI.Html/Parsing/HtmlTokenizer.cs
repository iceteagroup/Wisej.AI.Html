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
using System.Collections.Generic;
using System.Text;

namespace Wisej.AI.Html.Parsing
{
	/// <summary>
	/// The states of the <see cref="HtmlTokenizer"/> (HTML standard, 13.2.5).
	/// </summary>
	/// <remarks>
	/// The character reference states are not listed: references are decoded by
	/// <see cref="CharacterReferences.Consume"/> with an equivalent lookahead.
	/// </remarks>
	internal enum TokenizerState
	{
		/// <summary>13.2.5.1 Data state.</summary>
		Data,
		/// <summary>13.2.5.2 RCDATA state.</summary>
		Rcdata,
		/// <summary>13.2.5.3 RAWTEXT state.</summary>
		Rawtext,
		/// <summary>13.2.5.4 Script data state.</summary>
		ScriptData,
		/// <summary>13.2.5.5 PLAINTEXT state.</summary>
		Plaintext,
		/// <summary>13.2.5.6 Tag open state.</summary>
		TagOpen,
		/// <summary>13.2.5.7 End tag open state.</summary>
		EndTagOpen,
		/// <summary>13.2.5.8 Tag name state.</summary>
		TagName,
		/// <summary>13.2.5.9 RCDATA less-than sign state.</summary>
		RcdataLessThanSign,
		/// <summary>13.2.5.10 RCDATA end tag open state.</summary>
		RcdataEndTagOpen,
		/// <summary>13.2.5.11 RCDATA end tag name state.</summary>
		RcdataEndTagName,
		/// <summary>13.2.5.12 RAWTEXT less-than sign state.</summary>
		RawtextLessThanSign,
		/// <summary>13.2.5.13 RAWTEXT end tag open state.</summary>
		RawtextEndTagOpen,
		/// <summary>13.2.5.14 RAWTEXT end tag name state.</summary>
		RawtextEndTagName,
		/// <summary>13.2.5.15 Script data less-than sign state.</summary>
		ScriptDataLessThanSign,
		/// <summary>13.2.5.16 Script data end tag open state.</summary>
		ScriptDataEndTagOpen,
		/// <summary>13.2.5.17 Script data end tag name state.</summary>
		ScriptDataEndTagName,
		/// <summary>13.2.5.18 Script data escape start state.</summary>
		ScriptDataEscapeStart,
		/// <summary>13.2.5.19 Script data escape start dash state.</summary>
		ScriptDataEscapeStartDash,
		/// <summary>13.2.5.20 Script data escaped state.</summary>
		ScriptDataEscaped,
		/// <summary>13.2.5.21 Script data escaped dash state.</summary>
		ScriptDataEscapedDash,
		/// <summary>13.2.5.22 Script data escaped dash dash state.</summary>
		ScriptDataEscapedDashDash,
		/// <summary>13.2.5.23 Script data escaped less-than sign state.</summary>
		ScriptDataEscapedLessThanSign,
		/// <summary>13.2.5.24 Script data escaped end tag open state.</summary>
		ScriptDataEscapedEndTagOpen,
		/// <summary>13.2.5.25 Script data escaped end tag name state.</summary>
		ScriptDataEscapedEndTagName,
		/// <summary>13.2.5.26 Script data double escape start state.</summary>
		ScriptDataDoubleEscapeStart,
		/// <summary>13.2.5.27 Script data double escaped state.</summary>
		ScriptDataDoubleEscaped,
		/// <summary>13.2.5.28 Script data double escaped dash state.</summary>
		ScriptDataDoubleEscapedDash,
		/// <summary>13.2.5.29 Script data double escaped dash dash state.</summary>
		ScriptDataDoubleEscapedDashDash,
		/// <summary>13.2.5.30 Script data double escaped less-than sign state.</summary>
		ScriptDataDoubleEscapedLessThanSign,
		/// <summary>13.2.5.31 Script data double escape end state.</summary>
		ScriptDataDoubleEscapeEnd,
		/// <summary>13.2.5.32 Before attribute name state.</summary>
		BeforeAttributeName,
		/// <summary>13.2.5.33 Attribute name state.</summary>
		AttributeName,
		/// <summary>13.2.5.34 After attribute name state.</summary>
		AfterAttributeName,
		/// <summary>13.2.5.35 Before attribute value state.</summary>
		BeforeAttributeValue,
		/// <summary>13.2.5.36 Attribute value (double-quoted) state.</summary>
		AttributeValueDoubleQuoted,
		/// <summary>13.2.5.37 Attribute value (single-quoted) state.</summary>
		AttributeValueSingleQuoted,
		/// <summary>13.2.5.38 Attribute value (unquoted) state.</summary>
		AttributeValueUnquoted,
		/// <summary>13.2.5.39 After attribute value (quoted) state.</summary>
		AfterAttributeValueQuoted,
		/// <summary>13.2.5.40 Self-closing start tag state.</summary>
		SelfClosingStartTag,
		/// <summary>13.2.5.41 Bogus comment state.</summary>
		BogusComment,
		/// <summary>13.2.5.42 Markup declaration open state.</summary>
		MarkupDeclarationOpen,
		/// <summary>13.2.5.43 Comment start state.</summary>
		CommentStart,
		/// <summary>13.2.5.44 Comment start dash state.</summary>
		CommentStartDash,
		/// <summary>13.2.5.45 Comment state.</summary>
		Comment,
		/// <summary>13.2.5.46 Comment less-than sign state.</summary>
		CommentLessThanSign,
		/// <summary>13.2.5.47 Comment less-than sign bang state.</summary>
		CommentLessThanSignBang,
		/// <summary>13.2.5.48 Comment less-than sign bang dash state.</summary>
		CommentLessThanSignBangDash,
		/// <summary>13.2.5.49 Comment less-than sign bang dash dash state.</summary>
		CommentLessThanSignBangDashDash,
		/// <summary>13.2.5.50 Comment end dash state.</summary>
		CommentEndDash,
		/// <summary>13.2.5.51 Comment end state.</summary>
		CommentEnd,
		/// <summary>13.2.5.52 Comment end bang state.</summary>
		CommentEndBang,
		/// <summary>13.2.5.53 DOCTYPE state.</summary>
		Doctype,
		/// <summary>13.2.5.54 Before DOCTYPE name state.</summary>
		BeforeDoctypeName,
		/// <summary>13.2.5.55 DOCTYPE name state.</summary>
		DoctypeName,
		/// <summary>13.2.5.56 After DOCTYPE name state.</summary>
		AfterDoctypeName,
		/// <summary>13.2.5.57 After DOCTYPE public keyword state.</summary>
		AfterDoctypePublicKeyword,
		/// <summary>13.2.5.58 Before DOCTYPE public identifier state.</summary>
		BeforeDoctypePublicIdentifier,
		/// <summary>13.2.5.59 DOCTYPE public identifier (double-quoted) state.</summary>
		DoctypePublicIdentifierDoubleQuoted,
		/// <summary>13.2.5.60 DOCTYPE public identifier (single-quoted) state.</summary>
		DoctypePublicIdentifierSingleQuoted,
		/// <summary>13.2.5.61 After DOCTYPE public identifier state.</summary>
		AfterDoctypePublicIdentifier,
		/// <summary>13.2.5.62 Between DOCTYPE public and system identifiers state.</summary>
		BetweenDoctypePublicAndSystemIdentifiers,
		/// <summary>13.2.5.63 After DOCTYPE system keyword state.</summary>
		AfterDoctypeSystemKeyword,
		/// <summary>13.2.5.64 Before DOCTYPE system identifier state.</summary>
		BeforeDoctypeSystemIdentifier,
		/// <summary>13.2.5.65 DOCTYPE system identifier (double-quoted) state.</summary>
		DoctypeSystemIdentifierDoubleQuoted,
		/// <summary>13.2.5.66 DOCTYPE system identifier (single-quoted) state.</summary>
		DoctypeSystemIdentifierSingleQuoted,
		/// <summary>13.2.5.67 After DOCTYPE system identifier state.</summary>
		AfterDoctypeSystemIdentifier,
		/// <summary>13.2.5.68 Bogus DOCTYPE state.</summary>
		BogusDoctype,
		/// <summary>13.2.5.69 CDATA section state.</summary>
		CdataSection,
		/// <summary>13.2.5.70 CDATA section bracket state.</summary>
		CdataSectionBracket,
		/// <summary>13.2.5.71 CDATA section end state.</summary>
		CdataSectionEnd,
	}

	/// <summary>
	/// Splits HTML into tokens following the tokenization algorithm of the HTML standard (13.2.5).
	/// </summary>
	/// <remarks>
	/// <para>
	/// The input is normalized first (CR and CRLF become LF, 13.2.3.5), then consumed one state at a time.
	/// Runs of text are scanned in bulk, so the per-character state machine only runs on markup.
	/// </para>
	/// <para>
	/// Parse errors are not reported: the standard defines the recovery for every one of them, and the
	/// recovered tokens are exactly what browsers produce.
	/// </para>
	/// </remarks>
	internal sealed class HtmlTokenizer
	{
		/// <summary>The value returned by <see cref="Next"/> at the end of the input.</summary>
		private const int EOF = -1;

		// the normalized input and the position of the next character.
		private readonly string _input;
		private readonly int _length;
		private int _pos;

		// receives the tokens.
		private readonly IHtmlTokenSink _sink;

		// pending characters, delivered as one token before the next token.
		private readonly StringBuilder _text = new StringBuilder();

		// the tag being built.
		private readonly StringBuilder _tagName = new StringBuilder();
		private bool _isEndTag;
		private bool _selfClosing;
		private List<HtmlTokenAttribute> _attributes = new List<HtmlTokenAttribute>();
		private readonly StringBuilder _attributeName = new StringBuilder();
		private readonly StringBuilder _attributeValue = new StringBuilder();
		private bool _hasAttribute;

		// the comment or doctype being built.
		private readonly StringBuilder _comment = new StringBuilder();
		private readonly StringBuilder _doctypeName = new StringBuilder();
		private StringBuilder _publicId;
		private StringBuilder _systemId;
		private bool _hasDoctypeName;
		private bool _forceQuirks;

		// the temporary buffer of the end tag and script escape states.
		private readonly StringBuilder _buffer = new StringBuilder();

		// set when the end of file token has been delivered.
		private bool _eofEmitted;

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlTokenizer"/> class.
		/// </summary>
		/// <param name="input">The markup.</param>
		/// <param name="sink">Receives the tokens.</param>
		public HtmlTokenizer(string input, IHtmlTokenSink sink)
		{
			_input = NormalizeNewlines(input);
			_length = _input.Length;
			_sink = sink;
		}

		#region Properties

		/// <summary>
		/// Gets or sets the current state. The tree builder switches it after tags such as <c>&lt;script&gt;</c> and <c>&lt;textarea&gt;</c>.
		/// </summary>
		public TokenizerState State { get; set; }

		/// <summary>
		/// Gets or sets the name of the last start tag emitted, used to recognize the appropriate end tag
		/// of RCDATA, RAWTEXT and script data.
		/// </summary>
		public string LastStartTagName { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Tokenizes the whole input, delivering the tokens to the sink, the last one being the end of file.
		/// </summary>
		public void Run()
		{
			while (!_eofEmitted)
				Step();
		}

		/// <summary>
		/// Normalizes newlines: CRLF and CR become LF (13.2.3.5).
		/// </summary>
		/// <param name="input">The input.</param>
		/// <returns>The normalized input.</returns>
		private static string NormalizeNewlines(string input)
		{
			if (input.IndexOf('\r') < 0)
				return input;

			var sb = new StringBuilder(input.Length);
			for (var i = 0; i < input.Length; i++)
			{
				var c = input[i];
				if (c == '\r')
				{
					sb.Append('\n');
					if (i + 1 < input.Length && input[i + 1] == '\n')
						i++;
				}
				else
				{
					sb.Append(c);
				}
			}

			return sb.ToString();
		}

		#endregion

		#region Input

		/// <summary>
		/// Consumes the next character.
		/// </summary>
		/// <returns>The character, or <see cref="EOF"/>. The position advances in both cases, so <see cref="Reconsume"/> always applies.</returns>
		private int Next()
		{
			return _pos < _length ? _input[_pos++] : EofAdvance();
		}

		/// <summary>
		/// Advances past the end of the input.
		/// </summary>
		/// <returns><see cref="EOF"/>.</returns>
		private int EofAdvance()
		{
			_pos++;
			return EOF;
		}

		/// <summary>
		/// Pushes the last consumed character back, to be consumed again in another state.
		/// </summary>
		private void Reconsume()
		{
			_pos--;
		}

		/// <summary>
		/// Determines whether the input continues with a string, optionally ignoring ASCII case.
		/// </summary>
		/// <param name="value">The expected string, lowercase when <paramref name="ignoreCase"/> is set.</param>
		/// <param name="ignoreCase">Whether ASCII case is ignored.</param>
		/// <returns><c>true</c> when the next characters match.</returns>
		private bool LookingAt(string value, bool ignoreCase)
		{
			if (_pos + value.Length > _length)
				return false;

			for (var i = 0; i < value.Length; i++)
			{
				var c = _input[_pos + i];
				if (ignoreCase)
					c = ToLower(c);

				if (c != value[i])
					return false;
			}

			return true;
		}

		/// <summary>
		/// Returns the ASCII lowercase of a character.
		/// </summary>
		/// <param name="c">The character.</param>
		/// <returns>The character, lowercased when it is an ASCII uppercase letter.</returns>
		private static char ToLower(char c)
		{
			return c >= 'A' && c <= 'Z' ? (char)(c + 0x20) : c;
		}

		/// <summary>
		/// Determines whether a character is ASCII white space for the tokenizer (tab, LF, FF, space).
		/// </summary>
		/// <param name="c">The character.</param>
		/// <returns><c>true</c> for white space.</returns>
		private static bool IsWhitespace(int c)
		{
			return c == ' ' || c == '\n' || c == '\t' || c == '\f';
		}

		/// <summary>
		/// Determines whether a character is an ASCII letter.
		/// </summary>
		/// <param name="c">The character.</param>
		/// <returns><c>true</c> for <c>[A-Za-z]</c>.</returns>
		private static bool IsAsciiAlpha(int c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
		}

		#endregion

		#region Emission

		/// <summary>
		/// Delivers the pending characters as a single token.
		/// </summary>
		private void FlushText()
		{
			if (_text.Length == 0)
				return;

			var token = HtmlToken.Characters(_text.ToString());
			_text.Clear();
			_sink.ProcessToken(token);
		}

		/// <summary>
		/// Emits the current tag.
		/// </summary>
		private void EmitTag()
		{
			FinishAttribute();
			FlushText();

			var token = new HtmlToken
			{
				Type = _isEndTag ? HtmlTokenType.EndTag : HtmlTokenType.StartTag,
				Name = _tagName.ToString(),
				SelfClosing = _selfClosing,
			};

			// end tags never carry attributes.
			if (!_isEndTag && _attributes.Count > 0)
			{
				token.Attributes = _attributes;
				_attributes = new List<HtmlTokenAttribute>();
			}
			else
			{
				_attributes.Clear();
			}

			if (!_isEndTag)
				this.LastStartTagName = token.Name;

			_sink.ProcessToken(token);
		}

		/// <summary>
		/// Emits the current comment.
		/// </summary>
		private void EmitComment()
		{
			FlushText();
			_sink.ProcessToken(new HtmlToken { Type = HtmlTokenType.Comment, Data = _comment.ToString() });
		}

		/// <summary>
		/// Emits the current document type.
		/// </summary>
		private void EmitDoctype()
		{
			FlushText();
			_sink.ProcessToken(new HtmlToken
			{
				Type = HtmlTokenType.Doctype,
				Name = _hasDoctypeName ? _doctypeName.ToString() : null,
				PublicId = _publicId?.ToString(),
				SystemId = _systemId?.ToString(),
				ForceQuirks = _forceQuirks,
			});
		}

		/// <summary>
		/// Emits the end of file token.
		/// </summary>
		private void EmitEof()
		{
			FlushText();
			_eofEmitted = true;
			_sink.ProcessToken(new HtmlToken { Type = HtmlTokenType.EndOfFile });
		}

		/// <summary>
		/// Starts a new start or end tag.
		/// </summary>
		/// <param name="endTag">Whether the tag is an end tag.</param>
		private void StartTag(bool endTag)
		{
			_tagName.Clear();
			_isEndTag = endTag;
			_selfClosing = false;
			_attributes.Clear();
			_hasAttribute = false;
		}

		/// <summary>
		/// Starts a new attribute, completing the previous one.
		/// </summary>
		private void StartAttribute()
		{
			FinishAttribute();
			_hasAttribute = true;
			_attributeName.Clear();
			_attributeValue.Clear();
		}

		/// <summary>
		/// Adds the attribute being built to the tag, unless the tag already has an attribute with the
		/// same name: duplicates are dropped and the first one wins (13.2.5.33).
		/// </summary>
		private void FinishAttribute()
		{
			if (!_hasAttribute)
				return;

			_hasAttribute = false;

			var name = _attributeName.ToString();
			for (var i = 0; i < _attributes.Count; i++)
			{
				if (_attributes[i].Name == name)
					return;
			}

			_attributes.Add(new HtmlTokenAttribute(name, _attributeValue.ToString()));
		}

		/// <summary>
		/// Starts a new comment.
		/// </summary>
		private void StartComment()
		{
			_comment.Clear();
		}

		/// <summary>
		/// Starts a new document type.
		/// </summary>
		private void StartDoctype()
		{
			_doctypeName.Clear();
			_hasDoctypeName = false;
			_publicId = null;
			_systemId = null;
			_forceQuirks = false;
		}

		/// <summary>
		/// Determines whether the current end tag is appropriate: its name matches the last start tag.
		/// </summary>
		/// <returns><c>true</c> when the end tag closes the current raw text element.</returns>
		private bool IsAppropriateEndTag()
		{
			var last = this.LastStartTagName;
			if (last == null || last.Length != _tagName.Length)
				return false;

			for (var i = 0; i < last.Length; i++)
			{
				if (last[i] != _tagName[i])
					return false;
			}

			return true;
		}

		/// <summary>
		/// Consumes a character reference in text and appends its replacement to the pending characters.
		/// </summary>
		private void ConsumeTextReference()
		{
			var value = CharacterReferences.Consume(_input, ref _pos, inAttribute: false);
			_text.Append(value ?? "&");
		}

		/// <summary>
		/// Consumes a character reference in an attribute value and appends its replacement to the value.
		/// </summary>
		private void ConsumeAttributeReference()
		{
			var value = CharacterReferences.Consume(_input, ref _pos, inAttribute: true);
			_attributeValue.Append(value ?? "&");
		}

		#endregion

		#region States

		/// <summary>
		/// Runs one step of the state machine.
		/// </summary>
		private void Step()
		{
			switch (this.State)
			{
				case TokenizerState.Data: DataState(); break;
				case TokenizerState.Rcdata: RcdataState(); break;
				case TokenizerState.Rawtext: RawtextState(); break;
				case TokenizerState.ScriptData: ScriptDataState(); break;
				case TokenizerState.Plaintext: PlaintextState(); break;
				case TokenizerState.TagOpen: TagOpenState(); break;
				case TokenizerState.EndTagOpen: EndTagOpenState(); break;
				case TokenizerState.TagName: TagNameState(); break;
				case TokenizerState.RcdataLessThanSign: TextLessThanSignState(TokenizerState.RcdataEndTagOpen, TokenizerState.Rcdata); break;
				case TokenizerState.RcdataEndTagOpen: TextEndTagOpenState(TokenizerState.RcdataEndTagName, TokenizerState.Rcdata); break;
				case TokenizerState.RcdataEndTagName: TextEndTagNameState(TokenizerState.Rcdata); break;
				case TokenizerState.RawtextLessThanSign: TextLessThanSignState(TokenizerState.RawtextEndTagOpen, TokenizerState.Rawtext); break;
				case TokenizerState.RawtextEndTagOpen: TextEndTagOpenState(TokenizerState.RawtextEndTagName, TokenizerState.Rawtext); break;
				case TokenizerState.RawtextEndTagName: TextEndTagNameState(TokenizerState.Rawtext); break;
				case TokenizerState.ScriptDataLessThanSign: ScriptDataLessThanSignState(); break;
				case TokenizerState.ScriptDataEndTagOpen: TextEndTagOpenState(TokenizerState.ScriptDataEndTagName, TokenizerState.ScriptData); break;
				case TokenizerState.ScriptDataEndTagName: TextEndTagNameState(TokenizerState.ScriptData); break;
				case TokenizerState.ScriptDataEscapeStart: ScriptDataEscapeStartState(); break;
				case TokenizerState.ScriptDataEscapeStartDash: ScriptDataEscapeStartDashState(); break;
				case TokenizerState.ScriptDataEscaped: ScriptDataEscapedState(); break;
				case TokenizerState.ScriptDataEscapedDash: ScriptDataEscapedDashState(); break;
				case TokenizerState.ScriptDataEscapedDashDash: ScriptDataEscapedDashDashState(); break;
				case TokenizerState.ScriptDataEscapedLessThanSign: ScriptDataEscapedLessThanSignState(); break;
				case TokenizerState.ScriptDataEscapedEndTagOpen: TextEndTagOpenState(TokenizerState.ScriptDataEscapedEndTagName, TokenizerState.ScriptDataEscaped); break;
				case TokenizerState.ScriptDataEscapedEndTagName: TextEndTagNameState(TokenizerState.ScriptDataEscaped); break;
				case TokenizerState.ScriptDataDoubleEscapeStart: ScriptDataDoubleEscapeStartState(); break;
				case TokenizerState.ScriptDataDoubleEscaped: ScriptDataDoubleEscapedState(); break;
				case TokenizerState.ScriptDataDoubleEscapedDash: ScriptDataDoubleEscapedDashState(); break;
				case TokenizerState.ScriptDataDoubleEscapedDashDash: ScriptDataDoubleEscapedDashDashState(); break;
				case TokenizerState.ScriptDataDoubleEscapedLessThanSign: ScriptDataDoubleEscapedLessThanSignState(); break;
				case TokenizerState.ScriptDataDoubleEscapeEnd: ScriptDataDoubleEscapeEndState(); break;
				case TokenizerState.BeforeAttributeName: BeforeAttributeNameState(); break;
				case TokenizerState.AttributeName: AttributeNameState(); break;
				case TokenizerState.AfterAttributeName: AfterAttributeNameState(); break;
				case TokenizerState.BeforeAttributeValue: BeforeAttributeValueState(); break;
				case TokenizerState.AttributeValueDoubleQuoted: AttributeValueQuotedState('"'); break;
				case TokenizerState.AttributeValueSingleQuoted: AttributeValueQuotedState('\''); break;
				case TokenizerState.AttributeValueUnquoted: AttributeValueUnquotedState(); break;
				case TokenizerState.AfterAttributeValueQuoted: AfterAttributeValueQuotedState(); break;
				case TokenizerState.SelfClosingStartTag: SelfClosingStartTagState(); break;
				case TokenizerState.BogusComment: BogusCommentState(); break;
				case TokenizerState.MarkupDeclarationOpen: MarkupDeclarationOpenState(); break;
				case TokenizerState.CommentStart: CommentStartState(); break;
				case TokenizerState.CommentStartDash: CommentStartDashState(); break;
				case TokenizerState.Comment: CommentState(); break;
				case TokenizerState.CommentLessThanSign: CommentLessThanSignState(); break;
				case TokenizerState.CommentLessThanSignBang: CommentLessThanSignBangState(); break;
				case TokenizerState.CommentLessThanSignBangDash: CommentLessThanSignBangDashState(); break;
				case TokenizerState.CommentLessThanSignBangDashDash: CommentLessThanSignBangDashDashState(); break;
				case TokenizerState.CommentEndDash: CommentEndDashState(); break;
				case TokenizerState.CommentEnd: CommentEndState(); break;
				case TokenizerState.CommentEndBang: CommentEndBangState(); break;
				case TokenizerState.Doctype: DoctypeState(); break;
				case TokenizerState.BeforeDoctypeName: BeforeDoctypeNameState(); break;
				case TokenizerState.DoctypeName: DoctypeNameState(); break;
				case TokenizerState.AfterDoctypeName: AfterDoctypeNameState(); break;
				case TokenizerState.AfterDoctypePublicKeyword: AfterDoctypeKeywordState(isPublic: true); break;
				case TokenizerState.BeforeDoctypePublicIdentifier: BeforeDoctypeIdentifierState(isPublic: true); break;
				case TokenizerState.DoctypePublicIdentifierDoubleQuoted: DoctypeIdentifierQuotedState('"', isPublic: true); break;
				case TokenizerState.DoctypePublicIdentifierSingleQuoted: DoctypeIdentifierQuotedState('\'', isPublic: true); break;
				case TokenizerState.AfterDoctypePublicIdentifier: AfterDoctypePublicIdentifierState(); break;
				case TokenizerState.BetweenDoctypePublicAndSystemIdentifiers: BetweenDoctypePublicAndSystemIdentifiersState(); break;
				case TokenizerState.AfterDoctypeSystemKeyword: AfterDoctypeKeywordState(isPublic: false); break;
				case TokenizerState.BeforeDoctypeSystemIdentifier: BeforeDoctypeIdentifierState(isPublic: false); break;
				case TokenizerState.DoctypeSystemIdentifierDoubleQuoted: DoctypeIdentifierQuotedState('"', isPublic: false); break;
				case TokenizerState.DoctypeSystemIdentifierSingleQuoted: DoctypeIdentifierQuotedState('\'', isPublic: false); break;
				case TokenizerState.AfterDoctypeSystemIdentifier: AfterDoctypeSystemIdentifierState(); break;
				case TokenizerState.BogusDoctype: BogusDoctypeState(); break;
				case TokenizerState.CdataSection: CdataSectionState(); break;
				case TokenizerState.CdataSectionBracket: CdataSectionBracketState(); break;
				case TokenizerState.CdataSectionEnd: CdataSectionEndState(); break;

				default:
					throw new InvalidOperationException("Unknown tokenizer state " + this.State);
			}
		}

		#endregion

		#region Text states

		/// <summary>
		/// 13.2.5.1 Data state.
		/// </summary>
		private void DataState()
		{
			// copy the run of plain text in one operation.
			var start = _pos;
			while (_pos < _length)
			{
				var ch = _input[_pos];
				if (ch == '<' || ch == '&')
					break;

				_pos++;
			}

			if (_pos > start)
				_text.Append(_input, start, _pos - start);

			var c = Next();
			switch (c)
			{
				case '&':
					ConsumeTextReference();
					break;

				case '<':
					this.State = TokenizerState.TagOpen;
					break;

				case EOF:
					EmitEof();
					break;
			}
		}

		/// <summary>
		/// 13.2.5.2 RCDATA state.
		/// </summary>
		private void RcdataState()
		{
			var c = Next();
			switch (c)
			{
				case '&':
					ConsumeTextReference();
					break;

				case '<':
					this.State = TokenizerState.RcdataLessThanSign;
					break;

				case '\0':
					_text.Append('�');
					break;

				case EOF:
					EmitEof();
					break;

				default:
					_text.Append((char)c);
					break;
			}
		}

		/// <summary>
		/// 13.2.5.3 RAWTEXT state.
		/// </summary>
		private void RawtextState()
		{
			var c = Next();
			switch (c)
			{
				case '<':
					this.State = TokenizerState.RawtextLessThanSign;
					break;

				case '\0':
					_text.Append('�');
					break;

				case EOF:
					EmitEof();
					break;

				default:
					_text.Append((char)c);
					break;
			}
		}

		/// <summary>
		/// 13.2.5.4 Script data state.
		/// </summary>
		private void ScriptDataState()
		{
			var c = Next();
			switch (c)
			{
				case '<':
					this.State = TokenizerState.ScriptDataLessThanSign;
					break;

				case '\0':
					_text.Append('�');
					break;

				case EOF:
					EmitEof();
					break;

				default:
					_text.Append((char)c);
					break;
			}
		}

		/// <summary>
		/// 13.2.5.5 PLAINTEXT state.
		/// </summary>
		private void PlaintextState()
		{
			var c = Next();
			switch (c)
			{
				case '\0':
					_text.Append('�');
					break;

				case EOF:
					EmitEof();
					break;

				default:
					_text.Append((char)c);
					break;
			}
		}

		/// <summary>
		/// 13.2.5.9, 13.2.5.12 RCDATA and RAWTEXT less-than sign states.
		/// </summary>
		/// <param name="endTagOpen">The end tag open state of the text kind.</param>
		/// <param name="text">The text state of the text kind.</param>
		private void TextLessThanSignState(TokenizerState endTagOpen, TokenizerState text)
		{
			var c = Next();
			if (c == '/')
			{
				_buffer.Clear();
				this.State = endTagOpen;
				return;
			}

			_text.Append('<');
			Reconsume();
			this.State = text;
		}

		/// <summary>
		/// 13.2.5.10, 13.2.5.13, 13.2.5.16, 13.2.5.24 end tag open states of the text kinds.
		/// </summary>
		/// <param name="endTagName">The end tag name state of the text kind.</param>
		/// <param name="text">The text state of the text kind.</param>
		private void TextEndTagOpenState(TokenizerState endTagName, TokenizerState text)
		{
			var c = Next();
			if (IsAsciiAlpha(c))
			{
				StartTag(endTag: true);
				Reconsume();
				this.State = endTagName;
				return;
			}

			_text.Append("</");
			Reconsume();
			this.State = text;
		}

		/// <summary>
		/// 13.2.5.11, 13.2.5.14, 13.2.5.17, 13.2.5.25 end tag name states of the text kinds.
		/// </summary>
		/// <param name="text">The text state to return to when the end tag is not appropriate.</param>
		private void TextEndTagNameState(TokenizerState text)
		{
			var c = Next();

			if (IsWhitespace(c) && IsAppropriateEndTag())
			{
				this.State = TokenizerState.BeforeAttributeName;
				return;
			}

			if (c == '/' && IsAppropriateEndTag())
			{
				this.State = TokenizerState.SelfClosingStartTag;
				return;
			}

			if (c == '>' && IsAppropriateEndTag())
			{
				this.State = TokenizerState.Data;
				EmitTag();
				return;
			}

			if (IsAsciiAlpha(c))
			{
				_tagName.Append(ToLower((char)c));
				_buffer.Append((char)c);
				return;
			}

			// not the end of the element: the characters are text.
			_text.Append("</").Append(_buffer);
			Reconsume();
			this.State = text;
		}

		#endregion

		#region Script states

		/// <summary>
		/// 13.2.5.15 Script data less-than sign state.
		/// </summary>
		private void ScriptDataLessThanSignState()
		{
			var c = Next();
			if (c == '/')
			{
				_buffer.Clear();
				this.State = TokenizerState.ScriptDataEndTagOpen;
			}
			else if (c == '!')
			{
				_text.Append("<!");
				this.State = TokenizerState.ScriptDataEscapeStart;
			}
			else
			{
				_text.Append('<');
				Reconsume();
				this.State = TokenizerState.ScriptData;
			}
		}

		/// <summary>
		/// 13.2.5.18 Script data escape start state.
		/// </summary>
		private void ScriptDataEscapeStartState()
		{
			var c = Next();
			if (c == '-')
			{
				_text.Append('-');
				this.State = TokenizerState.ScriptDataEscapeStartDash;
			}
			else
			{
				Reconsume();
				this.State = TokenizerState.ScriptData;
			}
		}

		/// <summary>
		/// 13.2.5.19 Script data escape start dash state.
		/// </summary>
		private void ScriptDataEscapeStartDashState()
		{
			var c = Next();
			if (c == '-')
			{
				_text.Append('-');
				this.State = TokenizerState.ScriptDataEscapedDashDash;
			}
			else
			{
				Reconsume();
				this.State = TokenizerState.ScriptData;
			}
		}

		/// <summary>
		/// 13.2.5.20 Script data escaped state.
		/// </summary>
		private void ScriptDataEscapedState()
		{
			var c = Next();
			switch (c)
			{
				case '-':
					_text.Append('-');
					this.State = TokenizerState.ScriptDataEscapedDash;
					break;

				case '<':
					this.State = TokenizerState.ScriptDataEscapedLessThanSign;
					break;

				case '\0':
					_text.Append('�');
					break;

				case EOF:
					EmitEof();
					break;

				default:
					_text.Append((char)c);
					break;
			}
		}

		/// <summary>
		/// 13.2.5.21 Script data escaped dash state.
		/// </summary>
		private void ScriptDataEscapedDashState()
		{
			var c = Next();
			switch (c)
			{
				case '-':
					_text.Append('-');
					this.State = TokenizerState.ScriptDataEscapedDashDash;
					break;

				case '<':
					this.State = TokenizerState.ScriptDataEscapedLessThanSign;
					break;

				case '\0':
					_text.Append('�');
					this.State = TokenizerState.ScriptDataEscaped;
					break;

				case EOF:
					EmitEof();
					break;

				default:
					_text.Append((char)c);
					this.State = TokenizerState.ScriptDataEscaped;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.22 Script data escaped dash dash state.
		/// </summary>
		private void ScriptDataEscapedDashDashState()
		{
			var c = Next();
			switch (c)
			{
				case '-':
					_text.Append('-');
					break;

				case '<':
					this.State = TokenizerState.ScriptDataEscapedLessThanSign;
					break;

				case '>':
					_text.Append('>');
					this.State = TokenizerState.ScriptData;
					break;

				case '\0':
					_text.Append('�');
					this.State = TokenizerState.ScriptDataEscaped;
					break;

				case EOF:
					EmitEof();
					break;

				default:
					_text.Append((char)c);
					this.State = TokenizerState.ScriptDataEscaped;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.23 Script data escaped less-than sign state.
		/// </summary>
		private void ScriptDataEscapedLessThanSignState()
		{
			var c = Next();
			if (c == '/')
			{
				_buffer.Clear();
				this.State = TokenizerState.ScriptDataEscapedEndTagOpen;
			}
			else if (IsAsciiAlpha(c))
			{
				_buffer.Clear();
				_text.Append('<');
				Reconsume();
				this.State = TokenizerState.ScriptDataDoubleEscapeStart;
			}
			else
			{
				_text.Append('<');
				Reconsume();
				this.State = TokenizerState.ScriptDataEscaped;
			}
		}

		/// <summary>
		/// 13.2.5.26 Script data double escape start state.
		/// </summary>
		private void ScriptDataDoubleEscapeStartState()
		{
			var c = Next();
			if (IsWhitespace(c) || c == '/' || c == '>')
			{
				_text.Append((char)c);
				this.State = _buffer.ToString() == "script" ? TokenizerState.ScriptDataDoubleEscaped : TokenizerState.ScriptDataEscaped;
			}
			else if (IsAsciiAlpha(c))
			{
				_buffer.Append(ToLower((char)c));
				_text.Append((char)c);
			}
			else
			{
				Reconsume();
				this.State = TokenizerState.ScriptDataEscaped;
			}
		}

		/// <summary>
		/// 13.2.5.27 Script data double escaped state.
		/// </summary>
		private void ScriptDataDoubleEscapedState()
		{
			var c = Next();
			switch (c)
			{
				case '-':
					_text.Append('-');
					this.State = TokenizerState.ScriptDataDoubleEscapedDash;
					break;

				case '<':
					_text.Append('<');
					this.State = TokenizerState.ScriptDataDoubleEscapedLessThanSign;
					break;

				case '\0':
					_text.Append('�');
					break;

				case EOF:
					EmitEof();
					break;

				default:
					_text.Append((char)c);
					break;
			}
		}

		/// <summary>
		/// 13.2.5.28 Script data double escaped dash state.
		/// </summary>
		private void ScriptDataDoubleEscapedDashState()
		{
			var c = Next();
			switch (c)
			{
				case '-':
					_text.Append('-');
					this.State = TokenizerState.ScriptDataDoubleEscapedDashDash;
					break;

				case '<':
					_text.Append('<');
					this.State = TokenizerState.ScriptDataDoubleEscapedLessThanSign;
					break;

				case '\0':
					_text.Append('�');
					this.State = TokenizerState.ScriptDataDoubleEscaped;
					break;

				case EOF:
					EmitEof();
					break;

				default:
					_text.Append((char)c);
					this.State = TokenizerState.ScriptDataDoubleEscaped;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.29 Script data double escaped dash dash state.
		/// </summary>
		private void ScriptDataDoubleEscapedDashDashState()
		{
			var c = Next();
			switch (c)
			{
				case '-':
					_text.Append('-');
					break;

				case '<':
					_text.Append('<');
					this.State = TokenizerState.ScriptDataDoubleEscapedLessThanSign;
					break;

				case '>':
					_text.Append('>');
					this.State = TokenizerState.ScriptData;
					break;

				case '\0':
					_text.Append('�');
					this.State = TokenizerState.ScriptDataDoubleEscaped;
					break;

				case EOF:
					EmitEof();
					break;

				default:
					_text.Append((char)c);
					this.State = TokenizerState.ScriptDataDoubleEscaped;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.30 Script data double escaped less-than sign state.
		/// </summary>
		private void ScriptDataDoubleEscapedLessThanSignState()
		{
			var c = Next();
			if (c == '/')
			{
				_buffer.Clear();
				_text.Append('/');
				this.State = TokenizerState.ScriptDataDoubleEscapeEnd;
			}
			else
			{
				Reconsume();
				this.State = TokenizerState.ScriptDataDoubleEscaped;
			}
		}

		/// <summary>
		/// 13.2.5.31 Script data double escape end state.
		/// </summary>
		private void ScriptDataDoubleEscapeEndState()
		{
			var c = Next();
			if (IsWhitespace(c) || c == '/' || c == '>')
			{
				_text.Append((char)c);
				this.State = _buffer.ToString() == "script" ? TokenizerState.ScriptDataEscaped : TokenizerState.ScriptDataDoubleEscaped;
			}
			else if (IsAsciiAlpha(c))
			{
				_buffer.Append(ToLower((char)c));
				_text.Append((char)c);
			}
			else
			{
				Reconsume();
				this.State = TokenizerState.ScriptDataDoubleEscaped;
			}
		}

		#endregion

		#region Tag states

		/// <summary>
		/// 13.2.5.6 Tag open state.
		/// </summary>
		private void TagOpenState()
		{
			var c = Next();

			if (c == '!')
			{
				this.State = TokenizerState.MarkupDeclarationOpen;
			}
			else if (c == '/')
			{
				this.State = TokenizerState.EndTagOpen;
			}
			else if (IsAsciiAlpha(c))
			{
				StartTag(endTag: false);
				Reconsume();
				this.State = TokenizerState.TagName;
			}
			else if (c == '?')
			{
				StartComment();
				Reconsume();
				this.State = TokenizerState.BogusComment;
			}
			else if (c == EOF)
			{
				_text.Append('<');
				EmitEof();
			}
			else
			{
				// "a < b" is text.
				_text.Append('<');
				Reconsume();
				this.State = TokenizerState.Data;
			}
		}

		/// <summary>
		/// 13.2.5.7 End tag open state.
		/// </summary>
		private void EndTagOpenState()
		{
			var c = Next();

			if (IsAsciiAlpha(c))
			{
				StartTag(endTag: true);
				Reconsume();
				this.State = TokenizerState.TagName;
			}
			else if (c == '>')
			{
				// "</>" is ignored.
				this.State = TokenizerState.Data;
			}
			else if (c == EOF)
			{
				_text.Append("</");
				EmitEof();
			}
			else
			{
				StartComment();
				Reconsume();
				this.State = TokenizerState.BogusComment;
			}
		}

		/// <summary>
		/// 13.2.5.8 Tag name state.
		/// </summary>
		private void TagNameState()
		{
			// fast path for the common lowercase names.
			while (_pos < _length)
			{
				var ch = _input[_pos];
				if (ch >= 'a' && ch <= 'z')
				{
					_tagName.Append(ch);
					_pos++;
				}
				else
				{
					break;
				}
			}

			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					this.State = TokenizerState.BeforeAttributeName;
					break;

				case '/':
					this.State = TokenizerState.SelfClosingStartTag;
					break;

				case '>':
					this.State = TokenizerState.Data;
					EmitTag();
					break;

				case '\0':
					_tagName.Append('�');
					break;

				case EOF:
					EmitEof();
					break;

				default:
					_tagName.Append(ToLower((char)c));
					break;
			}
		}

		/// <summary>
		/// 13.2.5.32 Before attribute name state.
		/// </summary>
		private void BeforeAttributeNameState()
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					break;

				case '/':
				case '>':
				case EOF:
					Reconsume();
					this.State = TokenizerState.AfterAttributeName;
					break;

				case '=':
					// "<a =x>" creates an attribute named "=".
					StartAttribute();
					_attributeName.Append('=');
					this.State = TokenizerState.AttributeName;
					break;

				default:
					StartAttribute();
					Reconsume();
					this.State = TokenizerState.AttributeName;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.33 Attribute name state.
		/// </summary>
		private void AttributeNameState()
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
				case '/':
				case '>':
				case EOF:
					Reconsume();
					this.State = TokenizerState.AfterAttributeName;
					break;

				case '=':
					this.State = TokenizerState.BeforeAttributeValue;
					break;

				case '\0':
					_attributeName.Append('�');
					break;

				default:
					// '"', '\'' and '<' are parse errors but part of the name.
					_attributeName.Append(ToLower((char)c));
					break;
			}
		}

		/// <summary>
		/// 13.2.5.34 After attribute name state.
		/// </summary>
		private void AfterAttributeNameState()
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					break;

				case '/':
					this.State = TokenizerState.SelfClosingStartTag;
					break;

				case '=':
					this.State = TokenizerState.BeforeAttributeValue;
					break;

				case '>':
					this.State = TokenizerState.Data;
					EmitTag();
					break;

				case EOF:
					EmitEof();
					break;

				default:
					StartAttribute();
					Reconsume();
					this.State = TokenizerState.AttributeName;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.35 Before attribute value state.
		/// </summary>
		private void BeforeAttributeValueState()
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					break;

				case '"':
					this.State = TokenizerState.AttributeValueDoubleQuoted;
					break;

				case '\'':
					this.State = TokenizerState.AttributeValueSingleQuoted;
					break;

				case '>':
					// "<a href=>": missing value, the tag ends.
					this.State = TokenizerState.Data;
					EmitTag();
					break;

				default:
					Reconsume();
					this.State = TokenizerState.AttributeValueUnquoted;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.36, 13.2.5.37 Attribute value (double-quoted and single-quoted) states.
		/// </summary>
		/// <param name="quote">The closing quote.</param>
		private void AttributeValueQuotedState(char quote)
		{
			// copy the run of plain value characters in one operation.
			var start = _pos;
			while (_pos < _length)
			{
				var ch = _input[_pos];
				if (ch == quote || ch == '&' || ch == '\0')
					break;

				_pos++;
			}

			if (_pos > start)
				_attributeValue.Append(_input, start, _pos - start);

			var c = Next();
			if (c == quote)
			{
				this.State = TokenizerState.AfterAttributeValueQuoted;
			}
			else if (c == '&')
			{
				ConsumeAttributeReference();
			}
			else if (c == '\0')
			{
				_attributeValue.Append('�');
			}
			else if (c == EOF)
			{
				EmitEof();
			}
		}

		/// <summary>
		/// 13.2.5.38 Attribute value (unquoted) state.
		/// </summary>
		private void AttributeValueUnquotedState()
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					this.State = TokenizerState.BeforeAttributeName;
					break;

				case '&':
					ConsumeAttributeReference();
					break;

				case '>':
					this.State = TokenizerState.Data;
					EmitTag();
					break;

				case '\0':
					_attributeValue.Append('�');
					break;

				case EOF:
					EmitEof();
					break;

				default:
					// '"', '\'', '<', '=' and '`' are parse errors but part of the value.
					_attributeValue.Append((char)c);
					break;
			}
		}

		/// <summary>
		/// 13.2.5.39 After attribute value (quoted) state.
		/// </summary>
		private void AfterAttributeValueQuotedState()
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					this.State = TokenizerState.BeforeAttributeName;
					break;

				case '/':
					this.State = TokenizerState.SelfClosingStartTag;
					break;

				case '>':
					this.State = TokenizerState.Data;
					EmitTag();
					break;

				case EOF:
					EmitEof();
					break;

				default:
					// "<a b='c'd>": missing white space between attributes.
					Reconsume();
					this.State = TokenizerState.BeforeAttributeName;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.40 Self-closing start tag state.
		/// </summary>
		private void SelfClosingStartTagState()
		{
			var c = Next();
			if (c == '>')
			{
				_selfClosing = true;
				this.State = TokenizerState.Data;
				EmitTag();
			}
			else if (c == EOF)
			{
				EmitEof();
			}
			else
			{
				Reconsume();
				this.State = TokenizerState.BeforeAttributeName;
			}
		}

		#endregion

		#region Comment states

		/// <summary>
		/// 13.2.5.41 Bogus comment state.
		/// </summary>
		private void BogusCommentState()
		{
			var c = Next();
			switch (c)
			{
				case '>':
					this.State = TokenizerState.Data;
					EmitComment();
					break;

				case EOF:
					EmitComment();
					EmitEof();
					break;

				case '\0':
					_comment.Append('�');
					break;

				default:
					_comment.Append((char)c);
					break;
			}
		}

		/// <summary>
		/// 13.2.5.42 Markup declaration open state.
		/// </summary>
		private void MarkupDeclarationOpenState()
		{
			if (LookingAt("--", ignoreCase: false))
			{
				_pos += 2;
				StartComment();
				this.State = TokenizerState.CommentStart;
			}
			else if (LookingAt("doctype", ignoreCase: true))
			{
				_pos += 7;
				this.State = TokenizerState.Doctype;
			}
			else if (LookingAt("[CDATA[", ignoreCase: false))
			{
				_pos += 7;
				if (_sink.AllowCdata)
				{
					this.State = TokenizerState.CdataSection;
				}
				else
				{
					// in HTML content a CDATA section is a bogus comment.
					StartComment();
					_comment.Append("[CDATA[");
					this.State = TokenizerState.BogusComment;
				}
			}
			else
			{
				StartComment();
				this.State = TokenizerState.BogusComment;
			}
		}

		/// <summary>
		/// 13.2.5.43 Comment start state.
		/// </summary>
		private void CommentStartState()
		{
			var c = Next();
			if (c == '-')
			{
				this.State = TokenizerState.CommentStartDash;
			}
			else if (c == '>')
			{
				// "<!-->" is an empty comment.
				this.State = TokenizerState.Data;
				EmitComment();
			}
			else
			{
				Reconsume();
				this.State = TokenizerState.Comment;
			}
		}

		/// <summary>
		/// 13.2.5.44 Comment start dash state.
		/// </summary>
		private void CommentStartDashState()
		{
			var c = Next();
			switch (c)
			{
				case '-':
					this.State = TokenizerState.CommentEnd;
					break;

				case '>':
					this.State = TokenizerState.Data;
					EmitComment();
					break;

				case EOF:
					EmitComment();
					EmitEof();
					break;

				default:
					_comment.Append('-');
					Reconsume();
					this.State = TokenizerState.Comment;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.45 Comment state.
		/// </summary>
		private void CommentState()
		{
			// copy the run of plain comment text in one operation.
			var start = _pos;
			while (_pos < _length)
			{
				var ch = _input[_pos];
				if (ch == '<' || ch == '-' || ch == '\0')
					break;

				_pos++;
			}

			if (_pos > start)
				_comment.Append(_input, start, _pos - start);

			var c = Next();
			switch (c)
			{
				case '<':
					_comment.Append('<');
					this.State = TokenizerState.CommentLessThanSign;
					break;

				case '-':
					this.State = TokenizerState.CommentEndDash;
					break;

				case '\0':
					_comment.Append('�');
					break;

				case EOF:
					EmitComment();
					EmitEof();
					break;
			}
		}

		/// <summary>
		/// 13.2.5.46 Comment less-than sign state.
		/// </summary>
		private void CommentLessThanSignState()
		{
			var c = Next();
			if (c == '!')
			{
				_comment.Append('!');
				this.State = TokenizerState.CommentLessThanSignBang;
			}
			else if (c == '<')
			{
				_comment.Append('<');
			}
			else
			{
				Reconsume();
				this.State = TokenizerState.Comment;
			}
		}

		/// <summary>
		/// 13.2.5.47 Comment less-than sign bang state.
		/// </summary>
		private void CommentLessThanSignBangState()
		{
			var c = Next();
			if (c == '-')
			{
				this.State = TokenizerState.CommentLessThanSignBangDash;
			}
			else
			{
				Reconsume();
				this.State = TokenizerState.Comment;
			}
		}

		/// <summary>
		/// 13.2.5.48 Comment less-than sign bang dash state.
		/// </summary>
		private void CommentLessThanSignBangDashState()
		{
			var c = Next();
			if (c == '-')
			{
				this.State = TokenizerState.CommentLessThanSignBangDashDash;
			}
			else
			{
				Reconsume();
				this.State = TokenizerState.CommentEndDash;
			}
		}

		/// <summary>
		/// 13.2.5.49 Comment less-than sign bang dash dash state.
		/// </summary>
		private void CommentLessThanSignBangDashDashState()
		{
			// "<!--<!---->": a nested comment opener is only a parse error, every character
			// is reconsumed in the comment end state.
			this.State = TokenizerState.CommentEnd;
		}

		/// <summary>
		/// 13.2.5.50 Comment end dash state.
		/// </summary>
		private void CommentEndDashState()
		{
			var c = Next();
			if (c == '-')
			{
				this.State = TokenizerState.CommentEnd;
			}
			else if (c == EOF)
			{
				EmitComment();
				EmitEof();
			}
			else
			{
				_comment.Append('-');
				Reconsume();
				this.State = TokenizerState.Comment;
			}
		}

		/// <summary>
		/// 13.2.5.51 Comment end state.
		/// </summary>
		private void CommentEndState()
		{
			var c = Next();
			switch (c)
			{
				case '>':
					this.State = TokenizerState.Data;
					EmitComment();
					break;

				case '!':
					this.State = TokenizerState.CommentEndBang;
					break;

				case '-':
					_comment.Append('-');
					break;

				case EOF:
					EmitComment();
					EmitEof();
					break;

				default:
					_comment.Append("--");
					Reconsume();
					this.State = TokenizerState.Comment;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.52 Comment end bang state.
		/// </summary>
		private void CommentEndBangState()
		{
			var c = Next();
			switch (c)
			{
				case '-':
					_comment.Append("--!");
					this.State = TokenizerState.CommentEndDash;
					break;

				case '>':
					this.State = TokenizerState.Data;
					EmitComment();
					break;

				case EOF:
					EmitComment();
					EmitEof();
					break;

				default:
					_comment.Append("--!");
					Reconsume();
					this.State = TokenizerState.Comment;
					break;
			}
		}

		#endregion

		#region Doctype states

		/// <summary>
		/// 13.2.5.53 DOCTYPE state.
		/// </summary>
		private void DoctypeState()
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					this.State = TokenizerState.BeforeDoctypeName;
					break;

				case '>':
					Reconsume();
					this.State = TokenizerState.BeforeDoctypeName;
					break;

				case EOF:
					StartDoctype();
					_forceQuirks = true;
					EmitDoctype();
					EmitEof();
					break;

				default:
					Reconsume();
					this.State = TokenizerState.BeforeDoctypeName;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.54 Before DOCTYPE name state.
		/// </summary>
		private void BeforeDoctypeNameState()
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					break;

				case '\0':
					StartDoctype();
					_hasDoctypeName = true;
					_doctypeName.Append('�');
					this.State = TokenizerState.DoctypeName;
					break;

				case '>':
					StartDoctype();
					_forceQuirks = true;
					this.State = TokenizerState.Data;
					EmitDoctype();
					break;

				case EOF:
					StartDoctype();
					_forceQuirks = true;
					EmitDoctype();
					EmitEof();
					break;

				default:
					StartDoctype();
					_hasDoctypeName = true;
					_doctypeName.Append(ToLower((char)c));
					this.State = TokenizerState.DoctypeName;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.55 DOCTYPE name state.
		/// </summary>
		private void DoctypeNameState()
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					this.State = TokenizerState.AfterDoctypeName;
					break;

				case '>':
					this.State = TokenizerState.Data;
					EmitDoctype();
					break;

				case '\0':
					_doctypeName.Append('�');
					break;

				case EOF:
					_forceQuirks = true;
					EmitDoctype();
					EmitEof();
					break;

				default:
					_doctypeName.Append(ToLower((char)c));
					break;
			}
		}

		/// <summary>
		/// 13.2.5.56 After DOCTYPE name state.
		/// </summary>
		private void AfterDoctypeNameState()
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					return;

				case '>':
					this.State = TokenizerState.Data;
					EmitDoctype();
					return;

				case EOF:
					_forceQuirks = true;
					EmitDoctype();
					EmitEof();
					return;
			}

			Reconsume();

			if (LookingAt("public", ignoreCase: true))
			{
				_pos += 6;
				this.State = TokenizerState.AfterDoctypePublicKeyword;
			}
			else if (LookingAt("system", ignoreCase: true))
			{
				_pos += 6;
				this.State = TokenizerState.AfterDoctypeSystemKeyword;
			}
			else
			{
				_forceQuirks = true;
				this.State = TokenizerState.BogusDoctype;
			}
		}

		/// <summary>
		/// 13.2.5.57, 13.2.5.63 After DOCTYPE public and system keyword states.
		/// </summary>
		/// <param name="isPublic">Whether the keyword is <c>PUBLIC</c>.</param>
		private void AfterDoctypeKeywordState(bool isPublic)
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					this.State = isPublic ? TokenizerState.BeforeDoctypePublicIdentifier : TokenizerState.BeforeDoctypeSystemIdentifier;
					break;

				case '"':
				case '\'':
					StartIdentifier(isPublic);
					this.State = IdentifierState(isPublic, (char)c);
					break;

				case '>':
					_forceQuirks = true;
					this.State = TokenizerState.Data;
					EmitDoctype();
					break;

				case EOF:
					_forceQuirks = true;
					EmitDoctype();
					EmitEof();
					break;

				default:
					_forceQuirks = true;
					Reconsume();
					this.State = TokenizerState.BogusDoctype;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.58, 13.2.5.64 Before DOCTYPE public and system identifier states.
		/// </summary>
		/// <param name="isPublic">Whether the identifier is the public one.</param>
		private void BeforeDoctypeIdentifierState(bool isPublic)
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					break;

				case '"':
				case '\'':
					StartIdentifier(isPublic);
					this.State = IdentifierState(isPublic, (char)c);
					break;

				case '>':
					_forceQuirks = true;
					this.State = TokenizerState.Data;
					EmitDoctype();
					break;

				case EOF:
					_forceQuirks = true;
					EmitDoctype();
					EmitEof();
					break;

				default:
					_forceQuirks = true;
					Reconsume();
					this.State = TokenizerState.BogusDoctype;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.59, 13.2.5.60, 13.2.5.65, 13.2.5.66 DOCTYPE identifier (quoted) states.
		/// </summary>
		/// <param name="quote">The closing quote.</param>
		/// <param name="isPublic">Whether the identifier is the public one.</param>
		private void DoctypeIdentifierQuotedState(char quote, bool isPublic)
		{
			var identifier = isPublic ? _publicId : _systemId;
			var c = Next();

			if (c == quote)
			{
				this.State = isPublic ? TokenizerState.AfterDoctypePublicIdentifier : TokenizerState.AfterDoctypeSystemIdentifier;
			}
			else if (c == '\0')
			{
				identifier.Append('�');
			}
			else if (c == '>')
			{
				_forceQuirks = true;
				this.State = TokenizerState.Data;
				EmitDoctype();
			}
			else if (c == EOF)
			{
				_forceQuirks = true;
				EmitDoctype();
				EmitEof();
			}
			else
			{
				identifier.Append((char)c);
			}
		}

		/// <summary>
		/// 13.2.5.61 After DOCTYPE public identifier state.
		/// </summary>
		private void AfterDoctypePublicIdentifierState()
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					this.State = TokenizerState.BetweenDoctypePublicAndSystemIdentifiers;
					break;

				case '>':
					this.State = TokenizerState.Data;
					EmitDoctype();
					break;

				case '"':
				case '\'':
					StartIdentifier(isPublic: false);
					this.State = IdentifierState(false, (char)c);
					break;

				case EOF:
					_forceQuirks = true;
					EmitDoctype();
					EmitEof();
					break;

				default:
					_forceQuirks = true;
					Reconsume();
					this.State = TokenizerState.BogusDoctype;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.62 Between DOCTYPE public and system identifiers state.
		/// </summary>
		private void BetweenDoctypePublicAndSystemIdentifiersState()
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					break;

				case '>':
					this.State = TokenizerState.Data;
					EmitDoctype();
					break;

				case '"':
				case '\'':
					StartIdentifier(isPublic: false);
					this.State = IdentifierState(false, (char)c);
					break;

				case EOF:
					_forceQuirks = true;
					EmitDoctype();
					EmitEof();
					break;

				default:
					_forceQuirks = true;
					Reconsume();
					this.State = TokenizerState.BogusDoctype;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.67 After DOCTYPE system identifier state.
		/// </summary>
		private void AfterDoctypeSystemIdentifierState()
		{
			var c = Next();
			switch (c)
			{
				case '\t':
				case '\n':
				case '\f':
				case ' ':
					break;

				case '>':
					this.State = TokenizerState.Data;
					EmitDoctype();
					break;

				case EOF:
					_forceQuirks = true;
					EmitDoctype();
					EmitEof();
					break;

				default:
					// trailing garbage does not force quirks mode.
					Reconsume();
					this.State = TokenizerState.BogusDoctype;
					break;
			}
		}

		/// <summary>
		/// 13.2.5.68 Bogus DOCTYPE state.
		/// </summary>
		private void BogusDoctypeState()
		{
			var c = Next();
			if (c == '>')
			{
				this.State = TokenizerState.Data;
				EmitDoctype();
			}
			else if (c == EOF)
			{
				EmitDoctype();
				EmitEof();
			}
		}

		/// <summary>
		/// Starts a public or system identifier, which becomes present (possibly empty).
		/// </summary>
		/// <param name="isPublic">Whether the identifier is the public one.</param>
		private void StartIdentifier(bool isPublic)
		{
			if (isPublic)
				_publicId = new StringBuilder();
			else
				_systemId = new StringBuilder();
		}

		/// <summary>
		/// Returns the quoted identifier state for an identifier and a quote.
		/// </summary>
		/// <param name="isPublic">Whether the identifier is the public one.</param>
		/// <param name="quote">The opening quote.</param>
		/// <returns>The state.</returns>
		private static TokenizerState IdentifierState(bool isPublic, char quote)
		{
			if (isPublic)
				return quote == '"' ? TokenizerState.DoctypePublicIdentifierDoubleQuoted : TokenizerState.DoctypePublicIdentifierSingleQuoted;

			return quote == '"' ? TokenizerState.DoctypeSystemIdentifierDoubleQuoted : TokenizerState.DoctypeSystemIdentifierSingleQuoted;
		}

		#endregion

		#region CDATA states

		/// <summary>
		/// 13.2.5.69 CDATA section state.
		/// </summary>
		private void CdataSectionState()
		{
			var c = Next();
			if (c == ']')
			{
				this.State = TokenizerState.CdataSectionBracket;
			}
			else if (c == EOF)
			{
				EmitEof();
			}
			else
			{
				// NUL is emitted as is; the tree builder replaces it in foreign content.
				_text.Append((char)c);
			}
		}

		/// <summary>
		/// 13.2.5.70 CDATA section bracket state.
		/// </summary>
		private void CdataSectionBracketState()
		{
			var c = Next();
			if (c == ']')
			{
				this.State = TokenizerState.CdataSectionEnd;
			}
			else
			{
				_text.Append(']');
				Reconsume();
				this.State = TokenizerState.CdataSection;
			}
		}

		/// <summary>
		/// 13.2.5.71 CDATA section end state.
		/// </summary>
		private void CdataSectionEndState()
		{
			var c = Next();
			if (c == ']')
			{
				_text.Append(']');
			}
			else if (c == '>')
			{
				this.State = TokenizerState.Data;
			}
			else
			{
				_text.Append("]]");
				Reconsume();
				this.State = TokenizerState.CdataSection;
			}
		}

		#endregion
	}
}

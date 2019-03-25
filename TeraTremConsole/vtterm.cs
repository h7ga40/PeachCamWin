/*
 * Copyright (C) 1994-1998 T. Teranishi
 * (C) 2004-2017 TeraTerm Project
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHORS ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

/* TERATERM.EXE, VT terminal emulation */
using System;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace TeraTrem
{
	/* Parsing modes */
	enum ParsingMode
	{
		ModeFirst = 0,
		ModeESC = 1,
		ModeDCS = 2,
		ModeDCUserKey = 3,
		ModeSOS = 4,
		ModeCSI = 5,
		ModeXS = 6,
		ModeDLE = 7,
		ModeCAN = 8,
		ModeIgnore = -1,
	}

	/* DEC Locator Flag */
	enum DecLocator
	{
		DecLocatorOneShot = 1,
		DecLocatorPixel = 2,
		DecLocatorButtonDown = 4,
		DecLocatorButtonUp = 8,
		DecLocatorFiltered = 16,
	}

	class VTTerm
	{
		Buffer Buffer;
		VTDisp VTDisp;
		ttwinman ttwinman;
		keyboard keyboard;
		ttime ttime;
		teraprn teraprn;
		TTTSet ts;
		TComVar cv;

		[DllImport("user32.dll")]
		public extern static int MessageBeep(int p);

		static bool Accept8BitCtrl(int VTlevel, TTTSet ts) { return (VTlevel >= 2) && ((ts.TermFlag & TerminalFlags.TF_ACCEPT8BITCTRL) != 0); }

		const int NParamMax = 16;
		const int NSParamMax = 16;
		const int IntCharMax = 5;

		/* character attribute */
		TCharAttr CharAttr;

		/* various modes of VT emulation */
		bool RelativeOrgMode;
		bool InsertMode;
		bool LFMode;
		bool ClearThenHome;
		bool AutoWrapMode;
		bool FocusReportMode;
		bool AltScr;
		bool LRMarginMode;
		bool RectangleMode;
		bool BracketedPaste;

		public const string BracketStart = "\033[200~";
		public const string BracketEnd = "\033[201~";
		public int BracketStartLen = (BracketStart.Length - 1);
		public int BracketEndLen = (BracketEnd.Length - 1);

		int VTlevel;

		bool AcceptWheelToCursor;

		// save/restore cursor
		class TStatusBuff
		{
			public int CursorX, CursorY;
			public TCharAttr Attr;
			public int[] Glr = new int[2];
			public CharacterSets[] Gn = new CharacterSets[4]; // G0-G3, GL & GR
			public bool AutoWrapMode;
			public bool RelativeOrgMode;
		}

		// currently only used for AUTO CR/LF receive mode
		byte PrevCharacter;
		bool PrevCRorLFGeneratedCRLF;     // indicates that previous CR or LF really generated a CR+LF

		// status buffer for main screen & status line
		TStatusBuff SBuff1 = new TStatusBuff(), SBuff2 = new TStatusBuff(), SBuff3 = new TStatusBuff();

		bool ESCFlag, JustAfterESC;
		bool KanjiIn;
		bool EUCkanaIn, EUCsupIn;
		int EUCcount;
		bool Special;

		int[] Param = new int[NParamMax + 1];
		int[][] SubParam = new int[NParamMax + 1][];
		int NParam;
		int[] NSParam = new int[NParamMax + 1];
		bool FirstPrm;
		byte[] IntChar = new byte[IntCharMax + 1];
		int ICount;
		byte Prv;
		ParsingMode ParseMode;
		WindowId ChangeEmu;

		class TStack
		{
			public string title;
			public TStack next;
		}

		TStack TitleStack = null;

		/* user defined keys */
		bool WaitKeyId, WaitHi;

		/* GL, GR code group */
		int[] Glr = new int[2];
		/* G0, G1, G2, G3 code group */
		CharacterSets[] Gn = new CharacterSets[4];
		/* GL for single shift 2/3 */
		int GLtmp;
		/* single shift 2/3 flag */
		bool SSflag;
		/* JIS -> SJIS conversion flag */
		bool ConvJIS;
		char Kanji;
		bool Fallbacked;

		// variables for status line mode
		int StatusX = 0;
		bool StatusWrap = false;
		bool StatusCursor = true;
		int MainX, MainY; //cursor registers
		int MainTop, MainBottom; // scroll region registers
		bool MainWrap;
		bool MainCursor = true;

		/* status for printer escape sequences */
		bool PrintEX = true;  // printing extent
							  // (true: screen, false: scroll region)
		bool AutoPrintMode = false;
		bool PrinterMode = false;
		bool DirectPrn = false;

		/* User key */
		byte[] NewKeyStr = new byte[keyboard.FuncKeyStrMax];
		int NewKeyId, NewKeyLen;

		/* Mouse Report */
		MouseTrackingMode MouseReportMode;
		ExtendedMouseTrackingMode MouseReportExtMode;
		DecLocator DecLocatorFlag;
		int LastX, LastY;
		MouseButtons ButtonStat;
		int FilterTop, FilterBottom, FilterLeft, FilterRight;

		/* IME Status */
		bool IMEstat;

		/* Beep over-used */
		static long BeepStartTime = 0;
		static long BeepSuppressTime = 0;
		static long BeepOverUsedCount = 0;

		public VTTerm()
		{
			for (int i = 0; i < SubParam.Length; i++) {
				SubParam[i] = new int[NSParamMax + 1];
			}
		}

		void ClearParams()
		{
			ICount = 0;
			NParam = 1;
			NSParam[1] = 0;
			Param[1] = 0;
			Prv = 0;
		}

		void ResetSBuffer(TStatusBuff sbuff)
		{
			sbuff.CursorX = 0;
			sbuff.CursorY = 0;
			sbuff.Attr = VTDisp.DefCharAttr;
			if (ts.Language == Language.IdJapanese) {
				sbuff.Gn[0] = CharacterSets.IdASCII;
				sbuff.Gn[1] = CharacterSets.IdKatakana;
				sbuff.Gn[2] = CharacterSets.IdKatakana;
				sbuff.Gn[3] = CharacterSets.IdKanji;
				sbuff.Glr[0] = 0;
				if ((ts.KanjiCode == KanjiCodeId.IdJIS) && !ts.JIS7Katakana)
					sbuff.Glr[1] = 2;  // 8-bit katakana
				else
					sbuff.Glr[1] = 3;
			}
			else {
				sbuff.Gn[0] = CharacterSets.IdASCII;
				sbuff.Gn[1] = CharacterSets.IdSpecial;
				sbuff.Gn[2] = CharacterSets.IdASCII;
				sbuff.Gn[3] = CharacterSets.IdASCII;
				sbuff.Glr[0] = 0;
				sbuff.Glr[1] = 0;
			}
			sbuff.AutoWrapMode = true;
			sbuff.RelativeOrgMode = false;
		}

		void ResetAllSBuffers()
		{
			ResetSBuffer(SBuff1);
			// copy SBuff1 to SBuff2
			SBuff2 = SBuff1;
			SBuff3 = SBuff1;
		}

		void ResetCurSBuffer()
		{
			TStatusBuff Buff;

			if (AltScr) {
				Buff = SBuff3; // Alternate screen buffer
			}
			else {
				Buff = SBuff1; // Normal screen buffer
			}
			ResetSBuffer(Buff);
			SBuff2 = Buff;
		}

		public void ResetTerminal() /*reset variables but don't update screen */
		{
			VTDisp.DispReset();
			Buffer.BuffReset();

			/* Attribute */
			CharAttr = VTDisp.DefCharAttr;
			Special = false;
			Buffer.BuffSetCurCharAttr(CharAttr);

			/* Various modes */
			InsertMode = false;
			LFMode = (ts.CRSend == NewLineModes.IdCRLF);
			AutoWrapMode = true;
			keyboard.AppliKeyMode = false;
			keyboard.AppliCursorMode = false;
			keyboard.AppliEscapeMode = 0;
			AcceptWheelToCursor = ts.TranslateWheelToCursor;
			RelativeOrgMode = false;
			ts.ColorFlag &= ~ColorFlags.CF_REVERSEVIDEO;
			keyboard.AutoRepeatMode = true;
			FocusReportMode = false;
			MouseReportMode = MouseTrackingMode.IdMouseTrackNone;
			MouseReportExtMode = ExtendedMouseTrackingMode.IdMouseTrackExtNone;
			DecLocatorFlag = 0;
			ClearThenHome = false;
			RectangleMode = false;

			ChangeTerminalID();

			LastX = 0;
			LastY = 0;
			ButtonStat = 0;

			/* Character sets */
			ResetCharSet();

			/* ESC flag for device control sequence */
			ESCFlag = false;
			/* for TEK sequence */
			JustAfterESC = false;

			/* Parse mode */
			ParseMode = ParsingMode.ModeFirst;

			/* Clear printer mode */
			PrinterMode = false;

			// status buffers
			ResetAllSBuffers();

			// Alternate Screen Buffer
			AltScr = false;

			// Left/Right Margin Mode
			LRMarginMode = false;

			// Bracketed Paste Mode
			BracketedPaste = false;

			// Saved IME Status
			IMEstat = false;

			// previous received character
			PrevCharacter = unchecked((byte)-1); // none
			PrevCRorLFGeneratedCRLF = false;

			// Beep over-used
			BeepStartTime = DateTime.Now.Ticks;
			BeepSuppressTime = BeepStartTime - ts.BeepSuppressTime * 1000;
			BeepStartTime -= (ts.BeepOverUsedTime * 1000);
			BeepOverUsedCount = ts.BeepOverUsedCount;
		}

		void ResetCharSet()
		{
			if (ts.Language == Language.IdJapanese) {
				Gn[0] = CharacterSets.IdASCII;
				Gn[1] = CharacterSets.IdKatakana;
				Gn[2] = CharacterSets.IdKatakana;
				Gn[3] = CharacterSets.IdKanji;
				Glr[0] = 0;
				if ((ts.KanjiCode == KanjiCodeId.IdJIS) && !ts.JIS7Katakana)
					Glr[1] = 2;  // 8-bit katakana
				else
					Glr[1] = 3;
			}
			else {
				Gn[0] = CharacterSets.IdASCII;
				Gn[1] = CharacterSets.IdSpecial;
				Gn[2] = CharacterSets.IdASCII;
				Gn[3] = CharacterSets.IdASCII;
				Glr[0] = 0;
				Glr[1] = 0;
				cv.SendCode = CharacterSets.IdASCII;
				cv.SendKanjiFlag = false;
				cv.EchoCode = CharacterSets.IdASCII;
				cv.EchoKanjiFlag = false;
			}
			/* Kanji flag */
			KanjiIn = false;
			EUCkanaIn = false;
			EUCsupIn = false;
			SSflag = false;
			ConvJIS = false;
			Fallbacked = false;

			cv.Language = ts.Language;
			cv.CRSend = ts.CRSend;
			cv.KanjiCodeEcho = ts.KanjiCode;
			cv.JIS7KatakanaEcho = ts.JIS7Katakana;
			cv.KanjiCodeSend = ts.KanjiCodeSend;
			cv.JIS7KatakanaSend = ts.JIS7KatakanaSend;
			cv.KanjiIn = ts.KanjiIn;
			cv.KanjiOut = ts.KanjiOut;
		}

		void ResetKeypadMode(bool DisabledModeOnly)
		{
			if (!DisabledModeOnly || ts.DisableAppKeypad)
				keyboard.AppliKeyMode = false;
			if (!DisabledModeOnly || ts.DisableAppCursor)
				keyboard.AppliCursorMode = false;
		}

		void MoveToMainScreen()
		{
			StatusX = VTDisp.CursorX;
			StatusWrap = Buffer.Wrap;
			StatusCursor = VTDisp.IsCaretEnabled();

			Buffer.CursorTop = MainTop;
			Buffer.CursorBottom = MainBottom;
			Buffer.Wrap = MainWrap;
			VTDisp.DispEnableCaret(MainCursor);
			Buffer.MoveCursor(MainX, MainY); // move to main screen
		}

		void MoveToStatusLine()
		{
			MainX = VTDisp.CursorX;
			MainY = VTDisp.CursorY;
			MainTop = Buffer.CursorTop;
			MainBottom = Buffer.CursorBottom;
			MainWrap = Buffer.Wrap;
			MainCursor = VTDisp.IsCaretEnabled();

			VTDisp.DispEnableCaret(StatusCursor);
			Buffer.MoveCursor(StatusX, VTDisp.NumOfLines - 1); // move to status line
			Buffer.CursorTop = VTDisp.NumOfLines - 1;
			Buffer.CursorBottom = Buffer.CursorTop;
			Buffer.Wrap = StatusWrap;
		}

		public void HideStatusLine()
		{
			if (Buffer.isCursorOnStatusLine())
				MoveToMainScreen();
			StatusX = 0;
			StatusWrap = false;
			StatusCursor = true;
			Buffer.ShowStatusLine(0); //hide
		}

		void ChangeTerminalSize(int Nx, int Ny)
		{
			Buffer.BuffChangeTerminalSize(Nx, Ny);
			StatusX = 0;
			MainX = 0;
			MainY = 0;
			MainTop = 0;
			MainBottom = VTDisp.NumOfLines - Buffer.StatusLine - 1;
		}

		void SendCSIstr(string str, int len)
		{
			int l;

			if (str == null || len < 0)
				return;

			if (len == 0) {
				l = str.Length;
			}
			else {
				l = len;
			}

			if (keyboard.Send8BitMode)
				ttcmn.CommBinaryOut(cv, "\x9b", 1);
			else
				ttcmn.CommBinaryOut(cv, "\x1b[", 2);

			ttcmn.CommBinaryOut(cv, str, l);
		}

		void SendOSCstr(string str, int len, char TermChar)
		{
			int l;

			if (str == null || len < 0)
				return;

			if (len == 0) {
				l = str.Length;
			}
			else {
				l = len;
			}

			if (TermChar == (char)ControlCharacters.BEL) {
				ttcmn.CommBinaryOut(cv, "\033]", 2);
				ttcmn.CommBinaryOut(cv, str, l);
				ttcmn.CommBinaryOut(cv, "\007", 1);
			}
			else if (keyboard.Send8BitMode) {
				ttcmn.CommBinaryOut(cv, "\x9d", 1);
				ttcmn.CommBinaryOut(cv, str, l);
				ttcmn.CommBinaryOut(cv, "\x9c", 1);
			}
			else {
				ttcmn.CommBinaryOut(cv, "\x1b]", 2);
				ttcmn.CommBinaryOut(cv, str, l);
				ttcmn.CommBinaryOut(cv, "\x1b\\", 2);
			}
		}

		void SendDCSstr(string str, int len)
		{
			int l;

			if (str == null || len < 0)
				return;

			if (len == 0) {
				l = str.Length;
			}
			else {
				l = len;
			}

			if (keyboard.Send8BitMode) {
				ttcmn.CommBinaryOut(cv, "\x90", 1);
				ttcmn.CommBinaryOut(cv, str, l);
				ttcmn.CommBinaryOut(cv, "\x9c", 1);
			}
			else {
				ttcmn.CommBinaryOut(cv, "\x1bP", 2);
				ttcmn.CommBinaryOut(cv, str, l);
				ttcmn.CommBinaryOut(cv, "\x1b\\", 2);
			}
		}

		void BackSpace()
		{
			if (VTDisp.CursorX == Buffer.CursorLeftM || VTDisp.CursorX == 0) {
				if ((VTDisp.CursorY > 0) && ((ts.TermFlag & TerminalFlags.TF_BACKWRAP) != 0)) {
					Buffer.MoveCursor(VTDisp.NumOfColumns - 1, VTDisp.CursorY - 1);
					if (cv.HLogBuf != IntPtr.Zero && !ts.LogTypePlainText) filesys.Log1Byte((byte)ControlCharacters.BS);
				}
			}
			else if (VTDisp.CursorX > 0) {
				Buffer.MoveCursor(VTDisp.CursorX - 1, VTDisp.CursorY);
				if (cv.HLogBuf != IntPtr.Zero && !ts.LogTypePlainText) filesys.Log1Byte((byte)ControlCharacters.BS);
			}
		}

		void CarriageReturn(bool logFlag)
		{
			if (!ts.EnableContinuedLineCopy || logFlag)
				if (cv.HLogBuf != IntPtr.Zero) filesys.Log1Byte((byte)ControlCharacters.CR);

			if (RelativeOrgMode || VTDisp.CursorX > Buffer.CursorLeftM)
				Buffer.MoveCursor(Buffer.CursorLeftM, VTDisp.CursorY);
			else if (VTDisp.CursorX < Buffer.CursorLeftM)
				Buffer.MoveCursor(0, VTDisp.CursorY);

			Fallbacked = false;
		}

		void LineFeed(byte b, bool logFlag)
		{
			/* for auto print mode */
			if ((AutoPrintMode) &&
				(b >= (byte)ControlCharacters.LF) && (b <= (byte)ControlCharacters.FF))
				Buffer.BuffDumpCurrentLine(b);

			if (!ts.EnableContinuedLineCopy || logFlag)
				if (cv.HLogBuf != IntPtr.Zero) filesys.Log1Byte((byte)ControlCharacters.LF);

			if (VTDisp.CursorY < Buffer.CursorBottom)
				Buffer.MoveCursor(VTDisp.CursorX, VTDisp.CursorY + 1);
			else if (VTDisp.CursorY == Buffer.CursorBottom) Buffer.BuffScrollNLines(1);
			else if (VTDisp.CursorY < VTDisp.NumOfLines - Buffer.StatusLine - 1)
				Buffer.MoveCursor(VTDisp.CursorX, VTDisp.CursorY + 1);

			//ClearLineContinued();
			Buffer.BuffLineContinued(false);

			if (LFMode) CarriageReturn(logFlag);

			Fallbacked = false;
		}

		void Tab()
		{
			if (Buffer.Wrap && !ts.VTCompatTab) {
				CarriageReturn(false);
				LineFeed((byte)ControlCharacters.LF, false);
				if (ts.EnableContinuedLineCopy) {
					//SetLineContinued();
					Buffer.BuffLineContinued(true);
				}
				Buffer.Wrap = false;
			}
			Buffer.CursorForwardTab(1, AutoWrapMode);
			if (cv.HLogBuf != IntPtr.Zero) filesys.Log1Byte((byte)ControlCharacters.HT);
		}

		void PutChar(byte b)
		{
			bool SpecialNew;
			TCharAttr CharAttrTmp;

			CharAttrTmp = CharAttr;

			if (PrinterMode) { // printer mode
				teraprn.WriteToPrnFile(b, true);
				return;
			}

			if (Buffer.Wrap) {
				CarriageReturn(false);
				LineFeed((byte)ControlCharacters.LF, false);
				CharAttrTmp.Attr |= ts.EnableContinuedLineCopy ? AttributeBitMasks.AttrLineContinued : 0;
			}

			//  if (cv.HLogBuf!=0) filesys.Log1Byte(b);
			// (2005.2.20 yutaka)
			if (ts.LogTypePlainText) {
				if (__isascii(b) && !isprint(b)) {
					// ASCII文字で、非表示な文字はログ採取しない。
				}
				else {
					if (cv.HLogBuf != IntPtr.Zero) filesys.Log1Byte(b);
				}
			}
			else {
				if (cv.HLogBuf != IntPtr.Zero) filesys.Log1Byte(b);
			}

			Buffer.Wrap = false;

			SpecialNew = false;
			if ((b > 0x5F) && (b < 0x80)) {
				if (SSflag)
					SpecialNew = (Gn[GLtmp] == CharacterSets.IdSpecial);
				else
					SpecialNew = (Gn[Glr[0]] == CharacterSets.IdSpecial);
			}
			else if (b > 0xDF) {
				if (SSflag)
					SpecialNew = (Gn[GLtmp] == CharacterSets.IdSpecial);
				else
					SpecialNew = (Gn[Glr[1]] == CharacterSets.IdSpecial);
			}

			if (SpecialNew != Special) {
				Buffer.UpdateStr();
				Special = SpecialNew;
			}

			if (Special) {
				b = (byte)(b & 0x7F);
				CharAttrTmp.Attr |= AttributeBitMasks.AttrSpecial;
			}
			else
				CharAttrTmp.Attr |= CharAttr.Attr;

			Buffer.BuffPutChar(b, CharAttrTmp, InsertMode);

			if (VTDisp.CursorX == Buffer.CursorRightM || VTDisp.CursorX >= VTDisp.NumOfColumns - 1) {
				Buffer.UpdateStr();
				Buffer.Wrap = AutoWrapMode;
			}
			else {
				Buffer.MoveRight();
			}
		}

		private static bool __isascii(byte b)
		{
			return b <= 0x7F;
		}

		private static bool isprint(byte b)
		{
			return (b >= 0x20) && (b <= 0x7E);
		}

		void PutDecSp(byte b)
		{
			TCharAttr CharAttrTmp;

			CharAttrTmp = CharAttr;

			if (PrinterMode) { // printer mode
				teraprn.WriteToPrnFile(b, true);
				return;
			}

			if (Buffer.Wrap) {
				CarriageReturn(false);
				LineFeed((byte)ControlCharacters.LF, false);
				CharAttrTmp.Attr |= ts.EnableContinuedLineCopy ? AttributeBitMasks.AttrLineContinued : 0;
			}

			if (cv.HLogBuf != IntPtr.Zero) filesys.Log1Byte(b);
			/*
			  if (ts.LogTypePlainText && __isascii(b) && !isprint(b)) {
				// ASCII文字で、非表示な文字はログ採取しない。
			  } else {
				if (cv.HLogBuf!=0) filesys.Log1Byte(b);
			  }
			 */

			Buffer.Wrap = false;

			if (!Special) {
				Buffer.UpdateStr();
				Special = true;
			}

			CharAttrTmp.Attr |= AttributeBitMasks.AttrSpecial;
			Buffer.BuffPutChar(b, CharAttrTmp, InsertMode);

			if (VTDisp.CursorX == Buffer.CursorRightM || VTDisp.CursorX >= VTDisp.NumOfColumns - 1) {
				Buffer.UpdateStr();
				Buffer.Wrap = AutoWrapMode;
			}
			else {
				Buffer.MoveRight();
			}
		}

		void PutKanji(byte b)
		{
			int LineEnd;
			TCharAttr CharAttrTmp;
			CharAttrTmp = CharAttr;

			Kanji = (char)(Kanji + b);

			if (PrinterMode && DirectPrn) {
				teraprn.WriteToPrnFile((byte)(Kanji >> 8), false);
				teraprn.WriteToPrnFile((byte)(Kanji & 0xFF), true);
				return;
			}

			if (ConvJIS)
				Kanji = language.JIS2SJIS((char)(Kanji & 0x7f7f));

			if (PrinterMode) { // printer mode
				teraprn.WriteToPrnFile((byte)(Kanji >> 8), false);
				teraprn.WriteToPrnFile((byte)(Kanji & 0xFF), true);
				return;
			}

			if (VTDisp.CursorX > Buffer.CursorRightM)
				LineEnd = VTDisp.NumOfColumns - 1;
			else
				LineEnd = Buffer.CursorRightM;

			if (Buffer.Wrap) {
				CarriageReturn(false);
				LineFeed((byte)ControlCharacters.LF, false);
				if (ts.EnableContinuedLineCopy)
					CharAttrTmp.Attr |= AttributeBitMasks.AttrLineContinued;
			}
			else if (VTDisp.CursorX > LineEnd - 1) {
				if (AutoWrapMode) {
					if (ts.EnableContinuedLineCopy) {
						CharAttrTmp.Attr |= AttributeBitMasks.AttrLineContinued;
						if (VTDisp.CursorX == LineEnd)
							Buffer.BuffPutChar(0x20, CharAttr, false);
					}
					CarriageReturn(false);
					LineFeed((byte)ControlCharacters.LF, false);
				}
				else {
					return;
				}
			}

			Buffer.Wrap = false;

			if (cv.HLogBuf != IntPtr.Zero) {
				filesys.Log1Byte((byte)(Kanji >> 8));
				filesys.Log1Byte((byte)(Kanji & 0xFF));
			}

			if (Special) {
				Buffer.UpdateStr();
				Special = false;
			}

			Buffer.BuffPutKanji(Kanji, CharAttrTmp, InsertMode);

			if (VTDisp.CursorX < LineEnd - 1) {
				Buffer.MoveRight();
				Buffer.MoveRight();
			}
			else {
				Buffer.UpdateStr();
				Buffer.Wrap = AutoWrapMode;
			}
		}

		void PutDebugChar(byte b)
		{
			int i = 0;
			bool svInsertMode, svAutoWrapMode;
			AttributeBitMasks svCharAttr;

			if (keyboard.DebugFlag != KeyboardDebugFlag.DEBUG_FLAG_NONE) {
				svInsertMode = InsertMode;
				svAutoWrapMode = AutoWrapMode;
				InsertMode = false;
				AutoWrapMode = true;

				svCharAttr = CharAttr.Attr;
				if (CharAttr.Attr != AttributeBitMasks.AttrDefault) {
					Buffer.UpdateStr();
					CharAttr.Attr = AttributeBitMasks.AttrDefault;
				}

				if (keyboard.DebugFlag == KeyboardDebugFlag.DEBUG_FLAG_HEXD) {
					string buff = String.Format("{0:2X}", (uint)b);

					for (i = 0; i < 2; i++)
						PutChar((byte)buff[i]);
					PutChar((byte)' ');
				}
				else if (keyboard.DebugFlag == KeyboardDebugFlag.DEBUG_FLAG_NORM) {

					if ((b & 0x80) == 0x80) {
						Buffer.UpdateStr();
						CharAttr.Attr = AttributeBitMasks.AttrReverse;
						b = (byte)(b & 0x7f);
					}

					if (b <= (byte)ControlCharacters.US) {
						PutChar((byte)'^');
						PutChar((byte)(b + 0x40));
					}
					else if (b == (byte)ControlCharacters.DEL) {
						PutChar((byte)'<');
						PutChar((byte)'D');
						PutChar((byte)'E');
						PutChar((byte)'L');
						PutChar((byte)'>');
					}
					else
						PutChar(b);
				}

				if (CharAttr.Attr != svCharAttr) {
					Buffer.UpdateStr();
					CharAttr.Attr = svCharAttr;
				}
				InsertMode = svInsertMode;
				AutoWrapMode = svAutoWrapMode;
			}
		}

		void PrnParseControl(byte b) // printer mode
		{
			switch (b) {
			case (byte)ControlCharacters.NUL:
				return;
			case (byte)ControlCharacters.SO:
				if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_SO) != 0 && !DirectPrn) {
					if ((ts.Language == Language.IdJapanese) &&
						(ts.KanjiCode == KanjiCodeId.IdJIS) &&
						(ts.JIS7Katakana) &&
						((ts.TermFlag & TerminalFlags.TF_FIXEDJIS) != 0)) {
						Gn[1] = CharacterSets.IdKatakana;
					}
					Glr[0] = 1; /* LS1 */
					return;
				}
				break;
			case (byte)ControlCharacters.SI:
				if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_SI) != 0 && !DirectPrn) {
					Glr[0] = 0; /* LS0 */
					return;
				}
				break;
			case (byte)ControlCharacters.DC1:
			case (byte)ControlCharacters.DC3:
				return;
			case (byte)ControlCharacters.ESC:
				ICount = 0;
				JustAfterESC = true;
				ParseMode = ParsingMode.ModeESC;
				teraprn.WriteToPrnFile(0, true); // flush prn buff
				return;
			case (byte)ControlCharacters.CSI:
				if (!Accept8BitCtrl(VTlevel, ts)) {
					PutChar(b); /* Disp C1 char in VT100 mode */
					return;
				}
				ClearParams();
				FirstPrm = true;
				ParseMode = ParsingMode.ModeCSI;
				teraprn.WriteToPrnFile(0, true); // flush prn buff
				teraprn.WriteToPrnFile(b, false);
				return;
			}
			/* send the uninterpreted character to printer */
			teraprn.WriteToPrnFile(b, true);
		}

		void ParseControl(byte b)
		{
			if (PrinterMode) { // printer mode
				PrnParseControl(b);
				return;
			}

			if (b >= 0x80) /* C1 char */
			{
				/* English mode */
				if (ts.Language == Language.IdEnglish) {
					if (!Accept8BitCtrl(VTlevel, ts)) {
						PutChar(b); /* Disp C1 char in VT100 mode */
						return;
					}
				}
				else { /* Japanese mode */
					if ((ts.TermFlag & TerminalFlags.TF_ACCEPT8BITCTRL) == 0) {
						return; /* ignore C1 char */
					}
					/* C1 chars are interpreted as C0 chars in VT100 mode */
					if (VTlevel < 2) {
						b = (byte)(b & 0x7F);
					}
				}
			}
			switch ((ControlCharacters)b) {
			/* C0 group */
			case ControlCharacters.ENQ:
				ttcmn.CommBinaryOut(cv, new String(ts.Answerback), ts.AnswerbackLen);
				break;
			case ControlCharacters.BEL:
				if (ts.Beep != BeepType.IdBeepOff)
					RingBell(ts.Beep);
				break;
			case ControlCharacters.BS:
				BackSpace();
				break;
			case ControlCharacters.HT:
				Tab();
				break;
			case ControlCharacters.LF:
				if (ts.CRReceive == NewLineModes.IdLF) {
					// 受信時の改行コードが LF の場合は、サーバから LF のみが送られてくると仮定し、
					// CR+LFとして扱うようにする。
					// cf. http://www.neocom.ca/forum/viewtopic.php?t=216
					// (2007.1.21 yutaka)
					CarriageReturn(true);
					LineFeed(b, true);
					break;
				}
				else if (ts.CRReceive == NewLineModes.IdAUTO) {
					// 9th Apr 2012: AUTO CR/LF mode (tentner)
					// a CR or LF will generated a CR+LF, if the next character is the opposite, it will be ignored
					if (PrevCharacter != (byte)ControlCharacters.CR || !PrevCRorLFGeneratedCRLF) {
						CarriageReturn(true);
						LineFeed(b, true);
						PrevCRorLFGeneratedCRLF = true;
					}
					else {
						PrevCRorLFGeneratedCRLF = false;
					}
					break;
				}
				goto case ControlCharacters.VT;

			case ControlCharacters.VT:
				LineFeed(b, true);
				break;

			case ControlCharacters.FF:
				if ((ts.AutoWinSwitch > 0) && JustAfterESC) {
					ttcmn.CommInsert1Byte(cv, b);
					ttcmn.CommInsert1Byte(cv, (byte)ControlCharacters.ESC);
					ChangeEmu = WindowId.IdTEK;  /* Enter TEK Mode */
				}
				else
					LineFeed(b, true);
				break;
			case ControlCharacters.CR:
				if (ts.CRReceive == NewLineModes.IdAUTO) {
					// 9th Apr 2012: AUTO CR/LF mode (tentner)
					// a CR or LF will generated a CR+LF, if the next character is the opposite, it will be ignored
					if (PrevCharacter != (byte)ControlCharacters.LF || !PrevCRorLFGeneratedCRLF) {
						CarriageReturn(true);
						LineFeed(b, true);
						PrevCRorLFGeneratedCRLF = true;
					}
					else {
						PrevCRorLFGeneratedCRLF = false;
					}
				}
				else {
					CarriageReturn(true);
					if (ts.CRReceive == NewLineModes.IdCRLF) {
						ttcmn.CommInsert1Byte(cv, (byte)ControlCharacters.LF);
					}
				}
				break;
			case ControlCharacters.SO: /* LS1 */
				if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_SO) != 0) {
					if ((ts.Language == Language.IdJapanese) &&
						(ts.KanjiCode == KanjiCodeId.IdJIS) &&
						(ts.JIS7Katakana) &&
						((ts.TermFlag & TerminalFlags.TF_FIXEDJIS) != 0)) {
						Gn[1] = CharacterSets.IdKatakana;
					}

					Glr[0] = 1;
				}
				break;
			case ControlCharacters.SI: /* LS0 */
				if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_SI) != 0) {
					Glr[0] = 0;
				}
				break;
			case ControlCharacters.DLE:
				if ((ts.FTFlag & FileTransferFlags.FT_BPAUTO) != 0)
					ParseMode = ParsingMode.ModeDLE; /* Auto B-Plus activation */
				break;
			case ControlCharacters.CAN:
				if ((ts.FTFlag & FileTransferFlags.FT_ZAUTO) != 0)
					ParseMode = ParsingMode.ModeCAN; /* Auto ZMODEM activation */
													 //	else if (ts.AutoWinSwitch>0)
													 //		ChangeEmu = WindowId.IdTEK;  /* Enter TEK Mode */
				else
					ParseMode = ParsingMode.ModeFirst;
				break;
			case ControlCharacters.SUB:
				ParseMode = ParsingMode.ModeFirst;
				break;
			case ControlCharacters.ESC:
				ICount = 0;
				JustAfterESC = true;
				ParseMode = ParsingMode.ModeESC;
				break;
			case ControlCharacters.FS:
			case ControlCharacters.GS:
			case ControlCharacters.RS:
			case ControlCharacters.US:
				if (ts.AutoWinSwitch > 0) {
					ttcmn.CommInsert1Byte(cv, b);
					ChangeEmu = WindowId.IdTEK;  /* Enter TEK Mode */
				}
				break;

			/* C1 char */
			case ControlCharacters.IND:
				LineFeed(0, true);
				break;
			case ControlCharacters.NEL:
				LineFeed(0, true);
				CarriageReturn(true);
				break;
			case ControlCharacters.HTS:
				if ((ts.TabStopFlag & TabStopflags.TABF_HTS8) != 0)
					Buffer.SetTabStop();
				break;
			case ControlCharacters.RI:
				Buffer.CursorUpWithScroll();
				break;
			case ControlCharacters.SS2:
				if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_SS2) != 0) {
					GLtmp = 2;
					SSflag = true;
				}
				break;
			case ControlCharacters.SS3:
				if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_SS3) != 0) {
					GLtmp = 3;
					SSflag = true;
				}
				break;
			case ControlCharacters.DCS:
				ClearParams();
				ESCFlag = false;
				ParseMode = ParsingMode.ModeDCS;
				break;
			case ControlCharacters.SOS:
				ESCFlag = false;
				ParseMode = ParsingMode.ModeIgnore;
				break;
			case ControlCharacters.CSI:
				ClearParams();
				FirstPrm = true;
				ParseMode = ParsingMode.ModeCSI;
				break;
			case ControlCharacters.OSC:
				ClearParams();
				ParseMode = ParsingMode.ModeXS;
				break;
			case ControlCharacters.PM:
			case ControlCharacters.APC:
				ESCFlag = false;
				ParseMode = ParsingMode.ModeIgnore;
				break;
			}
		}

		void SaveCursor()
		{
			int i;
			TStatusBuff Buff;

			if (Buffer.isCursorOnStatusLine())
				Buff = SBuff2; // for status line
			else if (AltScr)
				Buff = SBuff3; // for alternate screen
			else
				Buff = SBuff1; // for main screen

			Buff.CursorX = VTDisp.CursorX;
			Buff.CursorY = VTDisp.CursorY;
			Buff.Attr = CharAttr;

			Buff.Glr[0] = Glr[0];
			Buff.Glr[1] = Glr[1];
			for (i = 0; i <= 3; i++)
				Buff.Gn[i] = Gn[i];

			Buff.AutoWrapMode = AutoWrapMode;
			Buff.RelativeOrgMode = RelativeOrgMode;
		}

		void RestoreCursor()
		{
			int i;
			TStatusBuff Buff;

			Buffer.UpdateStr();

			if (Buffer.isCursorOnStatusLine())
				Buff = SBuff2; // for status line
			else if (AltScr)
				Buff = SBuff3; // for alternate screen
			else
				Buff = SBuff1; // for main screen

			if (VTDisp.CursorX > VTDisp.NumOfColumns - 1)
				VTDisp.CursorX = VTDisp.NumOfColumns - 1;
			if (VTDisp.CursorY > VTDisp.NumOfLines - 1 - Buffer.StatusLine)
				VTDisp.CursorY = VTDisp.NumOfLines - 1 - Buffer.StatusLine;
			Buffer.MoveCursor(VTDisp.CursorX, VTDisp.CursorY);

			CharAttr = Buff.Attr;
			Buffer.BuffSetCurCharAttr(CharAttr);

			Glr[0] = Buff.Glr[0];
			Glr[1] = Buff.Glr[1];
			for (i = 0; i <= 3; i++)
				Gn[i] = Buff.Gn[i];

			AutoWrapMode = Buff.AutoWrapMode;
			RelativeOrgMode = Buff.RelativeOrgMode;
		}

		void AnswerTerminalType()
		{
			string Tmp;

			if (ts.TerminalID < TerminalId.IdVT320 || !keyboard.Send8BitMode)
				Tmp = "\x1b[?";
			else
				Tmp = "\x9b?";

			switch (ts.TerminalID) {
			case TerminalId.IdVT100:
				Tmp += "1;2";
				break;
			case TerminalId.IdVT100J:
				Tmp += "5;2";
				break;
			case TerminalId.IdVT101:
				Tmp += "1;0";
				break;
			case TerminalId.IdVT102:
				Tmp += "6";
				break;
			case TerminalId.IdVT102J:
				Tmp += "15";
				break;
			case TerminalId.IdVT220J:
				Tmp += "62;1;2;5;6;7;8";
				break;
			case TerminalId.IdVT282:
				Tmp += "62;1;2;4;5;6;7;8;10;11";
				break;
			case TerminalId.IdVT320:
				Tmp += "63;1;2;6;7;8";
				break;
			case TerminalId.IdVT382:
				Tmp += "63;1;2;4;5;6;7;8;10;15";
				break;
			case TerminalId.IdVT420:
				Tmp += "64;1;2;7;8;9;15;18;21";
				break;
			case TerminalId.IdVT520:
				Tmp += "65;1;2;7;8;9;12;18;19;21;23;24;42;44;45;46";
				break;
			case TerminalId.IdVT525:
				Tmp += "65;1;2;7;9;12;18;19;21;22;23;24;42;44;45;46";
				break;
			}
			Tmp += "c";

			ttcmn.CommBinaryOut(cv, Tmp, Tmp.Length); /* Report terminal ID */
		}

		void ESCSpace(byte b)
		{
			switch ((char)b) {
			case 'F':   // S7C1T
				keyboard.Send8BitMode = false;
				break;
			case 'G':   // S8C1T
				if (VTlevel >= 2) {
					keyboard.Send8BitMode = true;
				}
				break;
			}
		}

		void ESCSharp(byte b)
		{
			switch ((char)b) {
			case '8':  /* Fill screen with "E" (DECALN) */
				Buffer.BuffUpdateScroll();
				Buffer.BuffFillWithE();
				Buffer.CursorTop = 0;
				Buffer.CursorBottom = VTDisp.NumOfLines - 1 - Buffer.StatusLine;
				Buffer.CursorLeftM = 0;
				Buffer.CursorRightM = VTDisp.NumOfColumns - 1;
				Buffer.MoveCursor(0, 0);
				ParseMode = ParsingMode.ModeFirst;
				break;
			}
		}

		/* select double byte code set */
		void ESCDBCSSelect(byte b)
		{
			int Dist;

			if (ts.Language != Language.IdJapanese) return;

			switch (ICount) {
			case 1:
				if ((b == '@') || (b == 'B')) {
					Gn[0] = CharacterSets.IdKanji; /* Kanji -> G0 */
					if ((ts.TermFlag & TerminalFlags.TF_AUTOINVOKE) != 0)
						Glr[0] = 0; /* G0.GL */
				}
				break;
			case 2:
				/* Second intermediate char must be
			   '(' or ')' or '*' or '+'. */
				Dist = (IntChar[2] - '(') & 3; /* G0 - G3 */
				if ((b == '1') || (b == '3') ||
				(b == '@') || (b == 'B')) {
					Gn[Dist] = CharacterSets.IdKanji; /* Kanji -> G0-3 */
					if (((ts.TermFlag & TerminalFlags.TF_AUTOINVOKE) != 0) &&
						(Dist == 0))
						Glr[0] = 0; /* G0.GL */
				}
				break;
			}
		}

		void ESCSelectCode(byte b)
		{
			switch ((char)b) {
			case '0':
				if (ts.AutoWinSwitch > 0)
					ChangeEmu = WindowId.IdTEK; /* enter TEK mode */
				break;
			}
		}

		/* select single byte code set */
		void ESCSBCSSelect(byte b)
		{
			int Dist;

			/* Intermediate char must be '(' or ')' or '*' or '+'.	*/
			Dist = (IntChar[1] - '(') & 3; /* G0 - G3 */

			switch ((char)b) {
			case '0': Gn[Dist] = CharacterSets.IdSpecial; break;
			case '<': Gn[Dist] = CharacterSets.IdASCII; break;
			case '>': Gn[Dist] = CharacterSets.IdASCII; break;
			case 'A': Gn[Dist] = CharacterSets.IdASCII; break;
			case 'B': Gn[Dist] = CharacterSets.IdASCII; break;
			case 'H': Gn[Dist] = CharacterSets.IdASCII; break;
			case 'I':
				if (ts.Language == Language.IdJapanese)
					Gn[Dist] = CharacterSets.IdKatakana;
				break;
			case 'J': Gn[Dist] = CharacterSets.IdASCII; break;
			}

			if (((ts.TermFlag & TerminalFlags.TF_AUTOINVOKE) != 0) && (Dist == 0))
				Glr[0] = 0;  /* G0.GL */
		}

		void PrnParseEscape(byte b) // printer mode
		{
			int i;

			ParseMode = ParsingMode.ModeFirst;
			switch (ICount) {
			/* no intermediate char */
			case 0:
				switch ((char)b) {
				case '[': /* CSI */
					ClearParams();
					FirstPrm = true;
					teraprn.WriteToPrnFile((byte)ControlCharacters.ESC, false);
					teraprn.WriteToPrnFile((byte)'[', false);
					ParseMode = ParsingMode.ModeCSI;
					return;
				} /* end of case Icount=0 */
				break;
			/* one intermediate char */
			case 1:
				switch ((char)IntChar[1]) {
				case '$':
					if (!DirectPrn) {
						ESCDBCSSelect(b);
						return;
					}
					break;
				case '(':
				case ')':
				case '*':
				case '+':
					if (!DirectPrn) {
						ESCSBCSSelect(b);
						return;
					}
					break;
				}
				break;
			/* two intermediate char */
			case 2:
				if ((!DirectPrn) &&
				(IntChar[1] == '$') &&
				('(' <= IntChar[2]) &&
				(IntChar[2] <= '+')) {
					ESCDBCSSelect(b);
					return;
				}
				break;
			}
			// send the uninterpreted sequence to printer
			teraprn.WriteToPrnFile((byte)ControlCharacters.ESC, false);
			for (i = 1; i <= ICount; i++)
				teraprn.WriteToPrnFile(IntChar[i], false);
			teraprn.WriteToPrnFile(b, true);
		}

		void ParseEscape(byte b) /* b is the final char */
		{
			if (PrinterMode) { // printer mode
				PrnParseEscape(b);
				return;
			}

			switch (ICount) {
			case 0: /* no intermediate char */
				switch ((char)b) {
				case '6': // DECBI
					if (VTDisp.CursorY >= Buffer.CursorTop && VTDisp.CursorY <= Buffer.CursorBottom &&
						VTDisp.CursorX >= Buffer.CursorLeftM && VTDisp.CursorX <= Buffer.CursorRightM) {
						if (VTDisp.CursorX == Buffer.CursorLeftM) {
							Buffer.BuffScrollRight(1);
						}
						else {
							Buffer.MoveCursor(VTDisp.CursorX - 1, VTDisp.CursorY);
						}
					}
					break;
				case '7': SaveCursor(); break;
				case '8': RestoreCursor(); break;
				case '9': // DECFI
					if (VTDisp.CursorY >= Buffer.CursorTop && VTDisp.CursorY <= Buffer.CursorBottom &&
						VTDisp.CursorX >= Buffer.CursorLeftM && VTDisp.CursorX <= Buffer.CursorRightM) {
						if (VTDisp.CursorX == Buffer.CursorRightM) {
							Buffer.BuffScrollLeft(1);
						}
						else {
							Buffer.MoveCursor(VTDisp.CursorX + 1, VTDisp.CursorY);
						}
					}
					break;
				case '=': keyboard.AppliKeyMode = true; break;
				case '>': keyboard.AppliKeyMode = false; break;
				case 'D': /* IND */
					LineFeed(0, true);
					break;
				case 'E': /* NEL */
					Buffer.MoveCursor(0, VTDisp.CursorY);
					LineFeed(0, true);
					break;
				case 'H': /* HTS */
					if ((ts.TabStopFlag & TabStopflags.TABF_HTS7) != 0)
						Buffer.SetTabStop();
					break;
				case 'M': /* RI */
					Buffer.CursorUpWithScroll();
					break;
				case 'N': /* SS2 */
					if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_SS2) != 0) {
						GLtmp = 2;
						SSflag = true;
					}
					break;
				case 'O': /* SS3 */
					if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_SS3) != 0) {
						GLtmp = 3;
						SSflag = true;
					}
					break;
				case 'P': /* DCS */
					ClearParams();
					ESCFlag = false;
					ParseMode = ParsingMode.ModeDCS;
					return;
				case 'X': /* SOS */
				case '^': /* APC */
				case '_': /* PM  */
					ESCFlag = false;
					ParseMode = ParsingMode.ModeSOS;
					return;
				case 'Z': /* DECID */
					AnswerTerminalType();
					break;
				case '[': /* CSI */
					ClearParams();
					FirstPrm = true;
					ParseMode = ParsingMode.ModeCSI;
					return;
				case '\\': break; /* ST */
				case ']': /* XTERM sequence (OSC) */
					ClearParams();
					ParseMode = ParsingMode.ModeXS;
					return;
				case 'c': /* Hardware reset */
					HideStatusLine();
					ResetTerminal();
					keyboard.ClearUserKey();
					Buffer.ClearBuffer();
					if (ts.PortType == PortTypeId.IdSerial) // reset serial port
						commlib.CommResetSerial(ref ts, ref cv, true);
					break;
				case 'g': /* Visual Bell (screen original?) */
					RingBell(BeepType.IdBeepVisual);
					break;
				case 'n': /* LS2 */
					if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_LS2) != 0) {
						Glr[0] = 2;
					}
					break;
				case 'o': /* LS3 */
					if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_LS3) != 0) {
						Glr[0] = 3;
					}
					break;
				case '|': /* LS3R */
					if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_LS3R) != 0) {
						Glr[1] = 3;
					}
					break;
				case '}': /* LS2R */
					if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_LS2R) != 0) {
						Glr[1] = 2;
					}
					break;
				case '~': /* LS1R */
					if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_LS1R) != 0) {
						Glr[1] = 1;
					}
					break;
				}
				break;
			/* end of case Icount=0 */

			case 1: /* one intermediate char */
				switch ((char)IntChar[1]) {
				case ' ': ESCSpace(b); break;
				case '#': ESCSharp(b); break;
				case '$': ESCDBCSSelect(b); break;
				case '%': break;
				case '(':
				case ')':
				case '*':
				case '+':
					ESCSBCSSelect(b);
					break;
				}
				break;

			case 2: /* two intermediate char */
				if ((IntChar[1] == '$') && ('(' <= IntChar[2]) && (IntChar[2] <= '+'))
					ESCDBCSSelect(b);
				else if ((IntChar[1] == '%') && (IntChar[2] == '!'))
					ESCSelectCode(b);
				break;
			}
			ParseMode = ParsingMode.ModeFirst;
		}

		void EscapeSequence(byte b)
		{
			if (b <= (byte)ControlCharacters.US)
				ParseControl(b);
			else if ((b >= 0x20) && (b <= 0x2F)) {
				// TODO: ICount が IntCharMax に達した時、最後の IntChar を置き換えるのは妥当?
				if (ICount < IntCharMax)
					ICount++;
				IntChar[ICount] = b;
			}
			else if ((b >= 0x30) && (b <= 0x7E))
				ParseEscape(b);
			else if ((b >= 0x80) && (b <= 0x9F))
				ParseControl(b);
			else if (b >= 0xA0) {
				ParseMode = ParsingMode.ModeFirst;
				ParseFirst(b);
			}

			JustAfterESC = false;
		}

		void CheckParamVal(ref int p, int m)
		{
			if ((p) == 0) {
				(p) = 1;
			}
			else if ((p) > (m) || p < 0) {
				(p) = (m);
			}
		}

		void CheckParamValMax(ref int p, int m)
		{
			if ((p) > (m) || p <= 0) {
				(p) = (m);
			}
		}

		void RequiredParams(int n)
		{
			if ((n) > 1) {
				while (NParam < n) {
					NParam++;
					Param[NParam] = 0;
					NSParam[NParam] = 0;
				}
			}
		}

		void CSInsertCharacter()
		{
			// Insert space characters at cursor
			CheckParamVal(ref Param[1], VTDisp.NumOfColumns);

			Buffer.BuffUpdateScroll();
			Buffer.BuffInsertSpace(Param[1]);
		}

		void CSCursorUp(bool AffectMargin)
		{
			int topMargin, NewY;

			CheckParamVal(ref Param[1], VTDisp.CursorY);

			if (AffectMargin && VTDisp.CursorY >= Buffer.CursorTop)
				topMargin = Buffer.CursorTop;
			else
				topMargin = 0;

			NewY = VTDisp.CursorY - Param[1];
			if (NewY < topMargin)
				NewY = topMargin;

			Buffer.MoveCursor(VTDisp.CursorX, NewY);
		}

		void CSCursorUp1()
		{
			Buffer.MoveCursor(Buffer.CursorLeftM, VTDisp.CursorY);
			CSCursorUp(true);
		}

		void CSCursorDown(bool AffectMargin)
		{
			int bottomMargin, NewY;

			if (AffectMargin && VTDisp.CursorY <= Buffer.CursorBottom)
				bottomMargin = Buffer.CursorBottom;
			else
				bottomMargin = VTDisp.NumOfLines - Buffer.StatusLine - 1;

			CheckParamVal(ref Param[1], bottomMargin);

			NewY = VTDisp.CursorY + Param[1];
			if (NewY > bottomMargin)
				NewY = bottomMargin;

			Buffer.MoveCursor(VTDisp.CursorX, NewY);
		}

		void CSCursorDown1()
		{
			Buffer.MoveCursor(Buffer.CursorLeftM, VTDisp.CursorY);
			CSCursorDown(true);
		}

		void CSScreenErase()
		{
			Buffer.BuffUpdateScroll();
			switch (Param[1]) {
			case 0:
				// <ESC>[H(Cursor in left upper corner)によりカーソルが左上隅を指している場合、
				// <ESC>[Jは<ESC>[2Jと同じことなので、処理を分け、現行バッファをスクロールアウト
				// させるようにする。(2005.5.29 yutaka)
				// コンフィグレーションで切り替えられるようにした。(2008.5.3 yutaka)
				if (ts.ScrollWindowClearScreen &&
					(VTDisp.CursorX == 0 && VTDisp.CursorY == 0)) {
					//	Erase screen (scroll out)
					Buffer.BuffClearScreen();
					ttwinman.HVTWin.Update();

				}
				else {
					//	Erase characters from cursor to the end of screen
					Buffer.BuffEraseCurToEnd();
				}
				break;

			case 1:
				//	Erase characters from home to cursor
				Buffer.BuffEraseHomeToCur();
				break;

			case 2:
				//	Erase screen (scroll out)
				Buffer.BuffClearScreen();
				ttwinman.HVTWin.Update();
				if (ClearThenHome && !Buffer.isCursorOnStatusLine()) {
					if (RelativeOrgMode) {
						Buffer.MoveCursor(0, 0);
					}
					else {
						Buffer.MoveCursor(Buffer.CursorLeftM, Buffer.CursorTop);
					}
				}
				break;
			}
		}

		void CSQSelScreenErase()
		{
			Buffer.BuffUpdateScroll();
			switch (Param[1]) {
			case 0:
				//	Erase characters from cursor to end
				Buffer.BuffSelectedEraseCurToEnd();
				break;

			case 1:
				//	Erase characters from home to cursor
				Buffer.BuffSelectedEraseHomeToCur();
				break;

			case 2:
				//	Erase entire screen
				Buffer.BuffSelectedEraseScreen();
				break;
			}
		}

		void CSInsertLine()
		{
			// Insert lines at current position
			int Count, YEnd;

			if (VTDisp.CursorY < Buffer.CursorTop || VTDisp.CursorY > Buffer.CursorBottom) {
				return;
			}

			CheckParamVal(ref Param[1], VTDisp.NumOfLines);

			Count = Param[1];

			YEnd = Buffer.CursorBottom;
			if (VTDisp.CursorY > YEnd)
				YEnd = VTDisp.NumOfLines - 1 - Buffer.StatusLine;

			if (Count > YEnd + 1 - VTDisp.CursorY)
				Count = YEnd + 1 - VTDisp.CursorY;

			Buffer.BuffInsertLines(Count, YEnd);
		}

		void CSLineErase()
		{
			Buffer.BuffUpdateScroll();
			switch (Param[1]) {
			case 0: /* erase char from cursor to end of line */
				Buffer.BuffEraseCharsInLine(VTDisp.CursorX, VTDisp.NumOfColumns - VTDisp.CursorX);
				break;

			case 1: /* erase char from start of line to cursor */
				Buffer.BuffEraseCharsInLine(0, VTDisp.CursorX + 1);
				break;

			case 2: /* erase entire line */
				Buffer.BuffEraseCharsInLine(0, VTDisp.NumOfColumns);
				break;
			}
		}

		void CSQSelLineErase()
		{
			Buffer.BuffUpdateScroll();
			switch (Param[1]) {
			case 0: /* erase char from cursor to end of line */
				Buffer.BuffSelectedEraseCharsInLine(VTDisp.CursorX, VTDisp.NumOfColumns - VTDisp.CursorX);
				break;

			case 1: /* erase char from start of line to cursor */
				Buffer.BuffSelectedEraseCharsInLine(0, VTDisp.CursorX + 1);
				break;

			case 2: /* erase entire line */
				Buffer.BuffSelectedEraseCharsInLine(0, VTDisp.NumOfColumns);
				break;
			}
		}

		void CSDeleteNLines()
		// Delete lines from current line
		{
			int Count, YEnd;

			if (VTDisp.CursorY < Buffer.CursorTop || VTDisp.CursorY > Buffer.CursorBottom) {
				return;
			}

			CheckParamVal(ref Param[1], VTDisp.NumOfLines);
			Count = Param[1];

			YEnd = Buffer.CursorBottom;
			if (VTDisp.CursorY > YEnd)
				YEnd = VTDisp.NumOfLines - 1 - Buffer.StatusLine;

			if (Count > YEnd + 1 - VTDisp.CursorY)
				Count = YEnd + 1 - VTDisp.CursorY;

			Buffer.BuffDeleteLines(Count, YEnd);
		}

		void CSDeleteCharacter()
		{
			// Delete characters in current line from cursor
			CheckParamVal(ref Param[1], VTDisp.NumOfColumns);

			Buffer.BuffUpdateScroll();
			Buffer.BuffDeleteChars(Param[1]);
		}

		void CSEraseCharacter()
		{
			CheckParamVal(ref Param[1], VTDisp.NumOfColumns);

			Buffer.BuffUpdateScroll();
			Buffer.BuffEraseChars(Param[1]);
		}

		void CSScrollUp()
		{
			// TODO: スクロールの最大値を端末行数に制限すべきか要検討
			CheckParamVal(ref Param[1], int.MaxValue);

			Buffer.BuffUpdateScroll();
			Buffer.BuffRegionScrollUpNLines(Param[1]);
		}

		void CSScrollDown()
		{
			CheckParamVal(ref Param[1], VTDisp.NumOfLines);

			Buffer.BuffUpdateScroll();
			Buffer.BuffRegionScrollDownNLines(Param[1]);
		}

		void CSForwardTab()
		{
			CheckParamVal(ref Param[1], VTDisp.NumOfColumns);
			Buffer.CursorForwardTab(Param[1], AutoWrapMode);
		}

		void CSBackwardTab()
		{
			CheckParamVal(ref Param[1], VTDisp.NumOfColumns);
			Buffer.CursorBackwardTab(Param[1]);
		}

		void CSMoveToColumnN()
		{
			CheckParamVal(ref Param[1], VTDisp.NumOfColumns);

			Param[1]--;

			if (RelativeOrgMode) {
				if (Buffer.CursorLeftM + Param[1] > Buffer.CursorRightM)
					Buffer.MoveCursor(Buffer.CursorRightM, VTDisp.CursorY);
				else
					Buffer.MoveCursor(Buffer.CursorLeftM + Param[1], VTDisp.CursorY);
			}
			else {
				Buffer.MoveCursor(Param[1], VTDisp.CursorY);
			}
		}

		void CSCursorRight(bool AffectMargin)
		{
			int NewX, rightMargin;

			CheckParamVal(ref Param[1], VTDisp.NumOfColumns);

			if (AffectMargin && VTDisp.CursorX <= Buffer.CursorRightM) {
				rightMargin = Buffer.CursorRightM;
			}
			else {
				rightMargin = VTDisp.NumOfColumns - 1;
			}

			NewX = VTDisp.CursorX + Param[1];
			if (NewX > rightMargin)
				NewX = rightMargin;

			Buffer.MoveCursor(NewX, VTDisp.CursorY);
		}

		void CSCursorLeft(bool AffectMargin)
		{
			int NewX, leftMargin;

			CheckParamVal(ref Param[1], VTDisp.NumOfColumns);

			if (AffectMargin && VTDisp.CursorX >= Buffer.CursorLeftM) {
				leftMargin = Buffer.CursorLeftM;
			}
			else {
				leftMargin = 0;
			}

			NewX = VTDisp.CursorX - Param[1];
			if (NewX < leftMargin) {
				NewX = leftMargin;
			}

			Buffer.MoveCursor(NewX, VTDisp.CursorY);
		}

		void CSMoveToLineN()
		{
			CheckParamVal(ref Param[1], VTDisp.NumOfLines - Buffer.StatusLine);

			if (RelativeOrgMode) {
				if (Buffer.CursorTop + Param[1] - 1 > Buffer.CursorBottom)
					Buffer.MoveCursor(VTDisp.CursorX, Buffer.CursorBottom);
				else
					Buffer.MoveCursor(VTDisp.CursorX, Buffer.CursorTop + Param[1] - 1);
			}
			else {
				if (Param[1] > VTDisp.NumOfLines - Buffer.StatusLine)
					Buffer.MoveCursor(VTDisp.CursorX, VTDisp.NumOfLines - 1 - Buffer.StatusLine);
				else
					Buffer.MoveCursor(VTDisp.CursorX, Param[1] - 1);
			}
			Fallbacked = false;
		}

		void CSMoveToXY()
		{
			int NewX, NewY;

			RequiredParams(2);
			CheckParamVal(ref Param[1], VTDisp.NumOfLines - Buffer.StatusLine);
			CheckParamVal(ref Param[2], VTDisp.NumOfColumns);

			NewY = Param[1] - 1;
			NewX = Param[2] - 1;

			if (Buffer.isCursorOnStatusLine())
				NewY = VTDisp.CursorY;
			else if (RelativeOrgMode) {
				NewX += Buffer.CursorLeftM;
				if (NewX > Buffer.CursorRightM)
					NewX = Buffer.CursorRightM;

				NewY += Buffer.CursorTop;
				if (NewY > Buffer.CursorBottom)
					NewY = Buffer.CursorBottom;
			}
			else {
				if (NewY > VTDisp.NumOfLines - 1 - Buffer.StatusLine)
					NewY = VTDisp.NumOfLines - 1 - Buffer.StatusLine;
			}

			Buffer.MoveCursor(NewX, NewY);
			Fallbacked = false;
		}

		void CSDeleteTabStop()
		{
			Buffer.ClearTabStop(Param[1]);
		}

		void CS_h_Mode()        // SM
		{
			switch (Param[1]) {
			case 2: // KAM
				ttwinman.KeybEnabled = false; break;
			case 4: // IRM
				InsertMode = true; break;
			case 12:    // SRM
				ts.LocalEcho = false;
				if (cv.Ready && cv.TelFlag && (ts.TelEcho > 0))
					telnet.TelChangeEcho();
				break;
			case 20:    // ControlCharacters.LF/NL
				LFMode = true;
				ts.CRSend = NewLineModes.IdCRLF;
				cv.CRSend = NewLineModes.IdCRLF;
				break;
			case 33:    // WYSTCURM
				if ((ts.WindowFlag & WindowFlags.WF_CURSORCHANGE) != 0) {
					ts.NonblinkingCursor = true;
					VTDisp.ChangeCaret();
				}
				break;
			case 34:    // WYULCURM
				if ((ts.WindowFlag & WindowFlags.WF_CURSORCHANGE) != 0) {
					ts.CursorShape = CursorShapes.IdHCur;
					VTDisp.ChangeCaret();
				}
				break;
			}
		}

		void CS_i_Mode()        // MC
		{
			switch (Param[1]) {
			/* print screen */
			//  PrintEX --	true: print screen
			//		false: scroll region
			case 0:
				if ((ts.TermFlag & TerminalFlags.TF_PRINTERCTRL) != 0) {
					Buffer.BuffPrint(!PrintEX);
				}
				break;
			/* printer controller mode off */
			case 4: break; /* See PrnParseCS() */
						   /* printer controller mode on */
			case 5:
				if ((ts.TermFlag & TerminalFlags.TF_PRINTERCTRL) != 0) {
					if (!AutoPrintMode)
						teraprn.OpenPrnFile();
					DirectPrn = (ts.PrnDev[0] != 0);
					PrinterMode = true;
				}
				break;
			}
		}

		void CS_l_Mode()        // RM
		{
			switch (Param[1]) {
			case 2: // KAM
				ttwinman.KeybEnabled = true; break;
			case 4: // IRM
				InsertMode = false; break;
			case 12:    // SRM
				ts.LocalEcho = true;
				if (cv.Ready && cv.TelFlag && (ts.TelEcho > 0))
					telnet.TelChangeEcho();
				break;
			case 20:    // LF/NL
				LFMode = false;
				ts.CRSend = NewLineModes.IdCR;
				cv.CRSend = NewLineModes.IdCR;
				break;
			case 33:    // WYSTCURM
				if ((ts.WindowFlag & WindowFlags.WF_CURSORCHANGE) != 0) {
					ts.NonblinkingCursor = false;
					VTDisp.ChangeCaret();
				}
				break;
			case 34:    // WYULCURM
				if ((ts.WindowFlag & WindowFlags.WF_CURSORCHANGE) != 0) {
					ts.CursorShape = CursorShapes.IdBlkCur;
					VTDisp.ChangeCaret();
				}
				break;
			}
		}

		void CS_n_Mode()        // DSR
		{
			string Report;
			int X, Y;

			switch (Param[1]) {
			case 5:
				/* Device Status Report -> Ready */
				SendCSIstr("0n", 0);
				break;
			case 6:
				/* Cursor Position Report */
				if (Buffer.isCursorOnStatusLine()) {
					X = VTDisp.CursorX + 1;
					Y = 1;
				}
				else if (RelativeOrgMode) {
					X = VTDisp.CursorX - Buffer.CursorLeftM + 1;
					Y = VTDisp.CursorY - Buffer.CursorTop + 1;
				}
				else {
					X = VTDisp.CursorX + 1;
					Y = VTDisp.CursorY + 1;
				}
				Report = String.Format("{0};{1}R", Y, VTDisp.CursorX + 1);
				SendCSIstr(Report, Report.Length);
				break;
			}
		}

		void ParseSGRParams(ref TCharAttr attr, ref TCharAttr mask, int start)
		{
			int i, j, P, r, g, b, color;

			for (i = start; i <= NParam; i++) {
				P = Param[i];
				switch (P) {
				case 0: /* Clear all */
					attr.Attr = VTDisp.DefCharAttr.Attr;
					attr.Attr2 = VTDisp.DefCharAttr.Attr2 | (attr.Attr2 & AttributeBitMasks.Attr2Protect);
					attr.Fore = VTDisp.DefCharAttr.Fore;
					attr.Back = VTDisp.DefCharAttr.Back;
					mask.Attr = AttributeBitMasks.AttrSgrMask;
					mask.Attr2 = AttributeBitMasks.Attr2ColorMask;
					break;

				case 1: /* Bold */
					attr.Attr |= AttributeBitMasks.AttrBold;
					mask.Attr |= AttributeBitMasks.AttrBold;
					break;

				case 4: /* Under line */
					attr.Attr |= AttributeBitMasks.AttrUnder;
					mask.Attr |= AttributeBitMasks.AttrUnder;
					break;

				case 5: /* Blink */
					attr.Attr |= AttributeBitMasks.AttrBlink;
					mask.Attr |= AttributeBitMasks.AttrBlink;
					break;

				case 7: /* Reverse */
					attr.Attr |= AttributeBitMasks.AttrReverse;
					mask.Attr |= AttributeBitMasks.AttrReverse;
					break;

				case 22:    /* Bold off */
					attr.Attr &= ~AttributeBitMasks.AttrBold;
					mask.Attr |= AttributeBitMasks.AttrBold;
					break;

				case 24:    /* Under line off */
					attr.Attr &= ~AttributeBitMasks.AttrUnder;
					mask.Attr |= AttributeBitMasks.AttrUnder;
					break;

				case 25:    /* Blink off */
					attr.Attr &= ~AttributeBitMasks.AttrBlink;
					mask.Attr |= AttributeBitMasks.AttrBlink;
					break;

				case 27:    /* Reverse off */
					attr.Attr &= ~AttributeBitMasks.AttrReverse;
					mask.Attr |= AttributeBitMasks.AttrReverse;
					break;

				case 30:
				case 31:
				case 32:
				case 33:
				case 34:
				case 35:
				case 36:
				case 37:    /* text color */
					attr.Attr2 |= AttributeBitMasks.Attr2Fore;
					mask.Attr2 |= AttributeBitMasks.Attr2Fore;
					attr.Fore = (ColorCodes)(P - 30);
					break;

				case 38:    /* text color (256color mode) */
					if ((ts.ColorFlag & ColorFlags.CF_XTERM256) != 0) {
						/*
						 * Change foreground color. accept following formats.
						 *
						 * 38 ; 2 ; r ; g ; b
						 * 38 ; 2 : r : g : b
						 * 38 : 2 : r : g : b
						 * 38 ; 5 ; idx
						 * 38 ; 5 : idx
						 * 38 : 5 : idx
						 *
						 */
						color = -1;
						j = 0;
						if (NSParam[i] > 0) {
							P = SubParam[i][1];
							j++;
						}
						else if (i < NParam) {
							P = Param[i + 1];
							if (P == 2 || P == 5) {
								i++;
							}
						}
						switch (P) {
						case 2:
							r = g = b = 0;
							if (NSParam[i] > 0) {
								if (j < NSParam[i]) {
									r = SubParam[i][++j];
									if (j < NSParam[i]) {
										g = SubParam[i][++j];
									}
									if (j < NSParam[i]) {
										b = SubParam[i][++j];
									}
									color = VTDisp.DispFindClosestColor(r, g, b);
								}
							}
							else if (i < NParam && NSParam[i + 1] > 0) {
								r = Param[++i];
								g = SubParam[i][1];
								if (NSParam[i] > 1) {
									b = SubParam[i][2];
								}
								color = VTDisp.DispFindClosestColor(r, g, b);
							}
							else if (i + 2 < NParam) {
								r = Param[++i];
								g = Param[++i];
								b = Param[++i];
								color = VTDisp.DispFindClosestColor(r, g, b);
							}
							break;
						case 5:
							if (NSParam[i] > 0) {
								if (j < NSParam[i]) {
									color = SubParam[i][++j];
								}
							}
							else if (i < NParam) {
								color = Param[++i];
							}
							break;
						}
						if (color >= 0 && color < 256) {
							attr.Attr2 |= AttributeBitMasks.Attr2Fore;
							mask.Attr2 |= AttributeBitMasks.Attr2Fore;
							attr.Fore = (ColorCodes)color;
						}
					}
					break;

				case 39:    /* Reset text color */
					attr.Attr2 &= ~AttributeBitMasks.Attr2Fore;
					mask.Attr2 |= AttributeBitMasks.Attr2Fore;
					attr.Fore = (ColorCodes)AttributeBitMasks.AttrDefaultFG;
					break;

				case 40:
				case 41:
				case 42:
				case 43:
				case 44:
				case 45:
				case 46:
				case 47:    /* Back color */
					attr.Attr2 |= AttributeBitMasks.Attr2Back;
					mask.Attr2 |= AttributeBitMasks.Attr2Back;
					attr.Back = (ColorCodes)(P - 40);
					break;

				case 48:    /* Back color (256color mode) */
					if ((ts.ColorFlag & ColorFlags.CF_XTERM256) != 0) {
						color = -1;
						j = 0;
						if (NSParam[i] > 0) {
							P = SubParam[i][1];
							j++;
						}
						else if (i < NParam) {
							P = Param[i + 1];
							if (P == 2 || P == 5) {
								i++;
							}
						}
						switch (P) {
						case 2:
							r = g = b = 0;
							if (NSParam[i] > 0) {
								if (j < NSParam[i]) {
									r = SubParam[i][++j];
									if (j < NSParam[i]) {
										g = SubParam[i][++j];
									}
									if (j < NSParam[i]) {
										b = SubParam[i][++j];
									}
									color = VTDisp.DispFindClosestColor(r, g, b);
								}
							}
							else if (i < NParam && NSParam[i + 1] > 0) {
								r = Param[++i];
								g = SubParam[i][1];
								if (NSParam[i] > 1) {
									b = SubParam[i][2];
								}
								color = VTDisp.DispFindClosestColor(r, g, b);
							}
							else if (i + 2 < NParam) {
								r = Param[++i];
								g = Param[++i];
								b = Param[++i];
								color = VTDisp.DispFindClosestColor(r, g, b);
							}
							break;
						case 5:
							if (NSParam[i] > 0) {
								if (j < NSParam[i]) {
									color = SubParam[i][++j];
								}
							}
							else if (i < NParam) {
								color = Param[++i];
							}
							break;
						}
						if (color >= 0 && color < 256) {
							attr.Attr2 |= AttributeBitMasks.Attr2Back;
							mask.Attr2 |= AttributeBitMasks.Attr2Back;
							attr.Back = (ColorCodes)color;
						}
					}
					break;

				case 49:    /* Reset back color */
					attr.Attr2 &= ~AttributeBitMasks.Attr2Back;
					mask.Attr2 |= AttributeBitMasks.Attr2Back;
					attr.Back = (ColorCodes)AttributeBitMasks.AttrDefaultBG;
					break;

				case 90:
				case 91:
				case 92:
				case 93:
				case 94:
				case 95:
				case 96:
				case 97:    /* aixterm style text color */
					if ((ts.ColorFlag & ColorFlags.CF_AIXTERM16) != 0) {
						attr.Attr2 |= AttributeBitMasks.Attr2Fore;
						mask.Attr2 |= AttributeBitMasks.Attr2Fore;
						attr.Fore = (ColorCodes)(P - 90 + 8);
					}
					break;

				case 100:
					if ((ts.ColorFlag & ColorFlags.CF_AIXTERM16) == 0) {
						/* Reset text and back color */
						attr.Attr2 &= ~(AttributeBitMasks.Attr2Fore | AttributeBitMasks.Attr2Back);
						mask.Attr2 |= AttributeBitMasks.Attr2ColorMask;
						attr.Fore = (ColorCodes)AttributeBitMasks.AttrDefaultFG;
						attr.Back = (ColorCodes)AttributeBitMasks.AttrDefaultBG;
						break;
					}
					/* fall through to aixterm style back color */
					goto case 101;
				case 101:
				case 102:
				case 103:
				case 104:
				case 105:
				case 106:
				case 107:   /* aixterm style back color */
					if ((ts.ColorFlag & ColorFlags.CF_AIXTERM16) != 0) {
						attr.Attr2 |= AttributeBitMasks.Attr2Back;
						mask.Attr2 |= AttributeBitMasks.Attr2Back;
						attr.Back = (ColorCodes)(P - 100 + 8);
					}
					break;
				}
			}
		}

		void CSSetAttr()        // SGR
		{
			TCharAttr dummy = new TCharAttr();
			Buffer.UpdateStr();
			ParseSGRParams(ref CharAttr, ref dummy, 1);
			Buffer.BuffSetCurCharAttr(CharAttr);
		}

		void CSSetScrollRegion()
		{
			if (Buffer.isCursorOnStatusLine()) {
				Buffer.MoveCursor(0, VTDisp.CursorY);
				return;
			}

			RequiredParams(2);
			CheckParamVal(ref Param[1], VTDisp.NumOfLines - Buffer.StatusLine);
			CheckParamValMax(ref Param[2], VTDisp.NumOfLines - Buffer.StatusLine);

			if (Param[1] >= Param[2])
				return;

			Buffer.CursorTop = Param[1] - 1;
			Buffer.CursorBottom = Param[2] - 1;

			if (RelativeOrgMode)
				// TODO: 左マージンを無視してる。要実機確認。
				Buffer.MoveCursor(0, Buffer.CursorTop);
			else
				Buffer.MoveCursor(0, 0);
		}

		void CSSetLRScrollRegion()  // DECSLRM
		{
			//	TODO: ステータスライン上での挙動確認。
			//	if (Buffer.isCursorOnStatusLine()) {
			//		Buffer.MoveCursor(0,VTDisp.CursorY);
			//		return;
			//	}

			RequiredParams(2);
			CheckParamVal(ref Param[1], VTDisp.NumOfColumns);
			CheckParamValMax(ref Param[2], VTDisp.NumOfColumns);

			if (Param[1] >= Param[2])
				return;

			Buffer.CursorLeftM = Param[1] - 1;
			Buffer.CursorRightM = Param[2] - 1;

			if (RelativeOrgMode)
				Buffer.MoveCursor(Buffer.CursorLeftM, Buffer.CursorTop);
			else
				Buffer.MoveCursor(0, 0);
		}

		void CSSunSequence() /* Sun terminal private sequences */
		{
			int x, y, len;
			string Report;
			TStack t;

			switch (Param[1]) {
			case 1: // De-iconify window
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWCHANGE) != 0)
					VTDisp.DispShowWindow(WindowType.WINDOW_RESTORE);
				break;
			case 2: // Iconify window
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWCHANGE) != 0)
					VTDisp.DispShowWindow(WindowType.WINDOW_MINIMIZE);
				break;
			case 3: // set window position
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWCHANGE) != 0) {
					RequiredParams(3);
					VTDisp.DispMoveWindow(Param[2], Param[3]);
				}
				break;
			case 4: // set window size
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWCHANGE) != 0) {
					RequiredParams(3);
					VTDisp.DispResizeWin(Param[3], Param[2]);
				}
				break;
			case 5: // Raise window
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWCHANGE) != 0)
					VTDisp.DispShowWindow(WindowType.WINDOW_RAISE);
				break;
			case 6: // Lower window
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWCHANGE) != 0)
					VTDisp.DispShowWindow(WindowType.WINDOW_LOWER);
				break;
			case 7: // Refresh window
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWCHANGE) != 0)
					VTDisp.DispShowWindow(WindowType.WINDOW_REFRESH);
				break;
			case 8: /* set terminal size */
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWCHANGE) != 0) {
					RequiredParams(3);
					if (Param[2] <= 1) Param[2] = 24;
					if (Param[3] <= 1) Param[3] = 80;
					ChangeTerminalSize(Param[3], Param[2]);
				}
				break;
			case 9: // Maximize/Restore window
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWCHANGE) != 0) {
					RequiredParams(2);
					if (Param[2] == 0) {
						VTDisp.DispShowWindow(WindowType.WINDOW_RESTORE);
					}
					else if (Param[2] == 1) {
						VTDisp.DispShowWindow(WindowType.WINDOW_MAXIMIZE);
					}
				}
				break;

			case 10: // Full-screen
					 /*
					  * 本来ならば PuTTY のようなフルスクリーンモードを実装するべきだが、
					  * とりあえずは手抜きで最大化を利用する
					  */
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWCHANGE) != 0) {
					RequiredParams(2);
					switch (Param[2]) {
					case 0:
						VTDisp.DispShowWindow(WindowType.WINDOW_RESTORE);
						break;
					case 1:
						VTDisp.DispShowWindow(WindowType.WINDOW_MAXIMIZE);
						break;
					case 2:
						VTDisp.DispShowWindow(WindowType.WINDOW_TOGGLE_MAXIMIZE);
						break;
					}
				}
				break;

			case 11: // Report window state
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWREPORT) != 0) {
					Report = String.Format("{0}t", VTDisp.DispWindowIconified() ? 2 : 1);
					SendCSIstr(Report, Report.Length);
				}
				break;
			case 13: // Report window position
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWREPORT) != 0) {
					RequiredParams(2);
					switch (Param[2]) {
					case 0:
					case 1:
						VTDisp.DispGetWindowPos(out x, out y, false);
						break;
					case 2:
						VTDisp.DispGetWindowPos(out x, out y, true);
						break;
					default:
						return;
					}
					Report = String.Format("3;{0};{1}t", x, y);
					SendCSIstr(Report, Report.Length);
				}
				break;
			case 14: /* get window size */
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWREPORT) != 0) {
					RequiredParams(2);
					switch (Param[2]) {
					case 0:
					case 1:
						VTDisp.DispGetWindowSize(out x, out y, true);
						break;
					case 2:
						VTDisp.DispGetWindowSize(out x, out y, false);
						break;
					default:
						return;
					}

					Report = String.Format("4;{0};{1}t", y, x);
					SendCSIstr(Report, Report.Length);
				}
				break;

			case 15: // Report display size (pixel)
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWREPORT) != 0) {
					VTDisp.DispGetRootWinSize(out x, out y, true);
					Report = String.Format("5;{0};{1}t", VTDisp.FontHeight, VTDisp.FontWidth);
					SendCSIstr(Report, Report.Length);
				}
				break;

			case 16: // Report character cell size (pixel)
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWREPORT) != 0) {
					Report = String.Format("6;{0};{1}t", VTDisp.FontHeight, VTDisp.FontWidth);
					SendCSIstr(Report, Report.Length);
				}
				break;

			case 18: /* get terminal size */
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWREPORT) != 0) {
					Report = String.Format("8;{0};{1};t",
						VTDisp.NumOfLines - Buffer.StatusLine, VTDisp.NumOfColumns);
					SendCSIstr(Report, Report.Length);
				}
				break;

			case 19: // Report display size (character)
				if ((ts.WindowFlag & WindowFlags.WF_WINDOWREPORT) != 0) {
					VTDisp.DispGetRootWinSize(out x, out y, false);
					Report = String.Format("9;{0};{1}t", y, x);
					SendCSIstr(Report, Report.Length);
				}
				break;

			case 20: // Report icon label
				switch ((TitleReportingType)(ts.WindowFlag & WindowFlags.WF_TITLEREPORT)) {
				case TitleReportingType.IdTitleReportIgnore:
					// nothing to do
					break;

				case TitleReportingType.IdTitleReportAccept:
					switch (ts.AcceptTitleChangeRequest) {
					case TitleChangeRequestTypes.IdTitleChangeRequestOff:
						Report = String.Format("L{0}", ts.Title);
						break;

					case TitleChangeRequestTypes.IdTitleChangeRequestAhead:
						Report = String.Format("L{0} {1}", cv.TitleRemote, ts.Title);
						break;

					case TitleChangeRequestTypes.IdTitleChangeRequestLast:
						Report = String.Format("L{0} {1}", ts.Title, cv.TitleRemote);
						break;

					default:
						if (String.IsNullOrEmpty(cv.TitleRemote)) {
							Report = String.Format("L{0}", ts.Title);
						}
						else {
							Report = String.Format("L{0}", cv.TitleRemote);
						}
						break;
					}
					SendOSCstr(Report, Report.Length, (char)ControlCharacters.ST);
					break;

				default: // TitleReportingType.IdTitleReportEmpty:
					SendOSCstr("L", 0, (char)ControlCharacters.ST);
					break;
				}
				break;

			case 21: // Report window title
				switch ((TitleReportingType)(ts.WindowFlag & WindowFlags.WF_TITLEREPORT)) {
				case TitleReportingType.IdTitleReportIgnore:
					// nothing to do
					break;

				case TitleReportingType.IdTitleReportAccept:
					switch (ts.AcceptTitleChangeRequest) {
					case TitleChangeRequestTypes.IdTitleChangeRequestOff:
						Report = String.Format("l{0}", ts.Title);
						break;

					case TitleChangeRequestTypes.IdTitleChangeRequestAhead:
						Report = String.Format("l{0} {1}", cv.TitleRemote, ts.Title);
						break;

					case TitleChangeRequestTypes.IdTitleChangeRequestLast:
						Report = String.Format("l{0} {1}", ts.Title, cv.TitleRemote);
						break;

					default:
						if (String.IsNullOrEmpty(cv.TitleRemote)) {
							Report = String.Format("l{0}", ts.Title);
						}
						else {
							Report = String.Format("l{0}", cv.TitleRemote);
						}
						break;
					}
					SendOSCstr(Report, Report.Length, (char)ControlCharacters.ST);
					break;

				default: // IdTitleReportEmpty:
					SendOSCstr("l", 0, (char)ControlCharacters.ST);
					break;
				}
				break;

			case 22: // Push Title
				RequiredParams(2);
				switch (Param[2]) {
				case 0:
				case 1:
				case 2:
					if (ts.AcceptTitleChangeRequest != 0) {
						t = new TStack();
						t.title = cv.TitleRemote;
						t.next = TitleStack;
						TitleStack = t;
					}
					break;
				}
				break;

			case 23: // Pop Title
				RequiredParams(2);
				switch (Param[2]) {
				case 0:
				case 1:
				case 2:
					if (ts.AcceptTitleChangeRequest != 0 && TitleStack != null) {
						t = TitleStack;
						TitleStack = t.next;
						cv.TitleRemote = t.title;
						ttwinman.ChangeTitle();
					}
					break;
				}
				break;
			}
		}

		void CSLT(byte b)
		{
			switch ((char)b) {
			case 'r':
				if (ttime.CanUseIME()) {
					ttime.SetIMEOpenStatus(IMEstat);
				}
				break;

			case 's':
				if (ttime.CanUseIME()) {
					IMEstat = ttime.GetIMEOpenStatus();
				}
				break;

			case 't':
				if (ttime.CanUseIME()) {
					ttime.SetIMEOpenStatus(Param[1] == 1);
				}
				break;
			}
		}

		void CSEQ(byte b)
		{
			string Report = null;

			switch ((char)b) {
			case 'c': /* Tertiary terminal report (Tertiary DA) */
				if (Param[1] < 1) {
					Report = String.Format("!|{0:8}", ts.TerminalUID);
					SendDCSstr(Report, Report.Length);
				}
				break;
			}
		}

		void CSGT(byte b)
		{
			switch ((char)b) {
			case 'c': /* second terminal report (Secondary DA) */
				if (Param[1] == 0) {
					SendCSIstr(">32;331;0c", 0); /* VT382(>32) + xterm rev 331 */
				}
				break;

			case 'J':   // IO-8256 terminal
				if (Param[1] == 3) {
					RequiredParams(5);
					CheckParamVal(ref Param[2], VTDisp.NumOfLines - Buffer.StatusLine);
					CheckParamVal(ref Param[3], VTDisp.NumOfColumns);
					CheckParamValMax(ref Param[4], VTDisp.NumOfLines - Buffer.StatusLine);
					CheckParamValMax(ref Param[5], VTDisp.NumOfColumns);

					if (Param[2] > Param[4] || Param[3] > Param[5]) {
						return;
					}

					Buffer.BuffEraseBox(Param[3] - 1, Param[2] - 1, Param[5] - 1, Param[4] - 1);
				}
				break;

			case 'K':   // IO-8256 terminal
				switch (Param[1]) {
				case 3:
					RequiredParams(3);
					CheckParamVal(ref Param[2], VTDisp.NumOfColumns);
					CheckParamVal(ref Param[3], VTDisp.NumOfColumns);

					if (Param[2] > Param[3]) {
						return;
					}

					Buffer.BuffEraseCharsInLine(Param[2] - 1, Param[3] - Param[2] + 1);
					break;

				case 5:
					RequiredParams(3);
					switch (Param[2]) {
					case 3:
					case 4:
					case 5:
					case 6: // Draw Line
						Buffer.BuffDrawLine(CharAttr, Param[2], Param[3]);
						break;

					case 12: // Text color
						if ((Param[3] >= 0) && (Param[3] <= 7)) {
							switch (Param[3]) {
							case 3: CharAttr.Fore = ColorCodes.IdBlue; break;
							case 4: CharAttr.Fore = ColorCodes.IdCyan; break;
							case 5: CharAttr.Fore = ColorCodes.IdYellow; break;
							case 6: CharAttr.Fore = ColorCodes.IdMagenta; break;
							default: CharAttr.Fore = (ColorCodes)Param[3]; break;
							}
							CharAttr.Attr2 |= AttributeBitMasks.Attr2Fore;
							Buffer.BuffSetCurCharAttr(CharAttr);
						}
						break;
					}
					break;
				}
				break;
			}
		}

		void CSQExchangeColor()     // DECSCNM / Visual Bell
		{
			Color ColorRef;

			Buffer.BuffUpdateScroll();

			if ((ts.ColorFlag & ColorFlags.CF_REVERSECOLOR) != 0) {
				ColorRef = ts.VTColor[0];
				ts.VTColor[0] = ts.VTReverseColor[0];
				ts.VTReverseColor[0] = ColorRef;
				ColorRef = ts.VTColor[1];
				ts.VTColor[1] = ts.VTReverseColor[1];
				ts.VTReverseColor[1] = ColorRef;
			}
			else {
				ColorRef = ts.VTColor[0];
				ts.VTColor[0] = ts.VTColor[1];
				ts.VTColor[1] = ColorRef;
			}

			ColorRef = ts.VTBoldColor[0];
			ts.VTBoldColor[0] = ts.VTBoldColor[1];
			ts.VTBoldColor[1] = ColorRef;

			ColorRef = ts.VTBlinkColor[0];
			ts.VTBlinkColor[0] = ts.VTBlinkColor[1];
			ts.VTBlinkColor[1] = ColorRef;

			ColorRef = ts.URLColor[0];
			ts.URLColor[0] = ts.URLColor[1];
			ts.URLColor[1] = ColorRef;

			ts.ColorFlag ^= ColorFlags.CF_REVERSEVIDEO;

#if ALPHABLEND_TYPE2
			BGExchangeColor();
#endif
			VTDisp.DispChangeBackground();
			ttwinman.HVTWin.Update();
		}

		void CSQChangeColumnMode(int width)     // DECCOLM
		{
			ChangeTerminalSize(width, VTDisp.NumOfLines - Buffer.StatusLine);
			LRMarginMode = false;

			// DECCOLM では画面がクリアされるのが仕様
			// ClearOnResize が off の時はここでクリアする。
			// ClearOnResize が on の時は ChangeTerminalSize() を呼ぶとクリアされるので、
			// 余計なスクロールを避ける為にここではクリアしない。
			if ((ts.TermFlag & TerminalFlags.TF_CLEARONRESIZE) == 0) {
				Buffer.MoveCursor(0, 0);
				Buffer.BuffClearScreen();
				ttwinman.HVTWin.Update();
			}
		}

		void CSQ_h_Mode() // DECSET
		{
			int i;

			for (i = 1; i <= NParam; i++) {
				switch (Param[i]) {
				case 1: keyboard.AppliCursorMode = true; break;     // DECCKM
				case 3: CSQChangeColumnMode(132); break;        // DECCOLM
				case 5: /* Reverse Video (DECSCNM) */
					if ((ts.ColorFlag & ColorFlags.CF_REVERSEVIDEO) == 0)
						CSQExchangeColor(); /* Exchange text/back color */
					break;
				case 6: // DECOM
					if (Buffer.isCursorOnStatusLine())
						Buffer.MoveCursor(0, VTDisp.CursorY);
					else {
						RelativeOrgMode = true;
						Buffer.MoveCursor(0, Buffer.CursorTop);
					}
					break;
				case 7: AutoWrapMode = true; break;     // DECAWM
				case 8: keyboard.AutoRepeatMode = true; break;      // DECARM
				case 9: /* X10 Mouse Tracking */
					if (ts.MouseEventTracking)
						MouseReportMode = MouseTrackingMode.IdMouseTrackX10;
					break;
				case 12: /* att610 cursor blinking */
					if ((ts.WindowFlag & WindowFlags.WF_CURSORCHANGE) != 0) {
						ts.NonblinkingCursor = false;
						VTDisp.ChangeCaret();
					}
					break;
				case 19: PrintEX = true; break;     // DECPEX
				case 25: VTDisp.DispEnableCaret(true); break;   // cursor on (DECTCEM)
				case 38: // DECTEK
					if (ts.AutoWinSwitch > 0)
						ChangeEmu = WindowId.IdTEK; /* Enter TEK Mode */
					break;
				case 47: // Alternate Screen Buffer
					if (((ts.TermFlag & TerminalFlags.TF_ALTSCR) != 0) && !AltScr) {
						Buffer.BuffSaveScreen();
						AltScr = true;
					}
					break;
				case 59:
					if (ts.Language == Language.IdJapanese) {
						/* kanji terminal */
						Gn[0] = CharacterSets.IdASCII;
						Gn[1] = CharacterSets.IdKatakana;
						Gn[2] = CharacterSets.IdKatakana;
						Gn[3] = CharacterSets.IdKanji;
						Glr[0] = 0;
						if ((ts.KanjiCode == KanjiCodeId.IdJIS) &&
							!ts.JIS7Katakana)
							Glr[1] = 2;  // 8-bit katakana
						else
							Glr[1] = 3;
					}
					break;
				case 66: keyboard.AppliKeyMode = true; break;       // DECNKM
				case 67: ts.BSKey = DelId.IdBS; break;      // DECBKM
				case 69: LRMarginMode = true; break;        // DECLRMM (DECVSSM)
				case 1000: // Mouse Tracking
					if (ts.MouseEventTracking)
						MouseReportMode = MouseTrackingMode.IdMouseTrackVT200;
					break;
				case 1001: // Hilite Mouse Tracking
					if (ts.MouseEventTracking)
						MouseReportMode = MouseTrackingMode.IdMouseTrackVT200Hl;
					break;
				case 1002: // Button-Event Mouse Tracking
					if (ts.MouseEventTracking)
						MouseReportMode = MouseTrackingMode.IdMouseTrackBtnEvent;
					break;
				case 1003: // Any-Event Mouse Tracking
					if (ts.MouseEventTracking)
						MouseReportMode = MouseTrackingMode.IdMouseTrackAllEvent;
					break;
				case 1004: // Focus Report
					if (ts.MouseEventTracking)
						FocusReportMode = true;
					break;
				case 1005: // Extended Mouse Tracking (UTF-8)
					if (ts.MouseEventTracking)
						MouseReportExtMode = ExtendedMouseTrackingMode.IdMouseTrackExtUTF8;
					break;
				case 1006: // Extended Mouse Tracking (SGR)
					if (ts.MouseEventTracking)
						MouseReportExtMode = ExtendedMouseTrackingMode.IdMouseTrackExtSGR;
					break;
				case 1015: // Extended Mouse Tracking (rxvt-unicode)
					if (ts.MouseEventTracking)
						MouseReportExtMode = ExtendedMouseTrackingMode.IdMouseTrackExtURXVT;
					break;
				case 1047: // Alternate Screen Buffer
					if (((ts.TermFlag & TerminalFlags.TF_ALTSCR) != 0) && !AltScr) {
						Buffer.BuffSaveScreen();
						AltScr = true;
					}
					break;
				case 1048: // Save Cursor Position (Alternate Screen Buffer)
					if ((ts.TermFlag & TerminalFlags.TF_ALTSCR) != 0) {
						SaveCursor();
					}
					break;
				case 1049: // Alternate Screen Buffer
					if (((ts.TermFlag & TerminalFlags.TF_ALTSCR) != 0) && !AltScr) {
						SaveCursor();
						Buffer.BuffSaveScreen();
						Buffer.BuffClearScreen();
						AltScr = true;
					}
					break;
				case 2004: // Bracketed Paste Mode
					BracketedPaste = true;
					break;
				case 7727: // mintty Application Escape Mode
					keyboard.AppliEscapeMode = 1;
					break;
				case 7786: // Wheel to Cursor translation
					if (ts.TranslateWheelToCursor) {
						AcceptWheelToCursor = true;
					}
					break;
				case 8200: // ClearThenHome
					ClearThenHome = true;
					break;
				case 14001: // NetTerm mouse mode
					if (ts.MouseEventTracking)
						MouseReportMode = MouseTrackingMode.IdMouseTrackNetTerm;
					break;
				case 14002: // test Application Escape Mode 2
				case 14003: // test Application Escape Mode 3
				case 14004: // test Application Escape Mode 4
					keyboard.AppliEscapeMode = Param[i] - 14000;
					break;
				}
			}
		}

		void CSQ_i_Mode()       // DECMC
		{
			switch (Param[1]) {
			case 1:
				if ((ts.TermFlag & TerminalFlags.TF_PRINTERCTRL) != 0) {
					teraprn.OpenPrnFile();
					Buffer.BuffDumpCurrentLine((byte)ControlCharacters.LF);
					if (!AutoPrintMode)
						teraprn.ClosePrnFile();
				}
				break;
			/* auto print mode off */
			case 4:
				if (AutoPrintMode) {
					teraprn.ClosePrnFile();
					AutoPrintMode = false;
				}
				break;
			/* auto print mode on */
			case 5:
				if ((ts.TermFlag & TerminalFlags.TF_PRINTERCTRL) != 0) {
					if (!AutoPrintMode) {
						teraprn.OpenPrnFile();
						AutoPrintMode = true;
					}
				}
				break;
			}
		}

		void CSQ_l_Mode()       // DECRST
		{
			int i;

			for (i = 1; i <= NParam; i++) {
				switch (Param[i]) {
				case 1: keyboard.AppliCursorMode = false; break;        // DECCKM
				case 3: CSQChangeColumnMode(80); break;     // DECCOLM
				case 5: /* Normal Video (DECSCNM) */
					if ((ts.ColorFlag & ColorFlags.CF_REVERSEVIDEO) != 0)
						CSQExchangeColor(); /* Exchange text/back color */
					break;
				case 6: // DECOM
					if (Buffer.isCursorOnStatusLine())
						Buffer.MoveCursor(0, VTDisp.CursorY);
					else {
						RelativeOrgMode = false;
						Buffer.MoveCursor(0, 0);
					}
					break;
				case 7: AutoWrapMode = false; break;        // DECAWM
				case 8: keyboard.AutoRepeatMode = false; break; // DECARM
				case 9: MouseReportMode = MouseTrackingMode.IdMouseTrackNone; break; /* X10 Mouse Tracking */
				case 12: /* att610 cursor blinking */
					if ((ts.WindowFlag & WindowFlags.WF_CURSORCHANGE) != 0) {
						ts.NonblinkingCursor = true;
						VTDisp.ChangeCaret();
					}
					break;
				case 19: PrintEX = false; break;        // DECPEX
				case 25: VTDisp.DispEnableCaret(false); break;  // cursor off (DECTCEM)
				case 47: // Alternate Screen Buffer
					if (((ts.TermFlag & TerminalFlags.TF_ALTSCR) != 0) && AltScr) {
						Buffer.BuffRestoreScreen();
						AltScr = false;
					}
					break;
				case 59:
					if (ts.Language == Language.IdJapanese) {
						/* katakana terminal */
						Gn[0] = CharacterSets.IdASCII;
						Gn[1] = CharacterSets.IdKatakana;
						Gn[2] = CharacterSets.IdKatakana;
						Gn[3] = CharacterSets.IdKanji;
						Glr[0] = 0;
						if ((ts.KanjiCode == KanjiCodeId.IdJIS) &&
							!ts.JIS7Katakana)
							Glr[1] = 2;  // 8-bit katakana
						else
							Glr[1] = 3;
					}
					break;
				case 66: keyboard.AppliKeyMode = false; break;      // DECNKM
				case 67: ts.BSKey = DelId.IdDEL; break;     // DECBKM
				case 69: // DECLRMM (DECVSSM)
					LRMarginMode = false;
					Buffer.CursorLeftM = 0;
					Buffer.CursorRightM = VTDisp.NumOfColumns - 1;
					break;
				case 1000: // Mouse Tracking
				case 1001: // Hilite Mouse Tracking
				case 1002: // Button-Event Mouse Tracking
				case 1003: // Any-Event Mouse Tracking
					MouseReportMode = MouseTrackingMode.IdMouseTrackNone;
					break;
				case 1004: // Focus Report
					FocusReportMode = false;
					break;
				case 1005: // Extended Mouse Tracking (UTF-8)
				case 1006: // Extended Mouse Tracking (SGR)
				case 1015: // Extended Mouse Tracking (rxvt-unicode)
					MouseReportExtMode = ExtendedMouseTrackingMode.IdMouseTrackExtNone;
					break;
				case 1047: // Alternate Screen Buffer
					if (((ts.TermFlag & TerminalFlags.TF_ALTSCR) != 0) && AltScr) {
						Buffer.BuffClearScreen();
						Buffer.BuffRestoreScreen();
						AltScr = false;
					}
					break;
				case 1048: // Save Cursor Position (Alternate Screen Buffer)
					if ((ts.TermFlag & TerminalFlags.TF_ALTSCR) != 0) {
						RestoreCursor();
					}
					break;
				case 1049: // Alternate Screen Buffer
					if (((ts.TermFlag & TerminalFlags.TF_ALTSCR) != 0) && AltScr) {
						Buffer.BuffClearScreen();
						Buffer.BuffRestoreScreen();
						AltScr = false;
						RestoreCursor();
					}
					break;
				case 2004: // Bracketed Paste Mode
					BracketedPaste = false;
					break;
				case 7727: // mintty Application Escape Mode
					keyboard.AppliEscapeMode = 0;
					break;
				case 7786: // Wheel to Cursor translation
					AcceptWheelToCursor = false;
					break;
				case 8200: // ClearThenHome
					ClearThenHome = false;
					break;
				case 14001: // NetTerm mouse mode
					MouseReportMode = MouseTrackingMode.IdMouseTrackNone;
					break;
				case 14002: // test Application Escape Mode 2
				case 14003: // test Application Escape Mode 3
				case 14004: // test Application Escape Mode 4
					keyboard.AppliEscapeMode = 0;
					break;
				}
			}
		}

		void CSQ_n_Mode()       // DECDSR
		{
			switch (Param[1]) {
			case 53:
			case 55:
				/* Locator Device Status Report -> Ready */
				SendCSIstr("?50n", 0);
				break;
			}
		}

		void CSQuest(byte b)
		{
			switch ((char)b) {
			case 'J': CSQSelScreenErase(); break;   // DECSED
			case 'K': CSQSelLineErase(); break; // DECSEL
			case 'h': CSQ_h_Mode(); break;      // DECSET
			case 'i': CSQ_i_Mode(); break;      // DECMC
			case 'l': CSQ_l_Mode(); break;      // DECRST
			case 'n': CSQ_n_Mode(); break;      // DECDSR
			}
		}

		void SoftReset()
		// called by software-reset escape sequence handler
		{
			Buffer.UpdateStr();
			keyboard.AutoRepeatMode = true;
			VTDisp.DispEnableCaret(true); // cursor on
			InsertMode = false;
			RelativeOrgMode = false;
			keyboard.AppliKeyMode = false;
			keyboard.AppliCursorMode = false;
			keyboard.AppliEscapeMode = 0;
			AcceptWheelToCursor = ts.TranslateWheelToCursor;
			if (Buffer.isCursorOnStatusLine())
				MoveToMainScreen();
			Buffer.CursorTop = 0;
			Buffer.CursorBottom = VTDisp.NumOfLines - 1 - Buffer.StatusLine;
			Buffer.CursorLeftM = 0;
			Buffer.CursorRightM = VTDisp.NumOfColumns - 1;
			ResetCharSet();

			/* Attribute */
			CharAttr = VTDisp.DefCharAttr;
			Special = false;
			Buffer.BuffSetCurCharAttr(CharAttr);

			// status buffers
			ResetCurSBuffer();

			// Saved IME status
			IMEstat = false;
		}

		void CSExc(byte b)
		{
			switch ((char)b) {
			case 'p':
				/* Software reset */
				SoftReset();
				break;
			}
		}

		void CSDouble(byte b)
		{
			switch ((char)b) {
			case 'p': // DECSCL
					  /* Select terminal mode (software reset) */
				RequiredParams(2);

				SoftReset();
				ChangeTerminalID();
				if (Param[1] >= 61 && Param[1] <= 65) {
					if (VTlevel > Param[1] - 60) {
						VTlevel = Param[1] - 60;
					}
				}
				else {
					VTlevel = 1;
				}

				if (VTlevel < 2 || Param[2] == 1)
					keyboard.Send8BitMode = false;
				else
					keyboard.Send8BitMode = true;
				break;

			case 'q': // DECSCA
				switch (Param[1]) {
				case 0:
				case 2:
					CharAttr.Attr2 &= ~AttributeBitMasks.Attr2Protect;
					Buffer.BuffSetCurCharAttr(CharAttr);
					break;
				case 1:
					CharAttr.Attr2 |= AttributeBitMasks.Attr2Protect;
					Buffer.BuffSetCurCharAttr(CharAttr);
					break;
				default:
					/* nothing to do */
					break;
				}
				break;
			}
		}

		void CSDolRequestMode() // DECRQM
		{
			string buff;
			string pp = "";
			int len, resp = 0;

			switch (Prv) {
			case 0: /* ANSI Mode */
				resp = 4;
				pp = "";
				switch (Param[1]) {
				case 2: // KAM
					if (ttwinman.KeybEnabled)
						resp = 2;
					else
						resp = 1;
					break;
				case 4: // IRM
					if (InsertMode)
						resp = 1;
					else
						resp = 2;
					break;
				case 12:    // SRM
					if (ts.LocalEcho)
						resp = 2;
					else
						resp = 1;
					break;
				case 20:    // LNM
					if (LFMode)
						resp = 1;
					else
						resp = 2;
					break;
				case 33:    // WYSTCURM
					if (ts.NonblinkingCursor)
						resp = 1;
					else
						resp = 2;
					if ((ts.WindowFlag & WindowFlags.WF_CURSORCHANGE) == 0)
						resp += 2;
					break;
				case 34:    // WYULCURM
					if (ts.CursorShape == CursorShapes.IdHCur)
						resp = 1;
					else
						resp = 2;
					if ((ts.WindowFlag & WindowFlags.WF_CURSORCHANGE) == 0)
						resp += 2;
					break;
				}
				break;

			case (byte)'?': /* DEC Mode */
				pp = "?";
				switch (Param[1]) {
				case 1: // DECCKM
					if (keyboard.AppliCursorMode)
						resp = 1;
					else
						resp = 2;
					break;
				case 3: // DECCOLM
					if (VTDisp.NumOfColumns == 132)
						resp = 1;
					else
						resp = 2;
					break;
				case 5: // DECSCNM
					if ((ts.ColorFlag & ColorFlags.CF_REVERSEVIDEO) != 0)
						resp = 1;
					else
						resp = 2;
					break;
				case 6: // DECOM
					if (RelativeOrgMode)
						resp = 1;
					else
						resp = 2;
					break;
				case 7: // DECAWM
					if (AutoWrapMode)
						resp = 1;
					else
						resp = 2;
					break;
				case 8: // DECARM
					if (keyboard.AutoRepeatMode)
						resp = 1;
					else
						resp = 2;
					break;
				case 9: // XT_MSE_X10 -- X10 Mouse Tracking
					if (!ts.MouseEventTracking)
						resp = 4;
					else if (MouseReportMode == MouseTrackingMode.IdMouseTrackX10)
						resp = 1;
					else
						resp = 2;
					break;
				case 12:    // XT_CBLINK -- att610 cursor blinking
					if (ts.NonblinkingCursor)
						resp = 2;
					else
						resp = 1;
					if ((ts.WindowFlag & WindowFlags.WF_CURSORCHANGE) == 0)
						resp += 2;
					break;
				case 19:    // DECPEX
					if (PrintEX)
						resp = 1;
					else
						resp = 2;
					break;
				case 25:    // DECTCEM
					if (VTDisp.IsCaretEnabled())
						resp = 1;
					else
						resp = 2;
					break;
				case 38:    // DECTEK
					resp = 4;
					break;
				case 47:    // XT_ALTSCRN -- Alternate Screen / (DECGRPM)
					if ((ts.TermFlag & TerminalFlags.TF_ALTSCR) == 0)
						resp = 4;
					else if (AltScr)
						resp = 1;
					else
						resp = 2;
					break;
				case 59:    // DECKKDM
					if (ts.Language != Language.IdJapanese)
						resp = 0;
					else if ((ts.KanjiCode == KanjiCodeId.IdJIS) && (!ts.JIS7Katakana))
						resp = 4;
					else
						resp = 3;
					break;
				case 66:    // DECNKM
					if (keyboard.AppliKeyMode)
						resp = 1;
					else
						resp = 2;
					break;
				case 67:    // DECBKM
					if (ts.BSKey == DelId.IdBS)
						resp = 1;
					else
						resp = 2;
					break;
				case 69:    // DECRQM
					if (LRMarginMode)
						resp = 1;
					else
						resp = 2;
					break;
				case 1000:  // XT_MSE_X11
					if (!ts.MouseEventTracking)
						resp = 4;
					else if (MouseReportMode == MouseTrackingMode.IdMouseTrackVT200)
						resp = 1;
					else
						resp = 2;
					break;
				case 1001:  // XT_MSE_HL
#if false
					if (!ts.MouseEventTracking)
						resp = 4;
					else if (MouseReportMode == MouseTrackingMode.IdMouseTrackVT200Hl)
						resp = 1;
					else
						resp = 2;
#else
					resp = 4;
#endif
					break;
				case 1002:  // XT_MSE_BTN
					if (!ts.MouseEventTracking)
						resp = 4;
					else if (MouseReportMode == MouseTrackingMode.IdMouseTrackBtnEvent)
						resp = 1;
					else
						resp = 2;
					break;
				case 1003:  // XT_MSE_ANY
					if (!ts.MouseEventTracking)
						resp = 4;
					else if (MouseReportMode == MouseTrackingMode.IdMouseTrackAllEvent)
						resp = 1;
					else
						resp = 2;
					break;
				case 1004:  // XT_MSE_WIN
					if (!ts.MouseEventTracking)
						resp = 4;
					else if (FocusReportMode)
						resp = 1;
					else
						resp = 2;
					break;
				case 1005:  // XT_MSE_UTF
					if (!ts.MouseEventTracking)
						resp = 4;
					else if (MouseReportExtMode == ExtendedMouseTrackingMode.IdMouseTrackExtUTF8)
						resp = 1;
					else
						resp = 2;
					break;
				case 1006:  // XT_MSE_SGR
					if (!ts.MouseEventTracking)
						resp = 4;
					else if (MouseReportExtMode == ExtendedMouseTrackingMode.IdMouseTrackExtSGR)
						resp = 1;
					else
						resp = 2;
					break;
				case 1015:  // urxvt-style extended mouse tracking
					if (!ts.MouseEventTracking)
						resp = 4;
					else if (MouseReportExtMode == ExtendedMouseTrackingMode.IdMouseTrackExtURXVT)
						resp = 1;
					else
						resp = 2;
					break;
				case 1047:  // XT_ALTS_47
					if ((ts.TermFlag & TerminalFlags.TF_ALTSCR) == 0)
						resp = 4;
					else if (AltScr)
						resp = 1;
					else
						resp = 2;
					break;
				case 1048:
					if ((ts.TermFlag & TerminalFlags.TF_ALTSCR) == 0)
						resp = 4;
					else
						resp = 1;
					break;
				case 1049:  // XT_EXTSCRN
					if ((ts.TermFlag & TerminalFlags.TF_ALTSCR) == 0)
						resp = 4;
					else if (AltScr)
						resp = 1;
					else
						resp = 2;
					break;
				case 2004:  // RL_BRACKET
					if (BracketedPaste)
						resp = 1;
					else
						resp = 2;
					break;
				case 7727:  // MinTTY Application Escape Mode
					if (keyboard.AppliEscapeMode == 1)
						resp = 1;
					else
						resp = 2;
					break;
				case 7786:  // MinTTY Mousewheel reporting
					if (!ts.TranslateWheelToCursor)
						resp = 4;
					else if (AcceptWheelToCursor)
						resp = 1;
					else
						resp = 2;
					break;
				case 8200:  // ClearThenHome
					if (ClearThenHome)
						resp = 1;
					else
						resp = 2;
					break;
				case 14001: // NetTerm Mouse Reporting (TT)
					if (!ts.MouseEventTracking)
						resp = 4;
					else if (MouseReportMode == MouseTrackingMode.IdMouseTrackNetTerm)
						resp = 1;
					else
						resp = 2;
					break;
				case 14002: // test Application Escape Mode 2
				case 14003: // test Application Escape Mode 3
				case 14004: // test Application Escape Mode 4
					if (keyboard.AppliEscapeMode == Param[1] - 14000)
						resp = 1;
					else
						resp = 2;
					break;
				}
				break;
			}

			buff = String.Format("{0}{1};{2}$y", pp, Param[1], resp);
			SendCSIstr(buff, buff.Length);
		}

		void CSDol(byte b)
		{
			TCharAttr attr, mask;
			attr = VTDisp.DefCharAttr;
			mask = VTDisp.DefCharAttr;

			switch (b) {
			case (byte)'p': // DECRQM
				CSDolRequestMode();
				break;

			case (byte)'r': // DECCARA
			case (byte)'t': // DECRARA
				RequiredParams(4);
				CheckParamVal(ref Param[1], VTDisp.NumOfLines - Buffer.StatusLine);
				CheckParamVal(ref Param[2], VTDisp.NumOfColumns);
				CheckParamValMax(ref Param[3], VTDisp.NumOfLines - Buffer.StatusLine);
				CheckParamValMax(ref Param[4], VTDisp.NumOfColumns);

				if (Param[1] > Param[3] || Param[2] > Param[4]) {
					return;
				}

				if (RelativeOrgMode) {
					Param[1] += Buffer.CursorTop;
					if (Param[1] > Buffer.CursorBottom) {
						Param[1] = Buffer.CursorBottom + 1;
					}
					Param[3] += Buffer.CursorTop;
					if (Param[3] > Buffer.CursorBottom) {
						Param[3] = Buffer.CursorBottom + 1;
					}

					// TODO: 左右マージンのチェックを行う。
				}

				ParseSGRParams(ref attr, ref mask, 5);
				if (b == 'r') { // DECCARA
					attr.Attr &= AttributeBitMasks.AttrSgrMask;
					mask.Attr &= AttributeBitMasks.AttrSgrMask;
					attr.Attr2 &= AttributeBitMasks.Attr2ColorMask;
					mask.Attr2 &= AttributeBitMasks.Attr2ColorMask;
					if (RectangleMode) {
						Buffer.BuffChangeAttrBox(Param[2] - 1, Param[1] - 1, Param[4] - 1, Param[3] - 1, attr, mask);
					}
					else {
						Buffer.BuffChangeAttrStream(Param[2] - 1, Param[1] - 1, Param[4] - 1, Param[3] - 1, attr, mask);
					}
				}
				else { // DECRARA
					attr.Attr &= AttributeBitMasks.AttrSgrMask;
					if (RectangleMode) {
						Buffer.BuffChangeAttrBox(Param[2] - 1, Param[1] - 1, Param[4] - 1, Param[3] - 1, attr, null);
					}
					else {
						Buffer.BuffChangeAttrStream(Param[2] - 1, Param[1] - 1, Param[4] - 1, Param[3] - 1, attr, null);
					}
				}
				break;

			case (byte)'v': // DECCRA
				RequiredParams(8);
				CheckParamVal(ref Param[1], VTDisp.NumOfLines - Buffer.StatusLine);       // Src Y-start
				CheckParamVal(ref Param[2], VTDisp.NumOfColumns);          // Src X-start
				CheckParamValMax(ref Param[3], VTDisp.NumOfLines - Buffer.StatusLine);    // Src Y-end
				CheckParamValMax(ref Param[4], VTDisp.NumOfColumns);       // Src X-end
				CheckParamVal(ref Param[5], 1);             // Src Page
				CheckParamVal(ref Param[6], VTDisp.NumOfLines - Buffer.StatusLine);       // Dest Y
				CheckParamVal(ref Param[7], VTDisp.NumOfColumns);          // Dest X
				CheckParamVal(ref Param[8], 1);             // Dest Page

				if (Param[1] > Param[3] || Param[2] > Param[4]) {
					return;
				}

				if (RelativeOrgMode) {
					Param[1] += Buffer.CursorTop;
					if (Param[1] > Buffer.CursorBottom) {
						Param[1] = Buffer.CursorBottom + 1;
					}
					Param[3] += Buffer.CursorTop;
					if (Param[3] > Buffer.CursorBottom) {
						Param[3] = Buffer.CursorBottom + 1;
					}
					Param[6] += Buffer.CursorTop;
					if (Param[6] > Buffer.CursorBottom) {
						Param[6] = Buffer.CursorBottom + 1;
					}
					if (Param[6] + Param[3] - Param[1] > Buffer.CursorBottom) {
						Param[3] = Param[1] + Buffer.CursorBottom - Param[6] + 1;
					}

					// TODO: 左右マージンのチェックを行う。
				}

				// TODO: 1 origin になっている。0 origin に直す。
				Buffer.BuffCopyBox(Param[2], Param[1], Param[4], Param[3], Param[5], Param[7], Param[6], Param[8]);
				break;

			case (byte)'x': // DECFRA
				RequiredParams(5);
				if (Param[1] < 32 || (Param[1] > 127 && Param[1] < 160) || Param[1] > 255) {
					return;
				}
				CheckParamVal(ref Param[2], VTDisp.NumOfLines - Buffer.StatusLine);
				CheckParamVal(ref Param[3], VTDisp.NumOfColumns);
				CheckParamValMax(ref Param[4], VTDisp.NumOfLines - Buffer.StatusLine);
				CheckParamValMax(ref Param[5], VTDisp.NumOfColumns);

				if (Param[2] > Param[4] || Param[3] > Param[5]) {
					return;
				}

				if (RelativeOrgMode) {
					Param[2] += Buffer.CursorTop;
					if (Param[2] > Buffer.CursorBottom) {
						Param[2] = Buffer.CursorBottom + 1;
					}
					Param[4] += Buffer.CursorTop;
					if (Param[4] > Buffer.CursorBottom) {
						Param[4] = Buffer.CursorBottom + 1;
					}

					// TODO: 左右マージンのチェックを行う。
				}

				Buffer.BuffFillBox((byte)Param[1], Param[3] - 1, Param[2] - 1, Param[5] - 1, Param[4] - 1);
				break;

			case (byte)'z': // DECERA
			case (byte)'{': // DECSERA
				RequiredParams(4);
				CheckParamVal(ref Param[1], VTDisp.NumOfLines - Buffer.StatusLine);
				CheckParamVal(ref Param[2], VTDisp.NumOfColumns);
				CheckParamValMax(ref Param[3], VTDisp.NumOfLines - Buffer.StatusLine);
				CheckParamValMax(ref Param[4], VTDisp.NumOfColumns);

				if (Param[1] > Param[3] || Param[2] > Param[4]) {
					return;
				}

				if (RelativeOrgMode) {
					Param[1] += Buffer.CursorTop;
					if (Param[1] > Buffer.CursorBottom) {
						Param[1] = Buffer.CursorBottom + 1;
					}
					Param[3] += Buffer.CursorTop;
					if (Param[3] > Buffer.CursorBottom) {
						Param[3] = Buffer.CursorBottom + 1;
					}

					// TODO: 左右マージンのチェックを行う。
				}

				if (b == 'z') {
					Buffer.BuffEraseBox(Param[2] - 1, Param[1] - 1, Param[4] - 1, Param[3] - 1);
				}
				else {
					Buffer.BuffSelectiveEraseBox(Param[2] - 1, Param[1] - 1, Param[4] - 1, Param[3] - 1);
				}
				break;

			case (byte)'}': // DECSASD
				if ((ts.TermFlag & TerminalFlags.TF_ENABLESLINE) == 0 || Buffer.StatusLine == 0) {
					return;
				}

				switch (Param[1]) {
				case 0:
					if (Buffer.isCursorOnStatusLine()) {
						MoveToMainScreen();
					}
					break;

				case 1:
					if (!Buffer.isCursorOnStatusLine()) {
						MoveToStatusLine();
					}
					break;
				}
				break;

			case (byte)'~': // DECSSDT
				if ((ts.TermFlag & TerminalFlags.TF_ENABLESLINE) == 0) {
					return;
				}

				switch (Param[1]) {
				case 0:
				case 1:
					HideStatusLine();
					break;
				case 2:
					if (Buffer.StatusLine == 0) {
						Buffer.ShowStatusLine(1); // show
					}
					break;
				}
				break;
			}
		}

		void CSQDol(byte b)
		{
			switch (b) {
			case (byte)'p':
				CSDolRequestMode();
				break;
			}
		}

		void CSQuote(byte b)
		{
			int i, x, y;
			bool right;

			switch ((char)b) {
			case 'w': // Enable Filter Rectangle (DECEFR)
				if (MouseReportMode == MouseTrackingMode.IdMouseTrackDECELR) {
					RequiredParams(4);
					if ((DecLocatorFlag & DecLocator.DecLocatorPixel) != 0) {
						x = LastX + 1;
						y = LastY + 1;
					}
					else {
						VTDisp.DispConvWinToScreen(LastX, LastY, out x, out y, out right);
						x++;
						y++;
					}
					FilterTop = (Param[1] == 0) ? y : Param[1];
					FilterLeft = (Param[2] == 0) ? x : Param[2];
					FilterBottom = (Param[3] == 0) ? y : Param[3];
					FilterRight = (Param[4] == 0) ? x : Param[4];
					if (FilterTop > FilterBottom) {
						i = FilterTop; FilterTop = FilterBottom; FilterBottom = i;
					}
					if (FilterLeft > FilterRight) {
						i = FilterLeft; FilterLeft = FilterRight; FilterRight = i;
					}
					DecLocatorFlag |= DecLocator.DecLocatorFiltered;
					DecLocatorReport(MouseEvent.IdMouseEventMove, 0);
				}
				break;

			case 'z': // Enable DEC Locator reporting (DECELR)
				switch (Param[1]) {
				case 0:
					if (MouseReportMode == MouseTrackingMode.IdMouseTrackDECELR) {
						MouseReportMode = MouseTrackingMode.IdMouseTrackNone;
					}
					break;
				case 1:
					if (ts.MouseEventTracking) {
						MouseReportMode = MouseTrackingMode.IdMouseTrackDECELR;
						DecLocatorFlag &= ~DecLocator.DecLocatorOneShot;
					}
					break;
				case 2:
					if (ts.MouseEventTracking) {
						MouseReportMode = MouseTrackingMode.IdMouseTrackDECELR;
						DecLocatorFlag |= DecLocator.DecLocatorOneShot;
					}
					break;
				}
				if (NParam > 1 && Param[2] == 1) {
					DecLocatorFlag |= DecLocator.DecLocatorPixel;
				}
				else {
					DecLocatorFlag &= ~DecLocator.DecLocatorPixel;
				}
				break;

			case '{': // Select Locator Events (DECSLE)
				for (i = 1; i <= NParam; i++) {
					switch (Param[i]) {
					case 0:
						DecLocatorFlag &= ~(DecLocator.DecLocatorButtonUp | DecLocator.DecLocatorButtonDown | DecLocator.DecLocatorFiltered);
						break;
					case 1:
						DecLocatorFlag |= DecLocator.DecLocatorButtonDown;
						break;
					case 2:
						DecLocatorFlag &= ~DecLocator.DecLocatorButtonDown;
						break;
					case 3:
						DecLocatorFlag |= DecLocator.DecLocatorButtonUp;
						break;
					case 4:
						DecLocatorFlag &= ~DecLocator.DecLocatorButtonUp;
						break;
					}
				}
				break;

			case '|': // Request Locator Position (DECRQLP)
				DecLocatorReport(MouseEvent.IdMouseEventCurStat, 0);
				break;
			}
		}

		void CSSpace(byte b)
		{
			switch ((char)b) {
			case 'q': // DECSCUSR
				if ((ts.WindowFlag & WindowFlags.WF_CURSORCHANGE) != 0) {
					switch (Param[1]) {
					case 0:
					case 1:
						ts.CursorShape = CursorShapes.IdBlkCur;
						ts.NonblinkingCursor = false;
						break;
					case 2:
						ts.CursorShape = CursorShapes.IdBlkCur;
						ts.NonblinkingCursor = true;
						break;
					case 3:
						ts.CursorShape = CursorShapes.IdHCur;
						ts.NonblinkingCursor = false;
						break;
					case 4:
						ts.CursorShape = CursorShapes.IdHCur;
						ts.NonblinkingCursor = true;
						break;
					case 5:
						ts.CursorShape = CursorShapes.IdVCur;
						ts.NonblinkingCursor = false;
						break;
					case 6:
						ts.CursorShape = CursorShapes.IdVCur;
						ts.NonblinkingCursor = true;
						break;
					default:
						return;
					}
					VTDisp.ChangeCaret();
				}
				break;
			}
		}

		void CSAster(byte b)
		{
			switch (b) {
			case (byte)'x': // DECSACE
				switch (Param[1]) {
				case 0:
				case 1:
					RectangleMode = false;
					break;
				case 2:
					RectangleMode = true;
					break;
				}
				break;
			}
		}

		void PrnParseCS(byte b) // printer mode
		{
			ParseMode = ParsingMode.ModeFirst;
			switch (ICount) {
			/* no intermediate char */
			case 0:
				switch (Prv) {
				/* no private parameter */
				case 0:
					switch ((char)b) {
					case 'i':
						if (Param[1] == 4) {
							PrinterMode = false;
							// clear prn buff
							teraprn.WriteToPrnFile(0, false);
							if (!AutoPrintMode)
								teraprn.ClosePrnFile();
							return;
						}
						break;
					} /* of case Prv=0 */
					break;
				}
				break;
			/* one intermediate char */
			case 1: break;
			} /* of case Icount */

			teraprn.WriteToPrnFile(b, true);
		}

		void ParseCS(byte b) /* b is the final char */
		{
			if (PrinterMode) { // printer mode
				PrnParseCS(b);
				return;
			}

			switch (ICount) {
			case 0: /* no intermediate char */
				switch ((char)Prv) {
				case '\0': /* no private parameter */
					switch ((char)b) {
					// ISO/IEC 6429 / ECMA-48 Sequence
					case '@': CSInsertCharacter(); break;       // ICH
					case 'A': CSCursorUp(true); break;          // CUU
					case 'B': CSCursorDown(true); break;        // CUD
					case 'C': CSCursorRight(true); break;       // CUF
					case 'D': CSCursorLeft(true); break;        // CUB
					case 'E': CSCursorDown1(); break;           // CNL
					case 'F': CSCursorUp1(); break;             // CPL
					case 'G': CSMoveToColumnN(); break;         // CHA
					case 'H': CSMoveToXY(); break;              // CUP
					case 'I': CSForwardTab(); break;            // CHT
					case 'J': CSScreenErase(); break;           // ED
					case 'K': CSLineErase(); break;             // EL
					case 'L': CSInsertLine(); break;            // IL
					case 'M': CSDeleteNLines(); break;          // DL
																//case 'N': break;				// EF   -- Not support
																//case 'O': break;				// EA   -- Not support
					case 'P': CSDeleteCharacter(); break;       // DCH
																//case 'Q': break;				// SEE  -- Not support
																//case 'R': break;				// CPR  -- Report only, ignore.
					case 'S': CSScrollUp(); break;              // SU
					case 'T': CSScrollDown(); break;            // SD
																//case 'U': break;				// NP   -- Not support
																//case 'V': break;				// PP   -- Not support
																//case 'W': break;				// CTC  -- Not support
					case 'X': CSEraseCharacter(); break;        // ECH
																//case 'Y': break;				// CVT  -- Not support
					case 'Z': CSBackwardTab(); break;           // CBT
																//case '[': break;                            // SRS  -- Not support
																//case '\\': break;                           // PTX  -- Not support
																//case ']': break;                            // SDS  -- Not support
																//case '^': break;                            // SIMD -- Not support
					case '`': CSMoveToColumnN(); break;         // HPA
					case 'a': CSCursorRight(false); break;      // HPR
																//case 'b': break;                            // REP  -- Not support
					case 'c': AnswerTerminalType(); break;      // DA
					case 'd': CSMoveToLineN(); break;           // VPA
					case 'e': CSCursorDown(false); break;       // VPR
					case 'f': CSMoveToXY(); break;              // HVP
					case 'g': CSDeleteTabStop(); break;         // TBC
					case 'h': CS_h_Mode(); break;               // SM
					case 'i': CS_i_Mode(); break;               // MC
					case 'j': CSCursorLeft(false); break;       // HPB
					case 'k': CSCursorUp(false); break;         // VPB
					case 'l': CS_l_Mode(); break;               // RM
					case 'm': CSSetAttr(); break;               // SGR
					case 'n': CS_n_Mode(); break;               // DSR
																//	    case 'o': break;                            // DAQ  -- Not support

					// Private Sequence
					case 'r': CSSetScrollRegion(); break;       // DECSTBM
					case 's':
						if (LRMarginMode)
							CSSetLRScrollRegion();              // DECSLRM
						else
							SaveCursor();              // SCP (Save cursor (ANSI.SYS/SCO?))
						break;
					case 't': CSSunSequence(); break;           // DECSLPP / Window manipulation(dtterm?)
					case 'u': RestoreCursor(); break;           // RCP (Restore cursor (ANSI.SYS/SCO))
					}
					break; /* end of case Prv=0 */
				case '<': CSLT(b); break;    /* private parameter = '<' */
				case '=': CSEQ(b); break;    /* private parameter = '=' */
				case '>': CSGT(b); break;    /* private parameter = '>' */
				case '?': CSQuest(b); break; /* private parameter = '?' */
				} /* end of switch (Prv) */
				break; /* end of no intermediate char */
			case 1: /* one intermediate char */
				switch (Prv) {
				case 0:
					switch ((char)IntChar[1]) {
					case ' ': CSSpace(b); break;    /* intermediate char = ' ' */
					case '!': CSExc(b); break;      /* intermediate char = '!' */
					case '"': CSDouble(b); break;   /* intermediate char = '"' */
					case '$': CSDol(b); break;      /* intermediate char = '$' */
					case '*': CSAster(b); break;  /* intermediate char = '*' */
					case '\'': CSQuote(b); break;   /* intermediate char = '\'' */
					}
					break; /* end of case Prv=0 */
				case (byte)'?':
					switch (IntChar[1]) {
					case (byte)'$': CSQDol(b); break;    /* intermediate char = '$' */
					}
					break; /* end of case Prv=0 */
				} /* end of switch (Prv) */
				break; /* end of one intermediate char */
			} /* end of switch (Icount) */

			ParseMode = ParsingMode.ModeFirst;
		}

		void ParamIncr(ref int p, int b)
		{
			uint ptmp;
			if ((uint)p != UInt32.MaxValue) {
				ptmp = (uint)(p);
				if (ptmp > UInt32.MaxValue / 10 || ptmp * 10 > UInt32.MaxValue - (b - 0x30)) {
					(p) = unchecked((int)UInt32.MaxValue);
				}
				else {
					(p) = (int)(ptmp * 10 + b - 0x30);
				}
			}
		}

		void ControlSequence(byte b)
		{
			if ((b <= (byte)ControlCharacters.US) || (b >= 0x80) && (b <= 0x9F))
				ParseControl(b); /* ctrl char */
			else if ((b >= 0x40) && (b <= 0x7E))
				ParseCS(b); /* terminate char */
			else {
				if (PrinterMode)
					teraprn.WriteToPrnFile(b, false);

				if ((b >= 0x20) && (b <= 0x2F)) { /* intermediate char */
					if (ICount < IntCharMax) ICount++;
					IntChar[ICount] = b;
				}
				else if ((b >= 0x30) && (b <= 0x39)) { /* parameter value */
					if (NSParam[NParam] > 0) {
						ParamIncr(ref SubParam[NParam][NSParam[NParam]], b);
					}
					else {
						ParamIncr(ref Param[NParam], b);
					}
				}
				else if (b == 0x3A) { /* ':' Subparameter delimiter */
					if (NSParam[NParam] < NSParamMax) {
						NSParam[NParam]++;
						SubParam[NParam][NSParam[NParam]] = 0;
					}
				}
				else if (b == 0x3B) { /* ';' Parameter delimiter */
					if (NParam < NParamMax) {
						NParam++;
						Param[NParam] = 0;
						NSParam[NParam] = 0;
					}
				}
				else if ((b >= 0x3C) && (b <= 0x3F)) { /* private char */
					if (FirstPrm) Prv = b;
				}
				else if (b > 0xA0) {
					ParseMode = ParsingMode.ModeFirst;
					ParseFirst(b);
				}
			}
			FirstPrm = false;
		}

		int CheckUTF8Seq(byte b, int utf8_stat)
		{
			if (ts.Language == Language.IdUtf8 || (ts.Language == Language.IdJapanese && (ts.KanjiCode == KanjiCodeId.IdUTF8 || ts.KanjiCode == KanjiCodeId.IdUTF8m))) {
				if (utf8_stat > 0) {
					if (b >= 0x80 && b < 0xc0) {
						utf8_stat -= 1;
					}
					else { // Invalid UTF-8 sequence
						utf8_stat = 0;
					}
				}
				else if (b < 0xc0) {
					; // nothing to do
				}
				else if (b < 0xe0) { // 2byte sequence
					utf8_stat = 1;
				}
				else if (b < 0xf0) { // 3byte sequence
					utf8_stat = 2;
				}
				else if (b < 0xf8) { // 4byte sequence
					utf8_stat = 3;
				}
			}
			return utf8_stat;
		}

		static int utf8_stat = 0;

		void IgnoreString(byte b)
		{
			if ((ESCFlag && (b == '\\')) ||
				(b <= (byte)ControlCharacters.US && b != (byte)ControlCharacters.ESC && b != (byte)ControlCharacters.HT) ||
				(b == (byte)ControlCharacters.ST && ts.KanjiCode != KanjiCodeId.IdSJIS && utf8_stat == 0)) {
				ParseMode = ParsingMode.ModeFirst;
			}

			if (b == (byte)ControlCharacters.ESC) {
				ESCFlag = true;
			}
			else {
				ESCFlag = false;
			}

			utf8_stat = CheckUTF8Seq(b, utf8_stat);
		}

		void RequestStatusString(byte[] StrBuff, int StrLen)    // DECRQSS
		{
			string RepStr = null;
			int tmp;

			switch ((char)StrBuff[0]) {
			case ' ':
				switch (StrBuff[1]) {
				case (byte)'q': // DECSCUSR
					switch (ts.CursorShape) {
					case CursorShapes.IdBlkCur:
						tmp = 1;
						break;
					case CursorShapes.IdHCur:
						tmp = 3;
						break;
					case CursorShapes.IdVCur:
						tmp = 5;
						break;
					default:
						tmp = 1;
						break;
					}
					if (ts.NonblinkingCursor) {
						tmp++;
					}
					RepStr = String.Format("1$r{0} q", tmp);
					break;
				}
				break;
			case '"':
				switch ((char)StrBuff[1]) {
				case 'p': // DECSCL
					if (VTlevel > 1 && keyboard.Send8BitMode) {
						RepStr = String.Format("1$r6{0};0\"p", VTlevel);
					}
					else {
						RepStr = String.Format("1$r6{0};1\"p", VTlevel);
					}
					break;

				case 'q': // DECSCA
					if ((CharAttr.Attr2 & AttributeBitMasks.Attr2Protect) != 0) {
						RepStr = "1$r1\"q";
					}
					else {
						RepStr = "1$r0\"q";
					}
					break;
				}
				break;
			case '*':
				switch (StrBuff[1]) {
				case (byte)'x': // DECSACE
					RepStr = String.Format("1$r{0}*x", RectangleMode ? 2 : 0);
					break;
				}
				break;
			case 'm':   // SGR
				if (StrBuff[1] == 0) {
					RepStr = "1$r0";
					if ((CharAttr.Attr & AttributeBitMasks.AttrBold) != 0) {
						RepStr += ";1";
					}
					if ((CharAttr.Attr & AttributeBitMasks.AttrUnder) != 0) {
						RepStr += ";4";
					}
					if ((CharAttr.Attr & AttributeBitMasks.AttrBlink) != 0) {
						RepStr += ";5";
					}
					if ((CharAttr.Attr & AttributeBitMasks.AttrReverse) != 0) {
						RepStr += ";7";
					}
					if (((CharAttr.Attr2 & AttributeBitMasks.Attr2Fore) != 0) && ((ts.ColorFlag & ColorFlags.CF_ANSICOLOR) != 0)) {
						int color = (int)CharAttr.Fore;
						if (color <= 7 && ((CharAttr.Attr & AttributeBitMasks.AttrBold) != 0) && ((ts.ColorFlag & ColorFlags.CF_PCBOLD16) != 0)) {
							color += 8;
						}

						if (color <= 7) {
							RepStr += String.Format(";3{0}", color);
						}
						else if (color <= 15) {
							if ((ts.ColorFlag & ColorFlags.CF_AIXTERM16) != 0) {
								RepStr += String.Format(";9{0}", color - 8);
							}
							else if ((ts.ColorFlag & ColorFlags.CF_XTERM256) != 0) {
								RepStr += String.Format(";38;5;{0}", color);
							}
							else if ((ts.ColorFlag & ColorFlags.CF_PCBOLD16) != 0) {
								RepStr += String.Format(";3{0}", color - 8);
							}
						}
						else if ((ts.ColorFlag & ColorFlags.CF_XTERM256) != 0) {
							RepStr += String.Format(";38;5;{0}", color);
						}
					}
					if (((CharAttr.Attr2 & AttributeBitMasks.Attr2Back) != 0) && ((ts.ColorFlag & ColorFlags.CF_ANSICOLOR) != 0)) {
						int color = (int)CharAttr.Back;
						if (color <= 7 && ((CharAttr.Attr & AttributeBitMasks.AttrBlink) != 0) && ((ts.ColorFlag & ColorFlags.CF_PCBOLD16) != 0)) {
							color += 8;
						}
						if (color <= 7) {
							RepStr += String.Format(";4{0}", color);
						}
						else if (color <= 15) {
							if ((ts.ColorFlag & ColorFlags.CF_AIXTERM16) != 0) {
								RepStr += String.Format(";10{0}", color - 8);
							}
							else if ((ts.ColorFlag & ColorFlags.CF_XTERM256) != 0) {
								RepStr += String.Format(";48;5;{0}", color);
							}
							else if ((ts.ColorFlag & ColorFlags.CF_PCBOLD16) != 0) {
								RepStr += String.Format(";4{0}", color - 8);
							}
						}
						else if ((ts.ColorFlag & ColorFlags.CF_XTERM256) != 0) {
							RepStr += String.Format(";48;5;{0}", color);
						}
					}
					RepStr += 'm';
				}
				break;
			case 'r':   // DECSTBM
				if (StrBuff[1] == 0) {
					RepStr = String.Format("1$r{0};{1}r", Buffer.CursorTop + 1, Buffer.CursorBottom + 1);
				}
				break;
			case 's':   // DECSLRM
				if (StrBuff[1] == 0) {
					RepStr = String.Format("1$r{0};{0}s", Buffer.CursorLeftM + 1, Buffer.CursorRightM + 1);
				}
				break;
			}
			if (!String.IsNullOrEmpty(RepStr)) {
				RepStr = "0$r";
			}
			if ((ts.TermFlag & TerminalFlags.TF_INVALIDDECRPSS) != 0) {
				if (RepStr[0] == '0') {
					RepStr = "1" + RepStr.Substring(1);
				}
				else {
					RepStr = "0" + RepStr.Substring(1);
				}
			}
			SendDCSstr(RepStr, RepStr.Length);
		}

		int toHexStr(char[] buff, int offset, int buffsize, string str)
		{
			int len, i, copylen = offset;
			int c;

			len = str.Length;

			if (buffsize < len * 2) {
				return -1;
			}

			for (i = 0; i < len; i++) {
				c = str[i] >> 4;
				if (c <= 9) {
					c += '0';
				}
				else {
					c += 'a' - 10;
				}
				buff[copylen++] = (char)c;

				c = str[i] & 0xf;
				if (c <= 9) {
					c += '0';
				}
				else {
					c += 'a' - 10;
				}
				buff[copylen++] = (char)c;
			}

			return copylen - offset;
		}

		int TermcapString(char[] buff, int offset, int buffsize, string capname)
		{
			int len = 0, l;
			string capval = null;

			if (capname == "Co" || capname == "colors") {
				if ((ts.ColorFlag & ColorFlags.CF_ANSICOLOR) == 0) {
					return 0;
				}

				if ((ts.ColorFlag & ColorFlags.CF_XTERM256) != 0) {
					capval = "256";
				}
				else if ((ts.ColorFlag & ColorFlags.CF_FULLCOLOR) != 0) {
					capval = "16";
				}
				else {
					capval = "8";
				}
			}

			if (!String.IsNullOrEmpty(capval)) {
				if ((len = toHexStr(buff, offset, buffsize, capname)) < 0) {
					return 0;
				}

				if (buffsize <= len) {
					return 0;
				}
				buff[offset + len++] = (char)'=';

				if ((l = toHexStr(buff, offset + len, buffsize - len, capval)) < 0) {
					return 0;
				}
				len += l;
			}

			return len;
		}

		void RequestTermcapString(byte[] StrBuff, int StrLen)   // xterm experimental
		{
			char[] RepStr = new char[256];
			char[] CapName = new char[256];
			int i, len, replen, caplen = 0;

			RepStr[0] = '1';
			RepStr[1] = '+';
			RepStr[2] = 'r';
			replen = 3;

			for (i = 0; i < StrLen; i++) {
				if (StrBuff[i] == ';') {
					if (replen >= RepStr.Length) {
						caplen = 0;
						break;
					}
					if (replen > 3) {
						RepStr[replen++] = ';';
					}
					if (caplen > 0 && caplen < CapName.Length) {
						CapName[caplen] = '\0';
						len = TermcapString(RepStr, replen, RepStr.Length - replen, CapName.ToString());
						replen += len;
						caplen = 0;
						if (len == 0) {
							break;
						}
					}
					else {
						caplen = 0;
						break;
					}
				}
				else if (i + 1 < StrLen && isxdigit(StrBuff[i]) && isxdigit(StrBuff[i + 1])
				  && caplen < CapName.Length - 1) {
					if (Char.IsDigit((char)StrBuff[i])) {
						CapName[caplen] = (char)((StrBuff[i] - '0') * 16);
					}
					else {
						CapName[caplen] = (char)(((StrBuff[i] | 0x20) - 'a' + 10) * 16);
					}
					i++;
					if (Char.IsDigit((char)StrBuff[i])) {
						CapName[caplen] += (char)(StrBuff[i] - '0');
					}
					else {
						CapName[caplen] += (char)((StrBuff[i] | 0x20) - 'a' + 10);
					}
					caplen++;
				}
				else {
					caplen = 0;
					break;
				}
			}

			if (caplen != 0 && caplen < CapName.Length && replen < RepStr.Length) {
				if (replen > 3) {
					RepStr[replen++] = ';';
				}
				CapName[caplen] = '\0';
				len = TermcapString(RepStr, replen, RepStr.Length - replen, CapName.ToString());
				replen += len;
			}

			if (replen == 3) {
				RepStr[0] = '0';
			}
			SendDCSstr(RepStr.ToString(), replen);
		}

		void ParseDCS(byte Cmd, byte[] StrBuff, int len)
		{
			switch (ICount) {
			case 0:
				break;
			case 1:
				switch ((char)IntChar[1]) {
				case '!':
					if (Cmd == '{') { // DECSTUI
						if ((ts.TermFlag & TerminalFlags.TF_LOCKTUID) == 0) {
							int i;
							for (i = 0; i < 8 && isxdigit(StrBuff[i]); i++) {
								if (Char.IsLower((char)StrBuff[i])) {
									StrBuff[i] = (byte)Char.ToUpper((char)StrBuff[i]);
								}
							}
							if (len == 8 && i == 8) {
								Array.Copy(StrBuff, ts.TerminalUID, ts.TerminalUID.Length);
							}
						}
					}
					break;
				case '$':
					if (Cmd == 'q') { // DECRQSS
						RequestStatusString(StrBuff, len);
					}
					break;
				case '+':
					if (Cmd == 'q') { // Request termcap/terminfo string (xterm)
						RequestTermcapString(StrBuff, len);
					}
					break;
				default:
					break;
				}
				break;
			default:
				break;
			}
		}

		private bool isxdigit(byte p)
		{
			if ((p >= (byte)'0') && (p <= (byte)'9'))
				return true;
			if ((p >= (byte)'A') && (p <= (byte)'F'))
				return true;
			if ((p >= (byte)'a') && (p <= (byte)'f'))
				return true;
			return false;
		}

		const int ModeDcsFirst = 1;
		const int ModeDcsString = 2;
		byte[] StrBuff = new byte[256];
		int DcsParseMode = ModeDcsFirst;
		int StrLen;
		int DeviceControl_utf8_stat = 0;
		byte Cmd;

		void DeviceControl(byte b)
		{
			if ((ESCFlag && (b == '\\')) || (b == (byte)ControlCharacters.ST && ts.KanjiCode != KanjiCodeId.IdSJIS && DeviceControl_utf8_stat == 0)) {
				if (DcsParseMode == ModeDcsString) {
					StrBuff[StrLen] = 0;
					ParseDCS(Cmd, StrBuff, StrLen);
				}
				ESCFlag = false;
				ParseMode = ParsingMode.ModeFirst;
				DcsParseMode = ModeDcsFirst;
				StrLen = 0;
				DeviceControl_utf8_stat = 0;
				return;
			}

			if (b == (byte)ControlCharacters.ESC) {
				ESCFlag = true;
				DeviceControl_utf8_stat = 0;
				return;
			}
			else {
				ESCFlag = false;
			}

			DeviceControl_utf8_stat = CheckUTF8Seq(b, utf8_stat);

			switch (DcsParseMode) {
			case ModeDcsFirst:
				if (b <= (byte)ControlCharacters.US) {
					ParseControl(b);
				}
				else if ((b >= 0x20) && (b <= 0x2F)) {
					if (ICount < IntCharMax) ICount++;
					IntChar[ICount] = b;
				}
				else if ((b >= 0x30) && (b <= 0x39)) {
					Param[NParam] = Param[NParam] * 10 + b - 0x30;
				}
				else if (b == 0x3B) {
					if (NParam < NParamMax) {
						NParam++;
						Param[NParam] = -1;
					}
				}
				else if ((b >= 0x40) && (b <= 0x7E)) {
					if (ICount == 0 && b == '|') {
						ParseMode = ParsingMode.ModeDCUserKey;
						if (Param[1] < 1) keyboard.ClearUserKey();
						WaitKeyId = true;
						NewKeyId = 0;
					}
					else {
						Cmd = b;
						DcsParseMode = ModeDcsString;
					}
				}
				else {
					ParseMode = ParsingMode.ModeIgnore;
					DeviceControl_utf8_stat = 0;
					IgnoreString(b);
				}
				break;

			case ModeDcsString:
				if (b <= (byte)ControlCharacters.US && b != (byte)ControlCharacters.HT && b != (byte)ControlCharacters.CR) {
					ESCFlag = false;
					ParseMode = ParsingMode.ModeFirst;
					DcsParseMode = ModeDcsFirst;
					StrLen = 0;
				}
				else if (StrLen < StrBuff.Length - 1) {
					StrBuff[StrLen++] = b;
				}
				break;
			}
		}

		static int DCUserKey_utf8_stat = 0;

		void DCUserKey(byte b)
		{
			if (ESCFlag && (b == '\\') || (b == (byte)ControlCharacters.ST && ts.KanjiCode != KanjiCodeId.IdSJIS && DCUserKey_utf8_stat == 0)) {
				if (!WaitKeyId) keyboard.DefineUserKey(NewKeyId, NewKeyStr, NewKeyLen);
				ESCFlag = false;
				ParseMode = ParsingMode.ModeFirst;
				return;
			}

			if (b == (byte)ControlCharacters.ESC) {
				ESCFlag = true;
				return;
			}
			else ESCFlag = false;

			DCUserKey_utf8_stat = CheckUTF8Seq(b, DCUserKey_utf8_stat);

			if (WaitKeyId) {
				if ((b >= 0x30) && (b <= 0x39)) {
					if (NewKeyId < 1000)
						NewKeyId = NewKeyId * 10 + b - 0x30;
				}
				else if (b == 0x2F) {
					WaitKeyId = false;
					WaitHi = true;
					NewKeyLen = 0;
				}
			}
			else {
				if (b == 0x3B) {
					keyboard.DefineUserKey(NewKeyId, NewKeyStr, NewKeyLen);
					WaitKeyId = true;
					NewKeyId = 0;
				}
				else {
					if (NewKeyLen < keyboard.FuncKeyStrMax) {
						if (WaitHi) {
							NewKeyStr[NewKeyLen] = (byte)(ttlib.ConvHexChar(b) << 4);
							WaitHi = false;
						}
						else {
							NewKeyStr[NewKeyLen] = (byte)(NewKeyStr[NewKeyLen] + ttlib.ConvHexChar(b));
							WaitHi = true;
							NewKeyLen++;
						}
					}
				}
			}
		}

		bool XsParseColor(string colspec, ref Color color)
		{
			Match m;
			uint r, g, b;
			//	double dr, dg, db;

			r = g = b = 255;

			if (colspec == null || color == null) {
				return false;
			}

			if (colspec.StartsWith("rgb:")) {
				switch (colspec.Length) {
				case 9: // rgb:R/G/B
					m = Regex.Match(colspec, "rgb:([0-9a-fA-F])/([0-9a-fA-F])/([0-9a-fA-F])");
					if (!m.Success) {
						return false;
					}
					r = UInt32.Parse(m.Groups[0].Value, NumberStyles.HexNumber);
					g = UInt32.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
					b = UInt32.Parse(m.Groups[2].Value, NumberStyles.HexNumber);
					r *= 17; g *= 17; b *= 17;
					break;
				case 12:    // rgb:RR/GG/BB
					m = Regex.Match(colspec, "rgb:([0-9a-fA-F]{2})/([0-9a-fA-F]{2})/([0-9a-fA-F]{2})");
					if (!m.Success) {
						return false;
					}
					r = UInt32.Parse(m.Groups[0].Value, NumberStyles.HexNumber);
					g = UInt32.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
					b = UInt32.Parse(m.Groups[2].Value, NumberStyles.HexNumber);
					break;
				case 15:    // rgb:RRR/GGG/BBB
					m = Regex.Match(colspec, "rgb:([0-9a-fA-F]{3})/([0-9a-fA-F]{3})/([0-9a-fA-F]{3})");
					if (!m.Success) {
						return false;
					}
					r = UInt32.Parse(m.Groups[0].Value, NumberStyles.HexNumber);
					g = UInt32.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
					b = UInt32.Parse(m.Groups[2].Value, NumberStyles.HexNumber);
					r >>= 4; g >>= 4; b >>= 4;
					break;
				case 18:    // rgb:RRRR/GGGG/BBBB
					m = Regex.Match(colspec, "rgb:([0-9a-fA-F]{4})/([0-9a-fA-F]{4})/([0-9a-fA-F]{4})");
					if (!m.Success) {
						return false;
					}
					r = UInt32.Parse(m.Groups[0].Value, NumberStyles.HexNumber);
					g = UInt32.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
					b = UInt32.Parse(m.Groups[2].Value, NumberStyles.HexNumber);
					r >>= 8; g >>= 8; b >>= 8;
					break;
				default:
					return false;
				}
			}
			//	else if (_strnicmp(colspec, "rgbi:", 5) == 0) {
			//		; /* nothing to do */
			//	}
			else if (colspec[0] == '#') {
				switch (colspec.Length) {
				case 4: // #RGB
					m = Regex.Match(colspec, "#([0-9a-fA-F])([0-9a-fA-F])([0-9a-fA-F])");
					if (!m.Success) {
						return false;
					}
					r = UInt32.Parse(m.Groups[0].Value, NumberStyles.HexNumber);
					g = UInt32.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
					b = UInt32.Parse(m.Groups[2].Value, NumberStyles.HexNumber);
					r <<= 4; g <<= 4; b <<= 4;
					break;
				case 7: // #RRGGBB
					m = Regex.Match(colspec, "#([0-9a-fA-F]{2})([0-9a-fA-F]{2})([0-9a-fA-F]{2})");
					if (!m.Success) {
						return false;
					}
					r = UInt32.Parse(m.Groups[0].Value, NumberStyles.HexNumber);
					g = UInt32.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
					b = UInt32.Parse(m.Groups[2].Value, NumberStyles.HexNumber);
					break;
				case 10:    // #RRRGGGBBB
					m = Regex.Match(colspec, "#([0-9a-fA-F]{3})([0-9a-fA-F]{3})([0-9a-fA-F]{3})");
					if (!m.Success) {
						return false;
					}
					r = UInt32.Parse(m.Groups[0].Value, NumberStyles.HexNumber);
					g = UInt32.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
					b = UInt32.Parse(m.Groups[2].Value, NumberStyles.HexNumber);
					r >>= 4; g >>= 4; b >>= 4;
					break;
				case 13:    // #RRRRGGGGBBBB
					m = Regex.Match(colspec, "#([0-9a-fA-F]{4})([0-9a-fA-F]{4})([0-9a-fA-F]{4})");
					if (!m.Success) {
						return false;
					}
					r = UInt32.Parse(m.Groups[0].Value, NumberStyles.HexNumber);
					g = UInt32.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
					b = UInt32.Parse(m.Groups[2].Value, NumberStyles.HexNumber);
					r >>= 8; g >>= 8; b >>= 8;
					break;
				default:
					return false;
				}
			}
			else {
				return false;
			}

			if (r > 255 || g > 255 || b > 255) {
				return false;
			}

			color = Color.FromArgb((int)r, (int)g, (int)b);
			return true;
		}

		const int ModeXsFirst = 1;
		const int ModeXsString = 2;
		const int ModeXsColorNum = 3;
		const int ModeXsColorSpec = 4;
		const int ModeXsEsc = 5;
		byte XsParseMode = ModeXsFirst, PrevMode;
		byte[] XSequenceStrBuff = new byte[tttypes.TitleBuffSize];
		int ColorNumber, XSequenceStrLen;

		ANSIColors XtColor2TTColor(int mode, uint xt_color)
		{
			ANSIColors colornum = ANSIColors.CS_UNSPEC;

			switch ((mode >= 100) ? mode - 100 : mode) {
			case 4:
				switch (xt_color) {
				case 256:
					colornum = ANSIColors.CS_VT_BOLDFG;
					break;
				case 257:
					// Underline -- not supported.
					// colornum = ANSIColors.CS_VT_UNDERFG;
					break;
				case 258:
					colornum = ANSIColors.CS_VT_BLINKFG;
					break;
				case 259:
					colornum = ANSIColors.CS_VT_REVERSEBG;
					break;
				case (uint)ANSIColors.CS_UNSPEC:
					if (mode == 104) {
						colornum = ANSIColors.CS_ANSICOLOR_ALL;
					}
					break;
				default:
					if (xt_color <= 255) {
						colornum = (ANSIColors)xt_color;
					}
					break;
				}
				break;
			case 5:
				switch (xt_color) {
				case 0:
					colornum = ANSIColors.CS_VT_BOLDFG;
					break;
				case 1:
					// Underline -- not supported.
					// colornum = ANSIColors.CS_VT_UNDERFG;
					break;
				case 2:
					colornum = ANSIColors.CS_VT_BLINKFG;
					break;
				case 3:
					colornum = ANSIColors.CS_VT_REVERSEBG;
					break;
				case (uint)ANSIColors.CS_UNSPEC:
					if (mode == 105) {
						colornum = ANSIColors.CS_SP_ALL;
					}
					break;
				}
				break;
			case 10:
				colornum = ANSIColors.CS_VT_NORMALFG;
				break;
			case 11:
				colornum = ANSIColors.CS_VT_NORMALBG;
				break;
			case 15:
				colornum = ANSIColors.CS_TEK_FG;
				break;
			case 16:
				colornum = ANSIColors.CS_TEK_BG;
				break;
			}
			return colornum;
		}

		void XsProcColor(int mode, uint ColorNumber, string ColorSpec, char TermChar)
		{
			Color color = new Color();
			string StrBuff;
			ANSIColors colornum;

			colornum = XtColor2TTColor(mode, ColorNumber);

			if (colornum != ANSIColors.CS_UNSPEC) {
				if (ColorSpec == "?") {
					color = VTDisp.DispGetColor(colornum);
					if (mode == 4 || mode == 5) {
						StrBuff = String.Format("{0};{1};rgb:{2:04x}/{3:04x}/{4:04x}", mode, ColorNumber,
							color.R * 257, color.G * 257, color.B * 257);
					}
					else {
						StrBuff = String.Format("{0};rgb:{1:04x}/{2:04x}/{3:04x}", mode,
							color.R * 257, color.G * 257, color.B * 257);
					}
					SendOSCstr(StrBuff, StrBuff.Length, TermChar);
				}
				else if (XsParseColor(ColorSpec, ref color)) {
					VTDisp.DispSetColor(colornum, color);
				}
			}
		}

		void XsProcClipboard(string buff)
		{
			int len, blen, p;
			char[] cbbuff, notify_buff, notify_title;
			string hdr;
			char[] cbmem;
			int wide_len;
			char[] wide_cbmem;
			char[] wide_buf = null;

			p = 0;
			while ("cps01234567".IndexOf(buff[p]) >= 0) {
				p++;
			}

			if (buff[p++] == ';') {
				if (buff[p] == '?' && buff[p + 1] == 0) { // Read access
					if ((ts.CtrlFlag & ControlSequenceFlags.CSF_CBREAD) != 0) {
						if (ts.NotifyClipboardAccess != 0) {
							notify_title = new char[256];
							ttlib.get_lang_msg("MSG_CBACCESS_TITLE", notify_title, notify_title.Length,
										 "Clipboard Access", ts.UILanguageFile);
							notify_buff = new char[256];
							ttlib.get_lang_msg("MSG_CBACCESS_READ", notify_buff, notify_buff.Length,
										 "Remote host reads clipboard contents.", ts.UILanguageFile);
							ttcmn.NotifyInfoMessage(cv, notify_buff.ToString(), notify_title.ToString());
						}
						hdr = "\033]52;" + buff.Substring(p);
						clipboar.CBStartPasteB64(ttwinman.HVTWin, hdr, "\033\\");
					}
					else if (ts.NotifyClipboardAccess != 0) {
						notify_title = new char[256];
						ttlib.get_lang_msg("MSG_CBACCESS_REJECT_TITLE", notify_title, notify_title.Length,
									 "Rejected Clipboard Access", ts.UILanguageFile);
						notify_buff = new char[256];
						ttlib.get_lang_msg("MSG_CBACCESS_READ_REJECT", notify_buff, notify_buff.Length,
									 "Reject clipboard read access from remote.", ts.UILanguageFile);
						ttcmn.NotifyWarnMessage(cv, notify_buff.ToString(), notify_title.ToString());
					}
				}
				else if ((ts.CtrlFlag & ControlSequenceFlags.CSF_CBWRITE) != 0) { // Write access
					len = buff.Length;
					blen = len * 3 / 4 + 1;

					if ((cbmem = Buffer.GlobalAlloc<char>(Buffer.GMEM_MOVEABLE, blen)) == null) {
						return;
					};
					if ((cbbuff = Buffer.GlobalLock(cbmem)) == null) {
						Buffer.GlobalFree(cbmem);
						return;
					}

					len = ttlib.b64decode(cbbuff, blen, p);

					if (len < 0 || len >= blen) {
						Buffer.GlobalUnlock(cbmem);
						Buffer.GlobalFree(cbmem);
						return;
					}

					cbbuff[len] = '\0';
					Buffer.GlobalUnlock(cbmem);

					if (ts.NotifyClipboardAccess != 0) {
						notify_title = new char[256];
						ttlib.get_lang_msg("MSG_CBACCESS_TITLE", notify_title, notify_title.Length,
									 "Clipboard Access", ts.UILanguageFile);
						ttlib.get_lang_msg("MSG_CBACCESS_WRITE", ts.UIMsg, ts.UIMsg.Length,
									 "Remote host wirtes clipboard.", ts.UILanguageFile);
						string message = String.Format("{0}\n--\n{1}", ts.UIMsg, cbbuff);
						ttcmn.NotifyInfoMessage(cv, message, notify_title.ToString());
					}

					wide_len = Buffer.MultiByteToWideChar(Buffer.CP_ACP, 0, cbbuff, -1, null, 0);
					wide_cbmem = Buffer.GlobalAlloc<char>(Buffer.GMEM_MOVEABLE, sizeof(char) * wide_len);
					if (wide_cbmem != null) {
						wide_buf = Buffer.GlobalLock(wide_cbmem);
						Buffer.MultiByteToWideChar(Buffer.CP_ACP, 0, cbbuff, -1, wide_buf, wide_len);
						Buffer.GlobalUnlock(wide_cbmem);
					}

					if (clipboar.OpenClipboard(null)) {
						clipboar.EmptyClipboard();
						clipboar.SetClipboardData(clipboar.CF_TEXT, cbmem);
						if (wide_buf != null) {
							clipboar.SetClipboardData(clipboar.CF_UNICODETEXT, wide_cbmem);
						}
						clipboar.CloseClipboard();
					}
				}
				else if (ts.NotifyClipboardAccess != 0) {
					notify_title = new char[256];
					ttlib.get_lang_msg("MSG_CBACCESS_REJECT_TITLE", notify_title, notify_title.Length,
								 "Rejected Clipboard Access", ts.UILanguageFile);
					notify_buff = new char[256];
					ttlib.get_lang_msg("MSG_CBACCESS_WRITE_REJECT", notify_buff, notify_buff.Length,
								 "Reject clipboard write access from remote.", ts.UILanguageFile);
					ttcmn.NotifyWarnMessage(cv, notify_buff.ToString(), notify_title.ToString());
				}
			}
		}

		static char[] XSequence_StrBuff = null;
		static int XSequence_StrLen = 0, XSequence_StrBuffSize = 0;
		static int XSequence_utf8_stat = 0;
		static bool XSequence_realloc_failed = false;
		static bool XSequence_ESCflag = false, XSequence_HasParamStr = false;

		void XSequence(byte b)
		{
			int p;
			string color_spec;
			int new_size;
			uint color_num;
			char TermChar;

			TermChar = '\0';

			if (XSequence_ESCflag) {
				XSequence_ESCflag = false;
				if (b == '\\') {
					TermChar = (char)ControlCharacters.ST;
				}
				else {  // Invalid Sequence
					ParseMode = ParsingMode.ModeIgnore;
					XSequence_HasParamStr = false;
					IgnoreString(b);
					return;
				}
			}
			else if (b == (char)ControlCharacters.BEL) {
				TermChar = (char)ControlCharacters.BEL;
			}
			else if (b == (char)ControlCharacters.ST && Accept8BitCtrl(VTlevel, ts) && !(ts.Language == Language.IdJapanese && ts.KanjiCode == KanjiCodeId.IdSJIS) && XSequence_utf8_stat == 0) {
				TermChar = (char)ControlCharacters.ST;
			}

			if (TermChar != '\0') {
				if (XSequence_StrBuff != null) {
					if (XSequence_StrLen < XSequence_StrBuffSize) {
						XSequence_StrBuff[XSequence_StrLen] = '\0';
					}
					else {
						XSequence_StrBuff[XSequence_StrBuffSize - 1] = '\0';
					}
				}
				switch (Param[1]) {
				case 0: /* Change window title and icon name */
				case 1: /* Change icon name */
				case 2: /* Change window title */
					if (XSequence_StrBuff != null && ts.AcceptTitleChangeRequest != 0) {
						cv.TitleRemote = XSequence_StrBuff.ToString();
						// (2006.6.15 maya) タイトルに渡す文字列をSJISに変換
						//ttwinman.ConvertToCP932(cv.TitleRemote, cv.TitleRemote.Length);
						ttwinman.ChangeTitle();
					}
					break;
				case 4: /* Change/Query color palette */
				case 5: /* Change/Query special color */
					if (XSequence_StrBuff != null) {
						color_num = 0;
						color_spec = "";
						for (p = 0; XSequence_StrBuff[p] != '\0'; p++) {
							if (String.IsNullOrEmpty(color_spec)) {
								if (Char.IsDigit(XSequence_StrBuff[p])) {
									color_num = color_num * 10 + XSequence_StrBuff[p] - '0';
								}
								else if (XSequence_StrBuff[p] == ';') {
									color_spec = XSequence_StrBuff.ToString().Substring(p + 1);
								}
								else {
									break;
								}
							}
							else {
								if (XSequence_StrBuff[p] == ';') {
									XSequence_StrBuff[p] = '\0';
									XsProcColor(Param[1], color_num, color_spec, TermChar);
									color_num = 0;
									color_spec = "";
								}
							}
						}
						if (!String.IsNullOrEmpty(color_spec)) {
							XsProcColor(Param[1], color_num, color_spec, TermChar);
						}
					}
					break;
				case 10: /* Change/Query VT-Window foreground color */
				case 11: /* Change/Query VT-Window background color */
				case 12: /* Change/Query VT-Window cursor color */
				case 13: /* Change/Query mouse cursor foreground color */
				case 14: /* Change/Query mouse cursor background color */
				case 15: /* Change/Query Tek-Window foreground color */
				case 16: /* Change/Query Tek-Window foreground color */
				case 17: /* Change/Query highlight background color */
				case 18: /* Change/Query Tek-Window cursor color */
				case 19: /* Change/Query highlight foreground color */
					if (XSequence_StrBuff != null) {
						int mode = Param[1];
						color_spec = XSequence_StrBuff.ToString();
						for (p = 0; XSequence_StrBuff[p] != 0; p++) {
							if (XSequence_StrBuff[p] == ';') {
								XSequence_StrBuff[p] = '\0';
								XsProcColor(mode, 0, color_spec, TermChar);
								mode++;
								color_spec = XSequence_StrBuff.ToString().Substring(p + 1);
							}
						}
						XsProcColor(mode, 0, color_spec, TermChar);
					}
					break;
				case 52: /* Manipulate Clipboard data */
					if (XSequence_StrBuff != null) {
						XsProcClipboard(XSequence_StrBuff.ToString());
					}
					break;
				case 104: /* Reset color palette */
				case 105: /* Reset special color */
					if (XSequence_HasParamStr) {
						if (XSequence_StrBuff != null) {
							color_num = 0;
							for (p = 0; XSequence_StrBuff[p] != 0; p++) {
								if (Char.IsDigit(XSequence_StrBuff[p])) {
									color_num = color_num * 10 + XSequence_StrBuff[p] - '0';
								}
								else if (XSequence_StrBuff[p] == ';') {
									VTDisp.DispResetColor(XtColor2TTColor(Param[1], color_num));
									color_num = 0;
								}
								else {
									color_num = (uint)ANSIColors.CS_UNSPEC;
								}
							}
							if (color_num != (uint)ANSIColors.CS_UNSPEC) {
								VTDisp.DispResetColor(XtColor2TTColor(Param[1], color_num));
							}
						}
					}
					else {
						VTDisp.DispResetColor(XtColor2TTColor(Param[1], (uint)ANSIColors.CS_UNSPEC));
					}
					break;
				case 110: /* Reset VT-Window foreground color */
				case 111: /* Reset VT-Window background color */
				case 112: /* Reset VT-Window cursor color */
				case 113: /* Reset mouse cursor foreground color */
				case 114: /* Reset mouse cursor background color */
				case 115: /* Reset Tek-Window foreground color */
				case 116: /* Reset Tek-Window foreground color */
				case 117: /* Reset highlight background color */
				case 118: /* Reset Tek-Window cursor color */
				case 119: /* Reset highlight foreground color */
					VTDisp.DispResetColor(XtColor2TTColor(Param[1], (uint)ANSIColors.CS_UNSPEC));
					if (XSequence_HasParamStr && XSequence_StrBuff != null) {
						int mode = 0;
						for (p = 0; XSequence_StrBuff[p] != 0; p++) {
							if (Char.IsDigit(XSequence_StrBuff[p])) {
								mode = mode * 10 + XSequence_StrBuff[p] - '0';
							}
							else if (XSequence_StrBuff[p] == ';') {
								VTDisp.DispResetColor(XtColor2TTColor(mode, (uint)ANSIColors.CS_UNSPEC));
								mode = 0;
							}
							else {
								mode = unchecked((int)ANSIColors.CS_UNSPEC);
								break;
							}
						}
						if (mode != unchecked((int)ANSIColors.CS_UNSPEC)) {
							VTDisp.DispResetColor(XtColor2TTColor(mode, (uint)ANSIColors.CS_UNSPEC));
						}
					}
					break;
				}
				if (XSequence_StrBuff != null) {
					XSequence_StrBuff[0] = '\0';
					XSequence_StrLen = 0;
				}
				ParseMode = ParsingMode.ModeFirst;
				XSequence_HasParamStr = false;
				XSequence_utf8_stat = 0;
			}
			else if (b == (byte)ControlCharacters.ESC) {
				XSequence_ESCflag = true;
				XSequence_utf8_stat = 0;
			}
			else if (b <= (byte)ControlCharacters.US) { // Invalid Character
				ParseMode = ParsingMode.ModeFirst;
				XSequence_HasParamStr = false;
				XSequence_utf8_stat = 0;
			}
			else if (XSequence_HasParamStr) {
				XSequence_utf8_stat = CheckUTF8Seq(b, XSequence_utf8_stat);
				if (XSequence_StrLen + 1 < XSequence_StrBuffSize) {
					XSequence_StrBuff[XSequence_StrLen++] = (char)b;
				}
				else if (!XSequence_realloc_failed && XSequence_StrBuffSize < ts.MaxOSCBufferSize) {
					if (XSequence_StrBuff == null || XSequence_StrBuffSize == 0) {
						new_size = ts.Title.Length;
					}
					else {
						new_size = XSequence_StrBuffSize * 2;
					}
					if (new_size > ts.MaxOSCBufferSize) {
						new_size = ts.MaxOSCBufferSize;
					}

					Array.Resize(ref XSequence_StrBuff, new_size);
					p = 1;
					if (p == 0) {
						if (XSequence_StrBuff == null) {
							XSequence_StrBuffSize = 0;
							ParseMode = ParsingMode.ModeIgnore;
							XSequence_HasParamStr = false;
							IgnoreString(b);
							return;
						}
						XSequence_realloc_failed = true;
					}
					else {
						//XSequence_StrBuff = p;
						XSequence_StrBuffSize = new_size;
						if (XSequence_StrLen + 1 < XSequence_StrBuffSize) {
							XSequence_StrBuff[XSequence_StrLen++] = (char)b;
						}
					}
				}
			}
			else if (Char.IsDigit((char)b)) {
				Param[1] = Param[1] * 10 + b - '0';
			}
			else if (b == ';') {
				XSequence_HasParamStr = true;
			}
			else {
				ParseMode = ParsingMode.ModeIgnore;
				XSequence_HasParamStr = false;
				IgnoreString(b);
			}
		}

		void DLESeen(byte b)
		{
			ParseMode = ParsingMode.ModeFirst;
			if (((ts.FTFlag & FileTransferFlags.FT_BPAUTO) != 0) && (b == 'B'))
				filesys.BPStart(BPlusFunctionId.IdBPAuto); /* Auto B-Plus activation */
			ChangeEmu = (WindowId)(-1);
		}

		static int CANSeen_state = 0;

		void CANSeen(byte b)
		{
			if ((ts.FTFlag & FileTransferFlags.FT_ZAUTO) != 0) {
				if (CANSeen_state == 0 && b == 'B') {
					CANSeen_state = 1;
				}
				else if (CANSeen_state == 1 && b == '0') {
					CANSeen_state = 2;
				}
				else {
					if (CANSeen_state == 2) {
						if (b == '0'/* ZRQINIT */) {
							/* Auto ZMODEM activation (Receive) */
							filesys.ZMODEMStart(ZMODEMFunctionId.IdZAutoR);
						}
						else if (b == '1'/* ZRINIT */) {
							/* Auto ZMODEM activation (Send) */
							filesys.ZMODEMStart(ZMODEMFunctionId.IdZAutoS);
						}
					}
					ParseMode = ParsingMode.ModeFirst;
					ChangeEmu = (WindowId)(-1);
					CANSeen_state = 0;
				}
			}
			else {
				ParseMode = ParsingMode.ModeFirst;
				ChangeEmu = (WindowId)(-1);
			}
		}

		bool CheckKanji(byte b)
		{
			bool Check;

			if (ts.Language != Language.IdJapanese)
				return false;

			ConvJIS = false;

			if (ts.KanjiCode == KanjiCodeId.IdSJIS ||
				(ts.FallbackToCP932 && (ts.KanjiCode == KanjiCodeId.IdUTF8 || ts.KanjiCode == KanjiCodeId.IdUTF8m))) {
				if ((0x80 < b) && (b < 0xa0) || (0xdf < b) && (b < 0xfd)) {
					Fallbacked = true;
					return true; // SJIS kanji
				}
				if ((0xa1 <= b) && (b <= 0xdf)) {
					return false; // SJIS katakana
				}
			}

			if ((b >= 0x21) && (b <= 0x7e)) {
				Check = (Gn[Glr[0]] == CharacterSets.IdKanji);
				ConvJIS = Check;
			}
			else if ((b >= 0xA1) && (b <= 0xFE)) {
				Check = (Gn[Glr[1]] == CharacterSets.IdKanji);
				if (ts.KanjiCode == KanjiCodeId.IdEUC) {
					Check = true;
				}
				else if (ts.KanjiCode == KanjiCodeId.IdJIS && ((ts.TermFlag & TerminalFlags.TF_FIXEDJIS) != 0) && !ts.JIS7Katakana) {
					Check = false; // 8-bit katakana
				}
				ConvJIS = Check;
			}
			else {
				Check = false;
			}

			return Check;
		}

		bool CheckKorean(byte b)
		{
			bool Check = true;

			if (ts.Language != Language.IdKorean)
				return false;

			if (ts.KanjiCode == KanjiCodeId.IdSJIS) {
				if ((0xA1 <= b) && (b <= 0xFE)) {
					Check = true;
				}
				else {
					Check = false;
				}
			}

			return Check;
		}

		bool ParseFirstJP(byte b)
		// returns true if b is processed
		//  (actually allways returns true)
		{
			if (KanjiIn) {
				if ((!ConvJIS) && (0x3F < b) && (b < 0xFD) ||
					  ConvJIS && ((0x20 < b) && (b < 0x7f) ||
								   (0xa0 < b) && (b < 0xff))) {
					PutKanji(b);
					KanjiIn = false;
					return true;
				}
				else if ((ts.TermFlag & TerminalFlags.TF_CTRLINKANJI) == 0) {
					KanjiIn = false;
				}
				else if ((b == (byte)ControlCharacters.CR) && Buffer.Wrap) {
					CarriageReturn(false);
					LineFeed((byte)ControlCharacters.LF, false);
					Buffer.Wrap = false;
				}
			}

			if (SSflag) {
				if (Gn[GLtmp] == CharacterSets.IdKanji) {
					Kanji = (char)(b << 8);
					KanjiIn = true;
					SSflag = false;
					return true;
				}
				else if (Gn[GLtmp] == CharacterSets.IdKatakana) {
					b = (byte)(b | 0x80);
				}

				PutChar(b);
				SSflag = false;
				return true;
			}

			if ((!EUCsupIn) && (!EUCkanaIn) && (!KanjiIn) && CheckKanji(b)) {
				Kanji = (char)(b << 8);
				KanjiIn = true;
				return true;
			}

			if (b <= (byte)ControlCharacters.US) {
				ParseControl(b);
			}
			else if (b == 0x20) {
				PutChar(b);
			}
			else if ((b >= 0x21) && (b <= 0x7E)) {
				if (EUCsupIn) {
					EUCcount--;
					EUCsupIn = (EUCcount == 0);
					return true;
				}

				if ((Gn[Glr[0]] == CharacterSets.IdKatakana) || EUCkanaIn) {
					b = (byte)(b | 0x80);
					EUCkanaIn = false;
				}
				PutChar(b);
			}
			else if (b == 0x7f) {
				return true;
			}
			else if ((b >= 0x80) && (b <= 0x8D)) {
				ParseControl(b);
			}
			else if (b == 0x8E) { // SS2
				switch (ts.KanjiCode) {
				case KanjiCodeId.IdEUC:
					if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_SS2) != 0) {
						EUCkanaIn = true;
					}
					break;
				case KanjiCodeId.IdUTF8:
				case KanjiCodeId.IdUTF8m:
					PutChar((byte)'?');
					break;
				default:
					ParseControl(b);
					break;
				}
			}
			else if (b == 0x8F) { // SS3
				switch (ts.KanjiCode) {
				case KanjiCodeId.IdEUC:
					if ((ts.ISO2022Flag & ISO2022ShiftFlags.ISO2022_SS3) != 0) {
						EUCcount = 2;
						EUCsupIn = true;
					}
					break;
				case KanjiCodeId.IdUTF8:
				case KanjiCodeId.IdUTF8m:
					PutChar((byte)'?');
					break;
				default:
					ParseControl(b);
					break;
				}
			}
			else if ((b >= 0x90) && (b <= 0x9F)) {
				ParseControl(b);
			}
			else if (b == 0xA0) {
				PutChar(0x20);
			}
			else if ((b >= 0xA1) && (b <= 0xFE)) {
				if (EUCsupIn) {
					EUCcount--;
					EUCsupIn = (EUCcount == 0);
					return true;
				}

				if ((Gn[Glr[1]] != CharacterSets.IdASCII) ||
					(ts.KanjiCode == KanjiCodeId.IdEUC) && EUCkanaIn ||
					(ts.KanjiCode == KanjiCodeId.IdSJIS) ||
					(ts.KanjiCode == KanjiCodeId.IdJIS) &&
					!ts.JIS7Katakana &&
					((ts.TermFlag & TerminalFlags.TF_FIXEDJIS) != 0))
					PutChar(b); // katakana
				else {
					if (Gn[Glr[1]] == CharacterSets.IdASCII) {
						b = (byte)(b & 0x7f);
					}
					PutChar(b);
				}
				EUCkanaIn = false;
			}
			else {
				PutChar(b);
			}

			return true;
		}

		bool ParseFirstKR(byte b)
		// returns true if b is processed
		//  (actually allways returns true)
		{
			if (KanjiIn) {
				if ((0x41 <= b) && (b <= 0x5A) ||
					(0x61 <= b) && (b <= 0x7A) ||
					(0x81 <= b) && (b <= 0xFE)) {
					PutKanji(b);
					KanjiIn = false;
					return true;
				}
				else if ((ts.TermFlag & TerminalFlags.TF_CTRLINKANJI) == 0) {
					KanjiIn = false;
				}
				else if ((b == (byte)ControlCharacters.CR) && Buffer.Wrap) {
					CarriageReturn(false);
					LineFeed((byte)ControlCharacters.LF, false);
					Buffer.Wrap = false;
				}
			}

			if ((!KanjiIn) && CheckKorean(b)) {
				Kanji = (char)(b << 8);
				KanjiIn = true;
				return true;
			}

			if (b <= (byte)ControlCharacters.US) {
				ParseControl(b);
			}
			else if (b == 0x20) {
				PutChar(b);
			}
			else if ((b >= 0x21) && (b <= 0x7E)) {
				//		if (Gn[Glr[0]] == CharacterSets.IdKatakana) {
				//			b = b | 0x80;
				//		}
				PutChar(b);
			}
			else if (b == 0x7f) {
				return true;
			}
			else if ((0x80 <= b) && (b <= 0x9F)) {
				ParseControl(b);
			}
			else if (b == 0xA0) {
				PutChar(0x20);
			}
			else if ((b >= 0xA1) && (b <= 0xFE)) {
				if (Gn[Glr[1]] == CharacterSets.IdASCII) {
					b = (byte)(b & 0x7f);
				}
				PutChar(b);
			}
			else {
				PutChar(b);
			}

			return true;
		}

		void ParseASCII(byte b)
		{
			if (ts.Language == Language.IdJapanese) {
				ParseFirstJP(b);
				return;
			}

			if (SSflag) {
				PutChar(b);
				SSflag = false;
				return;
			}

			if (b <= (byte)ControlCharacters.US) {
				ParseControl(b);
			}
			else if ((b >= 0x20) && (b <= 0x7E)) {
				//Kanji = 0;
				//PutKanji(b);
				PutChar(b);
			}
			else if ((b == 0x8E) || (b == 0x8F)) {
				PutChar((byte)'?');
			}
			else if ((b >= 0x80) && (b <= 0x9F)) {
				ParseControl(b);
			}
			else if (b >= 0xA0) {
				//Kanji = 0;
				//PutKanji(b);
				PutChar(b);
			}
		}

		//
		// UTF-8
		//
		char GetPrecomposedChar(int start_index, char first_code, char code,
										 combining_map_t[] table, int tmax)
		{
			char result = '\0';
			int i;

			for (i = start_index; i < tmax; i++) {
				if (table[i].first_code != first_code) { // 1文字目が異なるなら、以降はもう調べなくてよい。
					break;
				}

				if (table[i].second_code == code) {
					result = (char)table[i].illegal_code;
					break;
				}
			}

			return (result);
		}

		int GetIndexOfCombiningFirstCode(char code, combining_map_t[] table, int tmax)
		{
			int low, mid, high;
			int index = -1;

			low = 0;
			high = tmax - 1;

			// binary search
			while (low < high) {
				mid = (low + high) / 2;
				if (table[mid].first_code < code) {
					low = mid + 1;
				}
				else {
					high = mid;
				}
			}

			if (table[low].first_code == code) {
				while (low >= 0 && table[low].first_code == code) {
					index = low;
					low--;
				}
			}

			return (index);
		}


		void UnicodeToCP932(char code)
		{
			int ret;
			byte[] mbchar = new byte[32];
			char[] wchar = new char[] { code };
			char cset = '\0';

			//wchar[0] = (byte)(code & 0xff);
			//wchar[1] = (byte)((code >> 8) & 0xff);

			if (ts.UnicodeDecSpMapping != 0) {
				cset = language.ConvertUnicode((char)code, codemap.mapUnicodeSymbolToDecSp, codemap.mapUnicodeSymbolToDecSp.Length);
			}
			if (((cset >> 8) & ts.UnicodeDecSpMapping) != 0) {
				PutDecSp((byte)(cset & 0xffu));
			}
			else {
				// Unicode -> CP932
				ret = Encoding.ASCII.GetBytes(wchar, 0, wchar.Length, mbchar, 0);
				switch (ret) {
				case -1:
					//if (_stricmp(ts.Locale, DEFAULT_LOCALE) == 0)
					//{
					// U+301Cなどは変換できない。Unicode -> Shift_JISへ変換してみる。
					cset = language.ConvertUnicode((char)code, codemap.mapUnicodeToSJIS, codemap.mapUnicodeToSJIS.Length);
					if (cset != 0) {
						Kanji = (char)(cset & 0xff00);
						PutKanji((byte)(cset & 0x00ff));
					}
					//}

					if (cset == 0) {
						PutChar((byte)'?');
						if (ts.UnknownUnicodeCharaAsWide) {
							PutChar((byte)'?');
						}
					}
					break;
				case 1:
					PutChar(mbchar[0]);
					break;
				default:
					Kanji = (char)(mbchar[0] << 8);
					PutKanji(mbchar[1]);
					break;
				}
			}
		}

		static byte[] ParseFirstUTF8_buf = new byte[3];
		static int ParseFirstUTF8_count = 0;
		static int ParseFirstUTF8_can_combining = 0;
		static char ParseFirstUTF8_first_code;
		static int ParseFirstUTF8_first_code_index;

		// UTF-8で受信データを処理する
		bool ParseFirstUTF8(byte b, int proc_combining)
		// returns true if b is processed
		//  (actually allways returns true)
		{
			char code;
			char[] mbchar = new char[32];
			char cset;

			if (ts.FallbackToCP932 && Fallbacked) {
				return ParseFirstJP(b);
			}

			if ((b & 0x80) != 0x80 || ((b & 0xe0) == 0x80 && ParseFirstUTF8_count == 0)) {
				// 1バイト目および2バイト目がASCIIの場合は、すべてASCII出力とする。
				// 1バイト目がC1制御文字(0x80-0x9f)の場合も同様。
				if (ParseFirstUTF8_count == 0 || ParseFirstUTF8_count == 1) {
					if (proc_combining == 1 && ParseFirstUTF8_can_combining == 1) {
						UnicodeToCP932(ParseFirstUTF8_first_code);
						ParseFirstUTF8_can_combining = 0;
					}

					if (ParseFirstUTF8_count == 1) {
						ParseASCII(ParseFirstUTF8_buf[0]);
					}
					ParseASCII(b);

					ParseFirstUTF8_count = 0;  // reset counter
					return true;
				}
			}

			ParseFirstUTF8_buf[ParseFirstUTF8_count++] = b;
			if (ParseFirstUTF8_count < 2) {
				return true;
			}

			// 2バイトコードの場合
			if ((ParseFirstUTF8_buf[0] & 0xe0) == 0xc0) {
				if ((ParseFirstUTF8_buf[1] & 0xc0) == 0x80) {

					if (proc_combining == 1 && ParseFirstUTF8_can_combining == 1) {
						UnicodeToCP932(ParseFirstUTF8_first_code);
						ParseFirstUTF8_can_combining = 0;
					}

					code = (char)((ParseFirstUTF8_buf[0] & 0x1fu) << 6);
					code |= (char)((ParseFirstUTF8_buf[1] & 0x3fu));

					UnicodeToCP932(code);
				}
				else {
					ParseASCII(ParseFirstUTF8_buf[0]);
					ParseASCII(ParseFirstUTF8_buf[1]);
				}
				ParseFirstUTF8_count = 0;
				return true;
			}

			if (ParseFirstUTF8_count < 3) {
				return true;
			}

			if ((ParseFirstUTF8_buf[0] & 0xe0) == 0xe0 &&
				(ParseFirstUTF8_buf[1] & 0xc0) == 0x80 &&
				(ParseFirstUTF8_buf[2] & 0xc0) == 0x80) { // 3バイトコードの場合

				// UTF-8 BOM(Byte Order Mark)
				if (ParseFirstUTF8_buf[0] == 0xef && ParseFirstUTF8_buf[1] == 0xbb && ParseFirstUTF8_buf[2] == 0xbf) {
					goto skip;
				}

				code = (char)((ParseFirstUTF8_buf[0] & 0xfu) << 12);
				code |= (char)((ParseFirstUTF8_buf[1] & 0x3fu) << 6);
				code |= (char)((ParseFirstUTF8_buf[2] & 0x3fu));

				if (proc_combining == 1) {
					if (ParseFirstUTF8_can_combining == 0) {
						if ((ParseFirstUTF8_first_code_index = GetIndexOfCombiningFirstCode(
								code, combining_map_t.mapCombiningToPrecomposed, combining_map_t.mapCombiningToPrecomposed.Length
								)) != -1) {
							ParseFirstUTF8_can_combining = 1;
							ParseFirstUTF8_first_code = code;
							ParseFirstUTF8_count = 0;
							return (true);
						}
					}
					else {
						ParseFirstUTF8_can_combining = 0;
						cset = GetPrecomposedChar(ParseFirstUTF8_first_code_index, ParseFirstUTF8_first_code, code, combining_map_t.mapCombiningToPrecomposed, combining_map_t.mapCombiningToPrecomposed.Length);
						if (cset != 0) { // success
							code = cset;

						}
						else { // error
							   // 2つめの文字が半濁点の1文字目に相当する場合は、再度検索を続ける。(2005.10.15 yutaka)
							if ((ParseFirstUTF8_first_code_index = GetIndexOfCombiningFirstCode(
									code, combining_map_t.mapCombiningToPrecomposed, combining_map_t.mapCombiningToPrecomposed.Length
									)) != -1) {

								// 1つめの文字はそのまま出力する
								UnicodeToCP932(ParseFirstUTF8_first_code);

								ParseFirstUTF8_can_combining = 1;
								ParseFirstUTF8_first_code = code;
								ParseFirstUTF8_count = 0;
								return (true);
							}

							UnicodeToCP932(ParseFirstUTF8_first_code);
							UnicodeToCP932(code);
							ParseFirstUTF8_count = 0;
							return (true);
						}
					}
				}

				UnicodeToCP932(code);

			skip:
				ParseFirstUTF8_count = 0;

			}
			else {
				ParseASCII(ParseFirstUTF8_buf[0]);
				ParseASCII(ParseFirstUTF8_buf[1]);
				ParseASCII(ParseFirstUTF8_buf[2]);
				ParseFirstUTF8_count = 0;

			}

			return true;
		}


		bool ParseFirstRus(byte b)
		// returns if b is processed
		{
			if (b >= 128) {
				b = language.RussConv(ts.RussHost, ts.RussClient, b);
				PutChar(b);
				return true;
			}
			return false;
		}

		void ParseFirst(byte b)
		{
			switch (ts.Language) {
			case Language.IdUtf8:
				ParseFirstUTF8(b, (ts.KanjiCode == KanjiCodeId.IdUTF8m) ? 1 : 0);
				return;

			case Language.IdJapanese:
				switch (ts.KanjiCode) {
				case KanjiCodeId.IdUTF8:
					if (ParseFirstUTF8(b, 0)) {
						return;
					}
					break;
				case KanjiCodeId.IdUTF8m:
					if (ParseFirstUTF8(b, 1)) {
						return;
					}
					break;
				default:
					if (ParseFirstJP(b)) {
						return;
					}
					break;
				}
				break;

			case Language.IdKorean:
				switch (ts.KanjiCode) {
				case KanjiCodeId.IdUTF8:
					if (ParseFirstUTF8(b, 0)) {
						return;
					}
					break;
				case KanjiCodeId.IdUTF8m:
					if (ParseFirstUTF8(b, 1)) {
						return;
					}
					break;
				default:
					if (ParseFirstKR(b)) {
						return;
					}
					break;
				}
				break;

			case Language.IdRussian:
				if (ParseFirstRus(b)) {
					return;
				}
				break;
			}

			if (SSflag) {
				PutChar(b);
				SSflag = false;
				return;
			}

			if (b <= (byte)ControlCharacters.US)
				ParseControl(b);
			else if ((b >= 0x20) && (b <= 0x7E))
				PutChar(b);
			else if ((b >= 0x80) && (b <= 0x9F))
				ParseControl(b);
			else if (b >= 0xA0)
				PutChar(b);
		}

		WindowId VTParse()
		{
			byte b;
			int c;

			c = ttcmn.CommRead1Byte(cv, out b);

			if (c == 0) return 0;

			VTDisp.CaretOff();
			VTDisp.UpdateCaretPosition(false);  // 非アクティブの場合のみ再描画する

			ChangeEmu = 0;

			/* Get Device Context */
			VTDisp.DispInitDC();

			Buffer.LockBuffer();

			while ((c > 0) && (ChangeEmu == 0)) {
				if (keyboard.DebugFlag != KeyboardDebugFlag.DEBUG_FLAG_NONE)
					PutDebugChar(b);
				else {
					switch (ParseMode) {
					case ParsingMode.ModeFirst:
						ParseFirst(b);
						break;
					case ParsingMode.ModeESC:
						EscapeSequence(b);
						break;
					case ParsingMode.ModeDCS:
						DeviceControl(b);
						break;
					case ParsingMode.ModeDCUserKey:
						DCUserKey(b);
						break;
					case ParsingMode.ModeSOS:
						IgnoreString(b);
						break;
					case ParsingMode.ModeCSI:
						ControlSequence(b);
						break;
					case ParsingMode.ModeXS:
						XSequence(b);
						break;
					case ParsingMode.ModeDLE:
						DLESeen(b);
						break;
					case ParsingMode.ModeCAN:
						CANSeen(b);
						break;
					case ParsingMode.ModeIgnore:
						IgnoreString(b);
						break;
					default:
						ParseMode = ParsingMode.ModeFirst;
						ParseFirst(b);
						break;
					}
				}

				if (ChangeEmu == 0)
					c = ttcmn.CommRead1Byte(cv, out b);
			}

			Buffer.BuffUpdateScroll();

			Buffer.BuffSetCaretWidth();
			Buffer.UnlockBuffer();

			/* release device context */
			VTDisp.DispReleaseDC();

			VTDisp.CaretOn();

			if (ChangeEmu > 0)
				ParseMode = ParsingMode.ModeFirst;

			return ChangeEmu;
		}

		string MakeLocatorReportStr(int ievent, int x, int y)
		{
			if (x < 0) {
				return String.Format("{0};{1}&w", ievent, ButtonStat);
			}
			else {
				return String.Format("{0};{1};{2};{3};0&w", ievent, ButtonStat, y, x);
			}
		}

		bool DecLocatorReport(MouseEvent Event, MouseButtons Button)
		{
			int x, y, MaxX, MaxY, len = 0;
			bool right;
			string buff = "";

			if ((DecLocatorFlag & DecLocator.DecLocatorPixel) != 0) {
				x = LastX + 1;
				y = LastY + 1;
				VTDisp.DispConvScreenToWin(VTDisp.NumOfColumns + 1, VTDisp.NumOfLines + 1, out MaxX, out MaxY);
				if (x < 1 || x > MaxX || y < 1 || y > MaxY) {
					x = -1;
				}
			}
			else {
				VTDisp.DispConvWinToScreen(LastX, LastY, out x, out y, out right);
				x++; y++;
				if (x < 1 || x > VTDisp.NumOfColumns || y < 1 || y > VTDisp.NumOfLines) {
					x = -1;
				}
			}

			switch (Event) {
			case MouseEvent.IdMouseEventCurStat:
				if (MouseReportMode == MouseTrackingMode.IdMouseTrackDECELR) {
					buff = MakeLocatorReportStr(1, x, y);
				}
				else {
					buff = "0&w";
				}
				break;

			case MouseEvent.IdMouseEventBtnDown:
				if ((DecLocatorFlag & DecLocator.DecLocatorButtonDown) != 0) {
					buff = MakeLocatorReportStr((int)Button * 2 + 2, x, y);
				}
				break;

			case MouseEvent.IdMouseEventBtnUp:
				if ((DecLocatorFlag & DecLocator.DecLocatorButtonUp) != 0) {
					buff = MakeLocatorReportStr((int)Button * 2 + 3, x, y);
				}
				break;

			case MouseEvent.IdMouseEventMove:
				if ((DecLocatorFlag & DecLocator.DecLocatorFiltered) != 0) {
					if (y < FilterTop || y > FilterBottom || x < FilterLeft || x > FilterRight) {
						buff = MakeLocatorReportStr(10, x, y);
						DecLocatorFlag &= ~DecLocator.DecLocatorFiltered;
					}
				}
				break;
			}

			if (len == 0) {
				return false;
			}

			SendCSIstr(buff, buff.Length);

			if ((DecLocatorFlag & DecLocator.DecLocatorOneShot) != 0) {
				MouseReportMode = MouseTrackingMode.IdMouseTrackNone;
			}
			return true;
		}

		string MakeMouseReportStr(int mb, int x, int y)
		{
			return String.Format("M{0}{1}{2}", mb + 32, x + 32, y + 32);
		}

		int LastSendX = -1, LastSendY = -1;
		MouseButtons LastButton = MouseButtons.IdButtonRelease;

		public bool MouseReport(MouseEvent Event, MouseButtons Button, int Xpos, int Ypos)
		{
			string Report = null;
			int x, y, modifier;
			bool right;

			switch (Event) {
			case MouseEvent.IdMouseEventBtnDown:
				ButtonStat |= (MouseButtons)(8 >> ((int)Button + 1));
				break;
			case MouseEvent.IdMouseEventBtnUp:
				ButtonStat &= (MouseButtons)~(8 >> ((int)Button + 1));
				break;
			}
			LastX = Xpos;
			LastY = Ypos;

			if (MouseReportMode == MouseTrackingMode.IdMouseTrackNone)
				return false;

			if (ts.DisableMouseTrackingByCtrl && keyboard.ControlKey())
				return false;

			if (MouseReportMode == MouseTrackingMode.IdMouseTrackDECELR)
				return DecLocatorReport(Event, Button);

			VTDisp.DispConvWinToScreen(Xpos, Ypos, out x, out y, out right);
			x++; y++;

			if (x < 1) x = 1;
			if (y < 1) y = 1;

			if (MouseReportMode != MouseTrackingMode.IdMouseTrackDECELR) {
				if (x > 0xff - 32)
					x = 0xff - 32;
				if (x > 0xff - 32)
					y = 0xff - 32;
			}

			if (keyboard.ShiftKey())
				modifier = 4;
			else
				modifier = 0;

			if (keyboard.ControlKey())
				modifier |= 8;

			if (keyboard.AltKey())
				modifier |= 16;

			modifier = (keyboard.ShiftKey() ? 4 : 0) | (keyboard.AltKey() ? 8 : 0) | (keyboard.ControlKey() ? 16 : 0);

			switch (Event) {
			case MouseEvent.IdMouseEventBtnDown:
				switch (MouseReportMode) {
				case MouseTrackingMode.IdMouseTrackX10:
					Report = MakeMouseReportStr((int)Button, x, y);
					break;

				case MouseTrackingMode.IdMouseTrackVT200:
				case MouseTrackingMode.IdMouseTrackBtnEvent:
				case MouseTrackingMode.IdMouseTrackAllEvent:
					Report = MakeMouseReportStr((int)Button | modifier, x, y);
					LastSendX = x;
					LastSendY = y;
					LastButton = Button;
					break;

				case MouseTrackingMode.IdMouseTrackNetTerm:
					Report = String.Format("\033}{0},{1}\r", y, x);
					ttcmn.CommBinaryOut(cv, Report, Report.Length);
					return true;

				case MouseTrackingMode.IdMouseTrackVT200Hl: /* not supported yet */
				default:
					return false;
				}
				break;

			case MouseEvent.IdMouseEventBtnUp:
				switch (MouseReportMode) {
				case MouseTrackingMode.IdMouseTrackVT200:
				case MouseTrackingMode.IdMouseTrackBtnEvent:
				case MouseTrackingMode.IdMouseTrackAllEvent:
					if (MouseReportExtMode == ExtendedMouseTrackingMode.IdMouseTrackExtSGR) {
						modifier |= 128;
					}
					else {
						Button = MouseButtons.IdButtonRelease;
					}
					Report = MakeMouseReportStr((int)MouseButtons.IdButtonRelease | modifier, x, y);
					LastSendX = x;
					LastSendY = y;
					LastButton = MouseButtons.IdButtonRelease;
					break;

				case MouseTrackingMode.IdMouseTrackX10: /* nothing to do */
				case MouseTrackingMode.IdMouseTrackNetTerm: /* nothing to do */
					return true;

				case MouseTrackingMode.IdMouseTrackVT200Hl: /* not supported yet */
				default:
					return false;
				}
				break;

			case MouseEvent.IdMouseEventMove:
				switch (MouseReportMode) {
				case MouseTrackingMode.IdMouseTrackBtnEvent:
					if ((int)LastButton == 3) {
						return false;
					}
					/* FALLTHROUGH */
					goto case MouseTrackingMode.IdMouseTrackAllEvent;
				case MouseTrackingMode.IdMouseTrackAllEvent:
					if (x == LastSendX && y == LastSendY) {
						return false;
					}
					Report = MakeMouseReportStr((int)LastButton | modifier | ((LastButton == MouseButtons.IdButtonRelease) ? 0 : 32), x, y);
					LastSendX = x;
					LastSendY = y;
					break;

				case MouseTrackingMode.IdMouseTrackVT200Hl: /* not supported yet */
				case MouseTrackingMode.IdMouseTrackX10: /* nothing to do */
				case MouseTrackingMode.IdMouseTrackVT200: /* nothing to do */
				case MouseTrackingMode.IdMouseTrackNetTerm: /* nothing to do */
				default:
					return false;
				}
				break;

			case MouseEvent.IdMouseEventWheel:
				switch (MouseReportMode) {
				case MouseTrackingMode.IdMouseTrackVT200:
				case MouseTrackingMode.IdMouseTrackBtnEvent:
				case MouseTrackingMode.IdMouseTrackAllEvent:
					Report = MakeMouseReportStr((int)Button | modifier | 64, x, y);
					break;

				case MouseTrackingMode.IdMouseTrackX10: /* nothing to do */
				case MouseTrackingMode.IdMouseTrackVT200Hl: /* not supported yet */
				case MouseTrackingMode.IdMouseTrackNetTerm: /* nothing to do */
					return false;
				}
				break;
			}

			if (Report == null)
				return false;

			SendCSIstr(Report, Report.Length);
			return true;
		}

		public void FocusReport(bool focus)
		{
			if (!FocusReportMode)
				return;

			if (focus) {
				// Focus In
				SendCSIstr("I", 0);
			}
			else {
				// Focus Out
				SendCSIstr("O", 0);
			}
		}

		void VisualBell()
		{
			CSQExchangeColor();
			Thread.Sleep(10);
			CSQExchangeColor();
		}

		void RingBell(BeepType type)
		{
			long now;

			now = DateTime.Now.Ticks;
			if (now - BeepSuppressTime < ts.BeepSuppressTime * 1000) {
				BeepSuppressTime = now;
			}
			else {
				if (now - BeepStartTime < ts.BeepOverUsedTime * 1000) {
					if (BeepOverUsedCount <= 1) {
						BeepSuppressTime = now;
					}
					else {
						BeepOverUsedCount--;
					}
				}
				else {
					BeepStartTime = now;
					BeepOverUsedCount = ts.BeepOverUsedCount;
				}

				switch (ts.Beep) {
				case BeepType.IdBeepOff:
					/* nothing to do */
					break;
				case BeepType.IdBeepOn:
					MessageBeep(0);
					break;
				case BeepType.IdBeepVisual:
					VisualBell();
					break;
				}
			}
		}

		void EndTerm()
		{
		}

		public bool BracketedPasteMode()
		{
			return BracketedPaste;
		}

		public bool WheelToCursorMode()
		{
			return AcceptWheelToCursor && keyboard.AppliCursorMode && !ts.DisableAppCursor && !(keyboard.ControlKey() && ts.DisableWheelToCursorByCtrl);
		}

		void ChangeTerminalID()
		{
			switch (ts.TerminalID) {
			case TerminalId.IdVT220J:
			case TerminalId.IdVT282:
				VTlevel = 2;
				break;
			case TerminalId.IdVT320:
			case TerminalId.IdVT382:
				VTlevel = 3;
				break;
			case TerminalId.IdVT420:
				VTlevel = 4;
				break;
			case TerminalId.IdVT520:
			case TerminalId.IdVT525:
				VTlevel = 5;
				break;
			default:
				VTlevel = 1;
				break;
			}

			if (VTlevel == 1) {
				keyboard.Send8BitMode = false;
			}
			else {
				keyboard.Send8BitMode = ts.Send8BitCtrl;
			}
		}

		internal void Init(ProgramDatas datas)
		{
			ttwinman = datas.ttwinman;
			teraprn = datas.teraprn;
			keyboard = datas.keyboard;
			ttime = datas.ttime;
			ts = datas.TTTSet;
			cv = datas.TComVar;
			Buffer = datas.Buffer;
			VTDisp = datas.VTDisp;
		}

		internal void Parse(byte[] data)
		{
			if (data.Length == 0) return;

			VTDisp.CaretOff();
			VTDisp.UpdateCaretPosition(false);  // 非アクティブの場合のみ再描画する

			ChangeEmu = 0;

			/* Get Device Context */
			VTDisp.DispInitDC();

			Buffer.LockBuffer();

			foreach (byte b in data) {
				if (keyboard.DebugFlag != KeyboardDebugFlag.DEBUG_FLAG_NONE)
					PutDebugChar(b);
				else {
					switch (ParseMode) {
					case ParsingMode.ModeFirst: ParseFirst(b); break;
					case ParsingMode.ModeESC: EscapeSequence(b); break;
					case ParsingMode.ModeDCS: DeviceControl(b); break;
					case ParsingMode.ModeDCUserKey: DCUserKey(b); break;
					case ParsingMode.ModeSOS: IgnoreString(b); break;
					case ParsingMode.ModeCSI: ControlSequence(b); break;
					case ParsingMode.ModeXS: XSequence(b); break;
					case ParsingMode.ModeDLE: DLESeen(b); break;
					case ParsingMode.ModeCAN: CANSeen(b); break;
					default:
						ParseMode = ParsingMode.ModeFirst;
						ParseFirst(b);
						break;
					}
				}
			}

			Buffer.BuffUpdateScroll();

			Buffer.BuffSetCaretWidth();
			Buffer.UnlockBuffer();

			/* release device context */
			VTDisp.DispReleaseDC();

			VTDisp.CaretOn();
		}
	}
}
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
/* IPv6 modification is Copyright(C) 2000 Jun-ya kato <kato@win6.jp> */

/* TTSET.DLL, setup file routines*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace TeraTrem
{
	class ttset
	{
		internal static void ReadIniFile(string p, TTTSet ts)
		{
			ts.Minimize = 0;
			ts.HideWindow = 0;
			ts.LogFlag = 0;         // Log flags
			ts.FTFlag = 0;              // File transfer flags
			ts.MenuFlag = 0;            // Menu flags
			ts.TermFlag = 0;            // Terminal flag
			ts.ColorFlag = 0;           // ANSI/Attribute color flags
			ts.FontFlag = 0;            // Font flag
			ts.PortFlag = 0;            // Port flags
			ts.WindowFlag = 0;          // Window flags
			ts.TelPort = 23;

			ts.DisableTCPEchoCR = false;

			/* VT terminal size  */
			ts.TerminalWidth = 80;
			ts.TerminalHeight = 24;

			/* Terminal size = Window size */
			ts.TermIsWin = true;

			/* Auto window resize flag */
			ts.AutoWinResize = true;

			/* CR Receive */
			ts.CRReceive = NewLineModes.IdCR;
			/* CR Send */
			ts.CRSend = NewLineModes.IdCR;
			ts.CRSend_ini = ts.CRSend;

			/* Local echo */
			ts.LocalEcho = false;
			ts.LocalEcho_ini = false;

			/* Answerback */
			ts.AnswerbackLen = 0;

			/* Cursor shape */
			ts.CursorShape = CursorShapes.IdBlkCur;

			/* xterm style 256 colors mode */
			ts.ColorFlag |= ColorFlags.CF_XTERM256;

			/* Enable scroll buffer */
			ts.EnableScrollBuff = 1;

			/* Scroll buffer size */
			ts.ScrollBuffMax = 10000;

			/* VT Color */
			ts.VTColor[0] = Color.Black;
			ts.VTColor[1] = Color.White;

			/* VT Bold Color */
			ts.VTBoldColor[0] = Color.Blue;
			ts.VTBoldColor[1] = Color.White;
			//ts.ColorFlag |= AnsiAttributeColorFlags.CF_BOLDCOLOR;

			/* VT Blink Color */
			ts.VTBlinkColor[0] = Color.Red;
			ts.VTBlinkColor[1] = Color.White;
			//ts.ColorFlag |= AnsiAttributeColorFlags.CF_BLINKCOLOR;

			/* VT Reverse Color */
			ts.VTReverseColor[0] = Color.White;
			ts.VTReverseColor[1] = Color.Black;
			//ts.ColorFlag |= AnsiAttributeColorFlags.CF_REVERSECOLOR;

			/* ANSI color */
			ts.ANSIColor[(int)ColorCodes.IdBack] = Color.FromArgb(0, 0, 0);
			ts.ANSIColor[(int)ColorCodes.IdRed] = Color.FromArgb(255, 0, 0);
			ts.ANSIColor[(int)ColorCodes.IdGreen] = Color.FromArgb(0, 255, 0);
			ts.ANSIColor[(int)ColorCodes.IdYellow] = Color.FromArgb(255, 255, 0);
			ts.ANSIColor[(int)ColorCodes.IdBlue] = Color.FromArgb(0, 0, 255);
			ts.ANSIColor[(int)ColorCodes.IdMagenta] = Color.FromArgb(255, 0, 255);
			ts.ANSIColor[(int)ColorCodes.IdCyan] = Color.FromArgb(0, 255, 255);
			ts.ANSIColor[(int)ColorCodes.IdFore] = Color.FromArgb(255, 255, 255);
			ts.ANSIColor[(int)ColorCodes.IdBack + 8] = Color.FromArgb(128, 128, 128);
			ts.ANSIColor[(int)ColorCodes.IdRed + 8] = Color.FromArgb(128, 0, 0);
			ts.ANSIColor[(int)ColorCodes.IdGreen + 8] = Color.FromArgb(0, 128, 0);
			ts.ANSIColor[(int)ColorCodes.IdYellow + 8] = Color.FromArgb(128, 128, 0);
			ts.ANSIColor[(int)ColorCodes.IdBlue + 8] = Color.FromArgb(0, 0, 128);
			ts.ANSIColor[(int)ColorCodes.IdMagenta + 8] = Color.FromArgb(128, 0, 128);
			ts.ANSIColor[(int)ColorCodes.IdCyan + 8] = Color.FromArgb(0, 128, 128);
			ts.ANSIColor[(int)ColorCodes.IdFore + 8] = Color.FromArgb(192, 192, 192);
			ts.ColorFlag |= ColorFlags.CF_ANSICOLOR;

			/* VT Font */
			//ts.VTFont = new Font("Terminal", 10);

			/* Bold font flag */
			ts.FontFlag |= FontFlags.FF_BOLD;

			/* BS key */
			ts.BSKey = DelId.IdBS;

			/* IME Flag  -- special option */
			ts.UseIME = true;

			/* IME-inline Flag  -- special option */
			ts.IMEInline = true;

			// フォーカス無効時のポリゴンカーソル (2008.1.24 yutaka)
			ts.KillFocusCursor = true;

			ts.MouseCursorName = Cursors.IBeam;
		}

		const string VTEditor = "VT editor keypad";
		const string VTNumeric = "VT numeric keypad";
		const string VTFunction = "VT function keys";
		const string XFunction = "X function keys";
		const string ShortCut = "Shortcut keys";

		static void GetInt(TKeyMap KeyMap, InternalKeyCodes KeyId, string Sect, string Key, string FName)
		{
			char[] Buf = new char[11];
			string Temp;
			ushort Num;

			//GetPrivateProfileString(Sect, Key, "", Buf, Buf.Length, FName);
			Temp = null;// new String(Buf);
			if (String.IsNullOrEmpty(Temp))
				Num = 0xFFFF;
			else if (String.Compare(Temp, "off", true) == 0)
				Num = 0xFFFF;
			else if (!UInt16.TryParse(Temp, out Num))
				Num = 0xFFFF;

			KeyMap.Map[(int)KeyId - 1] = Num;
		}

		internal static void ReadKeyboardCnf(string FName, TKeyMap KeyMap, bool ShowWarning)
		{
			int i, j, Ptr;
			string EntName;
			string TempStr;
			string KStr;

			// clear key map
			for (i = 0; i <= (int)InternalKeyCodes.IdKeyMax - 1; i++)
				KeyMap.Map[i] = 0xFFFF;
			for (i = 0; i <= (int)InternalKeyCodes.NumOfUserKey - 1; i++) {
				KeyMap.UserKeyPtr[i] = 0;
				KeyMap.UserKeyLen[i] = 0;
			}

			// VT editor keypad
			GetInt(KeyMap, InternalKeyCodes.IdUp, VTEditor, "Up", FName);

			GetInt(KeyMap, InternalKeyCodes.IdDown, VTEditor, "Down", FName);

			GetInt(KeyMap, InternalKeyCodes.IdRight, VTEditor, "Right", FName);

			GetInt(KeyMap, InternalKeyCodes.IdLeft, VTEditor, "Left", FName);

			GetInt(KeyMap, InternalKeyCodes.IdFind, VTEditor, "Find", FName);

			GetInt(KeyMap, InternalKeyCodes.IdInsert, VTEditor, "Insert", FName);

			GetInt(KeyMap, InternalKeyCodes.IdRemove, VTEditor, "Remove", FName);

			GetInt(KeyMap, InternalKeyCodes.IdSelect, VTEditor, "Select", FName);

			GetInt(KeyMap, InternalKeyCodes.IdPrev, VTEditor, "Prev", FName);

			GetInt(KeyMap, InternalKeyCodes.IdNext, VTEditor, "Next", FName);

			// VT numeric keypad
			GetInt(KeyMap, InternalKeyCodes.Id0, VTNumeric, "Num0", FName);

			GetInt(KeyMap, InternalKeyCodes.Id1, VTNumeric, "Num1", FName);

			GetInt(KeyMap, InternalKeyCodes.Id2, VTNumeric, "Num2", FName);

			GetInt(KeyMap, InternalKeyCodes.Id3, VTNumeric, "Num3", FName);

			GetInt(KeyMap, InternalKeyCodes.Id4, VTNumeric, "Num4", FName);

			GetInt(KeyMap, InternalKeyCodes.Id5, VTNumeric, "Num5", FName);

			GetInt(KeyMap, InternalKeyCodes.Id6, VTNumeric, "Num6", FName);

			GetInt(KeyMap, InternalKeyCodes.Id7, VTNumeric, "Num7", FName);

			GetInt(KeyMap, InternalKeyCodes.Id8, VTNumeric, "Num8", FName);

			GetInt(KeyMap, InternalKeyCodes.Id9, VTNumeric, "Num9", FName);

			GetInt(KeyMap, InternalKeyCodes.IdMinus, VTNumeric, "NumMinus", FName);

			GetInt(KeyMap, InternalKeyCodes.IdComma, VTNumeric, "NumComma", FName);

			GetInt(KeyMap, InternalKeyCodes.IdPeriod, VTNumeric, "NumPeriod", FName);

			GetInt(KeyMap, InternalKeyCodes.IdEnter, VTNumeric, "NumEnter", FName);

			GetInt(KeyMap, InternalKeyCodes.IdSlash, VTNumeric, "NumSlash", FName);

			GetInt(KeyMap, InternalKeyCodes.IdAsterisk, VTNumeric, "NumAsterisk", FName);

			GetInt(KeyMap, InternalKeyCodes.IdPlus, VTNumeric, "NumPlus", FName);

			GetInt(KeyMap, InternalKeyCodes.IdPF1, VTNumeric, "PF1", FName);

			GetInt(KeyMap, InternalKeyCodes.IdPF2, VTNumeric, "PF2", FName);

			GetInt(KeyMap, InternalKeyCodes.IdPF3, VTNumeric, "PF3", FName);

			GetInt(KeyMap, InternalKeyCodes.IdPF4, VTNumeric, "PF4", FName);

			// VT function keys
			GetInt(KeyMap, InternalKeyCodes.IdHold, VTFunction, "Hold", FName);

			GetInt(KeyMap, InternalKeyCodes.IdPrint, VTFunction, "Print", FName);

			GetInt(KeyMap, InternalKeyCodes.IdBreak, VTFunction, "Break", FName);

			GetInt(KeyMap, InternalKeyCodes.IdF6, VTFunction, "F6", FName);

			GetInt(KeyMap, InternalKeyCodes.IdF7, VTFunction, "F7", FName);

			GetInt(KeyMap, InternalKeyCodes.IdF8, VTFunction, "F8", FName);

			GetInt(KeyMap, InternalKeyCodes.IdF9, VTFunction, "F9", FName);

			GetInt(KeyMap, InternalKeyCodes.IdF10, VTFunction, "F10", FName);

			GetInt(KeyMap, InternalKeyCodes.IdF11, VTFunction, "F11", FName);

			GetInt(KeyMap, InternalKeyCodes.IdF12, VTFunction, "F12", FName);

			GetInt(KeyMap, InternalKeyCodes.IdF13, VTFunction, "F13", FName);

			GetInt(KeyMap, InternalKeyCodes.IdF14, VTFunction, "F14", FName);

			GetInt(KeyMap, InternalKeyCodes.IdHelp, VTFunction, "Help", FName);

			GetInt(KeyMap, InternalKeyCodes.IdDo, VTFunction, "Do", FName);

			GetInt(KeyMap, InternalKeyCodes.IdF17, VTFunction, "F17", FName);

			GetInt(KeyMap, InternalKeyCodes.IdF18, VTFunction, "F18", FName);

			GetInt(KeyMap, InternalKeyCodes.IdF19, VTFunction, "F19", FName);

			GetInt(KeyMap, InternalKeyCodes.IdF20, VTFunction, "F20", FName);

			// UDK
			GetInt(KeyMap, InternalKeyCodes.IdUDK6, VTFunction, "UDK6", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK7, VTFunction, "UDK7", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK8, VTFunction, "UDK8", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK9, VTFunction, "UDK9", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK10, VTFunction, "UDK10", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK11, VTFunction, "UDK11", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK12, VTFunction, "UDK12", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK13, VTFunction, "UDK13", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK14, VTFunction, "UDK14", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK15, VTFunction, "UDK15", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK16, VTFunction, "UDK16", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK17, VTFunction, "UDK17", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK18, VTFunction, "UDK18", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK19, VTFunction, "UDK19", FName);

			GetInt(KeyMap, InternalKeyCodes.IdUDK20, VTFunction, "UDK20", FName);

			// XTERM function keys
			GetInt(KeyMap, InternalKeyCodes.IdXF1, XFunction, "XF1", FName);

			GetInt(KeyMap, InternalKeyCodes.IdXF2, XFunction, "XF2", FName);

			GetInt(KeyMap, InternalKeyCodes.IdXF3, XFunction, "XF3", FName);

			GetInt(KeyMap, InternalKeyCodes.IdXF4, XFunction, "XF4", FName);

			GetInt(KeyMap, InternalKeyCodes.IdXF5, XFunction, "XF5", FName);

			// accelerator keys
			GetInt(KeyMap, InternalKeyCodes.IdCmdEditCopy, ShortCut, "EditCopy", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdEditPaste, ShortCut, "EditPaste", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdEditPasteCR, ShortCut, "EditPasteCR", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdEditCLS, ShortCut, "EditCLS", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdEditCLB, ShortCut, "EditCLB", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdCtrlOpenTEK, ShortCut, "ControlOpenTEK", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdCtrlCloseTEK, ShortCut, "ControlCloseTEK", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdLineUp, ShortCut, "LineUp", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdLineDown, ShortCut, "LineDown", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdPageUp, ShortCut, "PageUp", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdPageDown, ShortCut, "PageDown", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdBuffTop, ShortCut, "BuffTop", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdBuffBottom, ShortCut, "BuffBottom", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdNextWin, ShortCut, "NextWin", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdPrevWin, ShortCut, "PrevWin", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdNextSWin, ShortCut, "NextShownWin", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdPrevSWin, ShortCut, "PrevShownWin", FName);

			GetInt(KeyMap, InternalKeyCodes.IdCmdLocalEcho, ShortCut, "LocalEcho", FName);

			GetInt(KeyMap, InternalKeyCodes.IdScrollLock, ShortCut, "ScrollLock", FName);

			/* user keys */

			Ptr = 0;

			i = (int)InternalKeyCodes.IdUser1;
			do {
				EntName = String.Format("User{0}", i - InternalKeyCodes.IdUser1 + 1);
				//GetPrivateProfileString("User keys", EntName, "", TempStr, TempStr.Length, FName);
				TempStr = "";
				if (TempStr.Length > 0) {
					/* scan code */
					ttlib.GetNthString(TempStr, 1, out KStr);
					if (String.Compare(KStr, "off", true) == 0)
						KeyMap.Map[i - 1] = 0xFFFF;
					else {
						ttlib.GetNthNum(TempStr, 1, out j);
						KeyMap.Map[i - 1] = (ushort)j;
					}
					/* conversion flag */
					ttlib.GetNthNum(TempStr, 2, out j);
					KeyMap.UserKeyType[i - (int)InternalKeyCodes.IdUser1] = (byte)j;
					/* key string */
					/*	GetNthString(TempStr,3,KStr.Length,KStr); */
					KeyMap.UserKeyPtr[i - (int)InternalKeyCodes.IdUser1] = Ptr;
					/*	KeyMap.UserKeyLen[i-TeraTermInternalKeyCodes.IdUser1] =
						Hex2Str(KStr,&(KeyMap.UserKeyStr[Ptr]),KeyStrMax-Ptr+1);
					*/
					ttlib.GetNthString(TempStr, 3, out KeyMap.UserKeyStr[Ptr]);
					KeyMap.UserKeyLen[i - (int)InternalKeyCodes.IdUser1] =
						KeyMap.UserKeyStr[Ptr].Length;
					Ptr = Ptr + KeyMap.UserKeyLen[i - (int)InternalKeyCodes.IdUser1];
				}

				i++;
			}
			while ((i <= (int)InternalKeyCodes.IdKeyMax) && (TempStr.Length > 0) && (Ptr <= (int)InternalKeyCodes.KeyStrMax));

			for (j = 1; j <= (int)InternalKeyCodes.IdKeyMax - 1; j++)
				if (KeyMap.Map[j] != 0xFFFF)
					for (i = 0; i <= j - 1; i++)
						if (KeyMap.Map[i] == KeyMap.Map[j]) {
							if (ShowWarning) {
								TempStr = String.Format("Keycode {0} is used more than once", KeyMap.Map[j]);
								MessageBox.Show(TempStr,
										   "Tera Term: Error in keyboard setup file",
										   MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							}
							KeyMap.Map[i] = 0xFFFF;
						}
		}
	}
}

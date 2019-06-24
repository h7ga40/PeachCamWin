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

/* TERATERM.EXE, TELNET routines */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;

namespace TeraTrem
{
	class ttcmn
	{
		const int CW_USEDEFAULT = unchecked((int)0x80000000);
		static bool FirstInstance = true;
		static TMap pm = new TMap();

		internal static bool StartTeraTerm(TTTSet ts)
		{
			string Temp;

			if (FirstInstance) {
				// init window list
				pm.NWin = 0;
			}
			else {
				/* only the first instance uses saved position */
				pm.ts.VTPos.X = CW_USEDEFAULT;
				pm.ts.VTPos.Y = CW_USEDEFAULT;
				pm.ts.TEKPos.X = CW_USEDEFAULT;
				pm.ts.TEKPos.Y = CW_USEDEFAULT;
			}

			pm.ts.CopyTo(ts);

			// if (FirstInstance) { の部分から移動 (2008.3.13 maya)
			// 起動時には、共有メモリの HomeDir と SetupFName は空になる
			/* Get home directory */
			ts.HomeDir = Path.GetDirectoryName(Application.ExecutablePath);
			Environment.CurrentDirectory = ts.HomeDir;

			if (FirstInstance) {
				FirstInstance = false;
				return true;
			}
			else {
				return false;
			}
		}

		internal static void ChangeDefaultSet(TTTSet ts, TKeyMap km)
		{
			if ((ts != null) &&
				(String.Compare(ts.SetupFName, pm.ts.SetupFName, true) == 0)) {
				ts.CopyTo(pm.ts);
			}
			if (km != null) {
				km.CopyTo(pm.km);
			}
		}

		internal static InternalKeyCodes GetKeyCode(TKeyMap KeyMap, ushort Scan)
		{
			InternalKeyCodes Key;

			if (KeyMap == null) {
				KeyMap = pm.km;
			}
			Key = InternalKeyCodes.IdKeyMax;
			while ((Key > 0) && (KeyMap.Map[(int)Key - 1] != Scan)) {
				Key--;
			}
			return Key;
		}

		internal static void GetKeyStr(Control HWin, TKeyMap KeyMap, InternalKeyCodes KeyCode,
			bool AppliKeyMode, bool AppliCursorMode, bool Send8BitMode, byte[] KeyStr, int destlen,
			ref int Len, ref keyTypeIds Type)
		{
			MSG Msg = new MSG();
			char[] Temp = new char[201];

			if (KeyMap == null) {
				KeyMap = new TKeyMap();
				pm.km.CopyTo(KeyMap);
			}

			Type = keyTypeIds.IdBinary;  // key type
			Len = 0;
			switch (KeyCode) {
			case InternalKeyCodes.IdUp:
				if (Send8BitMode) {
					Len = 2;
					if (AppliCursorMode)
						strncpy_s(KeyStr, destlen, "\xb9A");
					else
						strncpy_s(KeyStr, destlen, "\x9bA");
				}
				else {
					Len = 3;
					if (AppliCursorMode)
						strncpy_s(KeyStr, destlen, "\x1bOA");
					else
						strncpy_s(KeyStr, destlen, "\x1b[A");
				}
				break;
			case InternalKeyCodes.IdDown:
				if (Send8BitMode) {
					Len = 2;
					if (AppliCursorMode)
						strncpy_s(KeyStr, destlen, "\xb9B");
					else
						strncpy_s(KeyStr, destlen, "\x9bB");
				}
				else {
					Len = 3;
					if (AppliCursorMode)
						strncpy_s(KeyStr, destlen, "\x1bOB");
					else
						strncpy_s(KeyStr, destlen, "\x1b[B");
				}
				break;
			case InternalKeyCodes.IdRight:
				if (Send8BitMode) {
					Len = 2;
					if (AppliCursorMode)
						strncpy_s(KeyStr, destlen, "\xb9C");
					else
						strncpy_s(KeyStr, destlen, "\x9bC");
				}
				else {
					Len = 3;
					if (AppliCursorMode)
						strncpy_s(KeyStr, destlen, "\x1bOC");
					else
						strncpy_s(KeyStr, destlen, "\x1b[C");
				}
				break;
			case InternalKeyCodes.IdLeft:
				if (Send8BitMode) {
					Len = 2;
					if (AppliCursorMode)
						strncpy_s(KeyStr, destlen, "\xb9D");
					else
						strncpy_s(KeyStr, destlen, "\x9bD");
				}
				else {
					Len = 3;
					if (AppliCursorMode)
						strncpy_s(KeyStr, destlen, "\x1bOD");
					else
						strncpy_s(KeyStr, destlen, "\x1b[D");
				}
				break;
			case InternalKeyCodes.Id0:
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9p");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOp");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'0';
				}
				break;
			case InternalKeyCodes.Id1:
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9q");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOq");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'1';
				}
				break;
			case InternalKeyCodes.Id2:
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9r");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOr");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'2';
				}
				break;
			case InternalKeyCodes.Id3:
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9s");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOs");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'3';
				}
				break;
			case InternalKeyCodes.Id4:
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9t");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOt");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'4';
				}
				break;
			case InternalKeyCodes.Id5:
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9u");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOu");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'5';
				}
				break;
			case InternalKeyCodes.Id6:
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9v");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOv");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'6';
				}
				break;
			case InternalKeyCodes.Id7:
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9w");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOw");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'7';
				}
				break;
			case InternalKeyCodes.Id8:
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9x");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOx");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'8';
				}
				break;
			case InternalKeyCodes.Id9:
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9y");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOy");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'9';
				}
				break;
			case InternalKeyCodes.IdMinus: /* numeric pad - key (DEC) */
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9m");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOm");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'-';
				}
				break;
			case InternalKeyCodes.IdComma: /* numeric pad , key (DEC) */
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9l");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOl");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)',';
				}
				break;
			case InternalKeyCodes.IdPeriod: /* numeric pad . key */
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9n");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOn");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'.';
				}
				break;
			case InternalKeyCodes.IdEnter: /* numeric pad enter key */
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9M");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOM");
					}
				}
				else {
					Type = keyTypeIds.IdText; // do new-line conversion
					Len = 1;
					KeyStr[0] = 0x0D;
				}
				break;
			case InternalKeyCodes.IdSlash: /* numeric pad slash key */
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9o");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOo");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'/';
				}
				break;
			case InternalKeyCodes.IdAsterisk: /* numeric pad asterisk key */
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9j");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOj");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'*';
				}
				break;
			case InternalKeyCodes.IdPlus: /* numeric pad plus key */
				if (AppliKeyMode) {
					if (Send8BitMode) {
						Len = 2;
						strncpy_s(KeyStr, destlen, "\xb9k");
					}
					else {
						Len = 3;
						strncpy_s(KeyStr, destlen, "\x1bOk");
					}
				}
				else {
					Len = 1;
					KeyStr[0] = (byte)'+';
				}
				break;
			case InternalKeyCodes.IdPF1: /* DEC Key: PF1 */
				if (Send8BitMode) {
					Len = 2;
					strncpy_s(KeyStr, destlen, "\xb9P");
				}
				else {
					Len = 3;
					strncpy_s(KeyStr, destlen, "\x1bOP");
				}
				break;
			case InternalKeyCodes.IdPF2: /* DEC Key: PF2 */
				if (Send8BitMode) {
					Len = 2;
					strncpy_s(KeyStr, destlen, "\xb9Q");
				}
				else {
					Len = 3;
					strncpy_s(KeyStr, destlen, "\x1bOQ");
				}
				break;
			case InternalKeyCodes.IdPF3: /* DEC Key: PF3 */
				if (Send8BitMode) {
					Len = 2;
					strncpy_s(KeyStr, destlen, "\xb9R");
				}
				else {
					Len = 3;
					strncpy_s(KeyStr, destlen, "\x1bOR");
				}
				break;
			case InternalKeyCodes.IdPF4: /* DEC Key: PF4 */
				if (Send8BitMode) {
					Len = 2;
					strncpy_s(KeyStr, destlen, "\xb9S");
				}
				else {
					Len = 3;
					strncpy_s(KeyStr, destlen, "\x1bOS");
				}
				break;
			case InternalKeyCodes.IdFind: /* DEC Key: Find */
				if (Send8BitMode) {
					Len = 3;
					strncpy_s(KeyStr, destlen, "\x9b1~");
				}
				else {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x1b[1~");
				}
				break;
			case InternalKeyCodes.IdInsert: /* DEC Key: Insert Here */
				if (Send8BitMode) {
					Len = 3;
					strncpy_s(KeyStr, destlen, "\x9b2~");
				}
				else {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x1b[2~");
				}
				break;
			case InternalKeyCodes.IdRemove: /* DEC Key: Remove */
				if (Send8BitMode) {
					Len = 3;
					strncpy_s(KeyStr, destlen, "\x9b3~");
				}
				else {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x1b[3~");
				}
				break;
			case InternalKeyCodes.IdSelect: /* DEC Key: Select */
				if (Send8BitMode) {
					Len = 3;
					strncpy_s(KeyStr, destlen, "\x9b4~");
				}
				else {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x1b[4~");
				}
				break;
			case InternalKeyCodes.IdPrev: /* DEC Key: Prev */
				if (Send8BitMode) {
					Len = 3;
					strncpy_s(KeyStr, destlen, "\x9b5~");
				}
				else {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x1b[5~");
				}
				break;
			case InternalKeyCodes.IdNext: /* DEC Key: Next */
				if (Send8BitMode) {
					Len = 3;
					strncpy_s(KeyStr, destlen, "\x9b6~");
				}
				else {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x1b[6~");
				}
				break;
			case InternalKeyCodes.IdF6: /* DEC Key: F6 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b17~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[17~");
				}
				break;
			case InternalKeyCodes.IdF7: /* DEC Key: F7 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b18~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[18~");
				}
				break;
			case InternalKeyCodes.IdF8: /* DEC Key: F8 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b19~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[19~");
				}
				break;
			case InternalKeyCodes.IdF9: /* DEC Key: F9 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b20~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[20~");
				}
				break;
			case InternalKeyCodes.IdF10: /* DEC Key: F10 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b21~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[21~");
				}
				break;
			case InternalKeyCodes.IdF11: /* DEC Key: F11 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b23~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[23~");
				}
				break;
			case InternalKeyCodes.IdF12: /* DEC Key: F12 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b24~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[24~");
				}
				break;
			case InternalKeyCodes.IdF13: /* DEC Key: F13 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b25~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[25~");
				}
				break;
			case InternalKeyCodes.IdF14: /* DEC Key: F14 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b26~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[26~");
				}
				break;
			case InternalKeyCodes.IdHelp: /* DEC Key: Help */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b28~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[28~");
				}
				break;
			case InternalKeyCodes.IdDo: /* DEC Key: Do */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b29~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[29~");
				}
				break;
			case InternalKeyCodes.IdF17: /* DEC Key: F17 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b31~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[31~");
				}
				break;
			case InternalKeyCodes.IdF18: /* DEC Key: F18 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b32~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[32~");
				}
				break;
			case InternalKeyCodes.IdF19: /* DEC Key: F19 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b33~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[33~");
				}
				break;
			case InternalKeyCodes.IdF20: /* DEC Key: F20 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b34~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[34~");
				}
				break;
			case InternalKeyCodes.IdXF1: /* XTERM F1 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b11~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[11~");
				}
				break;
			case InternalKeyCodes.IdXF2: /* XTERM F2 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b12~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[12~");
				}
				break;
			case InternalKeyCodes.IdXF3: /* XTERM F3 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b13~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[13~");
				}
				break;
			case InternalKeyCodes.IdXF4: /* XTERM F4 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b14~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[14~");
				}
				break;
			case InternalKeyCodes.IdXF5: /* XTERM F5 */
				if (Send8BitMode) {
					Len = 4;
					strncpy_s(KeyStr, destlen, "\x9b15~");
				}
				else {
					Len = 5;
					strncpy_s(KeyStr, destlen, "\x1b[15~");
				}
				break;
			case InternalKeyCodes.IdHold:
			case InternalKeyCodes.IdPrint:
			case InternalKeyCodes.IdBreak:
			case InternalKeyCodes.IdCmdEditCopy:
			case InternalKeyCodes.IdCmdEditPaste:
			case InternalKeyCodes.IdCmdEditPasteCR:
			case InternalKeyCodes.IdCmdEditCLS:
			case InternalKeyCodes.IdCmdEditCLB:
			case InternalKeyCodes.IdCmdCtrlOpenTEK:
			case InternalKeyCodes.IdCmdCtrlCloseTEK:
			case InternalKeyCodes.IdCmdLineUp:
			case InternalKeyCodes.IdCmdLineDown:
			case InternalKeyCodes.IdCmdPageUp:
			case InternalKeyCodes.IdCmdPageDown:
			case InternalKeyCodes.IdCmdBuffTop:
			case InternalKeyCodes.IdCmdBuffBottom:
			case InternalKeyCodes.IdCmdNextWin:
			case InternalKeyCodes.IdCmdPrevWin:
			case InternalKeyCodes.IdCmdNextSWin:
			case InternalKeyCodes.IdCmdPrevSWin:
			case InternalKeyCodes.IdCmdLocalEcho:
			case InternalKeyCodes.IdScrollLock:
				VTWindow.PostMessage(HWin.Handle, tttypes.WM_USER_ACCELCOMMAND, new IntPtr((int)KeyCode), IntPtr.Zero);
				break;
			default:
				if ((KeyCode >= InternalKeyCodes.IdUser1) && (KeyCode <= InternalKeyCodes.IdKeyMax)) {
					Type = (keyTypeIds)KeyMap.UserKeyType[KeyCode - InternalKeyCodes.IdUser1]; // key type
					Len = KeyMap.UserKeyLen[KeyCode - InternalKeyCodes.IdUser1];
					Temp = KeyMap.UserKeyStr[KeyMap.UserKeyPtr[KeyCode - InternalKeyCodes.IdUser1]].ToCharArray();
					Temp[Len] = '\0';
					if ((Type == keyTypeIds.IdBinary) || (Type == keyTypeIds.IdText))
						Len = ttlib.Hex2Str(Temp, KeyStr, destlen);
					else
						strncpy_s(KeyStr, destlen, new String(Temp));
				}
				else
					return;
				break;
			}
			/* remove WM_CHAR message for used keycode */
			keyboard.PeekMessage(ref Msg, HWin.Handle, VTWindow.WM_CHAR, VTWindow.WM_CHAR, keyboard.PM_REMOVE);
		}

		private static void strncpy_s(byte[] KeyStr, int destlen, string p)
		{
			int i = 0;

			foreach (char c in p) {
				KeyStr[i] = (byte)c;
				i++;
				if (i > destlen)
					break;
			}
		}

		private static int CommReadRawByte(TComVar cv, ref byte b)
		{
			if (!cv.Ready) {
				return 0;
			}

			if (cv.InBuffCount > 0) {
				b = cv.InBuff[cv.InPtr];
				cv.InPtr++;
				cv.InBuffCount--;
				if (cv.InBuffCount == 0) {
					cv.InPtr = 0;
				}
				return 1;
			}
			else {
				cv.InPtr = 0;
				return 0;
			}
		}

		public static void CommInsert1Byte(TComVar cv, byte b)
		{
			if (!cv.Ready) {
				return;
			}

			if (cv.InPtr == 0) {
				System.Buffer.BlockCopy(cv.InBuff, 0, cv.InBuff, 1, cv.InBuffCount);
			}
			else {
				cv.InPtr--;
			}
			cv.InBuff[cv.InPtr] = b;
			cv.InBuffCount++;

			if (cv.HBinBuf != IntPtr.Zero) {
				cv.BinSkip++;
			}
		}

		private static void Log1Bin(TComVar cv, byte b)
		{
			if (((cv.FilePause & /*ttftypes.OpLog*/1) != 0) || cv.ProtoFlag) {
				return;
			}
			if (cv.BinSkip > 0) {
				cv.BinSkip--;
				return;
			}
			cv.BinBuf[cv.BinPtr] = b;
			cv.BinPtr++;
			if (cv.BinPtr >= tttypes.InBuffSize) {
				cv.BinPtr = cv.BinPtr - tttypes.InBuffSize;
			}
			if (cv.BCount >= tttypes.InBuffSize) {
				cv.BCount = tttypes.InBuffSize;
				cv.BStart = cv.BinPtr;
			}
			else {
				cv.BCount++;
			}
		}

		public static int CommRead1Byte(TComVar cv, out byte b)
		{
			int c;

			b = 0;

			if (!cv.Ready) {
				return 0;
			}

			if ((cv.HLogBuf != IntPtr.Zero) &&
				((cv.LCount >= tttypes.InBuffSize - 10) ||
				 (cv.DCount >= tttypes.InBuffSize - 10))) {
				// 自分のバッファに余裕がない場合は、CPUスケジューリングを他に回し、
				// CPUがストールするの防ぐ。
				// (2006.10.13 yutaka)
				Thread.Sleep(1);
				return 0;
			}

			if ((cv.HBinBuf != IntPtr.Zero) &&
				(cv.BCount >= tttypes.InBuffSize - 10)) {
				return 0;
			}

			if (cv.TelMode) {
				c = 0;
			}
			else {
				c = CommReadRawByte(cv, ref b);
			}

			if ((c == 1) && cv.TelCRFlag) {
				cv.TelCRFlag = false;
				if (b == 0) {
					c = 0;
				}
			}

			if (c == 1) {
				if (cv.IACFlag) {
					cv.IACFlag = false;
					if (b != 0xFF) {
						cv.TelMode = true;
						CommInsert1Byte(cv, b);
						if (cv.HBinBuf != IntPtr.Zero) {
							cv.BinSkip--;
						}
						c = 0;
					}
				}
				else if ((cv.PortType == PortTypeId.IdTCPIP) && (b == 0xFF)) {
					if (!cv.TelFlag && cv.TelAutoDetect) { /* TTPLUG */
						cv.TelFlag = true;
					}
					if (cv.TelFlag) {
						cv.IACFlag = true;
						c = 0;
					}
				}
				else if (cv.TelFlag && !cv.TelBinRecv && (b == 0x0D)) {
					cv.TelCRFlag = true;
				}
			}

			if ((c == 1) && (cv.HBinBuf != IntPtr.Zero)) {
				Log1Bin(cv, b);
			}

			return c;
		}

		private static int CommRawOut(TComVar cv, char[] B, int C)
		{
			int a;

			if (!cv.Ready) {
				return C;
			}

			if (C > tttypes.OutBuffSize - cv.OutBuffCount) {
				a = tttypes.OutBuffSize - cv.OutBuffCount;
			}
			else {
				a = C;
			}
			if (cv.OutPtr > 0) {
				System.Buffer.BlockCopy(cv.OutBuff, cv.OutPtr, cv.OutBuff, 0, cv.OutBuffCount);
				cv.OutPtr = 0;
			}
			System.Buffer.BlockCopy(B, 0, cv.OutBuff, cv.OutBuffCount, a);
			cv.OutBuffCount = cv.OutBuffCount + a;
			return a;
		}

		public static int CommBinaryOut(TComVar cv, string B, int C)
		{
			int a, i, Len;
			char[] d = new char[3];

			if (!cv.Ready) {
				return C;
			}

			i = 0;
			a = 1;
			while ((a > 0) && (i < C)) {
				Len = 0;

				d[Len] = B[i];
				Len++;

				if (cv.TelFlag && (B[i] == '\x0d') && !cv.TelBinSend) {
					d[Len++] = '\x00';
				}
				else if (cv.TelFlag && (B[i] == '\xff')) {
					d[Len++] = '\xff';
				}

				if (tttypes.OutBuffSize - cv.OutBuffCount - Len >= 0) {
					CommRawOut(cv, d, Len);
					a = 1;
				}
				else {
					a = 0;
				}

				i += a;
			}
			return i;
		}

		internal static void CommBinaryBuffOut(TComVar tComVar, byte[] Code, int CodeLength)
		{
			byte[] data = new byte[CodeLength];
			System.Buffer.BlockCopy(Code, 0, data, 0, data.Length);
			DoDataReceive(data);
		}

		internal static void CommBinaryEcho(TComVar tComVar, byte[] Code, int CodeLength)
		{
			byte[] data = new byte[CodeLength];
			System.Buffer.BlockCopy(Code, 0, data, 0, data.Length);
			DoDataReceive(data);
		}

		internal static void CommTextOut(TComVar tComVar, byte[] Code, int CodeLength)
		{
			byte[] data = new byte[CodeLength];
			System.Buffer.BlockCopy(Code, 0, data, 0, data.Length);
			DoDataReceive(data);
		}

		internal static void CommTextEcho(TComVar tComVar, byte[] Code, int CodeLength)
		{
			byte[] data = new byte[CodeLength];
			System.Buffer.BlockCopy(Code, 0, data, 0, data.Length);
			DoDataReceive(data);
		}

		public static event DataReceiveEventHandlear DataReceive;
		public static object DataReceiveSender;

		public static void DoDataReceive(byte[] data)
		{
			if (DataReceive != null)
				DataReceive(DataReceiveSender, data);
		}

		public static void NotifyMessage(TComVar cv, string message, string title, int flag)
		{
			throw new NotImplementedException();
		}

		public static void NotifyInfoMessage(TComVar cv, string message, string title)
		{
			NotifyMessage(cv, message, title, 1);
		}

		public static void NotifyWarnMessage(TComVar cv, string message, string title)
		{
			NotifyMessage(cv, message, title, 2);
		}

		public static void NotifyErrorMessage(TComVar cv, string message, string title)
		{
			NotifyMessage(cv, message, title, 3);
		}
	}

	public delegate void DataReceiveEventHandlear(object sender, byte[] data);
}

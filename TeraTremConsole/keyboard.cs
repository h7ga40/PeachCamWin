/*
 * Copyright (C) 1994-1998 T. Teranishi
 * (C) 2006-2017 TeraTerm Project
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

/* TERATERM.EXE, keyboard routines */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

namespace TeraTrem
{
	/* KeyDown return type */
	enum KeyDownReturnType
	{
		KEYDOWN_COMMOUT = 1,    /* リモートに送信（BS Enter Spaceなど） */
		KEYDOWN_CONTROL = 2,    /* Ctrl,Shiftなど */
		KEYDOWN_OTHER = 0,  /* その他 */
	}

	enum KeyboardDebugFlag
	{
		DEBUG_FLAG_NONE = 0,
		DEBUG_FLAG_NORM = 1,
		DEBUG_FLAG_HEXD = 2,
		DEBUG_FLAG_NOUT = 3,
		DEBUG_FLAG_MAXD = 4,
	}

	class keyboard
	{
		TTTSet ts;
		TComVar cv;
		ttwinman ttwinman;

		public const int FuncKeyStrMax = 32;

		public bool AutoRepeatMode;
		public bool AppliKeyMode, AppliCursorMode;
		public int AppliEscapeMode;
		public bool Send8BitMode;
		public KeyboardDebugFlag DebugFlag = KeyboardDebugFlag.DEBUG_FLAG_NONE;

		byte[] FuncKeyStr = new byte[(int)InternalKeyCodes.NumOfUDK * FuncKeyStrMax];
		int[] FuncKeyLen = new int[(int)InternalKeyCodes.NumOfUDK];

		/*keyboard status*/
		Keys PreviousKey;

		/*key code map*/
		TKeyMap KeyMap = null;

		// Ctrl-\ support for NEC-PC98
		Keys VKBackslash;

#if !VK_PROCESSKEY
		const int VK_PROCESSKEY = 0xE5;
#endif

		public void SetKeyMap()
		{
			string TempDir;
			string TempName;

#if SHARED_KEYMAP
			if (String.IsNullOrEmpty(ts.KeyCnfFN)) return;
#else
			/*
			if (String.IsNullOrEmpty(ts.KeyCnfFN)) {
				if ( KeyMap != null ) {
					return;
				}
				ts.KeyCnfFN = "KEYBOARD.CNF";
			}
			*/
#endif
			TempName = Path.GetFileName(ts.KeyCnfFN);
			TempDir = Path.GetDirectoryName(ts.KeyCnfFN);
			if (String.IsNullOrEmpty(TempDir))
				TempDir = ts.HomeDir;
			TempName = Path.ChangeExtension(TempName, ".CNF");

			ts.KeyCnfFN = Path.Combine(TempDir, TempName);

			if (KeyMap == null)
				KeyMap = new TKeyMap();
			if (KeyMap != null) {
				ttset.ReadKeyboardCnf(ts.KeyCnfFN, KeyMap, true);
				//FreeTTSET();
			}
#if SHARED_KEYMAP
			if ((TempDir == ts.HomeDir) &&
				(TempName == "KEYBOARD.CNF")) {
				ttcmn.ChangeDefaultSet(null, KeyMap);
				KeyMap = null;
			}
#endif
		}

		public void ClearUserKey()
		{
			int i;

			i = 0;
			while (i < (int)InternalKeyCodes.NumOfUDK)
				FuncKeyLen[i++] = 0;
		}

		public void DefineUserKey(int NewKeyId, byte[] NewKeyStr, int NewKeyLen)
		{
			if ((NewKeyLen == 0) || (NewKeyLen > keyboard.FuncKeyStrMax)) return;

			if ((NewKeyId >= 17) && (NewKeyId <= 21))
				NewKeyId = NewKeyId - 17;
			else if ((NewKeyId >= 23) && (NewKeyId <= 26))
				NewKeyId = NewKeyId - 18;
			else if ((NewKeyId >= 28) && (NewKeyId <= 29))
				NewKeyId = NewKeyId - 19;
			else if ((NewKeyId >= 31) && (NewKeyId <= 34))
				NewKeyId = NewKeyId - 20;
			else
				return;

			System.Buffer.BlockCopy(NewKeyStr, 0, FuncKeyStr, NewKeyId * FuncKeyStrMax, NewKeyLen);
			FuncKeyLen[NewKeyId] = NewKeyLen;
		}

		int VKey2KeyStr(Keys VKey, Control HWin, byte[] Code, int CodeSize, ref keyTypeIds CodeType, ushort ModStat)
		{
			bool Single, Control, Shift;
			int CodeLength = 0;

			Single = false;
			Shift = false;
			Control = false;
			switch (ModStat) {
			case 0: Single = true; break;
			case 2: Shift = true; break;
			case 4: Control = true; break;
			}

			switch (VKey) {
			case Keys.Back:
				if (Control) {
					CodeLength = 1;
					if (ts.BSKey == DelId.IdDEL)
						Code[0] = 0x08;
					else
						Code[0] = 0x7F;
				}
				else if (Single) {
					CodeLength = 1;
					if (ts.BSKey == DelId.IdDEL)
						Code[0] = 0x7F;
					else
						Code[0] = 0x08;
				}
				break;
			case Keys.Return: /* CR Key */
				if (Single) {
					CodeType = keyTypeIds.IdText; // do new-line conversion
					CodeLength = 1;
					Code[0] = 0x0D;
				}
				break;
			case Keys.Escape: // Escape Key
				if (Single) {
					switch (AppliEscapeMode) {
					case 1:
						CodeLength = 3;
						Code[0] = 0x1B;
						Code[1] = (byte)'O';
						Code[2] = (byte)'[';
						break;
					case 2:
						CodeLength = 2;
						Code[0] = 0x1B;
						Code[1] = 0x1B;
						break;
					case 3:
						CodeLength = 2;
						Code[0] = 0x1B;
						Code[1] = 0x00;
						break;
					case 4:
						CodeLength = 8;
						Code[0] = 0x1B;
						Code[1] = 0x1B;
						Code[2] = (byte)'[';
						Code[3] = (byte)'=';
						Code[4] = (byte)'2';
						Code[5] = (byte)'7';
						Code[6] = (byte)'%';
						Code[7] = (byte)'~';
						break;
					}
				}
				break;
			case Keys.Space:
				if (Control) { // Ctrl-Space -> NUL
					CodeLength = 1;
					Code[0] = 0;
				}
				break;
			case Keys.Delete:
				if (Single) {
					if (ts.DelKey > 0) { // DEL character
						CodeLength = 1;
						Code[0] = 0x7f;
					}
					else if (!ts.StrictKeyMapping) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdRemove,
								  AppliKeyMode && !ts.DisableAppKeypad,
								  AppliCursorMode && !ts.DisableAppCursor,
								  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
				}
				break;
			case Keys.Up:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdUp,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.Down:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdDown,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.Right:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdRight,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.Left:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdLeft,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.Insert:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdInsert,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.Home:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdFind,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.End:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdSelect,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.Prior:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdPrev,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.Next:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdNext,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.F1:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdXF1,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.F2:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdXF2,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.F3:
				if (!ts.StrictKeyMapping) {
					if (Single) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdXF3,
								  AppliKeyMode && !ts.DisableAppKeypad,
								  AppliCursorMode && !ts.DisableAppCursor,
								  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
					else if (Shift) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF13,
								  AppliKeyMode && !ts.DisableAppKeypad,
								  AppliCursorMode && !ts.DisableAppCursor,
								  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
				}
				break;
			case Keys.F4:
				if (!ts.StrictKeyMapping) {
					if (Single) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdXF4,
								  AppliKeyMode && !ts.DisableAppKeypad,
								  AppliCursorMode && !ts.DisableAppCursor,
								  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
					else if (Shift) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF14,
								  AppliKeyMode && !ts.DisableAppKeypad,
								  AppliCursorMode && !ts.DisableAppCursor,
								  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
				}
				break;
			case Keys.F5:
				if (!ts.StrictKeyMapping) {
					if (Single) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdXF5,
								AppliKeyMode && !ts.DisableAppKeypad,
								AppliCursorMode && !ts.DisableAppCursor,
								Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
					else if (Shift) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdHelp,
								  AppliKeyMode && !ts.DisableAppKeypad,
								  AppliCursorMode && !ts.DisableAppCursor,
								  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
				}
				break;
			case Keys.F6:
				if (!ts.StrictKeyMapping) {
					if (Single) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF6,
								AppliKeyMode && !ts.DisableAppKeypad,
								AppliCursorMode && !ts.DisableAppCursor,
								Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
					else if (Shift) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdDo,
								  AppliKeyMode && !ts.DisableAppKeypad,
								  AppliCursorMode && !ts.DisableAppCursor,
								  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
				}
				break;
			case Keys.F7:
				if (!ts.StrictKeyMapping) {
					if (Single) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF7,
								AppliKeyMode && !ts.DisableAppKeypad,
								AppliCursorMode && !ts.DisableAppCursor,
								Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
					else if (Shift) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF17,
								  AppliKeyMode && !ts.DisableAppKeypad,
								  AppliCursorMode && !ts.DisableAppCursor,
								  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
				}
				break;
			case Keys.F8:
				if (!ts.StrictKeyMapping) {
					if (Single) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF8,
								AppliKeyMode && !ts.DisableAppKeypad,
								AppliCursorMode && !ts.DisableAppCursor,
								Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
					else if (Shift) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF18,
								  AppliKeyMode && !ts.DisableAppKeypad,
								  AppliCursorMode && !ts.DisableAppCursor,
								  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
				}
				break;
			case Keys.F9:
				if (!ts.StrictKeyMapping) {
					if (Single) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF9,
								AppliKeyMode && !ts.DisableAppKeypad,
								AppliCursorMode && !ts.DisableAppCursor,
								Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
					else if (Shift) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF19,
								  AppliKeyMode && !ts.DisableAppKeypad,
								  AppliCursorMode && !ts.DisableAppCursor,
								  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
				}
				break;
			case Keys.F10:
				if (!ts.StrictKeyMapping) {
					if (Single) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF10,
								AppliKeyMode && !ts.DisableAppKeypad,
								AppliCursorMode && !ts.DisableAppCursor,
								Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
					else if (Shift) {
						ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF20,
								  AppliKeyMode && !ts.DisableAppKeypad,
								  AppliCursorMode && !ts.DisableAppCursor,
								  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
					}
				}
				break;
			case Keys.F11:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF11,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.F12:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF12,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.F13:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF13,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.F14:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF14,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.F15:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdHelp,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.F16:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdDo,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.F17:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF17,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.F18:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF18,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.F19:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF19,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.F20:
				if (Single && !ts.StrictKeyMapping) {
					ttcmn.GetKeyStr(HWin, KeyMap, InternalKeyCodes.IdF20,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, CodeSize, ref CodeLength, ref CodeType);
				}
				break;
			case Keys.D2:
				//  case Keys.OEM_3: /* @ (106-JP Keyboard) */
				if (Control && !ts.StrictKeyMapping) {
					// Ctrl-2 -> NUL
					CodeLength = 1;
					Code[0] = 0;
				}
				break;
			case Keys.D3:
				if (Control && !ts.StrictKeyMapping) {
					// Ctrl-3 -> ESC
					switch (AppliEscapeMode) {
					case 1:
						CodeLength = 3;
						Code[0] = 0x1B;
						Code[1] = (byte)'O';
						Code[2] = (byte)'[';
						break;
					case 2:
						CodeLength = 2;
						Code[0] = 0x1B;
						Code[1] = 0x1B;
						break;
					case 3:
						CodeLength = 2;
						Code[0] = 0x1B;
						Code[1] = 0x00;
						break;
					case 4:
						CodeLength = 8;
						Code[0] = 0x1B;
						Code[1] = 0x1B;
						Code[2] = (byte)'[';
						Code[3] = (byte)'=';
						Code[4] = (byte)'2';
						Code[5] = (byte)'7';
						Code[6] = (byte)'%';
						Code[7] = (byte)'~';
						break;
					default:
						CodeLength = 1;
						Code[0] = 0x1b;
						break;
					}
				}
				break;
			case Keys.D4:
				if (Control && !ts.StrictKeyMapping) {
					// Ctrl-4 -> FS
					CodeLength = 1;
					Code[0] = 0x1c;
				}
				break;
			case Keys.D5:
				if (Control && !ts.StrictKeyMapping) {
					// Ctrl-5 -> GS
					CodeLength = 1;
					Code[0] = 0x1d;
				}
				break;
			case Keys.D6:
				//  case Keys.OEM_7: /* ^ (106-JP Keyboard) */
				if (Control && !ts.StrictKeyMapping) {
					// Ctrl-6 -> RS
					CodeLength = 1;
					Code[0] = 0x1e;
				}
				break;
			case Keys.D7:
			case Keys.Oem2: /* / (101/106-JP Keyboard) */
				if (Control && !ts.StrictKeyMapping) {
					// Ctrl-7 -> US
					CodeLength = 1;
					Code[0] = 0x1f;
				}
				break;
			case Keys.D8:
				if (Control && !ts.StrictKeyMapping) {
					// Ctrl-8 -> DEL
					CodeLength = 1;
					Code[0] = 0x7f;
				}
				break;
			case Keys.Oem102:
				if (Control && Shift && !ts.StrictKeyMapping) {
					// Shift-Ctrl-_ (102RT/106-JP Keyboard)
					CodeLength = 1;
					Code[0] = 0x7f;
				}
				break;
			default:
				if ((VKey == VKBackslash) && Control) { // Ctrl-\ support for NEC-PC98
					CodeLength = 1;
					Code[0] = 0x1c;
				}
				break;
			}

			return CodeLength;
		}

		public const int PM_NOREMOVE = 0x0000;
		public const int PM_REMOVE = 0x0001;
		public const int PM_NOYIELD = 0x0002;

		public KeyDownReturnType KeyDown(Control HWin, Keys VKey, ushort Count, ushort Scan)
		{
			InternalKeyCodes Key;
			MSG M = new MSG();
			byte[] KeyState = new byte[256];
			int i;
			int CodeCount;
			int CodeLength;
			byte[] Code = new byte[tttypes.MAXPATHLEN];
			keyTypeIds CodeType;
			ushort ModStat;

			if (VKey == Keys.ProcessKey) return KeyDownReturnType.KEYDOWN_CONTROL;

			if ((VKey == Keys.Shift) ||
				(VKey == Keys.Control) ||
				(VKey == Keys.Menu)) return KeyDownReturnType.KEYDOWN_CONTROL;

			/* debug mode */
			if ((ts.Debug > 0) && (VKey == Keys.Escape) && ShiftKey()) {
				VTTerm.MessageBeep(0);
				DebugFlag = (KeyboardDebugFlag)((int)(DebugFlag + 1) % (int)KeyboardDebugFlag.DEBUG_FLAG_MAXD);
				PeekMessage(ref M, HWin.Handle, VTWindow.WM_CHAR, VTWindow.WM_CHAR, PM_REMOVE);
				return KeyDownReturnType.KEYDOWN_CONTROL;
			}

			if (!AutoRepeatMode && (PreviousKey == VKey)) {
				PeekMessage(ref M, HWin.Handle, VTWindow.WM_CHAR, VTWindow.WM_CHAR, PM_REMOVE);
				return KeyDownReturnType.KEYDOWN_CONTROL;
			}

			PreviousKey = VKey;

			//if (Scan == 0)
			//    Scan = MapVirtualKey(VKey, 0);

			ModStat = 0;
			if (ShiftKey()) {
				Scan |= 0x200;
				ModStat = 2;
			}

			if (ControlKey()) {
				Scan |= 0x400;
				ModStat |= 4;
			}

			if (AltKey()) {
				Scan |= 0x800;
				if (!MetaKey(ts.MetaKey)) {
					ModStat |= 16;
				}
			}

			CodeCount = Count;
			CodeLength = 0;
			if (cv.TelLineMode) {
				CodeType = keyTypeIds.IdText;
			}
			else {
				CodeType = keyTypeIds.IdBinary;
			}

			/* exclude numeric keypad "." (scan code:83) */
			if ((VKey != Keys.Delete) || (ts.DelKey == 0) || (Scan == 83))
				/* Windows keycode -> Tera Term keycode */
				Key = ttcmn.GetKeyCode(KeyMap, Scan);
			else
				Key = 0;

			if (Key == 0) {
				CodeLength = VKey2KeyStr(VKey, HWin, Code, Code.Length, ref CodeType, ModStat);

				if (MetaKey(ts.MetaKey) && (CodeLength == 1)) {
					switch (ts.Meta8Bit) {
					case Meta8BitMode.IdMeta8BitRaw:
						Code[0] |= 0x80;
						CodeType = keyTypeIds.IdBinary;
						break;
					case Meta8BitMode.IdMeta8BitText:
						Code[0] |= 0x80;
						CodeType = keyTypeIds.IdText;
						break;
					default:
						Code[1] = Code[0];
						Code[0] = 0x1b;
						CodeLength = 2;
						break;
					}
					PeekMessage(ref M, HWin.Handle, VTWindow.WM_SYSCHAR, VTWindow.WM_SYSCHAR, PM_REMOVE);
				}
			}
			else {
				if (MetaKey(ts.MetaKey)) {
					PeekMessage(ref M, HWin.Handle, VTWindow.WM_SYSCHAR, VTWindow.WM_SYSCHAR, PM_REMOVE);
				}

				if ((InternalKeyCodes.IdUDK6 <= Key) && (Key <= InternalKeyCodes.IdUDK20) && (FuncKeyLen[Key - InternalKeyCodes.IdUDK6] > 0)) {
					System.Buffer.BlockCopy(FuncKeyStr, (Key - InternalKeyCodes.IdUDK6) * FuncKeyStrMax, Code, 0, FuncKeyLen[Key - InternalKeyCodes.IdUDK6]);
					CodeLength = FuncKeyLen[Key - InternalKeyCodes.IdUDK6];
					CodeType = keyTypeIds.IdBinary;
				}
				else
					ttcmn.GetKeyStr(HWin, KeyMap, Key,
							  AppliKeyMode && !ts.DisableAppKeypad,
							  AppliCursorMode && !ts.DisableAppCursor,
							  Send8BitMode, Code, Code.Length, ref CodeLength, ref CodeType);
			}

			if (CodeLength == 0) return KeyDownReturnType.KEYDOWN_OTHER;

			if (VKey == Keys.NumLock) {
				/* keep NumLock LED status */
				GetKeyboardState(KeyState);
				KeyState[(int)Keys.NumLock] = (byte)(KeyState[(int)Keys.NumLock] ^ 1);
				SetKeyboardState(KeyState);
			}

			PeekMessage(ref M, HWin.Handle, VTWindow.WM_CHAR, VTWindow.WM_CHAR, PM_REMOVE);

			if (ttwinman.KeybEnabled) {
				switch (CodeType) {
				case keyTypeIds.IdBinary:
					if (ttwinman.TalkStatus == TalkerMode.IdTalkKeyb) {
						for (i = 1; i <= CodeCount; i++) {
							ttcmn.CommBinaryBuffOut(cv, Code, CodeLength);
							if (ts.LocalEcho)
								ttcmn.CommBinaryEcho(cv, Code, CodeLength);
						}
					}
					break;
				case keyTypeIds.IdText:
					if (ttwinman.TalkStatus == TalkerMode.IdTalkKeyb) {
						for (i = 1; i <= CodeCount; i++) {
							if (ts.LocalEcho)
								ttcmn.CommTextEcho(cv, Code, CodeLength);
							ttcmn.CommTextOut(cv, Code, CodeLength);
						}
					}
					break;
				case keyTypeIds.IdMacro:
					Code[CodeLength] = 0;
					//RunMacro(Code, false);
					break;
				case keyTypeIds.IdCommand:
					Code[CodeLength] = 0;
					//if (UInt16.TryParse(Code, out wId))
					//	PostMessage(HWin, WM_COMMAND, MAKELONG(wId, 0), 0);
					break;
				}
			}
			return (CodeType == keyTypeIds.IdBinary || CodeType == keyTypeIds.IdText) ? KeyDownReturnType.KEYDOWN_COMMOUT : KeyDownReturnType.KEYDOWN_CONTROL;
		}

		public void KeyCodeSend(ushort KCode, ushort Count)
		{
			InternalKeyCodes Key;
			int i, CodeLength;
			byte[] Code = new byte[tttypes.MAXPATHLEN];
			keyTypeIds CodeType;
			Keys VKey;
			ushort Scan, State;
			uint dw;
			bool Ok;
			Control HWin;

			if (ttwinman.ActiveWin == WindowId.IdTEK)
				HWin = null;//ttwinman.HTEKWin;
			else
				HWin = ttwinman.HVTWin;

			CodeLength = 0;
			CodeType = keyTypeIds.IdBinary;
			Key = ttcmn.GetKeyCode(KeyMap, KCode);
			if (Key == 0) {
				Scan = (ushort)(KCode & 0x1FF);
				VKey = (Keys)MapVirtualKey(Scan, 1);
				State = 0;
				if ((KCode & 512) != 0) { /* shift */
					State = (ushort)(State | 2); /* bit 1 */
				}

				if ((KCode & 1024) != 0) { /* control */
					State = (ushort)(State | 4); /* bit 2 */
				}

				if ((KCode & 2048) != 0) { /* alt */
					State = (ushort)(State | 16); /* bit 4 */
				}

				CodeLength = VKey2KeyStr(VKey, HWin, Code, Code.Length, ref CodeType, State);

				if (CodeLength == 0) {
					i = -1;
					do {
						i++;
						dw = OemKeyScan((ushort)i);
						Ok = ((dw & 0xFFFFu) == Scan) &&
							 ((dw >> 16) == State);
					} while ((i < 255) && !Ok);
					if (Ok) {
						CodeType = keyTypeIds.IdText;
						CodeLength = 1;
						Code[0] = (byte)i;
					}
				}
			}
			else if ((InternalKeyCodes.IdUDK6 <= Key) && (Key <= InternalKeyCodes.IdUDK20) &&
				 (FuncKeyLen[Key - InternalKeyCodes.IdUDK6] > 0)) {
				System.Buffer.BlockCopy(FuncKeyStr, (Key - InternalKeyCodes.IdUDK6) * FuncKeyStrMax, Code, 0, FuncKeyLen[Key - InternalKeyCodes.IdUDK6]);
				CodeLength = FuncKeyLen[Key - InternalKeyCodes.IdUDK6];
				CodeType = keyTypeIds.IdBinary;
			}
			else
				ttcmn.GetKeyStr(HWin, KeyMap, Key,
						  AppliKeyMode && !ts.DisableAppKeypad,
						  AppliCursorMode && !ts.DisableAppCursor,
						  Send8BitMode, Code, Code.Length, ref CodeLength, ref CodeType);

			if (CodeLength == 0) return;
			if (ttwinman.TalkStatus == TalkerMode.IdTalkKeyb) {
				switch (CodeType) {
				case keyTypeIds.IdBinary:
					for (i = 1; i <= Count; i++) {
						ttcmn.CommBinaryBuffOut(cv, Code, CodeLength);
						if (ts.LocalEcho)
							ttcmn.CommBinaryEcho(cv, Code, CodeLength);
					}
					break;
				case keyTypeIds.IdText:
					for (i = 1; i <= Count; i++) {
						if (ts.LocalEcho)
							ttcmn.CommTextEcho(cv, Code, CodeLength);
						ttcmn.CommTextOut(cv, Code, CodeLength);
					}
					break;
				case keyTypeIds.IdMacro:
					Code[CodeLength] = 0;
					//RunMacro(Code, false);
					break;
				}
			}
		}

		public void KeyUp(Keys VKey)
		{
			if (PreviousKey == VKey) PreviousKey = 0;
		}

		public bool ShiftKey()
		{
			return (Control.ModifierKeys & Keys.Shift) != 0;
		}

		public bool ControlKey()
		{
			return (Control.ModifierKeys & Keys.Control) != 0;
		}

		public bool AltKey()
		{
			return (Control.ModifierKeys & Keys.Alt) != 0;
		}

		bool MetaKey(MetaId mode)
		{
			switch (mode) {
			case MetaId.IdMetaOn:
				return ((GetAsyncKeyState(Keys.Menu) & 0xFFFFFF80) != 0);
			case MetaId.IdMetaLeft:
				return ((GetAsyncKeyState(Keys.LMenu) & 0xFFFFFF80) != 0);
			case MetaId.IdMetaRight:
				return ((GetAsyncKeyState(Keys.RMenu) & 0xFFFFFF80) != 0);
			default:
				return false;
			}
		}

		public void InitKeyboard()
		{
			KeyMap = null;
			ClearUserKey();
			PreviousKey = 0;
			VKBackslash = Keys.OemBackslash;//(byte)(VkKeyScan('\\' & 0xFF));
		}

		public void EndKeyboard()
		{
		}

		internal void Init(ProgramDatas datas)
		{
			ttwinman = datas.ttwinman;
			ts = datas.TTTSet;
			cv = datas.TComVar;
		}

		[DllImport("user32.dll")]
		public static extern void PeekMessage(ref MSG lpMsg, IntPtr hWnd, int wMsgFilterMin, int wMsgFilterMax, int wRemoveMsg);
		[DllImport("user32.dll")]
		public static extern bool SetKeyboardState(byte[] KeyState);
		[DllImport("user32.dll")]
		public static extern bool GetKeyboardState(byte[] KeyState);
		[DllImport("user32.dll")]
		public static extern uint OemKeyScan(ushort wOemChar);
		[DllImport("user32.dll")]
		public static extern uint MapVirtualKey(uint Scan, uint p);
		[DllImport("user32.dll")]
		static extern ushort GetAsyncKeyState(Keys vKey);
	}
}

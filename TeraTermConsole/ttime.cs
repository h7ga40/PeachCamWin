/*
 * Copyright (C) 1994-1998 T. Teranishi
 * (C) 2007-2017 TeraTerm Project
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
/* Tera Term */
/* TERATERM.EXE, IME interface */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace TeraTerm
{
	class ttime
	{
		TTTSet ts;
		ttwinman ttwinman;

		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		private extern static IntPtr LoadLibrary(string lpFileName);

		[DllImport("kernel32")]
		private extern static bool FreeLibrary(IntPtr hModule);

		[DllImport("kernel32", CharSet = CharSet.Ansi)]
		private extern static IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

		const uint GCS_RESULTSTR = 0x0800;

		const int CFS_DEFAULT = 0x0000;
		const int CFS_RECT = 0x0001;
		const int CFS_POINT = 0x0002;
		const int CFS_FORCE_POSITION = 0x0020;
		const int CFS_CANDIDATEPOS = 0x0040;
		const int CFS_EXCLUDE = 0x0080;

		delegate int TImmGetCompositionString(IntPtr hIMC, uint dwIndex, IntPtr lpBuf, uint dwBufLen);
		delegate IntPtr TImmGetContext(IntPtr hWnd);
		delegate bool TImmReleaseContext(IntPtr hWnd, IntPtr hIMC);
		delegate bool TImmSetCompositionFont(IntPtr hIMC, [In]ref LOGFONT lplf);
		delegate bool TImmSetCompositionWindow(IntPtr hIMC, [In]ref COMPOSITIONFORM lpCompForm);
		delegate bool TImmGetOpenStatus(IntPtr hIMC);
		delegate bool TImmSetOpenStatus(IntPtr hIMC, bool fOpen);

		static TImmGetCompositionString PImmGetCompositionString;
		static TImmGetContext PImmGetContext;
		static TImmReleaseContext PImmReleaseContext;
		static TImmSetCompositionFont PImmSetCompositionFont;
		static TImmSetCompositionWindow PImmSetCompositionWindow;
		static TImmGetOpenStatus PImmGetOpenStatus;
		static TImmSetOpenStatus PImmSetOpenStatus;

		IntPtr HIMEDLL = IntPtr.Zero;
		Font lfIME;

		const int MAX_UIMSG = 1024;// i18n.h

		public bool LoadIME()
		{
			bool Err;
#if false
			PTTSet tempts;
#endif
			char[] uimsg = new char[MAX_UIMSG];
			string imm32_dll;

			if (HIMEDLL != IntPtr.Zero) return true;
			imm32_dll = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "imm32.dll");
			HIMEDLL = LoadLibrary(imm32_dll);
			if (HIMEDLL == IntPtr.Zero) {
				ttlib.get_lang_msg("MSG_TT_ERROR", uimsg, uimsg.Length, "Tera Term: Error", ts.UILanguageFile);
				ttlib.get_lang_msg("MSG_USE_IME_ERROR", ts.UIMsg, ts.UIMsg.Length, "Can't use IME", ts.UILanguageFile);
				MessageBox.Show(new String(ts.UIMsg), new String(uimsg), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				//WritePrivateProfileString("Tera Term", "IME", "off", ts.SetupFName);
				ts.UseIME = false;
#if false
				tempts = (PTTSet)malloc(TTTSet.Length);
				if (tempts!=null)
				{
					GetDefaultSet(tempts);
					tempts.UseIME = 0;
					ChangeDefaultSet(tempts,null);
					free(tempts);
				}
#endif
				return false;
			}

			Err = false;

			PImmGetCompositionString = (TImmGetCompositionString)Marshal.GetDelegateForFunctionPointer(GetProcAddress(
				HIMEDLL, "ImmGetCompositionStringA"), typeof(TImmGetCompositionString));
			if (PImmGetCompositionString == null) Err = true;

			PImmGetContext = (TImmGetContext)Marshal.GetDelegateForFunctionPointer(GetProcAddress(
				HIMEDLL, "ImmGetContext"), typeof(TImmGetContext));
			if (PImmGetContext == null) Err = true;

			PImmReleaseContext = (TImmReleaseContext)Marshal.GetDelegateForFunctionPointer(GetProcAddress(
				HIMEDLL, "ImmReleaseContext"), typeof(TImmReleaseContext));
			if (PImmReleaseContext == null) Err = true;

			PImmSetCompositionFont = (TImmSetCompositionFont)Marshal.GetDelegateForFunctionPointer(GetProcAddress(
				HIMEDLL, "ImmSetCompositionFontW"), typeof(TImmSetCompositionFont));
			if (PImmSetCompositionFont == null) Err = true;

			PImmSetCompositionWindow = (TImmSetCompositionWindow)Marshal.GetDelegateForFunctionPointer(GetProcAddress(
				HIMEDLL, "ImmSetCompositionWindow"), typeof(TImmSetCompositionWindow));
			if (PImmSetCompositionWindow == null) Err = true;

			PImmGetOpenStatus = (TImmGetOpenStatus)Marshal.GetDelegateForFunctionPointer(GetProcAddress(
				HIMEDLL, "ImmGetOpenStatus"), typeof(TImmGetOpenStatus));
			if (PImmGetOpenStatus == null) Err = true;

			PImmSetOpenStatus = (TImmSetOpenStatus)Marshal.GetDelegateForFunctionPointer(GetProcAddress(
				HIMEDLL, "ImmSetOpenStatus"), typeof(TImmSetOpenStatus));
			if (PImmSetOpenStatus == null) Err = true;

			if (Err) {
				FreeLibrary(HIMEDLL);
				HIMEDLL = IntPtr.Zero;
				return false;
			}
			else
				return true;
		}

		public void FreeIME()
		{
			IntPtr HTemp;

			if (HIMEDLL == IntPtr.Zero) return;
			HTemp = HIMEDLL;
			HIMEDLL = IntPtr.Zero;

			/* position of conv.window.default */
			SetConversionWindow(ttwinman.HVTWin, -1, 0);
			Thread.Sleep(1); // for safety
			FreeLibrary(HTemp);
		}

		public bool CanUseIME()
		{
			return (HIMEDLL != IntPtr.Zero);
		}

		public void SetConversionWindow(Control control, int X, int Y)
		{
			IntPtr hIMC;
			COMPOSITIONFORM cf = new COMPOSITIONFORM();

			if (HIMEDLL == IntPtr.Zero) return;
			// Adjust the position of conversion window
			hIMC = ImmGetContext(ttwinman.HVTWin);
			if (X >= 0) {
				cf.dwStyle = CFS_POINT;
				cf.ptCurrentPos.X = X;
				cf.ptCurrentPos.Y = Y;
			}
			else
				cf.dwStyle = CFS_DEFAULT;
			ImmSetCompositionWindow(hIMC, ref cf);

			// Set font for the conversion window
			if (lfIME != null)
				ImmSetCompositionFont(hIMC, lfIME);

			ImmReleaseContext(ttwinman.HVTWin, hIMC);
		}

		public void SetConversionLogFont(Font lf)
		{
			lfIME = (Font)lf.Clone();
		}

		public string GetConvString(uint wParam, uint lParam)
		{
			string result = null;
			IntPtr hIMC;
			IntPtr lpstr = IntPtr.Zero;
			int dwSize;

			if (HIMEDLL == IntPtr.Zero) return null;
			hIMC = ImmGetContext(ttwinman.HVTWin);
			if (hIMC == IntPtr.Zero) return null;
			try {
				if ((lParam & GCS_RESULTSTR) == 0)
					goto skip;

				// Get the size of the result string.
				//dwSize = ImmGetCompositionString(hIMC, GCS_RESULTSTR, null, 0);
				dwSize = ImmGetCompositionString(hIMC, GCS_RESULTSTR, IntPtr.Zero, 0);
				dwSize += sizeof(char);
				lpstr = Marshal.AllocHGlobal(dwSize);
				try {
#if false
					// Get the result strings that is generated by IME into lpstr.
					ImmGetCompositionString(hIMC, GCS_RESULTSTR, lpstr, dwSize);
#else
					ImmGetCompositionString(hIMC, GCS_RESULTSTR, lpstr, (uint)dwSize);
#endif
					result = Marshal.PtrToStringUni(lpstr);
				}
				finally {
					Marshal.FreeHGlobal(lpstr);
				}
			skip:;
			}
			finally {
				ImmReleaseContext(ttwinman.HVTWin, hIMC);
			}
			return result;
		}

		public bool GetIMEOpenStatus()
		{
			IntPtr hIMC;
			bool stat;

			hIMC = ImmGetContext(ttwinman.HVTWin);
			stat = ImmGetOpenStatus(hIMC);
			ImmReleaseContext(ttwinman.HVTWin, hIMC);

			return stat;
		}

		public void SetIMEOpenStatus(bool stat)
		{
			IntPtr hIMC;

			hIMC = ImmGetContext(ttwinman.HVTWin);
			ImmSetOpenStatus(hIMC, stat);
			ImmReleaseContext(ttwinman.HVTWin, hIMC);
		}

		public static int ImmGetCompositionString(IntPtr hIMC, uint dwIndex, IntPtr lpBuf, uint dwBufLen)
		{
			return PImmGetCompositionString(hIMC, dwIndex, lpBuf, dwBufLen);
		}

		public static IntPtr ImmGetContext(Control control)
		{
			return PImmGetContext(control.Handle);
		}

		public static bool ImmReleaseContext(Control control, IntPtr hIMC)
		{
			return PImmReleaseContext(control.Handle, hIMC);
		}

		public static bool ImmSetCompositionFont(IntPtr hIMC, Font font)
		{
			LOGFONT lplf = new LOGFONT();
			object obj = lplf;
			font.ToLogFont(obj);
			lplf = (LOGFONT)obj;
			return PImmSetCompositionFont(hIMC, ref lplf);
		}

		public static bool ImmSetCompositionWindow(IntPtr hIMC, ref COMPOSITIONFORM lpCompForm)
		{
			return PImmSetCompositionWindow(hIMC, ref lpCompForm);
		}

		public static bool ImmGetOpenStatus(IntPtr hIMC)
		{
			return PImmGetOpenStatus(hIMC);
		}

		public static bool ImmSetOpenStatus(IntPtr hIMC, bool fOpen)
		{
			return PImmSetOpenStatus(hIMC, fOpen);
		}

		internal void Init(ProgramDatas datas)
		{
			ttwinman = datas.ttwinman;
			ts = datas.TTTSet;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	struct COMPOSITIONFORM
	{
		public uint dwStyle;
		public Point ptCurrentPos;
		public RECT rcArea;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct LOGFONT
	{
		public const int LF_FACESIZE = 32;
		public int lfHeight;
		public int lfWidth;
		public int lfEscapement;
		public int lfOrientation;
		public int lfWeight;
		public byte lfItalic;
		public byte lfUnderline;
		public byte lfStrikeOut;
		public byte lfCharSet;
		public byte lfOutPrecision;
		public byte lfClipPrecision;
		public byte lfQuality;
		public byte lfPitchAndFamily;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE)]
		public string lfFaceName;
	}
}

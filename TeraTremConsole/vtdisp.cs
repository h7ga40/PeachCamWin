/*
 * Copyright (C) 1994-1998 T. Teranishi
 * (C) 2005-2018 TeraTerm Project
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

/* TERATERM.EXE, VT terminal display routines */
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;

namespace TeraTrem
{
	enum ScrollType
	{
		SCROLL_BOTTOM = 1,
		SCROLL_LINEDOWN = 2,
		SCROLL_LINEUP = 3,
		SCROLL_PAGEDOWN = 4,
		SCROLL_PAGEUP = 5,
		SCROLL_POS = 6,
		SCROLL_TOP = 7,
	}

	enum WindowType
	{
		WINDOW_MINIMIZE = 1,
		WINDOW_MAXIMIZE = 2,
		WINDOW_RESTORE = 3,
		WINDOW_RAISE = 4,
		WINDOW_LOWER = 5,
		WINDOW_REFRESH = 6,
		WINDOW_TOGGLE_MAXIMIZE = 7,
	}

	class VTDisp
	{
		const int CurWidth = 2;

		static readonly byte[][] DefaultColorTable = new byte[256][] {
			new byte[]{  0,  0,  0}, new byte[]{255,  0,  0}, new byte[]{  0,255,  0}, new byte[]{255,255,  0}, new byte[]{  0,  0,255}, new byte[]{255,  0,255}, new byte[]{  0,255,255}, new byte[]{255,255,255},  //   0 -   7
			new byte[]{128,128,128}, new byte[]{128,  0,  0}, new byte[]{  0,128,  0}, new byte[]{128,128,  0}, new byte[]{  0,  0,128}, new byte[]{128,  0,128}, new byte[]{  0,128,128}, new byte[]{192,192,192},  //   8 -  15
			new byte[]{  0,  0,  0}, new byte[]{  0,  0, 95}, new byte[]{  0,  0,135}, new byte[]{  0,  0,175}, new byte[]{  0,  0,215}, new byte[]{  0,  0,255}, new byte[]{  0, 95,  0}, new byte[]{  0, 95, 95},  //  16 -  23
			new byte[]{  0, 95,135}, new byte[]{  0, 95,175}, new byte[]{  0, 95,215}, new byte[]{  0, 95,255}, new byte[]{  0,135,  0}, new byte[]{  0,135, 95}, new byte[]{  0,135,135}, new byte[]{  0,135,175},  //  24 -  31
			new byte[]{  0,135,215}, new byte[]{  0,135,255}, new byte[]{  0,175,  0}, new byte[]{  0,175, 95}, new byte[]{  0,175,135}, new byte[]{  0,175,175}, new byte[]{  0,175,215}, new byte[]{  0,175,255},  //  32 -  39
			new byte[]{  0,215,  0}, new byte[]{  0,215, 95}, new byte[]{  0,215,135}, new byte[]{  0,215,175}, new byte[]{  0,215,215}, new byte[]{  0,215,255}, new byte[]{  0,255,  0}, new byte[]{  0,255, 95},  //  40 -  47
			new byte[]{  0,255,135}, new byte[]{  0,255,175}, new byte[]{  0,255,215}, new byte[]{  0,255,255}, new byte[]{ 95,  0,  0}, new byte[]{ 95,  0, 95}, new byte[]{ 95,  0,135}, new byte[]{ 95,  0,175},  //  48 -  55
			new byte[]{ 95,  0,215}, new byte[]{ 95,  0,255}, new byte[]{ 95, 95,  0}, new byte[]{ 95, 95, 95}, new byte[]{ 95, 95,135}, new byte[]{ 95, 95,175}, new byte[]{ 95, 95,215}, new byte[]{ 95, 95,255},  //  56 -  63
			new byte[]{ 95,135,  0}, new byte[]{ 95,135, 95}, new byte[]{ 95,135,135}, new byte[]{ 95,135,175}, new byte[]{ 95,135,215}, new byte[]{ 95,135,255}, new byte[]{ 95,175,  0}, new byte[]{ 95,175, 95},  //  64 -  71
			new byte[]{ 95,175,135}, new byte[]{ 95,175,175}, new byte[]{ 95,175,215}, new byte[]{ 95,175,255}, new byte[]{ 95,215,  0}, new byte[]{ 95,215, 95}, new byte[]{ 95,215,135}, new byte[]{ 95,215,175},  //  72 -  79
			new byte[]{ 95,215,215}, new byte[]{ 95,215,255}, new byte[]{ 95,255,  0}, new byte[]{ 95,255, 95}, new byte[]{ 95,255,135}, new byte[]{ 95,255,175}, new byte[]{ 95,255,215}, new byte[]{ 95,255,255},  //  80 -  87
			new byte[]{135,  0,  0}, new byte[]{135,  0, 95}, new byte[]{135,  0,135}, new byte[]{135,  0,175}, new byte[]{135,  0,215}, new byte[]{135,  0,255}, new byte[]{135, 95,  0}, new byte[]{135, 95, 95},  //  88 -  95
			new byte[]{135, 95,135}, new byte[]{135, 95,175}, new byte[]{135, 95,215}, new byte[]{135, 95,255}, new byte[]{135,135,  0}, new byte[]{135,135, 95}, new byte[]{135,135,135}, new byte[]{135,135,175},  //  96 - 103
			new byte[]{135,135,215}, new byte[]{135,135,255}, new byte[]{135,175,  0}, new byte[]{135,175, 95}, new byte[]{135,175,135}, new byte[]{135,175,175}, new byte[]{135,175,215}, new byte[]{135,175,255},  // 104 - 111
			new byte[]{135,215,  0}, new byte[]{135,215, 95}, new byte[]{135,215,135}, new byte[]{135,215,175}, new byte[]{135,215,215}, new byte[]{135,215,255}, new byte[]{135,255,  0}, new byte[]{135,255, 95},  // 112 - 119
			new byte[]{135,255,135}, new byte[]{135,255,175}, new byte[]{135,255,215}, new byte[]{135,255,255}, new byte[]{175,  0,  0}, new byte[]{175,  0, 95}, new byte[]{175,  0,135}, new byte[]{175,  0,175},  // 120 - 127
			new byte[]{175,  0,215}, new byte[]{175,  0,255}, new byte[]{175, 95,  0}, new byte[]{175, 95, 95}, new byte[]{175, 95,135}, new byte[]{175, 95,175}, new byte[]{175, 95,215}, new byte[]{175, 95,255},  // 128 - 135
			new byte[]{175,135,  0}, new byte[]{175,135, 95}, new byte[]{175,135,135}, new byte[]{175,135,175}, new byte[]{175,135,215}, new byte[]{175,135,255}, new byte[]{175,175,  0}, new byte[]{175,175, 95},  // 136 - 143
			new byte[]{175,175,135}, new byte[]{175,175,175}, new byte[]{175,175,215}, new byte[]{175,175,255}, new byte[]{175,215,  0}, new byte[]{175,215, 95}, new byte[]{175,215,135}, new byte[]{175,215,175},  // 144 - 151
			new byte[]{175,215,215}, new byte[]{175,215,255}, new byte[]{175,255,  0}, new byte[]{175,255, 95}, new byte[]{175,255,135}, new byte[]{175,255,175}, new byte[]{175,255,215}, new byte[]{175,255,255},  // 152 - 159
			new byte[]{215,  0,  0}, new byte[]{215,  0, 95}, new byte[]{215,  0,135}, new byte[]{215,  0,175}, new byte[]{215,  0,215}, new byte[]{215,  0,255}, new byte[]{215, 95,  0}, new byte[]{215, 95, 95},  // 160 - 167
			new byte[]{215, 95,135}, new byte[]{215, 95,175}, new byte[]{215, 95,215}, new byte[]{215, 95,255}, new byte[]{215,135,  0}, new byte[]{215,135, 95}, new byte[]{215,135,135}, new byte[]{215,135,175},  // 168 - 175
			new byte[]{215,135,215}, new byte[]{215,135,255}, new byte[]{215,175,  0}, new byte[]{215,175, 95}, new byte[]{215,175,135}, new byte[]{215,175,175}, new byte[]{215,175,215}, new byte[]{215,175,255},  // 176 - 183
			new byte[]{215,215,  0}, new byte[]{215,215, 95}, new byte[]{215,215,135}, new byte[]{215,215,175}, new byte[]{215,215,215}, new byte[]{215,215,255}, new byte[]{215,255,  0}, new byte[]{215,255, 95},  // 184 - 191
			new byte[]{215,255,135}, new byte[]{215,255,175}, new byte[]{215,255,215}, new byte[]{215,255,255}, new byte[]{255,  0,  0}, new byte[]{255,  0, 95}, new byte[]{255,  0,135}, new byte[]{255,  0,175},  // 192 - 199
			new byte[]{255,  0,215}, new byte[]{255,  0,255}, new byte[]{255, 95,  0}, new byte[]{255, 95, 95}, new byte[]{255, 95,135}, new byte[]{255, 95,175}, new byte[]{255, 95,215}, new byte[]{255, 95,255},  // 200 - 207
			new byte[]{255,135,  0}, new byte[]{255,135, 95}, new byte[]{255,135,135}, new byte[]{255,135,175}, new byte[]{255,135,215}, new byte[]{255,135,255}, new byte[]{255,175,  0}, new byte[]{255,175, 95},  // 208 - 215
			new byte[]{255,175,135}, new byte[]{255,175,175}, new byte[]{255,175,215}, new byte[]{255,175,255}, new byte[]{255,215,  0}, new byte[]{255,215, 95}, new byte[]{255,215,135}, new byte[]{255,215,175},  // 216 - 223
			new byte[]{255,215,215}, new byte[]{255,215,255}, new byte[]{255,255,  0}, new byte[]{255,255, 95}, new byte[]{255,255,135}, new byte[]{255,255,175}, new byte[]{255,255,215}, new byte[]{255,255,255},  // 224 - 231
			new byte[]{  8,  8,  8}, new byte[]{ 18, 18, 18}, new byte[]{ 28, 28, 28}, new byte[]{ 38, 38, 38}, new byte[]{ 48, 48, 48}, new byte[]{ 58, 58, 58}, new byte[]{ 68, 68, 68}, new byte[]{ 78, 78, 78},  // 232 - 239
			new byte[]{ 88, 88, 88}, new byte[]{ 98, 98, 98}, new byte[]{108,108,108}, new byte[]{118,118,118}, new byte[]{128,128,128}, new byte[]{138,138,138}, new byte[]{148,148,148}, new byte[]{158,158,158},  // 240 - 247
			new byte[]{168,168,168}, new byte[]{178,178,178}, new byte[]{188,188,188}, new byte[]{198,198,198}, new byte[]{208,208,208}, new byte[]{218,218,218}, new byte[]{228,228,228}, new byte[]{238,238,238}   // 248 - 255
		};

		ttwinman ttwinman;
		ttime ttime;
		TTTSet ts;
		TComVar cv;

		public int WinWidth, WinHeight;
		static bool Active = false;
		static bool CompletelyVisible;
		Font VTFont;
		public int FontHeight, FontWidth, ScreenWidth, ScreenHeight;
		public bool AdjustSize;
		public bool DontChangeSize = false;
		public int CursorX, CursorY;
		/* Virtual screen region */
		Rectangle VirtualScreen;

		// --- scrolling status flags
		public int WinOrgX, WinOrgY, NewOrgX, NewOrgY;

		public int NumOfLines, NumOfColumns;
		public int PageStart, BuffEnd;

		bool CursorOnDBCS = false;
		bool SaveWinSize = false;
		int WinWidthOld, WinHeightOld;
		Brush Background;
		Color[] ANSIColor = new Color[256];

		// caret variables
		int CaretStatus;
		bool CaretEnabled = true;

		// ---- device context and status flags
		Graphics VTDC = null; /* Device context for ControlCharacters.VT window */
		TCharAttr DCAttr;
		TCharAttr CurCharAttr;
		bool DCReverse;

		public TCharAttr DefCharAttr = new TCharAttr(AttributeBitMasks.AttrDefault, AttributeBitMasks.AttrDefault, (ColorCodes)AttributeBitMasks.AttrDefaultFG, (ColorCodes)AttributeBitMasks.AttrDefaultBG);

		// scrolling
		int ScrollCount = 0;
		int dScroll = 0;
		int SRegionTop;
		int SRegionBottom;

		Color[] BGVTColor = new Color[2];
		Color[] BGVTBoldColor = new Color[2];
		Color[] BGVTBlinkColor = new Color[2];
		Color[] BGVTReverseColor = new Color[2];
		/* begin - ishizaki */
		Color[] BGURLColor = new Color[2];
		/* end - ishizaki */

		void DispApplyANSIColor()
		{
			int i;

			for (i = (int)ColorCodes.IdBack; i <= (int)ColorCodes.IdFore + 8; i++)
				ANSIColor[i] = ts.ANSIColor[i];

			if ((ts.ColorFlag & ColorFlags.CF_USETEXTCOLOR) != 0) {
				ANSIColor[(int)ColorCodes.IdBack] = ts.VTColor[1]; // use background color for "Black"
				ANSIColor[(int)ColorCodes.IdFore] = ts.VTColor[0]; // use text color for "white"
			}
		}

		void InitColorTable()
		{
			int i;

			DispApplyANSIColor();

			for (i = 16; i <= 255; i++) {
				ANSIColor[i] = Color.FromArgb(DefaultColorTable[i][0], DefaultColorTable[i][1], DefaultColorTable[i][2]);
			}
		}

		void DispSetNearestColors(int start, int end, Graphics DispCtx)
		{
			Graphics TmpDC;
			int i;

			if (DispCtx != null) {
				TmpDC = DispCtx;
			}
			else {
				TmpDC = Graphics.FromHwnd(IntPtr.Zero);
			}

			for (i = start; i <= end; i++)
				ANSIColor[i] = TmpDC.GetNearestColor(ANSIColor[i]);

			if (DispCtx == null) {
				TmpDC.Dispose();
			}
		}

		const int CW_USEDEFAULT = unchecked((int)0x80000000);

		public void InitDisp()
		{
			Graphics TmpDC;
			bool bMultiDisplaySupport = false;

			TmpDC = Graphics.FromHwnd(IntPtr.Zero);

			InitColorTable();

			DispSetNearestColors((int)ColorCodes.IdBack, 255, TmpDC);

			/* background paintbrush */
			Background = new SolidBrush(ts.VTColor[1]);
			/* CRT width & height */
			{
				OperatingSystem ver = Environment.OSVersion;
				switch (ver.Platform) {
				// Windows 9x か NT かの判定
				case PlatformID.Win32Windows:
					if (ver.Version.Major > 4 ||
						(ver.Version.Major == 4 && ver.Version.Minor >= 10)) // Windows 98 or later
						bMultiDisplaySupport = true;
					break;
				case PlatformID.Win32NT:
					if (ver.Version.Major >= 5) // Windows 2000 or later
						bMultiDisplaySupport = true;
					break;
				default:
					break;
				}
			}
			if (bMultiDisplaySupport) {
				VirtualScreen = SystemInformation.VirtualScreen;
			}
			else {
				VirtualScreen = Screen.PrimaryScreen.Bounds;
			}

			TmpDC.Dispose();

			if ((ts.VTPos.X > VirtualScreen.Right) || (ts.VTPos.Y > VirtualScreen.Bottom)) {
				ts.VTPos.X = CW_USEDEFAULT;
				ts.VTPos.Y = CW_USEDEFAULT;
			}
			else if ((ts.VTPos.X < VirtualScreen.Left - 20) || (ts.VTPos.Y < VirtualScreen.Top - 20)) {
				ts.VTPos.X = CW_USEDEFAULT;
				ts.VTPos.Y = CW_USEDEFAULT;
			}
			else {
				if (ts.VTPos.X < VirtualScreen.Left) ts.VTPos.X = VirtualScreen.Left;
				if (ts.VTPos.Y < VirtualScreen.Top) ts.VTPos.Y = VirtualScreen.Top;
			}

			if ((ts.TEKPos.X > VirtualScreen.Right) || (ts.TEKPos.Y > VirtualScreen.Bottom)) {
				ts.TEKPos.X = CW_USEDEFAULT;
				ts.TEKPos.Y = CW_USEDEFAULT;
			}
			else if ((ts.TEKPos.X < VirtualScreen.Left - 20) || (ts.TEKPos.Y < VirtualScreen.Top - 20)) {
				ts.TEKPos.X = CW_USEDEFAULT;
				ts.TEKPos.Y = CW_USEDEFAULT;
			}
			else {
				if (ts.TEKPos.X < VirtualScreen.Left) ts.TEKPos.X = VirtualScreen.Left;
				if (ts.TEKPos.Y < VirtualScreen.Top) ts.TEKPos.Y = VirtualScreen.Top;
			}
		}

		public void EndDisp()
		{
			if (VTDC != null) DispReleaseDC();

			/* Delete fonts */
			VTFont.Dispose();

			if (Background != null) {
				Background.Dispose();
				Background = null;
			}
		}

		public void DispReset()
		{
			/* Cursor */
			CursorX = 0;
			CursorY = 0;

			/* Scroll status */
			ScrollCount = 0;
			dScroll = 0;

			if (IsCaretOn()) CaretOn();
			DispEnableCaret(true); // enable caret
		}

		public void DispConvWinToScreen(int Xw, int Yw, out int Xs, out int Ys, out bool Right)
		// Converts window coordinate to screen cordinate
		//   Xs: horizontal position in window coordinate (pixels)
		//   Ys: vertical
		//  Output
		//	 Xs, Ys: screen coordinate
		//   Right: true if the (Xs,Ys) is on the right half of
		//			 a character cell.
		{
			Xs = Xw / FontWidth + WinOrgX;
			Ys = Yw / FontHeight + WinOrgY;
			Right = (Xw - (Xs - WinOrgX) * FontWidth) >= FontWidth / 2;
		}

		public void DispConvScreenToWin(int Xs, int Ys, out int Xw, out int Yw)
		// Converts screen coordinate to window cordinate
		//   Xs: horizontal position in screen coordinate (characters)
		//   Ys: vertical
		//  Output
		//      Xw, Yw: window coordinate
		{
			Xw = (Xs - WinOrgX) * FontWidth;
			Yw = (Ys - WinOrgY) * FontHeight;
		}

		void SetLogFont()
		{
			VTFont = new Font(ts.VTFont, FontStyle.Regular);
		}

		public void ChangeFont()
		{
			int i;

			/* Delete Old Fonts */
			if (VTFont != null)
				VTFont.Dispose();

			/* Normal Font */
			SetLogFont();

			/* set IME font */
			ttime.SetConversionLogFont(VTFont);

			float h = VTFont.GetHeight();
			//Size size = TextRenderer.MeasureText("W", VTFont, new Size((int)h / 2, (int)h));
			Size size = new Size((int)h / 2, (int)h);
			FontWidth = size.Width + ts.FontDW;
			FontHeight = size.Height + ts.FontDH;
		}

		public void ResetIME()
		{
			/* reset IME */
			if (!ts.UseIME)
				ttime.FreeIME();
			else if (!ttime.LoadIME())
				ts.UseIME = false;

			if (ts.UseIME) {
				if (ts.IMEInline)
					ttime.SetConversionLogFont(VTFont);
				else
					ttime.SetConversionWindow(ttwinman.HVTWin, -1, 0);
			}

			if (IsCaretOn()) CaretOn();
		}

		public void ChangeCaret()
		{
			uint T;

			if (!Active) return;
			if (CaretEnabled) {
				DestroyCaret();
				switch (ts.CursorShape) {
				case CursorShapes.IdVCur:
					CreateCaret(ttwinman.HVTWin.Handle, IntPtr.Zero, CurWidth, FontHeight);
					break;
				case CursorShapes.IdHCur:
					CreateCaret(ttwinman.HVTWin.Handle, IntPtr.Zero, FontWidth, CurWidth);
					break;
				}
				CaretStatus = 1;
			}
			CaretOn();
			if (CaretEnabled &&
				(ts.NonblinkingCursor)) {
				T = GetCaretBlinkTime() * 2u / 3u;
				ttwinman.HVTWin.IdCaretTimer.Interval = (int)T;
				ttwinman.HVTWin.IdCaretTimer.Enabled = true;
			}
			UpdateCaretPosition(true);
		}

		// WM_KILLFOCUSされたときのカーソルを自分で描く
		public void CaretKillFocus(bool show)
		{
			int CaretX, CaretY;
			Point[] p = new Point[5];

			if (!ts.KillFocusCursor)
				return;

			/* Get Device Context */
			DispInitDC();

			CaretX = (CursorX - WinOrgX) * FontWidth;
			CaretY = (CursorY - WinOrgY) * FontHeight;

			p[0].X = CaretX;
			p[0].Y = CaretY;
			p[1].X = CaretX;
			p[1].Y = CaretY + FontHeight - 1;
			if (CursorOnDBCS)
				p[2].X = CaretX + FontWidth * 2 - 1;
			else
				p[2].X = CaretX + FontWidth - 1;
			p[2].Y = CaretY + FontHeight - 1;
			if (CursorOnDBCS)
				p[3].X = CaretX + FontWidth * 2 - 1;
			else
				p[3].X = CaretX + FontWidth - 1;
			p[3].Y = CaretY;
			p[4].X = CaretX;
			p[4].Y = CaretY;

			if (show) {
				// ポリゴンカーソルを表示（非フォーカス時）
				VTDC.DrawLines(new Pen(ts.VTColor[0]), p);
			}
			else {
				VTDC.DrawLines(new Pen(ts.VTColor[1]), p);
			}

			/* release device context */
			DispReleaseDC();
		}

		// ポリゴンカーソルを消したあとに、その部分の文字を再描画する。
		//
		// CaretOff()の直後に呼ぶこと。CaretOff()内から呼ぶと、無限再帰呼び出しとなり、
		// stack overflowになる。
		//
		// カーソル形状変更時(ChangeCaret)にも呼ぶことにしたため、関数名変更 -- 2009/04/17 doda.
		//
		public void UpdateCaretPosition(bool enforce)
		{
			int CaretX, CaretY;
			Rectangle rc;

			CaretX = (CursorX - WinOrgX) * FontWidth;
			CaretY = (CursorY - WinOrgY) * FontHeight;

			if (!enforce && !ts.KillFocusCursor)
				return;

			if (enforce == true || !Active) {
				if (CursorOnDBCS)
					rc = new Rectangle(CaretX, CaretY, FontWidth * 2, FontHeight);
				else
					rc = new Rectangle(CaretX, CaretY, FontWidth, FontHeight);
				// 指定よりも1ピクセル小さい範囲が再描画されるため
				// rc の right, bottom は1ピクセル大きくしている。
				ttwinman.HVTWin.Invalidate(rc, false);
			}
		}

		public void CaretOn()
		// Turn on the cursor
		{
			int CaretX, CaretY, H;
			IntPtr hImc;
			IntPtr color;
			bool ime_on;

			if (!ts.KillFocusCursor && !Active)
				return;

			/* IMEのon/off状態を見て、カーソルの色を変更する。
			 * WM_INPUTLANGCHANGE, WM_IME_NOTIFY ではカーソルの再描画のみ行う。
			 * (2010.5.20 yutaka)
			 */
			hImc = ttime.ImmGetContext(ttwinman.HVTWin);
			ime_on = ttime.ImmGetOpenStatus(hImc);
			ttime.ImmReleaseContext(ttwinman.HVTWin, hImc);
			if ((ts.WindowFlag & WindowFlags.WF_IMECURSORCHANGE) != 0 && ime_on) {
				color = new IntPtr(1);
			}
			else {
				color = new IntPtr(0);
			}

			CaretX = (CursorX - WinOrgX) * FontWidth;
			CaretY = (CursorY - WinOrgY) * FontHeight;

			if (ttime.CanUseIME() && ts.IMEInline) {
				/* set IME conversion window pos. & font */
				ttime.SetConversionWindow(ttwinman.HVTWin, CaretX, CaretY);
			}

			if (!CaretEnabled) return;

			if (Active) {
				if (ts.CursorShape != CursorShapes.IdVCur) {
					if (ts.CursorShape == CursorShapes.IdHCur) {
						CaretY = CaretY + FontHeight - CurWidth;
						H = CurWidth;
					}
					else {
						H = FontHeight;
					}

					DestroyCaret();
					if (CursorOnDBCS) {
						/* double width caret */
						CreateCaret(ttwinman.HVTWin.Handle, color, FontWidth * 2, H);
					}
					else {
						/* single width caret */
						CreateCaret(ttwinman.HVTWin.Handle, color, FontWidth, H);
					}
					CaretStatus = 1;
				}
				SetCaretPos(CaretX, CaretY);
			}

			while (CaretStatus > 0) {
				if (!Active) {
					CaretKillFocus(true);
				}
				else {
					ShowCaret(ttwinman.HVTWin.Handle);
				}
				CaretStatus--;
			}
		}

		public void CaretOff()
		{
			if (!ts.KillFocusCursor && !Active)
				return;

			if (CaretStatus == 0) {
				if (!Active) {
					CaretKillFocus(false);
				}
				else {
					HideCaret(ttwinman.HVTWin.Handle);
				}
				CaretStatus++;
			}
		}

		public void DispDestroyCaret()
		{
			DestroyCaret();
			if (ts.NonblinkingCursor)
				ttwinman.HVTWin.IdCaretTimer.Enabled = false;
		}

		public bool IsCaretOn()
		// check if caret is on
		{
			// 非アクティブ（フォーカス無効）の場合においても、カーソル描画を行いたいため、
			// 2つめの条件を追加する。(2008.1.24 yutaka)
			if (!ts.KillFocusCursor)
				return ((Active && (CaretStatus == 0)));
			else
				return ((Active && (CaretStatus == 0)) || (!Active && (CaretStatus == 0)));
		}

		public void DispEnableCaret(bool On)
		{
			if (!On) CaretOff();
			CaretEnabled = On;
		}

		public bool IsCaretEnabled()
		{
			return CaretEnabled;
		}

		public void DispSetCaretWidth(bool DW)
		{
			/* true if cursor is on a DBCS character */
			CursorOnDBCS = DW;
		}

		public void DispChangeWinSize(int Nx, int Ny)
		{
			int W, H, dW, dH;
			Rectangle R;

			if (SaveWinSize) {
				WinWidthOld = WinWidth;
				WinHeightOld = WinHeight;
				SaveWinSize = false;
			}
			else {
				WinWidthOld = NumOfColumns;
				WinHeightOld = NumOfLines;
			}

			WinWidth = Nx;
			WinHeight = Ny;

			ScreenWidth = WinWidth * FontWidth;
			ScreenHeight = WinHeight * FontHeight;

			AdjustScrollBar();

			R = ttwinman.HVTWin.Bounds;
			W = R.Right - R.Left;
			H = R.Bottom - R.Top;
			R = ttwinman.HVTWin.ClientRectangle;
			dW = ScreenWidth - R.Right + R.Left;
			dH = ScreenHeight - R.Bottom + R.Top;

			if ((dW != 0) || (dH != 0)) {
				AdjustSize = true;

				// SWP_NOMOVE を指定しているのになぜか 0,0 が反映され、
				// マルチディスプレイ環境ではプライマリモニタに
				// 移動してしまうのを修正 (2008.5.29 maya)
				//SetWindowPos(ttwinman.HVTWin,HWND_TOP,0,0,W+dW,H+dH,SWP_NOMOVE);

				// マルチディスプレイ環境で最大化したときに、
				// 隣のディスプレイにウィンドウの端がはみ出す問題を修正 (2008.5.30 maya)
				// また、上記の状態では最大化状態でもウィンドウを移動させることが出来る。
				if (ttwinman.MainForm != null && ttwinman.MainForm.WindowState != FormWindowState.Maximized) {
					ttwinman.HVTWin.SetBounds(R.Left, R.Top, W + dW, H + dH, BoundsSpecified.Size);
				}
			}
			else
				ttwinman.HVTWin.Invalidate(false);
		}

		public void ResizeWindow(int x, int y, int w, int h, int cw, int ch)
		{
			int dw, dh, NewX, NewY;
			Point Point;

			if (!AdjustSize) return;
			dw = ScreenWidth - cw;
			dh = ScreenHeight - ch;
			if ((dw != 0) || (dh != 0)) {
				ttwinman.HVTWin.SetBounds(x, y, w + dw, h + dh, BoundsSpecified.Size);
				AdjustSize = false;
			}
			else {
				AdjustSize = false;

				NewX = x;
				NewY = y;
				if (x + w > VirtualScreen.Right) {
					NewX = VirtualScreen.Right - w;
					if (NewX < 0) NewX = 0;
				}
				if (y + h > VirtualScreen.Bottom) {
					NewY = VirtualScreen.Bottom - h;
					if (NewY < 0) NewY = 0;
				}
				if ((NewX != x) || (NewY != y)) {
					ttwinman.HVTWin.SetBounds(NewX, NewY, w, h, BoundsSpecified.Location);
				}

				Point = new Point(0, ScreenHeight);
				Point = ttwinman.HVTWin.PointToScreen(Point);
				CompletelyVisible = (Point.Y <= VirtualScreen.Bottom);
				if (IsCaretOn()) CaretOn();
			}
		}

		public void PaintWindow(Graphics PaintDC, Rectangle PaintRect, bool fBkGnd,
			out int Xs, out int Ys, out int Xe, out int Ye)
		//  Paint window with background color &
		//  convert paint region from window coord. to screen coord.
		//  Called from WM_PAINT handler
		//    PaintRect: Paint region in window coordinate
		//    Return:
		//	*Xs, *Ys: upper left corner of the region
		//		    in screen coord.
		//	*Xe, *Ye: lower right
		{
			if (VTDC != null)
				DispReleaseDC();
			VTDC = PaintDC;
			DispInitDC();
			if (fBkGnd)
				VTDC.FillRectangle(Background, PaintRect);

			Xs = PaintRect.Left / FontWidth + WinOrgX;
			Ys = PaintRect.Top / FontHeight + WinOrgY;
			Xe = (PaintRect.Right - 1) / FontWidth + WinOrgX;
			Ye = (PaintRect.Bottom - 1) / FontHeight + WinOrgY;
		}

		public void DispEndPaint()
		{
			if (VTDC == null) return;
			VTDC = null;
		}

		public void DispClearWin()
		{
			ttwinman.HVTWin.Invalidate(false);

			ScrollCount = 0;
			dScroll = 0;
			if (WinHeight > NumOfLines)
				DispChangeWinSize(NumOfColumns, NumOfLines);
			else {
				if ((NumOfLines == WinHeight) && (ts.EnableScrollBuff > 0)) {
					ttwinman.HVTWin.VerticalScroll.Minimum = 0;
					ttwinman.HVTWin.VerticalScroll.Maximum = 1;
				}
				else {
					ttwinman.HVTWin.VerticalScroll.Minimum = 0;
					ttwinman.HVTWin.VerticalScroll.Maximum = NumOfLines - WinHeight;
				}

				ttwinman.HVTWin.HorizontalScroll.Value = 0;
				ttwinman.HVTWin.VerticalScroll.Value = 0;
			}
			if (IsCaretOn()) CaretOn();
		}

		public void DispChangeBackground()
		{
			DispReleaseDC();
			if (Background != null) Background.Dispose();

			if ((CurCharAttr.Attr2 & AttributeBitMasks.Attr2Back) != 0) {
				if (((int)CurCharAttr.Back < 16) && ((int)CurCharAttr.Back & 7) != 0)
					Background = new SolidBrush(ANSIColor[(int)CurCharAttr.Back ^ 8]);
				else
					Background = new SolidBrush(ANSIColor[(int)CurCharAttr.Back]);
			}
			else {
				Background = new SolidBrush(ts.VTColor[1]);
			}

			ttwinman.HVTWin.Invalidate(true);
		}

		public void DispChangeWin()
		{
			/* Change window caption */
			ttwinman.ChangeTitle();

			/* Menu bar / Popup menu */
			ttwinman.SwitchMenu();

			ttwinman.SwitchTitleBar();

			/* Change caret shape */
			ChangeCaret();

			if ((ts.ColorFlag & ColorFlags.CF_USETEXTCOLOR) == 0) {
#if !NO_ANSI_COLOR_EXTENSION
				ANSIColor[(int)ColorCodes.IdFore] = ts.ANSIColor[(int)ColorCodes.IdFore];
				ANSIColor[(int)ColorCodes.IdBack] = ts.ANSIColor[(int)ColorCodes.IdBack];
#else // NO_ANSI_COLOR_EXTENSION
				ANSIColor[(int)ColorCodes.IdFore ]   = Color.FromArgb(255,255,255);
				ANSIColor[(int)ColorCodes.IdBack ]   = Color.FromArgb(  0,  0,  0);
#endif // NO_ANSI_COLOR_EXTENSION
			}
			else { // use text (background) color for "white (black)"
				ANSIColor[(int)ColorCodes.IdFore] = ts.VTColor[0];
				ANSIColor[(int)ColorCodes.IdBack] = ts.VTColor[1];

				ANSIColor[(int)ColorCodes.IdFore] = BGVTColor[0];
				ANSIColor[(int)ColorCodes.IdBack] = BGVTColor[1];
			}

			/* change background color */
			DispChangeBackground();
		}

		Color m_TextColor;
		Color m_BackColor;

		public void DispInitDC()
		{
			if (VTDC == null) {
				VTDC = Graphics.FromHwnd(ttwinman.HVTWin.Handle);
			}

			m_TextColor = ts.VTColor[0];
			m_BackColor = ts.VTColor[1];

			DCAttr = DefCharAttr;
			DCReverse = false;
		}

		public void DispReleaseDC()
		{
			if (VTDC == null) return;
			VTDC = null;
		}

		bool isURLColored(TCharAttr x) { return ((ts.ColorFlag & ColorFlags.CF_URLCOLOR) != 0) && ((x.Attr & AttributeBitMasks.AttrURL) != 0); }
		bool isURLUnderlined(TCharAttr x) { return ((ts.FontFlag & FontFlags.FF_URLUNDERLINE) != 0) && ((x.Attr & AttributeBitMasks.AttrURL) != 0); }
		bool isBoldColored(TCharAttr x) { return ((ts.ColorFlag & ColorFlags.CF_BOLDCOLOR) != 0) && ((x.Attr & AttributeBitMasks.AttrBold) != 0); }
		bool isBlinkColored(TCharAttr x) { return ((ts.ColorFlag & ColorFlags.CF_BLINKCOLOR) != 0) && ((x.Attr & AttributeBitMasks.AttrBlink) != 0); }
		bool isReverseColored(TCharAttr x) { return ((ts.ColorFlag & ColorFlags.CF_REVERSECOLOR) != 0) && ((x.Attr & AttributeBitMasks.AttrReverse) != 0); }
		bool isForeColored(TCharAttr x) { return ((ts.ColorFlag & ColorFlags.CF_ANSICOLOR) != 0) && ((x.Attr2 & AttributeBitMasks.Attr2Fore) != 0); }
		bool isBackColored(TCharAttr x) { return ((ts.ColorFlag & ColorFlags.CF_ANSICOLOR) != 0) && ((x.Attr2 & AttributeBitMasks.Attr2Back) != 0); }

		public void DispSetupDC(TCharAttr Attr, bool Reverse)
		// Setup device context
		//   Attr: character attributes
		//   Reverse: true if text is selected (reversed) by mouse
		{
			Color TextColor, BackColor;
			int NoReverseColor = 2;

			if (VTDC == null) DispInitDC();

			if (TCharAttrCmp(DCAttr, Attr) == 0 && DCReverse == Reverse) {
				return;
			}
			DCAttr = Attr;
			DCReverse = Reverse;

			FontStyle fontStyle = 0;
			if ((Attr.Attr & AttributeBitMasks.AttrBold) != 0)
				fontStyle |= FontStyle.Bold;
			if (((Attr.Attr & AttributeBitMasks.AttrUnder) != 0) || isURLUnderlined(Attr))
				fontStyle |= FontStyle.Underline;
			if ((Attr.Attr & AttributeBitMasks.AttrSpecial) != 0)
				fontStyle |= FontStyle.Italic;

			Font font = new Font(VTFont, fontStyle);

			if ((ts.ColorFlag & ColorFlags.CF_FULLCOLOR) == 0) {
				if (isBlinkColored(Attr)) {
					TextColor = ts.VTBlinkColor[0];
					BackColor = ts.VTBlinkColor[1];
				}
				else if (isBoldColored(Attr)) {
					TextColor = ts.VTBoldColor[0];
					BackColor = ts.VTBoldColor[1];
				}
				/* begin - ishizaki */
				else if (isURLColored(Attr)) {
					TextColor = ts.URLColor[0];
					BackColor = ts.URLColor[1];
				}
				/* end - ishizaki */
				else {
					if (isForeColored(Attr)) {
						TextColor = ANSIColor[(int)Attr.Fore];
					}
					else {
						TextColor = ts.VTColor[0];
						NoReverseColor = 1;
					}

					if (isBackColored(Attr)) {
						BackColor = ANSIColor[(int)Attr.Back];
					}
					else {
						BackColor = ts.VTColor[1];
						if (NoReverseColor == 1) {
							NoReverseColor = (ts.ColorFlag & ColorFlags.CF_REVERSECOLOR) == 0 ? 1 : 0;
						}
					}
				}
			}
			else { // full color
				if (isForeColored(Attr)) {
					if ((int)Attr.Fore < 8 && (ts.ColorFlag & ColorFlags.CF_PCBOLD16) != 0) {
						if (((Attr.Attr & AttributeBitMasks.AttrBold) != 0) == (Attr.Fore != 0)) {
							TextColor = ANSIColor[(int)Attr.Fore];
						}
						else {
							TextColor = ANSIColor[(int)Attr.Fore ^ 8];
						}
					}
					else if ((int)Attr.Fore < 16 && ((int)Attr.Fore & 7) != 0) {
						TextColor = ANSIColor[(int)Attr.Fore ^ 8];
					}
					else {
						TextColor = ANSIColor[(int)Attr.Fore];
					}
				}
				else if (isBlinkColored(Attr))
					TextColor = ts.VTBlinkColor[0];
				else if (isBoldColored(Attr))
					TextColor = ts.VTBoldColor[0];
				else if (isURLColored(Attr))
					TextColor = ts.URLColor[0];
				else {
					TextColor = ts.VTColor[0];
					NoReverseColor = 1;
				}
				if (isBackColored(Attr)) {
					if ((int)Attr.Back < 8 && (ts.ColorFlag & ColorFlags.CF_PCBOLD16) != 0) {
						if (((Attr.Attr & AttributeBitMasks.AttrBlink) != 0) == (Attr.Back != 0)) {
							BackColor = ANSIColor[(int)Attr.Back];
						}
						else {
							BackColor = ANSIColor[(int)Attr.Back ^ 8];
						}
					}
					else if ((int)Attr.Back < 16 && ((int)Attr.Back & 7) != 0) {
						BackColor = ANSIColor[(int)Attr.Back ^ 8];
					}
					else {
						BackColor = ANSIColor[(int)Attr.Back];
					}
				}
				else if (isBlinkColored(Attr))
					BackColor = ts.VTBlinkColor[1];
				else if (isBoldColored(Attr))
					BackColor = ts.VTBoldColor[1];
				else if (isURLColored(Attr))
					BackColor = ts.URLColor[1];
				else {
					BackColor = ts.VTColor[1];
					if (NoReverseColor == 1) {
						NoReverseColor = (ts.ColorFlag & ColorFlags.CF_REVERSECOLOR) == 0 ? 1 : 0;
					}
				}
			}
#if USE_NORMAL_BGCOLOR_REJECT
			if (ts.UseNormalBGColor) {
				BackColor = ts.VTColor[1];
			}
#endif
			if (Reverse != ((Attr.Attr & AttributeBitMasks.AttrReverse) != 0)) {
				if ((Attr.Attr & AttributeBitMasks.AttrReverse) != 0 && (NoReverseColor == 0)) {
					m_TextColor = ts.VTReverseColor[0];
					m_BackColor = ts.VTReverseColor[1];
				}
				else {
					m_TextColor = BackColor;
					m_BackColor = TextColor;
				}
			}
			else {
				m_TextColor = TextColor;
				m_BackColor = BackColor;
			}
		}

		public void DispStr(char[] Buff, AttributeBitMasks[] Attr, int Offset, int Count, int Y, ref int X)
		// Display a string
		//   Buff: points the string
		//   Y: vertical position in window cordinate
		//  *X: horizontal position
		// Return:
		//  *X: horizontal position shifted by the width of the string
		{
			Rectangle RText;
			byte[] mbchar = new byte[32];
			int width = Count;

			for (int i = 0; i < Count; i++) {
				if ((Attr[Offset + i] & AttributeBitMasks.AttrKanji) != 0)
					width++;
			}

			using (Brush bkColor = new SolidBrush(m_BackColor)) {
				VTDC.FillRectangle(bkColor, X, Y, FontWidth * width, FontHeight);
			}

			RText = new Rectangle(X, Y, FontWidth, FontHeight);

			for (int i = 0; i < Count; i++) {
				string s = new string(Buff, Offset + i, 1);
				if ((Attr[Offset + i] & AttributeBitMasks.AttrKanji) != 0) {
					RText.Width = 2 * FontWidth;
					i++;
				}
				else {
					RText.Width = FontWidth;
				}
				TextRenderer.DrawText(VTDC, s, VTFont, RText, m_TextColor,
					TextFormatFlags.HorizontalCenter | TextFormatFlags.NoClipping);
				RText.Offset(RText.Width, 0);
			}

			X = RText.Right + 1;
		}

		public void DispEraseCurToEnd(int YEnd)
		{
			Rectangle R;

			if (VTDC == null) DispInitDC();
			R = new Rectangle(0, (CursorY + 1 - WinOrgY) * FontHeight, ScreenWidth, (YEnd - CursorY) * FontHeight);

			VTDC.FillRectangle(Background, R);

			R.X = (CursorX - WinOrgX) * FontWidth;
			R.Offset(0, -FontHeight);

			VTDC.FillRectangle(Background, R);
		}

		public void DispEraseHomeToCur(int YHome)
		{
			Rectangle R;

			if (VTDC == null) DispInitDC();
			R = new Rectangle(0, (YHome - WinOrgY) * FontHeight, ScreenWidth, (CursorY - YHome) * FontHeight);

			VTDC.FillRectangle(Background, R);
			R = new Rectangle(R.Left, R.Bottom, (CursorX + 1 - WinOrgX) * FontWidth, FontHeight);

			VTDC.FillRectangle(Background, R);
		}

		public void DispEraseCharsInLine(int XStart, int Count)
		{
			Rectangle R;

			if (VTDC == null) DispInitDC();

			R = new Rectangle((XStart - WinOrgX) * FontWidth, (CursorY - WinOrgY) * FontHeight, Count * FontWidth, FontHeight);

			VTDC.FillRectangle(Background, R);
		}

		public bool DispDeleteLines(int Count, int YEnd)
		// return value:
		//	 true  - screen is successfully updated
		//   false - screen is not updated
		{
			Rectangle R;

			if (Active && CompletelyVisible &&
				(YEnd + 1 - WinOrgY <= WinHeight)) {
				R = new Rectangle(0, (CursorY - WinOrgY) * FontHeight, ScreenWidth, (YEnd + 1 - CursorY) * FontHeight);
				ttwinman.HVTWin.ScrollWindow(0, -FontHeight * Count, R, R);
				ttwinman.HVTWin.Update();
				return true;
			}
			else
				return false;
		}

		public bool DispInsertLines(int Count, int YEnd)
		// return value:
		//	 true  - screen is successfully updated
		//   false - screen is not updated
		{
			Rectangle R;

			if (Active && CompletelyVisible &&
				(CursorY >= WinOrgY)) {
				R = new Rectangle(0, (CursorY - WinOrgY) * FontHeight, ScreenWidth, (YEnd + 1 - CursorY) * FontHeight);
				ttwinman.HVTWin.ScrollWindow(0, FontHeight * Count, R, R);
				ttwinman.HVTWin.Update();
				return true;
			}
			else
				return false;
		}

		public bool IsLineVisible(ref int X, ref int Y)
		//  Check the visibility of a line
		//	called from UpdateStr()
		//    *X, *Y: position of a character in the line. screen coord.
		//    Return: true if the line is visible.
		//	*X, *Y:
		//	  If the line is visible
		//	    position of the character in window coord.
		//	  Otherwise
		//	    no change. same as input value.
		{
			if ((dScroll != 0) &&
				(Y >= SRegionTop) &&
				(Y <= SRegionBottom)) {
				Y = Y + dScroll;
				if ((Y < SRegionTop) || (Y > SRegionBottom))
					return false;
			}

			if ((Y < WinOrgY) ||
				(Y >= WinOrgY + WinHeight))
				return false;

			/* screen coordinate . window coordinate */
			X = (X - WinOrgX) * FontWidth;
			Y = (Y - WinOrgY) * FontHeight;
			return true;
		}

		//-------------- scrolling functions --------------------

		void AdjustScrollBar() /* called by ChangeWindowSize() */
		{
			int XRange, YRange;
			int ScrollPosX, ScrollPosY;

			if (NumOfColumns - WinWidth > 0)
				XRange = NumOfColumns - WinWidth;
			else
				XRange = 0;

			if (BuffEnd - WinHeight > 0)
				YRange = BuffEnd - WinHeight;
			else
				YRange = 0;

			ScrollPosX = ttwinman.HVTWin.HorizontalScroll.Value;
			ScrollPosY = ttwinman.HVTWin.VerticalScroll.Value;
			if (ScrollPosX > XRange)
				ScrollPosX = XRange;
			if (ScrollPosY > YRange)
				ScrollPosY = YRange;

			WinOrgX = ScrollPosX;
			WinOrgY = ScrollPosY - PageStart;
			NewOrgX = WinOrgX;
			NewOrgY = WinOrgY;

			DontChangeSize = true;

			ttwinman.HVTWin.HorizontalScroll.Minimum = 0;
			ttwinman.HVTWin.HorizontalScroll.Maximum = XRange;

			if ((YRange == 0) && (ts.EnableScrollBuff > 0)) {
				ttwinman.HVTWin.VerticalScroll.Minimum = 0;
				ttwinman.HVTWin.VerticalScroll.Maximum = 1;
			}
			else {
				ttwinman.HVTWin.VerticalScroll.Minimum = 0;
				ttwinman.HVTWin.VerticalScroll.Maximum = YRange;
			}

			ttwinman.HVTWin.HorizontalScroll.Value = ScrollPosX;
			ttwinman.HVTWin.VerticalScroll.Value = ScrollPosY;

			DontChangeSize = false;
		}

		public void DispScrollToCursor(int CurX, int CurY)
		{
			if (CurX < NewOrgX)
				NewOrgX = CurX;
			else if (CurX >= NewOrgX + WinWidth)
				NewOrgX = CurX + 1 - WinWidth;

			if (CurY < NewOrgY)
				NewOrgY = CurY;
			else if (CurY >= NewOrgY + WinHeight)
				NewOrgY = CurY + 1 - WinHeight;
		}

		public void DispScrollNLines(int Top, int Bottom, int Direction)
		//  Scroll a region of the window by Direction lines
		//    updates window if necessary
		//  Top: top line of scroll region
		//  Bottom: bottom line
		//  Direction: +: forward, -: backward
		{
			if ((dScroll * Direction < 0) ||
				(dScroll * Direction > 0) &&
				((SRegionTop != Top) ||
				 (SRegionBottom != Bottom)))
				DispUpdateScroll();
			SRegionTop = Top;
			SRegionBottom = Bottom;
			dScroll = dScroll + Direction;
			if (Direction > 0)
				DispCountScroll(Direction);
			else
				DispCountScroll(-Direction);
		}

		public void DispCountScroll(int n)
		{
			ScrollCount = ScrollCount + n;
			if (ScrollCount >= ts.ScrollThreshold) DispUpdateScroll();
		}

		public void DispUpdateScroll()
		{
			int d;
			Rectangle R;

			ScrollCount = 0;

			/* Update partial scroll */
			if (dScroll != 0) {
				d = dScroll * FontHeight;
				R = new Rectangle(0, (SRegionTop - WinOrgY) * FontHeight, ScreenWidth, (SRegionBottom + 1 - SRegionTop) * FontHeight);
				ttwinman.HVTWin.ScrollWindow(0, -d, R, R);

				if ((SRegionTop == 0) && (dScroll > 0)) { // update scroll bar if BuffEnd is changed
					if ((BuffEnd == WinHeight) &&
						(ts.EnableScrollBuff > 0)) {
						ttwinman.HVTWin.VerticalScroll.Minimum = 0;
						ttwinman.HVTWin.VerticalScroll.Maximum = 1;
					}
					else {
						ttwinman.HVTWin.VerticalScroll.Minimum = 0;
						ttwinman.HVTWin.VerticalScroll.Maximum = BuffEnd - WinHeight;
					}
					ttwinman.HVTWin.VerticalScroll.Value = WinOrgY + PageStart;
				}
				dScroll = 0;
			}

			/* Update normal scroll */
			if (NewOrgX < 0) NewOrgX = 0;
			if (NewOrgX > NumOfColumns - WinWidth)
				NewOrgX = NumOfColumns - WinWidth;
			if (NewOrgY < -PageStart) NewOrgY = -PageStart;
			if (NewOrgY > BuffEnd - WinHeight - PageStart)
				NewOrgY = BuffEnd - WinHeight - PageStart;

			/* 最下行でだけ自動スクロールする設定の場合
			   NewOrgYが変化していなくてもバッファ行数が変化するので更新する */
			if (ts.AutoScrollOnlyInBottomLine != 0) {
				if ((BuffEnd == WinHeight) &&
					(ts.EnableScrollBuff > 0)) {
					ttwinman.HVTWin.VerticalScroll.Minimum = 0;
					ttwinman.HVTWin.VerticalScroll.Maximum = 1;
				}
				else {
					ttwinman.HVTWin.VerticalScroll.Minimum = 0;
					ttwinman.HVTWin.VerticalScroll.Maximum = BuffEnd - WinHeight;
				}
				ttwinman.HVTWin.VerticalScroll.Value = NewOrgY + PageStart;
			}

			if ((NewOrgX == WinOrgX) &&
				(NewOrgY == WinOrgY)) return;

			if (NewOrgX == WinOrgX) {
				d = (NewOrgY - WinOrgY) * FontHeight;
				ttwinman.HVTWin.ScrollWindow(0, -d);
			}
			else if (NewOrgY == WinOrgY) {
				d = (NewOrgX - WinOrgX) * FontWidth;
				ttwinman.HVTWin.ScrollWindow(-d, 0);
			}
			else
				ttwinman.HVTWin.Invalidate(true);

			/* Update scroll bars */
			if (NewOrgX != WinOrgX)
				ttwinman.HVTWin.HorizontalScroll.Value = NewOrgX;

			if (ts.AutoScrollOnlyInBottomLine == 0 && NewOrgY != WinOrgY) {
				if ((BuffEnd == WinHeight) &&
					(ts.EnableScrollBuff > 0)) {
					ttwinman.HVTWin.VerticalScroll.Minimum = 0;
					ttwinman.HVTWin.VerticalScroll.Maximum = 1;
				}
				else {
					ttwinman.HVTWin.VerticalScroll.Minimum = 0;
					ttwinman.HVTWin.VerticalScroll.Maximum = BuffEnd - WinHeight;
				}
				ttwinman.HVTWin.VerticalScroll.Value = NewOrgY + PageStart;
			}

			WinOrgX = NewOrgX;
			WinOrgY = NewOrgY;

			if (IsCaretOn()) CaretOn();
		}

		public void DispScrollHomePos()
		{
			NewOrgX = 0;
			NewOrgY = 0;
			DispUpdateScroll();
		}

		public void DispAutoScroll(Point p)
		{
			int X, Y;

			X = (p.X + FontWidth / 2) / FontWidth;
			Y = p.Y / FontHeight;
			if (X < 0)
				NewOrgX = WinOrgX + X;
			else if (X >= WinWidth)
				NewOrgX = NewOrgX + X - WinWidth + 1;
			if (Y < 0)
				NewOrgY = WinOrgY + Y;
			else if (Y >= WinHeight)
				NewOrgY = NewOrgY + Y - WinHeight + 1;

			DispUpdateScroll();
		}

		public void DispHScroll(ScrollType Func, int Pos)
		{
			switch (Func) {
			case ScrollType.SCROLL_BOTTOM:
				NewOrgX = NumOfColumns - WinWidth;
				break;
			case ScrollType.SCROLL_LINEDOWN: NewOrgX = WinOrgX + 1; break;
			case ScrollType.SCROLL_LINEUP: NewOrgX = WinOrgX - 1; break;
			case ScrollType.SCROLL_PAGEDOWN:
				NewOrgX = WinOrgX + WinWidth - 1;
				break;
			case ScrollType.SCROLL_PAGEUP:
				NewOrgX = WinOrgX - WinWidth + 1;
				break;
			case ScrollType.SCROLL_POS: NewOrgX = Pos; break;
			case ScrollType.SCROLL_TOP: NewOrgX = 0; break;
			}
			DispUpdateScroll();
		}

		public void DispVScroll(ScrollType Func, int Pos)
		{
			switch (Func) {
			case ScrollType.SCROLL_BOTTOM:
				NewOrgY = BuffEnd - WinHeight - PageStart;
				break;
			case ScrollType.SCROLL_LINEDOWN: NewOrgY = WinOrgY + 1; break;
			case ScrollType.SCROLL_LINEUP: NewOrgY = WinOrgY - 1; break;
			case ScrollType.SCROLL_PAGEDOWN:
				NewOrgY = WinOrgY + WinHeight - 1;
				break;
			case ScrollType.SCROLL_PAGEUP:
				NewOrgY = WinOrgY - WinHeight + 1;
				break;
			case ScrollType.SCROLL_POS: NewOrgY = Pos - PageStart; break;
			case ScrollType.SCROLL_TOP: NewOrgY = -PageStart; break;
			}
			DispUpdateScroll();
		}

		//-------------- end of scrolling functions --------

		void DispSetupFontDlg()
		//  Popup the Setup Font dialogbox and
		//  reset window
		{
			bool Ok;

			ts.VTFlag = 1;
			if (!ttdialog.LoadTTDLG()) return;
			Ok = ttdialog.ChooseFontDlg(ttwinman.HVTWin, VTFont, ts);
			ttdialog.FreeTTDLG();
			if (!Ok) return;

			ts.VTFont = new Font(VTFont, FontStyle.Regular);

			ChangeFont();

			DispChangeWinSize(WinWidth, WinHeight);

			ChangeCaret();
		}

		public void DispRestoreWinSize()
		//  Restore window size by double clik on caption bar
		{
			if (ts.TermIsWin) return;

			if ((WinWidth == NumOfColumns) && (WinHeight == NumOfLines)) {
				if (WinWidthOld > NumOfColumns)
					WinWidthOld = NumOfColumns;
				if (WinHeightOld > BuffEnd)
					WinHeightOld = BuffEnd;
				DispChangeWinSize(WinWidthOld, WinHeightOld);
			}
			else {
				SaveWinSize = true;
				DispChangeWinSize(NumOfColumns, NumOfLines);
			}
		}

		public void DispSetWinPos()
		{
			int CaretX, CaretY;
			Point Point;
			Rectangle R;

			R = ttwinman.HVTWin.Bounds;
			ts.VTPos.X = R.Left;
			ts.VTPos.Y = R.Top;

			if (ttime.CanUseIME() && ts.IMEInline) {
				CaretX = (CursorX - WinOrgX) * FontWidth;
				CaretY = (CursorY - WinOrgY) * FontHeight;
				/* set IME conversion window pos. */
				ttime.SetConversionWindow(ttwinman.HVTWin, CaretX, CaretY);
			}

			Point = new Point(0, ScreenHeight);
			Point = ttwinman.HVTWin.PointToScreen(Point);
			CompletelyVisible = (Point.Y <= VirtualScreen.Bottom);
		}

		public void DispMoveWindow(int x, int y)
		{
			ttwinman.HVTWin.SetBounds(x, y, 0, 0, BoundsSpecified.Location);
			DispSetWinPos();
			return;
		}

		public void DispSetActive(bool ActiveFlag)
		{
			Active = ActiveFlag;
			if (Active) {
				if (IsCaretOn()) {
					CaretKillFocus(false);
					// アクティブ時は無条件に再描画する
					UpdateCaretPosition(true);
				}

				ttwinman.HVTWin.Focus();
				ttwinman.ActiveWin = WindowId.IdVT;
			}
			else {
				if (ttime.CanUseIME()) {
					/* position & font of conv. window -> default */
					ttime.SetConversionWindow(ttwinman.HVTWin, -1, 0);
				}
			}
		}

		public int TCharAttrCmp(TCharAttr a, TCharAttr b)
		{
			if (a.Attr == b.Attr &&
				a.Attr2 == b.Attr2 &&
				a.Fore == b.Fore &&
				a.Back == b.Back) {
				return 0;
			}
			else {
				return 1;
			}
		}

		public void DispSetColor(ANSIColors num, Color color)
		{
			switch (num) {
			case ANSIColors.CS_VT_NORMALFG:
				ts.VTColor[0] = color;
				if ((ts.ColorFlag & ColorFlags.CF_USETEXTCOLOR) != 0) {
					ANSIColor[(byte)ColorCodes.IdFore] = ts.VTColor[0]; // use text color for "white"
				}
				break;
			case ANSIColors.CS_VT_NORMALBG:
				ts.VTColor[1] = color;
				if ((ts.ColorFlag & ColorFlags.CF_USETEXTCOLOR) != 0) {
					ANSIColor[(byte)ColorCodes.IdBack] = ts.VTColor[1]; // use background color for "Black"
				}
				if (ts.UseNormalBGColor) {
					ts.VTBoldColor[1] = ts.VTColor[1];
					ts.VTBlinkColor[1] = ts.VTColor[1];
					ts.URLColor[1] = ts.VTColor[1];
				}
				break;
			case ANSIColors.CS_VT_BOLDFG: ts.VTBoldColor[0] = color; break;
			case ANSIColors.CS_VT_BOLDBG: ts.VTBoldColor[1] = color; break;
			case ANSIColors.CS_VT_BLINKFG: ts.VTBlinkColor[0] = color; break;
			case ANSIColors.CS_VT_BLINKBG: ts.VTBlinkColor[1] = color; break;
			case ANSIColors.CS_VT_REVERSEFG: ts.VTReverseColor[0] = color; break;
			case ANSIColors.CS_VT_REVERSEBG: ts.VTReverseColor[1] = color; break;
			case ANSIColors.CS_VT_URLFG: ts.URLColor[0] = color; break;
			case ANSIColors.CS_VT_URLBG: ts.URLColor[1] = color; break;
			case ANSIColors.CS_TEK_FG: ts.TEKColor[0] = color; break;
			case ANSIColors.CS_TEK_BG: ts.TEKColor[1] = color; break;
			default:
				if ((int)num <= 255) {
					ANSIColor[(int)num] = color;
				}
				else {
					return;
				}
				break;
			}

			UpdateBGBrush();

			if (num == ANSIColors.CS_TEK_FG || num == ANSIColors.CS_TEK_BG) {
				if (ttwinman.HTEKWin != null)
					ttwinman.HTEKWin.Refresh();
			}
			else {
				ttwinman.HTEKWin.Refresh();
			}
		}

		public void DispResetColor(ANSIColors _num)
		{
			int num = (int)_num;

			if (_num == ANSIColors.CS_UNSPEC) {
				return;
			}

			switch (_num) {
			case ANSIColors.CS_TEK_FG:
				break;
			case ANSIColors.CS_TEK_BG:
				break;
			case ANSIColors.CS_ANSICOLOR_ALL:
				InitColorTable();
				DispSetNearestColors(0, 255, null);
				break;
			case ANSIColors.CS_SP_ALL:
				BGVTBoldColor[0] = ts.VTBoldColor[0];
				BGVTBlinkColor[0] = ts.VTBlinkColor[0];
				BGVTReverseColor[1] = ts.VTReverseColor[1];
				break;
			case ANSIColors.CS_ALL:
				// VT color Foreground
				BGVTColor[0] = ts.VTColor[0];
				BGVTBoldColor[0] = ts.VTBoldColor[0];
				BGVTBlinkColor[0] = ts.VTBlinkColor[0];
				BGVTReverseColor[0] = ts.VTReverseColor[0];
				BGURLColor[0] = ts.URLColor[0];

				// VT color Background
				BGVTColor[1] = ts.VTColor[1];
				BGVTReverseColor[1] = ts.VTReverseColor[1];
				if (ts.UseNormalBGColor) {
					BGVTBoldColor[1] = ts.VTColor[1];
					BGVTBlinkColor[1] = ts.VTColor[1];
					BGURLColor[1] = ts.VTColor[1];
				}
				else {
					BGVTBoldColor[1] = ts.VTBoldColor[1];
					BGVTBlinkColor[1] = ts.VTBlinkColor[1];
					BGURLColor[1] = ts.URLColor[1];
				}

				// ANSI Color / xterm 256 color
				InitColorTable();
				DispSetNearestColors(0, 255, null);
				break;
			default:
				if (num == (int)ColorCodes.IdBack) {
					if ((ts.ColorFlag & ColorFlags.CF_USETEXTCOLOR) != 0) {
						ANSIColor[(byte)ColorCodes.IdBack] = ts.VTColor[1]; // use background color for "Black"
					}
					else {
						ANSIColor[(byte)ColorCodes.IdBack] = ts.ANSIColor[(byte)ColorCodes.IdBack];
					}
					DispSetNearestColors(num, num, null);
				}
				else if (num == (int)ColorCodes.IdFore) {
					if ((ts.ColorFlag & ColorFlags.CF_USETEXTCOLOR) != 0) {
						ANSIColor[(byte)ColorCodes.IdFore] = ts.VTColor[0]; // use text color for "white"
					}
					else {
						ANSIColor[(byte)ColorCodes.IdFore] = ts.ANSIColor[(byte)ColorCodes.IdFore];
					}
					DispSetNearestColors(num, num, null);
				}
				else if (num <= 15) {
					ANSIColor[num] = ts.ANSIColor[num];
					DispSetNearestColors(num, num, null);
				}
				else if ((int)num <= 255) {
					ANSIColor[(int)num] = Color.FromArgb(DefaultColorTable[num][0], DefaultColorTable[num][1], DefaultColorTable[num][2]);
					DispSetNearestColors(num, num, null);
				}
				break;
			}

			UpdateBGBrush();

			if (num == (int)ANSIColors.CS_TEK_FG || num == (int)ANSIColors.CS_TEK_BG) {
				if (ttwinman.HTEKWin != null)
					ttwinman.HTEKWin.Refresh();
			}
			else {
				ttwinman.HVTWin.Refresh();
			}
		}

		public Color DispGetColor(ANSIColors num)
		{
			Color color;

			switch (num) {
			case ANSIColors.CS_VT_NORMALFG: color = ts.VTColor[0]; break;
			case ANSIColors.CS_VT_NORMALBG: color = ts.VTColor[1]; break;
			case ANSIColors.CS_VT_BOLDFG: color = ts.VTBoldColor[0]; break;
			case ANSIColors.CS_VT_BOLDBG: color = ts.VTBoldColor[1]; break;
			case ANSIColors.CS_VT_BLINKFG: color = ts.VTBlinkColor[0]; break;
			case ANSIColors.CS_VT_BLINKBG: color = ts.VTBlinkColor[1]; break;
			case ANSIColors.CS_VT_REVERSEFG: color = ts.VTReverseColor[0]; break;
			case ANSIColors.CS_VT_REVERSEBG: color = ts.VTReverseColor[1]; break;
			case ANSIColors.CS_VT_URLFG: color = ts.URLColor[0]; break;
			case ANSIColors.CS_VT_URLBG: color = ts.URLColor[1]; break;
			case ANSIColors.CS_TEK_FG: color = ts.TEKColor[0]; break;
			case ANSIColors.CS_TEK_BG: color = ts.TEKColor[1]; break;
			default:
				if ((int)num <= 255) {
					color = ANSIColor[(int)num];
				}
				else {
					color = ANSIColor[0];
				}
				break;
			}

			return color;
		}

		public void DispSetCurCharAttr(TCharAttr Attr)
		{
			CurCharAttr = Attr;
			UpdateBGBrush();
		}

		void UpdateBGBrush()
		{
			if (Background != null) Background.Dispose();

			if ((CurCharAttr.Attr2 & AttributeBitMasks.Attr2Back) != 0) {
				if (((int)CurCharAttr.Back < 16) && ((int)CurCharAttr.Back & 7) != 0)
					Background = new SolidBrush(ANSIColor[(int)CurCharAttr.Back ^ 8]);
				else
					Background = new SolidBrush(ANSIColor[(int)CurCharAttr.Back]);
			}
			else {
				Background = new SolidBrush(ts.VTColor[1]);
			}
		}

		public void DispShowWindow(WindowType mode)
		{
			switch (mode) {
			case WindowType.WINDOW_MINIMIZE:
				ttwinman.MainForm.WindowState = FormWindowState.Minimized;
				break;
			case WindowType.WINDOW_MAXIMIZE:
				ttwinman.MainForm.WindowState = FormWindowState.Maximized;
				break;
			case WindowType.WINDOW_RESTORE:
				ttwinman.MainForm.WindowState = FormWindowState.Normal;
				break;
			case WindowType.WINDOW_RAISE:
				//ttwinman.HVTWin.BringToFront();
				break;
			case WindowType.WINDOW_LOWER:
				ttwinman.HVTWin.SendToBack();
				break;
			case WindowType.WINDOW_REFRESH:
				ttwinman.HVTWin.Invalidate(false);
				break;
			}
		}

		public void DispResizeWin(int w, int h)
		{
			Rectangle r;

			if (w <= 0 || h <= 0) {
				r = ttwinman.HVTWin.Bounds;
				if (w <= 0) {
					w = r.Right - r.Left;
				}
				if (h <= 0) {
					h = r.Bottom - r.Top;
				}
			}
			ttwinman.HVTWin.SetBounds(0, 0, w, h, BoundsSpecified.Size);
			AdjustSize = false;
		}

		public bool DispWindowIconified()
		{
			return ttwinman.MainForm.WindowState == FormWindowState.Minimized;
		}

		public void DispGetWindowPos(out int x, out int y, bool client)
		{
			if (client) {
				x = ttwinman.HVTWin.Left;
				y = ttwinman.HVTWin.Top;
			}
			else {
				x = ttwinman.HVTWin.Left;
				y = ttwinman.HVTWin.Top;
			}
		}

		public void DispGetWindowSize(out int width, out int height, bool client)
		{
			Rectangle r;

			if (client) {
				r = ttwinman.HVTWin.ClientRectangle;
			}
			else {
				r = ttwinman.HVTWin.Bounds;
			}
			width = r.Right - r.Left;
			height = r.Bottom - r.Top;

			return;
		}

		public void DispGetRootWinSize(out int x, out int y, bool inPixels)
		{
			Screen monitor;
			Rectangle desktop, win, client;

			win = ttwinman.HVTWin.Bounds;
			client = ttwinman.HVTWin.ClientRectangle;

			if (Screen.AllScreens.Length > 1) {
				// マルチモニタがサポートされている場合
				monitor = Screen.FromControl(ttwinman.HVTWin);
				desktop = monitor.WorkingArea;
			}
			else {
				// マルチモニタがサポートされていない場合
				desktop = Screen.PrimaryScreen.WorkingArea;
			}

			x = (desktop.Right - desktop.Left - (win.Right - win.Left - client.Right)) / FontWidth;
			y = (desktop.Bottom - desktop.Top - (win.Bottom - win.Top - client.Bottom)) / FontHeight;

			return;
		}

		public int DispFindClosestColor(int red, int green, int blue)
		{
			int i, color, diff_r, diff_g, diff_b, diff, min;
			//char buff[1024];

			min = 0xfffffff;
			color = 0;

			if (red < 0 || red > 255 || green < 0 || green > 255 || blue < 0 || blue > 255)
				return -1;

			for (i = 0; i < 256; i++) {
				diff_r = red - ANSIColor[i].R;
				diff_g = green - ANSIColor[i].G;
				diff_b = blue - ANSIColor[i].B;
				diff = diff_r * diff_r + diff_g * diff_g + diff_b * diff_b;

				if (diff < min) {
					min = diff;
					color = i;
				}
			}

			if ((ts.ColorFlag & ColorFlags.CF_FULLCOLOR) != 0 && color < 16 && (color & 7) != 0) {
				color ^= 8;
			}
			return color;
		}

		internal void Init(ProgramDatas datas)
		{
			ttwinman = datas.ttwinman;
			ttime = datas.ttime;
			ts = datas.TTTSet;
			cv = datas.TComVar;
		}

		[DllImport("user32.dll")]
		public static extern bool CreateCaret(IntPtr hWnd, IntPtr hbm, int cx, int cy);
		[DllImport("user32.dll")]
		public static extern bool DestroyCaret();
		[DllImport("user32.dll")]
		public static extern bool SetCaretPos(int x, int y);
		[DllImport("user32.dll")]
		public static extern bool ShowCaret(IntPtr hWnd);
		[DllImport("user32.dll")]
		public static extern bool HideCaret(IntPtr hWnd);
		[DllImport("user32.dll")]
		public static extern uint GetCaretBlinkTime();
		[DllImport("user32.dll")]
		public static extern bool SetCaretBlinkTime(uint uMSeconds);
	}
}

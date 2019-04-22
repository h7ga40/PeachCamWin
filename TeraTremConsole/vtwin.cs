/*
 * Copyright (C) 1994-1998 T. Teranishi
 * (C) 2004-2018 TeraTerm Project
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
/* IPv6 modification is Copyright(C) 2000 Jun-ya Kato <kato@win6.jp> */

/* TERATERM.EXE, VT window */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

namespace TeraTrem
{
	public class VTWindow : ScrollableControl
	{
		ttwinman ttwinman;
		teraprn teraprn;
		clipboar clipboar;
		keyboard keyboard;
		ttime ttime;
		TTTSet ts;
		TComVar cv;
		Buffer Buffer;
		VTDisp VTDisp;
		VTTerm VTTerm;

		bool Minimized, FirstPaint;
		/* mouse status */
		bool LButton, MButton, RButton;
		bool DblClk, AfterDblClk, TplClk;
		int DblClkX, DblClkY;

		// "Hold" key status
		bool Hold;

		// ScrollLock key
		bool ScrollLock;

		public VTWindow()
		{
			ProgramDatas datas = new ProgramDatas();
			ttwinman = datas.ttwinman;
			teraprn = datas.teraprn;
			clipboar = datas.clipboar;
			keyboard = datas.keyboard;
			ttime = datas.ttime;
			ts = datas.TTTSet;
			cv = datas.TComVar;
			Buffer = datas.Buffer;
			VTDisp = datas.VTDisp;
			VTTerm = datas.VTTerm;

			ttwinman.Init(datas);
			keyboard.Init(datas);
			ttime.Init(datas);
			Buffer.Init(datas);
			VTDisp.Init(datas);
			VTTerm.Init(datas);

			ttime.LoadIME();

			Cursor = Cursors.IBeam;
			TabStop = true;

			ttcmn.DataReceiveSender = this;

			ttwinman.HVTWin = this;

			ttplug.TTXInit(ts, cv); /* TTPLUG */

			commlib.CommInit(cv);

			ttcmn.StartTeraTerm(ts);

			ts.VTFont = Font;
			ts.SetupFName = "TERATERM.INI";
			ts.KeyCnfFN = "KEYBOARD.CNF";

			ttset.ReadIniFile(ts.SetupFName, ts);

			keyboard.InitKeyboard();
			keyboard.SetKeyMap();

			// コマンドラインでも設定ファイルでも変更しないのでここで初期化 (2008.1.25 maya)
			cv.isSSH = 0;
			cv.TitleRemote = "";

			/* window status */
			VTDisp.AdjustSize = true;
			Minimized = false;
			LButton = false;
			MButton = false;
			RButton = false;
			DblClk = false;
			AfterDblClk = false;
			TplClk = false;
			Hold = false;
			FirstPaint = true;
			ScrollLock = false;  // 初期値は無効 (2006.11.14 yutaka)

			/* Initialize scroll buffer */
			Buffer.InitBuffer();

			VTDisp.InitDisp();
		}

		protected override bool IsInputKey(Keys keyData)
		{
			return true;// base.IsInputKey(keyData);
		}

		public const int WM_SIZE = 0x0005;
		public const int WM_ACTIVATE = 0x0006;

		public const int WM_SETFOCUS = 0x0007;
		public const int WM_KILLFOCUS = 0x0008;

		public const int WM_MOUSEACTIVATE = 0x0021;

		public const int WM_INPUTLANGCHANGE = 0x0051;

		public const int WM_KEYDOWN = 0x0100;
		public const int WM_KEYUP = 0x0101;
		public const int WM_CHAR = 0x0102;

		public const int WM_SYSCHAR = 0x0106;

		public const int WM_IME_STARTCOMPOSITION = 0x010D;
		public const int WM_IME_ENDCOMPOSITION = 0x010E;
		public const int WM_IME_COMPOSITION = 0x010F;
		public const int WM_IME_KEYLAST = 0x010F;
		public const int WM_IME_NOTIFY = 0x0282;

		public const int WM_TIMER = 0x0113;

		public const int WM_MOUSEMOVE = 0x0200;
		public const int WM_LBUTTONDOWN = 0x0201;
		public const int WM_LBUTTONUP = 0x0202;
		public const int WM_LBUTTONDBLCLK = 0x0203;
		public const int WM_RBUTTONDOWN = 0x0204;
		public const int WM_RBUTTONUP = 0x0205;
		public const int WM_RBUTTONDBLCLK = 0x0206;
		public const int WM_MBUTTONDOWN = 0x0207;
		public const int WM_MBUTTONUP = 0x0208;
		public const int WM_MBUTTONDBLCLK = 0x0209;
		public const int WM_MOUSEWHEEL = 0x020A;

		public const int WM_NCLBUTTONDBLCLK = 0x00A3;
		public const int WM_NCRBUTTONDBLCLK = 0x00A6;

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg) {
				case WM_SIZE:
					OnSize((uint)m.WParam.ToInt32(), m.LParam.ToInt32() & 0xFFFF, (int)(((uint)m.LParam.ToInt32()) >> 16));
					break;
				case WM_ACTIVATE:
					OnActivate((uint)(m.WParam.ToInt32() & 0xFFFF), m.LParam, (((uint)m.WParam.ToInt32()) >> 16) != 0);
					break;
				case WM_MOUSEACTIVATE:
					OnMouseActivate(m.WParam, m.LParam.ToInt32() & 0xFFFF, (int)(((uint)m.LParam.ToInt32()) >> 16));
					break;
				case WM_INPUTLANGCHANGE:
					OnIMEInputChange((uint)m.WParam.ToInt32(), m.LParam.ToInt32());
					break;
				case WM_KEYDOWN:
					OnKeyDown((uint)m.WParam.ToInt32(), (uint)m.LParam.ToInt32() & 0xFFFF, (((uint)m.LParam.ToInt32()) >> 16));
					break;
				case WM_KEYUP:
					OnKeyUp((uint)m.WParam.ToInt32(), (uint)m.LParam.ToInt32() & 0xFFFF, (((uint)m.LParam.ToInt32()) >> 16));
					break;
				case WM_CHAR:
					OnChar((uint)m.WParam.ToInt32(), (uint)m.LParam.ToInt32() & 0xFFFF, (((uint)m.LParam.ToInt32()) >> 16));
					break;
				case WM_IME_COMPOSITION:
					OnIMEComposition((uint)m.WParam.ToInt32(), (uint)m.LParam.ToInt32());
					break;
				case WM_IME_NOTIFY:
					OnIMENotify((uint)m.WParam.ToInt32(), m.LParam.ToInt32());
					break;
				case WM_TIMER:
					OnTimer((TimerId)m.WParam.ToInt32());
					break;
				case WM_LBUTTONDBLCLK:
					OnLButtonDblClk((uint)m.WParam.ToInt32(), new Point(m.LParam.ToInt32() & 0xFFFF, (int)(((uint)m.LParam.ToInt32()) >> 16)));
					break;
				case WM_NCLBUTTONDBLCLK:
					OnNcLButtonDblClk((uint)m.WParam.ToInt32(), new Point(m.LParam.ToInt32() & 0xFFFF, (int)(((uint)m.LParam.ToInt32()) >> 16)));
					break;
				case WM_NCRBUTTONDBLCLK:
					OnNcRButtonDown((uint)m.WParam.ToInt32(), new Point(m.LParam.ToInt32() & 0xFFFF, (int)(((uint)m.LParam.ToInt32()) >> 16)));
					break;
			}

			base.WndProc(ref m);

			switch (m.Msg) {
				case WM_KILLFOCUS:
					OnKillFocus2(m.WParam);
					break;
			}
		}

		protected override void OnHandleCreated(EventArgs e)
		{
#if ALPHABLEND_TYPE2
//<!--by AKASI
			if(BGNoFrame && ts.HideTitle > 0) {
				ExStyle  = GetWindowLong(Handle,GWL_EXSTYLE);
				ExStyle &= ~WS_EX_CLIENTEDGE;
				SetWindowLong(Handle,GWL_EXSTYLE,ExStyle);
			}
//-->
#endif
			VTDisp.DispSetActive(true);

			/* Reset TeraTrem */
			VTTerm.ResetTerminal();

			VTDisp.ChangeFont();

			VTDisp.ResetIME();

			Buffer.BuffChangeWinSize(VTDisp.NumOfColumns, VTDisp.NumOfLines);

			ttwinman.ChangeTitle();

			VTDisp.ChangeCaret();
		}

		const int WA_INACTIVE = 0;
		const int WA_ACTIVE = 1;
		const int WA_CLICKACTIVE = 2;

		void OnActivate(uint nState, IntPtr pWndOther, bool bMinimized)
		{
			VTDisp.DispSetActive(nState != WA_INACTIVE);
		}

		public void Activate(bool active)
		{
			VTDisp.DispSetActive(active);
		}

		void OnChar(uint nChar, uint nRepCnt, uint nFlags)
		{
			int i;
			byte[] Code;

			if (!ttwinman.KeybEnabled || (ttwinman.TalkStatus != TalkerMode.IdTalkKeyb)) {
				return;
			}

			if ((ts.MetaKey > 0) && keyboard.AltKey()) {
				PostMessage(Handle, VTWindow.WM_SYSCHAR, new IntPtr(nChar), MAKELONG(nRepCnt, nFlags));
				return;
			}

			if ((ts.Language == Language.IdRussian) && ((byte)nChar >= 128)) {
				nChar = (char)language.RussConv(ts.RussKeyb, ts.RussClient, (byte)nChar);
			}
			Code = Encoding.UTF8.GetBytes(new char[] { (char)nChar });

			for (i = 1; i <= nRepCnt; i++) {
				ttcmn.CommTextOut(cv, Code, Code.Length);
				if (ts.LocalEcho) {
					ttcmn.CommTextEcho(cv, Code, Code.Length);
				}
			}

			/* 最下行でだけ自動スクロールする設定の場合
			   リモートへのキー入力送信でスクロールさせる */
			if (ts.AutoScrollOnlyInBottomLine != 0 && VTDisp.WinOrgY != 0) {
				VTDisp.DispVScroll(ScrollType.SCROLL_BOTTOM, 0);
			}
		}

		private static IntPtr MAKELONG(uint lo, uint hi)
		{
			return new IntPtr((int)((lo & 0xFFFFu) | ((hi & 0xFFFFu) << 16)));
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			//base.OnPaintBackground(e);
		}

		void ButtonUp(bool Paste)
		{
			bool disableBuffEndSelect = false;
			bool pasteRButton = RButton && Paste;
			bool pasteMButton = MButton && Paste;

			/* disable autoscrolling */
			KillTimer(Handle, (int)TimerId.IdScrollTimer);
			ReleaseCapture();

			if (ts.SelectOnlyByLButton && (MButton || RButton)) {
				disableBuffEndSelect = true;
			}

			LButton = false;
			MButton = false;
			RButton = false;
			DblClk = false;
			TplClk = false;
			VTDisp.CaretOn();

			// SelectOnlyByLButton が on で 中・右クリックしたときに
			// バッファが選択状態だったら、選択内容がクリップボードに
			// コピーされてしまう問題を修正 (2007.12.6 maya)
			if (!disableBuffEndSelect) {
				Buffer.BuffEndSelect();
			}

			// added ConfirmPasteMouseRButton (2007.3.17 maya)
			if (pasteRButton && !ts.ConfirmPasteMouseRButton) {
				if (clipboar.CBStartPasteConfirmChange(Handle, false)) {
					clipboar.CBStartPaste(Handle, false, VTTerm.BracketedPasteMode(), 0, null, 0);
					/* 最下行でだけ自動スクロールする設定の場合
					   ペースト処理でスクロールさせる */
					if (ts.AutoScrollOnlyInBottomLine != 0 && VTDisp.WinOrgY != 0) {
						VTDisp.DispVScroll(ScrollType.SCROLL_BOTTOM, 0);
					}
				}
			}
			else if (pasteMButton) {
				if (clipboar.CBStartPasteConfirmChange(Handle, false)) {
					clipboar.CBStartPaste(Handle, false, VTTerm.BracketedPasteMode(), 0, null, 0);
					/* 最下行でだけ自動スクロールする設定の場合
					   ペースト処理でスクロールさせる */
					if (ts.AutoScrollOnlyInBottomLine != 0 && VTDisp.WinOrgY != 0) {
						VTDisp.DispVScroll(ScrollType.SCROLL_BOTTOM, 0);
					}
				}
			}
		}

		void ButtonDown(Point p, TeraTrem.MouseButtons LMR)
		{
			bool mousereport;

			if (mousereport = VTTerm.MouseReport(MouseEvent.IdMouseEventBtnDown, LMR, p.X, p.Y)) {
				return;
			}

			if (AfterDblClk && (LMR == TeraTrem.MouseButtons.IdLeftButton) &&
				(Math.Abs(p.X - DblClkX) <= GetSystemMetrics(SM_CXDOUBLECLK)) &&
				(Math.Abs(p.Y - DblClkY) <= GetSystemMetrics(SM_CYDOUBLECLK))) {
				/* triple click */
				KillTimer(Handle, (int)TimerId.IdDblClkTimer);
				AfterDblClk = false;
				Buffer.BuffTplClk(p.Y);
				LButton = true;
				TplClk = true;
				/* for AutoScrolling */
				SetCapture(Handle);
				SetTimer(Handle, (int)TimerId.IdScrollTimer, 100, IntPtr.Zero);
			}
			else {
				if (!(LButton || MButton || RButton)) {
					bool box = false;

					// select several pages of output from Tera Term window (2005.5.15 yutaka)
					if (LMR == TeraTrem.MouseButtons.IdLeftButton && keyboard.ShiftKey()) {
						Buffer.BuffSeveralPagesSelect(p.X, p.Y);

					}
					else {
						// Select rectangular block with Alt Key.Delete Shift key.(2005.5.15 yutaka)
						if (LMR == TeraTrem.MouseButtons.IdLeftButton && keyboard.AltKey()) {
							box = true;
						}

						// Starting the selection only by a left button.(2007.11.20 maya)
						if (!ts.SelectOnlyByLButton ||
							(ts.SelectOnlyByLButton && LMR == TeraTrem.MouseButtons.IdLeftButton)) {
							Buffer.BuffStartSelect(p.X, p.Y, box);
							TplClk = false;

							/* for AutoScrolling */
							SetCapture(Handle);
							SetTimer(Handle, (int)TimerId.IdScrollTimer, 100, IntPtr.Zero);
						}
					}
				}

				switch (LMR) {
					case TeraTrem.MouseButtons.IdRightButton:
						RButton = true;
						break;
					case TeraTrem.MouseButtons.IdMiddleButton:
						MButton = true;
						break;
					case TeraTrem.MouseButtons.IdLeftButton:
						LButton = true;
						break;
				}
			}
		}

		protected override void OnScroll(ScrollEventArgs se)
		{
			ScrollType Func = 0;

			switch (se.Type) {
				case ScrollEventType.Last:
					Func = ScrollType.SCROLL_BOTTOM;
					break;
				case ScrollEventType.EndScroll:
					break;
				case ScrollEventType.SmallIncrement:
					Func = ScrollType.SCROLL_LINEDOWN;
					break;
				case ScrollEventType.SmallDecrement:
					Func = ScrollType.SCROLL_LINEUP;
					break;
				case ScrollEventType.LargeIncrement:
					Func = ScrollType.SCROLL_PAGEDOWN;
					break;
				case ScrollEventType.LargeDecrement:
					Func = ScrollType.SCROLL_PAGEUP;
					break;
				case ScrollEventType.ThumbPosition:
				case ScrollEventType.ThumbTrack:
					Func = ScrollType.SCROLL_POS;
					break;
				case ScrollEventType.First:
					Func = ScrollType.SCROLL_TOP;
					break;
				default:
					break;
			}

			if (Func != 0) {
				if (se.ScrollOrientation == ScrollOrientation.HorizontalScroll) {
					VTDisp.DispHScroll(Func, se.NewValue);
				}
				else {
					VTDisp.DispVScroll(Func, se.NewValue);
				}
			}

			base.OnScroll(se);
		}

		void OnKeyDown(uint nChar, uint nRepCnt, uint nFlags)
		{
			byte[] KeyState = new byte[256];
			MSG M;

			switch (keyboard.KeyDown(this, (Keys)nChar, (ushort)nRepCnt, (ushort)(nFlags & 0x1ff))) {
				case KeyDownReturnType.KEYDOWN_OTHER:
					break;
				case KeyDownReturnType.KEYDOWN_CONTROL:
					return;
				case KeyDownReturnType.KEYDOWN_COMMOUT:
					/* 最下行でだけ自動スクロールする設定の場合
					   リモートへのキー入力送信でスクロールさせる */
					if (ts.AutoScrollOnlyInBottomLine != 0 && VTDisp.WinOrgY != 0) {
						VTDisp.DispVScroll(ScrollType.SCROLL_BOTTOM, 0);
					}
					return;
			}

			if ((ts.MetaKey > 0) && ((nFlags & 0x2000) != 0)) {
				/* for Ctrl+Alt+Key combination */
				keyboard.GetKeyboardState(KeyState);
				KeyState[(int)Keys.Menu] = 0;
				keyboard.SetKeyboardState(KeyState);
				M = new MSG();
				M.hwnd = Handle;
				M.message = WM_KEYDOWN;
				M.wParam = new IntPtr(nChar);
				M.lParam = new IntPtr((nRepCnt) | ((nFlags & 0xdfff) << 16));
				TranslateMessage(ref M);
			}
			else {
				// ScrollLockキーが点灯している場合は、マウスをクリックしっぱなし状態であると
				// 見なす。すなわち、パージング処理が一時停止する。
				// 当該キーを消灯させると、処理が再開される。(2006.11.14 yutaka)
#if false
				keyboard.GetKeyboardState((PBYTE)KeyState);
				if (KeyState[Keys.Scroll] == 0x81) { // on : scroll locked
					ScrollLock = true;
				} else if (KeyState[Keys.Scroll] == 0x80) { // off : scroll unlocked
					ScrollLock = false;
				} else {
					// do nothing
				}
#endif
			}
		}

		void OnKeyUp(uint nChar, uint nRepCnt, uint nFlags)
		{
			keyboard.KeyUp((Keys)nChar);
		}

		protected override void OnLostFocus(EventArgs e)
		{
			VTDisp.DispDestroyCaret();
			VTTerm.FocusReport(false);
			base.OnLostFocus(e);
		}

		void OnKillFocus2(IntPtr pNewWnd)
		{
			if (VTDisp.IsCaretOn()) {
				VTDisp.CaretKillFocus(true);
			}
		}

		void OnLButtonDblClk(uint nFlags, Point point)
		{
			if (LButton || MButton || RButton) {
				return;
			}

			DblClkX = point.X;
			DblClkY = point.Y;

			if (VTTerm.MouseReport(MouseEvent.IdMouseEventBtnDown, TeraTrem.MouseButtons.IdLeftButton, DblClkX, DblClkY)) {
				return;
			}

			if (Buffer.BuffUrlDblClk(DblClkX, DblClkY)) { // ブラウザ呼び出しの場合は何もしない。 (2005.4.3 yutaka)
				return;
			}

			Buffer.BuffDblClk(DblClkX, DblClkY);

			LButton = true;
			DblClk = true;
			AfterDblClk = true;
			SetTimer(Handle, (int)TimerId.IdDblClkTimer, GetDoubleClickTime(), IntPtr.Zero);

			/* for AutoScrolling */
			SetCapture(Handle);
			SetTimer(Handle, (int)TimerId.IdScrollTimer, 100, IntPtr.Zero);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Left) {
				ButtonDown(e.Location, TeraTrem.MouseButtons.IdLeftButton);
			}
			else if (e.Button == System.Windows.Forms.MouseButtons.Middle) {
				ButtonDown(e.Location, TeraTrem.MouseButtons.IdMiddleButton);
			}
			else if (e.Button == System.Windows.Forms.MouseButtons.Right) {
				ButtonDown(e.Location, TeraTrem.MouseButtons.IdRightButton);
			}
			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Left) {
				VTTerm.MouseReport(MouseEvent.IdMouseEventBtnUp, TeraTrem.MouseButtons.IdLeftButton, e.Location.X, e.Location.Y);

				if (LButton)
					ButtonUp(false);
			}
			else if (e.Button == System.Windows.Forms.MouseButtons.Middle) {
				bool mousereport;

				mousereport = VTTerm.MouseReport(MouseEvent.IdMouseEventBtnUp, TeraTrem.MouseButtons.IdMiddleButton, e.Location.X, e.Location.Y);

				if (MButton) {
					// added DisablePasteMouseMButton (2008.3.2 maya)
					if (ts.DisablePasteMouseMButton || mousereport) {
						ButtonUp(false);
					}
					else {
						ButtonUp(true);
					}
				}
			}
			else if (e.Button == System.Windows.Forms.MouseButtons.Right) {
				bool mousereport;

				mousereport = VTTerm.MouseReport(MouseEvent.IdMouseEventBtnUp, TeraTrem.MouseButtons.IdRightButton, e.Location.X, e.Location.Y);

				if (RButton) {
					// 右ボタン押下でのペーストを禁止する (2005.3.16 yutaka)
					if (ts.DisablePasteMouseRButton || mousereport)
						ButtonUp(false);
					else
						ButtonUp(true);
				}
			}

			base.OnMouseUp(e);
		}

		const int HTCLIENT = 1;
		const int HTCAPTION = 2;

		const int MA_ACTIVATE = 1;
		const int MA_ACTIVATEANDEAT = 2;
		const int MA_NOACTIVATE = 3;
		const int MA_NOACTIVATEANDEAT = 4;

		int OnMouseActivate(IntPtr pDesktopWnd, int nHitTest, int message)
		{
			if ((ts.SelOnActive == 0) &&
				(nHitTest == HTCLIENT))   //disable mouse event for text selection
				return MA_ACTIVATEANDEAT; //     when window is activated
			else
				return MA_ACTIVATE;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			int i;
			bool mousereport;

			mousereport = VTTerm.MouseReport(MouseEvent.IdMouseEventMove, 0, e.Location.X, e.Location.Y);

			if (!(LButton || MButton || RButton)) {
				// マウスカーソル直下にURL文字列があるかを走査する (2005.4.2 yutaka)
				Buffer.BuffChangeSelect(e.Location.X, e.Location.Y, 0);
				return;
			}

			if (!mousereport) {
				if (DblClk)
					i = 2;
				else if (TplClk)
					i = 3;
				else
					i = 1;

				if (!ts.SelectOnlyByLButton ||
					(ts.SelectOnlyByLButton && LButton)) {
					// SelectOnlyByLButton == true のときは、左ボタンダウン時のみ選択する (2007.11.21 maya)
					Buffer.BuffChangeSelect(e.Location.X, e.Location.Y, i);
				}
			}

			base.OnMouseMove(e);
		}

		protected override void OnMove(EventArgs e)
		{
			VTDisp.DispSetWinPos();
			base.OnMove(e);
		}

		const int WHEEL_DELTA = 120;

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			int line, i;
			Point pt = PointToClient(e.Location);

			line = Math.Abs(e.Delta) / WHEEL_DELTA; // ライン数
			if (line < 1) line = 1;

			// 一スクロールあたりの行数に変換する (2008.4.6 yutaka)
			if (line == 1 && ts.MouseWheelScrollLine > 0)
				line *= ts.MouseWheelScrollLine;

			if (!VTTerm.MouseReport(MouseEvent.IdMouseEventWheel, e.Delta < 0 ? TeraTrem.MouseButtons.IdMiddleButton : TeraTrem.MouseButtons.IdLeftButton, pt.X, pt.Y)) {
				if (VTTerm.WheelToCursorMode()) {
					if (e.Delta < 0) {
						keyboard.KeyDown(this, Keys.Down, (ushort)line, (ushort)((int)keyboard.MapVirtualKey((ushort)Keys.Down, 0) | 0x100));
						keyboard.KeyUp(Keys.Down);
					}
					else {
						keyboard.KeyDown(this, Keys.Up, (ushort)line, (ushort)((int)keyboard.MapVirtualKey((ushort)Keys.Up, 0) | 0x100));
						keyboard.KeyUp(Keys.Up);
					}
				}
				else {
					for (i = 0; i < line; i++) {
						if (e.Delta < 0)
							VTDisp.DispVScroll(ScrollType.SCROLL_LINEDOWN, VerticalScroll.Value);
						else
							VTDisp.DispVScroll(ScrollType.SCROLL_LINEUP, VerticalScroll.Value);
					}
				}
			}

			base.OnMouseWheel(e);
		}

		void OnNcLButtonDblClk(uint nHitTest, Point point)
		{
			if (!Minimized && (nHitTest == HTCAPTION)) {
				VTDisp.DispRestoreWinSize();
			}
			else {
				//CFrameWnd.OnNcLButtonDblClk(nHitTest, point);
			}
		}

		void OnNcRButtonDown(uint nHitTest, Point point)
		{
			if ((nHitTest == HTCAPTION) &&
				(ts.HideTitle > 0) &&
				keyboard.AltKey()) {
				ttwinman.MainForm.Close(); /* iconize */
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			int Xs, Ys, Xe, Ye;
			IntPtr hdc = e.Graphics.GetHdc();
			try {
				VTDisp.PaintWindow(hdc, e.ClipRectangle, true, out Xs, out Ys, out Xe, out Ye);
				Buffer.LockBuffer();
				Buffer.BuffUpdateRect(Xs, Ys, Xe, Ye);
				Buffer.UnlockBuffer();
				VTDisp.DispEndPaint();
			}
			finally {
				e.Graphics.ReleaseHdc();
			}
		}

		protected override void OnGotFocus(EventArgs e)
		{
			VTDisp.ChangeCaret();
			VTTerm.FocusReport(true);
			base.OnGotFocus(e);
		}

		const int SIZE_MINIMIZED = 1;
		const int SIZE_MAXIMIZED = 2;

		protected void OnSize(uint nType, int cx, int cy)
		{
			System.Drawing.Rectangle R;
			int w, h;

			Minimized = (nType == SIZE_MINIMIZED);

			if (FirstPaint && Minimized) {
				FirstPaint = false;
				return;
			}
			if (Minimized || VTDisp.DontChangeSize) {
				return;
			}

			if (nType == SIZE_MAXIMIZED) {
				ts.TerminalOldWidth = ts.TerminalWidth;
				ts.TerminalOldHeight = ts.TerminalHeight;
			}

			R = Bounds;
			w = R.Right - R.Left;
			h = R.Bottom - R.Top;
			if (VTDisp.AdjustSize) {
				VTDisp.ResizeWindow(R.Left, R.Top, w, h, cx, cy);
			}
			else {
				if (ts.FontScaling) {
					int NewFontWidth, NewFontHeight;
					bool FontChanged = false;

					NewFontWidth = cx / ts.TerminalWidth;
					NewFontHeight = cy / ts.TerminalHeight;

					if (NewFontWidth - ts.FontDW < 3) {
						NewFontWidth = ts.FontDW + 3;
					}
					if (NewFontWidth != VTDisp.FontWidth) {
						//ts.VTFont = new Font(ts.VTFont.FontFamily, NewFontWidth - ts.FontDW);
						VTDisp.FontWidth = NewFontWidth;
						//FontChanged = true;
					}

					if (NewFontHeight - ts.FontDH < 3) {
						NewFontHeight = ts.FontDH + 3;
					}
					if (NewFontHeight != VTDisp.FontHeight) {
						ts.VTFont = new Font(ts.VTFont.FontFamily, NewFontHeight - ts.FontDH);
						VTDisp.FontHeight = NewFontHeight;
						FontChanged = true;
					}

					w = ts.TerminalWidth;
					h = ts.TerminalHeight;

					if (FontChanged) {
						VTDisp.ChangeFont();
					}
				}
				else {
					w = cx / VTDisp.FontWidth;
					h = cy / VTDisp.FontHeight;
				}

				VTTerm.HideStatusLine();
				Buffer.BuffChangeWinSize(w, h);
			}

#if WINDOW_MAXMIMUM_ENABLED
			if (nType == SIZE_MAXIMIZED) {
				VTDisp.AdjustSize = false;
			}
#endif
		}

		protected override void OnFontChanged(EventArgs e)
		{
			ts.VTFont = Font;
			VTDisp.ChangeFont();
			base.OnFontChanged(e);
		}

		const int MK_LBUTTON = 0x0001;

		private void OnTimer(TimerId nIDEvent)
		{
			Point Point;
			PortTypeId PortType;
			uint T;

			if (nIDEvent == TimerId.IdCaretTimer) {
				if (ts.NonblinkingCursor) {
					T = VTDisp.GetCaretBlinkTime();
					VTDisp.SetCaretBlinkTime(T);
				}
				else {
					KillTimer(Handle, (int)TimerId.IdCaretTimer);
				}
				return;
			}
			else if (nIDEvent == TimerId.IdScrollTimer) {
				Point = MousePosition;
				Point = PointToClient(Point);
				VTDisp.DispAutoScroll(Point);
				if ((Point.X < 0) || (Point.X >= VTDisp.ScreenWidth) ||
					(Point.Y < 0) || (Point.Y >= VTDisp.ScreenHeight)) {
					PostMessage(Handle, WM_MOUSEMOVE, new IntPtr(MK_LBUTTON), MAKELONG((uint)Point.X, (uint)Point.Y));
				}
				return;
			}
			else if (nIDEvent == TimerId.IdCancelConnectTimer) {
				// まだ接続が完了していなければ、ソケットを強制クローズ。
				// CloseSocket()を呼びたいが、ここからは呼べないので、直接Win32APIをコールする。
				if (!cv.Ready) {
					//closesocket(cv.s);
					//cv.s = INVALID_SOCKET;  /* ソケット無効の印を付ける。(2010.8.6 yutaka) */
					//PostMessage(Handle, WM_USER_COMMNOTIFY, 0, FD_CLOSE);
				}
			}

			KillTimer(Handle, (int)nIDEvent);

			switch (nIDEvent) {
				case TimerId.IdDelayTimer:
					cv.CanSend = true;
					break;
				case TimerId.IdProtoTimer:
					filesys.ProtoDlgTimeOut();
					goto case TimerId.IdDblClkTimer;
				case TimerId.IdDblClkTimer:
					AfterDblClk = false;
					break;
				case TimerId.IdComEndTimer:
					if (!commlib.CommCanClose(cv)) {
						// wait if received data remains
						SetTimer(Handle, (int)TimerId.IdComEndTimer, 1, IntPtr.Zero);
						break;
					}
					cv.Ready = false;
					if (cv.TelFlag) {
						telnet.EndTelnet();
					}
					PortType = cv.PortType;
					commlib.CommClose(cv);
					//SetDdeComReady(0);
					if ((PortType == PortTypeId.IdTCPIP) &&
						((ts.PortFlag & PortFlags.PF_BEEPONCONNECT) != 0)) {
						VTTerm.MessageBeep(0);
					}
					if ((PortType == PortTypeId.IdTCPIP) &&
						(ts.AutoWinClose > 0) &&
						Enabled /*&&
						((HTEKWin == null) || IsWindowEnabled(HTEKWin))*/
																		 ) {
						// OnClose();
					}
					else {
						ttwinman.ChangeTitle();
						if (ts.ClearScreenOnCloseConnection) {
							OnEditClearScreen();
						}
					}
					break;
				case TimerId.IdPrnStartTimer:
					teraprn.PrnFileStart();
					break;
				case TimerId.IdPrnProcTimer:
					teraprn.PrnFileDirectProc();
					break;
			}
		}

		void OnIMEComposition(uint wParam, uint lParam)
		{
			string hstr;
			IntPtr lpstr;
			int Len;
			byte[] mbstr;
			int mlen;

			if (ttime.CanUseIME()) {
				hstr = ttime.GetConvString(wParam, lParam);
			}
			else {
				hstr = null;
			}

			if (hstr != null) {
				mbstr = Encoding.Default.GetBytes(hstr);

				// add this string into text buffer of application
				Len = strlen(mbstr);
				if (Len == 1) {
					switch (mbstr[0]) {
						case 0x20:
							if (keyboard.ControlKey()) {
								mbstr[0] = 0; /* Ctrl-Space */
							}
							break;
						case 0x5c: // Ctrl-\ support for NEC-PC98
							if (keyboard.ControlKey()) {
								mbstr[0] = 0x1c;
							}
							break;
					}
				}
				if (ts.LocalEcho) {
					ttcmn.CommTextEcho(cv, mbstr, Len);
				}
				ttcmn.CommTextOut(cv, mbstr, Len);
			}
		}

		private int strlen(byte[] mbstr)
		{
			int result = 0;
			foreach (byte b in mbstr) {
				if (b == 0)
					break;
			}
			return result;
		}

		void OnIMEInputChange(uint wParam, int lParam)
		{
			VTDisp.ChangeCaret();
		}

		const int IMN_SETOPENSTATUS = 0x0008;

		void OnIMENotify(uint wParam, int lParam)
		{
			if (wParam == IMN_SETOPENSTATUS) {
				VTDisp.ChangeCaret();
			}
		}

		void OnEditClearScreen()
		{
			Buffer.LockBuffer();
			Buffer.BuffClearScreen();
			if ((Buffer.StatusLine > 0) && (VTDisp.CursorY == VTDisp.NumOfLines - 1)) {
				Buffer.MoveCursor(0, VTDisp.CursorY);
			}
			else {
				Buffer.MoveCursor(0, 0);
			}
			Buffer.BuffUpdateScroll();
			Buffer.BuffSetCaretWidth();
			Buffer.UnlockBuffer();
		}

		public void Parse(byte[] data)
		{
			VTTerm.Parse(data);
		}

		public event DataReceiveEventHandlear DataReceive {
			add { ttcmn.DataReceive += value; }
			remove { ttcmn.DataReceive -= value; }
		}

		public const int SB_LINELEFT = 0;
		public const int SB_LINERIGHT = 1;
		public const int SB_PAGELEFT = 2;
		public const int SB_PAGERIGHT = 3;
		public const int SB_THUMBPOSITION = 4;
		public const int SB_THUMBTRACK = 5;
		public const int SB_LEFT = 6;
		public const int SB_RIGHT = 7;
		public const int SB_ENDSCROLL = 8;

		public const int SB_LINEUP = 0;
		public const int SB_LINEDOWN = 1;
		public const int SB_PAGEUP = 2;
		public const int SB_PAGEDOWN = 3;
		public const int SB_TOP = 6;
		public const int SB_BOTTOM = 7;

		public const int SIF_TRACKPOS = 0x10;
		public const int SIF_RANGE = 0x1;
		public const int SIF_POS = 0x4;
		public const int SIF_PAGE = 0x2;
		public const int SIF_ALL = SIF_RANGE | SIF_PAGE | SIF_POS | SIF_TRACKPOS;

		[DllImport("user32", SetLastError = true)]
		public static extern bool GetScrollInfo(IntPtr hWnd, int n, ref SCROLLINFO lpScrollInfo);

		[DllImport("user32", SetLastError = true)]
		public static extern bool SetScrollInfo(IntPtr hWnd, int n, ref SCROLLINFO lpScrollInfo, bool Redraw);

		[DllImport("user32")]
		public static extern IntPtr SetTimer(IntPtr hWnd, int uIDEvent, uint uElapse, IntPtr lpTimerFunc);
		[DllImport("user32")]
		public static extern void KillTimer(IntPtr hWnd, int uIDEvent);

		[DllImport("user32")]
		public static extern IntPtr GetCapture();
		[DllImport("user32")]
		public static extern IntPtr SetCapture(IntPtr hwnd);
		[DllImport("user32")]
		public static extern bool ReleaseCapture();

		[DllImport("user32")]
		public static extern bool TranslateMessage([In] ref MSG lpMsg);
		[DllImport("user32")]
		public static extern bool PostMessage(IntPtr Handle, int nMsg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32")]
		public static extern uint GetDoubleClickTime();

		const int SM_CXDOUBLECLK = 36;
		const int SM_CYDOUBLECLK = 37;

		[DllImport("user32")]
		public static extern int GetSystemMetrics(int nIndex);
	}

	class ProgramDatas
	{
		public ttwinman ttwinman;
		public teraprn teraprn;
		public clipboar clipboar;
		public keyboard keyboard;
		public ttime ttime;
		public TTTSet TTTSet;
		public TComVar TComVar;
		public Buffer Buffer;
		public VTDisp VTDisp;
		public VTTerm VTTerm;

		public ProgramDatas()
		{
			ttwinman = new ttwinman();
			teraprn = new teraprn();
			clipboar = new clipboar();
			keyboard = new keyboard();
			ttime = new ttime();
			TTTSet = new TTTSet();
			TComVar = new TComVar();
			Buffer = new Buffer();
			VTDisp = new VTDisp();
			VTTerm = new VTTerm();
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct MSG
	{
		public IntPtr hwnd;
		public uint message;
		public IntPtr wParam;
		public IntPtr lParam;
		public uint time;
		public Point pt;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SCROLLINFO
	{
		public int cbSize;
		public int fMask;
		public int nMin;
		public int nMax;
		public int nPage;
		public int nPos;
		public int nTrackPos;
	}
}

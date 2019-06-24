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

/* TERATERM.EXE, scroll buffer routines */

// URLを強調する（石崎氏パッチ 2005/4/2）
#define URL_EMPHASIS

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;

namespace TeraTrem
{
	class Buffer
	{
		ttwinman ttwinman;
		teraprn teraprn;
		clipboar clipboar;
		VTDisp VTDisp;
		TTTSet ts;
		TComVar cv;

		// スクロールバッファの最大長を拡張 (2004.11.28 yutaka)
		const int BuffYMax = 500000;
		const int BuffSizeMax = BuffYMax * 80;

		// status line
		public int StatusLine;  //0: none 1: shown 
								/* top, bottom, left & right margin */
		public int CursorTop, CursorBottom, CursorLeftM, CursorRightM;
		bool Selected;
		public bool Wrap;

		short[] TabStops = new short[256];
		int NTabStops;

		short BuffLock = 0;
		char[] HCodeBuff;
		AttributeBitMasks[] HAttrBuff;
		AttributeBitMasks[] HAttrBuff2;
		ColorCodes[] HAttrBuffFG;
		ColorCodes[] HAttrBuffBG;

		char[] CodeBuff;  /* Character code buffer */
		AttributeBitMasks[] AttrBuff;  /* Attribute buffer */
		AttributeBitMasks[] AttrBuff2; /* Color attr buffer */
		ColorCodes[] AttrBuffFG; /* Foreground color attr buffer */
		ColorCodes[] AttrBuffBG; /* Background color attr buffer */
		int CodeLine;
		int AttrLine;
		int AttrLine2;
		int AttrLineFG;
		int AttrLineBG;
		int LinePtr;
		int BufferSize;
		int NumOfLinesInBuff;
		int BuffStartAbs, BuffEndAbs;
		Point SelectStart, SelectEnd, SelectEndOld;
		bool BoxSelect;
		Point DblClkStart, DblClkEnd;

		int StrChangeStart, StrChangeCount;

		bool SeveralPageSelect;  // add (2005.5.15 yutaka)

		TCharAttr CurCharAttr;

		char[] SaveCodeBuff = null;
		AttributeBitMasks[] SaveAttrBuff;
		AttributeBitMasks[] SaveAttrBuff2;
		ColorCodes[] SaveAttrBuffFG;
		ColorCodes[] SaveAttrBuffBG;
		int SaveBuffX;
		int SaveBuffY;

		public bool isCursorOnStatusLine() { return StatusLine != 0 && VTDisp.CursorY == VTDisp.NumOfLines - 1; }

		private void memset<T>(T[] buff, int offset, T value, int count)
		{
			for (int i = offset; i < offset + count; i++)
				buff[i] = value;
		}

		private void memmove<T>(T[] dst, int dstOffset, T[] src, int srcOffset, int count)
		{
			if ((src != dst) || (srcOffset > dstOffset))
				for (int i = 0; i < count; i++)
					dst[dstOffset + i] = src[srcOffset + i];
			else
				for (int i = count; i >= 0; i--)
					dst[dstOffset + i] = src[srcOffset + i];
		}

		private int strncmp(char[] string1, int offset, string string2, int count)
		{
			int result = 0;

			for (int i = offset; i < offset + count; i++) {
				result = string1[i] - string2[i];
				if (result != 0)
					break;
			}

			return result;
		}

		int GetLinePtr(int Line)
		{
			int Ptr;

			Ptr = (int)(BuffStartAbs + Line) * (int)(VTDisp.NumOfColumns);
			while (Ptr >= BufferSize) {
				Ptr = Ptr - BufferSize;
			}
			return Ptr;
		}

		int NextLinePtr(int Ptr)
		{
			Ptr = Ptr + (int)VTDisp.NumOfColumns;
			if (Ptr >= BufferSize) {
				Ptr = Ptr - BufferSize;
			}
			return Ptr;
		}

		int PrevLinePtr(int Ptr)
		{
			Ptr = Ptr - (int)VTDisp.NumOfColumns;
			if (Ptr < 0) {
				Ptr = Ptr + BufferSize;
			}
			return Ptr;
		}

		bool ChangeBuffer(int Nx, int Ny)
		{
			char[] HCodeNew;
			AttributeBitMasks[] HAttrNew, HAttr2New;
			ColorCodes[] HAttrFGNew, HAttrBGNew;
			int NewSize;
			int NxCopy, NyCopy, i;
			char[] CodeDest = null;
			AttributeBitMasks[] AttrDest = null, AttrDest2 = null;
			ColorCodes[] AttrDestFG = null, AttrDestBG = null;
			int SrcPtr, DestPtr;
			short LockOld;

			if (Nx > tttypes.TermWidthMax) {
				Nx = tttypes.TermWidthMax;
			}
			if (ts.ScrollBuffMax > BuffYMax) {
				ts.ScrollBuffMax = BuffYMax;
			}
			if (Ny > ts.ScrollBuffMax) {
				Ny = ts.ScrollBuffMax;
			}

			if ((int)Nx * (int)Ny > BuffSizeMax) {
				Ny = BuffSizeMax / Nx;
			}

			NewSize = (int)Nx * (int)Ny;

			HCodeNew = null;
			HAttrNew = null;
			HAttr2New = null;
			HAttrFGNew = null;
			HAttrBGNew = null;

			if ((HCodeNew = GlobalAlloc<char>(GMEM_MOVEABLE, NewSize)) == null || (CodeDest = GlobalLock(HCodeNew)) == null) {
				goto allocate_error;
			}
			if ((HAttrNew = GlobalAlloc<AttributeBitMasks>(GMEM_MOVEABLE, NewSize)) == null || (AttrDest = GlobalLock(HAttrNew)) == null) {
				goto allocate_error;
			}
			if ((HAttr2New = GlobalAlloc<AttributeBitMasks>(GMEM_MOVEABLE, NewSize)) == null || (AttrDest2 = GlobalLock(HAttr2New)) == null) {
				goto allocate_error;
			}
			if ((HAttrFGNew = GlobalAlloc<ColorCodes>(GMEM_MOVEABLE, NewSize)) == null || (AttrDestFG = GlobalLock(HAttrFGNew)) == null) {
				goto allocate_error;
			}
			if ((HAttrBGNew = GlobalAlloc<ColorCodes>(GMEM_MOVEABLE, NewSize)) == null || (AttrDestBG = GlobalLock(HAttrBGNew)) == null) {
				goto allocate_error;
			}

			memset(CodeDest, 0, ' ', NewSize);
			memset(AttrDest, 0, AttributeBitMasks.AttrDefault, NewSize);
			memset(AttrDest2, 0, AttributeBitMasks.AttrDefault, NewSize);
			memset(AttrDestFG, 0, (ColorCodes)AttributeBitMasks.AttrDefaultFG, NewSize);
			memset(AttrDestBG, 0, (ColorCodes)AttributeBitMasks.AttrDefaultBG, NewSize);
			if (HCodeBuff != null) {
				if (VTDisp.NumOfColumns > Nx) {
					NxCopy = Nx;
				}
				else {
					NxCopy = VTDisp.NumOfColumns;
				}

				if (VTDisp.BuffEnd > Ny) {
					NyCopy = Ny;
				}
				else {
					NyCopy = VTDisp.BuffEnd;
				}
				LockOld = BuffLock;
				LockBuffer();
				SrcPtr = GetLinePtr(VTDisp.BuffEnd - NyCopy);
				DestPtr = 0;
				for (i = 1; i <= NyCopy; i++) {
					memmove(CodeDest, DestPtr, CodeBuff, SrcPtr, NxCopy);
					memmove(AttrDest, DestPtr, AttrBuff, SrcPtr, NxCopy);
					memmove(AttrDest2, DestPtr, AttrBuff2, SrcPtr, NxCopy);
					memmove(AttrDestFG, DestPtr, AttrBuffFG, SrcPtr, NxCopy);
					memmove(AttrDestBG, DestPtr, AttrBuffBG, SrcPtr, NxCopy);
					if ((AttrDest[DestPtr + NxCopy - 1] & AttributeBitMasks.AttrKanji) != 0) {
						CodeDest[DestPtr + NxCopy - 1] = ' ';
						AttrDest[DestPtr + NxCopy - 1] ^= AttributeBitMasks.AttrKanji;
					}
					SrcPtr = NextLinePtr(SrcPtr);
					DestPtr = DestPtr + (int)Nx;
				}
				FreeBuffer();
			}
			else {
				LockOld = 0;
				NyCopy = VTDisp.NumOfLines;
				Selected = false;
			}

			if (Selected) {
				SelectStart.Y = SelectStart.Y - VTDisp.BuffEnd + NyCopy;
				SelectEnd.Y = SelectEnd.Y - VTDisp.BuffEnd + NyCopy;
				if (SelectStart.Y < 0) {
					SelectStart = new Point(0, 0);
				}
				if (SelectEnd.Y < 0) {
					SelectEnd = new Point(0, 0);
				}

				Selected = (SelectEnd.Y > SelectStart.Y) ||
					((SelectEnd.Y == SelectStart.Y) && (SelectEnd.X > SelectStart.X));
			}

			HCodeBuff = HCodeNew;
			HAttrBuff = HAttrNew;
			HAttrBuff2 = HAttr2New;
			HAttrBuffFG = HAttrFGNew;
			HAttrBuffBG = HAttrBGNew;
			BufferSize = NewSize;
			NumOfLinesInBuff = Ny;
			BuffStartAbs = 0;
			VTDisp.BuffEnd = NyCopy;

			if (VTDisp.BuffEnd == NumOfLinesInBuff) {
				BuffEndAbs = 0;
			}
			else {
				BuffEndAbs = VTDisp.BuffEnd;
			}

			VTDisp.PageStart = VTDisp.BuffEnd - VTDisp.NumOfLines;

			LinePtr = 0;
			if (LockOld > 0) {
				CodeBuff = GlobalLock(HCodeBuff);
				AttrBuff = GlobalLock(HAttrBuff);
				AttrBuff2 = GlobalLock(HAttrBuff2);
				AttrBuffFG = GlobalLock(HAttrBuffFG);
				AttrBuffBG = GlobalLock(HAttrBuffBG);
				CodeLine = 0;
				AttrLine = 0;
				AttrLine2 = 0;
				AttrLineFG = 0;
				AttrLineBG = 0;
			}
			else {
				GlobalUnlock(HCodeNew);
				GlobalUnlock(HAttrNew);
				GlobalUnlock(HAttr2New);
				GlobalUnlock(HAttrFGNew);
				GlobalUnlock(HAttrBGNew);
			}

			BuffLock = LockOld;

			return true;

		allocate_error:
			if (CodeDest != null) GlobalUnlock(HCodeNew);
			if (AttrDest != null) GlobalUnlock(HAttrNew);
			if (AttrDest2 != null) GlobalUnlock(HAttr2New);
			if (AttrDestFG != null) GlobalUnlock(HAttrFGNew);
			if (AttrDestBG != null) GlobalUnlock(HAttrBGNew);
			if (HCodeNew != null) GlobalFree(HCodeNew);
			if (HAttrNew != null) GlobalFree(HAttrNew);
			if (HAttr2New != null) GlobalFree(HAttr2New);
			if (HAttrFGNew != null) GlobalFree(HAttrFGNew);
			if (HAttrBGNew != null) GlobalFree(HAttrBGNew);
			return false;
		}

		public void InitBuffer()
		{
			int Ny;

			/* setup terminal */
			VTDisp.NumOfColumns = ts.TerminalWidth;
			VTDisp.NumOfLines = ts.TerminalHeight;

			if (VTDisp.NumOfColumns <= 0)
				VTDisp.NumOfColumns = 80;
			else if (VTDisp.NumOfColumns > tttypes.TermWidthMax)
				VTDisp.NumOfColumns = tttypes.TermWidthMax;

			if (VTDisp.NumOfLines <= 0)
				VTDisp.NumOfLines = 24;
			else if (VTDisp.NumOfLines > tttypes.TermHeightMax)
				VTDisp.NumOfLines = tttypes.TermHeightMax;

			/* setup window */
			if (ts.EnableScrollBuff > 0) {
				if (ts.ScrollBuffSize < VTDisp.NumOfLines) {
					ts.ScrollBuffSize = VTDisp.NumOfLines;
				}
				Ny = ts.ScrollBuffSize;
			}
			else {
				Ny = VTDisp.NumOfLines;
			}

			if (!ChangeBuffer(VTDisp.NumOfColumns, Ny)) {
				Application.Exit();
				//PostQuitMessage(0);
			}

			if (ts.EnableScrollBuff > 0) {
				ts.ScrollBuffSize = NumOfLinesInBuff;
			}

			StatusLine = 0;
		}

		void NewLine(int Line)
		{
			LinePtr = GetLinePtr(Line);
			CodeLine = LinePtr;
			AttrLine = LinePtr;
			AttrLine2 = LinePtr;
			AttrLineFG = LinePtr;
			AttrLineBG = LinePtr;
		}

		public void LockBuffer()
		{
			BuffLock++;
			if (BuffLock > 1) {
				return;
			}
			CodeBuff = GlobalLock(HCodeBuff);
			AttrBuff = GlobalLock(HAttrBuff);
			AttrBuff2 = GlobalLock(HAttrBuff2);
			AttrBuffFG = GlobalLock(HAttrBuffFG);
			AttrBuffBG = GlobalLock(HAttrBuffBG);
			NewLine(VTDisp.PageStart + VTDisp.CursorY);
		}

		public void UnlockBuffer()
		{
			if (BuffLock == 0) {
				return;
			}
			BuffLock--;
			if (BuffLock > 0) {
				return;
			}
			if (HCodeBuff != null) {
				GlobalUnlock(HCodeBuff);
			}
			if (HAttrBuff != null) {
				GlobalUnlock(HAttrBuff);
			}
			if (HAttrBuff2 != null) {
				GlobalUnlock(HAttrBuff2);
			}
			if (HAttrBuffFG != null) {
				GlobalUnlock(HAttrBuffFG);
			}
			if (HAttrBuffBG != null) {
				GlobalUnlock(HAttrBuffBG);
			}
		}

		public void FreeBuffer()
		{
			BuffLock = 1;
			UnlockBuffer();
			if (HCodeBuff != null) {
				GlobalFree(HCodeBuff);
				HCodeBuff = null;
			}
			if (HAttrBuff != null) {
				GlobalFree(HAttrBuff);
				HAttrBuff = null;
			}
			if (HAttrBuff2 != null) {
				GlobalFree(HAttrBuff2);
				HAttrBuff2 = null;
			}
			if (HAttrBuffFG != null) {
				GlobalFree(HAttrBuffFG);
				HAttrBuffFG = null;
			}
			if (HAttrBuffBG != null) {
				GlobalFree(HAttrBuffBG);
				HAttrBuffBG = null;
			}
		}

		void BuffAllSelect()
		{
			SelectStart = new Point(0, 0);
			SelectEnd = new Point(0, VTDisp.BuffEnd);
			//	SelectEnd.X = VTDisp.NumOfColumns;
			//	SelectEnd.Y = VTDisp.BuffEnd - 1;
		}

		void BuffScreenSelect()
		{
			int X, Y;
			bool right;
			VTDisp.DispConvWinToScreen(0, 0, out X, out Y, out right);
			SelectStart = new Point(X, Y + VTDisp.PageStart);
			SelectEnd = new Point(0, SelectStart.Y + VTDisp.NumOfLines);
			//	SelectEnd.X = X + VTDisp.NumOfColumns;
			//	SelectEnd.Y = Y + VTDisp.PageStart + VTDisp.NumOfLines - 1;
		}

		void BuffCancelSelection()
		{
			SelectStart = new Point(0, 0);
			SelectEnd = new Point(0, 0);
		}

		public void BuffReset()
		// Reset buffer status. don't update real display
		//   called by ResetTerminal()
		{
			int i;

			/* Cursor */
			NewLine(VTDisp.PageStart);
			VTDisp.WinOrgX = 0;
			VTDisp.WinOrgY = 0;
			VTDisp.NewOrgX = 0;
			VTDisp.NewOrgY = 0;

			/* Top/bottom margin */
			CursorTop = 0;
			CursorBottom = VTDisp.NumOfLines - 1;
			CursorLeftM = 0;
			CursorRightM = VTDisp.NumOfColumns - 1;

			/* Tab stops */
			NTabStops = (VTDisp.NumOfColumns - 1) >> 3;
			for (i = 1; i <= NTabStops; i++) {
				TabStops[i - 1] = (short)(i * 8);
			}

			/* Initialize text selection region */
			SelectStart = new Point(0, 0);
			SelectEnd = SelectStart;
			SelectEndOld = SelectStart;
			Selected = false;

			StrChangeCount = 0;
			Wrap = false;
			StatusLine = 0;

			SeveralPageSelect = false; // yutaka

			/* Alternate Screen Buffer */
			BuffDiscardSavedScreen();
		}

		void BuffScroll(int Count, int Bottom)
		{
			int i, n;
			int SrcPtr, DestPtr;
			int BuffEndOld;

			if (Count > NumOfLinesInBuff) {
				Count = NumOfLinesInBuff;
			}

			DestPtr = GetLinePtr(VTDisp.PageStart + VTDisp.NumOfLines - 1 + Count);
			n = Count;
			if (Bottom < VTDisp.NumOfLines - 1) {
				SrcPtr = GetLinePtr(VTDisp.PageStart + VTDisp.NumOfLines - 1);
				for (i = VTDisp.NumOfLines - 1; i >= Bottom + 1; i--) {
					memmove(CodeBuff, DestPtr, CodeBuff, SrcPtr, VTDisp.NumOfColumns);
					memmove(AttrBuff, DestPtr, AttrBuff, SrcPtr, VTDisp.NumOfColumns);
					memmove(AttrBuff2, DestPtr, AttrBuff2, SrcPtr, VTDisp.NumOfColumns);
					memmove(AttrBuffFG, DestPtr, AttrBuffFG, SrcPtr, VTDisp.NumOfColumns);
					memmove(AttrBuffBG, DestPtr, AttrBuffBG, SrcPtr, VTDisp.NumOfColumns);
					memset(CodeBuff, SrcPtr, ' ', VTDisp.NumOfColumns);
					memset(AttrBuff, SrcPtr, AttributeBitMasks.AttrDefault, VTDisp.NumOfColumns);
					memset(AttrBuff2, SrcPtr, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, VTDisp.NumOfColumns);
					memset(AttrBuffFG, SrcPtr, CurCharAttr.Fore, VTDisp.NumOfColumns);
					memset(AttrBuffBG, SrcPtr, CurCharAttr.Back, VTDisp.NumOfColumns);
					SrcPtr = PrevLinePtr(SrcPtr);
					DestPtr = PrevLinePtr(DestPtr);
					n--;
				}
			}
			for (i = 1; i <= n; i++) {
				memset(CodeBuff, DestPtr, ' ', VTDisp.NumOfColumns);
				memset(AttrBuff, DestPtr, AttributeBitMasks.AttrDefault, VTDisp.NumOfColumns);
				memset(AttrBuff2, DestPtr, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, VTDisp.NumOfColumns);
				memset(AttrBuffFG, DestPtr, CurCharAttr.Fore, VTDisp.NumOfColumns);
				memset(AttrBuffBG, DestPtr, CurCharAttr.Back, VTDisp.NumOfColumns);
				DestPtr = PrevLinePtr(DestPtr);
			}

			BuffEndAbs = BuffEndAbs + Count;
			if (BuffEndAbs >= NumOfLinesInBuff) {
				BuffEndAbs = BuffEndAbs - NumOfLinesInBuff;
			}
			BuffEndOld = VTDisp.BuffEnd;
			VTDisp.BuffEnd = VTDisp.BuffEnd + Count;
			if (VTDisp.BuffEnd >= NumOfLinesInBuff) {
				VTDisp.BuffEnd = NumOfLinesInBuff;
				BuffStartAbs = BuffEndAbs;
			}
			VTDisp.PageStart = VTDisp.BuffEnd - VTDisp.NumOfLines;

			if (Selected) {
				SelectStart.Y = SelectStart.Y - Count + VTDisp.BuffEnd - BuffEndOld;
				SelectEnd.Y = SelectEnd.Y - Count + VTDisp.BuffEnd - BuffEndOld;
				if (SelectStart.Y < 0) {
					SelectStart = new Point(0, 0);
				}
				if (SelectEnd.Y < 0) {
					SelectEnd = new Point(0, 0);
				}
				Selected = (SelectEnd.Y > SelectStart.Y) ||
						   ((SelectEnd.Y == SelectStart.Y) &&
							(SelectEnd.X > SelectStart.X));
			}

			NewLine(VTDisp.PageStart + VTDisp.CursorY);
		}

		void NextLine()
		{
			LinePtr = NextLinePtr(LinePtr);
			CodeLine = LinePtr;
			AttrLine = LinePtr;
			AttrLine2 = LinePtr;
			AttrLineFG = LinePtr;
			AttrLineBG = LinePtr;
		}

		void PrevLine()
		{
			LinePtr = PrevLinePtr(LinePtr);
			CodeLine = LinePtr;
			AttrLine = LinePtr;
			AttrLine2 = LinePtr;
			AttrLineFG = LinePtr;
			AttrLineBG = LinePtr;
		}

		void EraseKanji(int LR)
		{
			// If cursor is on left/right half of a Kanji, erase it.
			//   LR: left(0)/right(1) flag

			if ((VTDisp.CursorX - LR >= 0) &&
				((AttrBuff[AttrLine + VTDisp.CursorX - LR] & AttributeBitMasks.AttrKanji) != 0)) {
				CodeBuff[CodeLine + VTDisp.CursorX - LR] = ' ';
				AttrBuff[AttrLine + VTDisp.CursorX - LR] = CurCharAttr.Attr;
				AttrBuff2[AttrLine2 + VTDisp.CursorX - LR] = CurCharAttr.Attr2;
				AttrBuffFG[AttrLineFG + VTDisp.CursorX - LR] = CurCharAttr.Fore;
				AttrBuffBG[AttrLineBG + VTDisp.CursorX - LR] = CurCharAttr.Back;
				if (VTDisp.CursorX - LR + 1 < VTDisp.NumOfColumns) {
					CodeBuff[CodeLine + VTDisp.CursorX - LR + 1] = ' ';
					AttrBuff[AttrLine + VTDisp.CursorX - LR + 1] = CurCharAttr.Attr;
					AttrBuff2[AttrLine2 + VTDisp.CursorX - LR + 1] = CurCharAttr.Attr2;
					AttrBuffFG[AttrLineFG + VTDisp.CursorX - LR + 1] = CurCharAttr.Fore;
					AttrBuffBG[AttrLineBG + VTDisp.CursorX - LR + 1] = CurCharAttr.Back;
				}
			}
		}

		public void EraseKanjiOnLRMargin(int ptr, int count)
		{
			int i;
			int pos;

			if (count < 1)
				return;

			for (i = 0; i < count; i++) {
				pos = ptr + CursorLeftM - 1;
				if (CursorLeftM > 0 && ((AttrBuff[pos] & AttributeBitMasks.AttrKanji) != 0)) {
					CodeBuff[pos] = ' ';
					AttrBuff[pos] &= ~AttributeBitMasks.AttrKanji;
					pos++;
					CodeBuff[pos] = ' ';
					AttrBuff[pos] &= ~AttributeBitMasks.AttrKanji;
				}
				pos = ptr + CursorRightM;
				if (CursorRightM < VTDisp.NumOfColumns - 1 && ((AttrBuff[pos] & AttributeBitMasks.AttrKanji) != 0)) {
					CodeBuff[pos] = ' ';
					AttrBuff[pos] &= ~AttributeBitMasks.AttrKanji;
					pos++;
					CodeBuff[pos] = ' ';
					AttrBuff[pos] &= ~AttributeBitMasks.AttrKanji;
				}
				ptr = NextLinePtr(ptr);
			}
		}

		public void BuffInsertSpace(int Count)
		// Insert space characters at the current position
		//   Count: Number of characters to be inserted
		{
			int MoveLen;
			int extr = 0;

			if (VTDisp.CursorX < CursorLeftM || VTDisp.CursorX > CursorRightM)
				return;

			NewLine(VTDisp.PageStart + VTDisp.CursorY);

			EraseKanji(1); /* if cursor is on right half of a kanji, erase the kanji */

			if (CursorRightM < VTDisp.NumOfColumns - 1 && ((AttrBuff[AttrLine + CursorRightM] & AttributeBitMasks.AttrKanji) != 0)) {
				CodeBuff[CodeLine + CursorRightM + 1] = ' ';
				AttrBuff[AttrLine + CursorRightM + 1] &= ~AttributeBitMasks.AttrKanji;
				extr = 1;
			}

			if (Count > CursorRightM + 1 - VTDisp.CursorX)
				Count = CursorRightM + 1 - VTDisp.CursorX;

			MoveLen = CursorRightM + 1 - VTDisp.CursorX - Count;

			if (MoveLen > 0) {
				memmove(CodeBuff, CodeLine + VTDisp.CursorX + Count, CodeBuff, CodeLine + VTDisp.CursorX, MoveLen);
				memmove(AttrBuff, AttrLine + VTDisp.CursorX + Count, AttrBuff, AttrLine + VTDisp.CursorX, MoveLen);
				memmove(AttrBuff2, AttrLine2 + VTDisp.CursorX + Count, AttrBuff2, AttrLine2 + VTDisp.CursorX, MoveLen);
				memmove(AttrBuffFG, AttrLineFG + VTDisp.CursorX + Count, AttrBuffFG, AttrLineFG + VTDisp.CursorX, MoveLen);
				memmove(AttrBuffBG, AttrLineBG + VTDisp.CursorX + Count, AttrBuffBG, AttrLineBG + VTDisp.CursorX, MoveLen);
			}
			memset(CodeBuff, CodeLine + VTDisp.CursorX, ' ', Count);
			memset(AttrBuff, AttrLine + VTDisp.CursorX, AttributeBitMasks.AttrDefault, Count);
			memset(AttrBuff2, AttrLine2 + VTDisp.CursorX, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, Count);
			memset(AttrBuffFG, AttrLineFG + VTDisp.CursorX, CurCharAttr.Fore, Count);
			memset(AttrBuffBG, AttrLineBG + VTDisp.CursorX, CurCharAttr.Back, Count);
			/* last char in current line is kanji first? */
			if ((AttrBuff[AttrLine + CursorRightM] & AttributeBitMasks.AttrKanji) != 0) {
				/* then delete it */
				CodeBuff[CodeLine + CursorRightM] = ' ';
				AttrBuff[AttrLine + CursorRightM] &= ~AttributeBitMasks.AttrKanji;
			}
			BuffUpdateRect(VTDisp.CursorX, VTDisp.CursorY, CursorRightM + extr, VTDisp.CursorY);
		}

		public void BuffEraseCurToEnd()
		// Erase characters from cursor to the end of screen
		{
			int TmpPtr;
			int offset;
			int i, YEnd;

			NewLine(VTDisp.PageStart + VTDisp.CursorY);
			EraseKanji(1); /* if cursor is on right half of a kanji, erase the kanji */
			offset = VTDisp.CursorX;
			TmpPtr = GetLinePtr(VTDisp.PageStart + VTDisp.CursorY);
			YEnd = VTDisp.NumOfLines - 1;
			if ((StatusLine > 0) && !isCursorOnStatusLine()) {
				YEnd--;
			}
			for (i = VTDisp.CursorY; i <= YEnd; i++) {
				memset(CodeBuff, TmpPtr + offset, ' ', VTDisp.NumOfColumns - offset);
				memset(AttrBuff, TmpPtr + offset, AttributeBitMasks.AttrDefault, VTDisp.NumOfColumns - offset);
				memset(AttrBuff2, TmpPtr + offset, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, VTDisp.NumOfColumns - offset);
				memset(AttrBuffFG, TmpPtr + offset, CurCharAttr.Fore, VTDisp.NumOfColumns - offset);
				memset(AttrBuffBG, TmpPtr + offset, CurCharAttr.Back, VTDisp.NumOfColumns - offset);
				offset = 0;
				TmpPtr = NextLinePtr(TmpPtr);
			}
			/* update window */
			VTDisp.DispEraseCurToEnd(YEnd);
		}

		public void BuffEraseHomeToCur()
		// Erase characters from home to cursor
		{
			int TmpPtr;
			int offset;
			int i, YHome;

			NewLine(VTDisp.PageStart + VTDisp.CursorY);
			EraseKanji(0); /* if cursor is on left half of a kanji, erase the kanji */
			offset = VTDisp.NumOfColumns;
			if (isCursorOnStatusLine()) {
				YHome = VTDisp.CursorY;
			}
			else {
				YHome = 0;
			}
			TmpPtr = GetLinePtr(VTDisp.PageStart + YHome);
			for (i = YHome; i <= VTDisp.CursorY; i++) {
				if (i == VTDisp.CursorY) {
					offset = VTDisp.CursorX + 1;
				}
				memset(CodeBuff, TmpPtr, ' ', offset);
				memset(AttrBuff, TmpPtr, AttributeBitMasks.AttrDefault, offset);
				memset(AttrBuff2, TmpPtr, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, offset);
				memset(AttrBuffFG, TmpPtr, CurCharAttr.Fore, offset);
				memset(AttrBuffBG, TmpPtr, CurCharAttr.Back, offset);
				TmpPtr = NextLinePtr(TmpPtr);
			}

			/* update window */
			VTDisp.DispEraseHomeToCur(YHome);
		}

		public void BuffInsertLines(int Count, int YEnd)
		// Insert lines at current position
		//   Count: number of lines to be inserted
		//   YEnd: bottom line number of scroll region (screen coordinate)
		{
			int i, linelen;
			int extl = 0, extr = 0;
			int SrcPtr, DestPtr;

			BuffUpdateScroll();

			if (CursorLeftM > 0)
				extl = 1;
			if (CursorRightM < VTDisp.NumOfColumns - 1)
				extr = 1;
			if (extl != 0 || extr != 0)
				EraseKanjiOnLRMargin(GetLinePtr(VTDisp.PageStart + VTDisp.CursorY), YEnd - VTDisp.CursorY + 1);

			SrcPtr = GetLinePtr(VTDisp.PageStart + YEnd - Count) + CursorLeftM;
			DestPtr = GetLinePtr(VTDisp.PageStart + YEnd) + CursorLeftM;
			linelen = CursorRightM - CursorLeftM + 1;
			for (i = YEnd - Count; i >= VTDisp.CursorY; i--) {
				memmove(CodeBuff, DestPtr, CodeBuff, SrcPtr, linelen);
				memmove(AttrBuff, DestPtr, AttrBuff, SrcPtr, linelen);
				memmove(AttrBuff2, DestPtr, AttrBuff2, SrcPtr, linelen);
				memmove(AttrBuffFG, DestPtr, AttrBuffFG, SrcPtr, linelen);
				memmove(AttrBuffBG, DestPtr, AttrBuffBG, SrcPtr, linelen);
				SrcPtr = PrevLinePtr(SrcPtr);
				DestPtr = PrevLinePtr(DestPtr);
			}
			for (i = 1; i <= Count; i++) {
				memset(CodeBuff, DestPtr, ' ', linelen);
				memset(AttrBuff, DestPtr, AttributeBitMasks.AttrDefault, linelen);
				memset(AttrBuff2, DestPtr, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, linelen);
				memset(AttrBuffFG, DestPtr, CurCharAttr.Fore, linelen);
				memset(AttrBuffBG, DestPtr, CurCharAttr.Back, linelen);
				DestPtr = PrevLinePtr(DestPtr);
			}

			if (CursorLeftM > 0 || CursorRightM < VTDisp.NumOfColumns - 1 || !VTDisp.DispInsertLines(Count, YEnd)) {
				BuffUpdateRect(CursorLeftM - extl, VTDisp.CursorY, CursorRightM + extr, YEnd);
			}
		}

		public void BuffEraseCharsInLine(int XStart, int Count)
		// erase characters in the current line
		//  XStart: start position of erasing
		//  Count: number of characters to be erased
		{
			bool LineContinued = false;

			if (ts.EnableContinuedLineCopy && XStart == 0 && ((AttrBuff[AttrLine + 0] & AttributeBitMasks.AttrLineContinued) != 0)) {
				LineContinued = true;
			}

			EraseKanji(1); /* if cursor is on right half of a kanji, erase the kanji */

			NewLine(VTDisp.PageStart + VTDisp.CursorY);
			memset(CodeBuff, CodeLine + XStart, ' ', Count);
			memset(AttrBuff, AttrLine + XStart, AttributeBitMasks.AttrDefault, Count);
			memset(AttrBuff2, AttrLine2 + XStart, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, Count);
			memset(AttrBuffFG, AttrLineFG + XStart, CurCharAttr.Fore, Count);
			memset(AttrBuffBG, AttrLineBG + XStart, CurCharAttr.Back, Count);

			if (ts.EnableContinuedLineCopy) {
				if (LineContinued) {
					BuffLineContinued(true);
				}

				if (XStart + Count >= VTDisp.NumOfColumns) {
					AttrBuff[NextLinePtr(LinePtr)] &= ~AttributeBitMasks.AttrLineContinued;
				}
			}

			VTDisp.DispEraseCharsInLine(XStart, Count);
		}

		public void BuffDeleteLines(int Count, int YEnd)
		// Delete lines from current line
		//   Count: number of lines to be deleted
		//   YEnd: bottom line number of scroll region (screen coordinate)
		{
			int i, linelen;
			int extl = 0, extr = 0;
			int SrcPtr, DestPtr;

			BuffUpdateScroll();

			if (CursorLeftM > 0)
				extl = 1;
			if (CursorRightM < VTDisp.NumOfColumns - 1)
				extr = 1;
			if (extl != 0 || extr != 0)
				EraseKanjiOnLRMargin(GetLinePtr(VTDisp.PageStart + VTDisp.CursorY), YEnd - VTDisp.CursorY + 1);

			SrcPtr = GetLinePtr(VTDisp.PageStart + VTDisp.CursorY + Count) + (int)CursorLeftM;
			DestPtr = GetLinePtr(VTDisp.PageStart + VTDisp.CursorY) + (int)CursorLeftM;
			linelen = CursorRightM - CursorLeftM + 1;
			for (i = VTDisp.CursorY; i <= YEnd - Count; i++) {
				memmove(CodeBuff, DestPtr, CodeBuff, SrcPtr, linelen);
				memmove(AttrBuff, DestPtr, AttrBuff, SrcPtr, linelen);
				memmove(AttrBuff2, DestPtr, AttrBuff2, SrcPtr, linelen);
				memmove(AttrBuffFG, DestPtr, AttrBuffFG, SrcPtr, linelen);
				memmove(AttrBuffBG, DestPtr, AttrBuffBG, SrcPtr, linelen);
				SrcPtr = NextLinePtr(SrcPtr);
				DestPtr = NextLinePtr(DestPtr);
			}
			for (i = YEnd + 1 - Count; i <= YEnd; i++) {
				memset(CodeBuff, DestPtr, ' ', linelen);
				memset(AttrBuff, DestPtr, AttributeBitMasks.AttrDefault, linelen);
				memset(AttrBuff2, DestPtr, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, linelen);
				memset(AttrBuffFG, DestPtr, CurCharAttr.Fore, linelen);
				memset(AttrBuffBG, DestPtr, CurCharAttr.Back, linelen);
				DestPtr = NextLinePtr(DestPtr);
			}

			if (CursorLeftM > 0 || CursorRightM < VTDisp.NumOfColumns - 1 || !VTDisp.DispDeleteLines(Count, YEnd)) {
				BuffUpdateRect(CursorLeftM - extl, VTDisp.CursorY, CursorRightM + extr, YEnd);
			}
		}

		public void BuffDeleteChars(int Count)
		// Delete characters in current line from cursor
		//   Count: number of characters to be deleted
		{
			int MoveLen;
			int extr = 0;

			if (VTDisp.CursorX < CursorLeftM || VTDisp.CursorX > CursorRightM)
				return;

			NewLine(VTDisp.PageStart + VTDisp.CursorY);

			EraseKanji(0); /* if cursor is on left harf of a kanji, erase the kanji */
			EraseKanji(1); /* if cursor on right half... */

			if (CursorRightM < VTDisp.NumOfColumns - 1 && ((AttrBuff[AttrLine + CursorRightM] & AttributeBitMasks.AttrKanji) != 0)) {
				CodeBuff[CodeLine + CursorRightM] = ' ';
				AttrBuff[AttrLine + CursorRightM] &= ~AttributeBitMasks.AttrKanji;
				CodeBuff[CodeLine + CursorRightM + 1] = ' ';
				AttrBuff[AttrLine + CursorRightM + 1] &= ~AttributeBitMasks.AttrKanji;
				extr = 1;
			}

			if (Count > CursorRightM + 1 - VTDisp.CursorX)
				Count = CursorRightM + 1 - VTDisp.CursorX;

			MoveLen = CursorRightM + 1 - VTDisp.CursorX - Count;

			if (MoveLen > 0) {
				memmove(CodeBuff, CodeLine + VTDisp.CursorX, CodeBuff, CodeLine + VTDisp.CursorX + Count, MoveLen);
				memmove(AttrBuff, AttrLine + VTDisp.CursorX, AttrBuff, AttrLine + VTDisp.CursorX + Count, MoveLen);
				memmove(AttrBuff2, AttrLine2 + VTDisp.CursorX, AttrBuff2, AttrLine2 + VTDisp.CursorX + Count, MoveLen);
				memmove(AttrBuffFG, AttrLineFG + VTDisp.CursorX, AttrBuffFG, AttrLineFG + VTDisp.CursorX + Count, MoveLen);
				memmove(AttrBuffBG, AttrLineBG + VTDisp.CursorX, AttrBuffBG, AttrLineBG + VTDisp.CursorX + Count, MoveLen);
			}
			memset(CodeBuff, CodeLine + VTDisp.CursorX + MoveLen, ' ', Count);
			memset(AttrBuff, AttrLine + VTDisp.CursorX + MoveLen, AttributeBitMasks.AttrDefault, Count);
			memset(AttrBuff2, AttrLine2 + VTDisp.CursorX + MoveLen, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, Count);
			memset(AttrBuffFG, AttrLineFG + VTDisp.CursorX + MoveLen, CurCharAttr.Fore, Count);
			memset(AttrBuffBG, AttrLineBG + VTDisp.CursorX + MoveLen, CurCharAttr.Back, Count);

			BuffUpdateRect(VTDisp.CursorX, VTDisp.CursorY, CursorRightM + extr, VTDisp.CursorY);
		}

		public void BuffEraseChars(int Count)
		// Erase characters in current line from cursor
		//   Count: number of characters to be deleted
		{
			NewLine(VTDisp.PageStart + VTDisp.CursorY);

			EraseKanji(0); /* if cursor is on left harf of a kanji, erase the kanji */
			EraseKanji(1); /* if cursor on right half... */

			if (Count > VTDisp.NumOfColumns - VTDisp.CursorX) {
				Count = VTDisp.NumOfColumns - VTDisp.CursorX;
			}
			memset(CodeBuff, CodeLine + VTDisp.CursorX, ' ', Count);
			memset(AttrBuff, AttrLine + VTDisp.CursorX, AttributeBitMasks.AttrDefault, Count);
			memset(AttrBuff2, AttrLine2 + VTDisp.CursorX, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, Count);
			memset(AttrBuffFG, AttrLineFG + VTDisp.CursorX, CurCharAttr.Fore, Count);
			memset(AttrBuffBG, AttrLineBG + VTDisp.CursorX, CurCharAttr.Back, Count);

			/* update window */
			VTDisp.DispEraseCharsInLine(VTDisp.CursorX, Count);
		}

		public void BuffFillWithE()
		// Fill screen with 'E' characters
		{
			int TmpPtr;
			int i;

			TmpPtr = GetLinePtr(VTDisp.PageStart);
			for (i = 0; i <= VTDisp.NumOfLines - 1 - StatusLine; i++) {
				memset(CodeBuff, TmpPtr, 'E', VTDisp.NumOfColumns);
				memset(AttrBuff, TmpPtr, AttributeBitMasks.AttrDefault, VTDisp.NumOfColumns);
				memset(AttrBuff2, TmpPtr, AttributeBitMasks.AttrDefault, VTDisp.NumOfColumns);
				memset(AttrBuffFG, TmpPtr, (ColorCodes)AttributeBitMasks.AttrDefaultFG, VTDisp.NumOfColumns);
				memset(AttrBuffBG, TmpPtr, (ColorCodes)AttributeBitMasks.AttrDefaultBG, VTDisp.NumOfColumns);
				TmpPtr = NextLinePtr(TmpPtr);
			}
			BuffUpdateRect(VTDisp.WinOrgX, VTDisp.WinOrgY, VTDisp.WinOrgX + VTDisp.WinWidth - 1, VTDisp.WinOrgY + VTDisp.WinHeight - 1);
		}

		public void BuffDrawLine(TCharAttr Attr, int Direction, int C)
		{ // IO-8256 terminal
			int Ptr;
			int i, X, Y;

			if (C == 0) {
				return;
			}
			Attr.Attr |= AttributeBitMasks.AttrSpecial;

			switch (Direction) {
			case 3:
			case 4:
				if (Direction == 3) {
					if (VTDisp.CursorY == 0) {
						return;
					}
					Y = VTDisp.CursorY - 1;
				}
				else {
					if (VTDisp.CursorY == VTDisp.NumOfLines - 1 - StatusLine) {
						return;
					}
					Y = VTDisp.CursorY + 1;
				}
				if (VTDisp.CursorX + C > VTDisp.NumOfColumns) {
					C = VTDisp.NumOfColumns - VTDisp.CursorX;
				}
				Ptr = GetLinePtr(VTDisp.PageStart + Y);
				memset(CodeBuff, Ptr + VTDisp.CursorX, 'q', C);
				memset(AttrBuff, Ptr + VTDisp.CursorX, Attr.Attr, C);
				memset(AttrBuff2, Ptr + VTDisp.CursorX, Attr.Attr2, C);
				memset(AttrBuffFG, Ptr + VTDisp.CursorX, Attr.Fore, C);
				memset(AttrBuffBG, Ptr + VTDisp.CursorX, Attr.Back, C);
				BuffUpdateRect(VTDisp.CursorX, Y, VTDisp.CursorX + C - 1, Y);
				break;
			case 5:
			case 6:
				if (Direction == 5) {
					if (VTDisp.CursorX == 0) {
						return;
					}
					X = VTDisp.CursorX - 1;
				}
				else {
					if (VTDisp.CursorX == VTDisp.NumOfColumns - 1) {
						X = VTDisp.CursorX - 1;
					}
					else {
						X = VTDisp.CursorX + 1;
					}
				}
				Ptr = GetLinePtr(VTDisp.PageStart + VTDisp.CursorY);
				if (VTDisp.CursorY + C > VTDisp.NumOfLines - StatusLine) {
					C = VTDisp.NumOfLines - StatusLine - VTDisp.CursorY;
				}
				for (i = 1; i <= C; i++) {
					CodeBuff[Ptr + X] = 'x';
					AttrBuff[Ptr + X] = Attr.Attr;
					AttrBuff2[Ptr + X] = Attr.Attr2;
					AttrBuffFG[Ptr + X] = Attr.Fore;
					AttrBuffBG[Ptr + X] = Attr.Back;
					Ptr = NextLinePtr(Ptr);
				}
				BuffUpdateRect(X, VTDisp.CursorY, X, VTDisp.CursorY + C - 1);
				break;
			}
		}

		public void BuffEraseBox
		  (int XStart, int YStart, int XEnd, int YEnd)
		{
			int C, i;
			int Ptr;

			if (XEnd > VTDisp.NumOfColumns - 1) {
				XEnd = VTDisp.NumOfColumns - 1;
			}
			if (YEnd > VTDisp.NumOfLines - 1 - StatusLine) {
				YEnd = VTDisp.NumOfLines - 1 - StatusLine;
			}
			if (XStart > XEnd) {
				return;
			}
			if (YStart > YEnd) {
				return;
			}
			C = XEnd - XStart + 1;
			Ptr = GetLinePtr(VTDisp.PageStart + YStart);
			for (i = YStart; i <= YEnd; i++) {
				if ((XStart > 0) &&
					((AttrBuff[Ptr + XStart - 1] & AttributeBitMasks.AttrKanji) != 0)) {
					CodeBuff[Ptr + XStart - 1] = ' ';
					AttrBuff[Ptr + XStart - 1] = CurCharAttr.Attr;
					AttrBuff2[Ptr + XStart - 1] = CurCharAttr.Attr2;
					AttrBuffFG[Ptr + XStart - 1] = CurCharAttr.Fore;
					AttrBuffBG[Ptr + XStart - 1] = CurCharAttr.Back;
				}
				if ((XStart + C < VTDisp.NumOfColumns) &&
					((AttrBuff[Ptr + XStart + C - 1] & AttributeBitMasks.AttrKanji) != 0)) {
					CodeBuff[Ptr + XStart + C] = ' ';
					AttrBuff[Ptr + XStart + C] = CurCharAttr.Attr;
					AttrBuff2[Ptr + XStart + C] = CurCharAttr.Attr2;
					AttrBuffFG[Ptr + XStart + C] = CurCharAttr.Fore;
					AttrBuffBG[Ptr + XStart + C] = CurCharAttr.Back;
				}
				memset(CodeBuff, Ptr + XStart, ' ', C);
				memset(AttrBuff, Ptr + XStart, AttributeBitMasks.AttrDefault, C);
				memset(AttrBuff2, Ptr + XStart, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, C);
				memset(AttrBuffFG, Ptr + XStart, CurCharAttr.Fore, C);
				memset(AttrBuffBG, Ptr + XStart, CurCharAttr.Back, C);
				Ptr = NextLinePtr(Ptr);
			}
			BuffUpdateRect(XStart, YStart, XEnd, YEnd);
		}

		public void BuffFillBox(char ch, int XStart, int YStart, int XEnd, int YEnd)
		{
			int Cols, i;
			int Ptr;

			if (XEnd > VTDisp.NumOfColumns - 1) {
				XEnd = VTDisp.NumOfColumns - 1;
			}
			if (YEnd > VTDisp.NumOfLines - 1 - StatusLine) {
				YEnd = VTDisp.NumOfLines - 1 - StatusLine;
			}
			if (XStart > XEnd) {
				return;
			}
			if (YStart > YEnd) {
				return;
			}
			Cols = XEnd - XStart + 1;
			Ptr = GetLinePtr(VTDisp.PageStart + YStart);
			for (i = YStart; i <= YEnd; i++) {
				if ((XStart > 0) &&
					((AttrBuff[Ptr + XStart - 1] & AttributeBitMasks.AttrKanji) != 0)) {
					CodeBuff[Ptr + XStart - 1] = ' ';
					AttrBuff[Ptr + XStart - 1] ^= AttributeBitMasks.AttrKanji;
				}
				if ((XStart + Cols < VTDisp.NumOfColumns) &&
					((AttrBuff[Ptr + XStart + Cols - 1] & AttributeBitMasks.AttrKanji) != 0)) {
					CodeBuff[Ptr + XStart + Cols] = ' ';
				}
				memset(CodeBuff, Ptr + XStart, ch, Cols);
				memset(AttrBuff, Ptr + XStart, CurCharAttr.Attr, Cols);
				memset(AttrBuff2, Ptr + XStart, CurCharAttr.Attr2, Cols);
				memset(AttrBuffFG, Ptr + XStart, CurCharAttr.Fore, Cols);
				memset(AttrBuffBG, Ptr + XStart, CurCharAttr.Back, Cols);
				Ptr = NextLinePtr(Ptr);
			}
			BuffUpdateRect(XStart, YStart, XEnd, YEnd);
		}

		//
		// TODO: 1 origin になってるのを 0 origin に直す
		//
		public void BuffCopyBox(
			int SrcXStart, int SrcYStart, int SrcXEnd, int SrcYEnd, int SrcPage,
			int DstX, int DstY, int DstPage)
		{
			int i, C, L;
			int SPtr, DPtr;

			SrcXStart--;
			SrcYStart--;
			SrcXEnd--;
			SrcYEnd--;
			SrcPage--;
			DstX--;
			DstY--;
			DstPage--;

			if (SrcXEnd > VTDisp.NumOfColumns - 1) {
				SrcXEnd = VTDisp.NumOfColumns - 1;
			}
			if (SrcYEnd > VTDisp.NumOfLines - 1 - StatusLine) {
				SrcYEnd = VTDisp.NumOfColumns - 1;
			}
			if (SrcXStart > SrcXEnd ||
				SrcYStart > SrcYEnd ||
				DstX > VTDisp.NumOfColumns - 1 ||
				DstY > VTDisp.NumOfLines - 1 - StatusLine) {
				return;
			}

			C = SrcXEnd - SrcXStart + 1;
			if (DstX + C > VTDisp.NumOfColumns) {
				C = VTDisp.NumOfColumns - DstX;
			}
			L = SrcYEnd - SrcYStart + 1;
			if (DstY + C > VTDisp.NumOfColumns) {
				C = VTDisp.NumOfColumns - DstX;
			}

			if (SrcXStart > DstX) {
				SPtr = GetLinePtr(VTDisp.PageStart + SrcYStart);
				DPtr = GetLinePtr(VTDisp.PageStart + DstY);
				for (i = 0; i < L; i++) {
					memmove(CodeBuff, DPtr + DstX, CodeBuff, SPtr + SrcXStart, C);
					memmove(AttrBuff, DPtr + DstX, AttrBuff, SPtr + SrcXStart, C);
					memmove(AttrBuff2, DPtr + DstX, AttrBuff2, SPtr + SrcXStart, C);
					memmove(AttrBuffFG, DPtr + DstX, AttrBuffFG, SPtr + SrcXStart, C);
					memmove(AttrBuffBG, DPtr + DstX, AttrBuffBG, SPtr + SrcXStart, C);
					SPtr = NextLinePtr(SPtr);
					DPtr = NextLinePtr(DPtr);
				}
			}
			else if (SrcXStart < DstX) {
				SPtr = GetLinePtr(VTDisp.PageStart + SrcYEnd);
				DPtr = GetLinePtr(VTDisp.PageStart + DstY + L - 1);
				for (i = L; i > 0; i--) {
					memmove(CodeBuff, DPtr + DstX, CodeBuff, SPtr + SrcXStart, C);
					memmove(AttrBuff, DPtr + DstX, AttrBuff, SPtr + SrcXStart, C);
					memmove(AttrBuff2, DPtr + DstX, AttrBuff2, SPtr + SrcXStart, C);
					memmove(AttrBuffFG, DPtr + DstX, AttrBuffFG, SPtr + SrcXStart, C);
					memmove(AttrBuffBG, DPtr + DstX, AttrBuffBG, SPtr + SrcXStart, C);
					SPtr = PrevLinePtr(SPtr);
					DPtr = PrevLinePtr(DPtr);
				}
			}
			else if (SrcYStart != DstY) {
				SPtr = GetLinePtr(VTDisp.PageStart + SrcYStart);
				DPtr = GetLinePtr(VTDisp.PageStart + DstY);
				for (i = 0; i < L; i++) {
					memmove(CodeBuff, DPtr + DstX, CodeBuff, SPtr + SrcXStart, C);
					memmove(AttrBuff, DPtr + DstX, AttrBuff, SPtr + SrcXStart, C);
					memmove(AttrBuff2, DPtr + DstX, AttrBuff2, SPtr + SrcXStart, C);
					memmove(AttrBuffFG, DPtr + DstX, AttrBuffFG, SPtr + SrcXStart, C);
					memmove(AttrBuffBG, DPtr + DstX, AttrBuffBG, SPtr + SrcXStart, C);
					SPtr = NextLinePtr(SPtr);
					DPtr = NextLinePtr(DPtr);
				}
			}
			BuffUpdateRect(DstX, DstY, DstX + C - 1, DstY + L - 1);
		}

		public void BuffChangeAttrBox(int XStart, int YStart, int XEnd, int YEnd, TCharAttr attr, TCharAttr? mask)
		{
			int C, i, j;
			int Ptr;

			if (XEnd > VTDisp.NumOfColumns - 1) {
				XEnd = VTDisp.NumOfColumns - 1;
			}
			if (YEnd > VTDisp.NumOfLines - 1 - StatusLine) {
				YEnd = VTDisp.NumOfLines - 1 - StatusLine;
			}
			if (XStart > XEnd || YStart > YEnd) {
				return;
			}
			C = XEnd - XStart + 1;
			Ptr = GetLinePtr(VTDisp.PageStart + YStart);

			if (mask != null) { // DECCARA
				for (i = YStart; i <= YEnd; i++) {
					j = Ptr + XStart - 1;
					if (XStart > 0 && ((AttrBuff[j] & AttributeBitMasks.AttrKanji) != 0)) {
						AttrBuff[j] = AttrBuff[j] & ~mask.Value.Attr | attr.Attr;
						AttrBuff2[j] = AttrBuff2[j] & ~mask.Value.Attr2 | attr.Attr2;
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Fore) != 0) { AttrBuffFG[j] = attr.Fore; }
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Back) != 0) { AttrBuffBG[j] = attr.Back; }
					}
					while (++j < Ptr + XStart + C) {
						AttrBuff[j] = AttrBuff[j] & ~mask.Value.Attr | attr.Attr;
						AttrBuff2[j] = AttrBuff2[j] & ~mask.Value.Attr2 | attr.Attr2;
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Fore) != 0) { AttrBuffFG[j] = attr.Fore; }
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Back) != 0) { AttrBuffBG[j] = attr.Back; }
					}
					if (XStart + C < VTDisp.NumOfColumns && ((AttrBuff[j - 1] & AttributeBitMasks.AttrKanji) != 0)) {
						AttrBuff[j] = AttrBuff[j] & ~mask.Value.Attr | attr.Attr;
						AttrBuff2[j] = AttrBuff2[j] & ~mask.Value.Attr2 | attr.Attr2;
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Fore) != 0) { AttrBuffFG[j] = attr.Fore; }
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Back) != 0) { AttrBuffBG[j] = attr.Back; }
					}
					Ptr = NextLinePtr(Ptr);
				}
			}
			else { // DECRARA
				for (i = YStart; i <= YEnd; i++) {
					j = Ptr + XStart - 1;
					if (XStart > 0 && ((AttrBuff[j] & AttributeBitMasks.AttrKanji) != 0)) {
						AttrBuff[j] ^= attr.Attr;
					}
					while (++j < Ptr + XStart + C) {
						AttrBuff[j] ^= attr.Attr;
					}
					if (XStart + C < VTDisp.NumOfColumns && ((AttrBuff[j - 1] & AttributeBitMasks.AttrKanji) != 0)) {
						AttrBuff[j] ^= attr.Attr;
					}
					Ptr = NextLinePtr(Ptr);
				}
			}
			BuffUpdateRect(XStart, YStart, XEnd, YEnd);
		}

		public void BuffChangeAttrStream(int XStart, int YStart, int XEnd, int YEnd, TCharAttr attr, TCharAttr? mask)
		{
			int i, j, endp;
			int Ptr;

			if (XEnd > VTDisp.NumOfColumns - 1) {
				XEnd = VTDisp.NumOfColumns - 1;
			}
			if (YEnd > VTDisp.NumOfLines - 1 - StatusLine) {
				YEnd = VTDisp.NumOfLines - 1 - StatusLine;
			}
			if (XStart > XEnd || YStart > YEnd) {
				return;
			}

			Ptr = GetLinePtr(VTDisp.PageStart + YStart);

			if (mask != null) { // DECCARA
				if (YStart == YEnd) {
					i = Ptr + XStart - 1;
					endp = Ptr + XEnd + 1;

					if (XStart > 0 && ((AttrBuff[i] & AttributeBitMasks.AttrKanji) != 0)) {
						AttrBuff[i] = AttrBuff[i] & ~mask.Value.Attr | attr.Attr;
						AttrBuff2[i] = AttrBuff2[i] & ~mask.Value.Attr2 | attr.Attr2;
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Fore) != 0) { AttrBuffFG[i] = attr.Fore; }
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Back) != 0) { AttrBuffBG[i] = attr.Back; }
					}
					while (++i < endp) {
						AttrBuff[i] = AttrBuff[i] & ~mask.Value.Attr | attr.Attr;
						AttrBuff2[i] = AttrBuff2[i] & ~mask.Value.Attr2 | attr.Attr2;
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Fore) != 0) { AttrBuffFG[i] = attr.Fore; }
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Back) != 0) { AttrBuffBG[i] = attr.Back; }
					}
					if (XEnd < VTDisp.NumOfColumns - 1 && ((AttrBuff[i - 1] & AttributeBitMasks.AttrKanji) != 0)) {
						AttrBuff[i] = AttrBuff[i] & ~mask.Value.Attr | attr.Attr;
						AttrBuff2[i] = AttrBuff2[i] & ~mask.Value.Attr2 | attr.Attr2;
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Fore) != 0) { AttrBuffFG[i] = attr.Fore; }
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Back) != 0) { AttrBuffBG[i] = attr.Back; }
					}
				}
				else {
					i = Ptr + XStart - 1;
					endp = Ptr + VTDisp.NumOfColumns;

					if (XStart > 0 && ((AttrBuff[i] & AttributeBitMasks.AttrKanji) != 0)) {
						AttrBuff[i] = AttrBuff[i] & ~mask.Value.Attr | attr.Attr;
						AttrBuff2[i] = AttrBuff2[i] & ~mask.Value.Attr2 | attr.Attr2;
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Fore) != 0) { AttrBuffFG[i] = attr.Fore; }
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Back) != 0) { AttrBuffBG[i] = attr.Back; }
					}
					while (++i < endp) {
						AttrBuff[i] = AttrBuff[i] & ~mask.Value.Attr | attr.Attr;
						AttrBuff2[i] = AttrBuff2[i] & ~mask.Value.Attr2 | attr.Attr2;
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Fore) != 0) { AttrBuffFG[i] = attr.Fore; }
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Back) != 0) { AttrBuffBG[i] = attr.Back; }
					}

					for (j = 0; j < YEnd - YStart - 1; j++) {
						Ptr = NextLinePtr(Ptr);
						i = Ptr;
						endp = Ptr + VTDisp.NumOfColumns;

						while (i < endp) {
							AttrBuff[i] = AttrBuff[i] & ~mask.Value.Attr | attr.Attr;
							AttrBuff2[i] = AttrBuff2[i] & ~mask.Value.Attr2 | attr.Attr2;
							if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Fore) != 0) { AttrBuffFG[i] = attr.Fore; }
							if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Back) != 0) { AttrBuffBG[i] = attr.Back; }
							i++;
						}
					}

					Ptr = NextLinePtr(Ptr);
					i = Ptr;
					endp = Ptr + XEnd + 1;

					while (i < endp) {
						AttrBuff[i] = AttrBuff[i] & ~mask.Value.Attr | attr.Attr;
						AttrBuff2[i] = AttrBuff2[i] & ~mask.Value.Attr2 | attr.Attr2;
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Fore) != 0) { AttrBuffFG[i] = attr.Fore; }
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Back) != 0) { AttrBuffBG[i] = attr.Back; }
						i++;
					}
					if (XEnd < VTDisp.NumOfColumns - 1 && ((AttrBuff[i - 1] & AttributeBitMasks.AttrKanji) != 0)) {
						AttrBuff[i] = AttrBuff[i] & ~mask.Value.Attr | attr.Attr;
						AttrBuff2[i] = AttrBuff2[i] & ~mask.Value.Attr2 | attr.Attr2;
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Fore) != 0) { AttrBuffFG[i] = attr.Fore; }
						if ((mask.Value.Attr2 & AttributeBitMasks.Attr2Back) != 0) { AttrBuffBG[i] = attr.Back; }
					}
				}
			}
			else { // DECRARA
				if (YStart == YEnd) {
					i = Ptr + XStart - 1;
					endp = Ptr + XEnd + 1;

					if (XStart > 0 && ((AttrBuff[i] & AttributeBitMasks.AttrKanji) != 0)) {
						AttrBuff[i] ^= attr.Attr;
					}
					while (++i < endp) {
						AttrBuff[i] ^= attr.Attr;
					}
					if (XEnd < VTDisp.NumOfColumns - 1 && ((AttrBuff[i - 1] & AttributeBitMasks.AttrKanji) != 0)) {
						AttrBuff[i] ^= attr.Attr;
					}
				}
				else {
					i = Ptr + XStart - 1;
					endp = Ptr + VTDisp.NumOfColumns;

					if (XStart > 0 && ((AttrBuff[i] & AttributeBitMasks.AttrKanji) != 0)) {
						AttrBuff[i] ^= attr.Attr;
					}
					while (++i < endp) {
						AttrBuff[i] ^= attr.Attr;
					}

					for (j = 0; j < YEnd - YStart - 1; j++) {
						Ptr = NextLinePtr(Ptr);
						i = Ptr;
						endp = Ptr + VTDisp.NumOfColumns;

						while (i < endp) {
							AttrBuff[i] ^= attr.Attr;
							i++;
						}
					}

					Ptr = NextLinePtr(Ptr);
					i = Ptr;
					endp = Ptr + XEnd + 1;

					while (i < endp) {
						AttrBuff[i] ^= attr.Attr;
						i++;
					}
					if (XEnd < VTDisp.NumOfColumns - 1 && ((AttrBuff[i - 1] & AttributeBitMasks.AttrKanji) != 0)) {
						AttrBuff[i] ^= attr.Attr;
					}
					Ptr = NextLinePtr(Ptr);
				}
			}
			BuffUpdateRect(0, YStart, VTDisp.NumOfColumns - 1, YEnd);
		}

		int LeftHalfOfDBCS(int Line, int CharPtr)
		// If CharPtr is on the right half of a DBCS character,
		// return pointer to the left half
		//   Line: points to a line in CodeBuff
		//   CharPtr: points to a char
		//   return: points to the left half of the DBCS
		{
			if ((CharPtr > 0) &&
				((AttrBuff[Line + CharPtr - 1] & AttributeBitMasks.AttrKanji) != 0)) {
				CharPtr--;
			}
			return CharPtr;
		}

		int MoveCharPtr(int Line, ref int x, int dx)
		// move character pointer x by dx character unit
		//   in the line specified by Line
		//   Line: points to a line in CodeBuff
		//   x: points to a character in the line
		//   dx: moving distance in character unit (-: left, +: right)
		//      One DBCS character is counted as one character.
		//      The pointer stops at the beginning or the end of line.
		// Output
		//   x: new pointer. x points to a SBCS character or
		//      the left half of a DBCS character.
		//   return: actual moving distance in character unit
		{
			int i;

			x = LeftHalfOfDBCS(Line, x);
			i = 0;
			while (dx != 0) {
				if (dx > 0) { // move right
					if ((AttrBuff[Line + x] & AttributeBitMasks.AttrKanji) != 0) {
						if (x < VTDisp.NumOfColumns - 2) {
							i++;
							x = x + 2;
						}
					}
					else if (x < VTDisp.NumOfColumns - 1) {
						i++;
						x++;
					}
					dx--;
				}
				else { // move left
					if (x > 0) {
						i--;
						x--;
					}
					x = LeftHalfOfDBCS(Line, x);
					dx++;
				}
			}
			return i;
		}

		void BuffCBCopy(bool Table)
		// copy selected text to clipboard
		{
			int MemSize;
			char[] CBPtr;
			int TmpPtr;
			int i, j, k, IStart, IEnd = 0;
			bool Sp, FirstChar;
			char b;
			bool LineContinued, PrevLineContinued;
			LineContinued = false;

			if (ttwinman.TalkStatus == TalkerMode.IdTalkCB) {
				return;
			}
			if (!Selected) {
				return;
			}

			// --- open clipboard and get CB memory
			if (BoxSelect) {
				MemSize = (SelectEnd.X - SelectStart.X + 3) *
						  (SelectEnd.Y - SelectStart.Y + 1) + 1;
			}
			else {
				MemSize = (SelectEnd.Y - SelectStart.Y) *
						  (VTDisp.NumOfColumns + 2) +
						  SelectEnd.X - SelectStart.X + 1;
			}
			CBPtr = clipboar.CBOpen(MemSize);
			if (CBPtr == null) {
				return;
			}

			// --- copy selected text to CB memory
			LockBuffer();

			CBPtr[0] = '\0';
			TmpPtr = GetLinePtr(SelectStart.Y);
			k = 0;
			for (j = SelectStart.Y; j <= SelectEnd.Y; j++) {
				if (BoxSelect) {
					IStart = SelectStart.X;
					IEnd = SelectEnd.X - 1;
				}
				else {
					IStart = 0;
					IEnd = VTDisp.NumOfColumns - 1;
					if (j == SelectStart.Y) {
						IStart = SelectStart.X;
					}
					if (j == SelectEnd.Y) {
						IEnd = SelectEnd.X - 1;
					}
				}
				i = LeftHalfOfDBCS(TmpPtr, IStart);
				if (i != IStart) {
					if (j == SelectStart.Y) {
						IStart = i;
					}
					else {
						IStart = i + 2;
					}
				}

				// exclude right-side space characters
				IEnd = LeftHalfOfDBCS(TmpPtr, IEnd);
				PrevLineContinued = LineContinued;
				LineContinued = false;
				if (ts.EnableContinuedLineCopy && j != SelectEnd.Y && !BoxSelect) {
					int NextTmpPtr = NextLinePtr(TmpPtr);
					if ((AttrBuff[NextTmpPtr] & AttributeBitMasks.AttrLineContinued) != 0) {
						LineContinued = true;
					}
					if (IEnd == VTDisp.NumOfColumns - 1 &&
						(AttrBuff[TmpPtr + IEnd] & AttributeBitMasks.AttrLineContinued) != 0) {
						MoveCharPtr(TmpPtr, ref IEnd, -1);
					}
				}
				if (!LineContinued)
					while ((IEnd > 0) && (CodeBuff[TmpPtr + IEnd] == 0x20)) {
						MoveCharPtr(TmpPtr, ref IEnd, -1);
					}
				if ((IEnd == 0) && (CodeBuff[TmpPtr] == 0x20)) {
					IEnd = -1;
				}
				else if ((AttrBuff[TmpPtr + IEnd] & AttributeBitMasks.AttrKanji) != 0) { /* DBCS first byte? */
					IEnd++;
				}

				Sp = false;
				FirstChar = true;
				i = IStart;
				while (i <= IEnd) {
					b = CodeBuff[TmpPtr + i];
					i++;
					if (!Sp) {
						if ((Table) && (b <= 0x20)) {
							Sp = true;
							b = '\x09';
						}
						if ((b != 0x09) || (!FirstChar) || PrevLineContinued) {
							FirstChar = false;
							CBPtr[k] = b;
							k++;
						}
					}
					else {
						if (b > 0x20) {
							Sp = false;
							FirstChar = false;
							CBPtr[k] = b;
							k++;
						}
					}
				}

				if (!LineContinued)
					if (j < SelectEnd.Y) {
						CBPtr[k] = '\x0d';
						k++;
						CBPtr[k] = '\x0a';
						k++;
					}

				TmpPtr = NextLinePtr(TmpPtr);
			}
			CBPtr[k] = '\0';
			LineContinued = false;
			if (ts.EnableContinuedLineCopy && j != SelectEnd.Y && !BoxSelect && j < VTDisp.BuffEnd - 1) {
				int NextTmpPtr = NextLinePtr(TmpPtr);
				if ((AttrBuff[NextTmpPtr] & AttributeBitMasks.AttrLineContinued) != 0) {
					LineContinued = true;
				}
				if (IEnd == VTDisp.NumOfColumns - 1 &&
					(AttrBuff[TmpPtr + IEnd] & AttributeBitMasks.AttrLineContinued) != 0) {
					MoveCharPtr(TmpPtr, ref IEnd, -1);
				}
			}
			if (!LineContinued)
				UnlockBuffer();

			// --- send CB memory to clipboard
			clipboar.CBClose();
			return;
		}

		public void BuffPrint(bool ScrollRegion)
		// Print screen or selected text
		{
			TeraPrnId Id;
			Point PrintStart, PrintEnd;
			TCharAttr CurAttr, TempAttr;
			int i, j, count;
			int IStart, IEnd;
			int TmpPtr;

			TempAttr = VTDisp.DefCharAttr;

			if (ScrollRegion) {
				Id = teraprn.VTPrintInit(TeraPrnId.IdPrnScrollRegion);
			}
			else if (Selected) {
				Id = teraprn.VTPrintInit(TeraPrnId.IdPrnScreen | TeraPrnId.IdPrnSelectedText);
			}
			else {
				Id = teraprn.VTPrintInit(TeraPrnId.IdPrnScreen);
			}
			if (Id == TeraPrnId.IdPrnCancel) {
				return;
			}

			/* set print region */
			if (Id == TeraPrnId.IdPrnSelectedText) {
				/* print selected region */
				PrintStart = SelectStart;
				PrintEnd = SelectEnd;
			}
			else if (Id == TeraPrnId.IdPrnScrollRegion) {
				/* print scroll region */
				PrintStart = new Point(0, VTDisp.PageStart + CursorTop);
				PrintEnd = new Point(VTDisp.NumOfColumns, VTDisp.PageStart + CursorBottom);
			}
			else {
				/* print current screen */
				PrintStart = new Point(0, VTDisp.PageStart);
				PrintEnd = new Point(VTDisp.NumOfColumns, VTDisp.PageStart + VTDisp.NumOfLines - 1);
			}
			if (PrintEnd.Y > VTDisp.BuffEnd - 1) {
				PrintEnd.Y = VTDisp.BuffEnd - 1;
			}

			LockBuffer();

			TmpPtr = GetLinePtr(PrintStart.Y);
			for (j = PrintStart.Y; j <= PrintEnd.Y; j++) {
				if (j == PrintStart.Y) {
					IStart = PrintStart.X;
				}
				else {
					IStart = 0;
				}
				if (j == PrintEnd.Y) {
					IEnd = PrintEnd.X - 1;
				}
				else {
					IEnd = VTDisp.NumOfColumns - 1;
				}

				while ((IEnd >= IStart) &&
					   (CodeBuff[TmpPtr + IEnd] == 0x20) &&
					   (AttrBuff[TmpPtr + IEnd] == AttributeBitMasks.AttrDefault) &&
					   (AttrBuff2[TmpPtr + IEnd] == AttributeBitMasks.AttrDefault)) {
					IEnd--;
				}

				i = IStart;
				while (i <= IEnd) {
					CurAttr.Attr = AttrBuff[TmpPtr + i] & ~AttributeBitMasks.AttrKanji;
					CurAttr.Attr2 = AttrBuff2[TmpPtr + i];
					CurAttr.Fore = AttrBuffFG[TmpPtr + i];
					CurAttr.Back = AttrBuffBG[TmpPtr + i];

					count = 1;
					while ((i + count <= IEnd) &&
						   (CurAttr.Attr == (AttrBuff[TmpPtr + i + count] & ~AttributeBitMasks.AttrKanji)) &&
						   (CurAttr.Attr2 == AttrBuff2[TmpPtr + i + count]) &&
						   (CurAttr.Fore == AttrBuffFG[TmpPtr + i + count]) &&
						   (CurAttr.Back == AttrBuffBG[TmpPtr + i + count]) ||
						   (i + count < VTDisp.NumOfColumns) &&
						   ((AttrBuff[TmpPtr + i + count - 1] & AttributeBitMasks.AttrKanji) != 0)) {
						count++;
					}

					if (VTDisp.TCharAttrCmp(CurAttr, TempAttr) != 0) {
						teraprn.PrnSetAttr(CurAttr);
						TempAttr = CurAttr;
					}
					teraprn.PrnOutText(CodeBuff, TmpPtr + i, count);
					i = i + count;
				}
				teraprn.PrnNewLine();
				TmpPtr = NextLinePtr(TmpPtr);
			}

			UnlockBuffer();
			teraprn.VTPrintEnd();
		}

		public void BuffDumpCurrentLine(char TERM)
		// Dumps current line to the file (for path through printing)
		//   HFile: file handle
		//   TERM: terminator character
		//	= LF or VT or FF
		{
			int i, j;

			i = VTDisp.NumOfColumns;
			while ((i > 0) && (CodeBuff[CodeLine + i - 1] == 0x20)) {
				i--;
			}
			for (j = 0; j < i; j++) {
				teraprn.WriteToPrnFile(CodeBuff[CodeLine + j], false);
			}
			teraprn.WriteToPrnFile('\0', true);
			if ((TERM >= (byte)ControlCharacters.LF) && (TERM <= (byte)ControlCharacters.FF)) {
				teraprn.WriteToPrnFile('\x0d', false);
				teraprn.WriteToPrnFile(TERM, true);
			}
		}

#if URL_EMPHASIS
		// RFC3986(Uniform Resource Identifier (URI): Generic Syntax)に準拠する
		// by sakura editor 1.5.2.1: etc_uty.cpp
		static byte[] url_char = {
		  /* +0  +1  +2  +3  +4  +5  +6  +7  +8  +9  +A  +B  +C  +D  +E  +F */
			  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,	/* +00: */
			  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,	/* +10: */
			  0,  1,  0,  1,  1,  1,  1,  0,  0,  0,  0,  1,  1,  1,  1,  1,	/* +20: " !"#$%&'()*+,-./" */
			  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  0,  1,  0,  1,	/* +30: "0123456789:;<=>?" */
			  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,	/* +40: "@ABCDEFGHIJKLMNO" */
			  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  0,  1,  0,  0,  1,	/* +50: "PQRSTUVWXYZ[\]^_" */
			  0,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,	/* +60: "`abcdefghijklmno" */
			  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  0,  0,  0,  1,  0,	/* +70: "pqrstuvwxyz{|}~ " */
			/* 0    : not url char
			 * 1    : url char
			 * other: url head char --> url_table array number + 1
			 */
		};
		static string[] prefix = {
			"https://",
			"http://",
			"sftp://",
			"tftp://",
			"news://",
			"ftp://",
			"mms://",
		};
#endif

		/* begin - ishizaki */
		void markURL(int x)
		{
#if URL_EMPHASIS
			int PrevCharPtr;
			AttributeBitMasks PrevCharAttr;
			char PrevCharCode;

			char ch = CodeBuff[CodeLine + x];

			if (ts.EnableClickableUrl == false &&
				(ts.ColorFlag & ColorFlags.CF_URLCOLOR) == 0)
				return;

			// 直前の行から連結しているか。
			if (x == 0) {
				PrevCharPtr = PrevLinePtr(LinePtr) + VTDisp.NumOfColumns - 1;
				PrevCharCode = CodeBuff[PrevCharPtr];
				PrevCharAttr = AttrBuff[PrevCharPtr];
				if (((PrevCharAttr & AttributeBitMasks.AttrURL) != 0) && ((AttrBuff[AttrLine + 0] & (AttributeBitMasks.AttrKanji | AttributeBitMasks.AttrSpecial)) == 0) && ((ch & 0x80) == 0) && (url_char[ch] != 0)) {
					if (((AttrBuff[AttrLine + 0] & AttributeBitMasks.AttrLineContinued) != 0) || (ts.JoinSplitURL &&
						(PrevCharCode == ts.JoinSplitURLIgnoreEOLChar || ts.JoinSplitURLIgnoreEOLChar == '\0'))) {
						AttrBuff[AttrLine + 0] |= AttributeBitMasks.AttrURL;
					}
				}
				return;
			}

			if ((x - 1 >= 0) && ((AttrBuff[AttrLine + x - 1] & AttributeBitMasks.AttrURL) != 0) &&
				((AttrBuff[AttrLine + x] & (AttributeBitMasks.AttrKanji | AttributeBitMasks.AttrSpecial)) == 0) &&
				((((ch & 0x80) == 0) && (url_char[ch] != 0)) || (x == VTDisp.NumOfColumns - 1 && ch == ts.JoinSplitURLIgnoreEOLChar))) {
				AttrBuff[AttrLine + x] |= AttributeBitMasks.AttrURL;
				return;
			}

			if ((x - 2 >= 0) && strncmp(CodeBuff, CodeLine + x - 2, "://", 3) == 0) {
				int i, len = -1;
				Rectangle rc;
				int CaretX, CaretY;

				foreach (string p in prefix) {
					len = p.Length/* - 1*/;
					if ((x - len >= 0) && strncmp(CodeBuff, CodeLine + x - len, p, len) == 0) {
						for (i = 0; i <= len; i++) {
							AttrBuff[AttrLine + x - i] |= AttributeBitMasks.AttrURL;
						}
						break;
					}
				}

				/* ハイパーリンクの色属性変更は、すでに画面へ出力後に、バッファを遡って URL 属性を
				 * 付け直すというロジックであるため、色が正しく描画されない場合がある。
				 * 少々強引だが、ハイパーリンクを発見したタイミングで、その行に再描画指示を出すことで、
				 * リアルタイムな色描画を実現する。
				 * (2009.8.26 yutaka)
				 */
				CaretX = (0 - VTDisp.WinOrgX) * VTDisp.FontWidth;
				CaretY = (VTDisp.CursorY - VTDisp.WinOrgY) * VTDisp.FontHeight;
				rc = new Rectangle(CaretX, CaretY, VTDisp.NumOfColumns * VTDisp.FontWidth, VTDisp.FontHeight);
				ttwinman.HVTWin.Invalidate(rc, false);
			}
#endif
		}
		/* end - ishizaki */

		public void BuffPutChar(char b, TCharAttr Attr, bool Insert)
		// Put a character in the buffer at the current position
		//   b: character
		//   Attr: attributes
		//   Insert: Insert flag
		{
			int XStart, LineEnd, MoveLen;
			int extr = 0;

			if (ts.EnableContinuedLineCopy && VTDisp.CursorX == 0 && ((AttrBuff[AttrLine + 0] & AttributeBitMasks.AttrLineContinued) != 0)) {
				Attr.Attr |= AttributeBitMasks.AttrLineContinued;
			}

			EraseKanji(1); /* if cursor is on right half of a kanji, erase the kanji */
			if (!Insert) {
				EraseKanji(0); /* if cursor on left half... */
			}

			if (Insert) {
				if (VTDisp.CursorX > CursorRightM)
					LineEnd = VTDisp.NumOfColumns - 1;
				else
					LineEnd = CursorRightM;

				if (LineEnd < VTDisp.NumOfColumns - 1 && ((AttrBuff[AttrLine + LineEnd] & AttributeBitMasks.AttrKanji) != 0)) {
					CodeBuff[CodeLine + LineEnd] = ' ';
					AttrBuff[AttrLine + LineEnd] &= ~AttributeBitMasks.AttrKanji;
					CodeBuff[CodeLine + LineEnd + 1] = ' ';
					AttrBuff[AttrLine + LineEnd + 1] &= ~AttributeBitMasks.AttrKanji;
					extr = 1;
				}

				MoveLen = LineEnd - VTDisp.CursorX;
				if (MoveLen > 0) {
					memmove(CodeBuff, CodeLine + VTDisp.CursorX + 1, CodeBuff, CodeLine + VTDisp.CursorX, MoveLen);
					memmove(AttrBuff, AttrLine + VTDisp.CursorX + 1, AttrBuff, AttrLine + VTDisp.CursorX, MoveLen);
					memmove(AttrBuff2, AttrLine2 + VTDisp.CursorX + 1, AttrBuff2, AttrLine2 + VTDisp.CursorX, MoveLen);
					memmove(AttrBuffFG, AttrLineFG + VTDisp.CursorX + 1, AttrBuffFG, AttrLineFG + VTDisp.CursorX, MoveLen);
					memmove(AttrBuffBG, AttrLineBG + VTDisp.CursorX + 1, AttrBuffBG, AttrLineBG + VTDisp.CursorX, MoveLen);
				}
				CodeBuff[CodeLine + VTDisp.CursorX] = b;
				AttrBuff[AttrLine + VTDisp.CursorX] = Attr.Attr;
				AttrBuff2[AttrLine2 + VTDisp.CursorX] = Attr.Attr2;
				AttrBuffFG[AttrLineFG + VTDisp.CursorX] = Attr.Fore;
				AttrBuffBG[AttrLineBG + VTDisp.CursorX] = Attr.Back;
				/* last char in current line is kanji first? */
				if ((AttrBuff[AttrLine + LineEnd] & AttributeBitMasks.AttrKanji) != 0) {
					/* then delete it */
					CodeBuff[CodeLine + LineEnd] = ' ';
					AttrBuff[AttrLine + LineEnd] = CurCharAttr.Attr;
					AttrBuff2[AttrLine2 + LineEnd] = CurCharAttr.Attr2;
					AttrBuffFG[AttrLineFG + LineEnd] = CurCharAttr.Fore;
					AttrBuffBG[AttrLineBG + LineEnd] = CurCharAttr.Back;
				}
				/* begin - ishizaki */
				markURL(VTDisp.CursorX + 1);
				markURL(VTDisp.CursorX);
				/* end - ishizaki */

				if (StrChangeCount == 0) {
					XStart = VTDisp.CursorX;
				}
				else {
					XStart = StrChangeStart;
				}
				StrChangeCount = 0;
				BuffUpdateRect(XStart, VTDisp.CursorY, LineEnd + extr, VTDisp.CursorY);
			}
			else {
				CodeBuff[CodeLine + VTDisp.CursorX] = b;
				AttrBuff[AttrLine + VTDisp.CursorX] = Attr.Attr;
				AttrBuff2[AttrLine2 + VTDisp.CursorX] = Attr.Attr2;
				AttrBuffFG[AttrLineFG + VTDisp.CursorX] = Attr.Fore;
				AttrBuffBG[AttrLineBG + VTDisp.CursorX] = Attr.Back;
				/* begin - ishizaki */
				markURL(VTDisp.CursorX);
				/* end - ishizaki */

				if (StrChangeCount == 0) {
					StrChangeStart = VTDisp.CursorX;
				}
				StrChangeCount++;
			}
		}

		public void BuffPutKanji(ushort w, TCharAttr Attr, bool Insert)
		// Put a kanji character in the buffer at the current position
		//   b: character
		//   Attr: attributes
		//   Insert: Insert flag
		{
			int XStart, LineEnd, MoveLen;
			int extr = 0;

			if (ts.EnableContinuedLineCopy && VTDisp.CursorX == 0 && ((AttrBuff[AttrLine + 0] & AttributeBitMasks.AttrLineContinued) != 0)) {
				Attr.Attr |= AttributeBitMasks.AttrLineContinued;
			}

			EraseKanji(1); /* if cursor is on right half of a kanji, erase the kanji */

			if (Insert) {
				if (VTDisp.CursorX > CursorRightM)
					LineEnd = VTDisp.NumOfColumns - 1;
				else
					LineEnd = CursorRightM;

				if (LineEnd < VTDisp.NumOfColumns - 1 && ((AttrBuff[AttrLine + LineEnd] & AttributeBitMasks.AttrKanji) != 0)) {
					CodeBuff[CodeLine + LineEnd] = ' ';
					AttrBuff[AttrLine + LineEnd] &= ~AttributeBitMasks.AttrKanji;
					CodeBuff[CodeLine + LineEnd + 1] = ' ';
					AttrBuff[AttrLine + LineEnd + 1] &= ~AttributeBitMasks.AttrKanji;
					extr = 1;
				}

				MoveLen = LineEnd - VTDisp.CursorX - 1;
				if (MoveLen > 0) {
					memmove(CodeBuff, CodeLine + VTDisp.CursorX + 2, CodeBuff, CodeLine + VTDisp.CursorX, MoveLen);
					memmove(AttrBuff, AttrLine + VTDisp.CursorX + 2, AttrBuff, AttrLine + VTDisp.CursorX, MoveLen);
					memmove(AttrBuff2, AttrLine2 + VTDisp.CursorX + 2, AttrBuff2, AttrLine2 + VTDisp.CursorX, MoveLen);
					memmove(AttrBuffFG, AttrLineFG + VTDisp.CursorX + 2, AttrBuffFG, AttrLineFG + VTDisp.CursorX, MoveLen);
					memmove(AttrBuffBG, AttrLineBG + VTDisp.CursorX + 2, AttrBuffBG, AttrLineBG + VTDisp.CursorX, MoveLen);
				}

				CodeBuff[CodeLine + VTDisp.CursorX] = (char)w;
				AttrBuff[AttrLine + VTDisp.CursorX] = Attr.Attr | AttributeBitMasks.AttrKanji; /* DBCS first byte */
				AttrBuff2[AttrLine2 + VTDisp.CursorX] = Attr.Attr2;
				AttrBuffFG[AttrLineFG + VTDisp.CursorX] = Attr.Fore;
				AttrBuffBG[AttrLineBG + VTDisp.CursorX] = Attr.Back;
				if (VTDisp.CursorX < LineEnd) {
					CodeBuff[CodeLine + VTDisp.CursorX + 1] = '\0';
					AttrBuff[AttrLine + VTDisp.CursorX + 1] = Attr.Attr;
					AttrBuff2[AttrLine2 + VTDisp.CursorX + 1] = Attr.Attr2;
					AttrBuffFG[AttrLineFG + VTDisp.CursorX + 1] = Attr.Fore;
					AttrBuffBG[AttrLineBG + VTDisp.CursorX + 1] = Attr.Back;
				}
				/* begin - ishizaki */
				markURL(VTDisp.CursorX);
				markURL(VTDisp.CursorX + 1);
				/* end - ishizaki */

				/* last char in current line is kanji first? */
				if ((AttrBuff[AttrLine + LineEnd] & AttributeBitMasks.AttrKanji) != 0) {
					/* then delete it */
					CodeBuff[CodeLine + LineEnd] = ' ';
					AttrBuff[AttrLine + LineEnd] = CurCharAttr.Attr;
					AttrBuff2[AttrLine2 + LineEnd] = CurCharAttr.Attr2;
					AttrBuffFG[AttrLineFG + LineEnd] = CurCharAttr.Fore;
					AttrBuffBG[AttrLineBG + LineEnd] = CurCharAttr.Back;
				}

				if (StrChangeCount == 0) {
					XStart = VTDisp.CursorX;
				}
				else {
					XStart = StrChangeStart;
				}
				StrChangeCount = 0;
				BuffUpdateRect(XStart, VTDisp.CursorY, LineEnd + extr, VTDisp.CursorY);
			}
			else {
				CodeBuff[CodeLine + VTDisp.CursorX] = (char)w;
				AttrBuff[AttrLine + VTDisp.CursorX] = Attr.Attr | AttributeBitMasks.AttrKanji; /* DBCS first byte */
				AttrBuff2[AttrLine2 + VTDisp.CursorX] = Attr.Attr2;
				AttrBuffFG[AttrLineFG + VTDisp.CursorX] = Attr.Fore;
				AttrBuffBG[AttrLineBG + VTDisp.CursorX] = Attr.Back;
				if (VTDisp.CursorX < VTDisp.NumOfColumns - 1) {
					CodeBuff[CodeLine + VTDisp.CursorX + 1] = '\0';
					AttrBuff[AttrLine + VTDisp.CursorX + 1] = Attr.Attr;
					AttrBuff2[AttrLine2 + VTDisp.CursorX + 1] = Attr.Attr2;
					AttrBuffFG[AttrLineFG + VTDisp.CursorX + 1] = Attr.Fore;
					AttrBuffBG[AttrLineBG + VTDisp.CursorX + 1] = Attr.Back;
				}
				/* begin - ishizaki */
				markURL(VTDisp.CursorX);
				markURL(VTDisp.CursorX + 1);
				/* end - ishizaki */

				if (StrChangeCount == 0) {
					StrChangeStart = VTDisp.CursorX;
				}
				StrChangeCount = StrChangeCount + 2;
			}
		}

		bool CheckSelect(int x, int y)
		//  subroutine called by BuffUpdateRect
		{
			int L, L1, L2;

			if (BoxSelect) {
				return (Selected &&
				((SelectStart.X <= x) && (x < SelectEnd.X) ||
				 (SelectEnd.X <= x) && (x < SelectStart.X)) &&
				((SelectStart.Y <= y) && (y <= SelectEnd.Y) ||
				 (SelectEnd.Y <= y) && (y <= SelectStart.Y)));
			}
			else {
				L = (int)MAKELONG((uint)x, (uint)y);
				L1 = (int)MAKELONG((uint)SelectStart.X, (uint)SelectStart.Y);
				L2 = (int)MAKELONG((uint)SelectEnd.X, (uint)SelectEnd.Y);

				return (Selected &&
					((L1 <= L) && (L < L2) || (L2 <= L) && (L < L1)));
			}
		}

		private static uint MAKELONG(uint x, uint y)
		{
			return ((x & 0xFFFFu) + ((y & 0xFFFFu) << 16));
		}

		public void BuffUpdateRect(int XStart, int YStart, int XEnd, int YEnd)
		// Display text in a rectangular region in the screen
		//   XStart: x position of the upper-left corner (screen cordinate)
		//   YStart: y position
		//   XEnd: x position of the lower-right corner (last character)
		//   YEnd: y position
		{
			int i, j, count;
			int IStart, IEnd;
			int X, Y;
			int TmpPtr;
			TCharAttr CurAttr, TempAttr;
			bool CurSel, TempSel, Caret;

			if (XStart >= VTDisp.WinOrgX + VTDisp.WinWidth) {
				return;
			}
			if (YStart >= VTDisp.WinOrgY + VTDisp.WinHeight) {
				return;
			}
			if (XEnd < VTDisp.WinOrgX) {
				return;
			}
			if (YEnd < VTDisp.WinOrgY) {
				return;
			}

			if (XStart < VTDisp.WinOrgX) {
				XStart = VTDisp.WinOrgX;
			}
			if (YStart < VTDisp.WinOrgY) {
				YStart = VTDisp.WinOrgY;
			}
			if (XEnd >= VTDisp.WinOrgX + VTDisp.WinWidth) {
				XEnd = VTDisp.WinOrgX + VTDisp.WinWidth - 1;
			}
			if (YEnd >= VTDisp.WinOrgY + VTDisp.WinHeight) {
				YEnd = VTDisp.WinOrgY + VTDisp.WinHeight - 1;
			}

			TempAttr = VTDisp.DefCharAttr;
			TempSel = false;

			Caret = VTDisp.IsCaretOn();
			if (Caret) {
				VTDisp.CaretOff();
			}

			VTDisp.DispSetupDC(VTDisp.DefCharAttr, TempSel);

			Y = (YStart - VTDisp.WinOrgY) * VTDisp.FontHeight;
			TmpPtr = GetLinePtr(VTDisp.PageStart + YStart);
			for (j = YStart + VTDisp.PageStart; j <= YEnd + VTDisp.PageStart; j++) {
				IStart = XStart;
				IEnd = XEnd;

				IStart = LeftHalfOfDBCS(TmpPtr, IStart);

				X = (IStart - VTDisp.WinOrgX) * VTDisp.FontWidth;

				i = IStart;
				do {
					CurAttr.Attr = AttrBuff[TmpPtr + i] & ~AttributeBitMasks.AttrKanji;
					CurAttr.Attr2 = AttrBuff2[TmpPtr + i];
					CurAttr.Fore = AttrBuffFG[TmpPtr + i];
					CurAttr.Back = AttrBuffBG[TmpPtr + i];
					CurSel = CheckSelect(i, j);
					count = 1;
					while ((i + count <= IEnd) &&
							(CurAttr.Attr == (AttrBuff[TmpPtr + i + count] & ~AttributeBitMasks.AttrKanji)) &&
							(CurAttr.Attr2 == AttrBuff2[TmpPtr + i + count]) &&
							(CurAttr.Fore == AttrBuffFG[TmpPtr + i + count]) &&
							(CurAttr.Back == AttrBuffBG[TmpPtr + i + count]) &&
							(CurSel == CheckSelect(i + count, j)) ||
							(i + count < VTDisp.NumOfColumns) &&
							((AttrBuff[TmpPtr + i + count - 1] & AttributeBitMasks.AttrKanji) != 0)) {
						count++;
					}

					if (VTDisp.TCharAttrCmp(CurAttr, TempAttr) != 0 || (CurSel != TempSel)) {
						VTDisp.DispSetupDC(CurAttr, CurSel);
						TempAttr = CurAttr;
						TempSel = CurSel;
					}
					VTDisp.DispStr(CodeBuff, AttrBuff, TmpPtr + i, count, Y, ref X);
					i = i + count;
				}
				while (i <= IEnd);
				Y = Y + VTDisp.FontHeight;
				TmpPtr = NextLinePtr(TmpPtr);
			}
			if (Caret) {
				VTDisp.CaretOn();
			}
		}

		public void UpdateStr()
		// Display not-yet-displayed string
		{
			int X, Y;
			TCharAttr TempAttr;
			int pos, len;

			if (StrChangeCount == 0) {
				return;
			}
			X = StrChangeStart;
			Y = VTDisp.CursorY;
			if (!VTDisp.IsLineVisible(ref X, ref Y)) {
				StrChangeCount = 0;
				return;
			}

			TempAttr.Attr = AttrBuff[AttrLine + StrChangeStart];
			TempAttr.Attr2 = AttrBuff2[AttrLine2 + StrChangeStart];
			TempAttr.Fore = AttrBuffFG[AttrLineFG + StrChangeStart];
			TempAttr.Back = AttrBuffBG[AttrLineBG + StrChangeStart];

			/* これから描画する文字列の始まりが「URL構成文字属性」だった場合、
			 * 当該色で行末までペイントされないようにする。
			 * (2009.10.24 yutaka)
			 */
			if ((TempAttr.Attr & AttributeBitMasks.AttrURL) != 0) {
				/* 開始位置からどこまでが AttributeBitMasks.AttrURL かをカウントする */
				len = 0;
				for (pos = 0; pos < StrChangeCount; pos++) {
					if (TempAttr.Attr != AttrBuff[AttrLine + StrChangeStart + pos])
						break;
					len++;
				}
				VTDisp.DispSetupDC(TempAttr, false);
				VTDisp.DispStr(CodeBuff, AttrBuff, CodeLine + StrChangeStart, len, Y, ref X);

				/* 残りの文字列があれば、ふつうに描画を行う。*/
				if (len < StrChangeCount) {
					TempAttr.Attr = AttrBuff[AttrLine + StrChangeStart + pos];
					TempAttr.Attr2 = AttrBuff2[AttrLine2 + StrChangeStart + pos];
					TempAttr.Fore = AttrBuffFG[AttrLineFG + StrChangeStart + pos];
					TempAttr.Back = AttrBuffBG[AttrLineBG + StrChangeStart + pos];

					VTDisp.DispSetupDC(TempAttr, false);
					VTDisp.DispStr(CodeBuff, AttrBuff, CodeLine + StrChangeStart + pos, (StrChangeCount - len), Y, ref X);
				}
			}
			else {
				VTDisp.DispSetupDC(TempAttr, false);
				VTDisp.DispStr(CodeBuff, AttrBuff, CodeLine + StrChangeStart, StrChangeCount, Y, ref X);
			}

			StrChangeCount = 0;
		}

#if false
		void UpdateStrUnicode()
		// Display not-yet-displayed string
		{
			int X, Y;
			TCharAttr TempAttr;

			if (StrChangeCount==0) return;
			X = StrChangeStart;
			Y = VTDisp.CursorY;
			if (! IsLineVisible(&X, &Y)) {
				StrChangeCount = 0;
				return;
			}

			TempAttr.Attr = AttrBuff[AttrLine + StrChangeStart];
		  TempAttr.Attr2 = AttrBuff2[AttrLine2 + StrChangeStart];
		  TempAttr.Fore = AttrBuffFG[AttrLineFG + StrChangeStart];
		  TempAttr.Back = AttrBuffBG[AttrLineBG + StrChangeStart];
			VTDisp.DispSetupDC(TempAttr, false);
		  DispStr(CodeBuff, CodeLine + StrChangeStart,StrChangeCount,Y, ref X);
			StrChangeCount = 0;
		}
#endif

		public void MoveCursor(int Xnew, int Ynew)
		{
			UpdateStr();

			if (VTDisp.CursorY != Ynew) {
				NewLine(VTDisp.PageStart + Ynew);
			}

			VTDisp.CursorX = Xnew;
			VTDisp.CursorY = Ynew;
			Wrap = false;

			/* 最下行でだけ自動スクロールする*/
			if (ts.AutoScrollOnlyInBottomLine == 0 || VTDisp.WinOrgY == 0) {
				VTDisp.DispScrollToCursor(VTDisp.CursorX, VTDisp.CursorY);
			}
		}

		public void MoveRight()
		/* move cursor right, but dont update screen.
		  this procedure must be called from DispChar&DispKanji only */
		{
			VTDisp.CursorX++;
			/* 最下行でだけ自動スクロールする */
			if (ts.AutoScrollOnlyInBottomLine == 0 || VTDisp.WinOrgY == 0) {
				VTDisp.DispScrollToCursor(VTDisp.CursorX, VTDisp.CursorY);
			}
		}

		public void BuffSetCaretWidth()
		{
			bool DW;

			/* check whether cursor on a DBCS character */
			DW = (((AttrBuff[AttrLine + VTDisp.CursorX]) & AttributeBitMasks.AttrKanji) != 0);
			VTDisp.DispSetCaretWidth(DW);
		}

		void ScrollUp1Line()
		{
			int i, linelen;
			int extl = 0, extr = 0;
			int SrcPtr = 0, DestPtr;

			if ((CursorTop <= VTDisp.CursorY) && (VTDisp.CursorY <= CursorBottom)) {
				UpdateStr();

				if (CursorLeftM > 0)
					extl = 1;
				if (CursorRightM < VTDisp.NumOfColumns - 1)
					extr = 1;
				if (extl != 0 || extr != 0)
					EraseKanjiOnLRMargin(GetLinePtr(VTDisp.PageStart + CursorTop), CursorBottom - CursorTop + 1);

				linelen = CursorRightM - CursorLeftM + 1;
				DestPtr = GetLinePtr(VTDisp.PageStart + CursorBottom) + CursorLeftM;
				for (i = CursorBottom - 1; i >= CursorTop; i--) {
					SrcPtr = PrevLinePtr(DestPtr);
					memmove(CodeBuff, DestPtr, CodeBuff, SrcPtr, linelen);
					memmove(AttrBuff, DestPtr, AttrBuff, SrcPtr, linelen);
					memmove(AttrBuff2, DestPtr, AttrBuff2, SrcPtr, linelen);
					memmove(AttrBuffFG, DestPtr, AttrBuffFG, SrcPtr, linelen);
					memmove(AttrBuffBG, DestPtr, AttrBuffBG, SrcPtr, linelen);
					DestPtr = SrcPtr;
				}
				memset(CodeBuff, SrcPtr, ' ', linelen);
				memset(AttrBuff, SrcPtr, AttributeBitMasks.AttrDefault, linelen);
				memset(AttrBuff2, SrcPtr, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, linelen);
				memset(AttrBuffFG, SrcPtr, CurCharAttr.Fore, linelen);
				memset(AttrBuffBG, SrcPtr, CurCharAttr.Back, linelen);

				if (CursorLeftM > 0 || CursorRightM < VTDisp.NumOfColumns - 1)
					BuffUpdateRect(CursorLeftM - extl, CursorTop, CursorRightM + extr, CursorBottom);
				else
					VTDisp.DispScrollNLines(CursorTop, CursorBottom, -1);
			}
		}

		public void BuffScrollNLines(int n)
		{
			int i, linelen;
			int extl = 0, extr = 0;
			int SrcPtr, DestPtr;

			if (n < 1) {
				return;
			}
			UpdateStr();

			if (CursorLeftM == 0 && CursorRightM == VTDisp.NumOfColumns - 1 && CursorTop == 0) {
				if (CursorBottom == VTDisp.NumOfLines - 1) {
					VTDisp.WinOrgY = VTDisp.WinOrgY - n;
					/* 最下行でだけ自動スクロールする */
					if (ts.AutoScrollOnlyInBottomLine != 0 && VTDisp.NewOrgY != 0) {
						VTDisp.NewOrgY = VTDisp.WinOrgY;
					}
					BuffScroll(n, CursorBottom);
					VTDisp.DispCountScroll(n);
					return;
				}
				else if (VTDisp.CursorY <= CursorBottom) {
					/* 最下行でだけ自動スクロールする */
					if (ts.AutoScrollOnlyInBottomLine != 0 && VTDisp.NewOrgY != 0) {
						/* スクロールさせない場合の処理 */
						VTDisp.WinOrgY = VTDisp.WinOrgY - n;
						VTDisp.NewOrgY = VTDisp.WinOrgY;
						BuffScroll(n, CursorBottom);
						VTDisp.DispCountScroll(n);
					}
					else {
						BuffScroll(n, CursorBottom);
						VTDisp.DispScrollNLines(VTDisp.WinOrgY, CursorBottom, n);
					}
					return;
				}
			}

			if ((CursorTop <= VTDisp.CursorY) && (VTDisp.CursorY <= CursorBottom)) {
				if (CursorLeftM > 0)
					extl = 1;
				if (CursorRightM < VTDisp.NumOfColumns - 1)
					extr = 1;
				if (extl != 0 || extr != 0)
					EraseKanjiOnLRMargin(GetLinePtr(VTDisp.PageStart + CursorTop), CursorBottom - CursorTop + 1);

				linelen = CursorRightM - CursorLeftM + 1;
				DestPtr = GetLinePtr(VTDisp.PageStart + CursorTop) + (int)CursorLeftM;
				if (n < CursorBottom - CursorTop + 1) {
					SrcPtr = GetLinePtr(VTDisp.PageStart + CursorTop + n) + (int)CursorLeftM;
					for (i = CursorTop + n; i <= CursorBottom; i++) {
						memmove(CodeBuff, DestPtr, CodeBuff, SrcPtr, linelen);
						memmove(AttrBuff, DestPtr, AttrBuff, SrcPtr, linelen);
						memmove(AttrBuff2, DestPtr, AttrBuff2, SrcPtr, linelen);
						memmove(AttrBuffFG, DestPtr, AttrBuffFG, SrcPtr, linelen);
						memmove(AttrBuffBG, DestPtr, AttrBuffBG, SrcPtr, linelen);
						SrcPtr = NextLinePtr(SrcPtr);
						DestPtr = NextLinePtr(DestPtr);
					}
				}
				else {
					n = CursorBottom - CursorTop + 1;
				}
				for (i = CursorBottom + 1 - n; i <= CursorBottom; i++) {
					memset(CodeBuff, DestPtr, ' ', linelen);
					memset(AttrBuff, DestPtr, AttributeBitMasks.AttrDefault, linelen);
					memset(AttrBuff2, DestPtr, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, linelen);
					memset(AttrBuffFG, DestPtr, CurCharAttr.Fore, linelen);
					memset(AttrBuffBG, DestPtr, CurCharAttr.Back, linelen);
					DestPtr = NextLinePtr(DestPtr);
				}
				if (CursorLeftM > 0 || CursorRightM < VTDisp.NumOfColumns - 1)
					BuffUpdateRect(CursorLeftM - extl, CursorTop, CursorRightM + extr, CursorBottom);
				else
					VTDisp.DispScrollNLines(CursorTop, CursorBottom, n);
			}
		}

		public void BuffRegionScrollUpNLines(int n)
		{
			int i, linelen;
			int extl = 0, extr = 0;
			int SrcPtr, DestPtr;

			if (n < 1) {
				return;
			}
			UpdateStr();

			if (CursorLeftM == 0 && CursorRightM == VTDisp.NumOfColumns - 1 && CursorTop == 0) {
				if (CursorBottom == VTDisp.NumOfLines - 1) {
					VTDisp.WinOrgY = VTDisp.WinOrgY - n;
					BuffScroll(n, CursorBottom);
					VTDisp.DispCountScroll(n);
				}
				else {
					BuffScroll(n, CursorBottom);
					VTDisp.DispScrollNLines(VTDisp.WinOrgY, CursorBottom, n);
				}
			}
			else {
				if (CursorLeftM > 0)
					extl = 1;
				if (CursorRightM < VTDisp.NumOfColumns - 1)
					extr = 1;
				if (extl != 0 || extr != 0)
					EraseKanjiOnLRMargin(GetLinePtr(VTDisp.PageStart + CursorTop), CursorBottom - CursorTop + 1);

				DestPtr = GetLinePtr(VTDisp.PageStart + CursorTop) + CursorLeftM;
				linelen = CursorRightM - CursorLeftM + 1;
				if (n < CursorBottom - CursorTop + 1) {
					SrcPtr = GetLinePtr(VTDisp.PageStart + CursorTop + n) + CursorLeftM;
					for (i = CursorTop + n; i <= CursorBottom; i++) {
						memmove(CodeBuff, DestPtr, CodeBuff, SrcPtr, linelen);
						memmove(AttrBuff, DestPtr, AttrBuff, SrcPtr, linelen);
						memmove(AttrBuff2, DestPtr, AttrBuff2, SrcPtr, linelen);
						memmove(AttrBuffFG, DestPtr, AttrBuffFG, SrcPtr, linelen);
						memmove(AttrBuffBG, DestPtr, AttrBuffBG, SrcPtr, linelen);
						SrcPtr = NextLinePtr(SrcPtr);
						DestPtr = NextLinePtr(DestPtr);
					}
				}
				else {
					n = CursorBottom - CursorTop + 1;
				}
				for (i = CursorBottom + 1 - n; i <= CursorBottom; i++) {
					memset(CodeBuff, DestPtr, ' ', linelen);
					memset(AttrBuff, DestPtr, AttributeBitMasks.AttrDefault, linelen);
					memset(AttrBuff2, DestPtr, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, linelen);
					memset(AttrBuffFG, DestPtr, CurCharAttr.Fore, linelen);
					memset(AttrBuffBG, DestPtr, CurCharAttr.Back, linelen);
					DestPtr = NextLinePtr(DestPtr);
				}

				if (CursorLeftM > 0 || CursorRightM < VTDisp.NumOfColumns - 1) {
					BuffUpdateRect(CursorLeftM - extl, CursorTop, CursorRightM + extr, CursorBottom);
				}
				else {
					VTDisp.DispScrollNLines(CursorTop, CursorBottom, n);
				}
			}
		}

		public void BuffRegionScrollDownNLines(int n)
		{
			int i, linelen;
			int extl = 0, extr = 0;
			int SrcPtr, DestPtr;

			if (n < 1) {
				return;
			}
			UpdateStr();

			if (CursorLeftM > 0)
				extl = 1;
			if (CursorRightM < VTDisp.NumOfColumns - 1)
				extr = 1;
			if (extl != 0 || extr != 0)
				EraseKanjiOnLRMargin(GetLinePtr(VTDisp.PageStart + CursorTop), CursorBottom - CursorTop + 1);

			DestPtr = GetLinePtr(VTDisp.PageStart + CursorBottom) + CursorLeftM;
			linelen = CursorRightM - CursorLeftM + 1;
			if (n < CursorBottom - CursorTop + 1) {
				SrcPtr = GetLinePtr(VTDisp.PageStart + CursorBottom - n) + CursorLeftM;
				for (i = CursorBottom - n; i >= CursorTop; i--) {
					memmove(CodeBuff, DestPtr, CodeBuff, SrcPtr, linelen);
					memmove(AttrBuff, DestPtr, AttrBuff, SrcPtr, linelen);
					memmove(AttrBuff2, DestPtr, AttrBuff2, SrcPtr, linelen);
					memmove(AttrBuffFG, DestPtr, AttrBuffFG, SrcPtr, linelen);
					memmove(AttrBuffBG, DestPtr, AttrBuffBG, SrcPtr, linelen);
					SrcPtr = PrevLinePtr(SrcPtr);
					DestPtr = PrevLinePtr(DestPtr);
				}
			}
			else {
				n = CursorBottom - CursorTop + 1;
			}
			for (i = CursorTop + n - 1; i >= CursorTop; i--) {
				memset(CodeBuff, DestPtr, ' ', linelen);
				memset(AttrBuff, DestPtr, AttributeBitMasks.AttrDefault, linelen);
				memset(AttrBuff2, DestPtr, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, linelen);
				memset(AttrBuffFG, DestPtr, CurCharAttr.Fore, linelen);
				memset(AttrBuffBG, DestPtr, CurCharAttr.Back, linelen);
				DestPtr = PrevLinePtr(DestPtr);
			}

			if (CursorLeftM > 0 || CursorRightM < VTDisp.NumOfColumns - 1) {
				BuffUpdateRect(CursorLeftM - extl, CursorTop, CursorRightM + extr, CursorBottom);
			}
			else {
				VTDisp.DispScrollNLines(CursorTop, CursorBottom, -n);
			}
		}

		public void BuffClearScreen()
		{ // clear screen
			if (isCursorOnStatusLine()) {
				BuffScrollNLines(1); /* clear status line */
			}
			else { /* clear main screen */
				UpdateStr();
				BuffScroll(VTDisp.NumOfLines - StatusLine, VTDisp.NumOfLines - 1 - StatusLine);
				VTDisp.DispScrollNLines(VTDisp.WinOrgY, VTDisp.NumOfLines - 1 - StatusLine, VTDisp.NumOfLines - StatusLine);
			}
		}

		public void BuffUpdateScroll()
		// Updates scrolling
		{
			UpdateStr();
			VTDisp.DispUpdateScroll();
		}

		public void CursorUpWithScroll()
		{
			if ((0 < VTDisp.CursorY) && (VTDisp.CursorY < CursorTop) ||
				(CursorTop < VTDisp.CursorY)) {
				MoveCursor(VTDisp.CursorX, VTDisp.CursorY - 1);
			}
			else if (VTDisp.CursorY == CursorTop) {
				ScrollUp1Line();
			}
		}

		// called by BuffDblClk
		//   check if a character is the word delimiter
		bool IsDelimiter(int Line, int CharPtr)
		{
			if ((AttrBuff[Line + CharPtr] & AttributeBitMasks.AttrKanji) != 0) {
				return (ts.DelimDBCS != 0);
			}
			return (Array.IndexOf(ts.DelimList, CodeBuff[Line + CharPtr]) != -1);
		}

		void GetMinMax(int i1, int i2, int i3, out int min, out int max)
		{
			if (i1 < i2) {
				min = i1;
				max = i2;
			}
			else {
				min = i2;
				max = i1;
			}
			if (i3 < min) {
				min = i3;
			}
			if (i3 > max) {
				max = i3;
			}
		}

		/* start - ishizaki */
		void invokeBrowser(int ptr)
		{
#if URL_EMPHASIS
			int i, start, end;
			char[] url = new char[1024];
			char[] param = new char[1024];
			int uptr;
			char ch;

			start = ptr;
			while ((AttrBuff[start] & AttributeBitMasks.AttrURL) != 0) {
				start--;
			}
			start++;

			end = ptr;
			while ((AttrBuff[end] & AttributeBitMasks.AttrURL) != 0) {
				end++;
			}
			end--;

			if (start + url.Length - 1 <= end) {
				end = start + url.Length - 2;
			}
			uptr = 0;
			for (i = 0; i < end - start + 1; i++) {
				ch = (char)CodeBuff[start + i];
				if ((start + i) % VTDisp.NumOfColumns == VTDisp.NumOfColumns - 1
					&& ch == ts.JoinSplitURLIgnoreEOLChar) {
					// 行末が行継続マーク用の文字の場合はスキップする
				}
				else {
					url[uptr++] = ch;
				}
			}
			url[uptr] = '\0';
			ProcessStartInfo sInfo = new ProcessStartInfo(new String(url));
			Process.Start(sInfo);
#endif
		}
		/* end - ishizaki */

		void ChangeSelectRegion()
		{
			Point TempStart, TempEnd;
			int j, IStart, IEnd;
			bool Caret;

			if ((SelectEndOld.X == SelectEnd.X) &&
				(SelectEndOld.Y == SelectEnd.Y)) {
				return;
			}

			if (BoxSelect) {
				int sx, ex, sy, ey;
				GetMinMax(SelectStart.X, SelectEndOld.X, SelectEnd.X, out sx, out ex);
				GetMinMax(SelectStart.Y, SelectEndOld.Y, SelectEnd.Y, out sy, out ey);
				TempStart = new Point(sx, sy);
				TempEnd = new Point(ex - 1, ey);
				Caret = VTDisp.IsCaretOn();
				if (Caret) {
					VTDisp.CaretOff();
				}
				VTDisp.DispInitDC();
				BuffUpdateRect(TempStart.X, TempStart.Y - VTDisp.PageStart,
							   TempEnd.X, TempEnd.Y - VTDisp.PageStart);
				VTDisp.DispReleaseDC();
				if (Caret) {
					VTDisp.CaretOn();
				}
				SelectEndOld = SelectEnd;
				return;
			}

			if ((SelectEndOld.Y < SelectEnd.Y) ||
				(SelectEndOld.Y == SelectEnd.Y) &&
				(SelectEndOld.X <= SelectEnd.X)) {
				TempStart = SelectEndOld;
				TempEnd = new Point(SelectEnd.X - 1, SelectEnd.Y);
			}
			else {
				TempStart = SelectEnd;
				TempEnd = new Point(SelectEndOld.X - 1, SelectEndOld.Y);
			}
			if (TempEnd.X < 0) {
				TempEnd.X = VTDisp.NumOfColumns - 1;
				TempEnd.Y--;
			}

			Caret = VTDisp.IsCaretOn();
			if (Caret) {
				VTDisp.CaretOff();
			}
			for (j = TempStart.Y; j <= TempEnd.Y; j++) {
				IStart = 0;
				IEnd = VTDisp.NumOfColumns - 1;
				if (j == TempStart.Y) {
					IStart = TempStart.X;
				}
				if (j == TempEnd.Y) {
					IEnd = TempEnd.X;
				}

				if ((IEnd >= IStart) && (j >= VTDisp.PageStart + VTDisp.WinOrgY) &&
					(j < VTDisp.PageStart + VTDisp.WinOrgY + VTDisp.WinHeight)) {
					VTDisp.DispInitDC();
					BuffUpdateRect(IStart, j - VTDisp.PageStart, IEnd, j - VTDisp.PageStart);
					VTDisp.DispReleaseDC();
				}
			}
			if (Caret) {
				VTDisp.CaretOn();
			}

			SelectEndOld = SelectEnd;
		}

		public bool BuffUrlDblClk(int Xw, int Yw)
		{
			int X, Y;
			int TmpPtr;
			bool url_invoked = false;

			if (!ts.EnableClickableUrl) {
				return false;
			}

			VTDisp.CaretOff();

			bool right;
			VTDisp.DispConvWinToScreen(Xw, Yw, out X, out Y, out right);
			Y = Y + VTDisp.PageStart;
			if ((Y < 0) || (Y >= VTDisp.BuffEnd)) {
				return false;
			}
			if (X < 0) X = 0;
			if (X >= VTDisp.NumOfColumns) {
				X = VTDisp.NumOfColumns - 1;
			}

			if ((Y >= 0) && (Y < VTDisp.BuffEnd)) {
				LockBuffer();
				TmpPtr = GetLinePtr(Y);
				/* start - ishizaki */
				if ((AttrBuff[TmpPtr + X] & AttributeBitMasks.AttrURL) != 0) {
					BoxSelect = false;
					SelectEnd = SelectStart;
					ChangeSelectRegion();

					url_invoked = true;
					invokeBrowser(TmpPtr + X);

					SelectStart = new Point(0, 0);
					SelectEnd = new Point(0, 0);
					SelectEndOld = new Point(0, 0);
					Selected = false;
				}
				UnlockBuffer();
			}
			return url_invoked;
		}

		public void BuffDblClk(int Xw, int Yw)
		//  Select a word at (Xw, Yw) by mouse double click
		//    Xw: horizontal position in window coordinate (pixels)
		//    Yw: vertical
		{
			int X, Y, YStart, YEnd;
			int IStart, IEnd, i;
			int TmpPtr;
			char b;
			bool DBCS;

			VTDisp.CaretOff();

			bool right;
			VTDisp.DispConvWinToScreen(Xw, Yw, out X, out Y, out right);
			Y = Y + VTDisp.PageStart;
			if ((Y < 0) || (Y >= VTDisp.BuffEnd)) {
				return;
			}
			if (X < 0) X = 0;
			if (X >= VTDisp.NumOfColumns) X = VTDisp.NumOfColumns - 1;

			BoxSelect = false;
			LockBuffer();
			SelectEnd = SelectStart;
			ChangeSelectRegion();

			if ((Y >= 0) && (Y < VTDisp.BuffEnd)) {
				TmpPtr = GetLinePtr(Y);

				IStart = X;
				IStart = LeftHalfOfDBCS(TmpPtr, IStart);
				IEnd = IStart;
				YStart = YEnd = Y;

				if (IsDelimiter(TmpPtr, IStart)) {
					b = CodeBuff[TmpPtr + IStart];
					DBCS = (AttrBuff[TmpPtr + IStart] & AttributeBitMasks.AttrKanji) != 0;
					while ((b == CodeBuff[TmpPtr + IStart]) ||
						   DBCS &&
						   ((AttrBuff[TmpPtr + IStart] & AttributeBitMasks.AttrKanji) != 0)) {
						MoveCharPtr(TmpPtr, ref IStart, -1); // move left
						if (ts.EnableContinuedLineCopy) {
							if (IStart <= 0) {
								// 左端の場合
								if (YStart > 0 && (AttrBuff[TmpPtr] & AttributeBitMasks.AttrLineContinued) != 0) {
									// 前の行に移動する
									YStart--;
									TmpPtr = GetLinePtr(YStart);
									IStart = VTDisp.NumOfColumns;
								}
								else {
									break;
								}
							}
						}
						else {
							if (IStart <= 0) {
								// 左端の場合は終わり
								break;
							}
						}
					}
					if ((b != CodeBuff[TmpPtr + IStart]) &&
						!(DBCS && ((AttrBuff[TmpPtr + IStart] & AttributeBitMasks.AttrKanji) != 0))) {
						// 最終位置が Delimiter でない場合にはひとつ右にずらす
						if (ts.EnableContinuedLineCopy && IStart == VTDisp.NumOfColumns - 1) {
							// 右端の場合には次の行へ移動する
							YStart++;
							TmpPtr = GetLinePtr(YStart);
							IStart = 0;
						}
						else {
							MoveCharPtr(TmpPtr, ref IStart, 1);
						}
					}

					// 行が移動しているかもしれないので、クリックした行を取り直す
					TmpPtr = GetLinePtr(YEnd);
					i = 1;
					while (((b == CodeBuff[TmpPtr + IEnd]) ||
							DBCS &&
							((AttrBuff[TmpPtr + IEnd] & AttributeBitMasks.AttrKanji) != 0))) {
						i = MoveCharPtr(TmpPtr, ref IEnd, 1); // move right
						if (ts.EnableContinuedLineCopy) {
							if (i == 0) {
								// 右端の場合
								if (YEnd < VTDisp.BuffEnd &&
									((AttrBuff[TmpPtr + IEnd + 1 + (DBCS ? 1 : 0)] & AttributeBitMasks.AttrLineContinued) != 0)) {
									// 次の行に移動する
									YEnd++;
									TmpPtr = GetLinePtr(YEnd);
									IEnd = 0;
								}
								else {
									break;
								}
							}
						}
						else {
							if (i == 0) {
								// 右端の場合は終わり
								break;
							}
						}
					}
				}
				else {
					while (!IsDelimiter(TmpPtr, IStart)) {
						MoveCharPtr(TmpPtr, ref IStart, -1); // move left
						if (ts.EnableContinuedLineCopy) {
							if (IStart <= 0) {
								// 左端の場合
								if (YStart > 0 && (AttrBuff[TmpPtr] & AttributeBitMasks.AttrLineContinued) != 0) {
									// 前の行に移動する
									YStart--;
									TmpPtr = GetLinePtr(YStart);
									IStart = VTDisp.NumOfColumns;
								}
								else {
									break;
								}
							}
						}
						else {
							if (IStart <= 0) {
								// 左端の場合は終わり
								break;
							}
						}
					}
					if (IsDelimiter(TmpPtr, IStart)) {
						// 最終位置が Delimiter の場合にはひとつ右にずらす
						if (ts.EnableContinuedLineCopy && IStart == VTDisp.NumOfColumns - 1) {
							// 右端の場合には次の行へ移動する
							YStart++;
							TmpPtr = GetLinePtr(YStart);
							IStart = 0;
						}
						else {
							MoveCharPtr(TmpPtr, ref IStart, 1);
						}
					}

					// 行が移動しているかもしれないので、クリックした行を取り直す
					TmpPtr = GetLinePtr(YEnd);
					i = 1;
					while (!IsDelimiter(TmpPtr, IEnd)) {
						i = MoveCharPtr(TmpPtr, ref IEnd, 1); // move right
						if (ts.EnableContinuedLineCopy) {
							if (i == 0) {
								// 右端の場合
								if (YEnd < VTDisp.BuffEnd && (AttrBuff[TmpPtr + IEnd + 1] & AttributeBitMasks.AttrLineContinued) != 0) {
									// 次の行に移動する
									YEnd++;
									TmpPtr = GetLinePtr(YEnd);
									IEnd = 0;
								}
								else {
									break;
								}
							}
						}
						else {
							if (i == 0) {
								// 右端の場合は終わり
								break;
							}
						}
					}
				}
				if (ts.EnableContinuedLineCopy) {
					if (IEnd == 0) {
						// 左端の場合には前の行へ移動する
						YEnd--;
						IEnd = VTDisp.NumOfColumns;
					}
					else if (i == 0) {
						IEnd = VTDisp.NumOfColumns;
					}
				}
				else {
					if (i == 0)
						IEnd = VTDisp.NumOfColumns;
				}

				SelectStart = new Point(IStart, YStart);
				SelectEnd = new Point(IEnd, YEnd);
				SelectEndOld = SelectStart;
				DblClkStart = SelectStart;
				DblClkEnd = SelectEnd;
				Selected = true;
				ChangeSelectRegion();
			}
			UnlockBuffer();
			return;
		}

		public void BuffTplClk(int Yw)
		//  Select a line at Yw by mouse tripple click
		//    Yw: vertical clicked position
		//			in window coordinate (pixels)
		{
			int X, Y;
			bool right;

			VTDisp.CaretOff();

			VTDisp.DispConvWinToScreen(0, Yw, out X, out Y, out right);
			Y = Y + VTDisp.PageStart;
			if ((Y < 0) || (Y >= VTDisp.BuffEnd)) {
				return;
			}

			LockBuffer();
			SelectEnd = SelectStart;
			ChangeSelectRegion();
			SelectStart = new Point(0, Y);
			SelectEnd = new Point(VTDisp.NumOfColumns, Y);
			SelectEndOld = SelectStart;
			DblClkStart = SelectStart;
			DblClkEnd = SelectEnd;
			Selected = true;
			ChangeSelectRegion();
			UnlockBuffer();
		}


		// The block of the text between old and new cursor positions is being selected.
		// This function enables to select several pages of output from Tera Term window.
		// add (2005.5.15 yutaka)
		public void BuffSeveralPagesSelect(int Xw, int Yw)
		//  Start text selection by mouse button down
		//    Xw: horizontal position in window coordinate (pixels)
		//    Yw: vertical
		{
			int X, Y;
			bool Right;

			VTDisp.DispConvWinToScreen(Xw, Yw, out X, out Y, out Right);
			Y = Y + VTDisp.PageStart;
			if ((Y < 0) || (Y >= VTDisp.BuffEnd)) {
				return;
			}
			if (X < 0) X = 0;
			if (X >= VTDisp.NumOfColumns) {
				X = VTDisp.NumOfColumns - 1;
			}

			SelectEnd = new Point(X, Y);
			//BoxSelect = false; // box selecting disabled
			SeveralPageSelect = true;
		}

		public void BuffStartSelect(int Xw, int Yw, bool Box)
		//  Start text selection by mouse button down
		//    Xw: horizontal position in window coordinate (pixels)
		//    Yw: vertical
		//    Box: Box selection if true
		{
			int X, Y;
			bool Right;
			int TmpPtr;

			VTDisp.DispConvWinToScreen(Xw, Yw, out X, out Y, out Right);
			Y = Y + VTDisp.PageStart;
			if ((Y < 0) || (Y >= VTDisp.BuffEnd)) {
				return;
			}
			if (X < 0) X = 0;
			if (X >= VTDisp.NumOfColumns) {
				X = VTDisp.NumOfColumns - 1;
			}

			SelectEndOld = SelectEnd;
			SelectEnd = SelectStart;

			LockBuffer();
			ChangeSelectRegion();
			UnlockBuffer();

			SelectStart = new Point(X, Y);
			if (SelectStart.X < 0) {
				SelectStart.X = 0;
			}
			if (SelectStart.X > VTDisp.NumOfColumns) {
				SelectStart.X = VTDisp.NumOfColumns;
			}
			if (SelectStart.Y < 0) {
				SelectStart.Y = 0;
			}
			if (SelectStart.Y >= VTDisp.BuffEnd) {
				SelectStart.Y = VTDisp.BuffEnd - 1;
			}

			TmpPtr = GetLinePtr(SelectStart.Y);
			// check if the cursor is on the right half of a character
			if ((SelectStart.X > 0) &&
				((AttrBuff[TmpPtr + SelectStart.X - 1] & AttributeBitMasks.AttrKanji) != 0) ||
				((AttrBuff[TmpPtr + SelectStart.X] & AttributeBitMasks.AttrKanji) == 0) &&
				 Right) {
				SelectStart.X++;
			}

			SelectEnd = SelectStart;
			SelectEndOld = SelectEnd;
			VTDisp.CaretOff();
			Selected = true;
			BoxSelect = Box;
		}

		public void BuffChangeSelect(int Xw, int Yw, int NClick)
		//  Change selection region by mouse move
		//    Xw: horizontal position of the mouse cursor
		//			in window coordinate
		//    Yw: vertical
		{
			int X, Y;
			bool Right;
			int TmpPtr;
			int i;
			char b;
			bool DBCS;

			VTDisp.DispConvWinToScreen(Xw, Yw, out X, out Y, out Right);
			Y = Y + VTDisp.PageStart;

			if (X < 0) X = 0;
			if (X > VTDisp.NumOfColumns) {
				X = VTDisp.NumOfColumns;
			}
			if (Y < 0) Y = 0;
			if (Y >= VTDisp.BuffEnd) {
				Y = VTDisp.BuffEnd - 1;
			}

			TmpPtr = GetLinePtr(Y);
			LockBuffer();
			// check if the cursor is on the right half of a character
			if ((X > 0) &&
				((AttrBuff[TmpPtr + X - 1] & AttributeBitMasks.AttrKanji) != 0) ||
				(X < VTDisp.NumOfColumns) &&
				((AttrBuff[TmpPtr + X] & AttributeBitMasks.AttrKanji) == 0) &&
				Right) {
				X++;
			}

			if (X > VTDisp.NumOfColumns) {
				X = VTDisp.NumOfColumns;
			}

			// check URL string on mouse over(2005/4/3 yutaka)
			if (NClick == 0) {
				// クリッカブルURLが有効の場合のみ、マウスカーソルを変形させる。(2009.8.27 yutaka)
				if (ts.EnableClickableUrl) {
					if ((AttrBuff[TmpPtr + X] & AttributeBitMasks.AttrURL) != 0) {
						ttwinman.HVTWin.Cursor = Cursors.Hand;
					}
					else {
						ttwinman.HVTWin.Cursor = ts.MouseCursorName;
						//SetCursor(LoadCursor(null, IDC_IBEAM));
					}
				}

				UnlockBuffer();
				return;
			}

#if false
	/* start - ishizaki */
	if (ts.EnableClickableUrl && (NClick == 2) && (AttrBuff[TmpPtr+X] & AttributeBitMasks.AttrURL)) {
		invokeBrowser(TmpPtr+X);

		SelectStart = new Point(0, 0);
		SelectEnd = new Point(0, 0);
		SelectEndOld = new Point(0, 0);
		Selected = false;
		goto end;
	}
	/* end - ishizaki */
#endif

			SelectEnd = new Point(X, Y);

			if (NClick == 2) { // drag after double click
				if ((SelectEnd.Y > SelectStart.Y) ||
					(SelectEnd.Y == SelectStart.Y) &&
					(SelectEnd.X >= SelectStart.X)) {
					if (SelectStart.X == DblClkEnd.X) {
						SelectEnd = DblClkStart;
						ChangeSelectRegion();
						SelectStart = DblClkStart;
						SelectEnd = new Point(X, Y);
					}
					MoveCharPtr(TmpPtr, ref X, -1);
					if (X < SelectStart.X) {
						X = SelectStart.X;
					}

					i = 1;
					if (IsDelimiter(TmpPtr, X)) {
						b = CodeBuff[TmpPtr + X];
						DBCS = (AttrBuff[TmpPtr + X] & AttributeBitMasks.AttrKanji) != 0;
						while ((i != 0) &&
							   ((b == CodeBuff[TmpPtr + SelectEnd.X]) ||
								DBCS &&
								((AttrBuff[TmpPtr + SelectEnd.X] & AttributeBitMasks.AttrKanji) != 0))) {
							int x = SelectEnd.X;
							i = MoveCharPtr(TmpPtr, ref x, 1); // move right
							SelectEnd.X = x;
						}
					}
					else {
						while ((i != 0) &&
							   !IsDelimiter(TmpPtr, SelectEnd.X)) {
							int x = SelectEnd.X;
							i = MoveCharPtr(TmpPtr, ref x, 1); // move right
							SelectEnd.X = x;
						}
					}
					if (i == 0) {
						SelectEnd.X = VTDisp.NumOfColumns;
					}
				}
				else {
					if (SelectStart.X == DblClkStart.X) {
						SelectEnd = DblClkEnd;
						ChangeSelectRegion();
						SelectStart = DblClkEnd;
						SelectEnd = new Point(X, Y);
					}
					if (IsDelimiter(TmpPtr, SelectEnd.X)) {
						b = CodeBuff[TmpPtr + SelectEnd.X];
						DBCS = (AttrBuff[TmpPtr + SelectEnd.X] & AttributeBitMasks.AttrKanji) != 0;
						while ((SelectEnd.X > 0) &&
							   ((b == CodeBuff[TmpPtr + SelectEnd.X]) ||
							   DBCS &&
							   ((AttrBuff[TmpPtr + SelectEnd.X] & AttributeBitMasks.AttrKanji) != 0))) {
							int x = SelectEnd.X;
							MoveCharPtr(TmpPtr, ref x, -1); // move left
							SelectEnd.X = x;
						}
						if ((b != CodeBuff[TmpPtr + SelectEnd.X]) &&
							!(DBCS &&
							((AttrBuff[TmpPtr + SelectEnd.X] & AttributeBitMasks.AttrKanji) != 0))) {
							int x = SelectEnd.X;
							MoveCharPtr(TmpPtr, ref x, 1);
							SelectEnd.X = x;
						}
					}
					else {
						while ((SelectEnd.X > 0) &&
							   !IsDelimiter(TmpPtr, SelectEnd.X)) {
							int x = SelectEnd.X;
							MoveCharPtr(TmpPtr, ref x, -1); // move left
							SelectEnd.X = x;
						}
						if (IsDelimiter(TmpPtr, SelectEnd.X)) {
							int x = SelectEnd.X;
							MoveCharPtr(TmpPtr, ref x, 1);
							SelectEnd.X = x;
						}
					}
				}
			}
			else if (NClick == 3) { // drag after tripple click
				if ((SelectEnd.Y > SelectStart.Y) ||
					(SelectEnd.Y == SelectStart.Y) &&
					(SelectEnd.X >= SelectStart.X)) {
					if (SelectStart.X == DblClkEnd.X) {
						SelectEnd = DblClkStart;
						ChangeSelectRegion();
						SelectStart = DblClkStart;
						SelectEnd = new Point(X, Y);
					}
					SelectEnd.X = VTDisp.NumOfColumns;
				}
				else {
					if (SelectStart.X == DblClkStart.X) {
						SelectEnd = DblClkEnd;
						ChangeSelectRegion();
						SelectStart = DblClkEnd;
						SelectEnd = new Point(X, Y);
					}
					SelectEnd.X = 0;
				}
			}

#if false
	/* start - ishizaki */
end:
	/* end - ishizaki */
#endif

			ChangeSelectRegion();
			UnlockBuffer();
		}

		public void BuffEndSelect()
		//  End text selection by mouse button up
		{
			Selected = (SelectStart.X != SelectEnd.X) ||
					   (SelectStart.Y != SelectEnd.Y);
			if (Selected) {
				if (BoxSelect) {
					if (SelectStart.X > SelectEnd.X) {
						SelectEndOld.X = SelectStart.X;
						SelectStart.X = SelectEnd.X;
						SelectEnd.X = SelectEndOld.X;
					}
					if (SelectStart.Y > SelectEnd.Y) {
						SelectEndOld.Y = SelectStart.Y;
						SelectStart.Y = SelectEnd.Y;
						SelectEnd.Y = SelectEndOld.Y;
					}
				}
				else if ((SelectEnd.Y < SelectStart.Y) ||
						 (SelectEnd.Y == SelectStart.Y) &&
						  (SelectEnd.X < SelectStart.X)) {
					SelectEndOld = SelectStart;
					SelectStart = SelectEnd;
					SelectEnd = SelectEndOld;
				}

				if (SeveralPageSelect) { // yutaka
										 // ページをまたぐ選択の場合、Mouse button up時にリージョンを塗り替える。
					LockBuffer();
					ChangeSelectRegion();
					UnlockBuffer();
					SeveralPageSelect = false;
					ttwinman.HVTWin.Invalidate(true); // ちょっと画面がちらつく
				}

				/* copy to the clipboard */
				if (ts.AutoTextCopy > 0) {
					LockBuffer();
					BuffCBCopy(false);
					UnlockBuffer();
				}
			}
		}

		public void BuffChangeWinSize(int Nx, int Ny)
		// Change window size
		//   Nx: new window width (number of characters)
		//   Ny: new window hight
		{
			if (Nx == 0) {
				Nx = 1;
			}
			if (Ny == 0) {
				Ny = 1;
			}

			if (ts.TermIsWin &&
				((Nx != VTDisp.NumOfColumns) || (Ny != VTDisp.NumOfLines))) {
				LockBuffer();
				BuffChangeTerminalSize(Nx, Ny - StatusLine);
				UnlockBuffer();
				Nx = VTDisp.NumOfColumns;
				Ny = VTDisp.NumOfLines;
			}
			if (Nx > VTDisp.NumOfColumns) {
				Nx = VTDisp.NumOfColumns;
			}
			if (Ny > VTDisp.BuffEnd) {
				Ny = VTDisp.BuffEnd;
			}
			VTDisp.DispChangeWinSize(Nx, Ny);
		}

		public void BuffChangeTerminalSize(int Nx, int Ny)
		{
			int i, Nb, W, H;
			bool St;

			Ny = Ny + StatusLine;
			if (Nx < 1) {
				Nx = 1;
			}
			if (Ny < 1) {
				Ny = 1;
			}
			if (Nx > tttypes.TermWidthMax) {
				Nx = tttypes.TermWidthMax;
			}
			if (ts.ScrollBuffMax > BuffYMax) {
				ts.ScrollBuffMax = BuffYMax;
			}
			if (Ny > ts.ScrollBuffMax) {
				Ny = ts.ScrollBuffMax;
			}

			St = isCursorOnStatusLine();
			if ((Nx != VTDisp.NumOfColumns) || (Ny != VTDisp.NumOfLines)) {
				if ((ts.ScrollBuffSize < Ny) ||
					(ts.EnableScrollBuff == 0)) {
					Nb = Ny;
				}
				else {
					Nb = ts.ScrollBuffSize;
				}

				if (!ChangeBuffer(Nx, Nb)) {
					return;
				}
				if (ts.EnableScrollBuff > 0) {
					ts.ScrollBuffSize = NumOfLinesInBuff;
				}
				if (Ny > NumOfLinesInBuff) {
					Ny = NumOfLinesInBuff;
				}

				if ((ts.TermFlag & TerminalFlags.TF_CLEARONRESIZE) == 0 && Ny != VTDisp.NumOfLines) {
					if (Ny > VTDisp.NumOfLines) {
						VTDisp.CursorY += Ny - VTDisp.NumOfLines;
						if (Ny > VTDisp.BuffEnd) {
							VTDisp.CursorY -= Ny - VTDisp.BuffEnd;
							VTDisp.BuffEnd = Ny;
						}
					}
					else {
						if (Ny > VTDisp.CursorY + StatusLine + 1) {
							VTDisp.BuffEnd -= VTDisp.NumOfLines - Ny;
						}
						else {
							VTDisp.BuffEnd -= VTDisp.NumOfLines - 1 - StatusLine - VTDisp.CursorY;
							VTDisp.CursorY = Ny - 1 - StatusLine;
						}
					}
				}

				VTDisp.NumOfColumns = Nx;
				VTDisp.NumOfLines = Ny;
				ts.TerminalWidth = Nx;
				ts.TerminalHeight = Ny - StatusLine;

				VTDisp.PageStart = VTDisp.BuffEnd - VTDisp.NumOfLines;
			}

			if ((ts.TermFlag & TerminalFlags.TF_CLEARONRESIZE) != 0) {
				BuffScroll(VTDisp.NumOfLines, VTDisp.NumOfLines - 1);
			}

			/* Set Cursor */
			if ((ts.TermFlag & TerminalFlags.TF_CLEARONRESIZE) != 0) {
				VTDisp.CursorX = 0;
				CursorRightM = VTDisp.NumOfColumns - 1;
				if (St) {
					VTDisp.CursorY = VTDisp.NumOfLines - 1;
					CursorTop = VTDisp.CursorY;
					CursorBottom = VTDisp.CursorY;
				}
				else {
					VTDisp.CursorY = 0;
					CursorTop = 0;
					CursorBottom = VTDisp.NumOfLines - 1 - StatusLine;
				}
			}
			else {
				CursorRightM = VTDisp.NumOfColumns - 1;
				if (VTDisp.CursorX >= VTDisp.NumOfColumns) {
					VTDisp.CursorX = VTDisp.NumOfColumns - 1;
				}
				if (St) {
					VTDisp.CursorY = VTDisp.NumOfLines - 1;
					CursorTop = VTDisp.CursorY;
					CursorBottom = VTDisp.CursorY;
				}
				else {
					if (VTDisp.CursorY >= VTDisp.NumOfLines - StatusLine) {
						VTDisp.CursorY = VTDisp.NumOfLines - 1 - StatusLine;
					}
					CursorTop = 0;
					CursorBottom = VTDisp.NumOfLines - 1 - StatusLine;
				}
			}
			CursorLeftM = 0;

			SelectStart = new Point(0, 0);
			SelectEnd = SelectStart;
			Selected = false;

			/* Tab stops */
			NTabStops = (VTDisp.NumOfColumns - 1) >> 3;
			for (i = 1; i <= NTabStops; i++) {
				TabStops[i - 1] = (short)(i * 8);
			}

			if (ts.TermIsWin) {
				W = VTDisp.NumOfColumns;
				H = VTDisp.NumOfLines;
			}
			else {
				W = VTDisp.WinWidth;
				H = VTDisp.WinHeight;
				if (ts.AutoWinResize || (VTDisp.NumOfColumns < W)) {
					W = VTDisp.NumOfColumns;
				}
				if (ts.AutoWinResize) {
					H = VTDisp.NumOfLines;
				}
				else if (VTDisp.BuffEnd < H) {
					H = VTDisp.BuffEnd;
				}
			}

			NewLine(VTDisp.PageStart + VTDisp.CursorY);

			/* Change Window Size */
			BuffChangeWinSize(W, H);
			VTDisp.WinOrgY = -VTDisp.NumOfLines;

			VTDisp.DispScrollHomePos();

			if (cv.Ready && cv.TelFlag) {
				telnet.TelInformWinSize(VTDisp.NumOfColumns, VTDisp.NumOfLines - StatusLine);
			}

			ttplug.TTXSetWinSize(VTDisp.NumOfLines - StatusLine, VTDisp.NumOfColumns); /* TTPLUG */
		}

		void ChangeWin()
		{
			int Ny;

			/* Change buffer */
			if (ts.EnableScrollBuff > 0) {
				if (ts.ScrollBuffSize < VTDisp.NumOfLines) {
					ts.ScrollBuffSize = VTDisp.NumOfLines;
				}
				Ny = ts.ScrollBuffSize;
			}
			else {
				Ny = VTDisp.NumOfLines;
			}

			if (NumOfLinesInBuff != Ny) {
				ChangeBuffer(VTDisp.NumOfColumns, Ny);
				if (ts.EnableScrollBuff > 0) {
					ts.ScrollBuffSize = NumOfLinesInBuff;
				}

				if (VTDisp.BuffEnd < VTDisp.WinHeight) {
					BuffChangeWinSize(VTDisp.WinWidth, VTDisp.BuffEnd);
				}
				else {
					BuffChangeWinSize(VTDisp.WinWidth, VTDisp.WinHeight);
				}
			}

			VTDisp.DispChangeWin();
		}

		public void ClearBuffer()
		{
			/* Reset buffer */
			VTDisp.PageStart = 0;
			BuffStartAbs = 0;
			VTDisp.BuffEnd = VTDisp.NumOfLines;
			if (VTDisp.NumOfLines == NumOfLinesInBuff) {
				BuffEndAbs = 0;
			}
			else {
				BuffEndAbs = VTDisp.NumOfLines;
			}

			SelectStart = new Point(0, 0);
			SelectEnd = SelectStart;
			SelectEndOld = SelectStart;
			Selected = false;

			NewLine(0);
			memset(CodeBuff, 0, ' ', BufferSize);
			memset(AttrBuff, 0, AttributeBitMasks.AttrDefault, BufferSize);
			memset(AttrBuff2, 0, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, BufferSize);
			memset(AttrBuffFG, 0, CurCharAttr.Fore, BufferSize);
			memset(AttrBuffBG, 0, CurCharAttr.Back, BufferSize);

			/* Home position */
			VTDisp.CursorX = 0;
			VTDisp.CursorY = 0;
			VTDisp.WinOrgX = 0;
			VTDisp.WinOrgY = 0;
			VTDisp.NewOrgX = 0;
			VTDisp.NewOrgY = 0;

			/* Top/bottom margin */
			CursorTop = 0;
			CursorBottom = VTDisp.NumOfLines - 1;
			CursorLeftM = 0;
			CursorRightM = VTDisp.NumOfColumns - 1;

			StrChangeCount = 0;

			VTDisp.DispClearWin();
		}

		public void SetTabStop()
		{
			int i, j;

			if (NTabStops < VTDisp.NumOfColumns) {
				i = 0;
				while ((TabStops[i] < VTDisp.CursorX) && (i < NTabStops)) {
					i++;
				}

				if ((i < NTabStops) && (TabStops[i] == VTDisp.CursorX)) {
					return;
				}

				for (j = NTabStops; j >= i + 1; j--) {
					TabStops[j] = TabStops[j - 1];
				}
				TabStops[i] = (short)VTDisp.CursorX;
				NTabStops++;
			}
		}

		public void CursorForwardTab(int count, bool AutoWrapMode)
		{
			int i, LineEnd;
			bool WrapState;

			WrapState = Wrap;

			if (VTDisp.CursorX > CursorRightM || VTDisp.CursorY < CursorTop || VTDisp.CursorY > CursorBottom)
				LineEnd = VTDisp.NumOfColumns - 1;
			else
				LineEnd = CursorRightM;

			for (i = 0; i < NTabStops && TabStops[i] <= VTDisp.CursorX; i++)
				;

			i += count - 1;

			if (i < NTabStops && TabStops[i] <= LineEnd) {
				MoveCursor(TabStops[i], VTDisp.CursorY);
			}
			else {
				MoveCursor(LineEnd, VTDisp.CursorY);
				if (!ts.VTCompatTab) {
					Wrap = AutoWrapMode;
				}
				else {
					Wrap = WrapState;
				}
			}
		}

		public void CursorBackwardTab(int count)
		{
			int i, LineStart;

			if (VTDisp.CursorX < CursorLeftM || VTDisp.CursorY < CursorTop || VTDisp.CursorY > CursorBottom)
				LineStart = 0;
			else
				LineStart = CursorLeftM;

			for (i = 0; i < NTabStops && TabStops[i] < VTDisp.CursorX; i++)
				;

			if (i < count || TabStops[i - count] < LineStart) {
				MoveCursor(LineStart, VTDisp.CursorY);
			}
			else {
				MoveCursor(TabStops[i - count], VTDisp.CursorY);
			}
		}

		public void ClearTabStop(int Ps)
		// Clear tab stops
		//   Ps = 0: clear the tab stop at cursor
		//      = 3: clear all tab stops
		{
			int i, j;

			if (NTabStops > 0) {
				switch (Ps) {
				case 0:
					if ((ts.TabStopFlag & TabStopflags.TABF_TBC0) != 0) {
						i = 0;
						while ((TabStops[i] != VTDisp.CursorX) && (i < NTabStops - 1)) {
							i++;
						}
						if (TabStops[i] == VTDisp.CursorX) {
							NTabStops--;
							for (j = i; j <= NTabStops; j++) {
								TabStops[j] = TabStops[j + 1];
							}
						}
					}
					break;
				case 3:
					if ((ts.TabStopFlag & TabStopflags.TABF_TBC3) != 0)
						NTabStops = 0;
					break;
				}
			}
		}

		public void ShowStatusLine(int Show)
		// show/hide status line
		{
			int Ny, Nb, W, H;

			BuffUpdateScroll();
			if (Show == StatusLine) {
				return;
			}
			StatusLine = Show;

			if (StatusLine == 0) {
				VTDisp.NumOfLines--;
				VTDisp.BuffEnd--;
				BuffEndAbs = VTDisp.PageStart + VTDisp.NumOfLines;
				if (BuffEndAbs >= NumOfLinesInBuff) {
					BuffEndAbs = BuffEndAbs - NumOfLinesInBuff;
				}
				Ny = VTDisp.NumOfLines;
			}
			else {
				Ny = ts.TerminalHeight + 1;
			}

			if ((ts.ScrollBuffSize < Ny) ||
				(ts.EnableScrollBuff == 0)) {
				Nb = Ny;
			}
			else {
				Nb = ts.ScrollBuffSize;
			}

			if (!ChangeBuffer(VTDisp.NumOfColumns, Nb)) {
				return;
			}
			if (ts.EnableScrollBuff > 0) {
				ts.ScrollBuffSize = NumOfLinesInBuff;
			}
			if (Ny > NumOfLinesInBuff) {
				Ny = NumOfLinesInBuff;
			}

			VTDisp.NumOfLines = Ny;
			ts.TerminalHeight = Ny - StatusLine;

			if (StatusLine == 1) {
				BuffScroll(1, VTDisp.NumOfLines - 1);
			}

			if (ts.TermIsWin) {
				W = VTDisp.NumOfColumns;
				H = VTDisp.NumOfLines;
			}
			else {
				W = VTDisp.WinWidth;
				H = VTDisp.WinHeight;
				if (ts.AutoWinResize || (VTDisp.NumOfColumns < W)) {
					W = VTDisp.NumOfColumns;
				}
				if (ts.AutoWinResize) {
					H = VTDisp.NumOfLines;
				}
				else if (VTDisp.BuffEnd < H) {
					H = VTDisp.BuffEnd;
				}
			}

			VTDisp.PageStart = VTDisp.BuffEnd - VTDisp.NumOfLines;
			NewLine(VTDisp.PageStart + VTDisp.CursorY);

			/* Change Window Size */
			BuffChangeWinSize(W, H);
			VTDisp.WinOrgY = -VTDisp.NumOfLines;
			VTDisp.DispScrollHomePos();

			MoveCursor(VTDisp.CursorX, VTDisp.CursorY);
		}

		public void BuffLineContinued(bool mode)
		{
			if (ts.EnableContinuedLineCopy) {
				if (mode) {
					AttrBuff[AttrLine + 0] |= AttributeBitMasks.AttrLineContinued;
				}
				else {
					AttrBuff[AttrLine + 0] &= ~AttributeBitMasks.AttrLineContinued;
				}
			}
		}

		public void BuffSetCurCharAttr(TCharAttr Attr)
		{
			CurCharAttr = Attr;
			VTDisp.DispSetCurCharAttr(Attr);
		}

		public void BuffSaveScreen()
		{
			char[] CodeDest;
			AttributeBitMasks[] AttrDest, AttrDest2;
			ColorCodes[] AttrDestFG, AttrDestBG;
			int ScrSize;
			int SrcPtr, DestPtr;
			int i;

			if (SaveCodeBuff == null) {
				ScrSize = VTDisp.NumOfColumns * VTDisp.NumOfLines;

				CodeDest = new char[ScrSize];
				AttrDest = new AttributeBitMasks[ScrSize];
				AttrDest2 = new AttributeBitMasks[ScrSize];
				AttrDestFG = new ColorCodes[ScrSize];
				AttrDestBG = new ColorCodes[ScrSize];

				SaveBuffX = VTDisp.NumOfColumns;
				SaveBuffY = VTDisp.NumOfLines;

				SrcPtr = GetLinePtr(VTDisp.PageStart);
				DestPtr = 0;

				for (i = 0; i < VTDisp.NumOfLines; i++) {
					memmove(CodeDest, DestPtr, CodeBuff, SrcPtr, VTDisp.NumOfColumns);
					memmove(AttrDest, DestPtr, AttrBuff, SrcPtr, VTDisp.NumOfColumns);
					memmove(AttrDest2, DestPtr, AttrBuff2, SrcPtr, VTDisp.NumOfColumns);
					memmove(AttrDestFG, DestPtr, AttrBuffFG, SrcPtr, VTDisp.NumOfColumns);
					memmove(AttrDestBG, DestPtr, AttrBuffBG, SrcPtr, VTDisp.NumOfColumns);
					SrcPtr = NextLinePtr(SrcPtr);
					DestPtr += VTDisp.NumOfColumns;
				}
			}
			return;
		}

		public void BuffRestoreScreen()
		{
			int ScrSize;
			int SrcPtr, DestPtr;
			int i, CopyX, CopyY;

			if (SaveCodeBuff != null) {
				ScrSize = SaveBuffX * SaveBuffY;

				CopyX = (SaveBuffX > VTDisp.NumOfColumns) ? VTDisp.NumOfColumns : SaveBuffX;
				CopyY = (SaveBuffY > VTDisp.NumOfLines) ? VTDisp.NumOfLines : SaveBuffY;

				SrcPtr = 0;
				DestPtr = GetLinePtr(VTDisp.PageStart);

				for (i = 0; i < CopyY; i++) {
					memmove(CodeBuff, DestPtr, SaveCodeBuff, SrcPtr, CopyX);
					memmove(AttrBuff, DestPtr, SaveAttrBuff, SrcPtr, CopyX);
					memmove(AttrBuff2, DestPtr, SaveAttrBuff2, SrcPtr, CopyX);
					memmove(AttrBuffFG, DestPtr, SaveAttrBuffFG, SrcPtr, CopyX);
					memmove(AttrBuffBG, DestPtr, SaveAttrBuffBG, SrcPtr, CopyX);
					if ((AttrBuff[DestPtr + CopyX - 1] & AttributeBitMasks.AttrKanji) != 0) {
						CodeBuff[DestPtr + CopyX - 1] = ' ';
						AttrBuff[DestPtr + CopyX - 1] ^= AttributeBitMasks.AttrKanji;
					}
					SrcPtr += SaveBuffX;
					DestPtr = NextLinePtr(DestPtr);
				}
				BuffUpdateRect(VTDisp.WinOrgX, VTDisp.WinOrgY, VTDisp.WinOrgX + VTDisp.WinWidth - 1, VTDisp.WinOrgY + VTDisp.WinHeight - 1);

				SaveCodeBuff = null;
				SaveAttrBuff = null;
				SaveAttrBuff2 = null;
				SaveAttrBuffFG = null;
				SaveAttrBuffBG = null;
			}
			return;
		}

		void BuffDiscardSavedScreen()
		{
			if (SaveCodeBuff != null) {
				SaveCodeBuff = null;
				SaveAttrBuff = null;
				SaveAttrBuff2 = null;
				SaveAttrBuffFG = null;
				SaveAttrBuffBG = null;
			}
		}

		public void BuffSelectedEraseCurToEnd()
		// Erase characters from cursor to the end of screen
		{
			int TmpPtr;
			int offset;
			int i, j, YEnd;

			NewLine(VTDisp.PageStart + VTDisp.CursorY);
			if ((AttrBuff2[AttrLine2 + VTDisp.CursorX] & AttributeBitMasks.Attr2Protect) == 0) {
				EraseKanji(1); /* if cursor is on right half of a kanji, erase the kanji */
			}
			offset = VTDisp.CursorX;
			TmpPtr = GetLinePtr(VTDisp.PageStart + VTDisp.CursorY);
			YEnd = VTDisp.NumOfLines - 1;
			if (StatusLine != 0 && !isCursorOnStatusLine()) {
				YEnd--;
			}
			for (i = VTDisp.CursorY; i <= YEnd; i++) {
				for (j = TmpPtr + offset; j < TmpPtr + VTDisp.NumOfColumns - offset; j++) {
					if ((AttrBuff2[j] & AttributeBitMasks.Attr2Protect) == 0) {
						CodeBuff[j] = ' ';
						AttrBuff[j] &= AttributeBitMasks.AttrSgrMask;
					}
				}
				offset = 0;
				TmpPtr = NextLinePtr(TmpPtr);
			}
			/* update window */
			BuffUpdateRect(0, VTDisp.CursorY, VTDisp.NumOfColumns, YEnd);
		}

		public void BuffSelectedEraseHomeToCur()
		// Erase characters from home to cursor
		{
			int TmpPtr;
			int offset;
			int i, j, YHome;

			NewLine(VTDisp.PageStart + VTDisp.CursorY);
			if ((AttrBuff2[AttrLine2 + VTDisp.CursorX] & AttributeBitMasks.Attr2Protect) == 0) {
				EraseKanji(0); /* if cursor is on left half of a kanji, erase the kanji */
			}
			offset = VTDisp.NumOfColumns;
			if (isCursorOnStatusLine()) {
				YHome = VTDisp.CursorY;
			}
			else {
				YHome = 0;
			}
			TmpPtr = GetLinePtr(VTDisp.PageStart + YHome);
			for (i = YHome; i <= VTDisp.CursorY; i++) {
				if (i == VTDisp.CursorY) {
					offset = VTDisp.CursorX + 1;
				}
				for (j = TmpPtr; j < TmpPtr + offset; j++) {
					if ((AttrBuff2[j] & AttributeBitMasks.Attr2Protect) == 0) {
						CodeBuff[j] = ' ';
						AttrBuff[j] &= AttributeBitMasks.AttrSgrMask;
					}
				}
				TmpPtr = NextLinePtr(TmpPtr);
			}

			/* update window */
			BuffUpdateRect(0, YHome, VTDisp.NumOfColumns, VTDisp.CursorY);
		}

		public void BuffSelectedEraseScreen()
		{
			BuffSelectedEraseHomeToCur();
			BuffSelectedEraseCurToEnd();
		}

		public void BuffSelectiveEraseBox(int XStart, int YStart, int XEnd, int YEnd)
		{
			int C, i, j;
			int Ptr;

			if (XEnd > VTDisp.NumOfColumns - 1) {
				XEnd = VTDisp.NumOfColumns - 1;
			}
			if (YEnd > VTDisp.NumOfLines - 1 - StatusLine) {
				YEnd = VTDisp.NumOfLines - 1 - StatusLine;
			}
			if (XStart > XEnd) {
				return;
			}
			if (YStart > YEnd) {
				return;
			}
			C = XEnd - XStart + 1;
			Ptr = GetLinePtr(VTDisp.PageStart + YStart);
			for (i = YStart; i <= YEnd; i++) {
				if ((XStart > 0) &&
					((AttrBuff[Ptr + XStart - 1] & AttributeBitMasks.AttrKanji) != 0) &&
					((AttrBuff2[Ptr + XStart - 1] & AttributeBitMasks.Attr2Protect) == 0)) {
					CodeBuff[Ptr + XStart - 1] = ' ';
					AttrBuff[Ptr + XStart - 1] &= AttributeBitMasks.AttrSgrMask;
				}
				if ((XStart + C < VTDisp.NumOfColumns) &&
					((AttrBuff[Ptr + XStart + C - 1] & AttributeBitMasks.AttrKanji) != 0) &&
					((AttrBuff2[Ptr + XStart + C - 1] & AttributeBitMasks.Attr2Protect) == 0)) {
					CodeBuff[Ptr + XStart + C] = ' ';
					AttrBuff[Ptr + XStart + C] &= AttributeBitMasks.AttrSgrMask;
				}
				for (j = Ptr + XStart; j < Ptr + XStart + C; j++) {
					if ((AttrBuff2[j] & AttributeBitMasks.Attr2Protect) == 0) {
						CodeBuff[j] = ' ';
						AttrBuff[j] &= AttributeBitMasks.AttrSgrMask;
					}
				}
				Ptr = NextLinePtr(Ptr);
			}
			BuffUpdateRect(XStart, YStart, XEnd, YEnd);
		}

		public void BuffSelectedEraseCharsInLine(int XStart, int Count)
		// erase non-protected characters in the current line
		//  XStart: start position of erasing
		//  Count: number of characters to be erased
		{
			int i;

			bool LineContinued = false;

			if (ts.EnableContinuedLineCopy && XStart == 0 && ((AttrBuff[AttrLine + 0] & AttributeBitMasks.AttrLineContinued) != 0)) {
				LineContinued = true;
			}

			if ((AttrBuff2[AttrLine2 + VTDisp.CursorX] & AttributeBitMasks.Attr2Protect) == 0) {
				EraseKanji(1); /* if cursor is on right half of a kanji, erase the kanji */
			}

			NewLine(VTDisp.PageStart + VTDisp.CursorY);
			for (i = XStart; i < XStart + Count; i++) {
				if ((AttrBuff2[AttrLine2 + i] & AttributeBitMasks.Attr2Protect) == 0) {
					CodeBuff[CodeLine + i] = ' ';
					AttrBuff[AttrLine + i] &= AttributeBitMasks.AttrSgrMask;
				}
			}

			if (ts.EnableContinuedLineCopy) {
				if (LineContinued) {
					BuffLineContinued(true);
				}

				if (XStart + Count >= VTDisp.NumOfColumns) {
					AttrBuff[NextLinePtr(LinePtr)] &= ~AttributeBitMasks.AttrLineContinued;
				}
			}

			BuffUpdateRect(XStart, VTDisp.CursorY, XStart + Count, VTDisp.CursorY);
		}

		public void BuffScrollLeft(int count)
		{
			int i, MoveLen;
			int LPtr, Ptr;

			UpdateStr();

			LPtr = GetLinePtr(VTDisp.PageStart + CursorTop);
			MoveLen = CursorRightM - CursorLeftM + 1 - count;
			for (i = CursorTop; i <= CursorBottom; i++) {
				Ptr = LPtr + CursorLeftM;

				if ((AttrBuff[LPtr + CursorRightM] & AttributeBitMasks.AttrKanji) != 0) {
					CodeBuff[LPtr + CursorRightM] = ' ';
					AttrBuff[LPtr + CursorRightM] &= ~AttributeBitMasks.AttrKanji;
					if (CursorRightM < VTDisp.NumOfColumns - 1) {
						CodeBuff[LPtr + CursorRightM + 1] = ' ';
					}
				}

				if ((AttrBuff[Ptr + count - 1] & AttributeBitMasks.AttrKanji) != 0) {
					CodeBuff[Ptr + count] = ' ';
				}

				if (CursorLeftM > 0 && (AttrBuff[Ptr - 1] & AttributeBitMasks.AttrKanji) != 0) {
					CodeBuff[Ptr - 1] = ' ';
					AttrBuff[Ptr - 1] &= ~AttributeBitMasks.AttrKanji;
				}

				memmove(CodeBuff, Ptr, CodeBuff, Ptr + count, MoveLen);
				memmove(AttrBuff, Ptr, AttrBuff, Ptr + count, MoveLen);
				memmove(AttrBuff2, Ptr, AttrBuff2, Ptr + count, MoveLen);
				memmove(AttrBuffFG, Ptr, AttrBuffFG, Ptr + count, MoveLen);
				memmove(AttrBuffBG, Ptr, AttrBuffBG, Ptr + count, MoveLen);

				memset(CodeBuff, Ptr + MoveLen, ' ', count);
				memset(AttrBuff, Ptr + MoveLen, AttributeBitMasks.AttrDefault, count);
				memset(AttrBuff2, Ptr + MoveLen, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, count);
				memset(AttrBuffFG, Ptr + MoveLen, CurCharAttr.Fore, count);
				memset(AttrBuffBG, Ptr + MoveLen, CurCharAttr.Back, count);

				LPtr = NextLinePtr(LPtr);
			}

			BuffUpdateRect(CursorLeftM - (CursorLeftM > 0 ? 1 : 0), CursorTop, CursorRightM + (CursorRightM < VTDisp.NumOfColumns - 1 ? 1 : 0), CursorBottom);
		}

		public void BuffScrollRight(int count)
		{
			int i, MoveLen;
			int LPtr, Ptr;

			UpdateStr();

			LPtr = GetLinePtr(VTDisp.PageStart + CursorTop);
			MoveLen = CursorRightM - CursorLeftM + 1 - count;
			for (i = CursorTop; i <= CursorBottom; i++) {
				Ptr = LPtr + CursorLeftM;

				if (CursorRightM < VTDisp.NumOfColumns - 1 && (AttrBuff[LPtr + CursorRightM] & AttributeBitMasks.AttrKanji) != 0) {
					CodeBuff[LPtr + CursorRightM + 1] = ' ';
				}

				if (CursorLeftM > 0 && ((AttrBuff[Ptr - 1] & AttributeBitMasks.AttrKanji) != 0)) {
					CodeBuff[Ptr - 1] = ' ';
					AttrBuff[Ptr - 1] &= ~AttributeBitMasks.AttrKanji;
					CodeBuff[Ptr] = ' ';
				}

				memmove(CodeBuff, Ptr + count, CodeBuff, Ptr, MoveLen);

				memmove(AttrBuff, Ptr + count, AttrBuff, Ptr, MoveLen);
				memmove(AttrBuff2, Ptr + count, AttrBuff2, Ptr, MoveLen);
				memmove(AttrBuffFG, Ptr + count, AttrBuffFG, Ptr, MoveLen);
				memmove(AttrBuffBG, Ptr + count, AttrBuffBG, Ptr, MoveLen);

				memset(CodeBuff, Ptr, ' ', count);

				memset(AttrBuff, Ptr, AttributeBitMasks.AttrDefault, count);
				memset(AttrBuff2, Ptr, CurCharAttr.Attr2 & AttributeBitMasks.Attr2ColorMask, count);
				memset(AttrBuffFG, Ptr, CurCharAttr.Fore, count);
				memset(AttrBuffBG, Ptr, CurCharAttr.Back, count);

				if ((AttrBuff[LPtr + CursorRightM] & AttributeBitMasks.AttrKanji) != 0) {
					CodeBuff[LPtr + CursorRightM] = ' ';
					AttrBuff[LPtr + CursorRightM] &= ~AttributeBitMasks.AttrKanji;
				}

				LPtr = NextLinePtr(LPtr);
			}

			BuffUpdateRect(CursorLeftM - (CursorLeftM > 0 ? 1 : 0), CursorTop, CursorRightM + (CursorRightM < VTDisp.NumOfColumns - 1 ? 1 : 0), CursorBottom);
		}

		// 現在行をまるごとバッファに格納する。返り値は現在のカーソル位置(X)。
		int BuffGetCurrentLineData(char[] buf, int bufsize)
		{
			int Ptr;

			Ptr = GetLinePtr(VTDisp.PageStart + VTDisp.CursorY);
			memset(buf, 0, '\0', bufsize);
			memmove(buf, 0, CodeBuff, Ptr, Math.Min(VTDisp.NumOfColumns, bufsize - 1));
			return (VTDisp.CursorX);
		}

		// 全バッファから指定した行を返す。
		int BuffGetAnyLineData(int offset_y, char[] buf, int bufsize)
		{
			int Ptr;
			int copysize = 0;

			if (offset_y >= VTDisp.BuffEnd)
				return -1;

			Ptr = GetLinePtr(offset_y);
			memset(buf, 0, '\0', bufsize);
			copysize = Math.Min(VTDisp.NumOfColumns, bufsize - 1);
			memmove(buf, 0, CodeBuff, Ptr, copysize);

			return (copysize);
		}


		bool BuffCheckMouseOnURL(int Xw, int Yw)
		{
			int X, Y;
			int TmpPtr;
			bool Result, Right;

			VTDisp.DispConvWinToScreen(Xw, Yw, out X, out Y, out Right);
			Y += VTDisp.PageStart;

			if (X < 0)
				X = 0;
			else if (X > VTDisp.NumOfColumns)
				X = VTDisp.NumOfColumns;
			if (Y < 0)
				Y = 0;
			else if (Y >= VTDisp.BuffEnd)
				Y = VTDisp.BuffEnd - 1;

			TmpPtr = GetLinePtr(Y);
			LockBuffer();

			if ((AttrBuff[TmpPtr + X] & AttributeBitMasks.AttrURL) != 0)
				Result = true;
			else
				Result = false;

			UnlockBuffer();

			return Result;
		}

		internal void Init(ProgramDatas datas)
		{
			ttwinman = datas.ttwinman;
			teraprn = datas.teraprn;
			clipboar = datas.clipboar;
			ts = datas.TTTSet;
			cv = datas.TComVar;
			VTDisp = datas.VTDisp;
		}

		public const int GMEM_MOVEABLE = 0;

		public static T[] GlobalAlloc<T>(int mode, int NewSize)
		{
			return new T[NewSize];
		}

		public static void GlobalFree<T>(T[] HCodeNew)
		{
		}

		public static T[] GlobalLock<T>(T[] HCodeBuff)
		{
			return HCodeBuff;
		}

		public static void GlobalUnlock<T>(T[] HCodeBuff)
		{
		}

		public const int CP_ACP = 0;			// default to ANSI code page
		public const int CP_OEMCP = 1;			// default to OEM  code page
		public const int CP_MACCP = 2;			// default to MAC  code page
		public const int CP_THREAD_ACP = 3;		// current thread's ANSI code page
		public const int CP_SYMBOL = 42;		// SYMBOL translations

		public const int CP_UTF7 = 65000;		// UTF-7 translation
		public const int CP_UTF8 = 65001;		// UTF-8 translation

		public static int MultiByteToWideChar(int cp, int v1, char[] cbbuff, int v2, object p, int v3)
		{
			throw new NotImplementedException();
		}
	}
}

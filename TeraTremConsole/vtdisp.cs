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
#if ALPHABLEND_TYPE2
		static int CRTWidth, CRTHeight;
#endif
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
		IntPtr Background;
		Color[] ANSIColor = new Color[256];
		int[] Dx = new int[tttypes.TermWidthMax];

		// caret variables
		int CaretStatus;
		bool CaretEnabled = true;

		// ---- device context and status flags
		IntPtr VTDC = IntPtr.Zero; /* Device context for ControlCharacters.VT window */
		TCharAttr DCAttr;
		TCharAttr CurCharAttr;
		bool DCReverse;
		IntPtr DCPrevFont;

		public TCharAttr DefCharAttr = new TCharAttr(AttributeBitMasks.AttrDefault, AttributeBitMasks.AttrDefault, (ColorCodes)AttributeBitMasks.AttrDefaultFG, (ColorCodes)AttributeBitMasks.AttrDefaultBG);

		// scrolling
		int ScrollCount = 0;
		int dScroll = 0;
		int SRegionTop;
		int SRegionBottom;

#if ALPHABLEND_TYPE2
		//<!--by AKASI

		const string BG_SECTION = "BG";

		enum BG_TYPE { BG_COLOR = 0, BG_PICTURE, BG_WALLPAPER }
		enum BG_PATTERN { BG_STRETCH = 0, BG_TILE, BG_CENTER, BG_FIT_WIDTH, BG_FIT_HEIGHT, BG_AUTOFIT, BG_AUTOFILL }

		struct BGSrc
		{
			Graphics hdc;
			BG_TYPE type;
			BG_PATTERN pattern;
			bool antiAlias;
			Color color;
			int alpha;
			int width;
			int height;
			string file;
			string fileTmp;
		}

		BGSrc BGDest = new BGSrc();
		BGSrc BGSrc1 = new BGSrc();
		BGSrc BGSrc2 = new BGSrc();

		int BGEnable;
		int BGReverseTextAlpha;
		int BGUseAlphaBlendAPI;
		bool BGNoFrame;
		bool BGFastSizeMove;

		string BGSPIPath;
#endif
		Color[] BGVTColor = new Color[2];
		Color[] BGVTBoldColor = new Color[2];
		Color[] BGVTBlinkColor = new Color[2];
		Color[] BGVTReverseColor = new Color[2];
		/* begin - ishizaki */
		Color[] BGURLColor = new Color[2];
		/* end - ishizaki */
#if ALPHABLEND_TYPE2
		Rectangle BGPrevRect;
		bool BGReverseText;

		bool BGNoCopyBits;
		bool BGInSizeMove;
		Brush BGBrushInSizeMove;

		Graphics hdcBGWork;
		Graphics hdcBGBuffer;
		Graphics hdcBG;

		struct WallpaperInfo
		{
			string filename;
			int pattern;
		}

		struct BGBLENDFUNCTION
		{
			byte BlendOp;
			byte BlendFlags;
			byte SourceConstantAlpha;
			byte AlphaFormat;
		}

		delegate bool BGAlphaBlend(Graphics sg, int sx, int sy, int sw, int sh, Graphics dg, int dx, int dy, int dw, int dh, BGBLENDFUNCTION bf);
		delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);
		delegate bool BGEnumDisplayMonitors(Graphics g, ref RECT rc, MonitorEnumProc mep, IntPtr lparam);


		//便利関数☆

		void dprintf(string format, params object[] args)
		{
			string buffer = String.Format(format, args);

			System.Diagnostics.Debug.WriteLine(buffer);
		}

		IntPtr CreateScreenCompatibleBitmap(int width, int height)
		{
			Graphics hdc;
			IntPtr hbm;

#if DEBUG
			dprintf("CreateScreenCompatibleBitmap : width = %d height = %d", width, height);
#endif

			hdc = Graphics.FromHwnd(IntPtr.Zero);

			hbm = CreateCompatibleBitmap(hdc, width, height);

			ReleaseDC(null, hdc);

#if DEBUG
			if (!hbm)
				dprintf("CreateScreenCompatibleBitmap : fail in CreateCompatibleBitmap");
#endif

			return hbm;
		}

		IntPtr CreateDIB24BPP(int width, int height, ref byte[] buf, ref int lenBuf)
		{
			Graphics hdc;
			IntPtr hbm;
			BITMAPINFO bmi;

#if DEBUG
			dprintf("CreateDIB24BPP : width = %d height = %d", width, height);
#endif

			if (!width || !height)
				return null;

			ZeroMemory(&bmi, bmi.Length);

			*lenBuf = ((width * 3 + 3) & ~3) * height;

			bmi.bmiHeader.biSize = bmi.bmiHeader.Length;
			bmi.bmiHeader.biWidth = width;
			bmi.bmiHeader.biHeight = height;
			bmi.bmiHeader.biPlanes = 1;
			bmi.bmiHeader.biBitCount = 24;
			bmi.bmiHeader.biSizeImage = *lenBuf;
			bmi.bmiHeader.biCompression = BI_RGB;

			hdc = Graphics.FromHwnd(IntPtr.Zero);

			hbm = CreateDIBSection(hdc, &bmi, DIB_RGB_COLORS, new IntPtr(buf), null, 0);

			ReleaseDC(null, hdc);

			return hbm;
		}

		Graphics CreateBitmapDC(IntPtr hbm)
		{
			Graphics hdc;

#if DEBUG
			dprintf("CreateBitmapDC : hbm = %x", hbm);
#endif

			hdc = CreateCompatibleDC(null);

			SaveDC(hdc);
			SelectObject(hdc, hbm);

			return hdc;
		}

		void DeleteBitmapDC(ref IntPtr hdc)
		{
			IntPtr hbm;

#if DEBUG
			dprintf("DeleteBitmapDC : *hdc = %x", hdc);
#endif

			if (hdc == IntPtr.Zero)
				return;

			hbm = GetCurrentObject(*hdc, OBJ_BITMAP);

			RestoreDC(hdc, -1);
			hbm.Dispose();
			DeleteDC(hdc);

			hdc = IntPtr.Zero;
		}

		void FillBitmapDC(Graphics hdc, Color color)
		{
			IntPtr hbm;
			BITMAP bm;
			Rectangle rect;
			Brush hBrush;

#if DEBUG
			dprintf("FillBitmapDC : hdc = %x color = %x", hdc, color);
#endif

			if (!hdc)
				return;

			hbm = GetCurrentObject(hdc, OBJ_BITMAP);
			GetObject(hbm, bm.Length, &bm);

			SetRect(&rect, 0, 0, bm.bmWidth, bm.bmHeight);
			hBrush = CreateSolidBrush(color);
			FillRect(hdc, &rect, hBrush);
			hBrush.Dispose();
		}

		IntPtr GetProcAddressWithDllName(string dllName, string procName)
		{
			HINSTANCE hDll;

			hDll = LoadLibrary(dllName);

			if (hDll)
				return GetProcAddress(hDll, procName);
			else
				return IntPtr.Zero;
		}

		void RandomFile(string filespec, string filename, int destlen)
		{
			int i;
			int file_num;
			string fullpath;
			string filePart;

			IntPtr hFind;
			WIN32_FIND_DATA fd;

			ExpandEnvironmentStrings(filespec_src, filespec, filespec.Length);

			//絶対パスに変換
			if (!GetFullPathName(filespec, tttypes.MAX_PATH, fullpath, ref filePart))
				return;

			//ファイルを数える
			hFind = FindFirstFile(fullpath, &fd);

			file_num = 0;

			if (hFind != INVALID_HANDLE_VALUE && filePart) {

				do {
					if (!(fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
						file_num++;

				} while (FindNextFile(hFind, &fd));
			}

			if (!file_num)
				return;

			FindClose(hFind);

			//何番目のファイルにするか決める。
			file_num = rand() % file_num + 1;

			hFind = FindFirstFile(fullpath, &fd);

			if (hFind != INVALID_HANDLE_VALUE) {
				i = 0;

				do {
					if (!(fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
						i++;
				} while (i < file_num && FindNextFile(hFind, &fd));

			}
			else {
				return;
			}

			FindClose(hFind);

			//ディレクトリ取得
			ZeroMemory(filename, destlen);
			{
				int tmplen;
				char* tmp;
				tmplen = filePart - fullpath + 1;
				tmp = (char*)_alloca(tmplen);
				strncpy_s(tmp, tmplen, fullpath, filePart - fullpath);
				strncpy_s(filename, destlen, tmp, _TRUNCATE);
			}
			strncat_s(filename, destlen, fd.cFileName, _TRUNCATE);
		}

		delegate int SPI_IsSupported(string s, uint dw);
		delegate int SPI_GetPicture(string s, long p2, uint p3, IntPtr p4, IntPtr p5, IntPtr p6, long p7);
		delegate int SPI_GetPluginInfo(int p1, string p2, int p3);

		bool LoadPictureWithSPI(string nameSPI, string nameFile, byte[] bufFile, long sizeFile, IntPtr hbuf, IntPtr hbmi)
		{
			HINSTANCE hSPI;
			char[] spiVersion = new char[8];
			SPI_IsSupported SPI_IsSupported;
			SPI_GetPicture SPI_GetPicture;
			SPI_GetPluginInfo SPI_GetPluginInfo;
			int ret;

			ret = false;
			hSPI = null;

			//SPI をロード
			hSPI = LoadLibrary(nameSPI);

			if (!hSPI)
				goto error;

			(FARPROC)SPI_GetPluginInfo = GetProcAddress(hSPI, "GetPluginInfo");
			(FARPROC)SPI_IsSupported = GetProcAddress(hSPI, "IsSupported");
			(FARPROC)SPI_GetPicture = GetProcAddress(hSPI, "GetPicture");

			if (!SPI_GetPluginInfo || !SPI_IsSupported || !SPI_GetPicture)
				goto error;

			//バージョンチェック
			SPI_GetPluginInfo(0, spiVersion, 8);

			if (spiVersion[2] != 'I' || spiVersion[3] != 'N')
				goto error;

			if (!SPI_IsSupported(nameFile, (ulong)bufFile))
				goto error;

			if (SPI_GetPicture(bufFile, sizeFile, 1, hbmi, hbuf, null, 0))
				goto error;

			ret = true;

		error:

			if (hSPI)
				FreeLibrary(hSPI);

			return ret;
		}

		bool SaveBitmapFile(string nameFile, byte[] pbuf, ref BITMAPINFO pbmi)
		{
			int bmiSize;
			uint writtenByte;
			IntPtr hFile;
			BITMAPFILEHEADER bfh;

			hFile = CreateFile(nameFile, GENERIC_WRITE, 0, null, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, null);

			if (hFile == INVALID_HANDLE_VALUE)
				return false;

			bmiSize = pbmi.bmiHeader.biSize;

			switch (pbmi.bmiHeader.biBitCount) {
			case 1:
				bmiSize += pbmi.bmiHeader.biClrUsed ? RGBQUAD.Length * 2 : 0;
				break;

			case 2:
				bmiSize += RGBQUAD.Length * 4;
				break;

			case 4:
				bmiSize += RGBQUAD.Length * 16;
				break;

			case 8:
				bmiSize += RGBQUAD.Length * 256;
				break;
			}

			ZeroMemory(&bfh, bfh.Length);
			bfh.bfType = MAKEWORD('B', 'M');
			bfh.bfOffBits = bfh.Length + bmiSize;
			bfh.bfSize = bfh.bfOffBits + pbmi.bmiHeader.biSizeImage;

			WriteFile(hFile, &bfh, bfh.Length, &writtenByte, 0);
			WriteFile(hFile, pbmi, bmiSize, &writtenByte, 0);
			WriteFile(hFile, pbuf, pbmi.bmiHeader.biSizeImage, &writtenByte, 0);

			CloseHandle(hFile);

			return true;
		}

		bool AlphaBlendWithoutAPI(Graphics hdcDest, int dx, int dy, int width, int height, Graphics hdcSrc, int sx, int sy, int sw, int sh, BGBLENDFUNCTION bf)
		{
			Graphics hdcDestWork, hdcSrcWork;
			int i, invAlpha, alpha;
			int lenBuf;
			byte* bufDest;
			byte* bufSrc;

			if (dx != 0 || dy != 0 || sx != 0 || sy != 0 || width != sw || height != sh)
				return false;

			hdcDestWork = CreateBitmapDC(CreateDIB24BPP(width, height, &bufDest, &lenBuf));
			hdcSrcWork = CreateBitmapDC(CreateDIB24BPP(width, height, &bufSrc, &lenBuf));

			if (!bufDest || !bufSrc)
				return false;

			BitBlt(hdcDestWork, 0, 0, width, height, hdcDest, 0, 0, SRCCOPY);
			BitBlt(hdcSrcWork, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);

			alpha = bf.SourceConstantAlpha;
			invAlpha = 255 - alpha;

			for (i = 0; i < lenBuf; i++, bufDest++, bufSrc++)
				*bufDest = (*bufDest * invAlpha + *bufSrc * alpha) >> 8;

			BitBlt(hdcDest, 0, 0, width, height, hdcDestWork, 0, 0, SRCCOPY);

			DeleteBitmapDC(&hdcDestWork);
			DeleteBitmapDC(&hdcSrcWork);

			return true;
		}

		// 画像読み込み関係

		void BGPreloadPicture(BGSrc src)
		{
			string spiPath;
			string filespec;
			string filePart;
			int fileSize;
			int readByte;
			byte[] fileBuf;

			IntPtr hbm;
			IntPtr hPictureFile;
			IntPtr hFind;
			WIN32_FIND_DATA fd;

#if DEBUG
			dprintf("Preload Picture : %s", src.file);
#endif

			//ファイルを読み込む
			hPictureFile = CreateFile(src.file, GENERIC_READ, 0, null, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, null);

			if (hPictureFile == INVALID_HANDLE_VALUE)
				return;

			fileSize = GetFileSize(hPictureFile, 0);

			//最低 2kb は確保 (Susie plugin の仕様より)
			fileBuf = Buffer.GlobalAlloc(GPTR, fileSize + 2048);

			//頭の 2kb は０で初期化
			ZeroMemory(fileBuf, 2048);

			ReadFile(hPictureFile, fileBuf, fileSize, &readByte, 0);

			CloseHandle(hPictureFile);

			// SPIPath を絶対パスに変換
			if (!GetFullPathName(BGSPIPath, tttypes.MAX_PATH, filespec, &filePart))
				return;

			//プラグインを当たっていく
			hFind = FindFirstFile(filespec, &fd);

			if (hFind != INVALID_HANDLE_VALUE && filePart) {
				//ディレクトリ取得
				ExtractDirName(filespec, spiPath);
				AppendSlash(spiPath, spiPath.Length);

				do {
					HLOCAL hbuf, hbmi;
					BITMAPINFO pbmi;
					char[] pbuf;
					string spiFileName;

					if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
						continue;

					strncpy_s(spiFileName, spiFileName.Length, spiPath, _TRUNCATE);
					strncat_s(spiFileName, spiFileName.Length, fd.cFileName, _TRUNCATE);

					if (LoadPictureWithSPI(spiFileName, src.file, fileBuf, fileSize, &hbuf, &hbmi)) {
						pbuf = LocalLock(hbuf);
						pbmi = LocalLock(hbmi);

						SaveBitmapFile(src.fileTmp, pbuf, pbmi);

						LocalUnlock(hbmi);
						LocalUnlock(hbuf);

						LocalFree(hbmi);
						LocalFree(hbuf);

						strncpy_s(src.file, src.file.Length, src.fileTmp, _TRUNCATE);

						break;
					}
				} while (FindNextFile(hFind, &fd));

				FindClose(hFind);
			}

			GlobalFree(fileBuf);

			//画像をビットマップとして読み込み

			hbm = LoadImage(0, src.file, IMAGE_BITMAP, 0, 0, LR_LOADFROMFILE);

			if (hbm) {
				BITMAP bm;

				GetObject(hbm, bm.Length, &bm);

				src.hdc = CreateBitmapDC(hbm);
				src.width = bm.bmWidth;
				src.height = bm.bmHeight;
			}
			else {
				src.type = BG_COLOR;
			}
		}

		void BGGetWallpaperInfo(ref WallpaperInfo wi)
		{
			int length;
			int style;
			int tile;
			string str;
			HKEY hKey;

			wi.pattern = BG_CENTER;
			strncpy_s(wi.filename, wi.filename.Length, "", _TRUNCATE);

			//レジストリキーのオープン
			if (RegOpenKeyEx(HKEY_CURRENT_USER, "Control Panel\\Desktop", 0, KEY_READ, &hKey) != ERROR_SUCCESS)
				return;

			//壁紙名ゲット
			length = tttypes.MAX_PATH;
			RegQueryValueEx(hKey, "Wallpaper", null, null, (byte*)(wi.filename), &length);

			//壁紙スタイルゲット
			length = 256;
			RegQueryValueEx(hKey, "WallpaperStyle", null, null, (byte*)str, &length);
			style = atoi(str);

			//壁紙スタイルゲット
			length = 256;
			RegQueryValueEx(hKey, "TileWallpaper", null, null, (byte*)str, &length);
			tile = atoi(str);

			//これでいいの？
			if (tile)
				wi.pattern = BG_TILE;
			else {
				switch (style) {
				case 0: // Center(中央に表示)
					wi.pattern = BG_CENTER;
					break;
				case 2: // Stretch(画面に合わせて伸縮) アスペクト比は無視される
					wi.pattern = BG_STRETCH;
					break;
				case 10: // Fill(ページ横幅に合わせる) とあるが、和訳がおかしい
						 // アスペクト比を維持して、はみ出してでも最大表示する
					wi.pattern = BG_AUTOFILL;
					break;
				case 6: // Fit(ページ縦幅に合わせる) とあるが、和訳がおかしい
						// アスペクト比を維持して、はみ出さないように最大表示する
					wi.pattern = BG_AUTOFIT;
					break;
				}
			}

			//レジストリキーのクローズ
			RegCloseKey(hKey);
		}

		void BGPreloadWallpaper(BGSrc src)
		{
			IntPtr hbm;
			WallpaperInfo wi;

			BGGetWallpaperInfo(&wi);

			//壁紙を読み込み
			//LR_CREATEDIBSECTION を指定するのがコツ
			if (wi.pattern == BG_STRETCH)
				hbm = LoadImage(0, wi.filename, IMAGE_BITMAP, CRTWidth, CRTHeight, LR_LOADFROMFILE | LR_CREATEDIBSECTION);
			else
				hbm = LoadImage(0, wi.filename, IMAGE_BITMAP, 0, 0, LR_LOADFROMFILE);

			//壁紙DCを作る
			if (hbm) {
				BITMAP bm;

				GetObject(hbm, bm.Length, &bm);

				src.hdc = CreateBitmapDC(hbm);
				src.width = bm.bmWidth;
				src.height = bm.bmHeight;
				src.pattern = wi.pattern;
			}
			else {
				src.hdc = null;
			}

			src.color = GetSysColor(COLOR_DESKTOP);
		}

		void BGPreloadSrc(BGSrc src)
		{
			DeleteBitmapDC(&(src.hdc));

			switch (src.type) {
			case BG_COLOR:
				break;

			case BG_WALLPAPER:
				BGPreloadWallpaper(src);
				break;

			case BG_PICTURE:
				BGPreloadPicture(src);
				break;
			}
		}

		void BGStretchPicture(Graphics hdcDest, BGSrc src, int x, int y, int width, int height, bool bAntiAlias)
		{
			if (!hdcDest || !src)
				return;

			if (bAntiAlias) {
				if (src.width != width || src.height != height) {
					IntPtr hbm;

					hbm = LoadImage(0, src.file, IMAGE_BITMAP, width, height, LR_LOADFROMFILE);

					if (!hbm)
						return;

					DeleteBitmapDC(&(src.hdc));
					src.hdc = CreateBitmapDC(hbm);
					src.width = width;
					src.height = height;
				}

				BitBlt(hdcDest, x, y, width, height, src.hdc, 0, 0, SRCCOPY);
			}
			else {
				SetStretchBltMode(src.hdc, COLORONCOLOR);
				StretchBlt(hdcDest, x, y, width, height, src.hdc, 0, 0, src.width, src.height, SRCCOPY);
			}
		}

		void BGLoadPicture(Graphics hdcDest, BGSrc src)
		{
			int x, y, width, height, pattern;
			Graphics hdc = null;

			FillBitmapDC(hdcDest, src.color);

			if (!src.height || !src.width)
				return;

			if (src.pattern == BG_AUTOFIT) {
				if ((src.height * ScreenWidth) > (ScreenHeight * src.width))
					pattern = BG_FIT_WIDTH;
				else
					pattern = BG_FIT_HEIGHT;
			}
			else {
				pattern = src.pattern;
			}

			switch (pattern) {
			case BG_STRETCH:
				BGStretchPicture(hdcDest, src, 0, 0, ScreenWidth, ScreenHeight, src.antiAlias);
				break;

			case BG_FIT_WIDTH:

				height = (src.height * ScreenWidth) / src.width;
				y = (ScreenHeight - height) / 2;

				BGStretchPicture(hdcDest, src, 0, y, ScreenWidth, height, src.antiAlias);
				break;

			case BG_FIT_HEIGHT:

				width = (src.width * ScreenHeight) / src.height;
				x = (ScreenWidth - width) / 2;

				BGStretchPicture(hdcDest, src, x, 0, width, ScreenHeight, src.antiAlias);
				break;

			case BG_TILE:
				for (x = 0; x < ScreenWidth; x += src.width)
					for (y = 0; y < ScreenHeight; y += src.height)
						BitBlt(hdcDest, x, y, src.width, src.height, src.hdc, 0, 0, SRCCOPY);
				break;

			case BG_CENTER:
				x = (ScreenWidth - src.width) / 2;
				y = (ScreenHeight - src.height) / 2;

				BitBlt(hdcDest, x, y, src.width, src.height, src.hdc, 0, 0, SRCCOPY);
				break;
			}
		}

		struct LoadWallpaperStruct
		{
			Rectangle? rectClient;
			Graphics hdcDest;
			BGSrc? src;
		}

		bool BGLoadWallpaperEnumFunc(IntPtr hMonitor, Graphics hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
		{
			Rectangle rectDest;
			Rectangle rectRgn;
			int monitorWidth;
			int monitorHeight;
			int destWidth;
			int destHeight;
			HRGN hRgn;
			int x;
			int y;

			LoadWallpaperStruct* lws = (LoadWallpaperStruct*)dwData;

			if (!IntersectRect(&rectDest, lprcMonitor, lws.rectClient))
				return true;

			//モニターにかかってる部分をマスク
			SaveDC(lws.hdcDest);
			CopyRect(&rectRgn, &rectDest);
			OffsetRect(&rectRgn, -lws.rectClient.Left, -lws.rectClient.Top);
			hRgn = CreateRectRgnIndirect(&rectRgn);
			SelectObject(lws.hdcDest, hRgn);

			//モニターの大きさ
			monitorWidth = lprcMonitor.Right - lprcMonitor.Left;
			monitorHeight = lprcMonitor.Bottom - lprcMonitor.Top;

			destWidth = rectDest.Right - rectDest.Left;
			destHeight = rectDest.Bottom - rectDest.Top;

			switch (lws.src.pattern) {
			case BG_CENTER:
			case BG_STRETCH:

				SetWindowOrgEx(lws.src.hdc,
							   lprcMonitor.Left + (monitorWidth - lws.src.width) / 2,
							   lprcMonitor.Top + (monitorHeight - lws.src.height) / 2, null);
				BitBlt(lws.hdcDest, rectDest.Left, rectDest.Top, destWidth, destHeight,
					   lws.src.hdc, rectDest.Left, rectDest.Top, SRCCOPY);

				break;
			case BG_TILE:

				SetWindowOrgEx(lws.src.hdc, 0, 0, null);

				for (x = rectDest.Left - (rectDest.Left % lws.src.width) - lws.src.width;
					x < rectDest.Right; x += lws.src.width)
					for (y = rectDest.Top - (rectDest.Top % lws.src.height) - lws.src.height;
						y < rectDest.Bottom; y += lws.src.height)
						BitBlt(lws.hdcDest, x, y, lws.src.width, lws.src.height, lws.src.hdc, 0, 0, SRCCOPY);
				break;
			}

			//リージョンを破棄
			RestoreDC(lws.hdcDest, -1);
			hRgn.Dispose();

			return true;
		}

		void BGLoadWallpaper(Graphics hdcDest, BGSrc src)
		{
			Rectangle rectClient;
			Point point;
			LoadWallpaperStruct lws;

			//取りあえずデスクトップ色で塗りつぶす
			FillBitmapDC(hdcDest, src.color);

			//壁紙が設定されていない
			if (!src.hdc)
				return;

			//hdcDestの座標系を仮想スクリーンに合わせる
			point = new Point(0, 0);
			ClientToScreen(ttwinman.HVTWin, &point);

			SetWindowOrgEx(hdcDest, point.X, point.Y, null);

			//仮想スクリーンでのクライアント領域
			GetClientRect(ttwinman.HVTWin, &rectClient);
			OffsetRect(&rectClient, point.X, point.Y);

			//モニターを列挙
			lws.rectClient = &rectClient;
			lws.src = src;
			lws.hdcDest = hdcDest;

			if (BGEnumDisplayMonitors) {
				(*BGEnumDisplayMonitors)(null, null, BGLoadWallpaperEnumFunc, (IntPtr) & lws);
			}
			else {
				Rectangle rectMonitor;

				SetRect(&rectMonitor, 0, 0, CRTWidth, CRTHeight);
				BGLoadWallpaperEnumFunc(null, null, &rectMonitor, (IntPtr) & lws);
			}

			//座標系を戻す
			SetWindowOrgEx(hdcDest, 0, 0, null);
		}

		void BGLoadSrc(Graphics hdcDest, BGSrc src)
		{
			switch (src.type) {
			case BG_COLOR:
				FillBitmapDC(hdcDest, src.color);
				break;

			case BG_WALLPAPER:
				BGLoadWallpaper(hdcDest, src);
				break;

			case BG_PICTURE:
				BGLoadPicture(hdcDest, src);
				break;
			}
		}

		void BGSetupPrimary(bool forceSetup)
		{
			Point point;
			Rectangle rect;

			if (!BGEnable)
				return;

			//窓の位置、大きさが変わったかチェック
			point = new Point(0, 0);
			ClientToScreen(ttwinman.HVTWin, &point);

			GetClientRect(ttwinman.HVTWin, &rect);
			OffsetRect(&rect, point.X, point.Y);

			if (!forceSetup && EqualRect(&rect, &BGPrevRect))
				return;

			CopyRect(&BGPrevRect, &rect);

#if DEBUG
			dprintf("BGSetupPrimary : BGInSizeMove = %d", BGInSizeMove);
#endif

			//作業用 DC 作成
			if (hdcBGWork) DeleteBitmapDC(&hdcBGWork);
			if (hdcBGBuffer) DeleteBitmapDC(&hdcBGBuffer);

			hdcBGWork = CreateBitmapDC(CreateScreenCompatibleBitmap(ScreenWidth, FontHeight));
			hdcBGBuffer = CreateBitmapDC(CreateScreenCompatibleBitmap(ScreenWidth, FontHeight));

			//hdcBGBuffer の属性設定
			SetBkMode(hdcBGBuffer, TRANSPARENT);

			if (!BGInSizeMove) {
				BGBLENDFUNCTION bf;
				Graphics hdcSrc = null;

				//背景 Graphics
				if (hdcBG) DeleteBitmapDC(&hdcBG);
				hdcBG = CreateBitmapDC(CreateScreenCompatibleBitmap(ScreenWidth, ScreenHeight));

				//作業用DC
				hdcSrc = CreateBitmapDC(CreateScreenCompatibleBitmap(ScreenWidth, ScreenHeight));

				//背景生成
				BGLoadSrc(hdcBG, &BGDest);

				ZeroMemory(&bf, bf.Length);
				bf.BlendOp = AC_SRC_OVER;

				if (bf.SourceConstantAlpha = BGSrc1.alpha) {
					BGLoadSrc(hdcSrc, &BGSrc1);
					BGAlphaBlend(hdcBG, 0, 0, ScreenWidth, ScreenHeight, hdcSrc, 0, 0, ScreenWidth, ScreenHeight, bf);
				}

				if (bf.SourceConstantAlpha = BGSrc2.alpha) {
					BGLoadSrc(hdcSrc, &BGSrc2);
					BGAlphaBlend(hdcBG, 0, 0, ScreenWidth, ScreenHeight, hdcSrc, 0, 0, ScreenWidth, ScreenHeight, bf);
				}

				DeleteBitmapDC(&hdcSrc);
			}
		}

		Color BGGetColor(string name, Color defcolor, string file)
		{
			uint r, g, b;
			string colorstr, defstr;

			_snprintf_s(defstr, defstr.Length, _TRUNCATE, "%d,%d,%d", defcolor.R, defcolor.G, defcolor.B);

			GetPrivateProfileString(BG_SECTION, name, defstr, colorstr, 255, file);

			r = g = b = 0;

			sscanf(colorstr, "%d , %d , %d", &r, &g, &b);

			return Color.FromArgb(r, g, b);
		}

		BG_PATTERN BGGetStrIndex(string name, BG_PATTERN def, string file, string[] strList, int nList)
		{
			string defstr, str;
			int i;

			def %= nList;

			strncpy_s(defstr, defstr.Length, strList[def], _TRUNCATE);
			GetPrivateProfileString(BG_SECTION, name, defstr, str, 64, file);

			for (i = 0; i < nList; i++)
				if (!_stricmp(str, strList[i]))
					return i;

			return 0;
		}

		bool BGGetOnOff(string name, bool def, string file)
		{
			string[] strList = { "Off", "On" };

			return BGGetStrIndex(name, def, file, strList, 2);
		}

		BG_PATTERN BGGetPattern(string name, BG_PATTERN def, string file)
		{
			string[] strList = { "stretch", "tile", "center", "fitwidth", "fitheight", "autofit" };

			return BGGetStrIndex(name, def, file, strList, 6);
		}

		BG_PATTERN BGGetType(string name, BG_TYPE def, string file)
		{
			string[] strList = { "color", "picture", "wallpaper" };

			return BGGetStrIndex(name, def, file, strList, 3);
		}

		void BGReadTextColorConfig(string file)
		{
			ANSIColor[(int)ColorCodes.IdFore] = BGGetColor("Fore", ANSIColor[(int)ColorCodes.IdFore], file);
			ANSIColor[(int)ColorCodes.IdBack] = BGGetColor("Back", ANSIColor[(int)ColorCodes.IdBack], file);
			ANSIColor[(int)ColorCodes.IdRed] = BGGetColor("Red", ANSIColor[(int)ColorCodes.IdRed], file);
			ANSIColor[(int)ColorCodes.IdGreen] = BGGetColor("Green", ANSIColor[(int)ColorCodes.IdGreen], file);
			ANSIColor[(int)ColorCodes.IdYellow] = BGGetColor("Yellow", ANSIColor[(int)ColorCodes.IdYellow], file);
			ANSIColor[(int)ColorCodes.IdBlue] = BGGetColor("Blue", ANSIColor[(int)ColorCodes.IdBlue], file);
			ANSIColor[(int)ColorCodes.IdMagenta] = BGGetColor("Magenta", ANSIColor[(int)ColorCodes.IdMagenta], file);
			ANSIColor[(int)ColorCodes.IdCyan] = BGGetColor("Cyan", ANSIColor[(int)ColorCodes.IdCyan], file);

			ANSIColor[(int)ColorCodes.IdFore + 8] = BGGetColor("DarkFore", ANSIColor[(int)ColorCodes.IdFore + 8], file);
			ANSIColor[(int)ColorCodes.IdBack + 8] = BGGetColor("DarkBack", ANSIColor[(int)ColorCodes.IdBack + 8], file);
			ANSIColor[(int)ColorCodes.IdRed + 8] = BGGetColor("DarkRed", ANSIColor[(int)ColorCodes.IdRed + 8], file);
			ANSIColor[(int)ColorCodes.IdGreen + 8] = BGGetColor("DarkGreen", ANSIColor[(int)ColorCodes.IdGreen + 8], file);
			ANSIColor[(int)ColorCodes.IdYellow + 8] = BGGetColor("DarkYellow", ANSIColor[(int)ColorCodes.IdYellow + 8], file);
			ANSIColor[(int)ColorCodes.IdBlue + 8] = BGGetColor("DarkBlue", ANSIColor[(int)ColorCodes.IdBlue + 8], file);
			ANSIColor[(int)ColorCodes.IdMagenta + 8] = BGGetColor("DarkMagenta", ANSIColor[(int)ColorCodes.IdMagenta + 8], file);
			ANSIColor[(int)ColorCodes.IdCyan + 8] = BGGetColor("DarkCyan", ANSIColor[(int)ColorCodes.IdCyan + 8], file);

			BGVTColor[0] = BGGetColor("VTFore", BGVTColor[0], file);
			BGVTColor[1] = BGGetColor("VTBack", BGVTColor[1], file);

			BGVTBlinkColor[0] = BGGetColor("VTBlinkFore", BGVTBlinkColor[0], file);
			BGVTBlinkColor[1] = BGGetColor("VTBlinkBack", BGVTBlinkColor[1], file);

			BGVTBoldColor[0] = BGGetColor("VTBoldFore", BGVTBoldColor[0], file);
			BGVTBoldColor[1] = BGGetColor("VTBoldBack", BGVTBoldColor[1], file);

			BGVTReverseColor[0] = BGGetColor("VTReverseFore", BGVTReverseColor[0], file);
			BGVTReverseColor[1] = BGGetColor("VTReverseBack", BGVTReverseColor[1], file);

			/* begin - ishizaki */
			BGURLColor[0] = BGGetColor("URLFore", BGURLColor[0], file);
			BGURLColor[1] = BGGetColor("URLBack", BGURLColor[1], file);
			/* end - ishizaki */
		}

		void BGReadIniFile(string file)
		{
			string path;

			// Easy Setting
			BGDest.pattern = BGGetPattern("BGPicturePattern", BGSrc1.pattern, file);
			BGDest.color = BGGetColor("BGPictureBaseColor", BGSrc1.color, file);

			GetPrivateProfileString(BG_SECTION, "BGPictureFile", BGSrc1.file, path, tttypes.MAX_PATH, file);
			RandomFile(path, BGDest.file, BGDest.file.Length);

			BGSrc1.alpha = 255 - GetPrivateProfileInt(BG_SECTION, "BGPictureTone", 255 - BGSrc1.alpha, file);

			if (!strcmp(BGDest.file, ""))
				BGSrc1.alpha = 255;

			BGSrc2.alpha = 255 - GetPrivateProfileInt(BG_SECTION, "BGFadeTone", 255 - BGSrc2.alpha, file);
			BGSrc2.color = BGGetColor("BGFadeColor", BGSrc2.color, file);

			BGReverseTextAlpha = GetPrivateProfileInt(BG_SECTION, "BGReverseTextTone", BGReverseTextAlpha, file);

			//Src1 の読み出し
			BGSrc1.type = BGGetType("BGSrc1Type", BGSrc1.type, file);
			BGSrc1.pattern = BGGetPattern("BGSrc1Pattern", BGSrc1.pattern, file);
			BGSrc1.antiAlias = BGGetOnOff("BGSrc1AntiAlias", BGSrc1.antiAlias, file);
			BGSrc1.alpha = GetPrivateProfileInt(BG_SECTION, "BGSrc1Alpha", BGSrc1.alpha, file);
			BGSrc1.color = BGGetColor("BGSrc1Color", BGSrc1.color, file);

			GetPrivateProfileString(BG_SECTION, "BGSrc1File", BGSrc1.file, path, tttypes.MAX_PATH, file);
			RandomFile(path, BGSrc1.file, BGSrc1.file.Length);

			//Src2 の読み出し
			BGSrc2.type = BGGetType("BGSrc2Type", BGSrc2.type, file);
			BGSrc2.pattern = BGGetPattern("BGSrc2Pattern", BGSrc2.pattern, file);
			BGSrc2.antiAlias = BGGetOnOff("BGSrc2AntiAlias", BGSrc2.antiAlias, file);
			BGSrc2.alpha = GetPrivateProfileInt(BG_SECTION, "BGSrc2Alpha", BGSrc2.alpha, file);
			BGSrc2.color = BGGetColor("BGSrc2Color", BGSrc2.color, file);

			GetPrivateProfileString(BG_SECTION, "BGSrc2File", BGSrc2.file, path, tttypes.MAX_PATH, file);
			RandomFile(path, BGSrc2.file, BGSrc2.file.Length);

			//Dest の読み出し
			BGDest.type = BGGetType("BGDestType", BGDest.type, file);
			BGDest.pattern = BGGetPattern("BGDestPattern", BGDest.pattern, file);
			BGDest.antiAlias = BGGetOnOff("BGDestAntiAlias", BGDest.antiAlias, file);
			BGDest.color = BGGetColor("BGDestColor", BGDest.color, file);

			GetPrivateProfileString(BG_SECTION, "BGDestFile", BGDest.file, path, tttypes.MAX_PATH, file);
			RandomFile(path, BGDest.file, BGDest.file.Length);

			//その他読み出し
			BGReverseTextAlpha = GetPrivateProfileInt(BG_SECTION, "BGReverseTextAlpha", BGReverseTextAlpha, file);
			BGReadTextColorConfig(file);
		}

		void BGDestruct()
		{
			if (!BGEnable)
				return;

			DeleteBitmapDC(&hdcBGBuffer);
			DeleteBitmapDC(&hdcBGWork);
			DeleteBitmapDC(&hdcBG);
			DeleteBitmapDC(&(BGDest.hdc));
			DeleteBitmapDC(&(BGSrc1.hdc));
			DeleteBitmapDC(&(BGSrc2.hdc));

			//テンポラリーファイル削除
			DeleteFile(BGDest.fileTmp);
			DeleteFile(BGSrc1.fileTmp);
			DeleteFile(BGSrc2.fileTmp);
		}

		void BGInitialize()
		{
			string path, config_file, tempPath;
			string msimg32_dll, user32_dll;

			// VTColor を読み込み
			BGVTColor[0] = ts.VTColor[0];
			BGVTColor[1] = ts.VTColor[1];

			BGVTBoldColor[0] = ts.VTBoldColor[0];
			BGVTBoldColor[1] = ts.VTBoldColor[1];

			BGVTBlinkColor[0] = ts.VTBlinkColor[0];
			BGVTBlinkColor[1] = ts.VTBlinkColor[1];

			BGVTReverseColor[0] = ts.VTReverseColor[0];
			BGVTReverseColor[1] = ts.VTReverseColor[1];

#if true
			// ハイパーリンク描画の復活。(2009.8.26 yutaka)
			/* begin - ishizaki */
			BGURLColor[0] = ts.URLColor[0];
			BGURLColor[1] = ts.URLColor[1];
			/* end - ishizaki */
#else
			// TODO: ハイパーリンクの描画がリアルタイムに行われないことがあるので、
			// 色属性変更はいったん取りやめることにする。将来、対応する。(2005.4.3 yutaka)
			BGURLColor[0] = ts.VTColor[0];
			BGURLColor[1] = ts.VTColor[1];
#endif

			// ANSI color設定のほうを優先させる (2005.2.3 yutaka)
			InitColorTable();

			//リソース解放
			BGDestruct();

			//BG が有効かチェック
			// 空の場合のみ、ディスクから読む。BGInitialize()が Tera Term 起動時以外にも、
			// Additional settings から呼び出されることがあるため。
			if (ts.EtermLookfeel.BGThemeFile[0] == '\0') {
				ts.EtermLookfeel.BGEnable = BGEnable = BGGetOnOff("BGEnable", false, ts.SetupFName);
			}
			else {
				BGEnable = BGGetOnOff("BGEnable", false, ts.SetupFName);
			}

			ts.EtermLookfeel.BGUseAlphaBlendAPI = BGGetOnOff("BGUseAlphaBlendAPI", true, ts.SetupFName);
			ts.EtermLookfeel.BGNoFrame = BGGetOnOff("BGNoFrame", false, ts.SetupFName);
			ts.EtermLookfeel.BGFastSizeMove = BGGetOnOff("BGFastSizeMove", true, ts.SetupFName);
			ts.EtermLookfeel.BGNoCopyBits = BGGetOnOff("BGFlickerlessMove", true, ts.SetupFName);

			GetPrivateProfileString(BG_SECTION, "BGSPIPath", "plugin", BGSPIPath, tttypes.MAX_PATH, ts.SetupFName);
			strncpy_s(ts.EtermLookfeel.BGSPIPath, ts.EtermLookfeel.BGSPIPath.Length, BGSPIPath, _TRUNCATE);

			if (ts.EtermLookfeel.BGThemeFile[0] == '\0') {
				//コンフィグファイルの決定
				GetPrivateProfileString(BG_SECTION, "BGThemeFile", "", path, tttypes.MAX_PATH, ts.SetupFName);
				strncpy_s(ts.EtermLookfeel.BGThemeFile, ts.EtermLookfeel.BGThemeFile.Length, path, _TRUNCATE);

				// 背景画像の読み込み
				_snprintf_s(path, path.Length, _TRUNCATE, "%s\\%s", ts.HomeDir, BG_THEME_IMAGEFILE);
				GetPrivateProfileString(BG_SECTION, BG_DESTFILE, "", ts.BGImageFilePath, ts.BGImageFilePath.Length, path);

				// 背景画像の明るさの読み込み。
				// BGSrc1Alpha と BGSrc2Alphaは同値として扱う。
				ts.BGImgBrightness = GetPrivateProfileInt(BG_SECTION, BG_THEME_IMAGE_BRIGHTNESS1, BG_THEME_IMAGE_BRIGHTNESS_DEFAULT, path);
			}

			if (!BGEnable)
				return;

			//乱数初期化
			// add cast (2006.2.18 yutaka)
			srand((uint)time(null));

			//BGシステム設定読み出し
			BGUseAlphaBlendAPI = ts.EtermLookfeel.BGUseAlphaBlendAPI;
			BGNoFrame = ts.EtermLookfeel.BGNoFrame;
			BGFastSizeMove = ts.EtermLookfeel.BGFastSizeMove;
			BGNoCopyBits = ts.EtermLookfeel.BGNoCopyBits;

#if false
  GetPrivateProfileString(BG_SECTION,"BGSPIPath","plugin",BGSPIPath,tttypes.MAX_PATH,ts.SetupFName);
  strncpy_s(ts.EtermLookfeel.BGSPIPath, ts.EtermLookfeel.BGSPIPath.Length, BGSPIPath, _TRUNCATE);
#endif

			//テンポラリーファイル名を生成
			GetTempPath(tttypes.MAX_PATH, tempPath);
			GetTempFileName(tempPath, "ttAK", 0, BGDest.fileTmp);
			GetTempFileName(tempPath, "ttAK", 0, BGSrc1.fileTmp);
			GetTempFileName(tempPath, "ttAK", 0, BGSrc2.fileTmp);

			//デフォルト値
			BGDest.type = BG_PICTURE;
			BGDest.pattern = BG_STRETCH;
			BGDest.color = Color.FromArgb(0, 0, 0);
			BGDest.antiAlias = true;
			strncpy_s(BGDest.file, BGDest.file.Length, "", _TRUNCATE);

			BGSrc1.type = BG_WALLPAPER;
			BGSrc1.pattern = BG_STRETCH;
			BGSrc1.color = Color.FromArgb(255, 255, 255);
			BGSrc1.antiAlias = true;
			BGSrc1.alpha = 255;
			strncpy_s(BGSrc1.file, BGSrc1.file.Length, "", _TRUNCATE);

			BGSrc2.type = BG_COLOR;
			BGSrc2.pattern = BG_STRETCH;
			BGSrc2.color = Color.FromArgb(0, 0, 0);
			BGSrc2.antiAlias = true;
			BGSrc2.alpha = 128;
			strncpy_s(BGSrc2.file, BGSrc2.file.Length, "", _TRUNCATE);

			BGReverseTextAlpha = 255;

			//設定の読み出し
			BGReadIniFile(ts.SetupFName);

			//コンフィグファイルの決定
			GetPrivateProfileString(BG_SECTION, "BGThemeFile", "", path, tttypes.MAX_PATH, ts.SetupFName);
			RandomFile(path, config_file, config_file.Length);

			//設定のオーバーライド
			if (strcmp(config_file, "")) {
				string dir, prevDir;

				//INIファイルのあるディレクトリに一時的に移動
				GetCurrentDirectory(tttypes.MAX_PATH, prevDir);

				ExtractDirName(config_file, dir);
				SetCurrentDirectory(dir);

				BGReadIniFile(config_file);

				SetCurrentDirectory(prevDir);
			}

			//SPI のパスを整形
			AppendSlash(BGSPIPath, BGSPIPath.Length);
			strncat_s(BGSPIPath, BGSPIPath.Length, "*", _TRUNCATE);

			//壁紙 or 背景をプリロード
			BGPreloadSrc(&BGDest);
			BGPreloadSrc(&BGSrc1);
			BGPreloadSrc(&BGSrc2);

			// AlphaBlend のアドレスを読み込み
			if (BGUseAlphaBlendAPI) {
				GetSystemDirectory(msimg32_dll, msimg32_dll.Length);
				strncat_s(msimg32_dll, msimg32_dll.Length, "\\msimg32.dll", _TRUNCATE);
				(FARPROC)BGAlphaBlend = GetProcAddressWithDllName(msimg32_dll, "AlphaBlend");
			}
			else {
				BGAlphaBlend = null;
			}

			if (!BGAlphaBlend)
				BGAlphaBlend = AlphaBlendWithoutAPI;

			//EnumDisplayMonitors を探す
			GetSystemDirectory(user32_dll, user32_dll.Length);
			strncat_s(user32_dll, user32_dll.Length, "\\user32.dll", _TRUNCATE);
			(FARPROC)BGEnumDisplayMonitors = GetProcAddressWithDllName(user32_dll, "EnumDisplayMonitors");
		}

		void BGExchangeColor()
		{
			Color ColorRef;
			if (ts.ColorFlag & AnsiAttributeColorFlags.CF_REVERSECOLOR) {
				ColorRef = BGVTColor[0];
				BGVTColor[0] = BGVTReverseColor[0];
				BGVTReverseColor[0] = ColorRef;
				ColorRef = BGVTColor[1];
				BGVTColor[1] = BGVTReverseColor[1];
				BGVTReverseColor[1] = ColorRef;
			}
			else {
				ColorRef = BGVTColor[0];
				BGVTColor[0] = BGVTColor[1];
				BGVTColor[1] = ColorRef;
			}

			ColorRef = BGVTBoldColor[0];
			BGVTBoldColor[0] = BGVTBoldColor[1];
			BGVTBoldColor[1] = ColorRef;

			ColorRef = BGVTBlinkColor[0];
			BGVTBlinkColor[0] = BGVTBlinkColor[1];
			BGVTBlinkColor[1] = ColorRef;

			ColorRef = BGURLColor[0];
			BGURLColor[0] = BGURLColor[1];
			BGURLColor[1] = ColorRef;

			//    BGReverseText = !BGReverseText;
		}

		void BGFillRect(Graphics hdc, Rectangle R, Brush brush)
		{
			if (!BGEnable)
				FillRect(hdc, R, brush);
			else
				BitBlt(VTDC, R.Left, R.Top, R.Right - R.Left, R.Bottom - R.Top, hdcBG, R.Left, R.Top, SRCCOPY);
		}

		void BGScrollWindow(IntPtr hWnd, int xa, int ya, Rectangle Rect, Rectangle ClipRect)
		{
			if (ts.MaximizedBugTweak) {
				// Eterm lookfeelが有効、もしくは最大化ウィンドウの場合はスクロールは使わない。
				// これにより、最大化ウィンドウで文字欠けとなる現象が改善される。(2008.2.1 doda, yutaka)
				if (BGEnable || IsZoomed(hWnd))
					InvalidateRect(ttwinman.HVTWin, ClipRect, false);
				else
					ScrollWindow(hWnd, xa, ya, Rect, ClipRect);
			}
			else {
				if (!BGEnable)
					ScrollWindow(hWnd, xa, ya, Rect, ClipRect);
				else
					InvalidateRect(ttwinman.HVTWin, ClipRect, false);
			}
		}

		void BGOnEnterSizeMove()
		{
			int r, g, b;

			if (!BGEnable || !BGFastSizeMove)
				return;

			BGInSizeMove = true;

			//背景色生成
			r = BGDest.color.R;
			g = BGDest.color.G;
			b = BGDest.color.B;

			r = (r * (255 - BGSrc1.alpha) + BGSrc1.color.R * BGSrc1.alpha) >> 8;
			g = (g * (255 - BGSrc1.alpha) + BGSrc1.color.G * BGSrc1.alpha) >> 8;
			b = (b * (255 - BGSrc1.alpha) + BGSrc1.color.B * BGSrc1.alpha) >> 8;

			r = (r * (255 - BGSrc2.alpha) + BGSrc2.color.R * BGSrc2.alpha) >> 8;
			g = (g * (255 - BGSrc2.alpha) + BGSrc2.color.G * BGSrc2.alpha) >> 8;
			b = (b * (255 - BGSrc2.alpha) + BGSrc2.color.B * BGSrc2.alpha) >> 8;

			BGBrushInSizeMove = CreateSolidBrush(Color.FromArgb(r, g, b));
		}

		void BGOnExitSizeMove()
		{
			if (!BGEnable || !BGFastSizeMove)
				return;

			BGInSizeMove = false;

			BGSetupPrimary(true);
			InvalidateRect(ttwinman.HVTWin, null, false);

			//ブラシを削除
			if (BGBrushInSizeMove) {
				BGBrushInSizeMove.Dispose();
				BGBrushInSizeMove = null;
			}
		}

		void BGOnSettingChange()
		{
			if (!BGEnable)
				return;

			CRTWidth = GetSystemMetrics(SM_CXSCREEN);
			CRTHeight = GetSystemMetrics(SM_CYSCREEN);

			//壁紙 or 背景をプリロード
			BGPreloadSrc(&BGDest);
			BGPreloadSrc(&BGSrc1);
			BGPreloadSrc(&BGSrc2);

			BGSetupPrimary(true);
			InvalidateRect(ttwinman.HVTWin, null, false);
		}

		//-->
#endif  // ALPHABLEND_TYPE2

		void DispApplyANSIColor()
		{
			int i;

			for (i = (int)ColorCodes.IdBack; i <= (int)ColorCodes.IdFore + 8; i++)
				ANSIColor[i] = ts.ANSIColor[i];

			if ((ts.ColorFlag & ColorFlags.CF_USETEXTCOLOR) != 0) {
#if ALPHABLEND_TYPE2
				ANSIColor[(int)ColorCodes.IdBack] = BGVTColor[1]; // use background color for "Black"
				ANSIColor[(int)ColorCodes.IdFore] = BGVTColor[0]; // use text color for "white"
#else
				ANSIColor[(int)ColorCodes.IdBack] = ts.VTColor[1]; // use background color for "Black"
				ANSIColor[(int)ColorCodes.IdFore] = ts.VTColor[0]; // use text color for "white"
#endif
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

#if ALPHABLEND_TYPE2
			CRTWidth = GetSystemMetrics(SM_CXSCREEN);
			CRTHeight = GetSystemMetrics(SM_CYSCREEN);

			BGInitialize();
#else
			InitColorTable();
#endif  // ALPHABLEND_TYPE2

			DispSetNearestColors((int)ColorCodes.IdBack, 255, TmpDC);

			/* background paintbrush */
			Background = CreateSolidBrush(ts.VTColor[1]);
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

		void EndDisp()
		{
			int i, j;

			if (VTDC != IntPtr.Zero) DispReleaseDC();

			/* Delete fonts */
			VTFont.Dispose();

			if (Background != IntPtr.Zero) {
				DeleteObject(Background);
				Background = IntPtr.Zero;
			}

#if ALPHABLEND_TYPE2
			//<!--by AKASI
			BGDestruct();
			//-->
#endif  // ALPHABLEND_TYPE2

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
			int i, j;
			TEXTMETRIC Metrics;
			Graphics TmpDC;

			/* Delete Old Fonts */
			if (VTFont != null)
				VTFont.Dispose();

			/* Normal Font */
			SetLogFont();

			/* set IME font */
			ttime.SetConversionLogFont(VTFont);

			TmpDC = Graphics.FromHwnd(ttwinman.HVTWin.Handle);
			IntPtr hdc = TmpDC.GetHdc();
			try {
				SelectObject(hdc, VTFont.ToHfont());
				GetTextMetrics(hdc, out Metrics);

				FontWidth = Metrics.tmAveCharWidth + ts.FontDW;
				FontHeight = Metrics.tmHeight + ts.FontDH;
			}
			finally {
				TmpDC.ReleaseHdc();
				TmpDC.Dispose();
			}

			for (i = 0; i < tttypes.TermWidthMax; i++)
				Dx[i] = FontWidth;
		}

		public void ResetIME()
		{
			/* reset language for communication */
			cv.Language = ts.Language;

			/* reset IME */
			if ((ts.Language == Language.IdJapanese) || (ts.Language == Language.IdKorean) || (ts.Language == Language.IdUtf8)) //HKS
			{
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
			}
			else
				ttime.FreeIME();

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
				VTWindow.SetTimer(ttwinman.HVTWin.Handle, (int)TimerId.IdCaretTimer, T, IntPtr.Zero);
			}
			UpdateCaretPosition(true);
		}

		// WM_KILLFOCUSされたときのカーソルを自分で描く
		public void CaretKillFocus(bool show)
		{
			int CaretX, CaretY;
			Point[] p = new Point[5];
			IntPtr oldpen;

			if (!ts.KillFocusCursor)
				return;

			// Eterm lookfeelの場合は何もしない
#if ALPHABLEND_TYPE2
			if (BGEnable)
				return;
#endif   // ALPHABLEND_TYPE2

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
				oldpen = SelectObject(VTDC, CreatePen(PS_SOLID, 0, ts.VTColor[0]));
			}
			else {
				oldpen = SelectObject(VTDC, CreatePen(PS_SOLID, 0, ts.VTColor[1]));
			}
			Polyline(VTDC, p, 5);
			oldpen = SelectObject(VTDC, oldpen);
			DeleteObject(oldpen);

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

			// Eterm lookfeelの場合は何もしない
#if ALPHABLEND_TYPE2
			if (BGEnable)
				return;
#endif   // ALPHABLEND_TYPE2

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

			if ((ts.Language == Language.IdJapanese || ts.Language == Language.IdKorean || ts.Language == Language.IdUtf8) &&
				ttime.CanUseIME() && ts.IMEInline) {
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
				VTWindow.KillTimer(ttwinman.HVTWin.Handle, (int)TimerId.IdCaretTimer);
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

		public void PaintWindow(IntPtr PaintDC, Rectangle PaintRect, bool fBkGnd,
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
			if (VTDC != IntPtr.Zero)
				DispReleaseDC();
			VTDC = PaintDC;
			DCPrevFont = SelectObject(VTDC, VTFont.ToHfont());
			DispInitDC();

#if ALPHABLEND_TYPE2
			//<!--by AKASI
			//if (fBkGnd)
			if (!BGEnable && fBkGnd)
				//-->
#else
			if (fBkGnd)
#endif  // ALPHABLEND_TYPE2

				FillRect(VTDC, PaintRect, Background);

			Xs = PaintRect.Left / FontWidth + WinOrgX;
			Ys = PaintRect.Top / FontHeight + WinOrgY;
			Xe = (PaintRect.Right - 1) / FontWidth + WinOrgX;
			Ye = (PaintRect.Bottom - 1) / FontHeight + WinOrgY;
		}

		public void DispEndPaint()
		{
			if (VTDC == IntPtr.Zero) return;
			SelectObject(VTDC, DCPrevFont);
			VTDC = IntPtr.Zero;
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
					SetScrollRange(ttwinman.HVTWin.Handle, Orientation.Vertical, 0, 1, false);
				}
				else
					SetScrollRange(ttwinman.HVTWin.Handle, Orientation.Vertical, 0, NumOfLines - WinHeight, false);

				SetScrollPos(ttwinman.HVTWin.Handle, Orientation.Horizontal, 0, true);
				SetScrollPos(ttwinman.HVTWin.Handle, Orientation.Vertical, 0, true);
			}
			if (IsCaretOn()) CaretOn();
		}

		public void DispChangeBackground()
		{
			DispReleaseDC();
			if (Background != IntPtr.Zero) DeleteObject(Background);

			if ((CurCharAttr.Attr2 & AttributeBitMasks.Attr2Back) != 0) {
				if (((int)CurCharAttr.Back < 16) && ((int)CurCharAttr.Back & 7) != 0)
					Background = CreateSolidBrush(ANSIColor[(int)CurCharAttr.Back ^ 8]);
				else
					Background = CreateSolidBrush(ANSIColor[(int)CurCharAttr.Back]);
			}
			else {
#if ALPHABLEND_TYPE2
				Background = CreateSolidBrush(BGVTColor[1]);
#else
				Background = CreateSolidBrush(ts.VTColor[1]);
#endif  // ALPHABLEND_TYPE2
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

#if ALPHABLEND_TYPE2
				ANSIColor[(int)ColorCodes.IdFore] = BGVTColor[0];
				ANSIColor[(int)ColorCodes.IdBack] = BGVTColor[1];
#endif  // ALPHABLEND_TYPE2

			}

			/* change background color */
			DispChangeBackground();
		}

		const int OPAQUE = 2;

		public void DispInitDC()
		{
			if (VTDC == IntPtr.Zero) {
				VTDC = Graphics.FromHwnd(ttwinman.HVTWin.Handle).GetHdc();
				if (VTFont != null)
					DCPrevFont = SelectObject(VTDC, VTFont.ToHfont());
			}
			else
				SelectObject(VTDC, VTFont.ToHfont());
#if ALPHABLEND_TYPE2
			SetTextColor(VTDC, BGVTColor[0]);
			SetBkColor(VTDC, BGVTColor[1]);
#else
			SetTextColor(VTDC, ts.VTColor[0]);
			SetBkColor(VTDC, ts.VTColor[1]);
#endif  // ALPHABLEND_TYPE2

			SetBkMode(VTDC, OPAQUE);
			DCAttr = DefCharAttr;
			DCReverse = false;

#if ALPHABLEND_TYPE2
			//<!--by AKASI
			BGReverseText = false;
			//-->
#endif  // ALPHABLEND_TYPE2
		}

		public void DispReleaseDC()
		{
			if (VTDC == IntPtr.Zero) return;
			if (DCPrevFont != IntPtr.Zero)
				SelectObject(VTDC, DCPrevFont);
			DCPrevFont = IntPtr.Zero;
			VTDC = IntPtr.Zero;
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

			if (VTDC == IntPtr.Zero) DispInitDC();

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

			SelectObject(VTDC, VTFont.ToHfont());

			if ((ts.ColorFlag & ColorFlags.CF_FULLCOLOR) == 0) {
				if (isBlinkColored(Attr)) {
#if ALPHABLEND_TYPE2 // AKASI
					TextColor = BGVTBlinkColor[0];
					BackColor = BGVTBlinkColor[1];
#else
					TextColor = ts.VTBlinkColor[0];
					BackColor = ts.VTBlinkColor[1];
#endif
				}
				else if (isBoldColored(Attr)) {
#if ALPHABLEND_TYPE2 // AKASI
					TextColor = BGVTBoldColor[0];
					BackColor = BGVTBoldColor[1];
#else
					TextColor = ts.VTBoldColor[0];
					BackColor = ts.VTBoldColor[1];
#endif
				}
				/* begin - ishizaki */
				else if (isURLColored(Attr)) {
#if ALPHABLEND_TYPE2 // AKASI
					TextColor = BGURLColor[0];
					BackColor = BGURLColor[1];
#else
					TextColor = ts.URLColor[0];
					BackColor = ts.URLColor[1];
#endif
				}
				/* end - ishizaki */
				else {
					if (isForeColored(Attr)) {
						TextColor = ANSIColor[(int)Attr.Fore];
					}
					else {
#if ALPHABLEND_TYPE2 // AKASI
						TextColor = BGVTColor[0];
#else
						TextColor = ts.VTColor[0];
#endif
						NoReverseColor = 1;
					}

					if (isBackColored(Attr)) {
						BackColor = ANSIColor[(int)Attr.Back];
					}
					else {
#if ALPHABLEND_TYPE2 // AKASI
						BackColor = BGVTColor[1];
#else
						BackColor = ts.VTColor[1];
#endif
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
#if ALPHABLEND_TYPE2 // AKASI
					TextColor = BGVTBlinkColor[0];
				else if (isBoldColored(Attr))
					TextColor = BGVTBoldColor[0];
				else if (isURLColored(Attr))
					TextColor = BGURLColor[0];
				else {
					TextColor = BGVTColor[0];
#else
					TextColor = ts.VTBlinkColor[0];
				else if (isBoldColored(Attr))
					TextColor = ts.VTBoldColor[0];
				else if (isURLColored(Attr))
					TextColor = ts.URLColor[0];
				else {
					TextColor = ts.VTColor[0];
#endif
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
#if ALPHABLEND_TYPE2 // AKASI
					BackColor = BGVTBlinkColor[1];
				else if (isBoldColored(Attr))
					BackColor = BGVTBoldColor[1];
				else if (isURLColored(Attr))
					BackColor = BGURLColor[1];
				else {
					BackColor = BGVTColor[1];
#else
					BackColor = ts.VTBlinkColor[1];
				else if (isBoldColored(Attr))
					BackColor = ts.VTBoldColor[1];
				else if (isURLColored(Attr))
					BackColor = ts.URLColor[1];
				else {
					BackColor = ts.VTColor[1];
#endif
					if (NoReverseColor == 1) {
						NoReverseColor = (ts.ColorFlag & ColorFlags.CF_REVERSECOLOR) == 0 ? 1 : 0;
					}
				}
			}
#if USE_NORMAL_BGCOLOR_REJECT
			if (ts.UseNormalBGColor) {
#if ALPHABLEND_TYPE2
				BackColor = BGVTColor[1];
#else
				BackColor = ts.VTColor[1];
#endif
			}
#endif
			if (Reverse != ((Attr.Attr & AttributeBitMasks.AttrReverse) != 0)) {
#if ALPHABLEND_TYPE2
				BGReverseText = true;
#endif
				if ((Attr.Attr & AttributeBitMasks.AttrReverse) != 0 && (NoReverseColor == 0)) {
#if ALPHABLEND_TYPE2
					SetTextColor(VTDC, BGVTReverseColor[0]);
					SetBkColor(VTDC, BGVTReverseColor[1]);
#else
					SetTextColor(VTDC, ts.VTReverseColor[0]);
					SetBkColor(VTDC, ts.VTReverseColor[1]);
#endif
				}
				else {
					SetTextColor(VTDC, BackColor);
					SetBkColor(VTDC, TextColor);
				}
			}
			else {
#if ALPHABLEND_TYPE2 // by AKASI
				BGReverseText = false;
#endif
				SetTextColor(VTDC, TextColor);
				SetBkColor(VTDC, BackColor);
			}
		}

#if true
		// 当面はこちらの関数を使う。(2004.11.4 yutaka)
		public void DispStr(byte[] Buff, int Offset, int Count, int Y, ref int X)
		// Display a string
		//   Buff: points the string
		//   Y: vertical position in window cordinate
		//  *X: horizontal position
		// Return:
		//  *X: horizontal position shifted by the width of the string
		{
			Rectangle RText;

			if ((ts.Language == Language.IdRussian) &&
				(ts.RussClient != ts.RussFont))
				language.RussConvStr(ts.RussClient, ts.RussFont, Buff, Offset, Count);

			RText = new Rectangle(X, Y, Count * FontWidth, FontHeight);

#if ALPHABLEND_TYPE2
			//<!--by AKASI
			if (!BGEnable) {
				ExtTextOut(VTDC, X + ts.FontDX, Y + ts.FontDY,
					ETO_CLIPPED | ETO_OPAQUE,
					&RText, Buff, Count, &Dx[0]);
			}
			else {

				int width;
				int height;
				int eto_options = ETO_CLIPPED;
				Rectangle rect;
				Font hPrevFont;

				width = Count * FontWidth;
				height = FontHeight;
				SetRect(&rect, 0, 0, width, height);

				//hdcBGBuffer の属性を設定
				hPrevFont = SelectObject(hdcBGBuffer, GetCurrentObject(VTDC, OBJ_FONT));
				SetTextColor(hdcBGBuffer, GetTextColor(VTDC));
				SetBkColor(hdcBGBuffer, GetBkColor(VTDC));

				//窓の移動、リサイズ中は背景を BGBrushInSizeMove で塗りつぶす
				if (BGInSizeMove)
					FillRect(hdcBGBuffer, &rect, BGBrushInSizeMove);

				BitBlt(hdcBGBuffer, 0, 0, width, height, hdcBG, X, Y, SRCCOPY);

				if (BGReverseText == true) {
					if (BGReverseTextAlpha < 255) {
						BGBLENDFUNCTION bf;
						Brush hbr;

						hbr = CreateSolidBrush(GetBkColor(hdcBGBuffer));
						FillRect(hdcBGWork, &rect, hbr);
						hbr.Dispose();

						ZeroMemory(&bf, bf.Length);
						bf.BlendOp = AC_SRC_OVER;
						bf.SourceConstantAlpha = BGReverseTextAlpha;

						BGAlphaBlend(hdcBGBuffer, 0, 0, width, height, hdcBGWork, 0, 0, width, height, bf);
					}
					else {
						eto_options |= ETO_OPAQUE;
					}
				}

				ExtTextOut(hdcBGBuffer, ts.FontDX, ts.FontDY, eto_options, &rect, Buff, Count, &Dx[0]);
				BitBlt(VTDC, X, Y, width, height, hdcBGBuffer, 0, 0, SRCCOPY);

				SelectObject(hdcBGBuffer, hPrevFont);
			}
			//-->
#else
			ExtTextOut(VTDC, X + ts.FontDX, Y + ts.FontDY, ETO_CLIPPED | ETO_OPAQUE, RText, Buff, Offset, Count, Dx);
#endif
			X = RText.Right;

			if ((ts.Language == Language.IdRussian) &&
				(ts.RussClient != ts.RussFont))
				language.RussConvStr(ts.RussFont, ts.RussClient, Buff, 0, Count);
		}

#else
		void DispStr(string Buff, int Count, int Y, ref int X)
		// Display a string
		//   Buff: points the string
		//   Y: vertical position in window cordinate
		//   X: horizontal position
		// Return:
		//   X: horizontal position shifted by the width of the string
		{
			Rectangle RText;
			wchar_t *wc;
			int len, wclen;
			CHAR ch;

#if false
		//#include <crtdbg.h>
			_CrtSetBreakAlloc(52);
			Buff[0] = 0x82;
			Buff[1] = 0xe4;
			Buff[2] = 0x82;
			Buff[3] = 0xbd;
			Buff[4] = 0x82;
			Buff[5] = 0xa9;
			Count = 6;
#endif

			setlocale(LC_ALL, ts.Locale);

			ch = Buff[Count];
			Buff[Count] = 0;
			len = mbstowcs(null, Buff, 0);

			wc = malloc(wchar_t.Length * (len + 1));
			if (wc == null)
				return;
			wclen = mbstowcs(wc, Buff, len + 1);
			Buff[Count] = ch;

			if ((ts.Language==Language.IdRussian) &&
				(ts.RussClient!=ts.RussFont))
				RussConvStr(ts.RussClient,ts.RussFont,Buff,Count);

			RText = new Rectangle(Y, Y + FontHeight, RText - Y - FontHeight, RText - Y); // 

			// Unicodeで出力する。
#if true
			// UTF-8環境において、tcshがEUC出力した場合、画面に何も表示されないことがある。
			// マウスでドラッグしたり、ログファイルへ保存してみると、文字化けした文字列を
			// 確認することができる。(2004.8.6 yutaka)
			ExtTextOutW(VTDC,X+ts.FontDX,Y+ts.FontDY,
				ETO_CLIPPED | ETO_OPAQUE,
				&RText, wc, wclen, null);
		//		&RText, wc, wclen, &Dx[0]);
#else
			TextOutW(VTDC, X+ts.FontDX, Y+ts.FontDY, wc, wclen);

#endif

			X = RText.Right;

			if ((ts.Language==Language.IdRussian) &&
				(ts.RussClient!=ts.RussFont))
				RussConvStr(ts.RussFont,ts.RussClient,Buff,Count);

			free(wc);
		}
#endif

		public void DispEraseCurToEnd(int YEnd)
		{
			Rectangle R;

			if (VTDC == IntPtr.Zero) DispInitDC();
			R = new Rectangle(0, (CursorY + 1 - WinOrgY) * FontHeight, ScreenWidth, (YEnd - CursorY) * FontHeight);

#if ALPHABLEND_TYPE2
			//<!--by AKASI
			//  FillRect(VTDC,&R,Background);
			BGFillRect(VTDC, &R, Background);
			//-->
#else
			FillRect(VTDC, R, Background);
#endif

			R.X = (CursorX - WinOrgX) * FontWidth;
			R.Offset(0, -FontHeight);

#if ALPHABLEND_TYPE2
			//<!--by AKASI
			//FillRect(VTDC,&R,Background);
			BGFillRect(VTDC, &R, Background);
			//-->
#else
			FillRect(VTDC, R, Background);
#endif
		}

		public void DispEraseHomeToCur(int YHome)
		{
			Rectangle R;

			if (VTDC == IntPtr.Zero) DispInitDC();
			R = new Rectangle(0, (YHome - WinOrgY) * FontHeight, ScreenWidth, (CursorY - YHome) * FontHeight);

#if ALPHABLEND_TYPE2
			//<!--by AKASI
			//FillRect(VTDC,&R,Background);
			BGFillRect(VTDC, &R, Background);
			//-->
#else
			FillRect(VTDC, R, Background);
#endif
			R = new Rectangle(R.Left, R.Bottom, (CursorX + 1 - WinOrgX) * FontWidth, FontHeight);

#if ALPHABLEND_TYPE2
			//<!--by AKASI
			//FillRect(VTDC,&R,Background);
			BGFillRect(VTDC, &R, Background);
			//-->
#else
			FillRect(VTDC, R, Background);
#endif
		}

		public void DispEraseCharsInLine(int XStart, int Count)
		{
			Rectangle R;

			if (VTDC == IntPtr.Zero) DispInitDC();

			R = new Rectangle((XStart - WinOrgX) * FontWidth, (CursorY - WinOrgY) * FontHeight, Count * FontWidth, FontHeight);

#if ALPHABLEND_TYPE2
			//<!--by AKASI
			//FillRect(VTDC,&R,Background);
			BGFillRect(VTDC, &R, Background);
			//-->
#else
			FillRect(VTDC, R, Background);
#endif
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
#if ALPHABLEND_TYPE2
				//<!--by AKASI
				//ScrollWindow(ttwinman.HVTWin,0,-FontHeight*Count,&R,&R);
				BGScrollWindow(ttwinman.HVTWin, 0, -FontHeight * Count, &R, &R);
				//-->
#else
				ScrollWindow(ttwinman.HVTWin, 0, -FontHeight * Count, R, R);
#endif
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
#if ALPHABLEND_TYPE2
				//<!--by AKASI
				//ScrollWindow(ttwinman.HVTWin,0,FontHeight*Count,&R,&R);
				BGScrollWindow(ttwinman.HVTWin, 0, FontHeight * Count, &R, &R);
				//-->
#else
				ScrollWindow(ttwinman.HVTWin, 0, FontHeight * Count, R, R);
#endif
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

			ScrollPosX = GetScrollPos(ttwinman.HVTWin.Handle, Orientation.Horizontal);
			ScrollPosY = GetScrollPos(ttwinman.HVTWin.Handle, Orientation.Vertical);
			if (ScrollPosX > XRange)
				ScrollPosX = XRange;
			if (ScrollPosY > YRange)
				ScrollPosY = YRange;

			WinOrgX = ScrollPosX;
			WinOrgY = ScrollPosY - PageStart;
			NewOrgX = WinOrgX;
			NewOrgY = WinOrgY;

			DontChangeSize = true;

			SetScrollRange(ttwinman.HVTWin.Handle, Orientation.Horizontal, 0, XRange, false);

			if ((YRange == 0) && (ts.EnableScrollBuff > 0)) {
				SetScrollRange(ttwinman.HVTWin.Handle, Orientation.Vertical, 0, 1, false);
			}
			else {
				SetScrollRange(ttwinman.HVTWin.Handle, Orientation.Vertical, 0, YRange, false);
			}

			SetScrollPos(ttwinman.HVTWin.Handle, Orientation.Horizontal, ScrollPosX, true);
			SetScrollPos(ttwinman.HVTWin.Handle, Orientation.Vertical, ScrollPosY, true);

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
#if ALPHABLEND_TYPE2
				//<!--by AKASI
				//  ScrollWindow(ttwinman.HVTWin,0,-d,&R,&R);
				BGScrollWindow(ttwinman.HVTWin, 0, -d, &R, &R);
				//-->
#else
				ScrollWindow(ttwinman.HVTWin, 0, -d, R, R);
#endif

				if ((SRegionTop == 0) && (dScroll > 0)) { // update scroll bar if BuffEnd is changed
					if ((BuffEnd == WinHeight) &&
						(ts.EnableScrollBuff > 0))
						SetScrollRange(ttwinman.HVTWin.Handle, Orientation.Vertical, 0, 1, true);
					else
						SetScrollRange(ttwinman.HVTWin.Handle, Orientation.Vertical, 0, BuffEnd - WinHeight, false);
					SetScrollPos(ttwinman.HVTWin.Handle, Orientation.Vertical, WinOrgY + PageStart, true);
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
					(ts.EnableScrollBuff > 0))
					SetScrollRange(ttwinman.HVTWin.Handle, Orientation.Vertical, 0, 1, true);
				else
					SetScrollRange(ttwinman.HVTWin.Handle, Orientation.Vertical, 0, BuffEnd - WinHeight, false);
				SetScrollPos(ttwinman.HVTWin.Handle, Orientation.Vertical, NewOrgY + PageStart, true);
			}

			if ((NewOrgX == WinOrgX) &&
				(NewOrgY == WinOrgY)) return;

			if (NewOrgX == WinOrgX) {
				d = (NewOrgY - WinOrgY) * FontHeight;
#if ALPHABLEND_TYPE2
				//<!--by AKASI
				//  ScrollWindow(ttwinman.HVTWin,0,-d,null,null);
				BGScrollWindow(ttwinman.HVTWin, 0, -d, null, null);
				//-->
#else
				ScrollWindow(ttwinman.HVTWin, 0, -d);
#endif
			}
			else if (NewOrgY == WinOrgY) {
				d = (NewOrgX - WinOrgX) * FontWidth;
#if ALPHABLEND_TYPE2
				//<!--by AKASI
				//  ScrollWindow(ttwinman.HVTWin,-d,0,null,null);
				BGScrollWindow(ttwinman.HVTWin, -d, 0, null, null);
				//-->
#else
				ScrollWindow(ttwinman.HVTWin, -d, 0);
#endif
			}
			else
				ttwinman.HVTWin.Invalidate(true);

			/* Update scroll bars */
			if (NewOrgX != WinOrgX)
				SetScrollPos(ttwinman.HVTWin.Handle, Orientation.Horizontal, NewOrgX, true);

			if (ts.AutoScrollOnlyInBottomLine == 0 && NewOrgY != WinOrgY) {
				if ((BuffEnd == WinHeight) &&
					(ts.EnableScrollBuff > 0))
					SetScrollRange(ttwinman.HVTWin.Handle, Orientation.Vertical, 0, 1, true);
				else
					SetScrollRange(ttwinman.HVTWin.Handle, Orientation.Vertical, 0, BuffEnd - WinHeight, false);
				SetScrollPos(ttwinman.HVTWin.Handle, Orientation.Vertical, NewOrgY + PageStart, true);
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

#if ALPHABLEND_TYPE2
			if (BGEnable)
				InvalidateRect(ttwinman.HVTWin, null, false);
#endif
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
				if ((ts.Language == Language.IdJapanese || ts.Language == Language.IdKorean || ts.Language == Language.IdUtf8) &&
					ttime.CanUseIME()) {
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
#if ALPHABLEND_TYPE2
			case ANSIColors.CS_VT_NORMALFG:
				BGVTColor[0] = color;
				if ((ts.ColorFlag & ColorFlags.CF_USETEXTCOLOR) != 0) {
					ANSIColor[(byte)ColorCodes.IdFore] = BGVTColor[0]; // use text color for "white"
				}
				break;
			case ANSIColors.CS_VT_NORMALBG:
				BGVTColor[1] = color;
				if ((ts.ColorFlag & ColorFlags.CF_USETEXTCOLOR) != 0) {
					ANSIColor[(byte)ColorCodes.IdBack] = BGVTColor[1]; // use background color for "Black"
				}
				if (ts.UseNormalBGColor) {
					BGVTBoldColor[1] = BGVTColor[1];
					BGVTBlinkColor[1] = BGVTColor[1];
					BGURLColor[1] = BGVTColor[1];
				}
				break;
			case ANSIColors.CS_VT_BOLDFG: BGVTBoldColor[0] = color; break;
			case ANSIColors.CS_VT_BOLDBG: BGVTBoldColor[1] = color; break;
			case ANSIColors.CS_VT_BLINKFG: BGVTBlinkColor[0] = color; break;
			case ANSIColors.CS_VT_BLINKBG: BGVTBlinkColor[1] = color; break;
			case ANSIColors.CS_VT_REVERSEFG: BGVTReverseColor[0] = color; break;
			case ANSIColors.CS_VT_REVERSEBG: BGVTReverseColor[1] = color; break;
			case ANSIColors.CS_VT_URLFG: BGURLColor[0] = color; break;
			case ANSIColors.CS_VT_URLBG: BGURLColor[1] = color; break;
#else
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
#endif
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
#if ALPHABLEND_TYPE2
			case ANSIColors.CS_VT_NORMALFG:
				BGVTColor[0] = ts.VTColor[0];
				if ((ts.ColorFlag & ColorFlags.CF_USETEXTCOLOR) != 0) {
					ANSIColor[(byte)ColorCodes.IdFore] = ts.VTColor[0]; // use text color for "white"
				}
				break;
			case ANSIColors.CS_VT_NORMALBG:
				BGVTColor[1] = ts.VTColor[1];
				if ((ts.ColorFlag & ColorFlags.CF_USETEXTCOLOR) != 0) {
					ANSIColor[(byte)ColorCodes.IdBack] = ts.VTColor[1]; // use background color for "Black"
				}
				if (ts.UseNormalBGColor) {
					BGVTBoldColor[1] = ts.VTColor[1];
					BGVTBlinkColor[1] = ts.VTColor[1];
					BGURLColor[1] = ts.VTColor[1];
				}
				break;
			case ANSIColors.CS_VT_BOLDFG: BGVTBoldColor[0] = ts.VTBoldColor[0]; break;
			case ANSIColors.CS_VT_BOLDBG: BGVTBoldColor[1] = ts.VTBoldColor[1]; break;
			case ANSIColors.CS_VT_BLINKFG: BGVTBlinkColor[0] = ts.VTBlinkColor[0]; break;
			case ANSIColors.CS_VT_BLINKBG: BGVTBlinkColor[1] = ts.VTBlinkColor[1]; break;
			case ANSIColors.CS_VT_REVERSEFG: BGVTReverseColor[0] = ts.VTReverseColor[0]; break;
			case ANSIColors.CS_VT_REVERSEBG: BGVTReverseColor[1] = ts.VTReverseColor[1]; break;
			case ANSIColors.CS_VT_URLFG: BGURLColor[0] = ts.URLColor[0]; break;
			case ANSIColors.CS_VT_URLBG: BGURLColor[1] = ts.URLColor[1]; break;
#endif
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
#if ALPHABLEND_TYPE2
						ANSIColor[(byte)ColorCodes.IdBack] = BGVTColor[1]; // use background color for "Black"
#else
						ANSIColor[(byte)ColorCodes.IdBack] = ts.VTColor[1]; // use background color for "Black"
#endif
					}
					else {
						ANSIColor[(byte)ColorCodes.IdBack] = ts.ANSIColor[(byte)ColorCodes.IdBack];
					}
					DispSetNearestColors(num, num, null);
				}
				else if (num == (int)ColorCodes.IdFore) {
					if ((ts.ColorFlag & ColorFlags.CF_USETEXTCOLOR) != 0) {
#if ALPHABLEND_TYPE2
						ANSIColor[(byte)ColorCodes.IdFore] = BGVTColor[0]; // use text color for "white"
#else
						ANSIColor[(byte)ColorCodes.IdFore] = ts.VTColor[0]; // use text color for "white"
#endif
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
			if (Background != IntPtr.Zero) DeleteObject(Background);

			if ((CurCharAttr.Attr2 & AttributeBitMasks.Attr2Back) != 0) {
				if (((int)CurCharAttr.Back < 16) && ((int)CurCharAttr.Back & 7) != 0)
					Background = CreateSolidBrush(ANSIColor[(int)CurCharAttr.Back ^ 8]);
				else
					Background = CreateSolidBrush(ANSIColor[(int)CurCharAttr.Back]);
			}
			else {
#if ALPHABLEND_TYPE2
				Background = CreateSolidBrush(BGVTColor[1]);
#else
				Background = CreateSolidBrush(ts.VTColor[1]);
#endif  // ALPHABLEND_TYPE2
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

		[DllImport("user32.dll")]
		public static extern int GetScrollPos(IntPtr hWnd, Orientation nBar);
		[DllImport("user32.dll")]
		public static extern int SetScrollPos(IntPtr hWnd, Orientation nBar, int nPos, bool bRedraw);
		[DllImport("user32.dll")]
		public static extern bool SetScrollRange(IntPtr hWnd, Orientation nBar, int nMinPos, int nMaxPos, bool bRedraw);
		[DllImport("user32.dll")]
		public static extern bool ScrollWindow(IntPtr hWnd, int XAmount, int YAmount, ref RECT lpRect, ref RECT lpClipRect);

		public static bool ScrollWindow(Control control, int XAmount, int YAmount, Rectangle rect, Rectangle clipRect)
		{
			RECT lpRect = new RECT(rect), lpClipRect = new RECT(clipRect);
			return ScrollWindow(control.Handle, XAmount, YAmount, ref lpRect, ref lpClipRect);
		}

		[DllImport("user32.dll")]
		public static extern bool ScrollWindow(IntPtr hWnd, int XAmount, int YAmount, IntPtr lpRect, IntPtr lpClipRect);

		public static bool ScrollWindow(Control control, int XAmount, int YAmount)
		{
			return ScrollWindow(control.Handle, XAmount, YAmount, IntPtr.Zero, IntPtr.Zero);
		}

		const int TRANSPARENT = 1;

		[DllImport("gdi32.dll")]
		public static extern int SetBkMode(IntPtr hdc, int iBkMode);
		[DllImport("gdi32.dll")]
		public static extern uint SetBkColor(IntPtr hdc, int crColor);

		public static uint SetBkColor(IntPtr hdc, Color color)
		{
			return SetBkColor(hdc, color.ToArgb() & 0x00FFFFFF);
		}

		[DllImport("gdi32.dll")]
		public static extern uint SetTextColor(IntPtr hdc, int crColor);

		public static uint SetTextColor(IntPtr hdc, Color color)
		{
			return SetTextColor(hdc, color.ToArgb() & 0x00FFFFFF);
		}

		[DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
		public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

		[DllImport("user32.dll")]
		public static extern int FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);

		public static int FillRect(IntPtr hDC, Rectangle rc, IntPtr hbr)
		{
			RECT lprc = new RECT(rc);
			return FillRect(hDC, ref lprc, hbr);
		}

		[DllImport("gdi32.dll")]
		public static extern bool Polyline(IntPtr hdc, Point[] lppt, int cPoints);

		const uint ETO_OPAQUE = 0x0002;
		const uint ETO_CLIPPED = 0x0004;

		//[DllImport("gdi32.dll")]
		//public static extern bool ExtTextOutW(IntPtr hdc, int X, int Y, uint fuOptions,
		//    ref RECT lprc, [MarshalAs(UnmanagedType.LPWStr)] string lpString,
		//    uint cbCount, int[] lpDx);

		//public static bool ExtTextOut(IntPtr hdc, int X, int Y, uint fuOptions, Rectangle rc, byte[] buff, int offset, int count, int[] lpDx)
		//{
		//    string lpString = Encoding.Default.GetString(buff, offset, count);
		//    return ExtTextOutW(hdc, X, Y, fuOptions, ref lprc, lpString, (uint)count, lpDx);
		//}

		[DllImport("gdi32.dll")]
		public static extern bool ExtTextOutA(IntPtr hdc, int X, int Y, uint fuOptions,
			ref RECT lprc, IntPtr lpString, uint cbCount, int[] lpDx);

		public static bool ExtTextOut(IntPtr hdc, int X, int Y, uint fuOptions, Rectangle rc, byte[] buff, int offset, int count, int[] lpDx)
		{
			bool result;
			RECT lprc = new RECT(rc);
			GCHandle hBuff = GCHandle.Alloc(buff, GCHandleType.Pinned);
			try {
				IntPtr lpString = Marshal.UnsafeAddrOfPinnedArrayElement(buff, offset);

				result = ExtTextOutA(hdc, X, Y, fuOptions, ref lprc, lpString, (uint)count, lpDx);
			}
			finally {
				hBuff.Free();
			}
			return result;
		}

		const int PS_SOLID = 0;

		[DllImport("gdi32.dll")]
		public static extern IntPtr CreatePen(int fnPenStyle, int nWidth, int crColor);

		public static IntPtr CreatePen(int fnPenStyle, int nWidth, Color color)
		{
			return CreatePen(fnPenStyle, nWidth, color.ToArgb() & 0x00FFFFFF);
		}

		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateSolidBrush(int crColor);

		public static IntPtr CreateSolidBrush(Color color)
		{
			return CreateSolidBrush(color.ToArgb() & 0x00FFFFFF);
		}

		[DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr hObject);

		[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
		public static extern bool GetTextMetrics(IntPtr hdc, out TEXTMETRIC lptm);
	}

	[Serializable, StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;

		public RECT(Rectangle rect)
		{
			Left = rect.Left;
			Top = rect.Top;
			Right = rect.Right;
			Bottom = rect.Bottom;
		}
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct TEXTMETRIC
	{
		public int tmHeight;
		public int tmAscent;
		public int tmDescent;
		public int tmInternalLeading;
		public int tmExternalLeading;
		public int tmAveCharWidth;
		public int tmMaxCharWidth;
		public int tmWeight;
		public int tmOverhang;
		public int tmDigitizedAspectX;
		public int tmDigitizedAspectY;
		public char tmFirstChar;
		public char tmLastChar;
		public char tmDefaultChar;
		public char tmBreakChar;
		public byte tmItalic;
		public byte tmUnderlined;
		public byte tmStruckOut;
		public byte tmPitchAndFamily;
		public byte tmCharSet;
	}
}
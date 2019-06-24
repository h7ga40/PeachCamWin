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

/* TERATERM.EXE, Clipboard routines */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace TeraTrem
{
	class clipboar
	{
		internal char[] CBOpen(int MemSize)
		{
			throw new NotImplementedException();
		}

		internal void CBClose()
		{
			throw new NotImplementedException();
		}

		internal static bool CBStartPasteConfirmChange(IntPtr Handle, bool p)
		{
			throw new NotImplementedException();
		}

		internal static void CBStartPaste(IntPtr Handle, bool p, object p_3, int p_4, object p_5, int p_6)
		{
			throw new NotImplementedException();
		}

		internal static object IsClipboardFormatAvailable(uint format)
		{
			throw new NotImplementedException();
		}

		public const uint CF_TEXT = 1;
		public const uint CF_BITMAP = 2;
		public const uint CF_METAFILEPICT = 3;
		public const uint CF_SYLK = 4;
		public const uint CF_DIF = 5;
		public const uint CF_TIFF = 6;
		public const uint CF_OEMTEXT = 7;
		public const uint CF_DIB = 8;
		public const uint CF_PALETTE = 9;
		public const uint CF_PENDATA = 10;
		public const uint CF_RIFF = 11;
		public const uint CF_WAVE = 12;
		public const uint CF_UNICODETEXT = 13;
		public const uint CF_ENHMETAFILE = 14;
		public const uint CF_HDROP = 15;
		public const uint CF_LOCALE = 16;
		public const uint CF_DIBV5 = 17;
		public const uint CF_MAX = 18;

		internal static void CBStartPasteB64(Control hVTWin, string hdr, string v)
		{
			throw new NotImplementedException();
		}

		internal static bool OpenClipboard(object p)
		{
			throw new NotImplementedException();
		}

		internal static void EmptyClipboard()
		{
			throw new NotImplementedException();
		}

		internal static void SetClipboardData(uint cF_TEXT, char[] cbmem)
		{
			throw new NotImplementedException();
		}

		internal static void CloseClipboard()
		{
			throw new NotImplementedException();
		}
	}
}

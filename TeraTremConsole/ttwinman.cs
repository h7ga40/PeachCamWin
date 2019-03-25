/*
 * Copyright (C) 1994-1998 T. Teranishi
 * (C) 2005-2017 TeraTerm Project
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

/* TERATERM.EXE, variables, flags related to VT win and TEK win */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace TeraTrem
{
	class ttwinman
	{
		TTTSet ts;

		public TalkerMode TalkStatus = TalkerMode.IdTalkKeyb; /* IdTalkKeyb, IdTalkCB, IdTalkTextFile */
		public Form MainForm = null;
		public Control HVTWin = null;
		public Control HTEKWin = null;
		public bool KeybEnabled = true;
		public WindowId ActiveWin = WindowId.IdVT; /* IdVT, IdTEK */

		// タイトルバーのCP932への変換を行う
		// 現在、SJIS、EUCのみに対応。
		// (2005.3.13 yutaka)
		public void ConvertToCP932(byte[] str, int destlen)
		{
			int len = str.Length;
			byte[] cc = new byte[len + 1];
			int c = 0;
			int i;
			byte b;
			char word;

			//if (strcmp(ts.Locale, DEFAULT_LOCALE) == 0)
			//{
			for (i = 0; i < len; i++) {
				b = str[i];
				if ((ts.KanjiCode == KanjiCodeId.IdSJIS && IsDBCSLeadByte(b))
					|| (ts.KanjiCode == KanjiCodeId.IdEUC && (b & 0x80) != 0)) {
					word = (char)(b << 8);

					if (i == len - 1) {
						cc[c++] = b;
						continue;
					}

					b = str[i + 1];
					word |= (char)b;
					i++;

					if (ts.KanjiCode == KanjiCodeId.IdSJIS) {
						// SJISはそのままCP932として出力する

					}
					else if (ts.KanjiCode == KanjiCodeId.IdEUC) {
						// EUC -> SJIS
						word &= unchecked((char)~0x8080u);
						word = language.JIS2SJIS(word);

					}
					else if (ts.KanjiCode == KanjiCodeId.IdJIS) {

					}
					else if (ts.KanjiCode == KanjiCodeId.IdUTF8) {

					}
					else if (ts.KanjiCode == KanjiCodeId.IdUTF8m) {

					}
					else {

					}

					cc[c++] = (byte)(word >> 8);
					cc[c++] = (byte)(word & 0xff);

				}
				else {
					cc[c++] = b;
				}
				//}

				cc[c] = 0;
				System.Buffer.BlockCopy(cc, 0, str, 0, str.Length);
			}
		}

		internal void ChangeTitle()
		{
		}

		internal void SwitchMenu()
		{
		}

		internal void SwitchTitleBar()
		{
		}

		internal void Init(ProgramDatas datas)
		{
			ts = datas.TTTSet;
		}

		[DllImport("kernel32.dll")]
		public static extern bool IsDBCSLeadByte(byte TestChar);
	}
}

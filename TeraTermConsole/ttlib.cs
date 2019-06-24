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

/* misc. routines  */
using System;
using System.Collections.Generic;
using System.Text;

namespace TeraTerm
{
	class ttlib
	{
		public static byte ConvHexChar(byte b)
		{
			if ((b >= '0') && (b <= '9')) {
				return (byte)(b - 0x30);
			}
			else if ((b >= 'A') && (b <= 'F')) {
				return (byte)(b - 0x37);
			}
			else if ((b >= 'a') && (b <= 'f')) {
				return (byte)(b - 0x57);
			}
			else {
				return 0;
			}
		}

		internal static void GetNthString(string TempStr, int p, out string KStr)
		{
			throw new NotImplementedException();
		}

		internal static void GetNthNum(string TempStr, int p, out int j)
		{
			throw new NotImplementedException();
		}

		internal static void get_lang_msg(string p, char[] uimsg, int p_3, string p_4, char[] p_5)
		{
			throw new NotImplementedException();
		}

		internal static int Hex2Str(char[] Hex, byte[] Str, int MaxLen)
		{
			byte b, c;
			int i, imax, j;

			j = 0;
			imax = Hex.Length;
			i = 0;
			while ((i < imax) && (j < MaxLen)) {
				b = (byte)Hex[i];
				if (b == '$') {
					i++;
					if (i < imax) {
						c = (byte)Hex[i];
					}
					else {
						c = 0x30;
					}
					b = (byte)(ConvHexChar(c) << 4);
					i++;
					if (i < imax) {
						c = (byte)Hex[i];
					}
					else {
						c = 0x30;
					}
					b = (byte)(b + ConvHexChar(c));
				};

				Str[j] = b;
				j++;
				i++;
			}
			if (j < MaxLen) {
				Str[j] = 0;
			}

			return j;
		}

		internal static int b64decode(char[] cbbuff, int blen, int p)
		{
			throw new NotImplementedException();
		}
	}
}

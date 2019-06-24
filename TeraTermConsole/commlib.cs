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
/* IPv6 modification is Copyright (C) 2000, 2001 Jun-ya KATO <kato@win6.jp> */

/* TERATERM.EXE, Communication routines */
using System;
using System.Collections.Generic;
using System.Text;

namespace TeraTerm
{
	class commlib
	{
		internal static void CommInit(TComVar cv)
		{
			cv.Open = false;
			cv.Ready = false;

			// log-buffer variables
			cv.HLogBuf = IntPtr.Zero;
			cv.HBinBuf = IntPtr.Zero;
			cv.LogBuf = null;
			cv.BinBuf = null;
			cv.LogPtr = 0;
			cv.LStart = 0;
			cv.LCount = 0;
			cv.BinPtr = 0;
			cv.BStart = 0;
			cv.BCount = 0;
			cv.DStart = 0;
			cv.DCount = 0;
			cv.BinSkip = 0;
			cv.FilePause = 0;
			cv.ProtoFlag = false;
			/* message flag */
			cv.NoMsg = 0;
		}

		internal static void CommResetSerial(ref TTTSet ts, ref TComVar cv, bool p)
		{
			throw new NotImplementedException();
		}

		internal static bool CommCanClose(TComVar cv)
		{
			throw new NotImplementedException();
		}

		internal static void CommClose(TComVar cv)
		{
			//throw new NotImplementedException();
		}
	}
}

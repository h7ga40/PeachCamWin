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

/* Constants and types for Tera Term */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace TeraTrem
{
	struct addrinfo
	{
	}

	enum TimerId
	{
		IdBreakTimer = 1,
		IdDelayTimer = 2,
		IdProtoTimer = 3,
		IdDblClkTimer = 4,
		IdScrollTimer = 5,
		IdComEndTimer = 6,
		IdCaretTimer = 7,
		IdPrnStartTimer = 8,
		IdPrnProcTimer = 9,
		IdCancelConnectTimer = 10,  // add (2007.1.10 yutaka)
		IdPasteDelayTimer = 11,
	}

	/* Window Id */
	enum WindowId
	{
		IdVT = 1,
		IdTEK = 2,
	}

	/* Talker mode */
	enum TalkerMode
	{
		IdTalkKeyb = 0,
		IdTalkCB = 1,
		IdTalkFile = 2,
		IdTalkQuiet = 3,
	}

	/* Character sets */
	enum CharacterSets
	{
		IdASCII = 0,
		IdKatakana = 1,
		IdKanji = 2,
		IdSpecial = 3,
	}

	/* Character attribute bit masks */
	enum AttributeBitMasks
	{
		AttrDefault = 0x00,
		AttrDefaultFG = 0x00,
		AttrDefaultBG = 0x00,
		AttrBold = 0x01,
		AttrUnder = 0x02,
		AttrSpecial = 0x04,
		AttrFontMask = 0x07,
		AttrBlink = 0x08,
		AttrReverse = 0x10,
		AttrLineContinued = 0x20 /* valid only at the beggining or end of a line */,
		/* begin - ishizaki */
		AttrURL = 0x40,
		/* end - ishizaki */
		AttrKanji = 0x80,
		/* Color attribute bit masks */
		Attr2Fore = 0x01,
		Attr2Back = 0x02,
		AttrSgrMask = (AttrBold | AttrUnder | AttrBlink | AttrReverse),
		AttrColorMask = (AttrBold | AttrBlink | AttrReverse),
		Attr2ColorMask = (Attr2Fore | Attr2Back),
		Attr2Protect = 0x04,
	}

	struct TCharAttr
	{
		public AttributeBitMasks Attr;
		public AttributeBitMasks Attr2;
		public ColorCodes Fore;
		public ColorCodes Back;

		public TCharAttr(AttributeBitMasks Attr, AttributeBitMasks Attr2, ColorCodes Fore, ColorCodes Back)
		{
			this.Attr = Attr;
			this.Attr2 = Attr2;
			this.Fore = Fore;
			this.Back = Back;
		}
	}

	/* Color codes */
	enum ColorCodes
	{
		IdBack = 0,
		IdRed = 1,
		IdGreen = 2,
		IdYellow = 3,
		IdBlue = 4,
		IdMagenta = 5,
		IdCyan = 6,
		IdFore = 7,
	}

	/* for DispSetColor / DispGetColor */
	// ANSIColor -- 0-255
	enum ANSIColors : uint
	{
		CS_VT_NORMALFG = 256,
		CS_VT_NORMALBG = 257,
		CS_VT_BOLDFG = 258,
		CS_VT_BOLDBG = 259,
		CS_VT_BLINKFG = 260,
		CS_VT_BLINKBG = 261,
		CS_VT_REVERSEFG = 262,
		CS_VT_REVERSEBG = 263,
		CS_VT_URLFG = 264,
		CS_VT_URLBG = 265,
		CS_VT_UNDERFG = 266,
		CS_VT_UNDERBG = 267,
		CS_TEK_FG = 268,
		CS_TEK_BG = 269,
		CS_ANSICOLOR_ALL = 270,
		CS_SP_ALL = 271,
		CS_UNSPEC = 0xffffffff,
		CS_ALL = CS_UNSPEC,
	}

	/* Kermit function id */
	enum KermitFunctionId
	{
		IdKmtReceive = 1,
		IdKmtGet = 2,
		IdKmtSend = 3,
		IdKmtFinish = 4,
	}

	/* XMODEM function id */
	enum XmodemFunctionId
	{
		IdXReceive = 1,
		IdXSend = 2,
	}

	/* YMODEM function id */
	enum YmodemFunctionId
	{
		IdYReceive = 1,
		IdYSend = 2,
	}

	/* ZMODEM function id */
	enum ZMODEMFunctionId
	{
		IdZReceive = 1,
		IdZSend = 2,
		IdZAutoR = 3,
		IdZAutoS = 4,
	}

	/* B-Plus function id */
	enum BPlusFunctionId
	{
		IdBPReceive = 1,
		IdBPSend = 2,
		IdBPAuto = 3,
	}

	/* Quick-VAN function id */
	enum QuickVanFunctionId
	{
		IdQVReceive = 1,
		IdQVSend = 2,
	}

	/* port type ID */
	enum PortTypeId
	{
		IdTCPIP = 1,
		IdSerial = 2,
		IdFile = 3,
		IdNamedPipe = 4,
	}

	/* XMODEM option */
	enum XmodemOption
	{
		XoptCheck = 1,
		XoptCRC = 2,
		Xopt1kCRC = 3,
		Xopt1kCksum = 4,
	}

	/* YMODEM option */
	enum YmodemOption
	{
		Yopt1K = 1,
		YoptG = 2,
		YoptSingle = 3,
	}

	/* KERMIT option */
	enum KERMITOption
	{
		KmtOptLongPacket = 1,
		KmtOptFileAttr = 2,
		KmtOptSlideWin = 4,
	}

	/* Language */
	enum Language
	{
		IdEnglish = 1,
		IdJapanese = 2,
		IdRussian = 3,
		IdKorean = 4, //HKS
		IdUtf8 = 5,
		IdLangMax = IdUtf8,
	}

	// LogDialog Option
	enum LogDialogOption
	{
		LOGDLG_BINARY = 1,
		LOGDLG_APPEND = (1 << 1),
		LOGDLG_PLAINTEXT = (1 << 2),
		LOGDLG_TIMESTAMP = (1 << 3),
		LOGDLG_HIDEDIALOG = (1 << 4),
		LOGDLG_INCSCRBUFF = (1 << 5),
		LOGDLG_UTC = (1 << 6),
		LOGDLG_ELAPSED = (1 << 7),
		/*
		 * ELAPSED TIME の時は LOGDLG_UTC を経過時間の基準を表すフラグとする
		 * LOGDLG_ELAPSEDCON == 0 => ログ開始から
		 * LOGDLG_ELAPSEDCON == 1 => 接続開始から
		 */
		LOGDLG_ELAPSEDCON = LOGDLG_UTC,
	}

	// Log Timestamp Type
	enum LogTimestampType
	{
		TIMESTAMP_LOCAL,
		TIMESTAMP_UTC,
		TIMESTAMP_ELAPSED_LOGSTART,
		TIMESTAMP_ELAPSED_CONNECTED
	};

	// log flags (used in ts.LogFlag) 
	enum LogFlags
	{
		LOG_TEL = 1,
		LOG_KMT = 2,
		LOG_X = 4,
		LOG_Z = 8,
		LOG_BP = 16,
		LOG_QV = 32,
		LOG_Y = 64,
	}

	// file transfer flags (used in ts.FTFlag)
	enum FileTransferFlags
	{
		FT_ZESCCTL = 1,
		FT_ZAUTO = 2,
		FT_BPESCCTL = 4,
		FT_BPAUTO = 8,
		FT_RENAME = 16,
	}

	// menu flags (used in ts.MenuFlag)
	enum MenuFlags
	{
		MF_NOSHOWMENU = 1,
		MF_NOPOPUP = 2,
		MF_NOLANGUAGE = 4,
		MF_SHOWWINMENU = 8,
	}

	// Terminal flags (used in ts.TermFlag)
	enum TerminalFlags
	{
		TF_FIXEDJIS = 1,
		TF_AUTOINVOKE = 2,
		TF_CTRLINKANJI = 8,
		TF_ALLOWWRONGSEQUENCE = 16,
		TF_ACCEPT8BITCTRL = 32,
		TF_ENABLESLINE = 64,
		TF_BACKWRAP = 128,
		TF_CLEARONRESIZE = 256,
		TF_ALTSCR = 512,
		TF_LOCKTUID = 1024,
		TF_INVALIDDECRPSS = 2048,
		TF_PRINTERCTRL = 4096,
	}

	// ANSI/Attribute color flags (used in ts.ColorFlag)
	enum ColorFlags
	{
		CF_PCBOLD16 = 1,
		CF_AIXTERM16 = 2,
		CF_XTERM256 = 4,
		CF_FULLCOLOR = (CF_PCBOLD16 | CF_AIXTERM16 | CF_XTERM256),

		CF_ANSICOLOR = 8,

		CF_BOLDCOLOR = 16,
		CF_BLINKCOLOR = 32,
		CF_REVERSECOLOR = 64,
		CF_URLCOLOR = 128,

		CF_USETEXTCOLOR = 256,
		CF_REVERSEVIDEO = 512,
	}

	// Font flags (used in ts.FontFlag)
	enum FontFlags
	{
		FF_BOLD = 1,
		FF_FAINT = 2,  // Not used
		FF_ITALIC = 4,   // Not used
		FF_UNDERLINE = 8,   // Not used
		FF_BLINK = 16,  // Not used
		FF_RAPIDBLINK = 32,  // Not used
		FF_REVERSE = 64,  // Not used
		FF_INVISIBLE = 128, // Not used
		FF_STRIKEOUT = 256, // Not used
		FF_URLUNDERLINE = 512,
	}

	// port flags (used in ts.PortFlag)
	enum PortFlags
	{
		PF_CONFIRMDISCONN = 1,
		PF_BEEPONCONNECT = 2,
	}

	// Window flags (used in ts.WindowFlag)
	enum WindowFlags
	{
		WF_CURSORCHANGE = 1,
		WF_WINDOWCHANGE = 2,
		WF_WINDOWREPORT = 4,
		WF_TITLEREPORT = 24, // (8 | 16)
		WF_IMECURSORCHANGE = 32,
	}

	// Tab Stop flags (used in ts.TabStopFlag)
	enum TabStopflags
	{
		TABF_NONE = 0,
		TABF_HTS7 = 1,
		TABF_HTS8 = 2,
		TABF_TBC0 = 4,
		TABF_TBC3 = 8,
		TABF_HTS = (TABF_HTS7 | TABF_HTS8),
		TABF_TBC = (TABF_TBC0 | TABF_TBC3),
		TABF_ALL = (TABF_HTS | TABF_TBC),
	}

	// ISO 2022 Shift flags (used in ts.ISO2022Flag)
	enum ISO2022ShiftFlags
	{
		ISO2022_SHIFT_NONE = 0x0000,
		ISO2022_SI = 0x0001,
		ISO2022_SO = 0x0002,
		ISO2022_LS2 = 0x0004,
		ISO2022_LS3 = 0x0008,
		ISO2022_LS1R = 0x0010,
		ISO2022_LS2R = 0x0020,
		ISO2022_LS3R = 0x0040,
		ISO2022_SS2 = 0x0100,
		ISO2022_SS3 = 0x0200,
		ISO2022_LS = (ISO2022_SI | ISO2022_SO | ISO2022_LS2 | ISO2022_LS3),
		ISO2022_LSR = (ISO2022_LS1R | ISO2022_LS2R | ISO2022_LS3R),
		ISO2022_SS = (ISO2022_SS2 | ISO2022_SS3),
		ISO2022_SHIFT_ALL = (ISO2022_LS | ISO2022_LSR | ISO2022_SS),
	}

	// Control Sequence flags (used in ts.CtrlFlag)
	enum ControlSequenceFlags
	{
		CSF_CBWRITE = 1,
		CSF_CBREAD = 2,
		CSF_CBRW = (CSF_CBREAD | CSF_CBWRITE),
	}

	// Debug Flags (used in ts.DebugModes)
	enum DebugFlags
	{
		DBGF_NONE = 0,
		DBGF_NORM = 1,
		DBGF_HEXD = 2,
		DBGF_NOUT = 4,
		DBGF_ALL = (DBGF_NORM | DBGF_HEXD | DBGF_NOUT),
	}

	// Clipboard Paste Flags (used in ts.PasteFlag)
	enum ClipboardPasteFlags
	{
		CPF_DISABLE_RBUTTON = 0x0001,
		CPF_CONFIRM_RBUTTON = 0x0002,
		CPF_DISABLE_MBUTTON = 0x0004,
		CPF_CONFIRM_CHANGEPASTE = 0x0010,
		CPF_CONFIRM_CHANGEPASTE_CR = 0x0020,
		CPF_TRIM_TRAILING_NL = 0x0100,
		CPF_NORMALIZE_LINEBREAK = 0x0200,
	}

	// Title Reporting Type
	enum TitleReportingType
	{
		IdTitleReportIgnore = 0,
		IdTitleReportAccept = 8,
		IdTitleReportEmpty = 24,
	}

	// iconf flags (used in ts.VTIcon and ts.TEKIcon)
	enum IconfFlags
	{
		IdIconDefault = 0,
	}

	// Beep type
	enum BeepType
	{
		IdBeepOff = 0,
		IdBeepOn = 1,
		IdBeepVisual = 2,
	}

	// TitleChangeRequest types
	enum TitleChangeRequestTypes
	{
		IdTitleChangeRequestOff = 0,
		IdTitleChangeRequestOverwrite = 1,
		IdTitleChangeRequestAhead = 2,
		IdTitleChangeRequestLast = 3,
	}

	// Meta8Bit mode
	enum Meta8BitMode
	{
		IdMeta8BitOff = 0,
		IdMeta8BitRaw = 1,
		IdMeta8BitText = 2,
	}

	// Eterm lookfeel alphablend structure
	class eterm_lookfeel_t
	{
		int BGEnable;
		int BGUseAlphaBlendAPI;
		char[] BGSPIPath = new char[tttypes.MAX_PATH];
		int BGFastSizeMove;
		int BGNoCopyBits;
		int BGNoFrame;
		char[] BGThemeFile = new char[tttypes.MAX_PATH];
	};

	/* TTTSet */
	//
	// NOTE: 下記のエラーがでることがある
	//   fatal error C1001: INTERNAL COMPILER ERROR (compiler file 'msc1.cpp', line 2701)
	// 
	class TTTSet
	{
		public const int LF_FACESIZE = 260;

		/*------ VTSet --------*/
		/* Tera Term home directory */
		public string HomeDir;

		/* Setup file name */
		public string SetupFName;
		public string KeyCnfFN;
		public string LogFN;
		public string MacroFN;
		public string HostName;

		public Point VTPos;
		public string VTFont;
		public Point VTFontSize;
		public byte VTFontCharSet;
		public int FontDW, FontDH, FontDX, FontDY;
		public char[] PrnFont = new char[LF_FACESIZE];
		public Point PrnFontSize;
		public int PrnFontCharSet;
		public Point VTPPI, TEKPPI;
		public int[] PrnMargin = new int[4];
		public char[] PrnDev = new char[80];
		public short PassThruDelay;
		public short PrnConvFF;
		public FontFlags FontFlag;
		public short RussFont;
		public int ScrollThreshold;
		public short Debug;
		public short LogFlag;
		public FileTransferFlags FTFlag;
		public short TransBin, Append;
		public short XmodemOpt, XmodemBin;
		public int ZmodemDataLen, ZmodemWinSize;
		public int QVWinSize;
		public char[] FileDir = new char[tttypes.MAXPATHLEN];
		public char[] FileSendFilter = new char[128];
		public Language Language;
		public char[] DelimList = new char[52];
		public short DelimDBCS;
		public short Minimize;
		public short HideWindow;
		public short MenuFlag;
		public short SelOnActive;
		public short AutoTextCopy;
		/*------ TEKSet --------*/
		public Point TEKPos;
		public char[] TEKFont = new char[LF_FACESIZE];
		public Point TEKFontSize;
		public int TEKFontCharSet;
		public int GINMouseCode;
		/*------ TermSet --------*/
		public int TerminalWidth;
		public int TerminalHeight;
		public bool TermIsWin;
		public bool AutoWinResize;
		public NewLineModes CRSend;
		public NewLineModes CRReceive;
		public bool LocalEcho;
		public char[] Answerback = new char[32];
		public int AnswerbackLen;
		public KanjiCodeId KanjiCode;
		public KanjiCodeId KanjiCodeSend;
		public bool JIS7Katakana;
		public bool JIS7KatakanaSend;
		public KanjiInModes KanjiIn;
		public KanjiInModes KanjiOut;
		public short RussHost;
		public short RussClient;
		public short RussPrint;
		public short AutoWinSwitch;
		public TerminalId TerminalID;
		public TerminalFlags TermFlag;
		/*------ WinSet --------*/
		public short VTFlag;
		public Font SampleFont;
		/* begin - ishizaki */
		/* short TmpColor[3][6]; */
		public short[,] TmpColor = new short[12, 6];
		/* end - ishizaki */
		/* Tera Term window setup variables */
		public char[] Title = new char[tttypes.TitleBuffSize];
		public short TitleFormat;
		public CursorShapes CursorShape;
		public bool NonblinkingCursor;
		public ushort EnableScrollBuff;
		public int ScrollBuffSize;
		public int ScrollBuffMax;
		public short HideTitle;
		public short PopupMenu;
		public ColorFlags ColorFlag;
		public short TEKColorEmu;
		public Color[] VTColor = new Color[2];
		public Color[] TEKColor = new Color[2];
		/* begin - ishizaki */
		public Color[] URLColor = new Color[2];
		/* end   - ishizaki */
		public Color[] VTBoldColor = new Color[2];       // SGR 1
		public Color[] VTFaintColor = new Color[2];      // SGR 2
		public Color[] VTItalicColor = new Color[2];     // SGR 3
		public Color[] VTUnderlineColor = new Color[2];  // SGR 4
		public Color[] VTBlinkColor = new Color[2];      // SGR 5
		public Color[] VTRapidBlinkColor = new Color[2]; // SGR 6
		public Color[] VTReverseColor = new Color[2];    // SGR 7
		public Color[] VTInvisibleColor = new Color[2];  // SGR 8
		public Color[] VTStrikeoutColor = new Color[2];  // SGR 9
		public Color[] DummyColor = new Color[2];
		public BeepType Beep;
		/*------ KeybSet --------*/
		public DelId BSKey;
		public short DelKey;
		public bool UseIME;
		public bool IMEInline;
		public MetaId MetaKey;
		public short RussKeyb;
		/*------ PortSet --------*/
		public PortTypeId PortType;
		/* TCP/IP */
		public short TCPPort;
		public short Telnet;
		public short TelPort;
		public short TelBin;
		public short TelEcho;
		public char[] TermType = new char[40];
		public short AutoWinClose;
		public PortFlags PortFlag;
		public short TCPCRSend;
		public short TCPLocalEcho;
		public short HistoryList;
		/* Serial */
		public short ComPort;
		public short Baud_; /* not in use */
		public short Parity;
		public short DataBit;
		public short StopBit;
		public short Flow;
		public short DelayPerChar;
		public short DelayPerLine;
		public short MaxComPort;
		public short ComAutoConnect;
#if !NO_COPYLINE_FIX
		public bool EnableContinuedLineCopy;
#endif // NO_COPYLINE_FIX */
#if !NO_ANSI_COLOR_EXTENSION
		public Color[] ANSIColor = new Color[16];
#endif // NO_ANSI_COLOR_EXTENSION */
#if !NO_INET6
		/* protocol used in connect() */
		public int ProtocolFamily;
#endif // NO_INET6
		public Cursor MouseCursorName = Cursors.IBeam;
		public int AlphaBlend;
		public char[] CygwinDirectory = new char[tttypes.MAX_PATH];
		public const string DEFAULT_LOCALE = "japanese";
		public char[] Locale = new char[80];
		public const int DEFAULT_CODEPAGE = 932;
		public int CodePage;
		public char[] ViewlogEditor = new char[tttypes.MAX_PATH];
		public bool LogTypePlainText;
		public short LogTimestamp;
		public char[] LogDefaultName = new char[80];
		public char[] LogDefaultPath = new char[tttypes.MAX_PATH];
		public short LogAutoStart;
		public bool DisablePasteMouseRButton;
		public bool ConfirmPasteMouseRButton;
		public short DisableAcceleratorSendBreak;
		public bool EnableClickableUrl;
		public eterm_lookfeel_t EtermLookfeel;
#if USE_NORMAL_BGCOLOR
		public short UseNormalBGColor;
#endif
		public char[] UILanguageFile = new char[tttypes.MAX_PATH];
		public char[] UIMsg = new char[tttypes.MAX_UIMSG];
		public short BroadcastCommandHistory;
		public short AcceptBroadcast;       // 337: 2007/03/20
		public bool DisableTCPEchoCR;  // TCPLocalEcho/TCPCRSend を無効にする (maya 2007.4.25)
		public int ConnectingTimeout;
		public bool VTCompatTab;
		public short TelKeepAliveInterval;
		public short MaxBroadcatHistory;
		public bool DisableAppKeypad;
		public bool DisableAppCursor;
		public short ClearComBuffOnOpen;
		public bool Send8BitCtrl;
		public char[] UILanguageFile_ini = new char[tttypes.MAX_PATH];
		public bool SelectOnlyByLButton;
		public short TelAutoDetect;
		public char[] XModemRcvCommand = new char[tttypes.MAX_PATH];
		public char[] ZModemRcvCommand = new char[tttypes.MAX_PATH];
		public short ConfirmFileDragAndDrop;
		public bool TranslateWheelToCursor;
		public short HostDialogOnStartup;
		public bool MouseEventTracking;
		public bool KillFocusCursor;
		public short LogHideDialog;
		public int TerminalOldWidth;
		public int TerminalOldHeight;
		public short MaximizedBugTweak;
		public short NotifyClipboardAccess;
		public short SaveVTWinPos;
		public bool DisablePasteMouseMButton;
		public int MouseWheelScrollLine;
		public NewLineModes CRSend_ini;
		public bool LocalEcho_ini;
		public char UnicodeDecSpMapping;
		public short VTIcon;
		public short TEKIcon;
		public bool ScrollWindowClearScreen;
		public short AutoScrollOnlyInBottomLine;
		public bool UnknownUnicodeCharaAsWide;
		public char[] YModemRcvCommand = new char[tttypes.MAX_PATH];
		public TitleChangeRequestTypes AcceptTitleChangeRequest;
		public Size PasteDialogSize;
		public bool DisableMouseTrackingByCtrl;
		public bool DisableWheelToCursorByCtrl;
		public bool StrictKeyMapping;
		public short Wait4allMacroCommand;
		public short DisableMenuSendBreak;
		public bool ClearScreenOnCloseConnection;
		public short DisableAcceleratorDuplicateSession;
		public int PasteDelayPerLine;
		public bool FontScaling;
		public Meta8BitMode Meta8Bit;
		public WindowFlags WindowFlag;
		public TabStopflags TabStopFlag;
		public short EnableLineMode;
		public char[] ConfirmChangePasteStringFile = new char[tttypes.MAX_PATH];
		public int Baud;
		public short LogBinary;
		public short DisableMenuDuplicateSession;
		public short DisableMenuNewConnection;
		public char[] TerminalUID = new char[9];
		public short ConfirmChangePasteCR;
		public bool JoinSplitURL;
		public int MaxOSCBufferSize;
		public char JoinSplitURLIgnoreEOLChar;
		public ISO2022ShiftFlags ISO2022Flag;
		public bool FallbackToCP932;
		public int BeepSuppressTime;
		public int BeepOverUsedTime;
		public int BeepOverUsedCount;
		internal bool UseNormalBGColor;
		internal ControlSequenceFlags CtrlFlag;

		internal void CopyTo(TTTSet dest)
		{
			dest.HomeDir = HomeDir;
			dest.SetupFName = SetupFName;
			dest.KeyCnfFN = KeyCnfFN;
			dest.VTPos = VTPos;
			dest.VTFont = VTFont;
			dest.VTFontSize = VTFontSize;
			dest.VTFontCharSet = VTFontCharSet;
			System.Buffer.BlockCopy(PrnFont, 0, dest.PrnFont, 0, PrnFont.Length);
			System.Buffer.BlockCopy(PrnMargin, 0, dest.PrnMargin, 0, PrnMargin.Length);
			System.Buffer.BlockCopy(PrnDev, 0, dest.PrnDev, 0, PrnDev.Length);
			dest.FontFlag = FontFlag;
			dest.LogFlag = LogFlag;
			dest.FTFlag = FTFlag;
			System.Buffer.BlockCopy(FileDir, 0, dest.FileDir, 0, FileDir.Length);
			System.Buffer.BlockCopy(FileSendFilter, 0, dest.FileSendFilter, 0, FileSendFilter.Length);
			dest.Language = Language;
			System.Buffer.BlockCopy(DelimList, 0, dest.DelimList, 0, DelimList.Length);
			dest.Minimize = Minimize;
			dest.HideWindow = HideWindow;
			dest.MenuFlag = MenuFlag;
			dest.TerminalWidth = TerminalWidth;
			dest.TerminalHeight = TerminalHeight;
			dest.CRSend = CRSend;
			dest.CRReceive = CRReceive;
			dest.LocalEcho = LocalEcho;
			System.Buffer.BlockCopy(Answerback, 0, dest.Answerback, 0, Answerback.Length);
			dest.AnswerbackLen = AnswerbackLen;
			dest.KanjiCode = KanjiCode;
			dest.KanjiCodeSend = KanjiCodeSend;
			dest.JIS7Katakana = JIS7Katakana;
			dest.JIS7KatakanaSend = JIS7KatakanaSend;
			dest.KanjiIn = KanjiIn;
			dest.KanjiOut = KanjiOut;
			dest.TermFlag = TermFlag;
			dest.VTFlag = VTFlag;
			System.Buffer.BlockCopy(TmpColor, 0, dest.TmpColor, 0, TmpColor.Length);
			System.Buffer.BlockCopy(Title, 0, dest.Title, 0, Title.Length);
			dest.CursorShape = CursorShape;
			dest.NonblinkingCursor = NonblinkingCursor;
			dest.EnableScrollBuff = EnableScrollBuff;
			dest.ScrollBuffSize = ScrollBuffSize;
			dest.ScrollBuffMax = ScrollBuffMax;
			dest.ColorFlag = ColorFlag;
			BlockCopy(VTColor, 0, dest.VTColor, 0, VTColor.Length);
			BlockCopy(TEKColor, 0, dest.TEKColor, 0, TEKColor.Length);
			BlockCopy(URLColor, 0, dest.URLColor, 0, URLColor.Length);
			BlockCopy(VTBoldColor, 0, dest.VTBoldColor, 0, VTBoldColor.Length);       // SGR 1
			BlockCopy(VTFaintColor, 0, dest.VTFaintColor, 0, VTFaintColor.Length);      // SGR 2
			BlockCopy(VTItalicColor, 0, dest.VTItalicColor, 0, VTItalicColor.Length);     // SGR 3
			BlockCopy(VTUnderlineColor, 0, dest.VTUnderlineColor, 0, VTUnderlineColor.Length);  // SGR 4
			BlockCopy(VTBlinkColor, 0, dest.VTBlinkColor, 0, VTBlinkColor.Length);      // SGR 5
			BlockCopy(VTRapidBlinkColor, 0, dest.VTRapidBlinkColor, 0, VTRapidBlinkColor.Length); // SGR 6
			BlockCopy(VTReverseColor, 0, dest.VTReverseColor, 0, VTReverseColor.Length);    // SGR 7
			BlockCopy(VTInvisibleColor, 0, dest.VTInvisibleColor, 0, VTInvisibleColor.Length);  // SGR 8
			BlockCopy(VTStrikeoutColor, 0, dest.VTStrikeoutColor, 0, VTStrikeoutColor.Length);  // SGR 9
			BlockCopy(DummyColor, 0, dest.DummyColor, 0, DummyColor.Length);
			dest.BSKey = BSKey;
			dest.UseIME = UseIME;
			dest.TelPort = TelPort;
			dest.PortFlag = PortFlag;
			BlockCopy(ANSIColor, 0, dest.ANSIColor, 0, sizeof(ANSIColors));
			dest.MouseCursorName = MouseCursorName;
			System.Buffer.BlockCopy(CygwinDirectory, 0, dest.CygwinDirectory, 0, CygwinDirectory.Length);
			System.Buffer.BlockCopy(Locale, 0, dest.Locale, 0, Locale.Length);
			System.Buffer.BlockCopy(ViewlogEditor, 0, dest.ViewlogEditor, 0, ViewlogEditor.Length);
			System.Buffer.BlockCopy(LogDefaultName, 0, dest.LogDefaultName, 0, LogDefaultName.Length);
			System.Buffer.BlockCopy(LogDefaultPath, 0, dest.LogDefaultPath, 0, LogDefaultPath.Length);
			System.Buffer.BlockCopy(UILanguageFile, 0, dest.UILanguageFile, 0, UILanguageFile.Length);
			System.Buffer.BlockCopy(UIMsg, 0, dest.UIMsg, 0, UIMsg.Length);
			dest.DisableTCPEchoCR = DisableTCPEchoCR;  // TCPLocalEcho/TCPCRSend を無効にする (maya 2007.4.25)
			dest.TerminalOldWidth = TerminalOldWidth;
			dest.TerminalOldHeight = TerminalOldHeight;
			dest.CRSend_ini = CRSend_ini;
			dest.LocalEcho_ini = LocalEcho_ini;
			System.Buffer.BlockCopy(YModemRcvCommand, 0, dest.YModemRcvCommand, 0, YModemRcvCommand.Length);
			System.Buffer.BlockCopy(TerminalUID, 0, dest.TerminalUID, 0, TerminalUID.Length);
		}

		private void BlockCopy(Color[] src, int srcOffset, Color[] dst, int dstOffset, int count)
		{
			for (int i = 0; i < count; i++) {
				dst[dstOffset] = src[srcOffset];
			}
		}
	}

	/* New Line modes */
	enum NewLineModes
	{
		IdCR = 1,
		IdCRLF = 2,
		IdLF = 3,
		IdAUTO = 4,
	}

	/* Terminal ID */
	enum TerminalId
	{
		IdVT100 = 1,
		IdVT100J = 2,
		IdVT101 = 3,
		IdVT102 = 4,
		IdVT102J = 5,
		IdVT220J = 6,
		IdVT282 = 7,
		IdVT320 = 8,
		IdVT382 = 9,
		IdVT420 = 10,
		IdVT520 = 11,
		IdVT525 = 12,
	}

	/* Kanji Code ID */
	enum KanjiCodeId
	{
		IdSJIS = 1,
		IdEUC = 2,
		IdJIS = 3,
		IdUTF8 = 4,
		IdUTF8m = 5,
	}

	// Russian code sets
	enum RussianCodeSets
	{
		IdWindows = 1,
		IdKOI8 = 2,
		Id866 = 3,
		IdISO = 4,
	}

	/* KanjiIn modes */
	enum KanjiInModes
	{
		IdKanjiInA = 1,
		IdKanjiInB = 2,
		/* KanjiOut modes */
		IdKanjiOutB = 1,
		IdKanjiOutJ = 2,
		IdKanjiOutH = 3,
	}

	/* Cursor shapes */
	enum CursorShapes
	{
		IdBlkCur = 1,
		IdVCur = 2,
		IdHCur = 3,
	}

	enum DelId
	{
		IdBS = 1,
		IdDEL = 2,
	}

	enum MetaId
	{
		IdMetaOff = 0,
		IdMetaOn = 1,
		IdMetaLeft = 2,
		IdMetaRight = 3,
	}

	/* Mouse tracking mode */
	enum MouseTrackingMode
	{
		IdMouseTrackNone = 0,
		IdMouseTrackDECELR = 1,
		IdMouseTrackX10 = 2,
		IdMouseTrackVT200 = 3,
		IdMouseTrackVT200Hl = 4, // not supported
		IdMouseTrackBtnEvent = 5,
		IdMouseTrackAllEvent = 6,
		IdMouseTrackNetTerm = 7,
		IdMouseTrackJSBTerm = 8,
	}

	/* Extended mouse tracking mode */
	enum ExtendedMouseTrackingMode
	{
		IdMouseTrackExtNone = 0,
		IdMouseTrackExtUTF8 = 1,
		IdMouseTrackExtSGR = 2,
		IdMouseTrackExtURXVT = 3,
	}

	/* Mouse event */
	enum MouseEvent
	{
		IdMouseEventCurStat = 0,
		IdMouseEventBtnDown = 1,
		IdMouseEventBtnUp = 2,
		IdMouseEventMove = 3,
		IdMouseEventWheel = 4,
	}

	/* Mouse buttons */
	enum MouseButtons
	{
		IdLeftButton = 0,
		IdMiddleButton = 1,
		IdRightButton = 2,
		IdButtonRelease = 3,
	}

	/* Serial port ID */
	enum SerialPortId
	{
		IdCOM1 = 1,
		IdCOM2 = 2,
		IdCOM3 = 3,
		IdCOM4 = 4,
		/* Baud rate ID */
		BaudNone = 0,
	}

	/* Parity ID */
	enum ParityId
	{
		IdParityNone = 1,
		IdParityOdd = 2,
		IdParityEven = 3,
		IdParityMark = 4,
		IdParitySpace = 5,
	}
	/* Data bit ID */
	enum DataBitId
	{
		IdDataBit7 = 1,
		IdDataBit8 = 2,
	}
	/* Stop bit ID */
	enum StopBitId
	{
		IdStopBit1 = 1,
		IdStopBit15 = 2,
		IdStopBit2 = 3,
	}
	/* Flow control ID */
	enum FlowControlId
	{
		IdFlowX = 1,
		IdFlowHard = 2,
		IdFlowNone = 3,
	}

	/* GetHostName dialog record */
	struct TGetHNRec
	{
		string SetupFN; // setup file name
		short PortType; // TCPIP/Serial
		string HostName; // host name 
		short Telnet; // non-zero: enable telnet
		short TelPort; // default TCP port# for telnet
		short TCPPort; // TCP port #
#if !NO_INET6
		short ProtocolFamily; // Protocol Family (AF_INET/AF_INET6/AF_UNSPEC)
#endif // NO_INET6
		short ComPort; // serial port #
		short MaxComPort; // max serial port #
	}

	/* Tera Term internal key codes */
	enum InternalKeyCodes
	{
		IdUp = 1,
		IdDown = 2,
		IdRight = 3,
		IdLeft = 4,
		Id0 = 5,
		Id1 = 6,
		Id2 = 7,
		Id3 = 8,
		Id4 = 9,
		Id5 = 10,
		Id6 = 11,
		Id7 = 12,
		Id8 = 13,
		Id9 = 14,
		IdMinus = 15,
		IdComma = 16,
		IdPeriod = 17,
		IdSlash = 18,
		IdAsterisk = 19,
		IdPlus = 20,
		IdEnter = 21,
		IdPF1 = 22,
		IdPF2 = 23,
		IdPF3 = 24,
		IdPF4 = 25,
		IdFind = 26,
		IdInsert = 27,
		IdRemove = 28,
		IdSelect = 29,
		IdPrev = 30,
		IdNext = 31,
		IdF6 = 32,
		IdF7 = 33,
		IdF8 = 34,
		IdF9 = 35,
		IdF10 = 36,
		IdF11 = 37,
		IdF12 = 38,
		IdF13 = 39,
		IdF14 = 40,
		IdHelp = 41,
		IdDo = 42,
		IdF17 = 43,
		IdF18 = 44,
		IdF19 = 45,
		IdF20 = 46,
		IdXF1 = 47,
		IdXF2 = 48,
		IdXF3 = 49,
		IdXF4 = 50,
		IdXF5 = 51,
		IdUDK6 = 52,
		IdUDK7 = 53,
		IdUDK8 = 54,
		IdUDK9 = 55,
		IdUDK10 = 56,
		IdUDK11 = 57,
		IdUDK12 = 58,
		IdUDK13 = 59,
		IdUDK14 = 60,
		IdUDK15 = 61,
		IdUDK16 = 62,
		IdUDK17 = 63,
		IdUDK18 = 64,
		IdUDK19 = 65,
		IdUDK20 = 66,
		IdHold = 67,
		IdPrint = 68,
		IdBreak = 69,
		IdCmdEditCopy = 70,
		IdCmdEditPaste = 71,
		IdCmdEditPasteCR = 72,
		IdCmdEditCLS = 73,
		IdCmdEditCLB = 74,
		IdCmdCtrlOpenTEK = 75,
		IdCmdCtrlCloseTEK = 76,
		IdCmdLineUp = 77,
		IdCmdLineDown = 78,
		IdCmdPageUp = 79,
		IdCmdPageDown = 80,
		IdCmdBuffTop = 81,
		IdCmdBuffBottom = 82,
		IdCmdNextWin = 83,
		IdCmdPrevWin = 84,
		IdCmdNextSWin = 85,
		IdCmdPrevSWin = 86,
		IdCmdLocalEcho = 87,
		IdScrollLock = 88,
		IdUser1 = 89,
		NumOfUDK = IdUDK20 - IdUDK6 + 1,
		NumOfUserKey = 99,
		IdKeyMax = IdUser1 + NumOfUserKey - 1,

		// key code for macro commands
		IdCmdDisconnect = 1000,
		IdCmdLoadKeyMap = 1001,
		IdCmdRestoreSetup = 1002,

		KeyStrMax = 1023,
	}

	// (user) key type IDs
	enum keyTypeIds
	{
		IdBinary = 0,  // transmit text without any modification
		IdText = 1,  // transmit text with new-line & DBCS conversions
		IdMacro = 2,  // activate macro
		IdCommand = 3,  // post a WM_COMMAND message
	}

	class TKeyMap
	{
		public ushort[] Map = new ushort[(int)InternalKeyCodes.IdKeyMax];
		/* user key str position/length in buffer */
		public int[] UserKeyPtr = new int[(int)InternalKeyCodes.NumOfUserKey];
		public int[] UserKeyLen = new int[(int)InternalKeyCodes.NumOfUserKey];
		//public byte[] UserKeyStr = new byte[(int)TeraTermInternalKeyCodes.KeyStrMax + 1];
		public string[] UserKeyStr = new string[(int)InternalKeyCodes.NumOfUserKey];
		/* user key type */
		public byte[] UserKeyType = new byte[(int)InternalKeyCodes.NumOfUserKey];

		internal void CopyTo(TKeyMap dest)
		{
			System.Buffer.BlockCopy(Map, 0, dest.Map, 0, Map.Length);
			System.Buffer.BlockCopy(UserKeyPtr, 0, dest.UserKeyPtr, 0, UserKeyPtr.Length);
			System.Buffer.BlockCopy(UserKeyLen, 0, dest.UserKeyLen, 0, UserKeyLen.Length);
			BlockCopy(UserKeyStr, 0, dest.UserKeyStr, 0, UserKeyStr.Length);
			System.Buffer.BlockCopy(UserKeyType, 0, dest.UserKeyType, 0, UserKeyType.Length);
		}

		private void BlockCopy(string[] src, int srcOffset, string[] dst, int dstOffset, int count)
		{
			for (int i = 0; i < count; i++) {
				dst[dstOffset] = src[srcOffset];
			}
		}
	}

	/* Control Characters */
	enum ControlCharacters
	{
		NUL = 0x00,
		SOH = 0x01,
		STX = 0x02,
		ETX = 0x03,
		EOT = 0x04,
		ENQ = 0x05,
		ACK = 0x06,
		BEL = 0x07,
		BS = 0x08,
		HT = 0x09,
		LF = 0x0A,
		VT = 0x0B,
		FF = 0x0C,
		CR = 0x0D,
		SO = 0x0E,
		SI = 0x0F,
		DLE = 0x10,
		DC1 = 0x11,
		XON = 0x11,
		DC2 = 0x12,
		DC3 = 0x13,
		XOFF = 0x13,
		DC4 = 0x14,
		NAK = 0x15,
		SYN = 0x16,
		ETB = 0x17,
		CAN = 0x18,
		EM = 0x19,
		SUB = 0x1A,
		ESC = 0x1B,
		FS = 0x1C,
		GS = 0x1D,
		RS = 0x1E,
		US = 0x1F,

		SP = 0x20,

		DEL = 0x7F,

		IND = 0x84,
		NEL = 0x85,
		SSA = 0x86,
		ESA = 0x87,
		HTS = 0x88,
		HTJ = 0x89,
		VTS = 0x8A,
		PLD = 0x8B,
		PLU = 0x8C,
		RI = 0x8D,
		SS2 = 0x8E,
		SS3 = 0x8F,
		DCS = 0x90,
		PU1 = 0x91,
		PU2 = 0x92,
		STS = 0x93,
		CCH = 0x94,
		MW = 0x95,
		SPA = 0x96,
		EPA = 0x97,
		SOS = 0x98,

		CSI = 0x9B,
		ST = 0x9C,
		OSC = 0x9D,
		PM = 0x9E,
		APC = 0x9F,
	}

	class TComVar
	{
		public byte[] InBuff = new byte[tttypes.InBuffSize];
		public int InBuffCount, InPtr;
		public byte[] OutBuff = new byte[tttypes.OutBuffSize];
		public int OutBuffCount, OutPtr;

		public IntPtr HWin;
		public bool Ready;
		public bool Open;
		public PortTypeId PortType;
		public short ComPort;
		public uint s; /* SOCKET */
		public short RetryCount;
		public IntPtr ComID;
		public bool CanSend, RRQ;

		public bool SendKanjiFlag;
		public bool EchoKanjiFlag;
		public CharacterSets SendCode;
		public CharacterSets EchoCode;
		public byte SendKanjiFirst;
		public byte EchoKanjiFirst;

		/* from VTSet */
		public Language Language;
		/* from TermSet */
		public NewLineModes CRSend;
		public KanjiCodeId KanjiCodeEcho;
		public bool JIS7KatakanaEcho;
		public KanjiCodeId KanjiCodeSend;
		public bool JIS7KatakanaSend;
		public KanjiInModes KanjiIn;
		public KanjiInModes KanjiOut;
		public short RussHost;
		public short RussClient;
		/* from PortSet */
		public short DelayPerChar;
		public short DelayPerLine;
		public bool TelBinRecv, TelBinSend;

		public bool DelayFlag;
		public bool TelFlag, TelMode;
		public bool IACFlag, TelCRFlag;
		public bool TelCRSend, TelCRSendEcho;
		public bool TelAutoDetect; /* TTPLUG */

		/* Text log */
		public IntPtr HLogBuf;
		public byte[] LogBuf;
		public int LogPtr, LStart, LCount;
		/* Binary log & DDE */
		public IntPtr HBinBuf;
		public byte[] BinBuf;
		public int BinPtr, BStart, BCount, DStart, DCount;
		public int BinSkip;
		public short FilePause;
		public bool ProtoFlag;
		/* message flag */
		public short NoMsg;
#if !NO_INET6
		/* if TRUE, teraterm trys to connect other protocol family */
		public bool RetryWithOtherProtocol;
		public addrinfo res0;
		public addrinfo res;
#endif // NO_INET6
		public char[] Locale;
		public int? CodePage;
		public int? ConnetingTimeout;

		public DateTime LastSendTime;
		public short isSSH;
		public string TitleRemote;

		public byte[] LineModeBuff = new byte[tttypes.OutBuffSize];
		public int LineModeBuffCount, FlushLen;
		public bool Flush;

		public bool TelLineMode;
		public CultureInfo locale;
	}

	enum CommandId
	{
		ID_FILE = 0,
		ID_EDIT = 1,
		ID_SETUP = 2,
		ID_CONTROL = 3,
		ID_HELPMENU = 4,

		ID_WINDOW_1 = 50801,
		ID_WINDOW_WINDOW = 50810,
		ID_TEKWINDOW_WINDOW = 51810,

		ID_TRANSFER = 9, // the position on [File] menu
		ID_SHOWMENUBAR = 995,
	}

	/* shared memory */
	class TMap
	{
		/* Setup information from "teraterm.ini" */
		public TTTSet ts = new TTTSet();
		/* Key code map from "keyboard.def" */
		public TKeyMap km = new TKeyMap();
		// Window list
		public int NWin;
		public IntPtr[] WinList = new IntPtr[tttypes.MAXNWIN];
		/* COM port use flag
		 *           bit 8  7  6  5  4  3  2  1
		 * char[0] : COM 8  7  6  5  4  3  2  1
		 * char[1] : COM16 15 14 13 12 11 10  9 ...
		 */
		public byte[] ComFlag = new byte[(tttypes.MAXCOMPORT - 1) / tttypes.CHAR_BIT + 1];
	}

	class tttypes
	{
		public const int HostNameMaxLength = 1024;
		//HostNameMaxLength = 80;
#if !NO_INET6
		public const int ProtocolFamilyMaxLength = 80;
#endif // NO_INET6

		/* internal tttypes.WM_USER messages */
		public const int WM_USER = 0x0400;
		public const int WM_USER_ACCELCOMMAND = WM_USER + 1;
		public const int WM_USER_CHANGEMENU = WM_USER + 2;
		public const int WM_USER_CLOSEIME = WM_USER + 3;
		public const int WM_USER_COMMNOTIFY = WM_USER + 4;
		public const int WM_USER_COMMOPEN = WM_USER + 5;
		public const int WM_USER_COMMSTART = WM_USER + 6;
		public const int WM_USER_DLGHELP2 = WM_USER + 7;
		public const int WM_USER_GETHOST = WM_USER + 8;
		public const int WM_USER_FTCANCEL = WM_USER + 9;
		public const int WM_USER_PROTOCANCEL = WM_USER + 10;
		public const int WM_USER_CHANGETBAR = WM_USER + 11;
		public const int WM_USER_KEYCODE = WM_USER + 12;
		public const int WM_USER_GETSERIALNO = WM_USER + 13;
		public const int WM_USER_CHANGETITLE = WM_USER + 14;

		public const int WM_USER_DDEREADY = WM_USER + 21;
		public const int WM_USER_DDECMNDEND = WM_USER + 22;
		public const int WM_USER_DDECOMREADY = WM_USER + 23;
		public const int WM_USER_DDEEND = WM_USER + 24;

		public const int MY_FORCE_FOREGROUND_MESSAGE = WM_USER + 31;

		// 横幅の最大値を300から500に変更 (2008.2.15 maya)
		public const int TermWidthMax = 500;
		public const int TermHeightMax = 200;

		public const int InBuffSize = 1024;
		public const int OutBuffSize = 1024;

		public const int MAXNWIN = 256;
		public const int MAXCOMPORT = 4096;
		public const int MAXHOSTLIST = 500;

		readonly string[] BaudList = {
			"110","300","600","1200","2400","4800","9600",
			"14400","19200","38400","57600","115200",
			"230400", "460800", "921600", null
		};

		public const int TitleBuffSize = 50;

		public const string TT_FILEMAPNAME = "ttset_memfilemap_15";

		public const int CHAR_BIT = 8;
		public const int MAX_PATH = 260;
		public const int MAXPATHLEN = 260;
		public const int MAX_UIMSG = 100;
	}
}

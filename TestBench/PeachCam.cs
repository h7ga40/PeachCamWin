using System;
using System.Runtime.InteropServices;

namespace TestBench
{
	internal static class DllImport
	{
		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		public static extern IntPtr LoadLibrary(string lpFileName);

		[DllImport("kernel32")]
		public static extern bool FreeLibrary(IntPtr hModule);

		[DllImport("kernel32", CharSet = CharSet.Ansi)]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		public static extern IntPtr FindResource(IntPtr hModule, int lpName, string lpType);

		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		public static extern int SizeofResource(IntPtr hModule, IntPtr hResInfo);

		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		public static extern IntPtr LockResource(IntPtr hResData);

		public static T GetFunction<T>(IntPtr module, string name) where T : Delegate
		{
			var addr = GetProcAddress(module, name);
			if (addr == IntPtr.Zero) {
				return null;
			}
			return (T)Marshal.GetDelegateForFunctionPointer(addr, typeof(T));
		}
	}

	public class PeachCam : IDisposable
	{
		private IntPtr m_InitModule;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void TSetTestBench(ITestBench testBench);
		private TSetTestBench m_SetTestBench;

		private IntPtr m_Module;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void TStart();
		private TStart m_Start;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void TStop();
		private TStop m_Stop;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void TARGB8888FromARGB4444(IntPtr dst, IntPtr src, int width, int height);
		static TARGB8888FromARGB4444 m_ARGB8888FromARGB4444;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void TARGB8888FromYCBCR422(IntPtr dst, IntPtr src, int width, int height);
		static TARGB8888FromYCBCR422 m_ARGB8888FromYCBCR422;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void TEnumerateDevices();
		static TEnumerateDevices m_EnumerateDevices;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr Tvideoio_VideoCapture_new1();
		static Tvideoio_VideoCapture_new1 m_videoio_VideoCapture_new1;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate bool Tvideoio_VideoCapture_open2(IntPtr ptr, int index);
		static Tvideoio_VideoCapture_open2 m_videoio_VideoCapture_open2;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void Tvideoio_VideoCapture_release(IntPtr ptr);
		static Tvideoio_VideoCapture_release m_videoio_VideoCapture_release;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void Tvideoio_VideoCapture_delete(IntPtr ptr);
		static Tvideoio_VideoCapture_delete m_videoio_VideoCapture_delete;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate bool TVideoCapture_Capture(IntPtr ptr, IntPtr dst, int width, int height);
		static TVideoCapture_Capture m_VideoCapture_Capture;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void TStdin([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]byte[] data, int length);
		static TStdin m_Stdin;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr TOpenMpsse(int index);
		static TOpenMpsse m_OpenMpsse;

		public PeachCam()
		{
			m_InitModule = DllImport.LoadLibrary("TestBenchInit.dll");
			if (m_InitModule == IntPtr.Zero)
				return;

			m_SetTestBench = DllImport.GetFunction<TSetTestBench>(m_InitModule, "SetTestBench");
			m_EnumerateDevices = DllImport.GetFunction<TEnumerateDevices>(m_InitModule, "EnumerateDevices");
			m_OpenMpsse = DllImport.GetFunction<TOpenMpsse>(m_InitModule, "OpenMpsse");

			if (m_SetTestBench == null) {
				var module = m_InitModule;
				m_InitModule = IntPtr.Zero;
				DllImport.FreeLibrary(module);
				return;
			}
		}

		public void Dispose()
		{
			var module = m_InitModule;
			m_InitModule = IntPtr.Zero;
			DllImport.FreeLibrary(module);
		}

		public void SetTestBench(ITestBench testBench)
		{
			m_SetTestBench(testBench);
		}

		public void Load()
		{
			m_Module = DllImport.LoadLibrary("PeachCam.dll");
			if (m_Module == IntPtr.Zero)
				return;

			m_Start = DllImport.GetFunction<TStart>(m_Module, "Start");
			m_Stop = DllImport.GetFunction<TStop>(m_Module, "Stop");
			m_ARGB8888FromARGB4444 = DllImport.GetFunction<TARGB8888FromARGB4444>(m_Module, "ARGB8888FromARGB4444");
			m_ARGB8888FromYCBCR422 = DllImport.GetFunction<TARGB8888FromYCBCR422>(m_Module, "ARGB8888FromYCBCR422");
			m_videoio_VideoCapture_new1 = DllImport.GetFunction<Tvideoio_VideoCapture_new1>(m_Module, "videoio_VideoCapture_new1");
			m_videoio_VideoCapture_open2 = DllImport.GetFunction<Tvideoio_VideoCapture_open2>(m_Module, "videoio_VideoCapture_open2");
			m_videoio_VideoCapture_release = DllImport.GetFunction<Tvideoio_VideoCapture_release>(m_Module, "videoio_VideoCapture_release");
			m_videoio_VideoCapture_delete = DllImport.GetFunction<Tvideoio_VideoCapture_delete>(m_Module, "videoio_VideoCapture_delete");
			m_VideoCapture_Capture = DllImport.GetFunction<TVideoCapture_Capture>(m_Module, "VideoCapture_Capture");
			m_Stdin = DllImport.GetFunction<TStdin>(m_Module, "Stdin");

			if (m_Start == null) {
				var module = m_Module;
				m_Module = IntPtr.Zero;
				DllImport.FreeLibrary(module);
				return;
			}
		}

		public void Start()
		{
			m_Start();
		}

		public static void ARGB8888FromARGB4444(IntPtr dst, IntPtr src, int width, int height)
		{
			m_ARGB8888FromARGB4444(dst, src, width, height);
		}

		public static void ARGB8888FromYCBCR422(IntPtr dst, IntPtr src, int width, int height)
		{
			m_ARGB8888FromYCBCR422(dst, src, width, height);
		}

		public static void EnumerateDevices()
		{
			m_EnumerateDevices();
		}

		public static IntPtr videoio_VideoCapture_new1()
		{
			return m_videoio_VideoCapture_new1();
		}

		public static bool videoio_VideoCapture_open2(IntPtr ptr, int index)
		{
			return m_videoio_VideoCapture_open2(ptr, index);
		}

		public static void videoio_VideoCapture_release(IntPtr ptr)
		{
			m_videoio_VideoCapture_release(ptr);
		}

		public static void videoio_VideoCapture_delete(IntPtr ptr)
		{
			m_videoio_VideoCapture_delete(ptr);
		}

		public static bool VideoCapture_Capture(IntPtr ptr, IntPtr dst, int width, int height)
		{
			return m_VideoCapture_Capture(ptr, dst, width, height);
		}

		internal static void Stdin(byte[] data)
		{
			m_Stdin(data, data.Length);
		}

		public static IntPtr OpenMpsse(int index)
		{
			return m_OpenMpsse(index);
		}
	}
}

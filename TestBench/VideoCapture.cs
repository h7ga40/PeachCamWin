using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBench
{
	public class VideoCapture : IDisposable
	{
		IntPtr _ptr;

		public VideoCapture()
		{
			_ptr = PeachCam.videoio_VideoCapture_new1();
		}

		public void Dispose()
		{
			PeachCam.videoio_VideoCapture_delete(_ptr);
		}

		public bool Open(int index)
		{
			return PeachCam.videoio_VideoCapture_open2(_ptr, index);
		}

		public void Release()
		{
			PeachCam.videoio_VideoCapture_release(_ptr);
		}

		internal bool Capture(IntPtr framebuff, int width, int height)
		{
			return PeachCam.VideoCapture_Capture(_ptr, framebuff, width, height);
		}
	}
}

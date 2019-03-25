using System.Drawing;

namespace TestBench
{
	public class TouchKeyPoint
	{
		byte[] data;
		int offset;

		public TouchKeyPoint(byte[] data, int offset)
		{
			this.data = data;
			this.offset = offset;
		}

		public bool Valid {
			get { return (data[offset] & 0x80) != 0; }
			set { data[offset] = (byte)((data[offset] & ~0x80) | (value ? 0x80 : 0)); }
		}

		public int LocationX {
			get { return ((data[offset] & 0x70) << 4) | data[offset + 1]; }
			set {
				data[offset] = (byte)((data[offset] & ~0x70) | ((value >> 4) & 0x70));
				data[offset + 1] = (byte)(value & 0xFF);
			}
		}

		public int LocationY {
			get { return ((data[offset] & 0x07) << 8) | data[offset + 2]; }
			set {
				data[offset] = (byte)((data[offset] & ~0x07) | ((value >> 8) & 0x07));
				data[offset + 2] = (byte)(value & 0xFF);
			}
		}
	}

	public class TouchKey
	{
		I2C i2c;
		const byte address = 0x55;
		byte[] data = new byte[8];
		TouchKeyPoint point1, point2;

		public TouchKey(I2C i2c)
		{
			this.i2c = i2c;
			point1 = new TouchKeyPoint(data, 2);
			point2 = new TouchKeyPoint(data, 5);
		}

		public bool MouseDown { get; set; }
		public Point MousePos { get; set; }

		public int Fingers {
			get { return data[0] & 0x07; }
			set { data[0] = (byte)((data[0] & ~0x07) | (value & 0x07)); }
		}

		public int Keys {
			get { return data[1]; }
			set { data[1] = (byte)value; }
		}

		void PrepareData()
		{
			int fingers = 0;

			if (MouseDown) {
				point1.Valid = true;
				point1.LocationX = MousePos.X;
				point1.LocationY = MousePos.Y;
				fingers++;
			}
			else {
				point1.Valid = false;
			}

			point2.Valid = false;

			Fingers = fingers;
			Keys = 0; // ???
		}
	}
}

using System;
using System.Text;

namespace TestBench
{
	public class ESP32Driver : IESP32
	{
		private readonly PinName en;
		private readonly PinName io0;
		private readonly PinName tx;
		private readonly PinName rx;
		private readonly bool debug;
		private readonly PinName rts;
		private readonly PinName cts;
		private readonly int baudrate;

		ESP32Socket[] sockets = new ESP32Socket[(int)ESP32.SOCKET_COUNT];

		public ESP32Driver(PinName en, PinName io0, PinName tx, PinName rx, bool debug, PinName rts, PinName cts, int baudrate)
		{
			this.en = en;
			this.io0 = io0;
			this.tx = tx;
			this.rx = rx;
			this.debug = debug;
			this.rts = rts;
			this.cts = cts;
			this.baudrate = baudrate;

			for (int i = 0; i < sockets.Length; i++) {
				sockets[i] = new ESP32Socket(i + 1);
			}
		}

		public int get_free_id()
		{
			for (int i = 0; i < sockets.Length; i++) {
				if (sockets[i].IsActive)
					continue;

				return sockets[i].Id;
			}

			return 0;
		}

		private ESP32Socket getSocket(int id)
		{
			for (int i = 0; i < sockets.Length; i++) {
				if (sockets[i].Id != id)
					continue;

				return sockets[i];
			}
			return null;
		}

		public bool open(string type, int id, string addr, int port, int opt = 0)
		{
			ESP32Socket socket = getSocket(id);
			if ((socket == null) || socket.IsActive)
				return false;
			return socket.open(type, addr, port, opt);
		}

		public bool close(int id, bool accept_id)
		{
			ESP32Socket socket = getSocket(id);
			if ((socket == null) || !socket.IsActive)
				return false;
			return socket.close(accept_id);
		}

		public bool accept(out int p_id)
		{
			p_id = 0;
			return false;
		}

		public bool send(int id, byte[] data, uint len)
		{
			ESP32Socket socket = getSocket(id);
			if ((socket == null) || !socket.IsActive)
				return false;
			return socket.send(data, len);
		}

		public bool recv(int id, byte[] data, uint len)
		{
			ESP32Socket socket = getSocket(id);
			if ((socket == null) || !socket.IsActive)
				return false;
			return socket.recv(data, len);
		}

		public void socket_attach(int id, IntPtr callback, IntPtr data)
		{
			ESP32Socket socket = getSocket(id);
			if ((socket == null) || !socket.IsActive)
				return;
			socket.socket_attach(callback, data);
		}

		public bool socket_setopt_i(int id, string optname, int optval)
		{
			ESP32Socket socket = getSocket(id);
			if ((socket == null) || !socket.IsActive)
				return false;
			return socket.socket_setopt_i(optname, optval);
		}

		public bool socket_setopt_s(int id, string optname, string optval)
		{
			ESP32Socket socket = getSocket(id);
			if ((socket == null) || !socket.IsActive)
				return false;
			return socket.socket_setopt_s(optname, optval);
		}

		public bool socket_getopt_i(int id, string optname, out int optval)
		{
			ESP32Socket socket = getSocket(id);
			if ((socket == null) || !socket.IsActive) {
				optval = 0;
				return false;
			}
			return socket.socket_getopt_i(optname, out optval);
		}

		public bool socket_getopt_s(int id, string optname, StringBuilder optval, int optlen)
		{
			ESP32Socket socket = getSocket(id);
			if ((socket == null) || !socket.IsActive) {
				return false;
			}
			return socket.socket_getopt_s(optname, optval);
		}

		public bool cre_server(ushort portno)
		{
			return true;
		}

		public bool del_server()
		{
			return true;
		}

		public void attach_wifi_status(IntPtr callback)
		{
		}

		public bool dhcp(bool enabled, int mode)
		{
			return true;
		}

		public bool set_network(string ip_address, string netmask, string gateway)
		{
			return true;
		}

		public bool connect(string ap, string passPhrase)
		{
			return true;
		}

		public bool disconnect()
		{
			return true;
		}

		public bool getMACAddress(string buf, int len)
		{
			return true;
		}

		public bool getIPAddress(string buf, int len)
		{
			return true;
		}

		public bool getGateway(string buf, int len)
		{
			return true;
		}

		public bool getNetmask(string buf, int len)
		{
			return true;
		}

		public byte getRSSI()
		{
			return 0;
		}

		public void scan(IntPtr callback, IntPtr usrdata)
		{
		}

		public bool ntp(bool enabled, int timezone, string server0, string server1, string server2)
		{
			return true;
		}

		public long ntp_time()
		{
			return 0;
		}

		public bool esp_time(out int sec, out int usec)
		{
			sec = 0;
			usec = 0;
			return true;
		}

		public int ping(string addr)
		{
			return 0;
		}

		public bool mdns(bool enabled, string hostname, string service, ushort portno)
		{
			return true;
		}

		public bool mdns_query(string hostname, string addr, int len)
		{
			return true;
		}

		public bool sleep(bool enebled)
		{
			return true;
		}
	}

	class ESP32Socket
	{
		public ESP32Socket(int id)
		{
			this.Id = id;
		}

		public bool IsActive { get; private set; }
		public int Id { get; private set; }

		internal bool open(string type, string addr, int port, int opt)
		{
			throw new NotImplementedException();
		}

		internal bool close(bool accept_id)
		{
			throw new NotImplementedException();
		}

		internal bool send(byte[] data, uint len)
		{
			throw new NotImplementedException();
		}

		internal bool recv(byte[] data, uint len)
		{
			throw new NotImplementedException();
		}

		internal void socket_attach(IntPtr callback, IntPtr data)
		{
			throw new NotImplementedException();
		}

		internal bool socket_setopt_i(string optname, int optval)
		{
			throw new NotImplementedException();
		}

		internal bool socket_setopt_s(string optname, string optval)
		{
			throw new NotImplementedException();
		}

		internal bool socket_getopt_i(string optname, out int optval)
		{
			throw new NotImplementedException();
		}

		internal bool socket_getopt_s(string optname, StringBuilder optval)
		{
			throw new NotImplementedException();
		}
	}
}

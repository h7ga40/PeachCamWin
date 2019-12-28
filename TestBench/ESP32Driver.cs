#define DEVICE_SERIAL_FC
/* ESP32 Example
 * Copyright (c) 2015 ARM Limited
 * Copyright (c) 2017 Renesas Electronics Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TestBench
{
	public class WiFiAccessPoint
	{
		private nsapi_wifi_ap ap;

		public WiFiAccessPoint(nsapi_wifi_ap ap)
		{
			this.ap = ap;
		}
	}

	public class WifiStatusEventArgs : EventArgs
	{
		public WifiStatusEventArgs(byte status)
		{
			Status = status;
		}

		public byte Status { get; set; }
	}

	public class ESP32Driver : IESP32
	{
		public const int NSAPI_ERROR_DEVICE_ERROR = -3012;

		public const int ESP32_CONNECT_TIMEOUT = 15000;
		public const int ESP32_RECV_TIMEOUT = 2000;
		public const int ESP32_MISC_TIMEOUT = 2000;

		public const byte WIFIMODE_STATION = 1;
		public const byte WIFIMODE_SOFTAP = 2;
		public const byte WIFIMODE_STATION_SOFTAP = 3;
		public const byte SOCKET_COUNT = 5;

		public const byte STATUS_DISCONNECTED = 0;
		public const byte STATUS_CONNECTED = 1;
		public const byte STATUS_GOT_IP = 2;

		private const int ESP32_DEFAULT_BAUD_RATE = 115200;
		private const int ESP32_ALL_SOCKET_IDS = -1;

		private readonly ITestBench testBench;
		private readonly Gpio _p_wifi_en;
		private readonly Gpio _p_wifi_io0;
		private bool init_end;
		private readonly Serial _serial;
		private ATCmdParser _parser;
		private int _wifi_mode;
		private int _baudrate;
		private PinName _rts;
		private PinName _cts;
		private FlowControl _flow_control;
		private int last_timeout_ms;
		private List<int> _accept_id = new List<int>();
		private uint _id_bits;
		private uint _id_bits_close;
		private bool _server_act;
		private readonly object _smutex; // Protect serial port access
		private byte _wifi_status;
		private IntPtr _wifi_status_self;
		private WifiStatusCallback _wifi_status_cb;
		private readonly bool[] _ids = new bool[SOCKET_COUNT];

		private struct TCBS
		{
			public SocketAttachCallback callback;
			public IntPtr data;
			public int Notified;
		}

		private readonly TCBS[] _cbs = new TCBS[SOCKET_COUNT];

		public ESP32Driver(ITestBench testBench, Gpio p_wifi_en, Gpio p_wifi_io0, Serial serial, bool debug)
		{
			this.testBench = testBench;
			_p_wifi_en = p_wifi_en;
			_p_wifi_io0 = p_wifi_io0;
			_serial = serial;
			_baudrate = serial.baudrate;
			_rts = serial.rts;
			_cts = serial.cts;
			_flow_control = serial.flow_control;
			_smutex = new object();

			_parser = new ATCmdParser(testBench, serial, "\r\n");
			_parser.oob("+IPD", new EventHandler(_packet_handler));
			_parser.oob("0,CONNECT", new EventHandler(_connect_handler_0));
			_parser.oob("1,CONNECT", new EventHandler(_connect_handler_1));
			_parser.oob("2,CONNECT", new EventHandler(_connect_handler_2));
			_parser.oob("3,CONNECT", new EventHandler(_connect_handler_3));
			_parser.oob("4,CONNECT", new EventHandler(_connect_handler_4));
			_parser.oob("0,CLOSED", new EventHandler(_closed_handler_0));
			_parser.oob("1,CLOSED", new EventHandler(_closed_handler_1));
			_parser.oob("2,CLOSED", new EventHandler(_closed_handler_2));
			_parser.oob("3,CLOSED", new EventHandler(_closed_handler_3));
			_parser.oob("4,CLOSED", new EventHandler(_closed_handler_4));
			_parser.oob("WIFI ", new EventHandler(_connection_status_handler));
		}

		public void debugOn(bool debug)
		{
			_parser.debug_on(debug);
		}

		public int get_firmware_version()
		{
			var result = new List<string>();
			bool done;

			lock (_smutex) {
				startup();
				done = _parser.send("AT+GMR")
						 && _parser.recv(@"SDK version:([0-9]+)", result)
						 && _parser.recv(@"OK");
			}

			if (done) {
				return Int32.Parse(result[0]);
			}
			else {
				// Older firmware versions do not prefix the version with "SDK version: "
				return -1;
			}
		}

		public bool startup()
		{
			if (init_end) {
				return true;
			}

			if (_p_wifi_io0 != null) {
				_p_wifi_io0.Value = true;
			}
			if (_p_wifi_en != null) {
				_p_wifi_en.Value = false;
				Thread.Sleep(10);
				_p_wifi_en.Value = true;
				_parser.recv(@"ready");
			}
			else {
				setTimeout(100);
				_parser.recv(@"ready");
			}

			reset();
			var success = _parser.send("AT+CWMODE={0}", _wifi_mode)
						&& _parser.recv(@"OK")
						&& _parser.send("AT+CIPMUX=1")
						&& _parser.recv(@"OK")
						&& _parser.send("AT+CWAUTOCONN=0")
						&& _parser.recv(@"OK")
						&& _parser.send("AT+CWQAP")
						&& _parser.recv(@"OK");
			if (success) {
				init_end = true;
			}

			return success;
		}

		public bool restart()
		{
			bool success;

			lock (_smutex) {
				if (!init_end) {
					success = startup();
				}
				else {
					reset();
					success = _parser.send("AT+CWMODE={0}", _wifi_mode)
						   && _parser.recv(@"OK")
						   && _parser.send("AT+CIPMUX=1")
						   && _parser.recv(@"OK");
				}
			}

			return success;
		}

		public bool set_mode(int mode)
		{
			//only 3 valid modes
			if (mode < 1 || mode > 3) {
				return false;
			}
			if (_wifi_mode != mode) {
				_wifi_mode = mode;
				return restart();
			}
			return true;
		}

		public bool cre_server(ushort port)
		{
			if (_server_act) {
				return false;
			}
			lock (_smutex) {
				startup();
				if (!(_parser.send("AT+CIPSERVER=1,{0}", port)
					&& _parser.recv(@"OK"))) {
					return false;
				}
				_server_act = true;
			}
			return true;
		}

		public bool del_server()
		{
			lock (_smutex) {
				startup();
				if (!(_parser.send("AT+CIPSERVER=0")
					&& _parser.recv(@"OK"))) {
					return false;
				}
				_server_act = false;
			}
			return true;
		}

		public void socket_handler(bool connect, int id)
		{
			_cbs[id].Notified = 0;
			if (connect) {
				_id_bits |= (1u << id);
				if (_server_act) {
					_accept_id.Add(id);
				}
			}
			else {
				_id_bits &= ~(1u << id);
				_id_bits_close |= (1u << id);
				if (_server_act) {
					for (var i = 0; i < _accept_id.Count; i++) {
						if (id == _accept_id[i]) {
							_accept_id.RemoveAt(i);
						}
					}
				}
			}
		}

		public bool accept(out int p_id)
		{
			var ret = false;
			p_id = -1;

			while (!ret) {
				if (!_server_act) {
					break;
				}

				lock (_smutex) {
					startup();
					if (_accept_id.Count > 0) {
						ret = true;
					}
					else {
						_parser.process_oob(); // Poll for inbound packets
						if (_accept_id.Count > 0) {
							ret = true;
						}
					}
					if (ret) {
						p_id = _accept_id[0];
						_accept_id.RemoveAt(0);
					}
				}
				if (!ret) {
					Thread.Sleep(5);
				}
			}

			if (ret) {
				for (var i = 0; i < 50; i++) {
					if ((_id_bits_close & (1 << p_id)) == 0) {
						break;
					}
					Thread.Sleep(10);
				}
			}

			return ret;
		}

		public bool reset()
		{
			for (var i = 0; i < 2; i++) {
				if (_parser.send("AT+RST")
					&& _parser.recv(@"OK")) {
					_serial.SetBaudRate(ESP32_DEFAULT_BAUD_RATE);
#if DEVICE_SERIAL_FC
					_serial.SetFlowControl(FlowControl.FlowControlNone);
#endif
					_parser.recv(@"ready");
					_clear_socket_packets(ESP32_ALL_SOCKET_IDS);

					if (_parser.send("AT+UART_CUR={0},8,1,0,{1}", _baudrate, _flow_control)
						&& _parser.recv(@"OK")) {
						_serial.SetBaudRate(_baudrate);
#if DEVICE_SERIAL_FC
						switch (_flow_control) {
						case FlowControl.FlowControlRTS:
							_serial.SetFlowControl(FlowControl.FlowControlRTS, _serial.rts);
							break;
						case FlowControl.FlowControlCTS:
							_serial.SetFlowControl(FlowControl.FlowControlCTS, _serial.cts);
							break;
						case FlowControl.FlowControlRTSCTS:
							_serial.SetFlowControl(FlowControl.FlowControlRTSCTS, _serial.rts, _serial.cts);
							break;
						case FlowControl.FlowControlNone:
						default:
							// do nothing
							break;
						}
#endif
					}

					return true;
				}
			}

			return false;
		}

		public bool dhcp(bool enabled, int mode)
		{
			//only 3 valid modes
			if (mode < 0 || mode > 2) {
				return false;
			}

			bool done;
			lock (_smutex) {
				startup();
				done = _parser.send("AT+CWDHCP={0},{1}", enabled ? 1 : 0, mode)
					&& _parser.recv(@"OK");
			}

			return done;
		}

		public bool connect(string ap, string passPhrase)
		{
			bool ret;

			_wifi_status = STATUS_DISCONNECTED;

			lock (_smutex) {
				startup();

				setTimeout(ESP32_CONNECT_TIMEOUT);
				ret = _parser.send("AT+CWJAP=\"{0}\",\"{1}\"", ap, passPhrase)
					&& _parser.recv(@"OK");
				setTimeout();
			}
			return ret;
		}

		public bool config_soft_ap(string ap, string passPhrase, byte chl, byte ecn)
		{
			bool ret;

			lock (_smutex) {
				startup();
				ret = _parser.send("AT+CWSAP=\"{0}\",\"{1}\",{2},{3}", ap, passPhrase, chl, ecn)
					&& _parser.recv(@"OK");
			}
			return ret;
		}

		public bool get_ssid(out string ap)
		{
			var result = new List<string>();
			bool ret;

			lock (_smutex) {
				startup();
				ret = _parser.send("AT+CWJAP?")
					&& _parser.recv(@"+CWJAP:""([^""]+)"",", result)
					&& _parser.recv(@"OK");
			}
			if (!ret) {
				ap = null;
				return false;
			}

			ap = (string)result[0];
			return true;
		}

		public bool disconnect()
		{
			bool ret;

			lock (_smutex) {
				startup();
				ret = _parser.send("AT+CWQAP") && _parser.recv(@"OK");
			}
			return ret;
		}

		public bool getIPAddress(byte[] buf, int len)
		{
			var result = new List<string>();
			bool ret;

			lock (_smutex) {
				startup();
				ret = _parser.send("AT+CIFSR")
					&& _parser.recv(@"+CIFSR:STAIP,""([^""]+)""", result)
					&& _parser.recv(@"OK");
			}

			if (!ret)
				return false;

			var addr = Encoding.UTF8.GetBytes((string)result[0]);
			Buffer.BlockCopy(addr, 0, buf, 0, addr.Length);

			return true;
		}

		public bool getIPAddress_ap(byte[] buf, int len)
		{
			var result = new List<string>();
			bool ret;

			lock (_smutex) {
				startup();
				ret = _parser.send("AT+CIFSR")
				   && _parser.recv(@"+CIFSR:APIP,""([^""]+)""", result)
				   && _parser.recv(@"OK");
			}

			if (!ret)
				return false;

			var addr = Encoding.UTF8.GetBytes((string)result[0]);
			Buffer.BlockCopy(addr, 0, buf, 0, addr.Length);

			return true;
		}

		public bool getMACAddress(byte[] buf, int len)
		{
			var result = new List<string>();
			bool ret;

			lock (_smutex) {
				startup();
				ret = _parser.send("AT+CIFSR")
					&& _parser.recv(@"+CIFSR:STAMAC,""([^""]+)""", result)
					&& _parser.recv(@"OK");
			}

			if (!ret)
				return false;

			var addr = Encoding.UTF8.GetBytes((string)result[0]);
			Buffer.BlockCopy(addr, 0, buf, 0, addr.Length);

			return true;
		}

		public bool getMACAddress_ap(byte[] buf, int len)
		{
			var result = new List<string>();
			bool ret;

			lock (_smutex) {
				startup();
				ret = _parser.send("AT+CIFSR")
				   && _parser.recv(@"+CIFSR:APMAC,""([^""]+)""", result)
				   && _parser.recv(@"OK");
			}

			if (!ret)
				return false;

			var addr = Encoding.UTF8.GetBytes((string)result[0]);
			Buffer.BlockCopy(addr, 0, buf, 0, addr.Length);

			return true;
		}

		public bool getGateway(byte[] buf, int len)
		{
			var result = new List<string>();
			bool ret;

			lock (_smutex) {
				startup();
				ret = _parser.send("AT+CIPSTA?")
					&& _parser.recv(@"+CIPSTA:gateway:""([^""]+)""", result)
					&& _parser.recv(@"OK");
			}

			if (!ret)
				return false;

			var addr = Encoding.UTF8.GetBytes((string)result[0]);
			Buffer.BlockCopy(addr, 0, buf, 0, addr.Length);

			return true;
		}

		public bool getGateway_ap(byte[] buf, int len)
		{
			var result = new List<string>();
			bool ret;

			lock (_smutex) {
				startup();
				ret = _parser.send("AT+CIPAP?")
				   && _parser.recv(@"+CIPAP:gateway:""([^""]+)""", result)
				   && _parser.recv(@"OK");
			}

			if (!ret)
				return false;

			var addr = Encoding.UTF8.GetBytes((string)result[0]);
			Buffer.BlockCopy(addr, 0, buf, 0, addr.Length);

			return true;
		}

		public bool getNetmask(byte[] buf, int len)
		{
			var result = new List<string>();
			bool ret;

			lock (_smutex) {
				startup();
				ret = _parser.send("AT+CIPSTA?")
					&& _parser.recv(@"+CIPSTA:netmask:""([^""]+)""", result)
					&& _parser.recv(@"OK");
			}

			if (!ret)
				return false;

			var addr = Encoding.UTF8.GetBytes((string)result[0]);
			Buffer.BlockCopy(addr, 0, buf, 0, addr.Length);

			return true;
		}

		public bool getNetmask_ap(byte[] buf, int len)
		{
			var result = new List<string>();
			bool ret;

			lock (_smutex) {
				startup();
				ret = _parser.send("AT+CIPAP?")
					&& _parser.recv(@"+CIPAP:netmask:""([^""]+)""", result)
					&& _parser.recv(@"OK");
			}

			if (!ret)
				return false;

			var addr = Encoding.UTF8.GetBytes((string)result[0]);
			Buffer.BlockCopy(addr, 0, buf, 0, addr.Length);

			return true;
		}

		public byte getRSSI()
		{
			var result = new List<string>();
			bool ret;
			byte channel, rssi;
			string ssid;
			string bssid;

			lock (_smutex) {
				startup();
				ret = _parser.send("AT+CWJAP?")
					&& _parser.recv(@"+CWJAP:""([^""]+)"",""([^""]+)"",([0-9]+),([0-9]+)\r\nOK", result);
			}

			if (!ret) {
				return 0;
			}

			ssid = (string)result[0];
			bssid = (string)result[1];
			channel = Byte.Parse(result[2]);
			rssi = Byte.Parse(result[3]);

			return rssi;
		}

		public int scan(nsapi_wifi_ap[] res, int limit)
		{
			var cnt = 0;

			if (!init_end) {
				lock (_smutex) {
					startup();
				}
				Thread.Sleep(1500);
			}

			lock (_smutex) {
				setTimeout(5000);
				if (!_parser.send("AT+CWLAP")) {
					return NSAPI_ERROR_DEVICE_ERROR;
				}

				nsapi_wifi_ap ap = new nsapi_wifi_ap();
				while (recv_ap(ref ap)) {
					if (cnt < limit) {
						res[cnt] = ap;
					}

					cnt++;
					if ((limit != 0) && (cnt >= limit)) {
						break;
					}
					setTimeout(500);
				}
				setTimeout(10);
				_parser.recv(@"OK");
				setTimeout();
			}

			return cnt;
		}

		public bool isConnected()
		{
			byte[] addr = new byte[64];
			return getIPAddress(addr, addr.Length);
		}

		public bool open(string type, int id, string addr, int port, int opt)
		{
			bool ret;

			if (id >= SOCKET_COUNT) {
				return false;
			}
			_cbs[id].Notified = 0;

			lock (_smutex) {
				startup();
				setTimeout(500);
				if (opt != 0) {
					ret = _parser.send("AT+CIPSTART=%d,\"{0}\",\"{1}\",{2}, {3}", id, type, addr, port, opt)
					   && _parser.recv(@"OK");
				}
				else {
					ret = _parser.send("AT+CIPSTART=%d,\"{0}\",\"{1}\",{2}", id, type, addr, port)
					   && _parser.recv(@"OK");
				}
				setTimeout();
				_clear_socket_packets(id);
			}

			return ret;
		}

		public bool send(int id, byte[] data, int amount)
		{
			int send_size;
			bool ret;
			var error_cnt = 0;
			var index = 0;

			_cbs[id].Notified = 0;
			if (amount == 0) {
				return true;
			}

			//May take a second try if device is busy
			lock (_smutex) {
				while (error_cnt < 2) {
					if (((_id_bits & (1 << id)) == 0)
					 || ((_id_bits_close & (1 << id)) != 0)) {
						return false;
					}
					send_size = amount;
					if (send_size > 2048) {
						send_size = 2048;
					}
					startup();
					ret = _parser.send("AT+CIPSEND={0},{1}", id, send_size)
							   && _parser.recv(@">")
							   && (_parser.write(data, index, send_size) >= 0)
							   && _parser.recv(@"SEND OK");
					if (ret) {
						amount -= send_size;
						index += send_size;
						error_cnt = 0;
						if (amount == 0) {
							return true;
						}
					}
					else {
						error_cnt++;
					}
				}
			}

			return false;
		}

		public class packet
		{
			public int id;
			public int index;
			public int len;
			public packet next;
			public byte[] data;

			public packet(int amount)
			{
				data = new byte[amount];
			}

			public void memcpy(int offset, byte[] dst, int dst_ofs, int len)
			{
				Buffer.BlockCopy(data, offset, dst, dst_ofs, len);
			}
		}

		public readonly List<packet> _packets = new List<packet>();
		public packet _packets_end;

		public void _packet_handler(object sender, EventArgs e)
		{
			var result = new List<string>();
			int id;
			int amount;
			int tmp_timeout;

			// parse out the packet
			if (!_parser.recv(@",([0-9]+),([0-9]+):", result)) {
				return;
			}
			id = Int32.Parse(result[0]);
			amount = Int32.Parse(result[1]);

			var packet = new packet(amount);
			if (packet == null) {
				return;
			}

			packet.id = id;
			packet.len = amount;
			packet.next = null;
			packet.index = 0;

			tmp_timeout = last_timeout_ms;
			setTimeout(500);
			if (_parser.read(packet.data, 0, amount) == 0) {
				setTimeout(tmp_timeout);
				return;
			}

			// append to packet list
			_packets.Add(packet);
		}

		public int recv(int id, byte[] data, int amount, int timeout)
		{
			packet p;
			uint retry_cnt = 0;
			var idx = 0;

			_cbs[id].Notified = 0;

			while (true) {
				lock (_smutex) {
					if (_serial.rts == PinName.NC) {
						setTimeout(1);
						while (_parser.process_oob()) ; // Poll for inbound packets
						setTimeout();
					}
					else if ((retry_cnt != 0) || (_packets == null)) {
						setTimeout(1);
						_parser.process_oob(); // Poll for inbound packets
						setTimeout();
					}
					else {
						// do nothing
					}

					// check if any packets are ready for us
					for (var i = 0; i < _packets.Count; i++) {
						p = _packets[i];

						if (p.id == id) {
							var q = p;

							if (q.len <= amount) { // Return and remove full packet
								q.memcpy(q.index, data, idx, q.len);
								if (_packets_end == p.next) {
									_packets_end = p;
								}
								p = p.next;
								idx += q.len;
								amount -= q.len;
							}
							else { // return only partial packet
								q.memcpy(q.index, data, idx, amount);
								q.len -= amount;
								q.index += amount;
								idx += amount;
								break;
							}
						}
						else {
							p = p.next;
						}
					}
					if (idx > 0) {
						return idx;
					}
					if (retry_cnt >= timeout) {
						if (((_id_bits & (1 << id)) == 0)
							|| ((_id_bits_close & (1 << id)) != 0)) {
							return -2;
						}
						else {
							return -1;
						}
					}
					retry_cnt++;
				}
				Thread.Sleep(1);
			}
		}

		public void _clear_socket_packets(int id)
		{
			var i = 0;
			var p = _packets[i];

			while (p != null) {
				if (p.id == id || id == ESP32_ALL_SOCKET_IDS) {
					var q = p;

					if (_packets_end == p.next) {
						_packets_end = p; // Set last packet next field/_packets
					}

					p = p.next;
				}
				else {
					// Point to last packet next field
					p = p.next;
				}
			}
		}

		public bool close(int id, bool wait_close)
		{
			if (wait_close) {
				lock (_smutex) {
					for (var j = 0; j < 2; j++) {
						if (((_id_bits & (1 << id)) == 0)
							|| ((_id_bits_close & (1u << id)) != 0)) {
							_id_bits_close &= ~(1u << id);
							_ids[id] = false;
							_clear_socket_packets(id);
							return true;
						}
						startup();
						setTimeout(500);
						_parser.process_oob(); // Poll for inbound packets
						setTimeout();
					}
				}
			}

			//May take a second try if device is busy
			for (var i = 0; i < 2; i++) {
				lock (_smutex) {
					if ((_id_bits & (1 << id)) == 0) {
						_id_bits_close &= ~(1u << id);
						_ids[id] = false;
						_clear_socket_packets(id);
						return true;
					}
					startup();
					setTimeout(500);
					if (_parser.send("AT+CIPCLOSE={0}", id)
						&& _parser.recv(@"OK")) {
						setTimeout();
						_clear_socket_packets(id);
						_id_bits_close &= ~(1u << id);
						_ids[id] = false;
						return true;
					}
					setTimeout();
				}
			}

			_ids[id] = false;
			return false;
		}

		public void setTimeout(int timeout_ms = -1)
		{
			last_timeout_ms = timeout_ms;
			_parser.set_timeout(timeout_ms);
		}

		public bool readable()
		{
			return _serial.IsReadable();
		}

		public bool writeable()
		{
			return _serial.IsWritable();
		}

		public void socket_attach(int id, SocketAttachCallback callback, IntPtr data)
		{
			_cbs[id].callback = callback;
			_cbs[id].data = data;
			_cbs[id].Notified = 0;
		}

		public bool recv_ap(ref nsapi_wifi_ap ap)
		{
			var result = new List<string>();
			int sec;
			var ret = _parser.recv(@"+CWLAP:\(([0-9]+),""([^""]+)"",([0-9]+),""([0-9a-fA-F]+):([0-9a-fA-F]+):([0-9a-fA-F]+):([0-9a-fA-F]+):([0-9a-fA-F]+):([0-9a-fA-F]+)"",([0-9]+)\)", result);

			sec = Int32.Parse(result[0]);
			//ap.security = sec < 5 ? (nsapi_security_t)sec : NSAPI_SECURITY_UNKNOWN;

			return ret;
		}

		public void _connect_handler_0(object sender, EventArgs e)
		{
			socket_handler(true, 0);
		}

		public void _connect_handler_1(object sender, EventArgs e)
		{
			socket_handler(true, 1);
		}

		public void _connect_handler_2(object sender, EventArgs e)
		{
			socket_handler(true, 2);
		}

		public void _connect_handler_3(object sender, EventArgs e)
		{
			socket_handler(true, 3);
		}

		public void _connect_handler_4(object sender, EventArgs e)
		{
			socket_handler(true, 4);
		}

		public void _closed_handler_0(object sender, EventArgs e)
		{
			socket_handler(false, 0);
		}

		public void _closed_handler_1(object sender, EventArgs e)
		{
			socket_handler(false, 1);
		}

		public void _closed_handler_2(object sender, EventArgs e)
		{
			socket_handler(false, 2);
		}

		public void _closed_handler_3(object sender, EventArgs e)
		{
			socket_handler(false, 3);
		}

		public void _closed_handler_4(object sender, EventArgs e)
		{
			socket_handler(false, 4);
		}

		public void _connection_status_handler(object sender, EventArgs e)
		{
			var result = new List<string>();
			string status;
			if (_parser.recv(@"([^""]+)\n", result)) {
				status = (string)result[0];

				if (status.CompareTo("CONNECTED\n") == 0) {
					_wifi_status = STATUS_CONNECTED;
				}
				else if (status.CompareTo("GOT IP\n") == 0) {
					_wifi_status = STATUS_GOT_IP;
				}
				else if (status.CompareTo("DISCONNECT\n") == 0) {
					_wifi_status = STATUS_DISCONNECTED;
				}
				else {
					return;
				}

				if (_wifi_status_cb != null) {
					_wifi_status_cb(_wifi_status_self, _wifi_status);
				}
			}
		}

		public int get_free_id()
		{
			// Look for an unused socket
			var id = -1;

			for (var i = 0; i < SOCKET_COUNT; i++) {
				if ((!_ids[i]) && ((_id_bits & (1 << i)) == 0)) {
					id = i;
					_ids[i] = true;
					break;
				}
			}

			return id;
		}

		public void @event()
		{
			for (var i = 0; i < SOCKET_COUNT; i++) {
				if ((_cbs[i].callback != null) && (_cbs[i].Notified == 0)) {
					_cbs[i].callback(_cbs[i].data);
					_cbs[i].Notified = 1;
				}
			}
		}

		public bool set_network(string ip_address, string netmask, string gateway)
		{
			bool ret;

			if (ip_address == null) {
				return false;
			}

			lock (_smutex) {
				if ((netmask != null) && (gateway != null)) {
					ret = _parser.send("AT+CIPSTA=\"{0}\",\"{1}\",\"{2}\"", ip_address, gateway, netmask)
					   && _parser.recv(@"OK");
				}
				else {
					ret = _parser.send("AT+CIPSTA=\"{0}\"", ip_address)
					   && _parser.recv(@"OK");
				}
			}

			return ret;
		}

		public bool set_network_ap(string ip_address, string netmask, string gateway)
		{
			bool ret;

			if (ip_address == null) {
				return false;
			}

			lock (_smutex) {
				if ((netmask != null) && (gateway != null)) {
					ret = _parser.send("AT+CIPAP=\"{0}\",\"{1}\",\"{2}\"", ip_address, gateway, netmask)
					   && _parser.recv(@"OK");
				}
				else {
					ret = _parser.send("AT+CIPAP=\"{0}\"", ip_address)
					   && _parser.recv(@"OK");
				}
			}

			return ret;
		}

		public void attach_wifi_status(IntPtr status_self, WifiStatusCallback status_cb)
		{
			_wifi_status_self = status_self;
			_wifi_status_cb = status_cb;
		}

		public byte get_wifi_status()
		{
			return _wifi_status;
		}

		public bool socket_setopt_i(int id, string optname, int optval)
		{
			bool ret;

			if (optname == null) {
				return false;
			}

			lock (_smutex) {
				ret = _parser.send("AT+CIPSETOPT=%d,\"{0}\",{1}", id, optname, optval)
					&& _parser.recv(@"OK");
			}

			return ret;
		}

		public bool socket_setopt_s(int id, string optname, string optval)
		{
			bool ret;

			if (optname == null) {
				return false;
			}

			lock (_smutex) {
				ret = _parser.send("AT+CIPSETOPT=%d,\"{0}\",{1}", id, optname, optval)
					&& _parser.recv(@"OK");
			}

			return ret;
		}

		public bool socket_getopt_i(int id, string optname, out int optval)
		{
			var result = new List<string>();
			bool ret;

			if ((optname == null) /*|| (optval == null)*/) {
				optval = 0;
				return false;
			}

			lock (_smutex) {
				ret = _parser.send("AT+CIPGETOPT={0},\"{1}\"", id, optname)
					&& _parser.recv(@"+CIPGETOPT:([0-9]+)\r\nOK", result);
			}

			if (!ret)
				optval = 0;
			else
				optval = Int32.Parse(result[0]);

			return ret;
		}

		public bool socket_getopt_s(int id, string optname, StringBuilder optval, int optlen)
		{
			var result = new List<string>();
			bool ret;

			if ((optname == null) /*|| (optval == null)*/) {
				return false;
			}

			lock (_smutex) {
				ret = _parser.send("AT+CIPGETOPT={0},\"{1}\"", id, optname)
					&& _parser.recv(@"+CIPGETOPT:([0-9]+)\r\nOK", result);
			}

			if (!ret)
				return false;

			optval.Append((string)result[0]);

			return ret;
		}

		public bool ntp(bool enabled, int timezone, string server0, string server1, string server2)
		{
			bool ret;

			lock (_smutex) {
				if (server0 != null) {
					if (server1 != null) {
						if (server2 != null) {
							ret = _parser.send("AT+CIPSNTPCFG={0},{1},\"{2}\",\"{3}\",\"{4}\"", enabled ? 1 : 0, timezone, server0, server1, server2)
								&& _parser.recv(@"OK");
						}
						else {
							ret = _parser.send("AT+CIPSNTPCFG={0},{1},\"{2}\",\"{3}\"", enabled ? 1 : 0, timezone, server0, server1)
								&& _parser.recv(@"OK");
						}
					}
					else {
						ret = _parser.send("AT+CIPSNTPCFG={0},{1},\"{2}\"", enabled ? 1 : 0, timezone, server0)
							&& _parser.recv(@"OK");
					}
				}
				else {
					ret = _parser.send("AT+CIPSNTPCFG={0},{1}", enabled ? 1 : 0, timezone)
						&& _parser.recv(@"OK");
				}
			}

			return ret;
		}

		public long ntp_time()
		{
			var result = new List<string>();
			bool ret;
			string mon, week;

			lock (_smutex) {
				ret = _parser.send("AT+CIPSNTPTIME?")
				   && _parser.recv(@"+CIPSNTPTIME:(\w+) (\w+) ([0-9]+) ([0-9]+):([0-9]+):([0-9]+) ([0-9]+)\r\nOK", result);
			}

			if (!ret) {
				return -1;
			}

			mon = (string)result[0];
			week = (string)result[1];
			var tm_mday = Int32.Parse(result[2]);
			var tm_hour = Int32.Parse(result[3]);
			var tm_min = Int32.Parse(result[4]);
			var tm_sec = Int32.Parse(result[5]);
			var tm_year = Int32.Parse(result[6]);
			var tm_mon = 0;
			switch (new string(new char[] { mon[0], mon[1], mon[2] })) {
			case "Jan": tm_mon = 0; break;
			case "Feb": tm_mon = 1; break;
			case "Mar": tm_mon = 2; break;
			case "Apr": tm_mon = 3; break;
			case "May": tm_mon = 4; break;
			case "Jun": tm_mon = 5; break;
			case "Jul": tm_mon = 6; break;
			case "Aug": tm_mon = 7; break;
			case "Sep": tm_mon = 8; break;
			case "Oct": tm_mon = 9; break;
			case "Nov": tm_mon = 10; break;
			case "Dec": tm_mon = 11; break;
			}

			return new DateTime(tm_year, tm_mon, tm_mday, tm_hour, tm_min, tm_sec).Ticks;
		}

		public bool esp_time(out int sec, out int usec)
		{
			var result = new List<string>();
			bool ret;

			lock (_smutex) {
				ret = _parser.send("AT+SYSTIME?")
					&& _parser.recv(@"+SYSTIME:([0-9]+)\.([0-9]+)\r\nOK", result);
			}

			sec = Int32.Parse(result[0]);
			usec = Int32.Parse(result[1]);

			return ret;
		}

		public int ping(string addr)
		{
			var result = new List<string>();
			bool ret;

			if (addr == null) {
				return 0;
			}

			lock (_smutex) {
				ret = _parser.send("AT+PING=\"{0}\"", addr)
					&& _parser.recv(@"+PING:([0-9]+)\r\nOK", result);
			}

			if (!ret)
				return -1;

			return Int32.Parse(result[0]);
		}

		public bool mdns(bool enabled, string hostname, string service, ushort portno)
		{
			bool ret;

			if ((hostname == null) != (service == null)) {
				return false;
			}

			lock (_smutex) {
				if (enabled) {
					if (hostname == null) {
						ret = _parser.send("AT+MDNS=1")
						   && _parser.recv(@"OK");
					}
					else {
						ret = _parser.send("AT+MDNS=1,\"{0}\",\"{1}\",{2}", hostname, service, portno)
						   && _parser.recv(@"OK");
					}
				}
				else {
					ret = _parser.send("AT+MDNS=0")
					   && _parser.recv(@"OK");
				}
			}

			return ret;
		}

		public bool mdns_query(string hostname, byte[] addr, int len)
		{
			var result = new List<string>();
			bool ret;

			if (hostname == null) {
				return false;
			}

			lock (_smutex) {
				ret = _parser.send("AT+CIPQRYHST=\"{0}\"", hostname)
					&& _parser.recv(@"+CIPQRYHST:""([^""]+)""", result)
					&& _parser.recv(@"OK");
			}

			if (!ret)
				return false;

			var s = Encoding.UTF8.GetBytes((string)result[0]);
			Buffer.BlockCopy(s, 0, addr, 0, s.Length);

			return true;
		}

		public bool sleep(bool enebled)
		{
			var result = new List<string>();
			bool ret;

			lock (_smutex) {
				ret = _parser.send("AT+SLEEP={0}", enebled ? 1 : 0)
					&& _parser.recv(@"OK", result);
			}

			if (!ret)
				return false;

			return Int32.Parse(result[0]) != 0;
		}
	}
}

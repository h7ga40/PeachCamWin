/* Copyright (c) 2017 ARM Limited
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
 *
 * @section DESCRIPTION
 *
 * Parser for the AT command syntax
 *
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TestBench
{
	internal class ATCmdParser : IDisposable
	{
		private struct TOOB
		{
			public readonly byte[] prefix;
			public readonly EventHandler cb;

			public TOOB(string prefix, EventHandler cb)
			{
				this.prefix = Encoding.UTF8.GetBytes(prefix);
				this.cb = cb;
			}
		}

		private const int LF = 10;
		private const int CR = 13;
		private readonly ITestBench testBench;
		private Serial serial;
		private byte[] _output_delimiter;
		private int _in_prev;
		private int _dbg_on;
		private bool _aborted;
		private readonly Queue<TOOB> oobs = new Queue<TOOB>();
		private readonly Encoder encoder = Encoding.UTF8.GetEncoder();
		private readonly byte[] _buffer;

		public ATCmdParser(ITestBench testBench, Serial serial,
			string output_delimiter, int buffer_size = 256, int timeout = 8000,
			bool debug = false)
		{
			this.testBench = testBench;
			this.serial = serial;

			_buffer = new byte[buffer_size];
			set_timeout(timeout);
			set_delimiter(output_delimiter);
			debug_on(debug);
		}

		public void Dispose()
		{
			serial.Dispose();
		}

		public void set_timeout(int timeout)
		{
			serial.SetTimeout(timeout);
		}

		public void set_delimiter(string output_delimiter)
		{
			_output_delimiter = Encoding.UTF8.GetBytes(output_delimiter);
		}

		public void debug_on(bool on)
		{
			_dbg_on = (on) ? 1 : 0;
		}

		public bool send(string command, params object[] args)
		{
			// Create and send command
			var _buffer = Encoding.UTF8.GetBytes(String.Format(command, args));

			for (var i = 0; i < _buffer.Length; i++) {
				if (putc(_buffer[i]) < 0) {
					return false;
				}
			}

			// Finish with newline
			foreach (var c in _output_delimiter) {
				if (putc(c) < 0) {
					return false;
				}
			}

			debug_if(_dbg_on, $"AT> {Encoding.UTF8.GetString(_buffer)}\n");
			return true;
		}

		private void debug_if(int dbg_on, string format)
		{
			if (dbg_on == 0)
				return;

			var buf = Encoding.UTF8.GetBytes(format);

			testBench.ConsoleWrite(buf, buf.Length);
		}

		public bool recv(string responselines, List<string> args = null)
		{
			var responses = responselines.Split('\n');
		restart:
			_aborted = false;
			foreach (var response in responses) {
				var whole_line_wanted = false;

				debug_if(_dbg_on, $"AT? {response}\n");
				// To workaround scanf's lack of error reporting, we actually
				// make two passes. One checks the validity with the modified
				// format string that only stores the matched characters (%n).
				// The other reads in the actual matched values.
				//
				// We keep trying the match until we succeed or some other error
				// derails us.
				var j = 0;

				while (true) {
					// Receive next character
					var c = getc();
					if (c < 0) {
						debug_if(_dbg_on, "AT(Timeout)\n");
						return false;
					}
					// Simplify newlines (borrowed from retarget.cpp)
					if ((c == CR && _in_prev != LF) ||
						(c == LF && _in_prev != CR)) {
						_in_prev = c;
						c = '\n';
					}
					else if ((c == CR && _in_prev == LF) ||
						(c == LF && _in_prev == CR)) {
						_in_prev = c;
						// onto next character
						continue;
					}
					else {
						_in_prev = c;
					}
					_buffer[j++] = (byte)c;
					_buffer[j] = 0;

					// Check for oob data
					foreach (var oob in oobs) {
						if (j == oob.prefix.Length && memcmp(
							oob.prefix, 0, _buffer, 0, oob.prefix.Length) == 0) {
							debug_if(_dbg_on, $"AT! {Encoding.UTF8.GetString(oob.prefix)}\n");
							oob.cb(this, EventArgs.Empty);

							if (_aborted) {
								debug_if(_dbg_on, "AT(Aborted)\n");
								return false;
							}
							// oob may have corrupted non-reentrant buffer,
							// so we need to set it up again
							goto restart;
						}
					}

					// Check for match
					Match m = null;
					if (whole_line_wanted && c != '\n') {
						// Don't attempt scanning until we get delimiter if they included it in format
						// This allows recv("Foo: %s\n") to work, and not match with just the first character of a string
						// (scanf does not itself match whitespace in its format string, so \n is not significant to it)
					}
					else {
						m = Regex.Match(Encoding.UTF8.GetString(_buffer, 0, j), response);
					}

					// We only succeed if all characters in the response are matched
					if (m.Success) {
						debug_if(_dbg_on, $"AT= {Encoding.UTF8.GetString(_buffer)}\n");
						// Store the found results
						int k = 0;
						foreach (Group g in m.Groups) {
							args[k] = g.Value;
						}

						// Jump to next line and continue parsing
						break;
					}

					// Clear the buffer when we hit a newline or ran out of space
					// running out of space usually means we ran into binary data
					if (c == '\n' || j + 1 >= _buffer.Length) {
						debug_if(_dbg_on, $"AT< {Encoding.UTF8.GetString(_buffer)}\n");
						j = 0;
					}
				}
			}

			return true;
		}

		public int putc(byte c)
		{
			serial.PutC(c);
			return 1;
		}

		public int getc()
		{
			return serial.GetC();
		}

		public void flush()
		{
			while (serial.IsReadable()) {
				serial.GetC();
			}
		}

		public int write(byte[] data, int offset, int size)
		{
			var i = offset;
			for (; i < size; i++) {
				if (putc(data[i]) < 0) {
					return -1;
				}
			}
			return i;
		}

		public int read(byte[] data, int offset, int size)
		{
			var i = offset;
			for (; i < size; i++) {
				var c = getc();
				if (c < 0) {
					return -1;
				}
				data[i] = (byte)c;
			}
			return i;
		}

		public void oob(string prefix, EventHandler cb)
		{
			oobs.Enqueue(new TOOB(prefix, cb));
		}

		public void abort()
		{
			_aborted = true;
		}

		public bool process_oob()
		{
			if (!serial.IsReadable()) {
				return false;
			}

			var i = 0;
			while (true) {
				// Receive next character
				var c = getc();
				if (c < 0) {
					return false;
				}
				// Simplify newlines (borrowed from retarget.cpp)
				if ((c == CR && _in_prev != LF) ||
					(c == LF && _in_prev != CR)) {
					_in_prev = c;
					c = '\n';
				}
				else if ((c == CR && _in_prev == LF) ||
				  (c == LF && _in_prev == CR)) {
					_in_prev = c;
					// onto next character
					continue;
				}
				else {
					_in_prev = c;
				}
				_buffer[i++] = (byte)c;
				_buffer[i] = 0;

				// Check for oob data
				foreach (var oob in oobs) {
					if (i == oob.prefix.Length && memcmp(
						oob.prefix, 0, _buffer, 0, oob.prefix.Length) == 0) {
						debug_if(_dbg_on, $"AT! {Encoding.UTF8.GetString(oob.prefix)}\r\n");
						oob.cb(this, EventArgs.Empty);
						return true;
					}
				}

				// Clear the buffer when we hit a newline or ran out of space
				// running out of space usually means we ran into binary data
				if (((i + 1) >= _buffer.Length) || (c == '\n')) {
					debug_if(_dbg_on, $"AT< {Encoding.UTF8.GetString(_buffer)}");
					i = 0;
				}
			}
		}

		private static int memcmp(byte[] src1, int pos1, byte[] src2, int pos2, int length)
		{
			var result = 0;
			for (var i = 0; i < length; i++) {
				result = src1[pos1++] - src2[pos2++];
				if (result != 0)
					return result;
			}
			return result;
		}
	}
}

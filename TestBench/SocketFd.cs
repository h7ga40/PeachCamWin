using System;

namespace TestBench
{
	public class SocketFd : IUnitInterface
	{
		private int domain;
		private int type;
		private int protocol;

		public SocketFd(int domain, int type, int protocol)
		{
			this.domain = domain;
			this.type = type;
			this.protocol = protocol;
		}

		public string TypeName => "SocketFd";

		public string InterfaceName => $"Fd{GetHashCode().ToString("X08")}";

		internal int Bind(byte[] addr, int addrlen)
		{
			throw new NotImplementedException();
		}

		internal int Listen(bool backlog)
		{
			throw new NotImplementedException();
		}

		internal int Connect(byte[] addr, int addrlen)
		{
			throw new NotImplementedException();
		}

		internal int Accept(byte[] addr, int addrlen)
		{
			throw new NotImplementedException();
		}

		internal int Send(byte[] buf, int len, int flags)
		{
			throw new NotImplementedException();
		}

		internal int SendTo(byte[] buf, int len, int flags, byte[] addr, int addrlen)
		{
			throw new NotImplementedException();
		}

		internal int Recv(byte[] buf, int len, int flags)
		{
			throw new NotImplementedException();
		}

		internal int RecvFrom(byte[] buf, int len, int flags)
		{
			throw new NotImplementedException();
		}

		internal int SetOption(int level, int option, byte[] optval, int optlen)
		{
			throw new NotImplementedException();
		}

		internal int GetOption(int level, int option, byte[] optval, ref int optlen)
		{
			throw new NotImplementedException();
		}
	}
}

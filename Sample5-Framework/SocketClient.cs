using Run;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Sample5
{
	static class SocketClient
	{
		public static async Task SendAsync(RunConfig cfg, IPAddress address, int port)
		{
			using (var clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
			{
				clientSocket.Connect(new IPEndPoint(address, port));

				using (Stream stream = File.OpenRead(cfg.FileName))
				{
					byte[] bytes = new byte[cfg.BufferSize];
					int readed = 0;
					do
					{
						readed = stream.Read(bytes, 0, cfg.BufferSize);
						await clientSocket.SendAsync(new ArraySegment<byte>(bytes, 0, readed), SocketFlags.None);
					} while (readed > 0);
				}
			}
		}
	}
}

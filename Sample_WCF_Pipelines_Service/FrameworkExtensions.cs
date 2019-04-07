using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sample_WCF_Pipelines_Service
{
	public static class FrameworkExtensions
	{
		public static Task<int> ReceiveAsync(this Socket socket, Memory<byte> memory, SocketFlags socketFlags)
		{
			var arraySegment = GetArray(memory);
			return SocketTaskExtensions.ReceiveAsync(socket, arraySegment, socketFlags);
		}

		public static Task<int> ReadAsync(this Stream stream, Memory<byte> memory)
		{
			var arraySegment = GetArray(memory);
			return stream.ReadAsync(arraySegment.Array, 0, arraySegment.Array.Length);
		}

		public static string GetString(this Encoding encoding, ReadOnlyMemory<byte> memory)
		{
			var arraySegment = GetArray(memory);
			return encoding.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
		}

		private static ArraySegment<byte> GetArray(Memory<byte> memory)
		{
			return GetArray((ReadOnlyMemory<byte>)memory);
		}

		private static ArraySegment<byte> GetArray(ReadOnlyMemory<byte> memory)
		{
			if (!MemoryMarshal.TryGetArray(memory, out var result))
			{
				throw new InvalidOperationException("Buffer backed by array was expected");
			}
			return result;
		}
	}
}

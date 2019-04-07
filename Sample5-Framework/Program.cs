// based on https://devblogs.microsoft.com/dotnet/system-io-pipelines-high-performance-io-in-net/

using Run;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// .Net Framework 4.6 is neaded for Pipelines nuget; 4.7.2 for system's socket async extensions; 
// + extensions code for sockets for reading into allocated Memory<T> from pool instead of byte[]
// C# 7.2 (latest major even with 7.3 won't work) for ref struct as returning value
// nuget System.IO.Pipelines for Pipe

namespace Sample5
{
	class Program
	{
		private static readonly RunConfig cfg = new RunConfig();

		const int port = 8087;

		static async Task Main(string[] args)
		{

			var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
			listenSocket.Listen(120);

			// Writing in Pipeline's memory via Stream
			Console.WriteLine("via Stream");

			var sw = new Stopwatch();
			sw.Start();
			for (int i = 0; i < cfg.Runs; i++)
			{
				Console.Write('.');
				using (Stream stream = File.OpenRead(cfg.FileName))
				{
					await ProcessLinesAsync(stream);
				}
			}

			if (!cfg.SingleRun)
			{
				Console.WriteLine(sw.Elapsed.TotalMilliseconds);
			}

			// Writing in Pipeline's memory via Socket
			Console.WriteLine("via Socket");

			var t1 = SocketClient.SendAsync(cfg, IPAddress.Loopback, port);
			var t2 = ProcessLinesAsync(await listenSocket.AcceptAsync());
			Task.WaitAll(t1, t2);

			if (!cfg.SingleRun)
			{
				Console.WriteLine("we are not going measure network loop speed");
			}

			Console.ReadKey();
		}

		#region Socket

		static Task ProcessLinesAsync(Socket socket)
		{
			var pipe = new Pipe();

			Task writing = FillPipeAsync(socket, pipe.Writer);
			Task reading = ReadPipeAsync(pipe.Reader);

			return Task.WhenAll(reading, writing);
		}

		static async Task FillPipeAsync(Socket socket, PipeWriter writer)
		{
			while (true)
			{
				// Allocate initial size for the PipeWriter
				Memory<byte> memory = writer.GetMemory(cfg.BufferSize);
				try
				{
					int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
					if (bytesRead == 0)
					{
						break;
					}
					// Tell the PipeWriter how much was read from the Socket
					writer.Advance(bytesRead);
				}
				catch (Exception)
				{
					break;
				}

				// Make the data available to the PipeReader
				FlushResult result = await writer.FlushAsync();

				if (result.IsCompleted)
				{
					break;
				}
			}

			// Tell the PipeReader that there's no more data coming
			writer.Complete();
		}

		#endregion

		#region Stream

		static Task ProcessLinesAsync(Stream stream)
		{
			var pipe = new Pipe();

			Task writing = FillPipeAsync(stream, pipe.Writer);
			Task reading = ReadPipeAsync(pipe.Reader);

			return Task.WhenAll(reading, writing);
		}

		static async Task FillPipeAsync(Stream stream, PipeWriter writer)
		{
			while (true)
			{
				// Allocate initial size for the PipeWriter
				Memory<byte> memory = writer.GetMemory(cfg.BufferSize);
				try
				{
					int bytesRead = await stream.ReadAsync(memory);
					if (bytesRead == 0)
					{
						break;
					}
					// Tell the PipeWriter how much was read from the Socket
					writer.Advance(bytesRead);
				}
				catch (Exception)
				{
					break;
				}

				// Make the data available to the PipeReader
				FlushResult result = await writer.FlushAsync();

				if (result.IsCompleted)
				{
					break;
				}
			}

			// Tell the PipeReader that there's no more data coming
			writer.Complete();
		}

		#endregion

		static async Task ReadPipeAsync(PipeReader reader)
		{
			int line = 0;

			while (true)
			{
				ReadResult result = await reader.ReadAsync();
				ReadOnlySequence<byte> buffer = result.Buffer;
				SequencePosition? position = null;

				do
				{
					// Look for a EOL in the buffer
					position = buffer.PositionOf((byte)'\n');

					if (position != null)
					{
						// Process the line
						ProcessLine(++line, buffer.Slice(0, position.Value));

						// Skip the line + the \n character (basically position)
						buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
					}
				}
				while (position != null);

				// Tell the PipeReader how much of the buffer we have consumed
				reader.AdvanceTo(buffer.Start, buffer.End);

				// Stop reading if there's no more data coming
				if (result.IsCompleted)
				{
					break;
				}
			}

			// Mark the PipeReader as complete
			reader.Complete();
		}

		private static void ProcessLine(int i, ReadOnlySequence<byte> buffer)
		{
			if (cfg.SingleRun)
			{
				Console.WriteLine(i);
				Console.WriteLine(Encoding.ASCII.GetString(buffer.ToArray())); // !!! materialization, full copy
			}
		}
	}
}

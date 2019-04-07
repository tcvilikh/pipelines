// based on https://devblogs.microsoft.com/dotnet/system-io-pipelines-high-performance-io-in-net/

using Run;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// .Net Framework 4.5 & C# 5 (or latest major) for async
// nuget System.Buffers for ArrayPool

namespace Sample3
{
	class Program
	{
		private static readonly RunConfig cfg = new RunConfig();

		static void Main(string[] args)
		{
			var sw = new Stopwatch();
			sw.Start();

			for (int i = 0; i < cfg.Runs; i++)
			{
				Console.Write('.');
				using (Stream stream = File.OpenRead(cfg.FileName))
				{
					ProcessLinesAsync(stream).GetAwaiter().GetResult();
				}
			}

			if (!cfg.SingleRun)
			{
				Console.WriteLine(sw.Elapsed.TotalMilliseconds);
			}

			Console.ReadKey();
		}

		static async Task ProcessLinesAsync(Stream stream)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(cfg.BufferSize);
			var bytesBuffered = 0;
			var bytesConsumed = 0;
			var i = 0;

			while (true)
			{
				// Calculate the amount of bytes remaining in the buffer
				var bytesRemaining = buffer.Length - bytesBuffered;

				if (bytesRemaining == 0)
				{
					// Double the buffer size and copy the previously buffered data into the new buffer
					var newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
					Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
					// Return the old buffer to the pool
					ArrayPool<byte>.Shared.Return(buffer);
					buffer = newBuffer;
					bytesRemaining = buffer.Length - bytesBuffered;
				}

				var bytesRead = await stream.ReadAsync(buffer, bytesBuffered, bytesRemaining);
				if (bytesRead == 0)
				{
					// EOF
					break;
				}

				// Keep track of the amount of buffered bytes
				bytesBuffered += bytesRead;
				int linePosition;
				do
				{
					// Look for a EOL in the buffered data
					linePosition = Array.IndexOf(buffer, (byte)'\n', bytesConsumed, bytesBuffered - bytesConsumed);

					if (linePosition >= 0)
					{
						// Calculate the length of the line based on the offset
						var lineLength = linePosition - bytesConsumed;

						ProcessLine(++i, buffer, bytesConsumed, lineLength);

						// Move the bytesConsumed to skip past the line we consumed (including \n)
						bytesConsumed += lineLength + 1;
					}
				}
				while (linePosition >= 0);
			}
		}

		static void ProcessLine(int line, byte[] buffer, int bytesConsumed, int lineLength)
		{
			if (cfg.SingleRun)
			{
				Console.WriteLine(line);
				Console.WriteLine(Encoding.UTF8.GetString(buffer, bytesConsumed, lineLength));
			}
		}
	}
}

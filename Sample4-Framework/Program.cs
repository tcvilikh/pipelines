// based on https://devblogs.microsoft.com/dotnet/system-io-pipelines-high-performance-io-in-net/

using Run;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// .Net Framework 4.7 & C# 7.0 (or latest major) for tuple result (int, int)
// C# 7.1 (latest major even with 7.3 won't work) for async Task Main
// nuget System.Buffers for ArrayPool

namespace Sample4
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

		public class BufferSegment
		{
			public byte[] Buffer { get; set; }
			public int Count { get; set; }
			public int Consumed { get; set; }
			public int Remaining => Buffer.Length - Count;
		}

		static async Task ProcessLinesAsync(Stream stream)
		{
			var segments = new List<BufferSegment>();
			var segment = new BufferSegment { Buffer = ArrayPool<byte>.Shared.Rent(cfg.BufferSize) };
			var pendingBufferIndex = 0;
			var line = 0;

			segments.Add(segment);

			while (true)
			{
				// Calculate the amount of bytes remaining in the buffer
				if (segment.Remaining < cfg.BufferSize)
				{
					// Allocate a new segment
					segment = new BufferSegment { Buffer = ArrayPool<byte>.Shared.Rent(cfg.BufferSize) };
					segments.Add(segment);
				}

				var bytesRead = await stream.ReadAsync(segment.Buffer, segment.Count, segment.Remaining);
				if (bytesRead == 0)
				{
					break;
				}

				// Keep track of the amount of buffered bytes
				segment.Count += bytesRead;

				while (true)
				{
					// Look for a EOL in the list of segments
					var (segmentIndex, segmentOffset) = IndexOf(segments, (byte)'\n', pendingBufferIndex, segments[pendingBufferIndex].Consumed);

					if (segmentIndex >= 0)
					{
						ProcessLine(++line, segments, pendingBufferIndex, segments[pendingBufferIndex].Consumed, segmentIndex, segmentOffset);

						segments[segmentIndex].Consumed = segmentOffset + 1;
						pendingBufferIndex = segmentIndex;
					}
					else
					{
						segments[pendingBufferIndex].Consumed = segments[pendingBufferIndex].Buffer.Length;
						break;
					}
				}

				// Drop fully consumed segments from the list so we don't look at them again
				var currentPendingBufferIndex = pendingBufferIndex;
				for (var i = currentPendingBufferIndex - 1; i >= 0; --i)
				{
					var consumedSegment = segments[i];
					ArrayPool<byte>.Shared.Return(consumedSegment.Buffer);
					segments.RemoveAt(i);
					--pendingBufferIndex;
				}
			}
		}

		static (int segmentIndex, int segmentOffest) IndexOf(List<BufferSegment> segments, byte value, int startBufferIndex, int startSegmentOffset)
		{
			var first = true;
			for (var i = startBufferIndex; i < segments.Count; ++i)
			{
				var segment = segments[i];
				// Start from the correct offset
				var offset = first ? startSegmentOffset : 0;
				var index = Array.IndexOf(segment.Buffer, value, offset, segment.Count - offset);

				if (index >= 0)
				{
					// Return the buffer index and the index within that segment where EOL was found
					return (i, index);
				}

				first = false;
			}
			return (-1, -1);
		}

		static void ProcessLine(int line, List<BufferSegment> buffer, int firstSegmentIndex, int firstSegmentOffset, int lastSegmentIndex, int lastSegmentOffset)
		{
			if (cfg.SingleRun)
			{
				Console.WriteLine(line);

				if (firstSegmentIndex == lastSegmentIndex)
				{
					Console.WriteLine(Encoding.UTF8.GetString(buffer[firstSegmentIndex].Buffer, firstSegmentOffset, lastSegmentOffset - firstSegmentOffset));
				}
				else
				{
					Console.Write(Encoding.UTF8.GetString(buffer[firstSegmentIndex].Buffer,
						firstSegmentOffset, buffer[firstSegmentIndex].Buffer.Length - firstSegmentOffset));
					for (int i = firstSegmentIndex + 1; i < lastSegmentIndex; i++)
					{
						Console.Write(Encoding.UTF8.GetString(buffer[i].Buffer));
					}
					Console.WriteLine(Encoding.UTF8.GetString(buffer[lastSegmentIndex].Buffer, 0, lastSegmentOffset));
				}
			}
		}
	}
}

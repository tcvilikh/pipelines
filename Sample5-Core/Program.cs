// based on https://devblogs.microsoft.com/dotnet/system-io-pipelines-high-performance-io-in-net/

using System;
using System.IO;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.Buffers;
using System.Text;
using Run;
using System.Diagnostics;

// .Net Core 2.1 for stream ReadAsync and for buffer.First/string.Create ReadOnlyMemory<T>/ReadOnlySequence<T>
// C# 7.2 (latest major even with 7.3 won't work) for ref struct as returning value
// nuget System.IO.Pipelines for Pipe

namespace Sample5
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

		static Task ProcessLinesAsync(Stream stream)
		{
			// my test max line is 4k+
			// this case dead locks
			//var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 4096, resumeWriterThreshold: 512));

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
				Memory<byte> memory = writer.GetMemory();
				try
				{
					int bytesRead = await stream.ReadAsync(memory);
					if (bytesRead == 0)
					{
						break;
					}
					// Tell the PipeWriter how much was read from the Stream
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

		static async Task ReadPipeAsync(PipeReader reader)
		{
			var line = 0;

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

		private static void ProcessLine(int line, ReadOnlySequence<byte> buffer)
		{
			if (cfg.SingleRun)
			{
				Console.WriteLine(line);

				if (buffer.IsSingleSegment)
				{
					Console.WriteLine(Encoding.ASCII.GetString(buffer.First.Span));
				}
				else
				{
					Console.WriteLine(string.Create((int)buffer.Length, buffer, (span, sequence) =>
					{
						foreach (var segment in sequence)
						{
							Encoding.ASCII.GetChars(segment.Span, span);
							span = span.Slice(segment.Length);
						}
					}));
				}
			}
		}
	}
}

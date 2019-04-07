using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace Sample_WCF_Pipelines_Service
{
	class Analyzer
	{
		public async Task ReadPipeAsync(PipeReader reader)
		{
			Console.WriteLine($"ReadPipeAsync started");

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

			Console.WriteLine($"ReadPipeAsync completed");
		}

		private static void ProcessLine(int i, ReadOnlySequence<byte> buffer)
		{
			Console.WriteLine(i);
			Console.WriteLine(Encoding.ASCII.GetString(buffer.ToArray())); // !!! materialization, full copy
		}
	}
}

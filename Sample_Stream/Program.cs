using Run;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Sample_Stream
{
	class Program
	{
		private static readonly RunConfig cfg = new RunConfig();

		static void Main()
		{
			var sw = new Stopwatch();
			sw.Start();

			for (int i = 0; i < cfg.Runs; i++)
			{
				Console.Write('.');

				// Client-side
				using (Stream stream = File.OpenRead(@"../../../smoke.txt"))
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

		// Server-side
		static async Task ProcessLinesAsync(Stream stream)
		{
			var line = 0;

			// Retriever, Producer
			using (var sr = new StreamReader(stream, Encoding.UTF8))
			{
				// Analyzer, Consumer
				while (!sr.EndOfStream)
				{
					ProcessLine(++line, await sr.ReadLineAsync());
				}
			}
		}

		static void ProcessLine(int line, string data)
		{
			if (cfg.SingleRun)
			{
				Console.WriteLine(line);
				Console.WriteLine(data);
			}
		}
	}
}

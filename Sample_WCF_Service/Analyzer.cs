using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Sample_WCF_Service
{
	class Analyzer
	{
		public async Task ProcessLinesAsync(Stream stream)
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

			Console.WriteLine("{0} Analyze completed", DateTime.Now);
		}

		static void ProcessLine(int line, string data)
		{
			Console.WriteLine(line);
			//Console.WriteLine(data);
		}
	}
}

using System;
using System.IO;
using System.IO.Pipelines;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Sample_WCF_Pipelines_Service
{
	[ServiceContract]
	public interface IService
	{
		[OperationContract]
		void Upload(Stream file);
	}

	public class Service : IService
	{
		public Service()
		{
			Console.WriteLine($"Service started at {Thread.CurrentThread.ManagedThreadId}");
		}

		public void Upload(Stream file)
		{
			var pipe = new Pipe();

			Task writing = FillPipeAsync(file, pipe.Writer);
			Task reading = new Analyzer().ReadPipeAsync(pipe.Reader);

			writing.Wait();
		}

		private async Task FillPipeAsync(Stream stream, PipeWriter writer)
		{
			while (true)
			{
				// Allocate initial size for the PipeWriter
				Memory<byte> memory = writer.GetMemory(4096);
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
				catch (Exception ex)
				{
					Console.WriteLine(ex);
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

			Console.WriteLine($"file readed from stream");
		}
	}
}

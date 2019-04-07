using System;
using System.IO;
using System.ServiceModel;
using System.Threading;

namespace Sample_WCF_Service
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
			new Analyzer().ProcessLinesAsync(file).GetAwaiter().GetResult();
		}
	}
}

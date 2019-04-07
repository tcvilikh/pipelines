using Sample_WCF_Client.ServiceReference;
using System;
using System.IO;

namespace Sample_WCF_Client
{
	class Program
	{
		static void Main()
		{
			using (var client = new ServiceClient())
			{
				using (Stream stream = File.OpenRead(@"../../../big.txt"))
				{
					client.Upload(stream);
				}
				Console.WriteLine("{0} File uploaded", DateTime.Now);
			}

			Console.ReadKey();
		}
	}
}

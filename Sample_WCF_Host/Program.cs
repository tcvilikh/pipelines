using Sample_WCF_Service;
using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Sample_WCF_Host
{
	class Program
	{
		static void Main()
		{
			try
			{
				Uri httpBaseAddress = new Uri("http://localhost:43210/Service");

				var serviceHost = new ServiceHost(typeof(Service), httpBaseAddress);

				// Add Endpoint to Host
				var binding = new WSHttpBinding();
				binding.ReceiveTimeout = TimeSpan.FromMinutes(3);
				binding.MaxReceivedMessageSize = int.MaxValue;
				serviceHost.AddServiceEndpoint(typeof(IService), binding, "");

				// Metadata Exchange
				ServiceMetadataBehavior serviceBehavior = new ServiceMetadataBehavior();
				serviceBehavior.HttpGetEnabled = true;
				serviceHost.Description.Behaviors.Add(serviceBehavior);

				// Open
				serviceHost.Open();
				Console.WriteLine("Service is live now at: {0}", httpBaseAddress);
				Console.ReadKey();
			}
			catch (Exception ex)
			{
				Console.WriteLine("There is an issue with service " + ex.Message);

				if ((ex.InnerException as System.Net.HttpListenerException)?.ErrorCode == 5)
				{
					Console.WriteLine("Run as admin or find command to modify user's rights at https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/configuring-http-and-https");
				}
			}
		}
	}
}

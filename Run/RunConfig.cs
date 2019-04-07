using System;
using System.Reflection;
using System.Runtime.Versioning;

namespace Run
{
	public class RunConfig
	{
		private string path = "../../../";
		private string corePath = "../../../../";

		public RunConfig(int bufferSize = 1024, bool singleRun = true, int runs = 5)
		{
			BufferSize = bufferSize;
			SingleRun = singleRun;
			Runs = singleRun ? 1 : runs;

			IsCore = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName?.StartsWith(".NetCore", StringComparison.InvariantCultureIgnoreCase) ?? false;
		}

		public bool IsCore { get; }

		public string FileName => (IsCore ? corePath : path) + (SingleRun ? "smoke.txt" : "big.txt");
		public int BufferSize { get; }

		public bool SingleRun { get; }
		public int Runs { get; }
	}
}

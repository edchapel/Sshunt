using NLog;
using NLog.Config;
using NLog.Targets;

namespace Sshunt
{
	internal static class LogConfiguration
	{
		public static void SetUp(LogLevel logLevel)
		{
			// Step 1. Create configuration object 
			var config = new LoggingConfiguration();

			// Step 2. Create targets and add them to the configuration 
			var consoleTarget = new ColoredConsoleTarget();
			config.AddTarget("console", consoleTarget);

			var fileTarget = new FileTarget();
			config.AddTarget("file", fileTarget);

			// Step 3. Set target properties 
			consoleTarget.Layout = @"${message} ${exception:format=tostring}";
			fileTarget.FileName = "${basedir}/sshunt.log";
			fileTarget.Layout = "${longdate} ${message} ${exception:format=tostring}";

			// Step 4. Define rules
			var rule1 = new LoggingRule("*", logLevel, consoleTarget);
			config.LoggingRules.Add(rule1);

			var rule2 = new LoggingRule("*", logLevel, fileTarget);
			config.LoggingRules.Add(rule2);

			// Step 5. Activate the configuration
			LogManager.Configuration = config;
		}
	}
}
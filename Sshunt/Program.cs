using System;
using System.Diagnostics;

using NLog;

namespace Sshunt
{
	internal class Program
	{
		private const int UNHANDLED_EXCEPTION_ERROR_CODE = 1;
		private const int INVALID_ARGS_ERROR_CODE = 2;
		private const int NO_IDENTITY_FILE_ERROR_CODE = 3;

		private static readonly Logger _Logger = LogManager.GetCurrentClassLogger();

		public static void Main(string[] args)
		{
			try
			{
				var options = new Options();

				string helpText;
				if (!options.TryParse(args, out helpText))
				{
					if (Environment.UserInteractive)
					{
						Console.Out.WriteLine(helpText);
					}
					Environment.ExitCode = INVALID_ARGS_ERROR_CODE;
					return;
				}

				LogConfiguration.SetUp(options.LogLevel);

				var identityFileLocator = new IdentityFileLocator();

				var sshClientFactory = new SshClientFactory(identityFileLocator);
				using (var sshClient = sshClientFactory.CreateSshClient(options))
				{
					var service = new SshService(sshClient, options);
					service.Connect();
				}
			}
			catch (UnableToLocateIdentityFileException e)
			{
				_Logger.Fatal(e.Message);
				Environment.ExitCode = NO_IDENTITY_FILE_ERROR_CODE;
			}
			catch (Exception e)
			{
				_Logger.FatalException("Unexpected exception", e);
				Environment.ExitCode = UNHANDLED_EXCEPTION_ERROR_CODE;
			}
#if DEBUG // Helpful when debugging so you can read the window before it disappears
			finally
			{
				if (Debugger.IsAttached && Environment.UserInteractive)
				{
					Console.Write("Hit any key to exit...");
					Console.ReadKey(true);
				}
			}
#endif

			_Logger.Factory.Flush(1000);
		}
	}
}
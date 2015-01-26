using System;
using System.Diagnostics;
using System.Threading;

using NLog;

using Sshunt.WindowsService;

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
			LogConfiguration.SetUp(LogLevel.Warn);

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
			new Program().Run(options, new CancellationTokenSource());
		}

		public void Run(Options options, CancellationTokenSource cancellationTokenSource)
		{
			try
			{
				LogConfiguration.SetUp(options.LogLevel);

				if (options.AreForWindowsService)
				{
					WindowsServiceController.Execute(options);
					return;
				}

				var identityFileLocator = new IdentityFileLocator();

				if (Environment.UserInteractive)
				{
					Console.CancelKeyPress += (s, e) =>
					                          {
						                          e.Cancel = true;
						                          if (!cancellationTokenSource.IsCancellationRequested)
						                          {
							                          cancellationTokenSource.Cancel();
						                          }
					                          };
				}

				var sshClientFactory = new SshClientFactory(identityFileLocator);
				using (var sshClient = sshClientFactory.CreateSshClient(options))
				{
					sshClient.KeepAliveInterval = TimeSpan.FromSeconds(60);
					var service = new SshService(sshClient, options);
					service.Connect(cancellationTokenSource.Token);
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
using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using NLog;

namespace Sshunt.WindowsService
{
	public class SshuntWindowsService : ServiceBase
	{
		private static readonly Logger _Logger = LogManager.GetCurrentClassLogger();

		private CancellationTokenSource _cancellationTokenSource;
		private Task _service;

		public SshuntWindowsService()
		{
			LogConfiguration.SetUp(LogLevel.Debug);
			ServiceName = ServiceConstants.DisplayName;
		}

		protected override void OnStart(string[] args)
		{
			_Logger.Info("Starting Windows service...");
			try
			{
				if (_cancellationTokenSource != null)
				{
					if (!_cancellationTokenSource.IsCancellationRequested)
					{
						_cancellationTokenSource.Cancel();
						_cancellationTokenSource.Dispose();
					}

					_cancellationTokenSource = null;
				}

				var settingsFilePath = WindowsServiceController.GetSettingsFilePath();
				if (!File.Exists(settingsFilePath))
				{
					_Logger.Fatal("Unable to locate settings file at: {0}", settingsFilePath);
					return;
				}

				var optionsJson = File.ReadAllText(settingsFilePath);
				var options = JsonConvert.DeserializeObject<Options>(optionsJson);

				_service = Task.Factory.StartNew(() => new Program().Run(options, _cancellationTokenSource));
				_Logger.Info("Started Windows service...");
			}
			catch (Exception e)
			{
				_Logger.ErrorException("Error starting service.", e);
			}
		}

		protected override void OnStop()
		{
			_Logger.Info("Stopping Windows service...");

			try
			{
				if (_cancellationTokenSource != null)
				{
					if (!_cancellationTokenSource.IsCancellationRequested)
					{
						_cancellationTokenSource.Cancel();
						_cancellationTokenSource.Dispose();
					}

					_cancellationTokenSource = null;
				}

				_Logger.Info("Stopped Windows service.");
			}
			catch (Exception e)
			{
				_Logger.ErrorException("Error stopping service.", e);
			}
			finally
			{
				_service = null;
			}
		}
	}
}
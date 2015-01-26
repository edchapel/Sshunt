using System;
using System.Collections;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

using Newtonsoft.Json;

using NLog;

namespace Sshunt.WindowsService
{
	internal static class WindowsServiceController
	{
		private static readonly Logger _Logger = LogManager.GetCurrentClassLogger();

		public static void Execute(Options options)
		{
			if (options.InstallService)
			{
				Install(options);
			}
			else if (options.UninstallService)
			{
				Uninstall();
			}
			else if (options.StartService)
			{
				StartService();
			}
			else if (options.StopService)
			{
				StopService();
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		public static void Install(Options options)
		{
			var jsonFile = GetSettingsFilePath();

			var optionsJson = JsonConvert.SerializeObject(options);
			File.WriteAllText(jsonFile, optionsJson);

			Install(true);
		}

		public static void Uninstall()
		{
			Install(false);
		}

		private static void Install(bool install)
		{
			try
			{
				_Logger.Info(install ? "Installing" : "Uninstalling");
				using (var inst = new AssemblyInstaller(typeof (Program).Assembly, null))
				{
					IDictionary state = new Hashtable();
					inst.UseNewContext = true;

					try
					{
						if (install)
						{
							inst.Install(state);
							inst.Commit(state);
						}
						else
						{
							inst.Uninstall(state);
						}
					}
					catch
					{
						try
						{
							inst.Rollback(state);
						}
						catch (Exception ex)
						{
							_Logger.Error("Error Rolling back");
							_Logger.ErrorException(ex.Message, ex);
						}
						throw;
					}
				}
			}
			catch (Exception ex)
			{
				_Logger.ErrorException(ex.Message, ex);
			}
		}

		public static void StartService()
		{
			AdjustService(true);
		}

		public static void StopService()
		{
			AdjustService(false);
		}

		private static void AdjustService(bool start)
		{
			var controller = GetServiceController();

			if (controller == null)
			{
				_Logger.Warn("Service not found");
				return;
			}

			_Logger.Info("Service found");

			if (start)
			{
				_Logger.Info("Attempting to start service");
				controller.Start();
				_Logger.Info("Service successfully started");
			}
			else
			{
				if (controller.Status.Equals(ServiceControllerStatus.Running)
				    && controller.CanStop)
				{
					_Logger.Info("Service is running. Attempting to stop service");
					controller.Stop();
					_Logger.Info("Service successfully stopped");
				}
			}
		}

		private static ServiceController GetServiceController()
		{
			_Logger.Info("Checking if service {0} exists", ServiceConstants.ServiceName);
			return ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == ServiceConstants.ServiceName);
		}

		public static string GetSettingsFilePath()
		{
			var codeBase = Assembly.GetExecutingAssembly().CodeBase;
			var uri = new UriBuilder(codeBase);
			var path = Uri.UnescapeDataString(uri.Path);
			var workingDir = Path.GetDirectoryName(path);
			var jsonFile = Path.Combine(workingDir, "service.settings");
			return jsonFile;
		}
	}
}
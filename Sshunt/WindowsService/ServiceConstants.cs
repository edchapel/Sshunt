using System.ServiceProcess;

namespace Sshunt.WindowsService
{
	public static class ServiceConstants
	{
		public const string Description = "Maintains persistent SSH connections and reconnects automatically.";
		public const string DisplayName = "Sshunt - Persistent SSH Connections";
		public const string ServiceName = "SSHUNT";
		public const ServiceStartMode StartType = ServiceStartMode.Automatic;
	}
}
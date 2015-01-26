using System.ComponentModel;
using System.ServiceProcess;

namespace Sshunt.WindowsService
{
	[RunInstaller(true)]
	public sealed class ServiceProcessInstaller : System.ServiceProcess.ServiceProcessInstaller
	{
		public ServiceProcessInstaller()
		{
			// Installs using local Service, this may need to be updated with the proper credentials after install
			Account = ServiceAccount.LocalService;
		}
	}
}
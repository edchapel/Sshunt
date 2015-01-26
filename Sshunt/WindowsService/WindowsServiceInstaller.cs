using System.ComponentModel;
using System.ServiceProcess;

namespace Sshunt.WindowsService
{
	[RunInstaller(true)]
	public sealed class WindowsServiceInstaller : ServiceInstaller
	{
		public WindowsServiceInstaller()
		{
			Description = ServiceConstants.Description;
			DisplayName = ServiceConstants.DisplayName;
			ServiceName = ServiceConstants.ServiceName;
			StartType = ServiceConstants.StartType;
		}
	}
}
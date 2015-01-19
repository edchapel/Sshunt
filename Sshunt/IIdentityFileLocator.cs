using Renci.SshNet;

namespace Sshunt
{
	internal interface IIdentityFileLocator
	{
		PrivateKeyFile FindIdentityFile(string identityFile, string keyPassphrase);
	}
}
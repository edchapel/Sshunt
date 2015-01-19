using NLog;

using Renci.SshNet;

namespace Sshunt
{
	internal class SshClientFactory
	{
		private readonly IIdentityFileLocator _identityFileLocator;
		private readonly Logger _logger;

		public SshClientFactory(IIdentityFileLocator identityFileLocator)
		{
			_identityFileLocator = identityFileLocator;
			_logger = LogManager.GetCurrentClassLogger();
		}

		public SshClient CreateSshClient(Options options)
		{
			var password = options.GetPassword();
			if (!string.IsNullOrWhiteSpace(password))
			{
				_logger.Warn("Using password to connect, against better advice...");
				return new SshClient(options.Host, options.Port, options.UserName, password);
			}

			var identityFile = _identityFileLocator.FindIdentityFile(options.IdentityFile, options.GetKeyPassphrase());
			if (identityFile != null)
			{
				return new SshClient(options.Host, options.Port, options.UserName, identityFile);
			}

			throw new UnableToLocateIdentityFileException(options.IdentityFile ?? "<no identitify file specified>");
		}
	}
}
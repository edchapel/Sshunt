using System;
using System.IO;

using NLog;

using Renci.SshNet;

namespace Sshunt
{
	internal class IdentityFileLocator : IIdentityFileLocator
	{
		private readonly Logger _logger;

		public IdentityFileLocator()
		{
			_logger = LogManager.GetCurrentClassLogger();
		}

		public PrivateKeyFile FindIdentityFile(string identityFile, string keyPassphrase)
		{
			if (string.IsNullOrWhiteSpace(identityFile))
			{
				var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

				if (string.IsNullOrWhiteSpace(userDir))
				{
					_logger.Error("Error looking for identity file: user profile dir is unknown.");
					return null;
				}
				if (!Directory.Exists(userDir))
				{
					_logger.Error("Error looking for identity file: unable to locate user profile dir at '{0}'.", userDir);
					return null;
				}

				var sshDir = Path.Combine(userDir, ".ssh");
				if (!Directory.Exists(sshDir))
				{
					_logger.Error("Error looking for identity file: unable to locate '{0}' dir.", sshDir);
					return null;
				}

				string[] possibleIdFileNames = {"id_dsa", "id_ecdsa", "id_ed25519", "id_rsa"};
				foreach (var possibleIdFileName in possibleIdFileNames)
				{
					var keyFile = Path.Combine(sshDir, possibleIdFileName);
					if (File.Exists(keyFile))
					{
						_logger.Debug("Using identity_file from '{0}'", keyFile);
						identityFile = keyFile;
						break;
					}

					_logger.Trace("Did not find identity_file at '{0}'", keyFile);
				}

				if (string.IsNullOrWhiteSpace(identityFile))
				{
					_logger.Error("Error looking for identity file: unable to auto-locate identity file.");
					return null;
				}
			}
			else if (!File.Exists(identityFile))
			{
				_logger.Error("Error looking for identity file: unable to locate identity file: {0}", identityFile);
				return null;
			}

			return string.IsNullOrWhiteSpace(keyPassphrase)
				? new PrivateKeyFile(identityFile)
				: new PrivateKeyFile(identityFile, keyPassphrase);
		}
	}
}
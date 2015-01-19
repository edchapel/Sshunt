using System;

namespace Sshunt
{
	internal class UnableToLocateIdentityFileException : Exception
	{
		public UnableToLocateIdentityFileException(string keyFilePath)
			: base(string.Format("Unable to locate an identity file from path '{0}'", keyFilePath)) {}

		public UnableToLocateIdentityFileException(string keyFilePath, Exception innerException)
			: base(string.Format("Unable to locate an identity file from path '{0}'", keyFilePath), innerException) {}
	}
}
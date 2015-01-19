using System.IO;

using NSubstitute;

using NUnit.Framework;

using Renci.SshNet;

using Sshunt.Properties;

namespace Sshunt
{
	[TestFixture]
	public class SshClientFactoryTests
	{
		[Test]
		public void Should_look_for_identity_file_when_password_empty()
		{
			using (var keyStream = new MemoryStream(Resources.TestKey))
			{
				var identityFileLocator = Substitute.For<IIdentityFileLocator>();
				identityFileLocator.FindIdentityFile(null, null).Returns(new PrivateKeyFile(keyStream));

				var sshClientFactory = new SshClientFactory(identityFileLocator);

				var options = new Options {Host = "foo@server", Password = string.Empty,};
				var sshClient = sshClientFactory.CreateSshClient(options);

				Assert.That(sshClient, Is.Not.Null);
				identityFileLocator.ReceivedWithAnyArgs().FindIdentityFile(null, null);
			}
		}

		[Test]
		public void Should_look_for_identity_file_when_password_null()
		{
			using (var keyStream = new MemoryStream(Resources.TestKey))
			{
				var identityFileLocator = Substitute.For<IIdentityFileLocator>();
				identityFileLocator.FindIdentityFile(null, null).Returns(new PrivateKeyFile(keyStream));

				var sshClientFactory = new SshClientFactory(identityFileLocator);

				var options = new Options {Host = "foo@server", Password = null,};
				var sshClient = sshClientFactory.CreateSshClient(options);

				Assert.That(sshClient, Is.Not.Null);
				identityFileLocator.ReceivedWithAnyArgs().FindIdentityFile(null, null);
			}
		}

		[Test]
		public void Should_not_look_for_identity_file_when_password_supplied()
		{
			var identityFileLocator = Substitute.For<IIdentityFileLocator>();
			var sshClientFactory = new SshClientFactory(identityFileLocator);

			var options = new Options {Host = "foo@server", Password = "bar",};
			var sshClient = sshClientFactory.CreateSshClient(options);

			Assert.That(sshClient, Is.Not.Null);
			identityFileLocator.DidNotReceiveWithAnyArgs().FindIdentityFile(null, null);
		}

		[Test]
		public void Should_throw_when_no_identity_file_can_be_found()
		{
			var identityFileLocator = Substitute.For<IIdentityFileLocator>();
			identityFileLocator.FindIdentityFile(null, null).Returns((PrivateKeyFile) null);

			var sshClientFactory = new SshClientFactory(identityFileLocator);

			var options = new Options {Host = "foo@server", Password = null,};

			Assert.Throws<UnableToLocateIdentityFileException>(() => sshClientFactory.CreateSshClient(options));
		}
	}
}
using System.Linq;

using NUnit.Framework;

namespace Sshunt
{
	[TestFixture]
	public class OptionsTests
	{
		[Test]
		public void Should_construct_without_exception()
		{
			Assert.That(new Options(), Is.Not.Null);
		}

		[Test]
		public void Should_fail_when_forward_has_negative_port()
		{
			var options = new Options();
			var args = new[] {"-L", "123:foo.com:-123", "host",};

			string text;
			Assert.That(options.TryParse(args, out text), Is.False, "Negative ports are not valid.");
			Assert.That(text, Is.StringContaining("unable to parse port forward '123:foo.com:-123'"));
		}

		[Test]
		public void Should_fail_when_forward_has_non_numeric_port()
		{
			var options = new Options();
			var args = new[] {"-L", "a:foo.com:123", "host",};

			string text;
			Assert.That(options.TryParse(args, out text), Is.False, "Non-numeric ports are not valid.");
			Assert.That(text, Is.StringContaining("unable to parse port forward 'a:foo.com:123'"));
		}

		[Test]
		public void Should_fail_when_host_is_missing()
		{
			var options = new Options();
			var args = new string[0];

			string text;
			Assert.That(options.TryParse(args, out text), Is.False, "Host is required.");
			Assert.That(text, Is.StringContaining("hostname required option is missing"));
		}

		[Test]
		public void Should_fail_when_identity_file_option_used_without_value()
		{
			var options = new Options();
			var args = new[] {"-i", "host",};

			string text;
			Assert.That(options.TryParse(args, out text), Is.False, "Host param is considered the identity_file, host missing");
			Assert.That(text, Is.StringContaining("hostname required option is missing"));
		}

		[Test]
		public void Should_fail_when_key_passphrase_option_used_without_value()
		{
			var options = new Options();
			var args = new[] {"--key-passphrase", "host",};

			string text;
			Assert.That(options.TryParse(args, out text), Is.False, "Host param is considered the key passphrase, host missing");
			Assert.That(text, Is.StringContaining("hostname required option is missing"));
		}

		[Test]
		public void Should_fail_when_password_and_identity_file_options_used_at_same_time()
		{
			string text;
			Assert.That(new Options().TryParse(new[] {"--password", "foo", "-i", "bar", "host",}, out text), Is.False, "--password and -i cannot be used together");
			Assert.That(text, Is.StringContaining("-i option violates mutual exclusiveness."));

			// Try the other order
			Assert.That(new Options().TryParse(new[] {"-i", "bar", "--password", "foo", "host",}, out text), Is.False, "-i and --password cannot be used together");
			Assert.That(text, Is.StringContaining("-i option violates mutual exclusiveness."));
		}

		[Test]
		public void Should_fail_when_password_option_used_without_value()
		{
			var options = new Options();
			var args = new[] {"--password", "host",};

			string text;
			Assert.That(options.TryParse(args, out text), Is.False, "Host param is considered the password, host missing");
			Assert.That(text, Is.StringContaining("hostname required option is missing"));
		}

		[Test]
		public void Should_fail_when_port_is_not_numerical()
		{
			var options = new Options();
			var args = new[] {"-p", "fff", "host",};

			string text;
			Assert.That(options.TryParse(args, out text), Is.False, "Non-numerical ports are not valid.");
			Assert.That(text, Is.StringContaining("-p option violates format."));
		}

		[Test]
		public void Should_fail_when_verbose_and_quiet_options_used_at_same_time()
		{
			string text;
			const string mutualExclusivenessErrorMessage = "-v/--verbose option violates mutual exclusiveness.";

			// Try all combinations

			Assert.That(new Options().TryParse(new[] {"-v", "-q", "host",}, out text), Is.False, "'-v -q' cannot be used together");
			Assert.That(text, Is.StringContaining(mutualExclusivenessErrorMessage));

			Assert.That(new Options().TryParse(new[] {"-v", "--quiet", "host",}, out text), Is.False, "'-v --quiet' cannot be used together");
			Assert.That(text, Is.StringContaining(mutualExclusivenessErrorMessage));

			Assert.That(new Options().TryParse(new[] {"--verbose", "-q", "host",}, out text), Is.False, "'--verbose -q' cannot be used together");
			Assert.That(text, Is.StringContaining(mutualExclusivenessErrorMessage));

			Assert.That(new Options().TryParse(new[] {"--verbose", "--quiet", "host",}, out text), Is.False, "'--verbose --quiet' cannot be used together");
			Assert.That(text, Is.StringContaining(mutualExclusivenessErrorMessage));

			Assert.That(new Options().TryParse(new[] {"-q", "-v", "host",}, out text), Is.False, "'-q -v' cannot be used together");
			Assert.That(text, Is.StringContaining(mutualExclusivenessErrorMessage));

			Assert.That(new Options().TryParse(new[] {"-q", "--verbose", "host",}, out text), Is.False, "'-q --verbose' cannot be used together");
			Assert.That(text, Is.StringContaining(mutualExclusivenessErrorMessage));

			Assert.That(new Options().TryParse(new[] {"--quiet", "-v", "host",}, out text), Is.False, "'--quiet -v' cannot be used together");
			Assert.That(text, Is.StringContaining(mutualExclusivenessErrorMessage));

			Assert.That(new Options().TryParse(new[] {"--quiet", "--verbose", "host",}, out text), Is.False, "'--quiet --verbose' cannot be used together");
			Assert.That(text, Is.StringContaining(mutualExclusivenessErrorMessage));
		}

		[Test]
		public void Should_fail_with_extra_unknown_args()
		{
			var options = new Options();
			var args = new[] {"host", "someExtraThing",};

			string text;
			Assert.That(options.TryParse(args, out text), Is.False, "Extra arguments at the end are not supported.");
			Assert.That(text, Is.StringContaining("unknown option someExtraThing"));
		}

		[Test]
		public void Should_get_non_null_forwarded_ports_when_args_not_passed()
		{
			var options = new Options();
			var forwardedPorts = options.GetForwardedPorts();
			Assert.That(forwardedPorts, Is.Empty, "Expected empty list of ports");
		}

		[Test]
		public void Should_have_correct_defaults()
		{
			var options = new Options();
			Assert.That(options.Verbose, Is.False, "Verbose");
			Assert.That(options.Quiet, Is.False, "Quiet");
		}

		[Test]
		public void Should_parse_a_local_and_remote_forward_into_forwarded_ports()
		{
			var options = new Options
			              {
				              LocalForwards = new[] {"foo:123:bar:456"},
				              RemoteForwards = new[] {"123:remote.host:456",}
			              };

			var forwardedPorts = options.GetForwardedPorts();
			Assume.That(forwardedPorts, Is.Not.Null, "Received null set of ports");

			var actual = forwardedPorts.Select(x => x.ToString())
			                           .ToArray();
			var expected = new[]
			               {
				               "Local foo:123 => bar:456",
				               "Remote 123 => remote.host:456",
			               };
			Assert.That(actual, Is.EquivalentTo(expected), "Different forward ports than the options specified.");
		}

		[Test]
		public void Should_parse_local_forwards_into_forwarded_ports()
		{
			var forwards = new[] {"123:127.0.0.1:456", "123:foo:456", "foo:123:bar:456",};
			var options = new Options {LocalForwards = forwards};

			var forwardedPorts = options.GetForwardedPorts();
			Assume.That(forwardedPorts, Is.Not.Null, "Received null set of ports");

			var actual = forwardedPorts.Select(x => x.ToString())
			                           .ToArray();
			var expected = new[]
			               {
				               "Local 123 => 127.0.0.1:456",
				               "Local 123 => foo:456",
				               "Local foo:123 => bar:456",
			               };
			Assert.That(actual, Is.EquivalentTo(expected), "Different local forward ports than the options specified.");
		}

		[Test]
		public void Should_parse_remote_forwards_into_forwarded_ports()
		{
			var forwards = new[] {"123:127.0.0.1:456", "123:foo:456", "foo:123:bar:456",};
			var options = new Options {RemoteForwards = forwards};

			var forwardedPorts = options.GetForwardedPorts();
			Assume.That(forwardedPorts, Is.Not.Null, "Received null set of ports");

			var actual = forwardedPorts.Select(x => x.ToString())
			                           .ToArray();
			var expected = new[]
			               {
				               "Remote 123 => 127.0.0.1:456",
				               "Remote 123 => foo:456",
				               "Remote foo:123 => bar:456",
			               };
			Assert.That(actual, Is.EquivalentTo(expected), "Different remote forward ports than the options specified.");
		}

		[Test]
		public void Should_succeed_with_only_a_host_arg()
		{
			var options = new Options();
			var args = new[] {"host",};

			string text;
			Assert.That(options.TryParse(args, out text), Is.True, "Args: '{0}', Errors: {1}", string.Join("', '", args), text);
			Assert.That(options.Host, Is.EqualTo("host"));
			Assert.That(options.UserName, Is.Null, "Username should be null");
		}

		[Test]
		public void Should_succeed_with_only_a_user_and_host_arg()
		{
			var options = new Options();
			var args = new[] {"user@host",};

			string text;
			Assert.That(options.TryParse(args, out text), Is.True, "Args: '{0}', Errors: {1}", string.Join("', '", args), text);
			Assert.That(options.Host, Is.EqualTo("host"));
			Assert.That(options.UserName, Is.EqualTo("user"));
		}

		[Test]
		public void Should_succeed_with_port_as_number()
		{
			var options = new Options();
			var args = new[] {"-p", "23", "host",};

			string text;
			Assert.That(options.TryParse(args, out text), Is.True, "Args: '{0}', Errors: {1}", string.Join("', '", args), text);
		}

		[Test]
		public void Should_succeed_with_various_noise_settings()
		{
			string text;

			var options = new Options();
			Assert.That(options.TryParse(new[] {"-v", "host",}, out text), Is.True, "-v not working");
			Assert.That(options.Verbose, Is.True, "Verbose setting not set from -v.");

			options = new Options();
			Assert.That(options.TryParse(new[] {"--verbose", "host",}, out text), Is.True, "--verbose not working");
			Assert.That(options.Verbose, Is.True, "Verbose setting not set from --verbose.");

			options = new Options();
			Assert.That(options.TryParse(new[] {"-q", "host",}, out text), Is.True, "-q not working");
			Assert.That(options.Quiet, Is.True, "Quiet setting not set from -q.");

			options = new Options();
			Assert.That(options.TryParse(new[] {"--quiet", "host",}, out text), Is.True, "--quiet not working");
			Assert.That(options.Quiet, Is.True, "Quiet setting not set from --quiet.");
		}
	}
}
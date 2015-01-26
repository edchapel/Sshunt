using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using CommandLine;
using CommandLine.Text;

using Newtonsoft.Json;

using NLog;

namespace Sshunt
{
	internal class Options
	{
		public static readonly Parser DefaultParser = new Parser(settings =>
		                                                         {
			                                                         settings.CaseSensitive = true;
			                                                         settings.IgnoreUnknownArguments = false;
			                                                         settings.MutuallyExclusive = true;
		                                                         });

		private readonly Parser _parser;
		private IEnumerable<ForwardedPortPromise> _forwardedPorts;
		private string _host;
		private IList<string> _portParseErrors;

		public Options(Parser parser = null)
		{
			_parser = parser ?? DefaultParser;
			LogLevel = LogLevel.Info;
		}

		[Option('p',
			DefaultValue = 22,
			MetaValue = "PORT",
			HelpText = "Port to connect to on the remote host.")]
		public int Port { get; set; }

		[Option('i',
			MutuallyExclusiveSet = "credential",
			MetaValue = "identity_file",
			HelpText = "Selects a file from which the identity (private key) for public key authentication is read. " +
			           "The default is  ~/.ssh/id_dsa, ~/.ssh/id_ecdsa, ~/.ssh/id_ed25519 and ~/.ssh/id_rsa (protocol version 2)." +
			           "Cannot be used with --password option.")]
		public string IdentityFile { get; set; }

		[Option("password",
			MutuallyExclusiveSet = "credential",
			MetaValue = "PASSWORD",
			HelpText = "Not recommended for security reasons. Use an identity file instead. Cannot be used with -i option. " +
			           "Cannot be used when Sshunt is running as a Windows service.")]
		[JsonIgnore]
		public string Password { get; set; }

		[Option("key-passphrase",
			MetaValue = "PASSPHRASE",
			HelpText = "Passphrase to the identity_file. Better to use an identity_file without a passphrase for headless operation. " +
			           "Cannot be used when Sshunt is running as a Windows service.")]
		[JsonIgnore]
		public string KeyPassphrase { get; set; }

		[Option('v', "verbose",
			MutuallyExclusiveSet = "noise",
			HelpText = "Verbose mode. Causes sshunt to print debugging messages about its progress. " +
			           "This is helpful in debugging connection, authentication, and configuration problems.")]
		public bool Verbose { get; set; }

		[Option('q', "quiet",
			MutuallyExclusiveSet = "noise",
			HelpText = "Quiet mode. Causes most warning and diagnostic messages to be suppressed. Header is not shown.")]
		[JsonIgnore]
		public bool Quiet { get; set; }

		[Option("install-svc",
//			MetaValue = "SERVICENAME",
			MutuallyExclusiveSet = "service",
			HelpText = "Installs Sshunt as a Windows service, with the name specified, for even more persistency. All required configuration items must be provided in this call.")]
		[JsonIgnore]
		public bool InstallService { get; set; }

		[Option("uninstall-svc",
//			MetaValue = "SERVICENAME",
			MutuallyExclusiveSet = "service",
			HelpText = "Uninstalls any Sshunt Windows services found with this name.")]
		[JsonIgnore]
		public bool UninstallService { get; set; }

		[Option("start-svc",
//			MetaValue = "SERVICENAME",
			MutuallyExclusiveSet = "service",
			HelpText = "Starts the named Sshunt Windows service.")]
		[JsonIgnore]
		public bool StartService { get; set; }

		[Option("stop-svc",
//			MetaValue = "SERVICENAME",
			MutuallyExclusiveSet = "service",
			HelpText = "Stops the named Sshunt Windows service.")]
		[JsonIgnore]
		public bool StopService { get; set; }

		[ValueOption(0)]
		public string Host
		{
			get { return _host; }
			set
			{
				if (!string.IsNullOrWhiteSpace(value))
				{
					_host = value;

					// Support 'user@host' or 'host'
					var match = Regex.Match(value, @"^(?:(?<user>[^@]+)@)?(?<host>.+)$", RegexOptions.ExplicitCapture);
					if (match.Success)
					{
						HostName = match.Groups["host"].Value;

						// User is optional
						var user = match.Groups["user"];
						UserName = user.Success ? user.Value : null;
						return;
					}
				}

				HostName = null;
				UserName = null;
			}
		}

		[ValueList(typeof (List<string>))]
		[JsonIgnore]
		public IList<string> OverFlow { get; set; }

		/// <summary>
		/// Null unless the CommondLine Parser finds an error. There can still be errors with this null.
		/// </summary>
		[ParserState]
		[JsonIgnore]
		public IParserState LastParserState { get; set; }

		[JsonIgnore]
		public bool AreExtendedOptionsValid
		{
			get { return !GetExtendedErrors(BaseSentenceBuilder.CreateBuiltIn()).Any(); }
		}

		[JsonIgnore]
		public string HostName { get; private set; }

		[JsonIgnore]
		public string UserName { get; private set; }

		public LogLevel LogLevel { get; private set; }

		[OptionList('R',
			MetaValue = "[bind_address:]port:host:hostport",
			HelpText = "Specifies that the given port on the remote (server) host is to be forwarded to the given host and port on the local side. " +
			           "This works by allocating a socket to listen to port on the remote side, and whenever a connection is made to this port, " +
			           "the connection is forwarded over the secure channel, and a connection is made to host port hostport from the local machine. " +
			           "Port forwardings can also be specified in the configuration file. Privileged ports can be forwarded only when logging in as " +
			           "root on the remote machine. IPv6 addresses can be specified by enclosing the address in square brackets. " +
			           "By default, the listening socket on the server will be bound to the loopback interface only. This may be overridden by " +
			           "specifying a bind_address. An empty bind_address, or the address ‘*’, indicates that the remote socket should listen on all " +
			           "interfaces. Specifying a remote bind_address will only succeed if the server's GatewayPorts option is enabled (see sshd_config(5)).")]
		[JsonProperty]
		public IList<string> RemoteForwards { get; set; }

		[OptionList('L',
			MetaValue = "[bind_address:]port:host:hostport",
			HelpText = "Specifies that the given port on the local (client) host is to be forwarded to the given host and port on the remote side." +
			           " This works by allocating a socket to listen to port on the local side, optionally bound to the specified bind_address." +
			           " Whenever a connection is made to this port, the connection is forwarded over the secure channel, and a connection is made to" +
			           " host port hostport from the remote machine. Port forwardings can also be specified in the configuration file. IPv6 addresses" +
			           " can be specified by enclosing the address in square brackets. Only the superuser can forward privileged ports. By default, the" +
			           " local port is bound in accordance with the GatewayPorts setting. However, an explicit bind_address may be used to bind the" +
			           " connection to a specific address. The bind_address of \"localhost\" indicates that the listening port be bound for local use" +
			           " only, while an empty address or ‘*’ indicates that the port should be available from all interfaces.")]
		[JsonProperty]
		public IList<string> LocalForwards { get; set; }

		[JsonIgnore]
		public bool AreForWindowsService
		{
			get
			{
				return StartService || StopService || InstallService || UninstallService;
//				return !string.IsNullOrWhiteSpace(Start) ||
//				       !string.IsNullOrWhiteSpace(Stop) ||
//				       !string.IsNullOrWhiteSpace(Install) ||
//				       !string.IsNullOrWhiteSpace(Uninstall);
			}
		}

		public bool TryParse(string[] args, out string helpText)
		{
			LogLevel = ParseNoise(args);
			if (!_parser.ParseArguments(args, this) || !AreExtendedOptionsValid)
			{
				// Only use the console width if the output is hooked up
				var displayWidth = Console.IsOutputRedirected ? int.MaxValue : Console.BufferWidth;

				helpText = GetUsageAndErrors(displayWidth);
				return false;
			}

			helpText = null;
			return true;
		}

		public string GetUsageAndErrors(int maximumDisplayWidth)
		{
			var helpText = HelpText.AutoBuild(this, x => x.MaximumDisplayWidth = maximumDisplayWidth);

			// Silly API allows only 5 lines at a time and a maximum width
			// This uses more width/lines than supported... so do them one at a time then.
			helpText.AddPreOptionsLine(@" "); // New line looks nicer after the copyright. Space is to avoid an exception in SSH.NET.
			helpText.AddPreOptionsLine("Usage: sshunt [options] [user@]host");
			helpText.AddPreOptionsLine("       sshunt <install-svc|uninstall-svc|start-svc|stop-svc> SERVICENAME");
			helpText.AddPreOptionsLine("              [options] [user@]host");
			helpText.AddPreOptionsLine("Options:      [-i identity_file | --password PASSWORD]");
			helpText.AddPreOptionsLine("              [--key-passphrase PASSPHRASE]");
			helpText.AddPreOptionsLine("              [-L [bind_address:]port:host:hostport]");
			helpText.AddPreOptionsLine("              [-R [bind_address:]port:host:hostport]");
			helpText.AddPreOptionsLine("              [-v|--verbose|-q|--quiet] [-p PORT]");

			var hasErrorHeader = false;
			if (LastParserState != null && LastParserState.Errors.Any())
			{
				hasErrorHeader = true;
				HelpText.DefaultParsingErrorsHandler(this, helpText);
			}

			var errors = GetExtendedErrors(helpText.SentenceBuilder).ToArray();
			if (errors.Any())
			{
				if (!hasErrorHeader)
				{
					helpText.AddPreOptionsLine(helpText.SentenceBuilder.ErrorsHeadingText);
				}

				foreach (var error in errors)
				{
					helpText.AddPreOptionsLine(error);
				}
			}

			return helpText;
		}

		/// <summary>
		/// Sets <see cref="Password"/> to null before returning the value.
		/// </summary>
		public string GetPassword()
		{
			var password = Password;
			Password = null;
			return password;
		}

		/// <summary>
		/// Sets <see cref="KeyPassphrase"/> to null before returning the value.
		/// </summary>
		public string GetKeyPassphrase()
		{
			var keyPassphrase = KeyPassphrase;
			KeyPassphrase = null;
			return keyPassphrase;
		}

		public IEnumerable<ForwardedPortPromise> GetForwardedPorts()
		{
			if (_forwardedPorts != null) return _forwardedPorts;

			if (RemoteForwards == null && LocalForwards == null)
			{
				_portParseErrors = new string[0];
				return _forwardedPorts = Enumerable.Empty<ForwardedPortPromise>();
			}

			var regex = new Regex(@"^(?:(?<boundHost>[^:]+):)?(?<boundPort>\d{1,5}):(?<host>[^:]+):(?<hostPort>\d{1,5})$", RegexOptions.ExplicitCapture);

			var portParseErrors = new List<string>();
			var ports = new[]
			            {
				            new {Forwards = RemoteForwards, CreatePromise = (Func<string, uint, string, uint, ForwardedPortPromise>) ForwardedPortPromise.CreateRemote,},
				            new {Forwards = LocalForwards, CreatePromise = (Func<string, uint, string, uint, ForwardedPortPromise>) ForwardedPortPromise.CreateLocal,},
			            }
				.Where(x => x.Forwards != null)
				.SelectMany(x => x.Forwards
				                  .Select(forward =>
				                          {
					                          var match = regex.Match(forward);
					                          if (match.Success)
					                          {
						                          uint boundPort;
						                          uint hostPort;

						                          if (uint.TryParse(match.Groups["boundPort"].Value, out boundPort) &&
						                              uint.TryParse(match.Groups["hostPort"].Value, out hostPort))
						                          {
							                          var boundHostMatch = match.Groups["boundHost"];
							                          return x.CreatePromise(boundHostMatch.Success ? boundHostMatch.Value : string.Empty,
							                                                 boundPort,
							                                                 match.Groups["host"].Value,
							                                                 hostPort);
						                          }
					                          }
					                          else
					                          {
						                          // Record this as an error
						                          portParseErrors.Add(string.Format("unable to parse port forward '{0}'", forward));
					                          }

					                          return null;
				                          }))
				.Where(p => p != null)
				.ToList();

			if (portParseErrors.Any())
			{
				_portParseErrors = portParseErrors;
				return _forwardedPorts = Enumerable.Empty<ForwardedPortPromise>();
			}

			_portParseErrors = new string[0];
			return _forwardedPorts = ports.AsReadOnly();
		}

		private IEnumerable<string> GetExtendedErrors(BaseSentenceBuilder sentenceBuilder)
		{
			if (string.IsNullOrWhiteSpace(HostName))
			{
				// Two leading spaces is standard for CommandLine
				yield return string.Format("  hostname {0}", sentenceBuilder.RequiredOptionMissingText);
			}

			if (string.IsNullOrWhiteSpace(UserName))
			{
				// Two leading spaces is standard for CommandLine
				yield return string.Format("  hostname {0}", sentenceBuilder.RequiredOptionMissingText);
			}

			if (AreForWindowsService)
			{
				if (!string.IsNullOrWhiteSpace(Password))
				{
					yield return "  password cannot be used with Windows service related arguments";
				}

				if (!string.IsNullOrWhiteSpace(KeyPassphrase))
				{
					yield return "  key-passphrase cannot be used with Windows service related arguments";
				}

				if (new[] {StartService, StopService, InstallService, UninstallService}.Where(x => x).Count() > 1)
				{
					yield return "  only one Windows service may be called at once";
				}
			}

			foreach (var overFlow in OverFlow)
			{
				// Two leading spaces is standard for CommandLine
				yield return string.Format("  unknown {0} {1}", sentenceBuilder.OptionWord, overFlow);
			}

			// Parse the ports now
			GetForwardedPorts();
			foreach (var portParseError in _portParseErrors)
			{
				// Two leading spaces is standard for CommandLine
				yield return portParseError;
			}
		}

		/// <summary>
		/// The CommandLine API doesn't support this ssh-like verbosity paramter.
		/// So we do it here imperatively.
		/// </summary>
		private static LogLevel ParseNoise(IEnumerable<string> args)
		{
			var logLevels = args.Select(a =>
			                            {
				                            switch (a.ToLower())
				                            {
					                            case "-v":
					                            case "--verbose":
						                            return LogLevel.Debug;

					                            case "-vv":
					                            case "-vvv":
						                            return LogLevel.Trace;

					                            case "-q":
					                            case "--quiet":
						                            return LogLevel.Error;
					                            default:
						                            return null;
				                            }
			                            })
			                    .Where(x => x != null)
			                    .OrderByDescending(x => x.Ordinal);

			return logLevels.FirstOrDefault() ?? LogLevel.Info;
		}
	}
}
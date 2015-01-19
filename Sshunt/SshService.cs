using System;
using System.Threading;

using NLog;

using Renci.SshNet;

namespace Sshunt
{
	/// <summary>
	///  Encapsulates the process of connecting and staying connected to the SSH server.
	/// </summary>
	internal class SshService
	{
		private readonly Logger _logger;
		private readonly Options _options;
		private readonly SshClient _sshClient;
		private ManualResetEventSlim _signal;

		public SshService(SshClient sshClient, Options options)
		{
			_sshClient = sshClient;
			_options = options;
			_logger = LogManager.GetCurrentClassLogger();
		}

		public void Connect()
		{
			if (_signal != null)
			{
				throw new InvalidOperationException("SshService is already connected.");
			}

			try
			{
				var allowReconnect = true;
				using (_signal = new ManualResetEventSlim(false))
				{
					Console.CancelKeyPress += (s, e) =>
					                          {
						                          e.Cancel = true;
						                          if (_signal != null)
						                          {
							                          _signal.Set();
						                          }
					                          };

					_sshClient.ErrorOccurred += (s, e) =>
					                            {
						                            _logger.Error("Remote error: {0}", e.Exception);
						                            _logger.Info("Reconnecting...");

						                            // There was a version of SSH.NET that unreliably and inaccurately reported itself as connected
						                            if (_sshClient.IsConnected)
						                            {
							                            // So, if connected, force a disconnect...
							                            try
							                            {
								                            _sshClient.Disconnect();
							                            }
								                            // ReSharper disable once EmptyGeneralCatchClause
							                            catch {} // and ignore any exceptions
						                            }

						                            allowReconnect = true;
						                            if (_signal != null)
						                            {
							                            _signal.Set();
						                            }
					                            };

					_sshClient.ConnectionInfo.AuthenticationBanner += (s, e) => Console.WriteLine(e.BannerMessage);

					_logger.Info("Connecting...");

					while (true)
					{
						_signal.Reset();
						allowReconnect = false;

						Connect(_sshClient, _signal);

						if (!allowReconnect) break;
					}

					if (_sshClient.IsConnected)
					{
						_logger.Info("Disconnecting...");
						_sshClient.Disconnect();
					}

					_logger.Info("Disconnected.");
				}
			}
			finally
			{
				_signal = null;
			}
		}

		private void Connect(SshClient sshClient, ManualResetEventSlim signal)
		{
			sshClient.Connect();


			foreach (var forwardedPortPromise in _options.GetForwardedPorts())
			{
				_logger.Debug("ForwardedPort: {0}", forwardedPortPromise);
				var forwardedPort = forwardedPortPromise.Build();
				sshClient.AddForwardedPort(forwardedPort);
				forwardedPort.Exception += (s, e) => _logger.Warn("Exception on forwarded port {0}", e.Exception);
				forwardedPort.RequestReceived += (s, e) => _logger.Trace("Request from '{0}' on port '{1}'.", e.OriginatorHost, e.OriginatorPort);
				forwardedPort.Start();
			}

			using (var shellStream = sshClient.CreateShellStream("Sshunt", 80, 40, 80, 40, 80*40*25))
			{
				shellStream.DataReceived += (s, e) =>
				                            {
					                            var line = sshClient.ConnectionInfo.Encoding.GetString(e.Data);
					                            _logger.Info(line);
				                            };

				signal.Wait();
			}
		}
	}
}
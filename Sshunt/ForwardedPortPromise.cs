using System;

using Renci.SshNet;

namespace Sshunt
{
	internal class ForwardedPortPromise
	{
		private readonly Func<ForwardedPort> _factory;

		private ForwardedPortPromise(Func<ForwardedPort> factory, string boundHost, uint boundPort, string host, uint port, Type type)
		{
			_factory = factory;
			BoundHost = string.IsNullOrWhiteSpace(boundHost) ? string.Empty : boundHost;
			BoundPort = boundPort;
			Host = host;
			Port = port;
			Type = type;
		}

		public string BoundHost { get; private set; }
		public uint BoundPort { get; private set; }
		public string Host { get; private set; }
		public uint Port { get; private set; }
		public Type Type { get; private set; }

		public ForwardedPort Build()
		{
			return _factory();
		}

		public override string ToString()
		{
			var type = Type.Name.Replace("ForwardedPort", string.Empty);
			return BoundHost == string.Empty
				? string.Format("{0} {1} => {2}:{3}", type, BoundPort, Host, Port)
				: string.Format("{0} {1}:{2} => {3}:{4}", type, BoundHost, BoundPort, Host, Port);
		}

		public static ForwardedPortPromise CreateRemote(string boundHost, uint boundPort, string host, uint port)
		{
			return Create(boundHost, boundPort, host, port, () => new ForwardedPortRemote(boundHost, boundPort, host, port));
		}

		public static ForwardedPortPromise CreateLocal(string boundHost, uint boundPort, string host, uint port)
		{
			return Create(boundHost, boundPort, host, port, () => new ForwardedPortLocal(boundHost, boundPort, host, port));
		}

		private static ForwardedPortPromise Create<T>(string boundHost, uint boundPort, string host, uint port, Func<T> factory)
			where T : ForwardedPort
		{
			return new ForwardedPortPromise(factory, boundHost, boundPort, host, port, typeof (T));
		}
	}
} ;
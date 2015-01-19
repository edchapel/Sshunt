# Sshunt

A simple application to maintain a persistent SSH connection. Pronounced like 'shunt'.

> The origin of the term is in the verb 'to shunt' meaning to turn away or follow a different path. -- [Wikipedia](http://en.wikipedia.org/wiki/Shunt_%28electrical%29)

Sshunt is most useful to open a SSH connection to tunnel one or more ports. In this way, it can serve as a clever and lightweight VPN.

### Purpose

Stay connected to an SSH host and reconnect when the connection fails.

### Usage

The options are meant to mimic [OpenBSD `ssh`](http://www.openbsd.org/cgi-bin/man.cgi/OpenBSD-current/man1/slogin.1?query=ssh&sec=1).

    sshunt host
    sshunt [-i identity_file | --password PASSWORD]
           [--key-passphrase PASSPHRASE]
           [-L [bind_address:]port:host:hostport]
           [-R [bind_address:]port:host:hostport]
           [-v|--verbose|-q|--quiet] [-p PORT] [user@]hostname

### Thanks

* [SSH.NET](http://sshnet.codeplex.com/)
* [CommandLine](https://github.com/gsscoder/commandline)
* [NUnit](http://nunit.org/)
* [NSubstitute](http://nsubstitute.github.io/)
* [NLog](https://github.com/NLog/NLog/)

### Roadmap

* Implement as Windows Service
* Read `~/.ssh/config` for default settings
* Support multiple connections
* Support config file

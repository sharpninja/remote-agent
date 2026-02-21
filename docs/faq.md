# Frequently Asked Questions

## Service Management

### How do I start the service when systemd is not available?

This applies to environments such as WSL (Windows Subsystem for Linux) where
systemd is not the active init system (i.e. `/run/systemd/system` does not
exist).

#### Automatic start on install

When you install the `remote-agent-service` .deb package on a non-systemd
system, the `postinst` script automatically starts the service using
`start-stop-daemon` and writes a PID file to `/run/remote-agent.pid`.

On WSL it also registers an auto-start entry in `/etc/wsl.conf`:

```ini
[boot]
command = "su -s /bin/sh remote-agent /usr/lib/remote-agent/service/wsl-start.sh"
```

After this is written, the service starts automatically every time the WSL
instance launches.

> **Tip:** For full `systemctl` support in WSL, add `systemd = true` under
> `[boot]` in `/etc/wsl.conf`, then run `wsl --shutdown` to restart the
> distro.

#### Starting manually

Use `remote-agent-ctl`, installed with the package:

```bash
sudo remote-agent-ctl start
```

The script works with or without systemd — it detects the active init system
automatically and delegates to `systemctl` when available, or falls back to
`start-stop-daemon` directly.

You can also stop, restart, and check status with the same script:

```bash
sudo remote-agent-ctl stop
sudo remote-agent-ctl restart
sudo remote-agent-ctl status
```

Environment overrides in `/etc/remote-agent/environment` (e.g.
`ASPNETCORE_URLS`) are sourced automatically by the script when running without
systemd.

#### Checking whether the service is running

```bash
_pid=$(cat /run/remote-agent.pid 2>/dev/null)
kill -0 "$_pid" 2>/dev/null && echo "running (pid $_pid)" || echo "not running"
```

#### Viewing logs

```bash
tail -f /var/log/remote-agent/service.log
```

Errors are written to the same file. A separate `.err` file is used only when
the service is started via the `daemonize` fallback (present on some
distributions such as Pengwin):

```bash
tail -f /var/log/remote-agent/service.err
```

#### Configuration

Runtime configuration is loaded from `/etc/remote-agent/appsettings.json`.
Environment overrides (e.g. `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`) are
read from `/etc/remote-agent/environment`. Both files are preserved during
package upgrades.

The service runs as the `remote-agent` system user with data stored in
`/var/lib/remote-agent/` and logs in `/var/log/remote-agent/`.

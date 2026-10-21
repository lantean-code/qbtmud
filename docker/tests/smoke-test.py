#!/usr/bin/env python3
"""Exercise the built image with disposable Docker volumes and no external network."""
import argparse
import json
import os
import re
import subprocess
import time
import uuid

parser = argparse.ArgumentParser()
parser.add_argument("image")
args = parser.parse_args()
docker = os.environ.get("QBTMUD_DOCKER", "docker")
prefix = "qbtmud-test-" + uuid.uuid4().hex[:12]
containers = []
volumes = []


def run(*arguments, input=None, check=True):
    result = subprocess.run([docker, *arguments], input=input, text=True,
                            encoding="utf-8", capture_output=True)
    if check and result.returncode:
        # Curl arguments may contain the disposable password; do not print them.
        raise RuntimeError("Docker operation failed: " + result.stderr.strip())
    return result


def volume(suffix):
    name = prefix + "-" + suffix
    run("volume", "create", name)
    volumes.append(name)
    return name


def helper(config, script, extra=()):
    return run("run", "--rm", "--network", "none", "--entrypoint", "/bin/bash",
               "-v", config + ":/config", *extra, args.image, "-c", script).stdout


def start(suffix, config, extra=()):
    name = prefix + "-" + suffix
    run("run", "-d", "--name", name, "--network", "none",
        "-e", "PUID=1234", "-e", "PGID=1235", "-e", "TZ=Europe/London",
        "-e", "WEBUI_PORT=18080", "-e", "TORRENTING_PORT=16881",
        "-v", config + ":/config", "-v", downloads + ":/downloads", *extra, args.image)
    containers.append(name)
    return name


def curl(name, path, *options):
    return run("exec", name, "curl", "--fail", "--silent", "--show-error",
               *options, "http://127.0.0.1:18080" + path).stdout


def ready(name):
    for _ in range(90):
        status = json.loads(run("inspect", name).stdout)[0]["State"]
        if not status["Running"]:
            logs = run("logs", name)
            diagnostic = "\n".join(line for line in (logs.stdout + logs.stderr).splitlines()
                                   if line.startswith("[qbtmud]"))
            raise RuntimeError("Container exited before readiness: " + diagnostic)
        if run("exec", name, "curl", "-fsS", "http://127.0.0.1:18080/", check=False).returncode == 0:
            return
        time.sleep(1)
    raise RuntimeError("Container did not become ready.")


def rejected(name):
    for _ in range(30):
        state = json.loads(run("inspect", name).stdout)[0]["State"]
        log_result = run("logs", name)
        logs = log_result.stdout + log_result.stderr
        if "Startup validation passed" in logs:
            raise AssertionError("Invalid startup passed its gate.")
        if state["Running"]:
            processes = run("top", name, "-eo", "comm", check=False).stdout
            assert "qbittorrent-nox" not in processes, "qBittorrent started before failure."
            assert run("exec", name, "curl", "-s", "--max-time", "1", "http://127.0.0.1:18080/",
                       check=False).returncode != 0, "WebUI listener started before failure."
            time.sleep(1)
        else:
            assert "[qbtmud]" in logs, "Failure did not include a qbtmud diagnostic."
            assert state["ExitCode"] != 0, "Failed startup returned a successful exit code."
            return
    raise AssertionError("Invalid startup did not stop the container.")


try:
    # Execute config tests against the base image's actual Bash/awk/coreutils.
    root = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
    run("run", "--rm", "--network", "none", "--entrypoint", "/bin/bash",
        "-v", root + ":/repo:ro", args.image, "/repo/docker/tests/test-config.sh")
    downloads = volume("downloads")
    config = volume("config")
    name = start("fresh", config)
    ready(name)
    index = curl(name, "/")
    assert "blazor.webassembly.js" in index and "MudBlazor" in index, "Default WebUI served."
    assert curl(name, "/appsettings.json")
    assert curl(name, "/_framework/blazor.webassembly.js")
    assert curl(name, "/i18n/webui_en.json")
    logs = run("logs", name).stdout
    match = re.search(r"temporary password.*?:\s*(\S+)", logs)
    assert match, "Initial temporary password was not available."
    cookie = "/tmp/qbtmud-test-cookie"
    login = curl(name, "/api/v2/auth/login", "-c", cookie, "--data-urlencode", "username=admin",
                 "--data-urlencode", "password=" + match.group(1))
    assert login.strip() in ("Ok.", ""), "Initial login response: " + repr(login)
    run("exec", name, "grep", "-q", "SID", cookie)
    password = uuid.uuid4().hex
    preferences = {"web_ui_password": password, "save_path": "/downloads/preserved/"}
    curl(name, "/api/v2/app/setPreferences", "-b", cookie,
         "--data-urlencode", "json=" + json.dumps(preferences))
    current = json.loads(curl(name, "/api/v2/app/preferences", "-b", cookie))
    preserved_path = current["save_path"]
    assert preserved_path.rstrip("/") == "/downloads/preserved", "Save path update was not applied."
    assert current["alternative_webui_enabled"] and current["web_ui_port"] == 18080
    run("exec", name, "/usr/local/libexec/qbtmud/configure", "/config/qBittorrent/qBittorrent.conf", "verify")
    assert current["listen_port"] == 16881
    run("stop", "--time", "15", name)
    # No running qBittorrent while deliberately changing the managed keys.
    helper(config, "sed -i 's|^WebUI\\\\RootFolder=.*|WebUI\\\\RootFolder=/wrong|; "
           "s|^WebUI\\\\AlternativeUIEnabled=.*|WebUI\\\\AlternativeUIEnabled=false|' "
           "/config/qBittorrent/qBittorrent.conf")
    # Prove this test actually introduced wrong values before restarting.
    assert "WebUI\\RootFolder=/wrong" in helper(config, "cat /config/qBittorrent/qBittorrent.conf")
    run("start", name)
    ready(name)
    assert curl(name, "/api/v2/auth/login", "-c", cookie, "--data-urlencode", "username=admin",
                "--data-urlencode", "password=" + password).strip() in ("Ok.", "")
    run("exec", name, "grep", "-q", "SID", cookie)
    current = json.loads(curl(name, "/api/v2/app/preferences", "-b", cookie))
    assert current["alternative_webui_enabled"]
    run("exec", name, "/usr/local/libexec/qbtmud/configure", "/config/qBittorrent/qBittorrent.conf", "verify")
    assert current["save_path"] == preserved_path
    run("exec", name, "s6-setuidgid", "abc", "touch", "/downloads/permission-check")
    run("stop", "--time", "15", name)
    helper(config, "/usr/local/libexec/qbtmud/configure /config/qBittorrent/qBittorrent.conf verify")
    print("PASS: first boot, served assets, authentication, restart, setting repair and mapped permissions")

    broken = volume("malformed")
    helper(broken, "mkdir -p /config/qBittorrent; printf '[Preferences]\\ninvalid\\n' > /config/qBittorrent/qBittorrent.conf")
    rejected(start("malformed", broken))
    assert "invalid" in helper(broken, "cat /config/qBittorrent/qBittorrent.conf")

    compliant = "[Preferences]\nWebUI\\RootFolder=/opt/qbtmud\nWebUI\\AlternativeUIEnabled=true\n"
    multiline = volume("multiline-config")
    original = '[Preferences]\nOther="prefix\nWebUI\\RootFolder=/opt/qbtmud\nWebUI\\AlternativeUIEnabled=true\nSuffix=end"\n'
    run("run", "--rm", "--network", "none", "--entrypoint", "/bin/bash", "-i",
        "-v", multiline + ":/config", args.image, "-c",
        "mkdir -p /config/qBittorrent; cat > /config/qBittorrent/qBittorrent.conf", input=original)
    rejected(start("multiline", multiline))
    assert helper(multiline, "cat /config/qBittorrent/qBittorrent.conf") == original

    for suffix, alias in (
        ("padded-section", "[ Preferences ]\nWebUI\\RootFolder=/wrong\n"),
        ("escaped-key", "%57ebUI\\AlternativeUIEnabled=false\n"),
    ):
        alias_config = volume(suffix + "-config")
        original = compliant + alias
        run("run", "--rm", "--network", "none", "--entrypoint", "/bin/bash", "-i",
            "-v", alias_config + ":/config", args.image, "-c",
            "mkdir -p /config/qBittorrent; cat > /config/qBittorrent/qBittorrent.conf", input=original)
        rejected(start(suffix, alias_config))
        assert helper(alias_config, "cat /config/qBittorrent/qBittorrent.conf") == original

    # Mount a copied UI tree so selective asset loss retains index.html and its directories.
    for suffix, damage in (
        ("missing-boot", "rm -f /opt/qbtmud/public/_framework/blazor.webassembly.js"),
        ("empty-framework", "find /opt/qbtmud/public/_framework -type f -delete"),
        ("empty-boot", ": > /opt/qbtmud/public/_framework/dotnet.js"),
        ("corrupt-runtime", "printf corrupt > /opt/qbtmud/public/_framework/dotnet.native.wasm"),
    ):
        assets = volume(suffix + "-assets")
        run("run", "--rm", "--network", "none", "--entrypoint", "/bin/bash",
            "-v", assets + ":/opt/qbtmud/public", args.image, "-c", damage)
        rejected(start(suffix, volume(suffix + "-config"), ("-v", assets + ":/opt/qbtmud/public",)))
    print("PASS: multiline records, Qt section/key aliases and missing, empty or corrupted boot assets are rejected")

    # Empty volume over the image UI exercises asset validation before edits.
    empty = volume("empty-ui")
    rejected(start("missing-ui", volume("missing-ui-config"), ("--mount", "type=volume,src=" + empty + ",dst=/opt/qbtmud,volume-nocopy",)))

    # A mounted empty script replaces the hook, but must not hide a prior success.
    skipped = volume("skipped-hook")
    run("run", "--rm", "--network", "none", "--entrypoint", "/bin/bash",
        "-v", skipped + ":/hooks", args.image, "-c", "touch /hooks/10-configure-qbtmud; chmod 755 /hooks/10-configure-qbtmud")
    rejected(start("skipped", volume("skipped-config"), ("--mount", "type=volume,src=" + skipped + ",dst=/custom-cont-init.d,volume-nocopy",)))

    readonly = volume("readonly")
    helper(readonly, "mkdir -p /config/qBittorrent; printf '[Preferences]\\n' > /config/qBittorrent/qBittorrent.conf; chmod 440 /config/qBittorrent/qBittorrent.conf")
    rejected(start("readonly", readonly))

    # Preserve /run/qbtmud between starts, then replace the hook with an empty one.
    state = volume("state")
    state_config = volume("state-config")
    stale_name = start("stale-prior", state_config, ("-v", state + ":/run/qbtmud",))
    ready(stale_name)
    run("stop", "--time", "15", stale_name)
    rejected(start("stale-skipped", state_config, ("-v", state + ":/run/qbtmud", "--mount",
                   "type=volume,src=" + skipped + ",dst=/custom-cont-init.d,volume-nocopy")))
    print("PASS: malformed/read-only config, missing UI, skipped hook and stale-success failures prevent service startup")
finally:
    for name in containers:
        run("rm", "-f", name, check=False)
    for name in volumes:
        run("volume", "rm", name, check=False)

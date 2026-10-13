#!/usr/bin/env python3
"""Retain the smoke-tested image without rebuilding it."""
import hashlib
import json
import sys
import tarfile
from pathlib import Path
from release import command, read, write
from image_archive import verify_saved_image

arch = sys.argv[1]
if arch not in ("amd64", "arm64"):
    raise ValueError("Unexpected architecture")
root = Path("docker/.build/publication")
folder = root / arch
folder.mkdir(parents=True, exist_ok=True)
image = json.loads(command("docker", "image", "inspect", "qbtmud:local").stdout)[0]
if image["Architecture"] != arch or image["Os"] != "linux":
    raise ValueError("Tested image has the wrong platform")
docker_archive = folder / "docker-image.tar"
command("docker", "save", "--output", str(docker_archive), "qbtmud:local")
with tarfile.open(docker_archive) as archive:
    config_digest = verify_saved_image(image, archive)
# Preserve Docker schema2/config bytes inside the OCI transport archive. Converting
# config media types would reserialise JSON and change its digest without a rebuild.
command("skopeo", "copy", "--format", "v2s2", "docker-archive:" + str(docker_archive),
        "oci-archive:" + str(folder / "image.tar"))
docker_archive.unlink()
inventory = command("docker", "run", "--rm", "--network", "none", "--entrypoint", "/bin/sh",
                    "qbtmud:local", "-c", "apk info -vv; /app/qbittorrent-nox --version; cat /build_version").stdout
(folder / "installed-software.txt").write_text(inventory, encoding="utf-8")
with tarfile.open(folder / "image.tar") as archive:
    index = json.load(archive.extractfile("index.json"))
    digest = index["manifests"][0]["digest"]
    manifest_bytes = archive.extractfile("blobs/sha256/" + digest[7:]).read()
    manifest = json.loads(manifest_bytes)
    if "sha256:" + hashlib.sha256(manifest_bytes).hexdigest() != digest:
        raise ValueError("Retained manifest checksum mismatch")
    if manifest["config"]["digest"] != config_digest:
        raise ValueError("Export changed the tested image configuration")
write(folder / "tested.json", {"architecture": arch, "imageId": config_digest,
                              "manifestDigest": digest, "inputs": read(root / "publication-inputs.json")})

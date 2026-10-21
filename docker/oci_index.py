"""Assemble an annotated OCI index without changing tested platform manifests."""
import hashlib
import json
from pathlib import Path
import re
import tarfile

INDEX_TYPE = "application/vnd.oci.image.index.v1+json"


def assemble(directory, inputs, annotation):
    directory = Path(directory)
    layout = directory / "index-layout"
    blobs = layout / "blobs/sha256"
    blobs.mkdir(parents=True, exist_ok=True)
    descriptors = []
    for arch in ("amd64", "arm64"):
        folder = directory / arch
        evidence = json.loads((folder / "tested.json").read_text(encoding="utf-8"))
        if evidence["inputs"] != inputs or evidence["architecture"] != arch:
            raise ValueError("Transferred test evidence does not match publication inputs")
        with tarfile.open(folder / "image.tar") as archive:
            index = json.load(archive.extractfile("index.json"))
            if len(index["manifests"]) != 1:
                raise ValueError("Expected a single retained platform manifest")
            descriptor = dict(index["manifests"][0])
            digest = descriptor["digest"]
            if not re.fullmatch(r"sha256:[a-f0-9]{64}", digest) or digest != evidence["manifestDigest"]:
                raise ValueError("Retained manifest digest does not match test evidence")
            manifest = json.load(archive.extractfile("blobs/sha256/" + digest[7:]))
            if manifest["config"]["digest"] != evidence["imageId"]:
                raise ValueError("Retained image configuration differs from test evidence")
            for member in archive.getmembers():
                if not member.isfile():
                    continue
                if member.name in ("index.json", "oci-layout"):
                    continue
                if not re.fullmatch(r"blobs/sha256/[a-f0-9]{64}", member.name):
                    raise ValueError("Unexpected OCI archive entry")
                data = archive.extractfile(member).read()
                if hashlib.sha256(data).hexdigest() != member.name.rsplit("/", 1)[1]:
                    raise ValueError("OCI blob checksum mismatch")
                (blobs / member.name.rsplit("/", 1)[1]).write_bytes(data)
            config = json.loads((blobs / manifest["config"]["digest"][7:]).read_bytes())
            if config["architecture"] != arch or config["os"] != "linux":
                raise ValueError("Retained platform configuration has the wrong architecture")
            descriptor["platform"] = {"os": "linux", "architecture": arch}
            descriptor.pop("annotations", None)
            descriptors.append(descriptor)
    index = {"schemaVersion": 2, "mediaType": INDEX_TYPE, "manifests": descriptors,
             "annotations": {annotation: json.dumps(inputs, separators=(",", ":"))}}
    data = json.dumps(index, separators=(",", ":")).encode()
    digest = "sha256:" + hashlib.sha256(data).hexdigest()
    (blobs / digest[7:]).write_bytes(data)
    (layout / "oci-layout").write_text('{"imageLayoutVersion":"1.0.0"}', encoding="utf-8")
    root = {"schemaVersion": 2, "mediaType": INDEX_TYPE, "manifests": [
        {"mediaType": INDEX_TYPE, "digest": digest, "size": len(data),
         "annotations": {"org.opencontainers.image.ref.name": "public"}}]}
    (layout / "index.json").write_text(json.dumps(root), encoding="utf-8")
    return layout, digest

"""Verify saved images using content identities, not Docker API default fields."""
import hashlib
import json
import re


def verify_saved_image(image, archive):
    saved = json.load(archive.extractfile("manifest.json"))
    if len(saved) != 1:
        raise ValueError("Expected one tested image in the Docker archive")
    config_bytes = archive.extractfile(saved[0]["Config"]).read()
    config = json.loads(config_bytes)
    digest = "sha256:" + hashlib.sha256(config_bytes).hexdigest()
    if (config["rootfs"]["diff_ids"] != image["RootFS"]["Layers"]
            or config["architecture"] != image["Architecture"] or config["os"] != image["Os"]):
        raise ValueError("Saved archive does not match the tested filesystem/platform")
    # Classic Docker identifies images by the exact configuration blob. Its
    # inspect API may add default fields that are absent from that original blob.
    if digest == image["Id"]:
        return digest
    # The containerd image store identifies an image by its manifest/index.
    # Verify that captured identity links to this exact configuration blob.
    root = json.load(archive.extractfile("index.json"))
    if not any(item["digest"] == image["Id"] for item in root["manifests"]):
        raise ValueError("Saved archive does not contain the tested image identity")

    def contains_config(blob_digest, depth=0):
        if depth > 4 or not re.fullmatch(r"sha256:[a-f0-9]{64}", blob_digest):
            raise ValueError("Invalid saved manifest identity")
        data = archive.extractfile("blobs/sha256/" + blob_digest[7:]).read()
        if "sha256:" + hashlib.sha256(data).hexdigest() != blob_digest:
            raise ValueError("Saved manifest checksum mismatch")
        value = json.loads(data)
        if "manifests" in value:
            return any(contains_config(item["digest"], depth + 1) for item in value["manifests"])
        return value.get("config", {}).get("digest") == digest

    if not contains_config(image["Id"]):
        raise ValueError("Saved configuration is not part of the tested image")
    return digest

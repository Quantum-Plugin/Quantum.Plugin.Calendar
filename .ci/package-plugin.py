"""Validate a published plugin and create its distributable archive."""

import hashlib
import importlib.util
import json
import re
import sys
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

spec = importlib.util.spec_from_file_location(
    "assets", Path(__file__).with_name("verify-plugin-assets.py")
)
assets = importlib.util.module_from_spec(spec)
spec.loader.exec_module(assets)
directory = Path(sys.argv[1]).resolve()
for pattern in ("*.staticwebassets.*.json", "*.pdb"):
    for path in directory.rglob(pattern):
        path.unlink()
assets.verify(directory)
manifest = json.loads((directory / "plugin.json").read_text(encoding="utf-8-sig"))
assembly = directory / "Quantum.CalendarPlugin.dll"
for encoding in ("utf-8", "utf-16-le"):
    content = assembly.read_bytes().decode(encoding, errors="ignore")
    if re.search(r"(?i)([a-z]:[\\/]Users[\\/]|/Users/|/home/[^/]+/|/tmp/)", content):
        raise ValueError("The plugin assembly contains a workstation or build-agent path")
for name in ("LICENSE", "NOTICE"):
    if not (directory / name).is_file():
        raise ValueError(f"Missing required attribution: {name}")
archive = directory.parent / f"{manifest['id']}-{manifest['version']}.zip"
with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED) as package:
    for path in sorted(directory.rglob("*")):
        if path.is_file():
            package.write(path, path.relative_to(directory).as_posix())
print(f"Created {archive.name}")

repository = Path(__file__).resolve().parents[1]
sdk_range = (repository / ".ci/sdk-version-support.txt").read_text(encoding="utf-8").strip()
sdk_packages = ET.parse(repository / "Directory.Packages.props").getroot().iter("PackageVersion")
sdk_pin = next(package.attrib["Version"] for package in sdk_packages if package.attrib.get("Include") == "Quantum.Plugin.Abstraction")
if not sdk_range or not re.fullmatch(r"\[[^\[\],()]+\]", sdk_pin):
    raise ValueError("Release metadata requires a declared SDK range and an exact SDK build pin")
digest = hashlib.sha256(archive.read_bytes()).hexdigest()
metadata = {"pluginId": manifest["id"], "version": manifest["version"], "buildSdkVersion": sdk_pin[1:-1], "sdkVersionSupport": sdk_range, "archive": archive.name, "sha256": digest}
(archive.parent / "release-metadata.json").write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8")
(archive.parent / "SHA256SUMS.txt").write_text(f"{digest}  {archive.name}\n", encoding="utf-8")

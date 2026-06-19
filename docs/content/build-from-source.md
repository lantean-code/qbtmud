# Build from Source

Build qbtmud locally from the repository.

## Prerequisites

You need:

- the .NET SDK version pinned in [`global.json`](https://github.com/lantean-code/qbtmud/blob/develop/global.json)
- the `wasm-tools` workload for that SDK
- a local checkout of the repository

Install the WebAssembly workload if needed:

```sh
dotnet workload install wasm-tools --skip-manifest-update
```

## Clone the repository

```sh
git clone https://github.com/lantean-code/qbtmud.git
cd qbtmud
```

## Restore and build

```sh
dotnet restore
dotnet build
```

## Publish the files for qBittorrent

```sh
dotnet publish src/Lantean.QBTMud/Lantean.QBTMud.csproj --configuration Release --output output/publish
mkdir -p output/alternative-ui/public
cp -a output/publish/wwwroot/. output/alternative-ui/public/
```

qBittorrent expects the alternative WebUI root folder to contain `public/`, so the published files are staged into `output/alternative-ui/public`.

## Use the local build

Point qBittorrent's alternative WebUI root folder at:

```text
output/alternative-ui
```

Do not point qBittorrent at `output/publish`, because that folder does not match the alternative WebUI layout qBittorrent expects.

For non-standard hosting, edit `output/alternative-ui/public/appsettings.json` before using the output. See [Advanced setup](advanced-setup.md) for the settings reference.

## Run tests

```sh
dotnet test
```

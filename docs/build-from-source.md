# Build from Source

This guide covers building qbtmud locally from the repository.

## Prerequisites

- the .NET SDK version pinned in [`global.json`](../global.json)
- the repository checked out locally

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

## Publish the WebUI files

```sh
dotnet publish src/Lantean.QBTMud/Lantean.QBTMud.csproj --configuration Release --output output/publish
mkdir -p output/alternative-ui/public
cp -a output/publish/wwwroot/. output/alternative-ui/public/
```

qBittorrent expects an alternative WebUI root folder that contains `public/`. The commands above stage the published output into `output/alternative-ui/public`.

## Use the local build in qBittorrent

Point qBittorrent's alternative WebUI root folder at `output/alternative-ui`.

## Run tests

If you are making local changes, run the test suite before treating the build as validated.

```sh
dotnet test
```

## Notes

This page is intentionally focused on the basic local build and publish flow.

If the build, packaging, or release process becomes more sophisticated, split those topics into dedicated documents rather than overloading this page.

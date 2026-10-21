# Bundled software and source locations

qbtmud and its container packaging are covered by the repository's MIT licence,
copied to `/opt/qbtmud/LICENSE`. This is an aggregate image: that licence does not
replace the licences of qBittorrent, LinuxServer's base or their dependencies.

The image retains the upstream filesystem, notices and package metadata. Its OCI
labels record the exact LinuxServer tag/index digest and qbtmud UI/packaging commits.

- qbtmud: https://github.com/lantean-code/qbtmud (use the recorded UI commit).
- LinuxServer qBittorrent recipe and scripts: https://github.com/linuxserver/docker-qbittorrent
  (use the recorded upstream tag).
- qBittorrent: https://github.com/qbittorrent/qBittorrent/releases and
  https://github.com/qbittorrent/qBittorrent/blob/release-5.2.4/COPYING.
  Its binary distribution is GPLv3-or-later, with its documented OpenSSL exception.
- LinuxServer obtains its static qBittorrent binary from
  https://github.com/userdocs/qbittorrent-nox-static. That project's matching
  release and build scripts identify the static binary's dependencies and sources.
- qBittorrent CLI: https://github.com/fedarovich/qbittorrent-cli.
- Alpine packages: https://pkgs.alpinelinux.org/ and https://gitlab.alpinelinux.org/alpine/aports.
- LinuxServer base: https://github.com/linuxserver/docker-baseimage-alpine.
- unrar: https://github.com/linuxserver/docker-unrar. Its licence includes
  restrictions on using its code to develop a RAR archiver.

Publication retains an installed-software inventory for each architecture. Before
redistributing a new upstream pin, review these records and provide corresponding
source/build materials required by the bundled licences through the distribution
site. Source links and this notice alone are not a claim that every redistribution
obligation has been fulfilled. Do not label the aggregate image as MIT-only.

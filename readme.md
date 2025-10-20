# qbtmud

qbtmud is a drop-in replacement for qBittorrent's default WebUI, implementing all of its functionality with a modern and user-friendly interface.

## Features

qbtmud replicates all core features of the qBittorrent WebUI, including:

- **Torrent Management** – Add, remove, and control torrents.
- **Tracker Control** – View and manage trackers.
- **Peer Management** – Monitor and manage peers connected to torrents.
- **File Prioritization** – Select and prioritize specific files within a torrent.
- **Speed Limits** – Set global and per-torrent speed limits.
- **RSS Integration** – Subscribe to RSS feeds for automated torrent downloads.
- **Search Functionality** – Integrated torrent search.
- **Sequential Downloading** – Download files in order for media streaming.
- **Super Seeding Mode** – Efficiently distribute torrents as an initial seeder.
- **IP Filtering** – Improve security by filtering specific IP addresses.
- **IPv6 Support** – Full support for IPv6 networks.
- **Bandwidth Scheduler** – Schedule bandwidth limits.
- **WebUI Access** – Remotely manage torrents through the WebUI.

![image](https://github.com/user-attachments/assets/c4e383fd-bff0-4367-b6de-79e19a632f11)
![image](https://github.com/user-attachments/assets/4ff56ed6-cc11-42cd-a070-23f086fd8821)
![image](https://github.com/user-attachments/assets/e321c5a2-ccf1-4205-828d-7ed7adade7dd)


For a detailed explanation of these features, refer to the [qBittorrent Options Guide](https://github.com/qbittorrent/qBittorrent/wiki/Explanation-of-Options-in-qBittorrent).

---

## Installation

To install qbtmud without building from source:

### 1. Download the Latest Release
- Go to the [qbtmud Releases](https://github.com/lantean-code/qbtmud/releases) page.
- Download the latest release archive for your operating system.

### 2. Extract the Archive
- Extract the contents of the downloaded archive to a directory of your choice.

### 3. Configure qBittorrent to Use qbtmud
- Open qBittorrent and navigate to `Tools` > `Options` > `Web UI`.
- Enable the option **"Use alternative WebUI"**.
- Set the **"Root Folder"** to the directory where you extracted qbtmud.
- Click **OK** to save the settings.

### 4. Access qbtmud
- Open your web browser and go to `http://localhost:8080` (or the port configured in qBittorrent).

For more detailed instructions, refer to the [Alternate WebUI Usage Guide](https://github.com/qbittorrent/qBittorrent/wiki/Alternate-WebUI-usage).

---

## Building from Source

To build qbtmud from source, you need to have the **.NET 9.0 SDK** installed on your system.

### 1. Clone the Repository
```sh
git clone https://github.com/lantean-code/qbtmud.git
cd qbtmud
```

### 2. Restore Dependencies
```sh
dotnet restore
```

### 3. Build and Publish the Application
```sh
dotnet publish --configuration Release
```

This will output the Web UI files to `Lantean.QBTMud\bin\Release\net9.0\publish\wwwroot`.

### 4. Configure qBittorrent to Use qbtmud
Follow the same steps as in the **Installation** section to set qbtmud as your WebUI.

### 5. Run qbtmud
Navigate to the directory containing the built files and run the application using the appropriate command for your OS.

By following these steps, you can set up qbtmud to manage your qBittorrent server with an improved web interface, offering better functionality and usability.

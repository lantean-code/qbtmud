Short answer: the remaining search‑plugin strings don’t exist in qBittorrent WebUI because our UI has **extra controls and a different flow**, not because WebUI is missing functionality.

What **does match** (already localized):
- `PluginSelectDlg`: “Search plugins”, “Uninstall”, “Check for updates”, “Close”
- `SearchPluginsTable`: “Enabled”, “Name”, “Version”, “Url”
- `SearchEngineWidget`: “There aren’t any search plugins installed.”

What **doesn’t have WebUI equivalents** (and why):
- **“Install from URL” / “Install from server path”**  
  WebUI uses a single “Install new plugin” action that opens a modal with **one** input:  
  “Plugin path:” + placeholder “URL or local directory” (PluginSourceDlg). We split it into two sections, so there’s no matching text.
- **“Enable” / “Disable”**  
  WebUI uses a context‑menu toggle labeled **“Enabled”** (no standalone “Enable/Disable” strings).
- **“Refresh plugins”**  
  No refresh button in WebUI; not in translations.
- **Table headers “Select”, “Identifier”, “Last update”, “Source”**  
  WebUI’s table is **Name / Version / Url / Enabled** only; these extra columns don’t exist.
- **“Not provided”**  
  Not in WebUI; they don’t show a “no URL” message.

So it’s **text too different + extra UI**, not missing WebUI functionality. If you want strict WebUI parity here, we’d need to change the UI to match their single‑input install flow and table columns. Otherwise, we should add these remaining strings to overrides.

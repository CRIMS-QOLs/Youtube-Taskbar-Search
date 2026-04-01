# Youtube search bar to taskbar

A lightweight, seamless Windows desktop application that acts as a quick search bar pinned beautifully underneath your active windows or right above your taskbar. 

This application offers both **YouTube** and **ChatGPT** specific search integrations. Switch seamlessly between them to instantly retrieve information or entertainment without opening a browser first.

## Features
- **Dual Search Engines:** Effortlessly toggle between YouTube and ChatGPT by clicking the right-side icon on the search bar.
- **ChatGPT Auto-Submission:** Automatically handles finding your open ChatGPT browser window and auto-sends your query.
- **Always Accessible:** Designed to stay firmly pinned underneath everything, so it behaves exactly like a part of your desktop background or taskbar. 
- **Lightweight System Footprint:** Actively trims and clears background memory when unfocused to ensure practically zero impact on system resources.
- **System Tray Management:** Easy to access and toggle visibility or "Run on Startup" state from the system tray (Notification Area).
- **Run on Startup:** Built-in settings via the Tray Icon to automatically start the app whenever you log into Windows, with a small delay so it always appears cleanly over the desktop wallpaper.

## Requirements
- Windows 10 or Windows 11
- .NET Framework / .NET Core Runtime (depending on the build target)

## Installation

### From Pre-compiled Installer
1. Download the latest installer from the [Releases](https://github.com/yourusername/YoutubeTaskbarSearch/releases) page.
2. Run `YoutubeTaskbarSearch_Installer.exe` and follow the instructions.
3. Once installed, it will automatically launch and place an icon in your system tray.

### Build from Source
To build from source, you will need Visual Studio or the .NET CLI environment.
1. Clone this repository: `git clone https://github.com/yourusername/YoutubeTaskbarSearch.git`
2. Open the solution (`YoutubeTaskbarSearch.csproj`) in Visual Studio.
3. Restore NuGet packages if any.
4. Build the solution and run.

## Usage
- The application will drop down directly onto your desktop.
- Click within the pill-shaped UI, type your query, and press **Enter**.
- Depending on the selected search engine:
  - **YouTube:** Your query will open in your default web browser on a YouTube search results page.
  - **ChatGPT:** Your query will open in your default browser, and the app will attempt to automatically submit it to the ChatGPT chat interface.
- To switch search engines, **click on the secondary/faded icon**.
- To move the window around (if needed), **click and hold** on the background of the search bar, then drag it to your desired position.

## Managing the App
- In the bottom right corner of your Windows taskbar (System Tray), look for the YouTube/ChatGPT icon.
- **Left-Click:** Show or hide the search bar.
- **Right-Click:** Open the context menu to toggle **"Run on Startup"** or to **Exit** the application securely.

## Contributing
Contributions are welcome! Feel free to open issues or submit pull requests for any improvements or bug fixes.

## License
MIT License. See `LICENSE` for more information.

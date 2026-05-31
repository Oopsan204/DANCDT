# WebView2 Runtime Setup Guide

To ensure the DACDT_2026 application runs correctly, you need the WebView2 Runtime installed on your system.

## Options for Setup

### Option 1: System-wide Installation (Recommended)
We have included a helper script to automate this for you.
1. Run the `download_webview2_runtime.ps1` script located in the application directory.
2. Select **Option 1**.
3. This will download and run the official Microsoft Evergreen Installer automatically.

### Option 2: Embedded Runtime (Advanced)
If you prefer not to install the runtime system-wide, you can use the "Fixed Version" package.
1. Run `download_webview2_runtime.ps1` and select **Option 2** for instructions.
2. Follow the link to download the Fixed Version from Microsoft.
3. Extract the contents into a folder named `WebView2Runtime` inside the application directory.

## Troubleshooting
If you receive an error related to "WebView2Loader.dll" or runtime missing:
- Ensure you have run the setup script.
- Verify that your Windows version is supported.
- Check if the application is being run with necessary permissions.
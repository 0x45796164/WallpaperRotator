# Gallery-DL GUI

A lightweight, efficient graphical interface for [gallery-dl](https://github.com/mikf/gallery-dl), designed for rapid batch downloading of image galleries and collections.

## Features

-   **Clipboard Watcher**: Automatically detects and adds URLs copied to your clipboard. Optimized for use with extensions like [Copy All URLs](https://chromewebstore.google.com/detail/copy-all-urls-free/pnbocjclllbkfkkchadljokjclnpakia).
-   **Smart Queue**: Automatically removes duplicates and formats URLs.
-   **Multi-Threaded**: Download multiple galleries simultaneously with configurable worker threads.
-   **Archive Support**: Optionally uses a local database (`archive.sqlite`) to track downloaded files and prevent re-downloading content.
-   **Drag & Drop**: Simply drag URLs or text files onto the window (requires `tkinterdnd2`).
-   **Cross-Platform**: Works on Windows, macOS, and Linux.

## Requirements

1.  **Python 3.x**
2.  **gallery-dl**: Must be installed and accessible in your system PATH.
    ```bash
    pip install -U gallery-dl
    ```
3.  **(Optional) tkinterdnd2**: For drag-and-drop support.
    ```bash
    pip install tkinterdnd2
    ```

## Usage

1.  Run the script:
    ```bash
    python gallery-dl-gui.py
    ```
2.  **Add URLs**:
    -   Paste URLs directly into the text area.
    -   Enable **"Watch Clipboard"** to automatically capture copied links.
    -   Drag and drop links or text files into the window.
3.  **Configure**:
    -   Set your **Save Directory**.
    -   (Optional) Load a **Cookies** file for authenticated sites.
    -   Adjust **Workers** count for speed.
4.  **Download**:
    -   Press **Start** (or `Ctrl+Enter`) to begin.
    -   Monitor progress in the console log.

# ATtiny Studio

![License](https://img.shields.io/badge/license-GPL--3.0--or--later-blue.svg)
![Target](https://img.shields.io/badge/platform-win--x64-lightgrey.svg)
![Framework](https://img.shields.io/badge/.NET-10.0-purple.svg)

ATtiny Studio is a professional, high-performance embedded tool designed for the ATtiny85 and other AVR microcontrollers. Built for speed and reliability, it provides a modern interface for flashing, fuse management, and batch production.

---

## Key Features

### Comprehensive Writing Capabilities
The application supports three critical writing modes for full hardware control:
- **Flash Memory**: Write `.HEX` production files to the primary program storage.
- **Fuse Configuration**: Professional management of Low, High, and Extended fuse bytes to control clock speeds and hardware features.
- **EEPROM Storage**: Write and read non-volatile data via `.BIN` files.

### Professional Toolset
- **Live EEPROM Viewer**: Integrated Hex and ASCII visualizer for real-time memory inspection.
- **Batch Flashing System**: Streamlined interface for high-volume production with auto-next and custom delay settings.
- **Chip Information**: Detailed technical readouts including signature bytes and hardware specifications.
- **Integrated Driver Suite**: One-click access to CH340, Zadig (libusb), FTDI, and CP210x drivers.
- **Built-in Code Snippets**: A library of ready-to-use Arduino IDE examples for interrupts, sleep modes, and peripherals.

> [!IMPORTANT]
> **Settings Persistence**: Application configurations (Chip selection, Programmer type, Baud rate, etc.) will **not** be saved between sessions unless the application is installed on your system. To enable settings persistence, go to the **About** tab and click **Install Now**.

---

## Supported Hardware & Programmers

### Compatible Write Devices (Programmers)
ATtiny Studio interfaces with a wide range of industry-standard programmers:
- **AVR ISP**: stk500v1, stk500v2, avrispmkII.
- **USB Native**: USBasp, USBtiny.
- **Development Boards**: Arduino (as ISP).
- **Advanced Tools**: BusPirate, Picoprog, JTAGICE.

### Supported Microcontrollers
- **ATtiny Series**: 13, 25, 45, 85.
- **ATmega Series**: 48, 88, 168, 328P.
- **High Performance**: 32U4, 2560.

---

## 📸 Screenshots

| EEPROM Viewer | Main Interface | Chip Info |
| :--- | :--- | :--- |
| ![EEPROM](Screenshots/Screenshot%202026-04-02%20231222.png) | ![Main](Screenshots/Screenshot%202026-04-02%20231146.png) | ![Chip](Screenshots/Screenshot%202026-04-02%20231112.png) |

| Batch Flasher | Fuse Manager |
| :--- | :--- |
| ![Batch](Screenshots/Screenshot%202026-04-02%20231100.png) | ![Fuses](Screenshots/Screenshot%202026-04-02%20233226.png) |

---

## Environment Setup and Build Guide

### 1. Prerequisites
- **Windows 10/11** (64-bit).
- **Internet Connection** (for the initial SDK retrieval).

### 2. Step-by-Step Setup

1.  **Clone the Repository**:
    ```bash
    git clone https://github.com/SlickAlex1/ATtinyStudio.git
    cd ATtinyStudio
    ```

2.  **Initialize the Compiler**:
    Execute **`UpdateCompiler.bat`** to download the portable **.NET 10 SDK**.

3.  **Build the Project**:
    Execute **`build.bat`** to compile the **single-file executable**.

4.  **Locate the Output**:
    Find your app in the **`Output/`** directory as `ATtinyStudio.exe`.

---

## License and Credits

### Project License
Licensed under the **GNU General Public License v3.0**. 

### Third-Party Credits
- **AVRDUDE**: (v8.1+) - Licensed under **GPLv2**.
- **.NET Runtime**: Licensed under the MIT License.
- **Drivers**: Compatibility drivers remain property of their respective owners.

---

## Contact

**SlickAlex** - [GitHub Profile](https://github.com/SlickAlex1)

Project Link: [https://github.com/SlickAlex1/ATtinyStudio](https://github.com/SlickAlex1/ATtinyStudio)
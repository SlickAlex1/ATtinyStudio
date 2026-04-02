# ATtiny Studio

![License](https://img.shields.io/badge/license-GPL--3.0--or--later-blue.svg)
![Target](https://img.shields.io/badge/platform-win--x64-lightgrey.svg)
![Framework](https://img.shields.io/badge/.NET-10.0-purple.svg)

ATtiny Studio is a professional, high-performance embedded tool designed for the ATtiny85 and other AVR microcontrollers. Built for speed and reliability, it provides a modern interface for flashing, fuse management, and batch production.

---

## Key Features

### Comprehensive Write Operations
The application supports three distinct write modes to ensure full control over your hardware:
- **Flash Writing**: Upload production-ready `.HEX` files to the main program memory.
- **Fuse Management**: Configure system clocks and hardware behaviors via Low, High, and Extended fuse bytes (one-click presets included).
- **EEPROM Writing**: Store and retrieve non-volatile data using binary `.BIN` files.

### Specialized Tools
- **Live EEPROM Viewer**: A real-time Hex and ASCII visualizer that allows you to inspect the chip's internal memory without external software.
- **Batch Production Tool**: Automate the flashing of hundreds of chips with custom delays and auto-next functionality.
- **Driver Suite**: Integrated access to essential drivers for CH340, FTDI, CP210x, and Zadig (libusb).
- **Code Snippets**: An extensive, built-in library of Arduino IDE snippets for everything from basic Blinks to advanced Sleep modes and NeoPixels.

### Supported Microcontrollers
The tool is pre-configured for a wide variety of 8-bit AVR chips, including:
- **ATtiny Series**: ATtiny13, ATtiny25, ATtiny45, ATtiny85.
- **ATmega Series**: ATmega48, ATmega88, ATmega168, ATmega328P.
- **Performance Series**: ATmega32U4, ATmega2560.

---

## 📸 Screenshots

| Main Interface | Fuse Manager | Batch Flasher |
| :--- | :--- | :--- |
| ![Main](Screenshots/Screenshot%202026-04-02%20230843.png) | ![Fuses](Screenshots/Screenshot%202026-04-02%20231100.png) | ![Batch](Screenshots/Screenshot%202026-04-02%20231112.png) |

| EEPROM Viewer | Code Snippets |
| :--- | :--- |
| ![EEPROM](Screenshots/Screenshot%202026-04-02%20231146.png) | ![Snippets](Screenshots/Screenshot%202026-04-02%20231222.png) |

---

## Environment Setup and Build Guide

ATtiny Studio utilizes a standalone, portable build system. The project can be compiled without a full Visual Studio installation.

### 1. Prerequisites
- **Windows 10/11** (64-bit).
- **Internet Connection** (required for the initial SDK retrieval).

### 2. Step-by-Step Setup

1.  **Clone the Repository**:
    ```bash
    git clone https://github.com/SlickAlex1/ATtinyStudio.git
    cd ATtinyStudio
    ```

2.  **Initialize the Compiler**:
    Execute **`UpdateCompiler.bat`**. This script creates a local `Compiler/` directory and downloads the latest portable **.NET 10 SDK**.

3.  **Build the Project**:
    Execute **`build.bat`**. The build script automates metadata generation, asset embedding, and compilation into a **single-file executable**.

4.  **Locate the Output**:
    The compiled application will be generated in the **`Output/`** directory as `ATtinyStudio.exe`.

---

## Sample Code (Arduino IDE)

The following examples demonstrate basic operations for the ATtiny85. More advanced examples are available directly within the application.

### Basic Blink
Blinks an LED on physical pin 5 (Digital Pin 0).
```cpp
void setup() {
  pinMode(0, OUTPUT); 
}

void loop() {
  digitalWrite(0, HIGH);
  delay(1000);
  digitalWrite(0, LOW);
  delay(1000);
}
```

---

## License and Credits

### Project License
This project is licensed under the **GNU General Public License v3.0**. Detailed terms can be found in the [LICENSE](LICENSE) file.

### Third-Party Credits
- **AVRDUDE**: (v8.1+) - Licensed under **GPLv2**. [Source](https://github.com/avrdudes/avrdude).
- **.NET Runtime**: Licensed under the MIT License.
- **Drivers**: Hardware compatibility drivers (CH340, FTDI, CP210x) remain property of their respective owners.

---

## Contact

**SlickAlex** - [GitHub Profile](https://github.com/SlickAlex1)

Project Link: [https://github.com/SlickAlex1/ATtinyStudio](https://github.com/SlickAlex1/ATtinyStudio)

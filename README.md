# ATtiny Studio

![License](https://img.shields.io/badge/license-GPL--3.0--or--later-blue.svg)
![Target](https://img.shields.io/badge/platform-win--x64-lightgrey.svg)
![Framework](https://img.shields.io/badge/.NET-10.0-purple.svg)

ATtiny Studio is a professional, high-performance embedded tool designed for the ATtiny85 and other AVR microcontrollers. Built for speed and reliability, it provides a modern interface for flashing, fuse management, and batch production.

---

## Contributing

We welcome and appreciate all contributions. Whether you are fixing a bug, adding a feature, or improving documentation, your help improves ATtiny Studio for the community.

1.  **Fork** the repository.
2.  **Create** your feature branch (`git checkout -b feature/AmazingFeature`).
3.  **Commit** your changes (`git commit -m 'Add some AmazingFeature'`).
4.  **Push** to the branch (`git push origin feature/AmazingFeature`).
5.  **Open** a Pull Request.

Technical suggestions and bug reports can be submitted via the GitHub Issues page.

---

## Key Features

- **High-Speed Flasher**: Optimized for ATtiny85, supports .HEX and .BIN files.
- **Batch Production Tool**: Automate the flashing of multiple chips with custom delays and auto-next functionality.
- **Fuse Manager**: Simplifies clock speed settings (Internal 1MHz, 8MHz, 16MHz PLL) with one-click presets.
- **EEPROM Viewer**: Live Hex/ASCII visualization of chip memory.
- **Driver Suite**: Integrated access to CH340, Zadig (libusb), FTDI, and CP210x drivers.
- **Code Snippets**: Quick-copy library for common tasks including Blink, PWM, ADC, and SoftwareSerial.
- **64-Bit Native**: Optimized for modern Windows (win-x64) environments.

---

## Environment Setup and Build Guide

ATtiny Studio utilizes a standalone, portable build system. The project can be compiled without a full Visual Studio installation.

### 1. Prerequisites
- **Windows 10/11** (64-bit).
- **Internet Connection** (required for the initial SDK retrieval).

### 2. Step-by-Step Setup

1.  **Clone the Repository**:
    ```bash
    git clone https://github.com/SlickAlex/Attiny85-Retro-Console.git
    cd Attiny85-Retro-Console
    ```

2.  **Initialize the Compiler**:
    Execute **`UpdateCompiler.bat`**. This script performs the following:
    - Creates a local `Compiler/` directory.
    - Automatically downloads and extracts the latest portable **.NET 10 SDK**.
    - Verifies the installation.
    *This ensures a clean environment without global system modifications.*

3.  **Build the Project**:
    Execute **`build.bat`**. The build script automates:
    - Environment validation.
    - Metadata generation from `metadata.txt`.
    - Asset embedding (Avrdude, Drivers, Icons).
    - Compilation into a **single-file, self-contained executable**.

4.  **Locate the Output**:
    The compiled application will be generated in the **`Output/`** directory as `ATtinyStudio.exe`.

---

## Common Code Examples

The following snippets provide a baseline for ATtiny85 development using standard AVR C.

### Basic Blink
Configures PB0 as an output and toggles it with a 500ms delay.
```cpp
#include <avr/io.h>
#include <util/delay.h>

int main(void) {
    DDRB |= (1 << PB0); // Set PB0 as output
    while (1) {
        PORTB |= (1 << PB0); // Pin High
        _delay_ms(500);
        PORTB &= ~(1 << PB0); // Pin Low
        _delay_ms(500);
    }
}
```

### Pulse Width Modulation (PWM)
Initializes Fast PWM on PB0 using Timer 0.
```cpp
void init_pwm() {
    DDRB |= (1 << PB0); // Set PB0 as output
    TCCR0A = (1 << COM0A1) | (1 << WGM01) | (1 << WGM00); // Fast PWM mode
    TCCR0B = (1 << CS01); // Prescaler 8
    OCR0A = 127; // 50% Duty Cycle
}
```

### Analog to Digital Conversion (ADC)
Reads the analog value from PB4 (ADC2).
```cpp
uint16_t read_adc() {
    // Select PB4 (ADC2) and set reference to VCC
    ADMUX = (1 << MUX1); 
    // Enable ADC and set prescaler to 64 (125kHz at 8MHz clock)
    ADCSRA = (1 << ADEN) | (1 << ADPS2) | (1 << ADPS1); 
    
    ADCSRA |= (1 << ADSC); // Start conversion
    while (ADCSRA & (1 << ADSC)); // Wait for completion
    
    return ADC;
}
```

---

## License and Credits

### Project License
This project is licensed under the **GNU General Public License v3.0**. Detailed terms can be found in the [LICENSE](LICENSE) file. The source code must remain open for forks and redistributions.

### Third-Party Credits
- **AVRDUDE**: (v8.1+) - Licensed under **GPLv2**. [Source](https://github.com/avrdudes/avrdude).
- **.NET Runtime**: Licensed under the **MIT License**.
- **Drivers**: Hardware compatibility drivers (CH340, FTDI, CP210x) are the property of their respective owners.

---

## Screenshots

> [!TIP]
> Place application screenshots in the `Assets/` directory to update the previews below.

| Main Interface | Batch Flasher | Chip Info |
| :--- | :--- | :--- |
| ![Main](Assets/screenshot_main.png) | ![Batch](Assets/screenshot_batch.png) | ![Info](Assets/screenshot_info.png) |

---

## Contact

**SlickAlex** - [GitHub Profile](https://github.com/SlickAlex)

Project Link: [https://github.com/SlickAlex/Attiny85-Retro-Console](https://github.com/SlickAlex/Attiny85-Retro-Console)

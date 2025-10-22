# 🎹 3x3 RGB Keyboard

A compact **3x3 mechanical macro keyboard** powered by an **Arduino Pro Micro**, featuring **Cherry MX Red** switches, a **128x64 OLED display**, and **per-key RGB lighting**. Built on a **custom PCB** with **custom firmware** and a **Windows configuration app** for key mapping.

---

## ⚙️ Features

- 9 × Cherry MX Red mechanical switches  
- 128×64 OLED display for dynamic info or menus  
- RGB LEDs under each key  
- Fully programmable key bindings via Windows app  
- USB HID compliant (plug and play)  
- Open-source hardware and firmware

---

## 🧠 Hardware Overview

| Component         | Description                            |
|-------------------|----------------------------------------|
| MCU               | Arduino Pro Micro (ATmega32U4)         |
| Switches          | Cherry MX Red                          |
| Display           | 128×64 OLED (I2C)                      |
| Lighting          | RGB LEDs (individually addressable)    |
| PCB               | Custom designed, compact layout        |

---

## 💻 Software

- **Firmware:** Custom Arduino-based firmware with HID support  
- **Windows App:** Simple interface to assign and save custom key bindings  

> Firmware communicates with the app over serial to store new configurations in EEPROM.

---

## 🔌 Getting Started

1. Connect the keyboard via USB.  
2. Launch the Windows configuration app.  
3. Assign key bindings and save to device.  
4. Enjoy your personalized macro setup.

---

## 🧰 Future Additions

- Profiles and macro layers  
- Animated RGB effects  
- On-screen OLED menus  
- Firmware update utility  

---

## 🧑‍💻 Author

Designed and developed by **Soso Chkhortolia @ ARCHMASTER**  
Hardware, firmware, and configuration tool all made from scratch.

---

Let's go


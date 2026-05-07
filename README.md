# Seymu Desktop Price Calculator 🌲📐

Professional desktop application for wood product pricing, featuring offline-first architecture, cloud synchronization, and dynamic branding.

## 🚀 Key Features

- **Intuitive Calculations:** Support for advanced width syntax (e.g., `5x10` or `5-7-8`) for rapid data entry.
- **Offline-First Storage:** Local SQLite database ensures the app works without internet.
- **Cloud Sync:** Seamlessly synchronizes local data with **Neon PostgreSQL** when online.
- **Dynamic Branding:** Ability for users to upload their own company logo and information.
- **Professional Installer:** Self-contained distribution with no external dependencies required for the end-user.
- **Thermal Printing:** Optimized receipt printing for thermal printers.

## 🛠 Tech Stack

- **Framework:** .NET 8 (WPF)
- **Local DB:** SQLite (via Microsoft.Data.Sqlite)
- **Cloud DB:** PostgreSQL (Neon)
- **Sync Engine:** Custom Sync Service with UUID-based conflict resolution.
- **UI:** Modern XAML with Material Design components.

## 📦 Installation

1. Download the latest `InstaladorSeymu.msi` from the releases/debug folder.
2. Run the installer and follow the "Next, Next, Finish" wizard.
3. No .NET Runtime installation is required (Self-contained).

## 🔒 Permissions

The application is designed to run without administrative privileges by storing data and assets in the user's `%AppData%` folder.

---
Developed with ❤️ by Randola347

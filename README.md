# Seymu Desktop Price Calculator 🌲📐

Seymu Desktop Price Calculator is a desktop application for wood product pricing built with .NET 8 WPF. This repository is prepared as a public demo and portfolio project for recruiters and hiring managers.

## 🚀 Demo Purpose

This repository contains a demo-oriented version of the application with test data and a public-ready configuration structure. The company name shown in the app is the real business name, while no real customer or business-sensitive information is included.

## ✨ What the Application Does

This project is a desktop quotation tool for wood products. It allows a user to:

- enter customer information,
- select the wood type and thickness,
- enter dimensions and pricing details,
- calculate the subtotal, VAT, and final total automatically,
- save the quotation locally,
- and print a thermal receipt when a compatible printer is available.

Although the interface is in Spanish, the project is presented here in English so recruiters can quickly understand its purpose and scope.

## 🛠 Tech Stack

- Framework: .NET 8 (WPF)
- Local database: SQLite
- Cloud database: PostgreSQL via Neon
- UI: XAML with Material Design components

## 📦 How to Run

1. Clone the repository.
2. Open `SeymuPriceCalculator/SeymuPriceCalculator.csproj` in Visual Studio or build it with the .NET CLI using `dotnet build SeymuPriceCalculator/SeymuPriceCalculator.csproj`.
3. Copy `SeymuPriceCalculator/appsettings.example.json` to `SeymuPriceCalculator/appsettings.json` if you want to configure cloud sync.
4. Run the application. The desktop demo works locally without cloud sync.

### Online sync

The app includes an optional PostgreSQL synchronization feature. To test it with your own database:

1. Create a PostgreSQL database.
2. Run the SQL script in [docs/neon-schema.sql](docs/neon-schema.sql).
   - Important: the current app sync logic expects `cotizaciones`, `piezas`, and `clientes` to use `uuid TEXT PRIMARY KEY` fields.
   - If your database uses `id SERIAL PRIMARY KEY`, the app will fail with errors like `column "uuid" does not exist`.
3. Copy `SeymuPriceCalculator/appsettings.example.json` to `SeymuPriceCalculator/appsettings.json` and update the `NeonConnectionString` value.
4. Launch the app. If the connection is unavailable, the app continues working locally and displays sync status in the interface.

> `SeymuPriceCalculator/appsettings.json` is excluded from Git by `.gitignore`, so local credentials stay private.

## ▶️ User Flow

A typical demo flow is:

1. Enter the customer name and phone number.
2. Select the wood type and thickness.
3. Add one or more pieces with dimensions and price.
4. Review the calculated total.
5. Save the quotation and print it if a thermal printer is available.

## 🔒 Demo Notes

- This version is intended for portfolio/demo use only.
- Sensitive data should stay in your local `appsettings.json` file and is not committed to the repository.

## 📄 Documentation

See [docs/demo.md](docs/demo.md) for details about the demo setup and data expectations.

---
Developed for demo and portfolio purposes.

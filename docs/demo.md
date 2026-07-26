# Demo Version Notes

This repository contains a public demo version of Seymu Desktop Price Calculator.

## Purpose

- Portfolio and demonstration use only.
- No real customer, business, or production data is included.
- The application keeps its existing business logic and workflow intact.

## Data and Configuration

- The database is prepared as a clean demo environment.
- The default company profile uses placeholder values.
- Local image/logo loading remains supported without requiring any database image fields.
- Cloud configuration is not required for the demo flow.

## Local Setup

1. Copy `SeymuPriceCalculator/appsettings.example.json` to `SeymuPriceCalculator/appsettings.json`.
2. Replace the placeholder Neon connection string if you want to test cloud sync.
3. Build and run the application locally. The app works with local SQLite data even if cloud sync is not configured.

## Neon Sync Notes

- Use the schema in `docs/neon-schema.sql` for the online database.
- The app expects remote tables `cotizaciones`, `piezas`, and `clientes` to use `uuid TEXT PRIMARY KEY`.
- A schema based on `id SERIAL PRIMARY KEY` is incompatible with the current sync logic.

> `SeymuPriceCalculator/appsettings.json` is ignored by Git, so local credentials are not committed.

## Important

This demo should not be used as a production deployment and should never contain sensitive or real customer records.

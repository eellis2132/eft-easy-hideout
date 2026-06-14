# Easy Hideout

A hideout upgrade tracker for Escape from Tarkov.

Easy Hideout pulls station data from [tarkov.dev](https://tarkov.dev) and helps you plan your hideout progression — tracking what items you need, what you can afford, and what to focus on first.

## Features

- **Wishlist** — All items needed across your upcoming upgrades, with quantity tracking
- **Priority Lists** — Auto-ranked focus items by cross-station impact, plus L+1 readiness per station
- **Active Nodes** — Visual map of your hideout with blocking dependencies shown
- **Shopping** — Flea market shopping list sorted by cost, with price drop indicators
- **Item Pool** — Aggregate view of everything needed across all unfinished upgrades
- **Settings** — Profile management, auto-refresh, and manual data sync

## Download

Grab the latest release from the [Releases](../../releases) page. Single `.exe`, no installer needed.

## Requirements

- Windows 10 or later
- Internet connection (for pulling data from tarkov.dev)

## Usage

1. Download `EasyHideout.exe` from Releases
2. Run it — no install required, data is stored in `%AppData%\EasyHideout`
3. Go to **Settings → Pull Hideout Data** on first launch
4. Create a profile with your PMC level and edition
5. Start tracking

## Notes

- All data comes from the public [tarkov.dev](https://tarkov.dev) API — no account or API key required
- Your progress is stored locally in a SQLite database in `%AppData%\EasyHideout`
- Prices can be refreshed independently of the full data sync via **Settings → Refresh Prices**

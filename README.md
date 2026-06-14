# Easy Hideout

A hideout upgrade planner and tracker for Escape from Tarkov.

> **Easy Hideout does not interact with the game, modify any game files, or read any game data.**
> It is a standalone desktop app. All game data (station requirements, item lists, prices) is pulled
> from the public [tarkov.dev](https://tarkov.dev) API. Your progress is tracked manually by you.

---

## What it does

Easy Hideout lets you plan your hideout progression without tabbing out to spreadsheets or wikis.
You tell it your current station levels and what items you have — it figures out what to build next,
what to buy, and what to focus on.

## Features

- **Wishlist** — Every item needed across your upcoming upgrades in one list, with quantity tracking you update manually
- **Priority Lists** — Auto-ranks items by cross-station impact so you know what to farm first; also shows which L+1 upgrades you're closest to completing
- **Active Nodes** — Visual overview of your hideout showing what's unlocked, what's blocked, and why
- **Shopping** — Flea market shopping list for items you still need, sorted by cost, with price drop indicators after a refresh
- **Item Pool** — Aggregate view of everything needed across all unfinished upgrades
- **Settings** — Profile management, auto-refresh, and manual data sync

## Download

Grab the latest release from the [Releases](../../releases) page. Single `.exe`, no installer needed — just download and run.

> **Windows SmartScreen warning:** Because this app isn't code-signed, Windows may show a "Windows protected your PC" popup the first time you run it. Click **More info → Run anyway**. Every release includes a VirusTotal scan link so you can verify it yourself.

## Requirements

- Windows 10 or later
- Internet connection (used only to fetch data from tarkov.dev)

## First-time setup

1. Download `EasyHideout.exe` from the Releases page
2. Run it — no installation required. App data is stored in `%AppData%\EasyHideout`
3. Go to **Settings → Pull Hideout Data**
   - This downloads station structure, level requirements, and item data from [tarkov.dev](https://tarkov.dev)
   - It does **not** pull your in-game hideout — EFT does not expose that data externally
   - You set your own station levels and item counts manually inside the app
4. Create a profile with your PMC level and game edition
5. Set your current station levels in the **Active Nodes** tab
6. Start tracking

## Updating prices

Flea market prices shift constantly. Use **Settings → Refresh Prices** to pull the latest
`avg24hPrice` values from tarkov.dev without doing a full data sync. Price drops of 15% or
more are flagged with a green badge on the Shopping and Wishlist tabs.

## Privacy & safety

- No account, login, or API key required
- The app makes outbound requests only to `api.tarkov.dev/graphql` (public API, no authentication)
- All your progress data is stored locally at `%AppData%\EasyHideout\easyhideout.db` (SQLite)
- Nothing is sent anywhere — your data never leaves your machine

## License

[CC BY-NC 4.0](LICENSE) — free to use and share, not for commercial use.

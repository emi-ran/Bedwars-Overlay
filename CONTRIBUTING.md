# Contributing

Thanks for wanting to improve Bedwars Overlay.

## Good Contributions

- Bug fixes.
- Clear UI improvements.
- Better translations.
- Small quality-of-life features.
- Documentation improvements.

## Before Opening a Pull Request

1. Keep the change focused.
2. Avoid unrelated formatting or refactors.
3. Do not commit API keys, tokens, logs with private data, or `.env` files.
4. Run the checks:

```powershell
dotnet build "BedwarsOverlay.App\BedwarsOverlay.App.csproj"
dotnet test "BedwarsOverlay.Tests\BedwarsOverlay.Tests.csproj"
```

If the running app locks build outputs, close Bedwars Overlay before building or build with a temporary `OutputPath`.

## Development Notes

- Target platform is Windows.
- Keep the overlay lightweight and easy to use.
- User-facing text should be added to both English and Turkish localization files.
- If a change affects setup or usage, update both README files.
- Player-facing behavior changes belong in `CHANGELOG.md`, the handover state belongs in `PROGRESS.md`, and future work belongs in `PLAN.md`.
- Schema changes must go through `AppDatabase`'s versioned migration (`PRAGMA user_version`) and must keep existing user data in `%LOCALAPPDATA%\BedwarsOverlay\data.db`.

## Security

Do not open public issues for private keys or sensitive reports. Remove secrets before sharing logs or screenshots.

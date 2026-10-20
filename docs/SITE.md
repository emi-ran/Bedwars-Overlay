# Product website

The bilingual product landing page is a dependency-free static site implemented with OpenCode using `opencode/deepseek-v4.1-flash-free`.

## Files

- `index.html`: Turkish fallback content, semantic sections, sample roster, setup guide and FAQ.
- `assets/site.css`: paper/charcoal palette, amber accents, team colors, responsive layouts and animation styles.
- `assets/site.js`: Turkish/English translations, safe local language preference, team-grouping demo, Threat breakdown and reduced-motion handling.
- `assets/favicon.svg`: pixel bed mark.
- `.nojekyll`: serves the directory as static files on GitHub Pages.

Existing application screenshots are preserved. The landing page uses an explicitly labeled illustrative demo rather than presenting the older screenshots as the current application UI. Demo scores and players are sample data, not live API results or calculations from the app's scorer.

## Local preview

From the repository root:

```powershell
python -m http.server 8765 --bind 127.0.0.1 --directory docs
```

Open <http://127.0.0.1:8765/>. There is no install or build step. Language defaults to the browser language, supports TR/EN buttons, and is stored locally when storage is available. The page collects no API keys and has no analytics or external font dependencies. Downloads link to the stable GitHub latest-release page.

## Publishing

The owner reviewed the local preview and approved pushing the website on 2026-10-20.

GitHub Pages source: repository Settings → Pages → Deploy from a branch → `main` / `/docs`, matching the FlowPilot repository's setup. Website: <https://emi-ran.github.io/Bedwars-Overlay/>. Future pushes that change the site are published automatically; verify the Pages deployment before reporting an update as live.

## Maintaining content

The v1.4.4 FAQ covers respawn roster retention, DC/reconnect handling and the teammate `Split!` reminder in both languages. The existing interactive demo still illustrates team/Threat behavior only.

Keep Turkish HTML fallback copy and both dictionaries in `assets/site.js` consistent. Update the product claims when desktop behavior changes. Keep sample data labeled, retain keyboard-accessible controls and reduced-motion support, and preserve stable download links across future releases.

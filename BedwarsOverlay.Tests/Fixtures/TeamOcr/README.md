# Reviewed TAB OCR corpus — 2026-10-26

## Second October 28 batch

Reviewed-25..29 add five originals (77 visible prefixed roster names). A name-only fallback fixes merged `Yrezna_2010` and distorted `momochiki1089` in both last captures: 15/15 each. Batch total 73/77, no extra/wrong teams. Reviewed-26 Gray `ciapagallina`/`MatchedAuthor52` and reviewed-27 Gray `amisotheist`/`_Puzo_` remain explicit known misses due to background blending. Full corpus: 29 images, 363/369 correct assignments, zero extra/wrong, six known misses. These totals supersede earlier scoped results below. `TabName` trace entries identify retries; acceptance thresholds remain unchanged.

## Expansion — 2026-10-28

24 available full originals are now independently labeled and paired with native-size redacted fixtures; missing earlier PNGs have been regenerated from their local originals. Original recorded predictions are preserved. The new batch (reviewed-9 through reviewed-24) contains 192 visible prefixed roster names: updated full-image OCR assigns 190 correctly, with zero wrong/extra assignments. Raw case-sensitive exact names occur in at least one same-color pass for 154/192 (80.2%); this best-of-passes name metric is not single-pass accuracy or character accuracy. Full corpus: 290/292 correct team assignments.

Reviewed-14 has TAB closed: expected teams are empty despite a visible world nametag. Its redacted fixture retains the small nametag region as well as the top-center area, so the negative is exercised without retaining chat/voice overlays. Reviewed-22 preserves `oliverooel` and `Lauucha2` as Gray ground truth but explicitly marks them in `knownMissingNames`; tests assert they remain unknown rather than hiding them or accepting a wrong team. Their background merges with text pixels. Remove known-miss markers only with a verified fix. Respawn rows without a prefix and absent roster players are excluded, not counted as OCR misses.

Optional `BEDWARS_OCR_REPORT` writes fresh predictions and pass traces per image to a chosen local directory for analysis. It does not overwrite capture history. The earlier six-image results below are historical scope, not the expanded corpus score.

Six user captures, independently labeled from the original TAB rows, not OCR predictions. `expected.json` includes the exact input roster and only names visibly carrying a team prefix. Original size is preserved; everything outside the top-center TAB area is blacked out. No chat is retained. `sourceImage` links to the Git-ignored original development capture.

| Fixture | Visible, prefixed roster names | Original recorded correct | Updated correct |
| --- | ---: | ---: | ---: |
| reviewed-1 | 16 | 13 | 16 |
| reviewed-2 | 13 | 10 | 13 |
| reviewed-3 | 16 | 12 | 16 |
| reviewed-4 | 12 | 10 | 12 |
| reviewed-5 | 13 | 10 | 13 |
| reviewed-6 | 6 | 2 | 6 |
| Total | 76 | 57 | 76 |

Neither recorded nor updated outputs contain an incorrect team assignment in this set. Reviewed-4 shows `TryHardnt` without a visible team prefix during respawn; deliberately unlabelled, not counted as a miss. Non-roster spectator names and roster players absent from TAB are excluded. Reviewed-6 is a later, shorter six-player list, not the earlier full roster.

`TeamOcrCorpusTests` checks exact matches and rejects additional assignments. It uses these redacted fixtures by default. To rerun against original full screenshots locally, set `BEDWARS_OCR_CORPUS` to the absolute `images/team-ocr` path before `dotnet test --filter FullyQualifiedName~TeamOcrCorpusTests`, then unset it. Both variants were checked; these results describe this corpus, not guaranteed accuracy on other clients/scales. Existing small dimmed TAB fixture remains a separate regression test.

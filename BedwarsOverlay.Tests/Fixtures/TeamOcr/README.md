# Reviewed TAB OCR corpus — 2026-10-26

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

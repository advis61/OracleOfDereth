# Oracle of Dereth Quest Curator

Windows utility for merging one or more `*-newflags.csv` Discord submissions into
`OracleOfDereth/Resources/quests.csv`.

1. Run `OracleOfDereth.Curator.exe`.
2. Confirm the master path (it is detected automatically when run from the repository).
3. Add submission CSV files, or drag them onto the window.
4. Select **Preview merge** and review every action.
5. Select **Save merged quests.csv**.

Existing rows are marked `Verified` in `Verified Conquest`; their curated metadata is
preserved. A row previously restricted to another server is made shared so Conquest
clients can load it. New rows retain matching submission fields, use the master's column
order, and are added with `Server=Conquest` and `Verified Conquest=Verified`. The complete
master is sorted A-Z by `QuestFlag` after every merge.

Previewing performs a required validation pass. It rejects malformed field counts,
unterminated quoted fields, blank or duplicate headers, blank quest flags, duplicate master
flags, and duplicate flags in the merged result. Repeated evidence in multiple submission
files is coalesced into a single result row. For new flags, the first non-empty value wins
and later submissions fill only empty metadata fields; all contributing filenames appear
in the preview. Existing curated metadata is never replaced by submission data.

When a new flag has no useful submitted quest name, the app may generate a conservative
name from recognized flag components. The preview marks it in the `NameInferred` column.
A useful name found in a later duplicate submission automatically replaces the inference.

Saving creates a timestamped backup beside the master before replacing it. The app does
not commit or push changes.

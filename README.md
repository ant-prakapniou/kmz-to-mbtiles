# Kmz2Mbtiles

Converts georeferenced KMZ archives of archival aerial photographs into MBTiles
ready for use in mobile map viewers, while normalizing file names for long-term
archiving.

The project was created to process a library of German archival aerial photographs
from World War II. Each photograph is manually georeferenced in Google Earth and
saved as a KMZ; this tool automates the rest of the pipeline.

## What it does

For each input KMZ, the pipeline:

1. Extracts the archive.
2. Derives a normalized master name from the embedded JPG (the only name
   considered authoritative, since it comes from the archive).
3. Patches the KML `<name>` and `<href>` to match the master name and writes a
   normalized KMZ for archival.
4. Creates a GCP-tagged GeoTIFF in EPSG:4326 from the four corners in
   `gx:LatLonQuad`.
5. Reprojects to EPSG:3857, converts to MBTiles with PNG tiles, and builds
   overviews for smooth zooming on mobile devices.

## Requirements

- Windows x64
- GDAL command-line tools on `PATH` (`gdal_translate`, `gdalwarp`, `gdaladdo`,
  `gdalinfo`)
- Python 3 (only for the verification script)

The published executable is self-contained and does not require a .NET runtime
on the target machine.

## Usage

Drag and drop one or more `.kmz` files onto `Kmz2Mbtiles.bat`, or run the
executable directly:

```
Kmz2Mbtiles.exe "path\to\file.kmz"
```

## Output

Results are written next to the input KMZ:

- `normalized/` — KMZ archives with synchronized `<name>`, `<href>` and JPG file
  names.
- `mbtiles/` — ready-to-use MBTiles files.

## Verification

`compare_mbtiles.py` compares two MBTiles files by the SHA-256 hashes of their
tiles and reports any difference, per zoom level.

Drag and drop two `.mbtiles` files onto `compare_mbtiles.bat`, or run from a
console:

```
compare_mbtiles.bat reference.mbtiles output.mbtiles
```

The script requires Python 3 and is intended as a manual verification tool,
not as part of the conversion pipeline.

## Expected KMZ structure

The pipeline assumes a fixed KMZ layout produced by Google Earth:

- `doc.kml` with a `<GroundOverlay>` containing `gx:LatLonQuad`.
- `files/` with exactly one `.jpg`.

Deviations from this structure are treated as errors.

## Published files

A release publish produces five files:

```
Kmz2Mbtiles.exe     — self-contained executable (Windows x64)
Kmz2Mbtiles.bat     — drag-and-drop launcher for conversion
compare_mbtiles.py  — MBTiles verification script
compare_mbtiles.bat — drag-and-drop launcher for comparison
e_sqlite3.dll       — native SQLite library (required at runtime)
```

## Project structure

```
src/
├── Kmz2Mbtiles.Cli/    — executable, console UI, batch launcher, verification script
└── Kmz2Mbtiles.Core/   — conversion pipeline, GDAL wrapper, naming, KMZ helpers
```

## License

MIT — see [LICENSE](LICENSE).
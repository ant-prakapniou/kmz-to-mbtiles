#!/usr/bin/env python3
"""
compare_mbtiles.py

Compares two MBTiles files by the contents of their `tiles` table.
For each tile, computes SHA-256 of the tile data and compares the resulting sets.

Usage:
    python compare_mbtiles.py file1.mbtiles file2.mbtiles
"""

import hashlib
import sqlite3
import sys
from pathlib import Path


def load_tiles(mbtiles_path: Path) -> dict[tuple[int, int, int], str]:
    """Return a dict mapping (zoom, column, row) to SHA-256 of tile_data."""
    tiles: dict[tuple[int, int, int], str] = {}
    con = sqlite3.connect(str(mbtiles_path))
    try:
        cur = con.execute(
            "SELECT zoom_level, tile_column, tile_row, tile_data FROM tiles"
        )
        for z, x, y, data in cur:
            h = hashlib.sha256(data).hexdigest()
            tiles[(z, x, y)] = h
    finally:
        con.close()
    return tiles


def main(argv: list[str]) -> int:
    if len(argv) < 3:
        print("Usage: python compare_mbtiles.py file1.mbtiles file2.mbtiles")
        return 1

    p1 = Path(argv[1])
    p2 = Path(argv[2])

    print(f"File 1: {p1}")
    print(f"File 2: {p2}")
    print()

    print("Reading tiles from file 1...")
    t1 = load_tiles(p1)
    print(f"  total tiles: {len(t1)}")

    print("Reading tiles from file 2...")
    t2 = load_tiles(p2)
    print(f"  total tiles: {len(t2)}")
    print()

    keys1 = set(t1.keys())
    keys2 = set(t2.keys())

    only1 = keys1 - keys2
    only2 = keys2 - keys1
    common = keys1 & keys2

    print(f"Only in file 1 : {len(only1)}")
    print(f"Only in file 2 : {len(only2)}")
    print(f"Common keys    : {len(common)}")
    print()

    diff_content = 0
    for k in common:
        if t1[k] != t2[k]:
            diff_content += 1

    print(f"Common keys with DIFFERENT content: {diff_content}")
    print()

    if not only1 and not only2 and diff_content == 0:
        print(">>> RESULT: files are IDENTICAL by tile content.")
    else:
        print(">>> RESULT: differences detected.")
        if only1:
            sample = list(only1)[:5]
            print(f"    sample only in file 1: {sample}")
        if only2:
            sample = list(only2)[:5]
            print(f"    sample only in file 2: {sample}")
        if diff_content:
            print(f"    {diff_content} tiles with different content")

    # Per-zoom breakdown
    print()
    print("Per-zoom breakdown (file1 / file2 / common with differences):")
    zoom_levels = sorted({z for (z, _, _) in keys1 | keys2})
    for z in zoom_levels:
        c1 = sum(1 for (zz, _, _) in keys1 if zz == z)
        c2 = sum(1 for (zz, _, _) in keys2 if zz == z)
        cd = sum(1 for k in common if k[0] == z and t1[k] != t2[k])
        print(f"  z={z:2d}: {c1:6d} / {c2:6d} / {cd:6d}")

    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
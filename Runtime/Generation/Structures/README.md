# Structures

The structure layer places deterministic multi-cell structure footprints into generated chunk data.

## Responsibilities

- choose deterministic structure anchors per chunk
- validate region and terrain requirements
- prevent overlap with caves, resources, and other structures
- stamp structure IDs into generated cells
- remain independent from prefabs, Tilemaps, and rendering

## Flow

```text
region + terrain + caves + resources
                |
                v
        StructureCatalog
                |
                v
          StructurePass
                |
                v
      GeneratedCell.Structure
```

Structures are disabled by default. A structure definition contains a stable ID, region/terrain requirements, spawn chance, maximum count per chunk, origin-distance constraint, and footprint size.

A structure footprint is treated atomically: every cell in the footprint must be eligible before the structure is stamped.

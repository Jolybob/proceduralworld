# Structures

The structure layer places deterministic multi-cell structure footprints into generated chunk data.

Structures are the first feature type implemented on top of the package's generic world-space feature planning kernel.

## Responsibilities

- define structure-specific content and region/terrain requirements
- use shared deterministic feature placement rules
- discover world-space placements from their deterministic owner chunks
- validate the local chunk intersection before materialization
- stamp structure IDs into generated cells
- remain independent from prefabs, Tilemaps, and rendering

## Architecture

```text
StructureDefinition
       |
       v
IWorldFeaturePlacementDefinition
       |
       v
WorldFeaturePlacementPlanner
       |
       v
WorldFeaturePlacement
       |
       v
StructurePlacement adapter
       |
       v
StructurePlacementPass
       |
       v
GeneratedCell.Structure
```

The shared feature layer owns world-space placement identity, rectangular footprint geometry, deterministic owner-chunk planning, de-duplication, and chunk-intersection discovery. `StructureDefinition` keeps structure-specific content concerns such as region and terrain compatibility.

`IStructurePlacementSource` and `StructurePlacementPlanner` remain available as compatibility-facing structure APIs. Internally they delegate to the generic feature placement implementation.

Structures are disabled by default. A structure definition contains a stable ID, region/terrain requirements, spawn chance, maximum count per owner chunk, origin-distance constraint, and footprint size.

A structure footprint is treated atomically: every cell in the footprint must be eligible before the structure is stamped into any local intersection.

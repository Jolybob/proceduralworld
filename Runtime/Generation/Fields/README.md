# Fields

The field layer produces reusable world-space values. It does not decide which biome, terrain, cave, or structure a cell becomes.

## Responsibilities

- deterministic scalar noise fields
- combined environmental sampling
- world-space measurements such as normalized distance
- shared inputs for later generation systems

The main abstraction is `IEnvironmentFieldProvider`. A provider can be replaced without changing region, terrain, cave, or structure passes.

`EnvironmentSample` is the shared data contract between the field layer and downstream generation decisions.

## Flow

```text
seed + settings
      -> noise fields
      -> IEnvironmentFieldProvider
      -> EnvironmentSample
      -> region resolver / terrain / caves / structures
```

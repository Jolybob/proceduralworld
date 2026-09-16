# Fields

The field layer produces reusable deterministic world-space values. It does not decide which biome, terrain, cave, or structure a cell becomes.

## Responsibilities

- deterministic scalar noise fields
- combined environmental sampling
- cave density sampling
- world-space measurements such as normalized distance
- shared inputs for later generation systems

## Main abstractions

- `INoiseField` — deterministic scalar noise
- `IEnvironmentFieldProvider` — reusable environmental sampling
- `ICaveFieldProvider` — reusable cave-density sampling

Providers are injected into `WorldGenerationContext`, allowing a project to replace one field implementation without changing downstream generation passes.

## Flow

```text
seed + settings
      -> noise fields
      -> environment fields -> EnvironmentSample -> regions
      -> cave fields        -> CavePass -> cell modifiers
      -> future resource / structure fields
```

## Determinism

A field should derive its result only from stable inputs such as seed, world coordinates, and generation settings. Avoid scene state, time, global random state, or renderer state inside field implementations.

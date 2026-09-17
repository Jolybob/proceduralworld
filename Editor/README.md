# World plan graph editor

The Editor assembly contains the Unity GraphView-based authoring UI for `WorldPlanGraphAsset`.

Open it from **Window > Procedural World > World Plan Graph** or the asset inspector.

The graph window is intentionally an editor-only view. It edits serialized node/connection data and canvas positions; it does not instantiate runtime world objects and canvas positions are excluded from deterministic runtime compilation.

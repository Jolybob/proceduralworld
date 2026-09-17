using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldEditServiceTests
    {
        [Test]
        public void WorldCoordinatesMapToCorrectChunkAndLocalCell()
        {
            Assert.AreEqual(new ChunkCoord(0, 0), WorldChunkCoordinates.ToChunk(new WorldPosition(0, 0), 4));
            Assert.AreEqual(new ChunkCoord(0, 0), WorldChunkCoordinates.ToChunk(new WorldPosition(3, 3), 4));
            Assert.AreEqual(new ChunkCoord(1, 0), WorldChunkCoordinates.ToChunk(new WorldPosition(4, 0), 4));
            Assert.AreEqual(new ChunkCoord(-1, 0), WorldChunkCoordinates.ToChunk(new WorldPosition(-1, 0), 4));
            Assert.AreEqual(new ChunkCoord(-2, -1), WorldChunkCoordinates.ToChunk(new WorldPosition(-5, -2), 4));

            Assert.AreEqual(3, WorldChunkCoordinates.ToLocalX(new WorldPosition(-1, 0), 4));
            Assert.AreEqual(2, WorldChunkCoordinates.ToLocalY(new WorldPosition(-5, -2), 4));
        }

        [Test]
        public void EditServiceCanReadAndWriteLoadedWorldCell()
        {
            const int seed = 71593;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var store = new InMemoryWorldChunkStore();
            var persistence = new WorldChunkPersistenceService(generator, store);
            var sink = new TestSink();
            var streaming = new WorldPersistentChunkStreamingController(
                persistence,
                new ChunkStreamingPlanner(0),
                sink);

            streaming.Update(new ChunkCoord(-1, 0));
            var edit = new WorldEditService(streaming, 4);

            WorldPosition position = new WorldPosition(-1, 2);
            Assert.IsTrue(edit.TryGetCell(position, out GeneratedCell before));

            Assert.IsTrue(edit.TrySetTile(position, WorldTile.Core));
            Assert.IsTrue(edit.TryGetCell(position, out GeneratedCell after));
            Assert.AreEqual(WorldTile.Core, after.Tile);
            Assert.AreEqual(before.Region, after.Region);
        }

        [Test]
        public void EditServiceRejectsCellsInUnloadedChunks()
        {
            var edit = new WorldEditService(new EmptyAccess(), 4);
            GeneratedCell cell;

            Assert.IsFalse(edit.TryGetCell(new WorldPosition(12, -8), out cell));
            Assert.IsFalse(edit.TrySetTile(new WorldPosition(12, -8), WorldTile.Core));
        }

        private sealed class EmptyAccess : IWorldChunkAccess
        {
            public bool TryGetChunk(ChunkCoord coordinate, out GeneratedChunk chunk)
            {
                chunk = null;
                return false;
            }

            public bool TryGetCell(WorldPosition position, out GeneratedCell cell)
            {
                cell = default(GeneratedCell);
                return false;
            }

            public bool SetCell(WorldPosition position, GeneratedCell cell)
            {
                return false;
            }
        }

        private sealed class TestSink : IWorldChunkSink
        {
            private readonly System.Collections.Generic.Dictionary<ChunkCoord, GeneratedChunk> chunks =
                new System.Collections.Generic.Dictionary<ChunkCoord, GeneratedChunk>();

            public void Load(ChunkCoord coordinate, GeneratedChunk chunk)
            {
                chunks[coordinate] = chunk;
            }

            public void Unload(ChunkCoord coordinate)
            {
                chunks.Remove(coordinate);
            }
        }
    }
}

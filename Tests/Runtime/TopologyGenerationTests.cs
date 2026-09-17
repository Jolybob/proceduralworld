using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class TopologyGenerationTests
    {
        [Test]
        public void GeneratedCellsStartWithConsistentTopology()
        {
            var solid = new GeneratedCell(WorldTile.Core, 0);
            var empty = new GeneratedCell(WorldTile.Empty, 0);

            Assert.AreEqual(CellTopology.Solid, solid.Topology);
            Assert.AreEqual(CellTopology.Empty, empty.Topology);
        }

        [Test]
        public void VoronoiEdgeFieldIsDeterministic()
        {
            var a = new VoronoiEdgeField(51746, 80f);
            var b = new VoronoiEdgeField(51746, 80f);

            var points = new[]
            {
                new WorldPosition(0, 0),
                new WorldPosition(37, 81),
                new WorldPosition(-123, 49),
                new WorldPosition(512, -257)
            };

            for (int i = 0; i < points.Length; i++)
            {
                float first = a.SampleEdgeDistance(points[i].X, points[i].Y);
                float second = b.SampleEdgeDistance(points[i].X, points[i].Y);
                Assert.AreEqual(first, second);
            }
        }

        [Test]
        public void ChasmPassMarksVoronoiBoundaries()
        {
            var settings = new WorldGenerationSettings
            {
                chunkSize = 32,
                chasmsEnabled = true,
                chasmCellSize = 16f,
                chasmWidth = 6f,
                chasmMinimumDistance = 0f
            };
            var field = new VoronoiEdgeField(12345, settings.chasmCellSize);
            var chunk = new GeneratedChunk(new ChunkCoord(0, 0), settings.chunkSize);
            var context = new WorldGenerationContext(
                12345,
                settings,
                new ChunkCoord(0, 0),
                chunk,
                new SeededPerlinNoiseField(12345, 0.02f),
                new DefaultEnvironmentFieldProvider(12345, settings,
                    new SeededPerlinNoiseField(12345, 0.02f)),
                new DefaultCaveFieldProvider(12345, settings),
                new WorldRandomService(12345),
                ResourceCatalog.CreateDefault(),
                StructureCatalog.CreateDefault());

            for (int y = 0; y < settings.chunkSize; y++)
            {
                for (int x = 0; x < settings.chunkSize; x++)
                    chunk.SetCell(x, y, new GeneratedCell(WorldTile.Deep, 1));
            }

            new ChasmPass(field, settings).Execute(context);

            int chasmCount = 0;
            for (int i = 0; i < chunk.Cells.Length; i++)
            {
                if (chunk.Cells[i].Topology != CellTopology.Chasm)
                    continue;

                chasmCount++;
                Assert.IsTrue((chunk.Cells[i].Flags & GeneratedCellFlags.Chasm) != 0);
                Assert.AreEqual(WorldTile.Empty, chunk.Cells[i].Tile);
            }

            Assert.Greater(chasmCount, 0);
        }

        [Test]
        public void ChasmGenerationIsContinuousAcrossChunkBoundaries()
        {
            const int chunkSize = 32;
            const float cellSize = 16f;
            var settings = new WorldGenerationSettings
            {
                chunkSize = chunkSize,
                chasmsEnabled = true,
                chasmCellSize = cellSize,
                chasmWidth = 3f,
                chasmMinimumDistance = 0f
            };
            var field = new VoronoiEdgeField(777, cellSize);
            var left = new GeneratedChunk(new ChunkCoord(0, 0), chunkSize);
            var right = new GeneratedChunk(new ChunkCoord(1, 0), chunkSize);

            var contextLeft = new WorldGenerationContext(
                777, settings, new ChunkCoord(0, 0), left,
                new SeededPerlinNoiseField(777, 0.02f));
            var contextRight = new WorldGenerationContext(
                777, settings, new ChunkCoord(1, 0), right,
                new SeededPerlinNoiseField(777, 0.02f));

            for (int i = 0; i < left.Cells.Length; i++)
                left.Cells[i] = new GeneratedCell(WorldTile.Deep, 1);
            for (int i = 0; i < right.Cells.Length; i++)
                right.Cells[i] = new GeneratedCell(WorldTile.Deep, 1);

            new ChasmPass(field, settings).Execute(contextLeft);
            new ChasmPass(field, settings).Execute(contextRight);

            for (int y = 0; y < chunkSize; y++)
            {
                int worldX = chunkSize;
                bool expected = field.SampleEdgeDistance(worldX, y) <= settings.chasmWidth;
                bool actual = right.GetCell(0, y).Topology == CellTopology.Chasm;
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void PersistenceIncludesTopologyState()
        {
            var solid = new GeneratedCell(WorldTile.Deep, 1);
            var chasm = solid;
            chasm.SetTopology(CellTopology.Chasm, WorldTile.Empty);

            Assert.AreNotEqual(solid.Topology, chasm.Topology);
        }
    }
}

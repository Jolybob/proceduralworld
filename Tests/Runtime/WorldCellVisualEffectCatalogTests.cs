using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Jolybob.ProceduralWorld.Tilemap;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldCellVisualEffectCatalogTests
    {
        private TileBase firstTile;
        private TileBase secondTile;

        [SetUp]
        public void SetUp()
        {
            firstTile = ScriptableObject.CreateInstance<Tile>();
            secondTile = ScriptableObject.CreateInstance<Tile>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(firstTile);
            UnityEngine.Object.DestroyImmediate(secondTile);
        }

        [Test]
        public void EffectsAreEvaluatedByOrder()
        {
            var catalog = new WorldCellVisualEffectCatalog(new IWorldCellVisualEffect[]
            {
                new StubEffect(20, secondTile, true),
                new StubEffect(10, firstTile, true)
            });

            TileBase resolved;
            Assert.That(catalog.TryResolve(new GeneratedCell(WorldTile.Deep, 1), out resolved), Is.True);
            Assert.That(resolved, Is.SameAs(firstTile));
        }

        [Test]
        public void UnresolvedEffectFallsThroughToNextEffect()
        {
            var catalog = new WorldCellVisualEffectCatalog(new IWorldCellVisualEffect[]
            {
                new StubEffect(10, firstTile, false),
                new StubEffect(20, secondTile, true)
            });

            TileBase resolved;
            Assert.That(catalog.TryResolve(new GeneratedCell(WorldTile.Deep, 1), out resolved), Is.True);
            Assert.That(resolved, Is.SameAs(secondTile));
        }

        [Test]
        public void NoEffectMatchReturnsFalseAndNullTile()
        {
            var catalog = new WorldCellVisualEffectCatalog(new IWorldCellVisualEffect[]
            {
                new StubEffect(10, firstTile, false)
            });

            TileBase resolved;
            Assert.That(catalog.TryResolve(new GeneratedCell(WorldTile.Deep, 1), out resolved), Is.False);
            Assert.That(resolved, Is.Null);
        }

        [Test]
        public void DuplicateEffectOrderFailsFast()
        {
            Assert.Throws<ArgumentException>(() => new WorldCellVisualEffectCatalog(new IWorldCellVisualEffect[]
            {
                new StubEffect(10, firstTile, true),
                new StubEffect(10, secondTile, true)
            }));
        }

        [Test]
        public void NullEffectFailsFast()
        {
            Assert.Throws<ArgumentException>(() => new WorldCellVisualEffectCatalog(new IWorldCellVisualEffect[]
            {
                null
            }));
        }

        private sealed class StubEffect : IWorldCellVisualEffect
        {
            private readonly TileBase tile;
            private readonly bool shouldResolve;

            public StubEffect(int order, TileBase tile, bool shouldResolve)
            {
                Order = order;
                this.tile = tile;
                this.shouldResolve = shouldResolve;
            }

            public int Order { get; }

            public bool TryResolve(GeneratedCell cell, out TileBase resolvedTile)
            {
                resolvedTile = shouldResolve ? tile : null;
                return shouldResolve;
            }
        }
    }
}

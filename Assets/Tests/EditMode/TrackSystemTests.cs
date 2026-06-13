using System;
using NUnit.Framework;
using HorseBetting.Config;
using HorseBetting.Data;
using HorseBetting.Systems;
using UnityEngine;

namespace HorseBetting.Tests.EditMode
{
    [TestFixture]
    public class TrackSystemTests
    {
        private TrackConfig _config;
        private TrackSystem _system;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TrackConfig>();
            _config.horsePreferences = new TrackPreference[]
            {
                new TrackPreference { horseIndex = 0, grassModifier = 2, mudModifier = -1, snowModifier = 0 },
                new TrackPreference { horseIndex = 1, grassModifier = -2, mudModifier = 3, snowModifier = 1 },
                new TrackPreference { horseIndex = 2, grassModifier = 0, mudModifier = 0, snowModifier = 2 },
                new TrackPreference { horseIndex = 3, grassModifier = 1, mudModifier = 1, snowModifier = -3 },
                new TrackPreference { horseIndex = 4, grassModifier = -1, mudModifier = 2, snowModifier = 1 },
                new TrackPreference { horseIndex = 5, grassModifier = 3, mudModifier = -2, snowModifier = -1 },
                new TrackPreference { horseIndex = 6, grassModifier = 0, mudModifier = 1, snowModifier = 3 },
                new TrackPreference { horseIndex = 7, grassModifier = 1, mudModifier = -1, snowModifier = -2 }
            };
            _system = new TrackSystem(_config);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void SelectTrack_ReturnsValidTrackType()
        {
            TrackType result = _system.SelectTrack();

            Assert.IsTrue(
                result == TrackType.Grass || result == TrackType.Mud || result == TrackType.Snow,
                $"SelectTrack returned invalid TrackType: {result}");
        }

        [Test]
        public void SelectTrack_SetsCurrentTrack()
        {
            TrackType result = _system.SelectTrack();

            Assert.AreEqual(result, _system.CurrentTrack);
        }

        [Test]
        public void SelectTrack_CanProduceAllThreeTypes()
        {
            bool hasGrass = false, hasMud = false, hasSnow = false;

            // Run enough times to statistically guarantee all types appear
            for (int i = 0; i < 200; i++)
            {
                TrackType track = _system.SelectTrack();
                switch (track)
                {
                    case TrackType.Grass: hasGrass = true; break;
                    case TrackType.Mud: hasMud = true; break;
                    case TrackType.Snow: hasSnow = true; break;
                }

                if (hasGrass && hasMud && hasSnow) break;
            }

            Assert.IsTrue(hasGrass, "SelectTrack never produced Grass");
            Assert.IsTrue(hasMud, "SelectTrack never produced Mud");
            Assert.IsTrue(hasSnow, "SelectTrack never produced Snow");
        }

        [Test]
        public void GetTrackModifier_ReturnsGrassModifier()
        {
            int modifier = _system.GetTrackModifier(0, TrackType.Grass);

            Assert.AreEqual(2, modifier);
        }

        [Test]
        public void GetTrackModifier_ReturnsMudModifier()
        {
            int modifier = _system.GetTrackModifier(1, TrackType.Mud);

            Assert.AreEqual(3, modifier);
        }

        [Test]
        public void GetTrackModifier_ReturnsSnowModifier()
        {
            int modifier = _system.GetTrackModifier(2, TrackType.Snow);

            Assert.AreEqual(2, modifier);
        }

        [Test]
        public void GetTrackModifier_CorrectForAllHorses()
        {
            // Verify all 8 horses return correct grass modifier
            int[] expectedGrass = { 2, -2, 0, 1, -1, 3, 0, 1 };
            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual(expectedGrass[i], _system.GetTrackModifier(i, TrackType.Grass),
                    $"Grass modifier incorrect for horse {i}");
            }
        }

        [Test]
        public void GetTrackModifier_ThrowsForInvalidHorseIndex()
        {
            Assert.Throws<ArgumentException>(() => _system.GetTrackModifier(99, TrackType.Grass));
        }

        [Test]
        public void GetTrackModifier_ThrowsWhenPreferencesNull()
        {
            _config.horsePreferences = null;
            Assert.Throws<InvalidOperationException>(() => _system.GetTrackModifier(0, TrackType.Grass));
        }

        [Test]
        public void Reset_DoesNotThrow()
        {
            _system.SelectTrack();
            Assert.DoesNotThrow(() => _system.Reset());
        }

        [Test]
        public void Initialize_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _system.Initialize());
        }

        [Test]
        public void CurrentTrack_DefaultsToGrass()
        {
            // Before SelectTrack is called, CurrentTrack should be default (Grass = 0)
            Assert.AreEqual(TrackType.Grass, _system.CurrentTrack);
        }

        [Test]
        public void Reset_ResetsCurrentTrackToDefault()
        {
            _system.SelectTrack();
            _system.Reset();

            Assert.AreEqual(default(TrackType), _system.CurrentTrack);
        }
    }
}

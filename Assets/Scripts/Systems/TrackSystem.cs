using System;
using HorseBetting.Config;
using HorseBetting.Core;
using HorseBetting.Data;

namespace HorseBetting.Systems
{
    public class TrackSystem : ITrackSystem
    {
        private readonly TrackConfig _config;
        private readonly Random _random;
        private TrackType _currentTrack;

        public TrackType CurrentTrack => _currentTrack;

        public TrackSystem(TrackConfig config)
        {
            _config = config;
            _random = new Random();
        }

        public void Initialize()
        {
            // No-op: track selection is triggered explicitly via SelectTrack
        }

        public void Reset()
        {
            _currentTrack = default;
        }

        public TrackType SelectTrack()
        {
            TrackType[] trackTypes = (TrackType[])Enum.GetValues(typeof(TrackType));
            int index = _random.Next(trackTypes.Length);
            _currentTrack = trackTypes[index];
            return _currentTrack;
        }

        public int GetTrackModifier(int horseIndex, TrackType track)
        {
            if (_config.horsePreferences == null)
                throw new InvalidOperationException("Track config horse preferences are not set.");

            for (int i = 0; i < _config.horsePreferences.Length; i++)
            {
                TrackPreference pref = _config.horsePreferences[i];
                if (pref.horseIndex == horseIndex)
                {
                    return track switch
                    {
                        TrackType.Grass => pref.grassModifier,
                        TrackType.Mud => pref.mudModifier,
                        TrackType.Snow => pref.snowModifier,
                        _ => throw new ArgumentOutOfRangeException(nameof(track))
                    };
                }
            }

            throw new ArgumentException($"No track preference found for horse index {horseIndex}.", nameof(horseIndex));
        }
    }
}

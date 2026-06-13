using HorseBetting.Data;

namespace HorseBetting.Core
{
    public interface ITrackSystem : IGameSystem
    {
        TrackType SelectTrack();
        int GetTrackModifier(int horseIndex, TrackType track);
        TrackType CurrentTrack { get; }
    }
}

namespace Mapify.Editor
{
#if UNITY_EDITOR

    internal class SnappedTrack
    {
        public Track Track { get; private set; }
        public BezierPoint Point { get; private set; }

        public SnappedTrack(Track aTrack, BezierPoint aPoint)
        {
            Track = aTrack;
            Point = aPoint;
        }

        public void UnSnapped()
        {
            if (!Track){ return; }
            Track.UnSnapped(Point);
        }
    }

#endif
}

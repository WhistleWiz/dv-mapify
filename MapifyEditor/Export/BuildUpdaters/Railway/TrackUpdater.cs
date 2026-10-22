#if UNITY_EDITOR
using Mapify.Editor.Utils;

namespace Mapify.Editor.StateUpdaters
{
    public class TrackUpdater : BuildUpdater
    {
        protected override void Update(Scenes scenes)
        {
            var tracks = scenes.railwayScene.GetAllComponents<Track>();
            foreach (var track in tracks)
            {
                track.InEditorName = track.gameObject.name;
            }
        }
    }
}
#endif

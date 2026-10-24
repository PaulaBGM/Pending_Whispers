using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

[TrackClipType(typeof(BookSpriteClip))]
[TrackBindingType(typeof(UIDocument))]
public class BookSpriteTrack : TrackAsset
{
    public override Playable CreateTrackMixer(
        PlayableGraph graph,
        GameObject go,
        int inputCount)
    {
        return ScriptPlayable<BookSpriteMixerBehavior>
            .Create(graph, inputCount);
    }
}
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class BookSpriteClip : PlayableAsset, ITimelineClipAsset
{
    public BookSpriteBehaviour template = new BookSpriteBehaviour();

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(
        PlayableGraph graph,
        GameObject owner)
    {
        return ScriptPlayable<BookSpriteBehaviour>.Create(
            graph,
            template
        );
    }
}

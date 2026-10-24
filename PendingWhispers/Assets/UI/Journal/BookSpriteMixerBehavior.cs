using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

public class BookSpriteMixerBehavior : PlayableBehaviour
{
    public override void ProcessFrame(
        Playable playable,
        FrameData info,
        object playerData)
    {
        UIDocument document = playerData as UIDocument;

        if (document == null)
            return;

        VisualElement root = document.rootVisualElement;

        for (int i = 0; i < playable.GetInputCount(); i++)
        {
            if (playable.GetInputWeight(i) <= 0f)
                continue;

            Playable input = playable.GetInput(i);

            if (!input.IsValid())
                continue;

            var clipPlayable =
                (ScriptPlayable<BookSpriteBehaviour>)input;

            BookSpriteBehaviour clip = clipPlayable.GetBehaviour();

            if (clip.frames == null || clip.frames.Length == 0)
                continue;

            VisualElement element =
                root.Q<VisualElement>(clip.elementName);

            if (element == null)
                continue;

            double duration = input.GetDuration();

            float progress = duration > 0
                ? Mathf.Clamp01((float)(input.GetTime() / duration))
                : 1f;

            int index = Mathf.Min(
                clip.frames.Length - 1,
                Mathf.FloorToInt(progress * clip.frames.Length)
            );

            if (clip.frames[index] != null)
            {
                element.style.backgroundImage =
                    new StyleBackground(clip.frames[index]);
            }
        }
    }
}
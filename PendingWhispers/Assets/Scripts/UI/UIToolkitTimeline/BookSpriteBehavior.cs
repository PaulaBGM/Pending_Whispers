using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class BookSpriteBehaviour : PlayableBehaviour
{
    public string elementName = "BookAnimation";
    public Sprite[] frames;
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CharacterUIController : MonoBehaviour
{
    public static CharacterUIController Instance;

    public Image leftCharacter;
    public Image rightCharacter;

    public float fadeSpeed = 5f;

    [Header("Colors")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    void Awake()
    {
        Instance = this;
    }

    public void SetCharacter(DialogueCharacter character, Sprite expressionSprite)
    {
        if (character == null)
            return;

        Image target = character.isLeftSide
            ? leftCharacter
            : rightCharacter;

        target.sprite = expressionSprite;
        target.gameObject.SetActive(true);

        SetActiveSpeaker(character.isLeftSide);
    }

    public void SetActiveSpeaker(bool isLeft)
    {
        StopAllCoroutines();

        if (isLeft)
        {
            StartCoroutine(Fade(leftCharacter, activeColor));
            StartCoroutine(Fade(rightCharacter, inactiveColor));
        }
        else
        {
            StartCoroutine(Fade(leftCharacter, inactiveColor));
            StartCoroutine(Fade(rightCharacter, activeColor));
        }
    }

    IEnumerator Fade(Image img, Color target)
    {
        while (Vector4.Distance(img.color, target) > 0.01f)
        {
            img.color = Color.Lerp(img.color, target, Time.deltaTime * fadeSpeed);
            yield return null;
        }

        img.color = target;
    }

    public void ResetCharacters()
    {
        leftCharacter.color = activeColor;
        rightCharacter.color = activeColor;
    }
}
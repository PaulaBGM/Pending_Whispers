using UnityEngine;

[System.Serializable]
public class DialogueCharacter
{
    public string id;
    public string displayName;

    // Sprite completo para diálogo
    public Sprite sprite;
    
    //NUEVO: retrato para journal / UI
    public Sprite portrait;

    public bool isLeftSide;
}
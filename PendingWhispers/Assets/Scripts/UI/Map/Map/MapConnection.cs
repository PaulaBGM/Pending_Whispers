using UnityEngine;

public class MapConnection : MonoBehaviour
{
    public MapNode fromNode;
    public MapNode toNode;

    public bool isUnlocked = false;
    public GameObject visualPath;

    public void Unlock()
    {
        isUnlocked = true;
        visualPath.SetActive(true);
    }
}
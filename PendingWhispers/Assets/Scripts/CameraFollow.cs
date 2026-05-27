using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private PlayerController_MovementInteraction player;

    [SerializeField] private Transform minLimit;
    [SerializeField] private Transform maxLimit;

    private float halfHeight;
    private float halfWidth;

    void Start()
    {
        Camera cam = Camera.main;
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * cam.aspect;
    }

    void OnEnable()
    {
        PlayerController_MovementInteraction.OnPlayerSpawned += SetPlayer;
    }

    void OnDisable()
    {
        PlayerController_MovementInteraction.OnPlayerSpawned -= SetPlayer;

    }

    void SetPlayer(PlayerController_MovementInteraction p)
    {
        player = p;
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 pos = player.transform.position;

        float x = Mathf.Clamp(pos.x, minLimit.position.x + halfWidth, maxLimit.position.x - halfWidth);
        float y = Mathf.Clamp(pos.y, minLimit.position.y + halfHeight, maxLimit.position.y - halfHeight);

        transform.position = new Vector3(x, y, transform.position.z);
    }
}
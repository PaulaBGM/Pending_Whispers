using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private PlayerController player;

    [Header("Camera Limits")] 
    [SerializeField] private Transform minLimit;
    [SerializeField] private Transform maxLimit;

    private float halfHeight;
    private float halfWidth;

    void Start()
    {
        halfHeight = Camera.main.orthographicSize;
        halfWidth = halfHeight * Camera.main.aspect;
    }

    void OnEnable()
    {
        PlayerController.OnPlayerSpawned += SetPlayer;
    }

    void OnDisable()
    {
        PlayerController.OnPlayerSpawned -= SetPlayer;
    }

    void SetPlayer(PlayerController p)
    {
        player = p;
    }

    void LateUpdate()
    {
        if (player == null) return;

        float clampedX = Mathf.Clamp(player.transform.position.x, minLimit.position.x + halfWidth, maxLimit.position.x - halfWidth);

        float clampedY = Mathf.Clamp(player.transform.position.y, minLimit.position.y + halfHeight, maxLimit.position.y - halfHeight);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}
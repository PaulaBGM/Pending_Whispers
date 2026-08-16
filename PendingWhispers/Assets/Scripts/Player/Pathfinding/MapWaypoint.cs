using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MapWaypoint : MonoBehaviour
{
    [SerializeField] private List<MapWaypoint> connections = new();

    public IReadOnlyList<MapWaypoint> Connections => connections;

    private RectTransform rectTransform;

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            return rectTransform;
        }
    }

    public Vector2 AnchoredPosition => RectTransform.anchoredPosition;
}
using UnityEngine;

public class WormController : MonoBehaviour
{
    [Header("Links")]
    public Transform[] segments;

    [Header("Movement")]
    public float segmentSpacing = 1.5f;
    public float minMoveThreshold = 0.0001f;

    private void Start()
    {
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].SetParent(null);
        }
    }

    void Update()
    {
        MoveSegments();
    }

    void MoveSegments()
    {
        Vector3 previousPos = transform.position;

        for (int i = 0; i < segments.Length; i++)
        {
            Transform seg = segments[i];

            Vector3 toPrev = previousPos - seg.localPosition;
            float distance = toPrev.magnitude;

            if (distance < minMoveThreshold)
            {
                previousPos = seg.localPosition;
                continue;
            }

            if (distance > segmentSpacing)
            {
                Vector3 newPos = previousPos - toPrev.normalized * segmentSpacing;
                seg.localPosition = newPos;
            }

            previousPos = seg.localPosition;
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < segments.Length; i++)
        {
            Destroy(segments[i].gameObject);
        }
    }
}
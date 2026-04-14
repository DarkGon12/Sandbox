using UnityEngine;

[DisallowMultipleComponent]
public class InupObjectView : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Detection")]
    [SerializeField] private float activationDistance = 4f;

    [Header("Rotation")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 35f;

    [Header("Scale")]
    [SerializeField] private float hoverScaleMultiplier = 1.08f;
    [SerializeField] private float scaleLerpSpeed = 6f;

    [Header("URP Light")]
    [SerializeField] private Light targetLight;
    [SerializeField] private float normalIntensity = 1f;
    [SerializeField] private float highlightedIntensity = 2.2f;
    [SerializeField] private float lightPulseSpeed = 4f;
    [SerializeField] private float lightLerpSpeed = 6f;

    [Header("Optional Hover Motion")]
    [SerializeField] private bool useHoverMotion = false;
    [SerializeField] private float hoverAmplitude = 0.08f;
    [SerializeField] private float hoverSpeed = 2f;

    private Vector3 _baseScale;
    private Vector3 _basePosition;
    private float _pulseTime;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _basePosition = transform.localPosition;

        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag(playerTag);
            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
        }

        if (targetLight != null)
        {
            targetLight.intensity = normalIntensity;
        }
    }

    private void Update()
    {
        RotateObject();

        bool isPlayerNear = IsPlayerNear();

        UpdateScale(isPlayerNear);
        UpdateLight(isPlayerNear);
        UpdateHover(isPlayerNear);
    }

    private void RotateObject()
    {
        transform.Rotate(rotationAxis.normalized, rotationSpeed * Time.deltaTime, Space.Self);
    }

    private bool IsPlayerNear()
    {
        if (player == null)
            return false;

        float distance = Vector3.Distance(player.position, transform.position);
        return distance <= activationDistance;
    }

    private void UpdateScale(bool isHighlighted)
    {
        Vector3 targetScale = isHighlighted
            ? _baseScale * hoverScaleMultiplier
            : _baseScale;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * scaleLerpSpeed
        );
    }

    private void UpdateLight(bool isHighlighted)
    {
        if (targetLight == null)
            return;

        float targetIntensity = normalIntensity;

        if (isHighlighted)
        {
            _pulseTime += Time.deltaTime * lightPulseSpeed;

            // Пульсация от 0 до 1
            float pulse = (Mathf.Sin(_pulseTime) + 1f) * 0.5f;

            // Немного гуляем вокруг highlightedIntensity
            float pulseIntensity = Mathf.Lerp(
                highlightedIntensity * 0.85f,
                highlightedIntensity,
                pulse
            );

            targetIntensity = pulseIntensity;
        }
        else
        {
            _pulseTime = 0f;
        }

        targetLight.intensity = Mathf.Lerp(
            targetLight.intensity,
            targetIntensity,
            Time.deltaTime * lightLerpSpeed
        );
    }

    private void UpdateHover(bool isHighlighted)
    {
        if (!useHoverMotion)
            return;

        Vector3 targetPosition = _basePosition;

        if (isHighlighted)
        {
            float offsetY = Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
            targetPosition = _basePosition + new Vector3(0f, offsetY, 0f);
        }

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * 6f
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}
using DG.Tweening;
using UnityEngine;
using TMPro;

public class CreaturePanelTutorial : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI _tutorialTitle;
    [SerializeField] private Color _color; 
    [SerializeField] private float _colorDuration = 1.2f;
    [SerializeField] private float _pulseTextDuration = 0.8f;
    [SerializeField] private float _pulseTextScale = 1.05f;
    [Header("Hand")]
    [SerializeField] private Transform _tutorialHand;
    [SerializeField] private float _pulseScale = 1.1f;
    [SerializeField] private float _duration = 0.6f;

    [Header("Refs")]
    [SerializeField] private Transform _newParent;
    [SerializeField] private CarControlButton _shootButton;

    private int _touchCount = 0;
    private Vector3 _startScale;
    private Sequence _sequence;

    private void OnEnable() => PlayTextAnimation();

    private void OnDisable() => _sequence?.Kill();

    public void PlayTextAnimation()
    {
        _sequence?.Kill();

        _tutorialTitle.transform.localScale = Vector3.one;
        Color startColor = _tutorialTitle.color;
        _sequence = DOTween.Sequence();

        _sequence.Append(
            _tutorialTitle
                .DOColor(_color, _colorDuration)
                .SetEase(Ease.InOutSine)
        )
        .Append(
            _tutorialTitle
                .DOColor(startColor, _colorDuration)
                .SetEase(Ease.InOutSine)
        )
        .SetLoops(-1);

        _tutorialTitle.transform
            .DOScale(_pulseTextScale, _pulseTextDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        _tutorialTitle
            .DOFade(0.85f, 1.2f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void Start()
    {
        _shootButton.OnDown += NextStep;
        _startScale = _tutorialHand.localScale;

        _tutorialHand
            .DOScale(_startScale * _pulseScale, _duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void NextStep()
    {
        _touchCount += 1;
        _tutorialHand.gameObject.SetActive(true);
        _tutorialHand.parent = _newParent;

        RectTransform rt = _tutorialHand.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;

        if (_touchCount > 1)
        {
            _shootButton.OnDown -= NextStep;
            _tutorialHand.gameObject.SetActive(false);
        }
    }
}
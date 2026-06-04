using System.Collections;
using Sandbox.Player;
using Sandbox.Weapon;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(100)]
public class RainbowWeaponModification : MonoBehaviour
{
    [SerializeField] private CarControlButton _fireButton;

    [SerializeField] private Material _emotion_1;
    [SerializeField] private Material _emotion_2;

    [SerializeField] private MeshRenderer _emotionDisplay;

    [SerializeField] private AudioSource _shootSource;

    [SerializeField] private WeaponStandart _weaponStandart;

    private EventTrigger _eventTrigger;
    private EventTrigger.Entry _downEntry;
    private EventTrigger.Entry _upEntry;

    private Coroutine _setupCoroutine;

    private bool _isPressed;
    private bool _effectIsActive;

    private void Awake()
    {
        if (_weaponStandart == null)
            _weaponStandart = GetComponent<WeaponStandart>();

        if (_shootSource == null)
            _shootSource = GetComponent<AudioSource>();

        if (_shootSource != null)
            _shootSource.loop = true;
    }

    private void OnEnable()
    {
        _isPressed = false;
        _effectIsActive = false;

        SetEmotion(_emotion_1);

        _setupCoroutine = StartCoroutine(SetupButtonNextFrame());
    }

    private void OnDisable()
    {
        if (_setupCoroutine != null)
        {
            StopCoroutine(_setupCoroutine);
            _setupCoroutine = null;
        }

        RemoveButtonListeners();

        _isPressed = false;
        DisableFiringEffect();
    }

    private void Update()
    {
        if (!_isPressed)
            return;

        if (_weaponStandart != null && _weaponStandart.IsReloading)
        {
            DisableFiringEffect();
            return;
        }

        EnableFiringEffect();
    }

    private IEnumerator SetupButtonNextFrame()
    {
        yield return null;

        SetupButton();
    }

    private void SetupButton()
    {
        if (_fireButton == null)
            return;

        if (_eventTrigger == null)
            _eventTrigger = _fireButton.GetComponent<EventTrigger>()
                            ?? _fireButton.gameObject.AddComponent<EventTrigger>();

        RemoveButtonListeners();

        _downEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerDown
        };
        _downEntry.callback.AddListener((_) => StartFiringEffect());

        _upEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerUp
        };
        _upEntry.callback.AddListener((_) => StopFiringEffect());

        _eventTrigger.triggers.Add(_downEntry);
        _eventTrigger.triggers.Add(_upEntry);
    }

    private void RemoveButtonListeners()
    {
        if (_eventTrigger == null)
            return;

        if (_downEntry != null)
            _eventTrigger.triggers.Remove(_downEntry);

        if (_upEntry != null)
            _eventTrigger.triggers.Remove(_upEntry);

        _downEntry = null;
        _upEntry = null;
    }

    private void StartFiringEffect()
    {
        _isPressed = true;

        if (_weaponStandart != null && _weaponStandart.IsReloading)
        {
            DisableFiringEffect();
            return;
        }

        EnableFiringEffect();
    }

    private void StopFiringEffect()
    {
        _isPressed = false;

        DisableFiringEffect();
    }

    private void EnableFiringEffect()
    {
        if (_effectIsActive)
            return;

        _effectIsActive = true;

        SetEmotion(_emotion_2);

        if (_shootSource != null && !_shootSource.isPlaying)
            _shootSource.Play();
    }

    private void DisableFiringEffect()
    {
        if (!_effectIsActive)
        {
            SetEmotion(_emotion_1);

            if (_shootSource != null && _shootSource.isPlaying)
                _shootSource.Stop();

            return;
        }

        _effectIsActive = false;

        SetEmotion(_emotion_1);

        if (_shootSource != null)
            _shootSource.Stop();
    }

    private void SetEmotion(Material emotionMaterial)
    {
        if (_emotionDisplay == null || emotionMaterial == null)
            return;

        Material[] materials = _emotionDisplay.materials;

        if (materials.Length < 2)
            return;

        materials[1] = emotionMaterial;
        _emotionDisplay.materials = materials;
    }
}
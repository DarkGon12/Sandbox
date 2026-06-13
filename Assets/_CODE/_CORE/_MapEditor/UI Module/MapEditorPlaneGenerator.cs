using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sandbox.MapEditor
{
    public sealed class MapEditorPlaneGenerator : MonoBehaviour
    {
        public static GameObject CurrentPlane { get; private set; }

        private enum PlaneSizeMode
        {
            TransformScale,
            ExactWorldUnits
        }

        [Header("Input")]
        [SerializeField] private TMP_InputField _mapSizeInput;
        [SerializeField] private Button _acceptButton;
        [SerializeField] private int _minMapSize = 1;
        [SerializeField] private int _maxMapSize = 99;

        [Header("Plane")]
        [SerializeField] private string _generatedPlaneName = "MapEditor_GeneratedPlane";
        [SerializeField] private Transform _planeParent;
        [SerializeField] private Material _planeMaterial;
        [SerializeField] private PlaneSizeMode _sizeMode = PlaneSizeMode.TransformScale;
        [SerializeField] private float _unityPlaneBaseSize = 10f;

        private GameObject _generatedPlane;

        private void Awake()
        {
            SetupInputField();
        }

        private void OnEnable()
        {
            if (_acceptButton != null)
                _acceptButton.onClick.AddListener(AcceptMapSize);
        }

        private void OnDisable()
        {
            if (_acceptButton != null)
                _acceptButton.onClick.RemoveListener(AcceptMapSize);
        }

        public void AcceptMapSize()
        {
            if (!TryGetMapSize(out int mapSize))
                return;

            GeneratePlane(mapSize);
        }

        private void GeneratePlane(int mapSize)
        {
            DestroyPreviousPlane();

            _generatedPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _generatedPlane.name = _generatedPlaneName;
            _generatedPlane.transform.SetParent(_planeParent, false);
            _generatedPlane.transform.localPosition = Vector3.zero;
            _generatedPlane.transform.localRotation = Quaternion.identity;
            _generatedPlane.transform.localScale = GetPlaneScale(mapSize);

            if (_planeMaterial != null && _generatedPlane.TryGetComponent(out Renderer renderer))
                renderer.sharedMaterial = _planeMaterial;

            CurrentPlane = _generatedPlane;
        }

        private Vector3 GetPlaneScale(int mapSize)
        {
            if (_sizeMode == PlaneSizeMode.ExactWorldUnits)
            {
                float baseSize = Mathf.Max(0.01f, _unityPlaneBaseSize);
                float exactScale = mapSize / baseSize;
                return new Vector3(exactScale, 1f, exactScale);
            }

            return new Vector3(mapSize, 1f, mapSize);
        }

        private void DestroyPreviousPlane()
        {
            if (_generatedPlane == null)
                _generatedPlane = GameObject.Find(_generatedPlaneName);

            if (_generatedPlane != null)
            {
                _generatedPlane.SetActive(false);
                Destroy(_generatedPlane);
            }

            if (CurrentPlane == _generatedPlane)
                CurrentPlane = null;

            _generatedPlane = null;
        }

        private bool TryGetMapSize(out int mapSize)
        {
            mapSize = 0;

            if (_mapSizeInput == null || string.IsNullOrWhiteSpace(_mapSizeInput.text))
                return false;

            if (!int.TryParse(_mapSizeInput.text, out int parsedSize))
                return false;

            if (parsedSize < _minMapSize)
                return false;

            mapSize = Mathf.Min(parsedSize, _maxMapSize);
            return true;
        }

        private void SetupInputField()
        {
            if (_mapSizeInput == null)
                return;

            _mapSizeInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _mapSizeInput.characterLimit = 2;
        }
    }
}

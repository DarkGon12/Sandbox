using MessagePipe;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Sandbox.MapEditor
{
    public sealed class MapEditorPlanePainter : MonoBehaviour
    {
        [Serializable]
        public sealed class PaintMaterial
        {
            private const string BaseMapProperty = "_BaseMap";
            private const string MainTexProperty = "_MainTex";

            [SerializeField] private string _name;
            [SerializeField] private Sprite _textureAvatar;
            [SerializeField] private Material _material;
            [SerializeField] private Texture _texture;
            [SerializeField] private Color _fallbackColor = Color.white;
            [SerializeField] private float _textureTiling = 1f;

            private Texture2D _readableTexture;
            private Texture _readableSource;

            public Sprite Avatar => _textureAvatar;
            public Material Material => _material;
            public Texture Texture => _texture != null ? _texture : GetMaterialTexture();
            public Color FallbackColor => _material != null && _material.HasProperty("_BaseColor")
                ? _material.GetColor("_BaseColor")
                : _fallbackColor;
            public float TextureTiling => Mathf.Max(0.01f, _textureTiling);

            public Texture2D GetReadableTexture()
            {
                Texture source = Texture;

                if (source == null || source.width <= 0 || source.height <= 0)
                    return null;

                if (_readableTexture != null && _readableSource == source)
                    return _readableTexture;

                DisposeRuntimeTexture();

                _readableSource = source;
                _readableTexture = CreateReadableTexture(source);

                return _readableTexture;
            }

            public void DisposeRuntimeTexture()
            {
                _readableSource = null;

                if (_readableTexture == null)
                    return;

                UnityEngine.Object.Destroy(_readableTexture);
                _readableTexture = null;
            }

            private Texture GetMaterialTexture()
            {
                if (_material == null)
                    return null;

                Texture baseMap = GetMaterialTexture(BaseMapProperty);

                if (baseMap != null)
                    return baseMap;

                Texture mainTex = GetMaterialTexture(MainTexProperty);

                if (mainTex != null)
                    return mainTex;

                return _material.mainTexture;
            }

            private Texture GetMaterialTexture(string propertyName)
            {
                return _material.HasProperty(propertyName)
                    ? _material.GetTexture(propertyName)
                    : null;
            }

            private static Texture2D CreateReadableTexture(Texture source)
            {
                RenderTexture previous = RenderTexture.active;
                RenderTexture renderTexture = RenderTexture.GetTemporary(
                    source.width,
                    source.height,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Default);

                try
                {
                    Graphics.Blit(source, renderTexture);
                    RenderTexture.active = renderTexture;

                    Texture2D readableTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                    readableTexture.name = source.name + "_ReadablePaintCopy";
                    readableTexture.wrapMode = source.wrapMode;
                    readableTexture.filterMode = source.filterMode;
                    readableTexture.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
                    readableTexture.Apply(false);

                    return readableTexture;
                }
                finally
                {
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(renderTexture);
                }
            }
        }

        [Header("Raycast")]
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private float _rayDistance = 500f;
        [SerializeField] private string _groundPlaneName = "MapEditor_GeneratedPlane";

        [Header("Paint")]
        [SerializeField] private List<PaintMaterial> _paintMaterials = new List<PaintMaterial>();
        [SerializeField] private int _selectedMaterialIndex;
        [SerializeField] private int _textureResolution = 1024;
        [SerializeField, Range(0f, 1f)] private float _brushOpacity = 1f;
        [SerializeField, Range(0f, 1f)] private float _brushSoftness = 0.55f;
        [SerializeField] private Color _emptyGroundColor = Color.gray;

        [Header("Brush Size")]
        [SerializeField] private Slider _brushSizeSlider;
        [SerializeField] private float _minBrushRadius = 0.5f;
        [SerializeField] private float _maxBrushRadius = 5f;
        [SerializeField] private float _defaultBrushRadius = 2f;

        [Header("Input")]
        [SerializeField] private CarControlButton _paintButton;
        [SerializeField] private bool _allowMousePaint = true;

        [Header("Preview")]
        [SerializeField] private bool _showBrushPreview = true;
        [SerializeField] private Color _brushPreviewColor = new Color(0.3f, 0.85f, 1f, 0.22f);
        [SerializeField] private float _previewLift = 0.03f;

        public Texture2D PaintTexture => _paintTexture;
        public int SelectedMaterialIndex => _selectedMaterialIndex;

        private Texture2D _paintTexture;
        private Renderer _groundRenderer;
        private Material _runtimeGroundMaterial;
        private GameObject _brushPreview;
        private Material _brushPreviewMaterial;
        private float _brushRadius;

        private IDisposable _disposable;

        [Inject] private ISubscriber<SelectMaterialPaintEvent> _selectMaterialPaintEvent;

        public List<PaintMaterial> GetMaterialCollection => _paintMaterials;

        private void Awake()
        {
            _camera = _camera != null ? _camera : Camera.main;
            _brushRadius = _defaultBrushRadius;

            SetupBrushSlider();
            CreateBrushPreview();
        }

        private void Start()
        {
            var bag = DisposableBag.CreateBuilder();
            _selectMaterialPaintEvent.Subscribe(SelectedMaterialEvent).AddTo(bag);
            _disposable = bag.Build();
        }

        private void SelectedMaterialEvent(SelectMaterialPaintEvent evt) => 
            SetSelectedMaterialIndex(evt.MaterialIndex);

        private void OnDestroy()
        {
            _disposable.Dispose();

            if (_brushPreview != null)
                Destroy(_brushPreview);

            if (_brushPreviewMaterial != null)
                Destroy(_brushPreviewMaterial);

            if (_paintMaterials == null)
                return;

            foreach (PaintMaterial paintMaterial in _paintMaterials)
                paintMaterial?.DisposeRuntimeTexture();
        }

        private void Update()
        {
            UpdateBrushRadius();

            if (!TryRaycastGround(out RaycastHit hit))
            {
                SetPreviewVisible(false);
                return;
            }

            UpdateBrushPreview(hit);

            if (IsPaintPressed())
                Paint(hit);
        }

        public void SetSelectedMaterialIndex(int index)
        {
            if (_paintMaterials == null || _paintMaterials.Count == 0)
                return;

            _selectedMaterialIndex = Mathf.Clamp(index, 0, _paintMaterials.Count - 1);
        }

        public void SetBrushRadius(float radius)
        {
            _brushRadius = Mathf.Clamp(radius, _minBrushRadius, _maxBrushRadius);

            if (_brushSizeSlider != null)
                _brushSizeSlider.SetValueWithoutNotify(_brushRadius);
        }

        private bool TryRaycastGround(out RaycastHit hit)
        {
            hit = new RaycastHit();

            if (_camera == null)
                return false;

            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (!Physics.Raycast(ray, out hit, _rayDistance, _groundMask, QueryTriggerInteraction.Ignore))
                return false;

            GameObject groundPlane = MapEditorPlaneGenerator.CurrentPlane;

            if (groundPlane == null)
                groundPlane = GameObject.Find(_groundPlaneName);

            return groundPlane != null && hit.collider != null && hit.collider.gameObject == groundPlane;
        }

        private void Paint(RaycastHit hit)
        {
            if (!TryPrepareGround(hit.collider, out Bounds bounds))
                return;

            if (_paintMaterials == null || _paintMaterials.Count == 0)
                return;

            int materialIndex = Mathf.Clamp(_selectedMaterialIndex, 0, _paintMaterials.Count - 1);
            PaintMaterial paintMaterial = _paintMaterials[materialIndex];

            Vector2 uv = hit.textureCoord;
            int centerX = Mathf.RoundToInt(uv.x * (_paintTexture.width - 1));
            int centerY = Mathf.RoundToInt(uv.y * (_paintTexture.height - 1));
            int radiusX = Mathf.Max(1, Mathf.CeilToInt(_brushRadius / bounds.size.x * _paintTexture.width));
            int radiusY = Mathf.Max(1, Mathf.CeilToInt(_brushRadius / bounds.size.z * _paintTexture.height));

            int minX = Mathf.Max(0, centerX - radiusX);
            int maxX = Mathf.Min(_paintTexture.width - 1, centerX + radiusX);
            int minY = Mathf.Max(0, centerY - radiusY);
            int maxY = Mathf.Min(_paintTexture.height - 1, centerY + radiusY);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float normalizedX = (x - centerX) / (float)radiusX;
                    float normalizedY = (y - centerY) / (float)radiusY;
                    float distance = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);

                    if (distance > 1f)
                        continue;

                    Color currentColor = _paintTexture.GetPixel(x, y);
                    Color targetColor = SamplePaintColor(paintMaterial, x, y);
                    float falloff = Mathf.Lerp(1f, 1f - distance, _brushSoftness);
                    float strength = Mathf.Clamp01(_brushOpacity * falloff);

                    _paintTexture.SetPixel(x, y, Color.Lerp(currentColor, targetColor, strength));
                }
            }

            _paintTexture.Apply(false);
        }

        private bool TryPrepareGround(Collider groundCollider, out Bounds bounds)
        {
            bounds = new Bounds();

            if (groundCollider == null)
                return false;

            Renderer renderer = groundCollider.GetComponent<Renderer>();

            if (renderer == null)
                return false;

            bounds = renderer.bounds;

            if (_groundRenderer == renderer && _paintTexture != null)
                return true;

            _groundRenderer = renderer;
            _runtimeGroundMaterial = renderer.material;
            _paintTexture = CreatePaintTexture(_runtimeGroundMaterial);
            _runtimeGroundMaterial.mainTexture = _paintTexture;

            if (_runtimeGroundMaterial.HasProperty("_BaseColor"))
                _runtimeGroundMaterial.SetColor("_BaseColor", Color.white);

            return true;
        }

        private Texture2D CreatePaintTexture(Material material)
        {
            int resolution = Mathf.Max(32, _textureResolution);
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color fillColor = GetBaseGroundColor(material);
            Color[] pixels = new Color[resolution * resolution];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = fillColor;

            texture.SetPixels(pixels);
            texture.Apply(false);

            return texture;
        }

        private Color GetBaseGroundColor(Material material)
        {
            if (material != null && material.HasProperty("_BaseColor"))
                return material.GetColor("_BaseColor");

            return _emptyGroundColor;
        }

        private Color SamplePaintColor(PaintMaterial paintMaterial, int x, int y)
        {
            Texture2D texture = paintMaterial.GetReadableTexture();

            if (texture == null)
                return paintMaterial.FallbackColor;

            float u = Mathf.Repeat(x / (float)_paintTexture.width * paintMaterial.TextureTiling, 1f);
            float v = Mathf.Repeat(y / (float)_paintTexture.height * paintMaterial.TextureTiling, 1f);

            return texture.GetPixelBilinear(u, v);
        }

        private void SetupBrushSlider()
        {
            if (_brushSizeSlider == null)
                return;

            _brushSizeSlider.minValue = _minBrushRadius;
            _brushSizeSlider.maxValue = _maxBrushRadius;
            _brushSizeSlider.SetValueWithoutNotify(_brushRadius);
        }

        private void UpdateBrushRadius()
        {
            if (_brushSizeSlider != null)
                _brushRadius = Mathf.Clamp(_brushSizeSlider.value, _minBrushRadius, _maxBrushRadius);
        }

        private bool IsPaintPressed()
        {
            return (_paintButton != null && _paintButton.Pressed)
                || (_allowMousePaint && Input.GetMouseButton(0));
        }

        private void CreateBrushPreview()
        {
            if (!_showBrushPreview)
                return;

            _brushPreview = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _brushPreview.name = "MapEditor_BrushPreview";
            _brushPreview.transform.SetParent(transform, false);
            _brushPreview.SetActive(false);

            Collider previewCollider = _brushPreview.GetComponent<Collider>();

            if (previewCollider != null)
                Destroy(previewCollider);

            Renderer renderer = _brushPreview.GetComponent<Renderer>();

            if (renderer == null)
                return;

            _brushPreviewMaterial = CreatePreviewMaterial();
            renderer.sharedMaterial = _brushPreviewMaterial;
        }

        private Material CreatePreviewMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material material = new Material(shader);
            material.color = _brushPreviewColor;
            material.renderQueue = 3000;

            SetTransparentMaterial(material);

            return material;
        }

        private void SetTransparentMaterial(Material material)
        {
            if (material == null)
                return;

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);

            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);

            if (material.HasProperty("_AlphaClip"))
                material.SetFloat("_AlphaClip", 0f);

            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
        }

        private void UpdateBrushPreview(RaycastHit hit)
        {
            if (_brushPreview == null)
                return;

            SetPreviewVisible(true);
            _brushPreview.transform.position = hit.point + hit.normal * _previewLift;
            _brushPreview.transform.localScale = Vector3.one * (_brushRadius * 2f);
        }

        private void SetPreviewVisible(bool visible)
        {
            if (_brushPreview != null && _brushPreview.activeSelf != visible)
                _brushPreview.SetActive(visible);
        }
    }
}

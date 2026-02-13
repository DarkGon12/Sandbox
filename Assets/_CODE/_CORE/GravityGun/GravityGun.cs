using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GravityGun : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private Button _startFire;
    [SerializeField] private GameObject _buttonPanels;

    [Header("Rotate Buttons")]
    [SerializeField] private Button _rotateLeftButton;
    [SerializeField] private Button _rotateRightButton;

    [Header("Distance Buttons")]
    [SerializeField] private Button _distanceUpButton;
    [SerializeField] private Button _distanceDownButton;

    [Header("Settings")]
    [SerializeField] private float _maxDistance = 25f;
    [SerializeField] private float _minDistance = 2f;
    [SerializeField] private float _maxMass = 40f;

    [Header("Spring")]
    [SerializeField] private float _spring = 700f;
    [SerializeField] private float _damper = 90f;

    [Header("Control")]
    [SerializeField] private float _rotateSpeed = 120f;
    [SerializeField] private float _distanceSpeed = 6f;

    [Header("Beam")]
    [SerializeField] private int _beamPoints = 18;

    private bool _isFire;
    private bool _rotateLeft;
    private bool _rotateRight;
    private bool _distanceUp;
    private bool _distanceDown;

    private CreatureRagdollHelper _currentCreature;

    private OutlinePlugin.Outline _outlinePickObject;
    private Rigidbody _heldObject;
    private Rigidbody _handleBody;
    private SpringJoint _joint;

    private float _holdDistance;
    private Vector3 _beamEndPoint;

    private void Awake()
    {
        SetupHoldButton(_rotateLeftButton, () => _rotateLeft = true, () => _rotateLeft = false);
        SetupHoldButton(_rotateRightButton, () => _rotateRight = true, () => _rotateRight = false);
        SetupHoldButton(_distanceUpButton, () => _distanceUp = true, () => _distanceUp = false);
        SetupHoldButton(_distanceDownButton, () => _distanceDown = true, () => _distanceDown = false);

        _lineRenderer.positionCount = _beamPoints;
    }

    private void OnEnable()
    {
        _startFire.onClick.AddListener(ToggleFire);
    }

    private void OnDisable()
    {
        _isFire = false;
        _startFire.onClick.RemoveListener(ToggleFire);
    }

    private void Update()
    {
        HandleRaycast();
        UpdateBeam();
        HandleControls();
    }

    private void FixedUpdate()
    {
        if (_handleBody != null)
            _handleBody.MovePosition(_firePoint.position + _firePoint.forward * _holdDistance);
    }

    private void ToggleFire()
    {
        _isFire = !_isFire;
        _lineRenderer.gameObject.SetActive(_isFire);

        if (!_isFire)
            ReleaseObject();
    }

    private void HandleRaycast()
    {
        Ray ray = new Ray(_firePoint.position, _firePoint.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, _maxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            _beamEndPoint = hit.point;

            if (_isFire && _heldObject == null)
            {
                Rigidbody rb = hit.collider.attachedRigidbody;
                if (rb == null) return;

                CreatureRagdollHelper creature =
                    hit.collider.GetComponentInParent<CreatureRagdollHelper>();

                if (creature != null && creature.CompareTag("Creature"))
                {
                    _currentCreature = creature;

                    if (!_currentCreature.gameObject.GetComponent<CreatureRagdollHelper>().enabled)
                        return;

                    _currentCreature.EnableRagdoll();
                }

                if (rb.mass <= _maxMass)
                {
                    PickupObject(rb, hit.point);
                    _buttonPanels.SetActive(true);
                    OutlineEnable(hit.collider.gameObject);
                }
            }
        }
        else
        {
            _beamEndPoint = _firePoint.position + _firePoint.forward * _maxDistance;
        }
    }

    private void OutlineEnable(GameObject obj)
    {
        GameObject root;

        if (obj.transform.GetComponent<GravityItem>() != null)
        {
            root = obj.gameObject;

            if (root.GetComponent<OutlinePlugin.Outline>() == null)
            {
                _outlinePickObject = root.gameObject.AddComponent<OutlinePlugin.Outline>();
                _outlinePickObject.OutlineColor = new Color(0, 255, 253, 255);
                _outlinePickObject.OutlineWidth = 10f;
            }
            else
            {
                _outlinePickObject = root.GetComponent<OutlinePlugin.Outline>();
                _outlinePickObject.enabled = true;
            }
        }
        else
        {
            if (obj.transform.parent != null)
                root = obj.transform.root.gameObject;
            else
                root = obj.gameObject;

            if (root.GetComponent<OutlinePlugin.Outline>() == null)
            {
                _outlinePickObject = root.gameObject.AddComponent<OutlinePlugin.Outline>();
                _outlinePickObject.OutlineColor = new Color(0, 255, 253, 255);
                _outlinePickObject.OutlineWidth = 10f;
            }
            else
            {
                _outlinePickObject = root.GetComponent<OutlinePlugin.Outline>();
                _outlinePickObject.enabled = true;
            }
        }
    }

    private void PickupObject(Rigidbody rb, Vector3 hitPoint)
    {
        _heldObject = rb;
        _heldObject.interpolation = RigidbodyInterpolation.Interpolate;
        _heldObject.collisionDetectionMode = CollisionDetectionMode.Continuous;

        _holdDistance = Vector3.Distance(_firePoint.position, hitPoint);

        GameObject handle = new GameObject("PhysicsHandle");

        _handleBody = handle.AddComponent<Rigidbody>();
        _handleBody.isKinematic = true;

        _joint = handle.AddComponent<SpringJoint>();
        _joint.autoConfigureConnectedAnchor = false;
        _joint.connectedBody = _heldObject;
        _joint.spring = _spring;
        _joint.damper = _damper;
        _joint.maxDistance = 0f;
    }

    private void ReleaseObject()
    {
        if (_joint != null)
            Destroy(_joint);

        if (_handleBody != null)
            Destroy(_handleBody.gameObject);

        if (_currentCreature != null)
        {
            _currentCreature.DisableRagdoll();
            _currentCreature = null;
        }

        _heldObject = null;
        _joint = null;
        _handleBody = null;
        _buttonPanels.SetActive(false);

        if (_outlinePickObject != null)
            _outlinePickObject.enabled = false;
    }

    private void HandleControls()
    {
        if (_heldObject == null) return;

        if (_rotateLeft)
            _heldObject.transform.Rotate(Vector3.up * -_rotateSpeed * Time.deltaTime, Space.World);

        if (_rotateRight)
            _heldObject.transform.Rotate(Vector3.up * _rotateSpeed * Time.deltaTime, Space.World);

        if (_distanceUp)
            _holdDistance += _distanceSpeed * Time.deltaTime;

        if (_distanceDown)
            _holdDistance -= _distanceSpeed * Time.deltaTime;

        _holdDistance = Mathf.Clamp(_holdDistance, _minDistance, _maxDistance);
    }

    private void UpdateBeam()
    {
        Vector3 start = _firePoint.position;
        Vector3 end = _heldObject != null ? _heldObject.position : _beamEndPoint;

        Vector3 forward = _firePoint.forward;
        Vector3 toObject = (end - start);

        float distance = toObject.magnitude;
        Vector3 toObjectDir = toObject.normalized;

        float alignment = Vector3.Dot(forward, toObjectDir);

        float curveAmount = 1f - alignment;

        Vector3 bendDirection = Vector3.Cross(Vector3.Cross(forward, toObjectDir), forward).normalized;

        Vector3 midPoint = start + forward * (distance * 0.5f);

        Vector3 controlPoint = midPoint + bendDirection * curveAmount * distance * 0.4f;

        for (int i = 0; i < _beamPoints; i++)
        {
            float t = i / (float)(_beamPoints - 1);

            Vector3 point =
                Mathf.Pow(1 - t, 2) * start +
                2 * (1 - t) * t * controlPoint +
                Mathf.Pow(t, 2) * end;

            _lineRenderer.SetPosition(i, point);
        }
    }

    private void SetupHoldButton(Button button, System.Action onDown, System.Action onUp)
    {
        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener((_) => onDown());
        trigger.triggers.Add(down);

        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener((_) => onUp());
        trigger.triggers.Add(up);
    }
}

using Creatures.Navigation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Creatures.Control
{
    [RequireComponent(typeof(Rigidbody))]
    public class CreaturesController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;

        [Header("Camera")]
        [SerializeField] private Transform _camPivot;
        [SerializeField] private float sensitivity = 0.15f;

        [Header("Combat")]
        [SerializeField] private int _damage = 15;
        [SerializeField] private float _attackDistance = 1.5f;
        [SerializeField] private float _attackRadius = 0.5f;
        [SerializeField] private LayerMask _creatureLayer;
        [SerializeField] private float _attackCooldown = 0.8f;

        private float _nextAttackTime;

        private Rigidbody rb;
        private Animator anim;
        private CreatureInformation info;
        private CreatureControllerHandle _owner;

        private FloatingJoystick _joy;
        private CarControlButton _attackButton;
        private Button _exitButton;
        private Camera _mainCam;

        private float xRotation;
        private int lookFingerId = -1;
        private Vector2 lastPos;

        private bool _active;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            anim = GetComponentInChildren<Animator>();
            info = GetComponent<CreatureInformation>();
            enabled = false;
            _mainCam = Camera.main;
        }

        public void EnableControl(CreatureControllerHandle owner)
        {
            _owner = owner;
            _joy = owner.Joystick;
            _attackButton = owner.AttackButton;
            _exitButton = owner.ExitButton;

            _exitButton.onClick.RemoveAllListeners();
            _exitButton.onClick.AddListener(() => _owner.ReleaseControl(info));

            _mainCam.transform.SetParent(_camPivot);
            _mainCam.transform.localPosition = Vector3.zero;
            _mainCam.transform.rotation = _camPivot.rotation;
            _mainCam.transform.GetChild(0).gameObject.SetActive(false);

            _active = true;
            enabled = true;
        }

        private void OnDisable()
        {
            _active = false;
            lookFingerId = -1;
        }

        private void Update()
        {
            if (!_active)
                return;

            if (_attackButton.Pressed && Time.time >= _nextAttackTime)
            {
                Attack();
                _nextAttackTime = Time.time + _attackCooldown;
            }

            HandleMovement();
            HandleCamera();

            if (info.Health <= 0)
                OnDeath();
        }

        // ================= MOVEMENT =================
        private void HandleMovement()
        {
            Vector3 dir = new Vector3(_joy.Direction.x, 0, _joy.Direction.y);

            bool walking = dir.sqrMagnitude > 0.01f;
            anim.SetBool("walk", walking);
            anim.SetBool("idle", !walking);

            if (!walking)
                return;

            Vector3 move = transform.forward * dir.z + transform.right * dir.x;
            rb.MovePosition(rb.position + move * _moveSpeed * Time.deltaTime);
        }

        // ================= CAMERA =================
        private void HandleCamera()
        {
            foreach (var touch in Input.touches)
            {
                if (touch.position.x < Screen.width * 0.5f)
                    continue;

                ProcessTouch(touch);
            }
        }

        private void ProcessTouch(Touch touch)
        {
            if (touch.phase == TouchPhase.Began)
            {
                lookFingerId = touch.fingerId;
                lastPos = touch.position;
            }

            if (touch.fingerId != lookFingerId)
                return;

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.position - lastPos;
                lastPos = touch.position;
                ApplyLook(delta);
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                lookFingerId = -1;
            }
        }

        private void ApplyLook(Vector2 delta)
        {
            float yaw = delta.x * sensitivity;
            float pitch = delta.y * sensitivity;

            xRotation -= pitch;
            xRotation = Mathf.Clamp(xRotation, -45f, 45f);
            _camPivot.localRotation = Quaternion.Euler(xRotation, 0, 0);

            transform.Rotate(Vector3.up * yaw);
        }

        // ================= COMBAT =================
        private void Attack()
        {
            anim.SetTrigger("attack");

            Vector3 center = transform.position + transform.forward * _attackDistance;

            Collider[] hits = Physics.OverlapSphere(
                center,
                _attackRadius,
                ~0,
                QueryTriggerInteraction.Ignore
            );
            HashSet<CreatureInformation> damagedTargets = new HashSet<CreatureInformation>();

            foreach (var hit in hits)
            {
                var target = hit.transform.root.GetComponent<CreatureInformation>();

                if (target == null || target == info)
                    continue;

                if (damagedTargets.Add(target))
                {
                    target.TakeDamage(_damage, info);
                    target.GetComponent<CreatureEnemySensor>().SetEnemy(info);
                }
            }
        }

        public void OnDeath()
        {
            _owner.ReleaseControl(info);
            enabled = false;
        }

        private void OnDestroy()
        {
            if (enabled == true)
                _owner.ReleaseControl(info);
        }
    }
}
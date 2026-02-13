using UnityEngine.EventSystems;
using UnityEngine;

namespace Sandbox.Player
{
    public class TouchLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField] private float sensitivity = 0.15f;
        [SerializeField] private Transform player;
        [SerializeField] private Transform cam;
        [SerializeField] private WeaponHolderMotion _weaponMotion;

        private float xRotation;
        private Vector2 lastPos;
        private Rigidbody rb;

        void Awake()
        {
            rb = player.GetComponent<Rigidbody>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            lastPos = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - lastPos;
            lastPos = eventData.position;

            float yaw = delta.x * sensitivity;
            float pitch = delta.y * sensitivity;

            xRotation -= pitch;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            Quaternion deltaRotation = Quaternion.Euler(0f, yaw, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);

            _weaponMotion.ApplyLookSway(delta);
        }
    }
}
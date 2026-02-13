using UnityEngine;

namespace Creatures.UI
{
    public class CreaturesCommandWindow : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private RectTransform _commandPanel;

        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.CompareTag("Creature"))
                    OpenPanel();
                else
                    ClosePanel();
            }
        }

        public void OpenPanel()
        {
            _commandPanel.gameObject.SetActive(true);
        }

        public void ClosePanel()
        {
            _commandPanel.gameObject.SetActive(false);
        }
    }
}
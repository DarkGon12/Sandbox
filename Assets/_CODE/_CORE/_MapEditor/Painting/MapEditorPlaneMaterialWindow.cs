using MessagePipe;
using UnityEngine;
using Zenject;

namespace Sandbox.MapEditor
{
    public class MapEditorPlaneMaterialWindow : MonoBehaviour
    {
        [SerializeField] private Transform _cellContent;
        [SerializeField] private GameObject _cellPrefab;

        private MapEditorPlanePainter _planePainter;

        [Inject] private DiContainer _container;

        private void Awake()
        {
            _planePainter = GetComponent<MapEditorPlanePainter>();
        }

        private void Start()
        {
            InitializeMaterialCells();
        }

        private void InitializeMaterialCells()
        {
            for (int i = 0; i < _planePainter.GetMaterialCollection.Count; i++)
            {
                GameObject cell = _container.InstantiatePrefab(_cellPrefab, _cellContent);
                cell.GetComponent<MapEditorPlaneMaterialCell>().InitializeCell(i, _planePainter.GetMaterialCollection[i].Avatar);
            }
        }
    }
}
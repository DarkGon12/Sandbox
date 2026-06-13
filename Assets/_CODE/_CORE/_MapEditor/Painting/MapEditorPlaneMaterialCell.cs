using UnityEngine.UI;
using MessagePipe;
using UnityEngine;
using Zenject;

public class MapEditorPlaneMaterialCell : MonoBehaviour
{
    [SerializeField] private Image _materialAvatar;
    [SerializeField] private Button _materialBtn;

    [Inject] private IPublisher<SelectMaterialPaintEvent> _selectMaterialEventPub;

    private int _materialIndex;

    public void InitializeCell(int materialIndex, Sprite avatar)
    {
        _materialIndex = materialIndex;
        _materialAvatar.sprite = avatar;
        _materialBtn.onClick.AddListener(() => SelectMaterial());
    }

    private void SelectMaterial()
    {
        _selectMaterialEventPub.Publish(new() { MaterialIndex = _materialIndex });
    }
}
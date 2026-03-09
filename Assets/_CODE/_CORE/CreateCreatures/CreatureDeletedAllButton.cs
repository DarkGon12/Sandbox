using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class CreatureDeletedAllButton : MonoBehaviour
{
    [SerializeField] private Button _creatureRemoveAllButton;

    [Inject] private IPublisher<CreatureRemoveAll> _creatureRemoveAllEventPub;

    private void Start()
    {
        _creatureRemoveAllButton.onClick.AddListener(Deleted);
    }

    private void Deleted()
    {
        _creatureRemoveAllEventPub.Publish(new());
    }
}
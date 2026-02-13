using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Creatures
{
    public class CreaturesTransformButton : MonoBehaviour
    {
        [SerializeField] private Button _leftBtn;
        [SerializeField] private Button _rightBtn;
        [SerializeField] private Button _dropBtn;
        [SerializeField] private Button _sumScaleBtn;
        [SerializeField] private Button _minuScaleBtn;

        [Inject] private IPublisher<CreatureTransformCommandEvent> _creatureTransformEventPub;

        private void Start()
        {
            _leftBtn.onClick.AddListener(LeftRotate);
            _rightBtn.onClick.AddListener(RightRotate);
            _dropBtn.onClick.AddListener(DropTransform);
            _sumScaleBtn.onClick.AddListener(SumScale);
            _minuScaleBtn.onClick.AddListener(MinuScale);
        }

        private void SumScale()
        {
            _creatureTransformEventPub.Publish(new() { CommandIndex = 3 });
        }

        private void MinuScale()
        {
            _creatureTransformEventPub.Publish(new() { CommandIndex = 4 });
        }

        private void RightRotate()
        {
            _creatureTransformEventPub.Publish(new() { CommandIndex = 1 });
        }

        private void LeftRotate()
        {
            _creatureTransformEventPub.Publish(new() { CommandIndex = 2 });
        }

        private void DropTransform()
        {
            _creatureTransformEventPub.Publish(new() { CommandIndex = 0 });
        }
    }
}
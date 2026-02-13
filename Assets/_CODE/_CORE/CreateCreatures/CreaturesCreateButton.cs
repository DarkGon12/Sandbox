using UnityEngine.UI;
using UnityEngine;
using MessagePipe;
using Zenject;
using TMPro;
using UnityEngine.SceneManagement;

namespace Creatures.UI
{
    public class CreaturesCreateButton : MonoBehaviour
    {
        [SerializeField] private Image _creatureIcon;
        [SerializeField] private TextMeshProUGUI _creaturePrice;

        [Inject] private IPublisher<CreateCreatureEvent> _createCreatureEventPub;
        [Inject] private IPublisher<SelectedWeaponEvent> _selectedWeaponEventPub;

        [Inject] private IPublisher<LoadSceneEvent> _loadSceneEventPub;
        [Inject] private IPublisher<ShowInterstitialAdEvent> _showInterAdEventPub;

        private int _weaponIndex;
        private string _sceneName;

        private CreaturesData _creatureData;
        private Button _createButton;

        public void SetIcon(Sprite icon) => _creatureIcon.sprite = icon;
        public void SetPrice(string price) => _creaturePrice.text = price;
        public void SetData(CreaturesData data) => _creatureData = data;

        public void Start()
        {
            _createButton = GetComponent<Button>();
            _createButton.onClick.AddListener(CreateCreature);
        }

        public void InitWeaponCard(int weaponIndex)
        {
            _weaponIndex = weaponIndex;

            if (_createButton == null) 
                _createButton = GetComponent<Button>();

            _createButton.onClick.RemoveListener(CreateCreature);
            _createButton.onClick.AddListener(SelectWeapon);
        }

        public void InitMapCard(string mapIndex)
        {
            _sceneName = mapIndex;

            if (_createButton == null)
                _createButton = GetComponent<Button>();

            _createButton.onClick.RemoveListener(CreateCreature);
            _createButton.onClick.AddListener(SelectMap);
        }

        private void CreateCreature()
        {
            _createCreatureEventPub.Publish(new() { CreaturesData = _creatureData });
            _showInterAdEventPub.Publish(new() { });
        }

        private void SelectWeapon()
        {
            _selectedWeaponEventPub.Publish(new() { WeaponIndex = _weaponIndex });
            _showInterAdEventPub.Publish(new() { });
        }

        private void SelectMap()
        {
            _loadSceneEventPub.Publish(new() { SceneName = _sceneName });
        }
    }
}
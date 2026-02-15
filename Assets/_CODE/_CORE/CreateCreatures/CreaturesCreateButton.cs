using UnityEngine.UI;
using UnityEngine;
using MessagePipe;
using Zenject;
using TMPro;
using UnityEngine.SceneManagement;
using Sandbox.Analyse;

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

        [Inject] private IPublisher<CreatureWindowView> _showWindowEventPub;

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
            _showWindowEventPub.Publish(new() { IsShow = false });

            FirebaseSender.LogEventCreature("User select card", 
                _creatureData.GetUIInfo.CreatureName, _creatureData.GetCreatureType);
        }

        private void SelectWeapon()
        {
            _selectedWeaponEventPub.Publish(new() { WeaponIndex = _weaponIndex });
            _showInterAdEventPub.Publish(new() { });
            _showWindowEventPub.Publish(new() { IsShow = false });

            FirebaseSender.LogEventCreature("User select weapon",
    _creatureData.GetUIInfo.CreatureName, _creatureData.GetCreatureType);
        }

        private void SelectMap()
        {
            _loadSceneEventPub.Publish(new() { SceneName = _sceneName });

            FirebaseSender.LogEventCreature("User select map",
    _creatureData.GetUIInfo.CreatureName, _creatureData.GetCreatureType);
        }
    }
}
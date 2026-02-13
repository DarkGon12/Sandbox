using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using Zenject;

namespace Creatures.UI
{
    public class CreaturesView : MonoBehaviour
    {
        [SerializeField] private GameObject _prefabCreatureCard;

        [SerializeField] private Transform _contentCreatures;
        [SerializeField] private Transform _contentEntity;
        [SerializeField] private Transform _contentWeapon;
        [SerializeField] private Transform _contentCar;
        [SerializeField] private Transform _contentMap;

        [Inject] private CreaturesRepository _creaturesRepository;
        [Inject] private DiContainer _container;

        private void Start()
        {
            InstanceUiCreature();
        }

        public void InstanceUiCreature()
        {
            List<CreaturesData> data = _creaturesRepository.CreaturesCollection;

            for (int i = 0; i < data.Count; i++)
            {
                GameObject creatureUI = null;

                if (data[i].GetCreatureType == CreaturesType.Creatures)
                    creatureUI = _container.InstantiatePrefab(_prefabCreatureCard, _contentCreatures);
                if (data[i].GetCreatureType == CreaturesType.Entity)
                    creatureUI = _container.InstantiatePrefab(_prefabCreatureCard, _contentEntity);
                if (data[i].GetCreatureType == CreaturesType.Car)
                    creatureUI = _container.InstantiatePrefab(_prefabCreatureCard, _contentCar);
                if (data[i].GetCreatureType == CreaturesType.Map)
                {
                    creatureUI = _container.InstantiatePrefab(_prefabCreatureCard, _contentMap);
                    InitMapCard(creatureUI, data[i]);
                    continue;
                }
                if (data[i].GetCreatureType == CreaturesType.Weapon)
                {
                    creatureUI = _container.InstantiatePrefab(_prefabCreatureCard, _contentWeapon);
                    InitWeaponCard(creatureUI, data[i]);
                    continue;
                }

                if (creatureUI == null)
                {
                    Debug.LogError("Not instantiate creatureUI ");
                    return;
                }

                CreaturesCreateButton button = creatureUI.GetComponent<CreaturesCreateButton>();

                if (data[i].GetUIInfo.CreatureIcon != null)
                    button.SetIcon(data[i].GetUIInfo.CreatureIcon);

                if (data[i].GetUIInfo.CreaturePrice != null)
                    button.SetPrice(data[i].GetUIInfo.CreaturePrice);

                button.SetData(data[i]);
                _container.Inject(creatureUI);
            }
        }

        private void InitWeaponCard(GameObject card, CreaturesData data)
        {
            CreaturesCreateButton button = card.GetComponent<CreaturesCreateButton>();

            if (data.GetUIInfo.CreatureIcon != null)
                button.SetIcon(data.GetUIInfo.CreatureIcon);

            if (data.GetUIInfo.CreaturePrice != null)
                button.SetPrice(data.GetUIInfo.CreaturePrice);

            button.SetData(data);
            _container.Inject(card);

            card.GetComponent<CreaturesCreateButton>().InitWeaponCard(data.GetWeaponIndex);
        }

        private void InitMapCard(GameObject card, CreaturesData data)
        {
            CreaturesCreateButton button = card.GetComponent<CreaturesCreateButton>();

            if (data.GetUIInfo.CreatureIcon != null)
                button.SetIcon(data.GetUIInfo.CreatureIcon);

            if (data.GetUIInfo.CreaturePrice != null)
                button.SetPrice(data.GetUIInfo.CreaturePrice);

            button.SetData(data);
            _container.Inject(card);

            card.GetComponent<CreaturesCreateButton>().InitMapCard(data.GetMapIndex);
        }
    }
}
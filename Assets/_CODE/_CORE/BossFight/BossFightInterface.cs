using System.Collections;
using System.Collections.Generic;
using Creatures.Navigation;
using UnityEngine;
using Creatures;
using Cretures;
using TMPro;
using Zenject;

public class BossFightInterface : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _countToSpawn;
    [SerializeField] private int _countFromSpawn = 300;
    [SerializeField] private CreatureState _attackPlayerState;

    [Space(10)]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private List<CreaturesData> _creatureCollection = new List<CreaturesData>();

    [Inject] private DiContainer _container;

    private int _currentTime;

    private void Start()
    {
        StartTimer();
    }

    private void StartTimer()
    {
        _currentTime = _countFromSpawn;
        StartCoroutine(TimeToSpawn());
    }

    private IEnumerator TimeToSpawn()
    {
        while (_currentTime >= 0)
        {
            yield return new WaitForSeconds(1);
            _currentTime -= 1;
            _countToSpawn.text = _currentTime.ToString("0 Sec");

            if (_currentTime == 0)
            {
                _countToSpawn.text = "0 Sec";
                SpawnBoss();
            }
        }
    }

    private void SpawnBoss()
    {
        CreaturesData data = _creatureCollection[Random.Range(0, _creatureCollection.Count)];
        GameObject boss = _container.InstantiatePrefab(data.GetObject, _spawnPoint);
        boss.transform.SetParent(null);

        boss.GetComponent<CreaturesSpawnEffects>().SetEndScale(3f);
        boss.GetComponent<CreaturesSpawnEffects>().PlaySpawnEffect();
        boss.GetComponent<CreatureInformation>().SetHealth(350);
        boss.GetComponent<CreatureInformation>().SetDamage(30);
        boss.GetComponent<CreatureStateView>().SetState(_attackPlayerState);
    }
}
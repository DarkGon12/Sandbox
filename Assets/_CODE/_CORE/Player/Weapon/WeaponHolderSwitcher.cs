using MessagePipe;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class WeaponHolderSwitcher : MonoBehaviour
{
    [SerializeField] private List<GameObject> _weaponCollection = new List<GameObject>();

    [Inject] private ISubscriber<SelectedWeaponEvent> _selectedWeaponEventSub;

    private GameObject _selectedWeapon;
    private IDisposable _disposable;

    private void Start()
    {
        var bag = DisposableBag.CreateBuilder();
        _selectedWeaponEventSub.Subscribe(SetWeapon).AddTo(bag);
        bag.Add(_disposable);
        _disposable = bag.Build();

        _selectedWeapon = _weaponCollection[0];
    }

    public void SetWeapon(SelectedWeaponEvent evt)
    {
        _selectedWeapon.SetActive(false);
        _selectedWeapon = _weaponCollection[evt.WeaponIndex];
        _selectedWeapon.SetActive(true);
    }

    public void SetWeapon(int index)
    {
        _selectedWeapon.SetActive(false);
        _selectedWeapon = _weaponCollection[index];
        _selectedWeapon.SetActive(true);
    }

    private void OnDestroy() => _disposable?.Dispose();
}
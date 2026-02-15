using MessagePipe;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class WeaponHolderSwitcher : MonoBehaviour
{
    [SerializeField] private List<GameObject> _weaponCollection = new List<GameObject>();
    [SerializeField] private CarControlButton _lastWeaponButton;

    [Inject] private ISubscriber<SelectedWeaponEvent> _selectedWeaponEventSub;

    private int _lastWeaponIndex;
    private GameObject _selectedWeapon;
    private IDisposable _disposable;

    private void Start()
    {
        _lastWeaponButton.OnDown += GetLastWeapon;
        var bag = DisposableBag.CreateBuilder();
        _selectedWeaponEventSub.Subscribe(SetWeapon).AddTo(bag);
        bag.Add(_disposable);
        _disposable = bag.Build();

        _selectedWeapon = _weaponCollection[0];
    }

    public void SetWeapon(SelectedWeaponEvent evt)
    {
        SetLastWeapon();
        if (!_lastWeaponButton.gameObject.activeSelf)
            _lastWeaponButton.gameObject.SetActive(true);

        _selectedWeapon.SetActive(false);
        _selectedWeapon = _weaponCollection[evt.WeaponIndex];
        _selectedWeapon.SetActive(true);
    }

    public void SetWeapon(int index)
    {
        SetLastWeapon();
        if (index != 0 && !_lastWeaponButton.gameObject.activeSelf)
            _lastWeaponButton.gameObject.SetActive(true);

        _selectedWeapon.SetActive(false);
        _selectedWeapon = _weaponCollection[index];
        _selectedWeapon.SetActive(true);
    }

    public void GetLastWeapon()
    {
        if (_lastWeaponIndex <= 0)
            return;

        int last = _lastWeaponIndex;
        SetLastWeapon();
        SetWeapon(last);
    }

    private int SetLastWeapon()
    {
        for (int i = 1; i < _weaponCollection.Count; i++)
        {
            if (_weaponCollection[i].activeSelf)
            {
                _lastWeaponIndex = i;
                return i;
            }
        }

        return _lastWeaponIndex;
    }


    private void OnDestroy()
    {
        _lastWeaponButton.OnDown -= GetLastWeapon;
        _disposable?.Dispose();
    }
}
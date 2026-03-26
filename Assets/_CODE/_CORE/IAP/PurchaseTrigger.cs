using MessagePipe;
using UnityEngine;
using Zenject;

public class PurchaseTrigger : MonoBehaviour
{
    [SerializeField] private CharacterPurchaseSO _characterPurchaseSo;

    [Inject] private IPublisher<SelectedWeaponEvent> _selectedWeaponEventPub;
    [Inject] private IPublisher<CreateCreatureEvent> _createCreatureEventPub;
    [Inject] private IPublisher<IAPManagerSendEvent> _iapPublisher;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Player")
            TryBuy();
    }

    public void TryBuy()
    {
        if (PlayerPrefs.GetInt("iap_owned_" + _characterPurchaseSo.ProductId) == 1)
        {
            SelectedToolgun();
            return;
        }

        if (_characterPurchaseSo == null)
            return;

        if (string.IsNullOrWhiteSpace(_characterPurchaseSo.ProductId))
            return;

        _iapPublisher.Publish(new() { ProductName = _characterPurchaseSo.ProductId });
    }

    public void SelectedToolgun()
    {
        if (_characterPurchaseSo.IsWeapon)
        {
            _selectedWeaponEventPub.Publish(new() { WeaponIndex = _characterPurchaseSo.Data.GetWeaponIndex });
            return;
        }

        _createCreatureEventPub.Publish(new() { CreaturesData = _characterPurchaseSo.Data });
    }
}
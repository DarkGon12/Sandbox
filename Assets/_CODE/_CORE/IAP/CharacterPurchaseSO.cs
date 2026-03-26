using Creatures;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterPurchaseSO", menuName = "IAP/Character Purchase")]
public sealed class CharacterPurchaseSO : ScriptableObject
{
    [SerializeField] private string _productId;
    [SerializeField] private CreaturesData _creatureData;
    [SerializeField] private bool _isWeapon = false;

    public string ProductId => _productId;
    public CreaturesData Data => _creatureData;
    public bool IsWeapon => _isWeapon;
}
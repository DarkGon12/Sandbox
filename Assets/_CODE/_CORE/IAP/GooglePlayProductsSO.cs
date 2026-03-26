using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

[CreateAssetMenu(fileName = "GooglePlayProductsSO", menuName = "IAP/Google Play Products")]
public sealed class GooglePlayProductsSO : ConfigBase
{
    [SerializeField] private List<ProductEntry> _products = new();

    public IReadOnlyList<ProductEntry> Products => _products;

    public bool TryGetById(string productId, out ProductEntry entry)
    {
        for (int i = 0; i < _products.Count; i++)
        {
            if (_products[i].ProductId == productId)
            {
                entry = _products[i];
                return true;
            }
        }

        entry = null;
        return false;
    }

    public List<ProductDefinition> BuildDefinitions()
    {
        var result = new List<ProductDefinition>(_products.Count);

        for (int i = 0; i < _products.Count; i++)
        {
            var item = _products[i];

            if (string.IsNullOrWhiteSpace(item.ProductId))
                continue;

            result.Add(new ProductDefinition(item.ProductId, item.ProductType));
        }

        return result;
    }
}

[Serializable]
public sealed class ProductEntry
{
    public string ProductId;
    public ProductType ProductType = ProductType.NonConsumable;
}
using MessagePipe;
using Sandbox.Analyse;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using Zenject;

public sealed class IAPManager : MonoBehaviour
{
    public bool IsInitialized { get; private set; }
    public bool IsConnecting { get; private set; }

    private StoreController _storeController;
    private readonly Dictionary<string, Product> _productsById = new();
    private readonly HashSet<string> _processedPendingKeys = new();

    [Inject] private GooglePlayProductsSO _productsSo;

    [Inject] private ISubscriber<IAPManagerSendEvent> _sendIapEventSub;
    [Inject] private IPublisher<IAPManagerResultEvent> _iapResultPublisher;
    private IDisposable _disposable;

    private async void Start()
    {
        var bag = DisposableBag.CreateBuilder();
        _sendIapEventSub.Subscribe(OnBuyRequested).AddTo(bag);
        _disposable = bag.Build();

        await InitializeAsync();
    }

    private void OnDestroy()
    {
        _disposable?.Dispose();
        UnsubscribeStoreEvents();
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized || IsConnecting)
            return;

        IsConnecting = true;

        try
        {
            await UnityServices.InitializeAsync();

            _storeController = UnityIAPServices.StoreController();

            SubscribeStoreEvents();

            await _storeController.Connect();

            var definitions = _productsSo.BuildDefinitions();
            _storeController.FetchProducts(definitions);
        }
        catch (Exception ex)
        {
            IsConnecting = false;
            IsInitialized = false;
            PublishResult(string.Empty, false, ex.Message);
            Debug.LogError($"IAP initialize exception: {ex}");
        }
    }

    public Product GetProduct(string productId)
    {
        _productsById.TryGetValue(productId, out var product);
        return product;
    }

    public string GetLocalizedPrice(string productId)
    {
        var product = GetProduct(productId);
        return product != null ? product.metadata.localizedPriceString : string.Empty;
    }

    private void OnBuyRequested(IAPManagerSendEvent evt)
    {
        if (evt == null || string.IsNullOrWhiteSpace(evt.ProductName))
            return;

        BuyProduct(evt.ProductName);
    }

    public void BuyProduct(string productId)
    {
        if (!IsInitialized || _storeController == null)
        {
            PublishResult(productId, false, "IAP not initialized");
            return;
        }

        var product = _storeController.GetProductById(productId);

        if (product == null)
        {
            PublishResult(productId, false, "Product not loaded");
            return;
        }

        _storeController.PurchaseProduct(product);
    }

    private void SubscribeStoreEvents()
    {
        if (_storeController == null)
            return;

        _storeController.OnProductsFetched += HandleProductsFetched;
        _storeController.OnProductsFetchFailed += HandleProductsFetchFailed;
        _storeController.OnPurchasesFetched += HandlePurchasesFetched;
        _storeController.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;
        _storeController.OnPurchasePending += HandlePurchasePending;
        _storeController.OnPurchaseFailed += HandlePurchaseFailed;
        _storeController.OnPurchaseDeferred += HandlePurchaseDeferred;
        _storeController.OnPurchaseConfirmed += HandlePurchaseConfirmed;
        _storeController.OnStoreDisconnected += HandleStoreDisconnected;
    }

    private void UnsubscribeStoreEvents()
    {
        if (_storeController == null)
            return;

        _storeController.OnProductsFetched -= HandleProductsFetched;
        _storeController.OnProductsFetchFailed -= HandleProductsFetchFailed;
        _storeController.OnPurchasesFetched -= HandlePurchasesFetched;
        _storeController.OnPurchasesFetchFailed -= HandlePurchasesFetchFailed;
        _storeController.OnPurchasePending -= HandlePurchasePending;
        _storeController.OnPurchaseFailed -= HandlePurchaseFailed;
        _storeController.OnPurchaseDeferred -= HandlePurchaseDeferred;
        _storeController.OnPurchaseConfirmed -= HandlePurchaseConfirmed;
        _storeController.OnStoreDisconnected -= HandleStoreDisconnected;
    }

    private void HandleProductsFetched(List<Product> products)
    {
        _productsById.Clear();

        for (int i = 0; i < products.Count; i++)
        {
            var product = products[i];

            if (product == null || product.definition == null)
                continue;

            _productsById[product.definition.id] = product;
        }

        IsConnecting = false;
        IsInitialized = true;

        _storeController.FetchPurchases();
    }

    private void HandleProductsFetchFailed(ProductFetchFailed failure)
    {
        IsConnecting = false;
        IsInitialized = false;

        var message = failure != null ? failure.FailureReason : "Unknown product fetch failure";
        PublishResult(string.Empty, false, message);
        Debug.LogError($"IAP products fetch failed: {message}");
    }

    private void HandlePurchasesFetched(Orders orders)
    {
        if (orders == null)
            return;

        foreach (var confirmedOrder in orders.ConfirmedOrders)
        {
            if (confirmedOrder == null)
                continue;

            RestoreConfirmedOrder(confirmedOrder);
        }
    }

    private void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        var reason = failure != null ? failure.FailureReason.ToString() : "Unknown";
        var message = failure != null ? failure.Message : string.Empty;
        Debug.LogError($"IAP purchases fetch failed: {reason} | {message}");
    }

    private void HandlePurchasePending(PendingOrder order)
    {
        if (order == null)
            return;

        var orderKey = GetOrderKey(order);

        if (!string.IsNullOrEmpty(orderKey) && _processedPendingKeys.Contains(orderKey))
            return;

        try
        {
            GrantOrder(order);

            if (!string.IsNullOrEmpty(orderKey))
                _processedPendingKeys.Add(orderKey);

            _storeController.ConfirmPurchase(order);
        }
        catch (Exception ex)
        {
            var productId = GetFirstProductId(order);
            PublishResult(productId, false, ex.Message);
            Debug.LogError($"IAP grant failed: {ex}");
        }
    }

    private void HandlePurchaseFailed(FailedOrder order)
    {
        var productId = GetFirstProductId(order);
        var reason = order != null ? order.FailureReason.ToString() : "Unknown";
        var details = order != null ? order.Details : string.Empty;

        PublishResult(productId, false, $"{reason} | {details}");
        Debug.LogError($"IAP purchase failed: {productId} | {reason} | {details}");
    }

    private void HandlePurchaseDeferred(DeferredOrder order)
    {
        var productId = GetFirstProductId(order);
        PublishResult(productId, false, "Deferred");
    }

    private void HandlePurchaseConfirmed(Order order)
    {
        var productId = GetFirstProductId(order);

        FirebaseSender.LogEventIAPManager("Player buy product", productId);
        Debug.Log($"IAP purchase confirmed: {productId}");
    }

    private void HandleStoreDisconnected(StoreConnectionFailureDescription failure)
    {
        var message = failure != null ? failure.Message : "Store disconnected";
        PublishResult(string.Empty, false, message);
        Debug.LogError($"IAP store disconnected: {message}");
    }

    private void RestoreConfirmedOrder(Order order)
    {
        var items = order.CartOrdered.Items();

        for (int i = 0; i < items.Count; i++)
        {
            var product = items[i].Product;

            if (product == null || product.definition == null)
                continue;

            var productId = product.definition.id;
            SetCharacterOwned(productId);
        }
    }

    private void GrantOrder(Order order)
    {
        var items = order.CartOrdered.Items();

        for (int i = 0; i < items.Count; i++)
        {
            var product = items[i].Product;

            if (product == null || product.definition == null)
                continue;

            var productId = product.definition.id;
            SetCharacterOwned(productId);
            PublishResult(productId, true, "Success");
        }
    }

    private void SetCharacterOwned(string productId)
    {
        Debug.Log("$_IAPManager_ productID: " + productId + " have buy");
        PlayerPrefs.SetInt(GetOwnershipKey(productId), 1);
        PlayerPrefs.Save();
    }

    public bool IsCharacterOwned(string productId)
    {
        return PlayerPrefs.GetInt(GetOwnershipKey(productId), 0) == 1;
    }

    private string GetOwnershipKey(string productId)
    {
        return $"iap_owned_{productId}";
    }

    private void PublishResult(string productId, bool success, string message)
    {
        _iapResultPublisher?.Publish(new IAPManagerResultEvent
        {
            ProductName = productId,
            Success = success,
            Message = message
        });
    }

    private string GetFirstProductId(Order order)
    {
        if (order == null)
            return string.Empty;

        var items = order.CartOrdered.Items();

        if (items == null || items.Count == 0 || items[0].Product == null || items[0].Product.definition == null)
            return string.Empty;

        return items[0].Product.definition.id;
    }

    private string GetOrderKey(Order order)
    {
        if (order == null || order.Info == null)
            return string.Empty;

        if (!string.IsNullOrEmpty(order.Info.Receipt))
            return order.Info.Receipt;

        var productId = GetFirstProductId(order);
        return string.IsNullOrEmpty(productId) ? string.Empty : $"fallback_{productId}";
    }
}
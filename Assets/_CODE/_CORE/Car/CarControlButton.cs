using UnityEngine.EventSystems;
using UnityEngine;
using System;

public class CarControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Action OnDown;
    public Action OnUp;

    public bool Pressed { get; private set; }
    public int PointerId { get; private set; } = -1;

    public void OnPointerDown(PointerEventData eventData)
    {
        Pressed = true;
        PointerId = eventData.pointerId;
        OnDown?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Pressed = false;
        PointerId = -1;
        OnUp?.Invoke();
    }
}
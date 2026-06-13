using TMPro;
using UnityEngine;

public class AmmoDefinition : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _ammoText;

    public void UpdateUI(int currentAmmo, int maxAmmo)
    {
        _ammoText.text = currentAmmo.ToString("00") + "/" + maxAmmo;
    }

    public void ShowUI(bool state) => gameObject.SetActive(state);
}
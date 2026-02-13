using UnityEngine.SceneManagement;
using UnityEngine;
using Zone.Menu;
using Zenject;

public class BootManager : MonoBehaviour
{
    [SerializeField] private LocationId _locationId;

    [Inject] private SceneInfo _sceneInfo;

    private void Awake()
    {
        SceneManager.LoadScene(_sceneInfo.GetLocationNameById(_locationId), LoadSceneMode.Single);
    }
}
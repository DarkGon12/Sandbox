using UnityEngine;
using UnityEngine.SceneManagement;
using Zone.Menu;

public class SceneInfo : MonoBehaviour
{
    public string GetLocationNameById(LocationId id)
    {
        if(SceneManager.GetSceneByName(id.ToString()) == null)
        {
            Debug.LogWarning("Scene by id not found");
            return null;
        }

        if (id == LocationId.None)
        {
            Debug.LogWarning("LocationId return None");
            return null;
        }

        return id.ToString();
    }
}
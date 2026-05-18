using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MobilePerfomanceManager : MonoBehaviour
{
    [Header("Current Limits")]
    [SerializeField] private int _maxEntities = 100;
    [SerializeField] private int _maxCreatures = 50;

    [Header("Limits Range")]
    [SerializeField] private int minEntities = 30;
    [SerializeField] private int maxEntities = 300;

    [SerializeField] private int minCreatures = 10;
    [SerializeField] private int maxCreatures = 150;

    [Header("Performance Settings")]
    [SerializeField] private float checkInterval = 2f;
    [SerializeField] private float lowFPSThreshold = 40f;
    [SerializeField] private float highFPSThreshold = 55f;

    [SerializeField] private bool _debug;

    private float timer;
    private int frameCounter;
    private float fps;

    private UniversalRenderPipelineAsset urpAsset;

    void Start()
    {
        urpAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Application.backgroundLoadingPriority = ThreadPriority.Low; // Help prevent ANR during pause
    }

    void Update()
    {
        MeasureFPS();
    }

    void MeasureFPS()
    {
        timer += Time.unscaledDeltaTime;
        frameCounter++;

        if (timer >= checkInterval)
        {
            fps = frameCounter / timer;

            AdjustPerformance(fps);

            frameCounter = 0;
            timer = 0;

            if (_debug)
                Debug.Log($"FPS: {fps} | Creatures: {_maxCreatures} | Entities: {_maxEntities}");
        }
    }

    void AdjustPerformance(float currentFPS)
    {
        if (currentFPS < lowFPSThreshold)
        {
            ReduceLoad();
        }
        else if (currentFPS > highFPSThreshold)
        {
            IncreaseLoad();
        }
    }

    void ReduceLoad()
    {
        _maxCreatures -= 5;
        _maxEntities -= 10;

        _maxCreatures = Mathf.Clamp(_maxCreatures, minCreatures, maxCreatures);
        _maxEntities = Mathf.Clamp(_maxEntities, minEntities, maxEntities);

        SetShadows(0);
    }    

    public void SetShadows(float distance)
    {
        urpAsset.shadowDistance = distance;
    }

    void IncreaseLoad()
    {
        _maxCreatures += 5;
        _maxEntities += 10;

        _maxCreatures = Mathf.Clamp(_maxCreatures, minCreatures, maxCreatures);
        _maxEntities = Mathf.Clamp(_maxEntities, minEntities, maxEntities);

        SetShadows(50);
    }

    public float GetFPS()
    {
        return fps;
    }

    public int GetMaxCreature() => _maxCreatures;
    public int GetMaxEntyties() => _maxEntities;
}
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Zenject;

[CreateAssetMenu(fileName = "ConfigRepository", menuName = "Config/Repository")]
public class ConfigRepository : ScriptableObjectInstaller<ConfigRepository>
{
    public List<ConfigBase> configs;

    public override void InstallBindings()
    {
        foreach (var config in configs)
        {
            Container.Bind(config.GetType()).FromInstance(config).AsSingle().NonLazy();
        }
    }

    public T GetConfig<T>() where T : ConfigBase
    {
        var config = configs.OfType<T>().FirstOrDefault();

        if (config == null)
        {
            Debug.LogWarning($"Config of type {typeof(T).Name} not found.");
        }

        return config;
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using Zenject;
using MessagePipe;
using System;

namespace Sandbox
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private Slider progressBar;
        [SerializeField] private string gameSceneName;

        [Inject] private ISubscriber<LoadSceneEvent> _loadSceneEventSub;

        private IDisposable _disposable;

        private void Awake()
        {
            var bag = DisposableBag.CreateBuilder();
            _loadSceneEventSub.Subscribe(LoadSceneEvent).AddTo(bag);
            bag.Add(_disposable);
            _disposable = bag.Build();
        }

        private void Start()
        {
            GameObject.DontDestroyOnLoad(this);
            Load();
        }

        private void LoadSceneEvent(LoadSceneEvent evt)
        {
            gameSceneName = evt.SceneName;
            Load();
        }

        public void Load()
        {
            SceneManager.LoadScene("LoadingScene");
            StartCoroutine(LoadSceneAsync());
        }

        private IEnumerator LoadSceneAsync()
        {
            yield return null;

            if (progressBar == null)
            {
                progressBar = FindFirstObjectByType<Slider>();

                if (progressBar == null)
                {
                    Debug.LogError("SceneLoader: Slider не найден на LoadingScene");
                    yield break;
                }

                progressBar.value = 0f;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(gameSceneName);
            operation.allowSceneActivation = false;

            float fakeProgress = 0f;

            while (operation.progress < 0.9f)
            {
                fakeProgress = Mathf.MoveTowards(fakeProgress, operation.progress, Time.deltaTime);
                progressBar.value = fakeProgress;
                yield return null;
            }

            while (fakeProgress < 1f)
            {
                fakeProgress = Mathf.MoveTowards(fakeProgress, 1f, Time.deltaTime);
                progressBar.value = fakeProgress;
                yield return null;
            }

            operation.allowSceneActivation = true;
        }

        private void OnDestroy() => _disposable?.Dispose();
    }
}
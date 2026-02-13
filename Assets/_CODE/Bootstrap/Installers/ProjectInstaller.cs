using GoogleMobileAds.Sample;
using MessagePipe;
using UnityEngine;
using Zenject;

namespace Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        private MessagePipeOptions options;

        public override void InstallBindings()
        {
            options = Container.BindMessagePipe();


            BindSceneInstaller();
            BindMessages();
        }

        private void BindSceneInstaller()
        {
            Container.Bind<SceneInfo>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<InterstitialAdController>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<AppOpenAdController>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<AdsBanner>().FromComponentInHierarchy().AsSingle().NonLazy();
        }

        private void BindMessages()
        {
            Container.BindMessageBroker<LoadSceneEvent>(options);
            Container.BindMessageBroker<ShowInterstitialAdEvent>(options);

            Container.BindMessageBroker<CreatureDeadEvent>(options);
            Container.BindMessageBroker<CreateCreatureEvent>(options); 
            Container.BindMessageBroker<CreatureCommandEvent>(options);
            Container.BindMessageBroker<CreatureTransformCommandEvent>(options);

            Container.BindMessageBroker<SelectedWeaponEvent>(options); 
        }
    }
}
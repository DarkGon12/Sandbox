using MessagePipe;
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
            Container.Bind<AppOpenAdController>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<RewardedAdService>().FromComponentInHierarchy().AsSingle().NonLazy();
        }

        private void BindMessages()
        {
            Container.BindMessageBroker<LoadSceneEvent>(options);
            Container.BindMessageBroker<ShowInterstitialAdEvent>(options);

            Container.BindMessageBroker<CreatureRemoveAll>(options);
            Container.BindMessageBroker<CreatureDeadEvent>(options);
            Container.BindMessageBroker<CreateCreatureEvent>(options); 
            Container.BindMessageBroker<CreatureCommandEvent>(options);
            Container.BindMessageBroker<CreatureTransformCommandEvent>(options);

            Container.BindMessageBroker<SelectedWeaponEvent>(options);
            Container.BindMessageBroker<CreatureWindowView>(options);
            Container.BindMessageBroker<CreatureVisibleEffect>(options);
            Container.BindMessageBroker<ChangePlayerTagEvent>(options);
        }
    }
}
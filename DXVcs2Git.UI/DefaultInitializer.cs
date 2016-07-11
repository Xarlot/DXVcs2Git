using DXVcs2Git.UI.ViewModels;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;

namespace DXVcs2Git.UI {
    public static class DefaultInitializer {
        public static RootViewModel RootViewModel { get; private set; }
        static IUnityContainer RootContainer { get; } = new UnityContainer();
        static IUnityContainer WorkHorseContainer { get; set; }

        static DefaultInitializer() {
            RootViewModel = new RootViewModel();

            BuildRootConfiguration();
            BuildWorkHorseConfiguration();
        }
        static void BuildRootConfiguration() {
            RootContainer.RegisterInstance(RootViewModel);
            RootContainer.RegisterType<RepositoriesViewModel>(new ContainerControlledLifetimeManager());
        }
        static void BuildWorkHorseConfiguration() {
            WorkHorseContainer = RootContainer.CreateChildContainer();

            WorkHorseContainer.RegisterType<EditRepositoriesViewModel>(new PerResolveLifetimeManager());
            WorkHorseContainer.RegisterType<EditBranchViewModel>(new PerResolveLifetimeManager());
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(WorkHorseContainer));
        }
        public static void Initialize() {
        }

        //static void BuildColorizer() {
        //    ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(WorkHorseContainer));
        //    WorkHorseContainer = RootContainer.CreateChildContainer();
        //    WorkHorseContainer.RegisterType<UndoManager>();
        //    WorkHorseContainer.RegisterType<ColorizerViewModel, ColorizerViewModel>(new ContainerControlledLifetimeManager());
        //    WorkHorseContainer.RegisterType<IThemeDesignerViewModel, ColorizerViewModel>();
        //    WorkHorseContainer.RegisterType<IThemeDesignerColorizerViewModel, ColorizerViewModel>();
        //    WorkHorseContainer.RegisterType<IThemeDesignerAddinsContainer, ThemeDesignerAddinsContainer>(new ContainerControlledLifetimeManager(), new InjectionConstructor(Constants.AddinsPath));
        //    WorkHorseContainer.RegisterInstance(WorkHorseContainer.Resolve<ThemeDesignerState>());

        //    Package.Initialize();
        //}

        //static void ReleaseColorizer() {
        //    WorkHorseContainer.Dispose();
        //}
        //public static void Initialize() {
        //    Messenger.Register(RootContainer, GlobalOperations.RebuildContainer, x => RebuildContainer());
        //    RootViewModel = new RootViewModel();
        //    BuildColorizer();
        //}
        //static void RebuildContainer() {
        //    ReleaseColorizer();
        //    BuildColorizer();
        //}

        //static void BuildRootConfiguration() {
        //    RootContainer.RegisterInstance<IThemeDesignerOwner>(Package);
        //    RootContainer.RegisterInstance<IThemeDesignerServiceContainer>(Package);
        //    RootContainer.RegisterType<IThemeDesignerContainer, ThemeDesignerContainer>(new ContainerControlledLifetimeManager());
        //    RootContainer.RegisterType<ThemeDesignerMainMenuBuilder, ThemeDesignerMainMenuBuilder>();
        //    RootContainer.RegisterType<IThemeDesignerMainMenuCommandsSource, ThemeDesignerMainMenuBuilder>();
        //    RootContainer.RegisterType<IThemeDesignerColorizerCommandsSource, ThemeDesignerColorizerMenuBuilder>();
        //    RootContainer.RegisterType<ThemeDesignerStateMachine>(new ContainerControlledLifetimeManager());
        //    RootContainer.RegisterType<ThemeDesignerState>();
        //    RootContainer.RegisterType<ISolution, Solution>();
        //    RootContainer.RegisterType<DTE>(new ContainerControlledLifetimeManager());
        //    RootContainer.RegisterType<IServiceContainer, ServiceContainer>(new ContainerControlledLifetimeManager());
        //    RootContainer.RegisterType<IVsSolution, SVsSolution>();
        //    RootContainer.RegisterType<IThemeDesignerMessenger, ThemeDesignerMessenger>(new ContainerControlledLifetimeManager());
        //    RootContainer.RegisterType<IThemeDesignerGlobalSettings, ThemeDesignerGlobalSettings>(new ContainerControlledLifetimeManager());
        //    ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(RootContainer));
    }
}


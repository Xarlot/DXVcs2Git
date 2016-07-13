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
            WorkHorseContainer.RegisterType<EditBranchDescriptionViewModel>(new PerResolveLifetimeManager());
            WorkHorseContainer.RegisterType<EditMergeRequestViewModel>(new PerResolveLifetimeManager());
            WorkHorseContainer.RegisterType<EditBranchChangesViewModel>(new PerResolveLifetimeManager());
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(WorkHorseContainer));
        }
        public static void Initialize() {
        }
    }
}


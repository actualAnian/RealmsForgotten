using System;
using TaleWorlds.Library;

namespace RealmsForgotten.UI
{
    public static class ViewModelExtensions
    {
        public static bool HasExtensionType(this ViewModel model) => ViewModelExtensionManager.Instance.HasViewModelExtensionType(model);
        public static bool HasExtensionInstance(this ViewModel model) => ViewModelExtensionManager.Instance.HasViewModelExtensionInstance(model);
        public static IViewModelExtension GetExtensionInstance(this ViewModel model) => ViewModelExtensionManager.Instance.GetExtensionInstance(model);
        public static Type GetExtensionType(this ViewModel model) => ViewModelExtensionManager.Instance.GetExtensionType(model);
    }
}

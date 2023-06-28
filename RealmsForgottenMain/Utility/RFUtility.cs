using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Utility
{
    internal class RFUtility
    {
        public static void RemoveInitialStateOption(string name)
        {
            try
            {
                FieldInfo field = typeof(TaleWorlds.MountAndBlade.Module).GetField("_initialStateOptions", BindingFlags.Instance | BindingFlags.NonPublic);
                object value = field.GetValue(TaleWorlds.MountAndBlade.Module.CurrentModule);
                //bool flag = !(value.GetType() == typeof(List<InitialStateOption>));
                if (value.GetType() == typeof(List<InitialStateOption>))
                {
                    List<InitialStateOption> list = (List<InitialStateOption>)value;
                    foreach (InitialStateOption initialStateOption in list)
                    {
                        bool flag2 = initialStateOption.Id.Contains(name);
                        if (flag2)
                        {
                            list.Remove(initialStateOption);
                        }
                    }
                    field.SetValue(typeof(TaleWorlds.MountAndBlade.Module).GetField("_initialStateOptions", BindingFlags.Instance | BindingFlags.NonPublic), list);
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(ex.Message));
            }
        }
    }
}

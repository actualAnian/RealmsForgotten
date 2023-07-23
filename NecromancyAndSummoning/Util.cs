using System;
using System.Reflection;

namespace RealmsForgotten.NecromancyAndSummoning
{
	// Token: 0x02000007 RID: 7
	internal class Util
	{
		// Token: 0x06000064 RID: 100 RVA: 0x0000547C File Offset: 0x0000367C
		internal static object GetInstanceProperty<T>(T instance, string propertyName)
		{
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			return typeof(T).GetProperty(propertyName, bindingAttr);
		}

		// Token: 0x06000065 RID: 101 RVA: 0x000054A4 File Offset: 0x000036A4
		internal static object GetInstanceField<T>(T instance, string fieldName)
		{
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			return typeof(T).GetField(fieldName, bindingAttr);
		}
	}
}

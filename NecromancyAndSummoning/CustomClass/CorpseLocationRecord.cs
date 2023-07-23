using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.NecromancyAndSummoning.CustomClass
{
	// Token: 0x02000010 RID: 16
	internal class CorpseLocationRecord
	{
		// Token: 0x1700001E RID: 30
		// (get) Token: 0x060000A6 RID: 166 RVA: 0x00005AB2 File Offset: 0x00003CB2
		// (set) Token: 0x060000A7 RID: 167 RVA: 0x00005ABA File Offset: 0x00003CBA
		public Vec3 location { get; set; }

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x060000A8 RID: 168 RVA: 0x00005AC3 File Offset: 0x00003CC3
		// (set) Token: 0x060000A9 RID: 169 RVA: 0x00005ACB File Offset: 0x00003CCB
		public BasicCharacterObject deadUnit { get; set; }

		// Token: 0x060000AA RID: 170 RVA: 0x00005AD4 File Offset: 0x00003CD4
		public CorpseLocationRecord(BasicCharacterObject deadUnit, Vec3 location)
		{
			this.deadUnit = deadUnit;
			this.location = location;
		}
	}
}

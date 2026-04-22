using System;

namespace NecromancyAndSummoning.CustomClass
{
	// Token: 0x02000015 RID: 21
	internal class UnitBuildFromPart
	{
		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060000C8 RID: 200 RVA: 0x00005C1D File Offset: 0x00003E1D
		// (set) Token: 0x060000C9 RID: 201 RVA: 0x00005C25 File Offset: 0x00003E25
		public string[] UnitId { get; set; }

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060000CA RID: 202 RVA: 0x00005C2E File Offset: 0x00003E2E
		// (set) Token: 0x060000CB RID: 203 RVA: 0x00005C36 File Offset: 0x00003E36
		public int[] BoneNeeded { get; set; }

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060000CC RID: 204 RVA: 0x00005C3F File Offset: 0x00003E3F
		// (set) Token: 0x060000CD RID: 205 RVA: 0x00005C47 File Offset: 0x00003E47
		public int Amount { get; set; }
	}
}

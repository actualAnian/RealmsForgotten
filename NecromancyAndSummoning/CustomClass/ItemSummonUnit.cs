using System;

namespace NecromancyAndSummoning.CustomClass
{
	// Token: 0x02000013 RID: 19
	internal class ItemSummonUnit
	{
		// Token: 0x17000026 RID: 38
		// (get) Token: 0x060000B9 RID: 185 RVA: 0x00005B66 File Offset: 0x00003D66
		// (set) Token: 0x060000BA RID: 186 RVA: 0x00005B6E File Offset: 0x00003D6E
		public string ItemId { get; set; }

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x060000BB RID: 187 RVA: 0x00005B77 File Offset: 0x00003D77
		// (set) Token: 0x060000BC RID: 188 RVA: 0x00005B7F File Offset: 0x00003D7F
		public string[] UnitId { get; set; }

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x060000BD RID: 189 RVA: 0x00005B88 File Offset: 0x00003D88
		// (set) Token: 0x060000BE RID: 190 RVA: 0x00005B90 File Offset: 0x00003D90
		public int SummonAmount { get; set; }
	}
}

using System;
using System.Collections.Generic;
using RealmsForgotten.NecromancyAndSummoning.CustomClass;
using Newtonsoft.Json;

namespace RealmsForgotten.NecromancyAndSummoning.Config
{
	// Token: 0x0200000B RID: 11
	internal class ItemUnitConfig
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600006E RID: 110 RVA: 0x0000589D File Offset: 0x00003A9D
		// (set) Token: 0x0600006F RID: 111 RVA: 0x000058A5 File Offset: 0x00003AA5
		[JsonProperty("itemInfectUnit")]
		public List<ItemInfectUnit> ItemInfectUnit { get; set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000072 RID: 114 RVA: 0x000058BF File Offset: 0x00003ABF
		// (set) Token: 0x06000073 RID: 115 RVA: 0x000058C7 File Offset: 0x00003AC7
		[JsonProperty("itemSummonUnit")]
		public List<ItemSummonUnit> ItemSummonUnit { get; set; }
	}
}

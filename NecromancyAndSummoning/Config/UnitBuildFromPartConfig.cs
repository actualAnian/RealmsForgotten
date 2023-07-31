using System;
using System.Collections.Generic;
using RealmsForgotten.NecromancyAndSummoning.CustomClass;
using Newtonsoft.Json;

namespace RealmsForgotten.NecromancyAndSummoning.Config
{
	// Token: 0x0200000C RID: 12
	internal class UnitBuildFromPartConfig
	{
		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000075 RID: 117 RVA: 0x000058D9 File Offset: 0x00003AD9
		// (set) Token: 0x06000076 RID: 118 RVA: 0x000058E1 File Offset: 0x00003AE1
		[JsonProperty("boneParts")]
		public string[] BoneParts { get; set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000077 RID: 119 RVA: 0x000058EA File Offset: 0x00003AEA
		// (set) Token: 0x06000078 RID: 120 RVA: 0x000058F2 File Offset: 0x00003AF2
		[JsonProperty("unitBuildFromPart")]
		public List<UnitBuildFromPart> UnitBuildFromPart { get; set; }
	}
}

using System;
using System.Collections.Generic;
using RealmsForgotten.NecromancyAndSummoning.CustomClass;
using Newtonsoft.Json;

namespace RealmsForgotten.NecromancyAndSummoning.Config
{
	// Token: 0x0200000D RID: 13
	internal class UnitUnitConfig
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600007A RID: 122 RVA: 0x00005904 File Offset: 0x00003B04
		// (set) Token: 0x0600007B RID: 123 RVA: 0x0000590C File Offset: 0x00003B0C
		[JsonProperty("unitInfectUnit")]
		public List<UnitInfectUnit> UnitInfectUnit { get; set; }
	}
}

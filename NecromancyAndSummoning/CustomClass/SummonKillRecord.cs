using System;
using TaleWorlds.CampaignSystem.Party;

namespace RealmsForgotten.NecromancyAndSummoning.CustomClass
{
	// Token: 0x02000014 RID: 20
	internal class SummonKillRecord
	{
		// Token: 0x17000029 RID: 41
		// (get) Token: 0x060000C0 RID: 192 RVA: 0x00005BA2 File Offset: 0x00003DA2
		// (set) Token: 0x060000C1 RID: 193 RVA: 0x00005BAA File Offset: 0x00003DAA
		internal PartyBase party { get; set; }

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x060000C2 RID: 194 RVA: 0x00005BB3 File Offset: 0x00003DB3
		// (set) Token: 0x060000C3 RID: 195 RVA: 0x00005BBB File Offset: 0x00003DBB
		internal string unitId { get; set; }

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x060000C4 RID: 196 RVA: 0x00005BC4 File Offset: 0x00003DC4
		// (set) Token: 0x060000C5 RID: 197 RVA: 0x00005BCC File Offset: 0x00003DCC
		internal int killCount { get; set; }

		// Token: 0x060000C6 RID: 198 RVA: 0x00005BD5 File Offset: 0x00003DD5
		public SummonKillRecord()
		{
			this.party = null;
			this.unitId = "";
			this.killCount = 0;
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x00005BFB File Offset: 0x00003DFB
		public SummonKillRecord(PartyBase party, string unitId, int killCount)
		{
			this.party = party;
			this.unitId = unitId;
			this.killCount = killCount;
		}
	}
}

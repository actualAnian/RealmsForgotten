using System;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.NecromancyAndSummoning.CustomClass
{
	// Token: 0x0200000F RID: 15
	internal class BattleInfectedRecord
	{
		// Token: 0x1700001B RID: 27
		// (get) Token: 0x0600009E RID: 158 RVA: 0x00005A37 File Offset: 0x00003C37
		// (set) Token: 0x0600009F RID: 159 RVA: 0x00005A3F File Offset: 0x00003C3F
		public Clan clan { get; private set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x060000A0 RID: 160 RVA: 0x00005A48 File Offset: 0x00003C48
		// (set) Token: 0x060000A1 RID: 161 RVA: 0x00005A50 File Offset: 0x00003C50
		public string infectedUnitId { get; private set; }

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x060000A2 RID: 162 RVA: 0x00005A59 File Offset: 0x00003C59
		// (set) Token: 0x060000A3 RID: 163 RVA: 0x00005A61 File Offset: 0x00003C61
		public int infectedUnitNumber { get; internal set; }

		// Token: 0x060000A4 RID: 164 RVA: 0x00005A6A File Offset: 0x00003C6A
		public BattleInfectedRecord()
		{
			this.clan = null;
			this.infectedUnitId = "";
			this.infectedUnitNumber = 0;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00005A90 File Offset: 0x00003C90
		public BattleInfectedRecord(Clan clan, string infectedUnitId, int infectedUnitNumber)
		{
			this.clan = clan;
			this.infectedUnitId = infectedUnitId;
			this.infectedUnitNumber = infectedUnitNumber;
		}
	}
}

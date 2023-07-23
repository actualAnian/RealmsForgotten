using System;
using Newtonsoft.Json;

namespace RealmsForgotten.NecromancyAndSummoning.Config
{
	// Token: 0x0200000E RID: 14
	internal class Config
	{
		// Token: 0x1700000B RID: 11
		// (get) Token: 0x0600007D RID: 125 RVA: 0x0000591E File Offset: 0x00003B1E
		// (set) Token: 0x0600007E RID: 126 RVA: 0x00005926 File Offset: 0x00003B26
		[JsonProperty("enablePlayerSummon")]
		public bool EnablePlayerSummon { get; set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600007F RID: 127 RVA: 0x0000592F File Offset: 0x00003B2F
		// (set) Token: 0x06000080 RID: 128 RVA: 0x00005937 File Offset: 0x00003B37
		[JsonProperty("enableTroopSummon")]
		public bool EnableTroopSummon { get; set; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000081 RID: 129 RVA: 0x00005940 File Offset: 0x00003B40
		// (set) Token: 0x06000082 RID: 130 RVA: 0x00005948 File Offset: 0x00003B48
		[JsonProperty("summonEachKillXp")]
		public int SummonEachKillXp { get; set; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000083 RID: 131 RVA: 0x00005951 File Offset: 0x00003B51
		// (set) Token: 0x06000084 RID: 132 RVA: 0x00005959 File Offset: 0x00003B59
		[JsonProperty("enableTroopInfect")]
		public bool EnableTroopInfect { get; set; }

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000085 RID: 133 RVA: 0x00005962 File Offset: 0x00003B62
		// (set) Token: 0x06000086 RID: 134 RVA: 0x0000596A File Offset: 0x00003B6A
		[JsonProperty("enableItemInfect")]
		public bool EnableItemInfect { get; set; }

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000089 RID: 137 RVA: 0x00005984 File Offset: 0x00003B84
		// (set) Token: 0x0600008A RID: 138 RVA: 0x0000598C File Offset: 0x00003B8C
		[JsonProperty("enableNPCInfect")]
		public bool EnableNPCInfect { get; set; }

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x0600008B RID: 139 RVA: 0x00005995 File Offset: 0x00003B95
		// (set) Token: 0x0600008C RID: 140 RVA: 0x0000599D File Offset: 0x00003B9D
		[JsonProperty("NPCInfectionResistPercentage")]
		public int npcInfectionResistPercentage { get; set; }

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600008D RID: 141 RVA: 0x000059A6 File Offset: 0x00003BA6
		// (set) Token: 0x0600008E RID: 142 RVA: 0x000059AE File Offset: 0x00003BAE
		[JsonProperty("infectionBasePercentage")]
		public int InfectionBasePercentage { get; set; }

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600008F RID: 143 RVA: 0x000059B7 File Offset: 0x00003BB7
		// (set) Token: 0x06000090 RID: 144 RVA: 0x000059BF File Offset: 0x00003BBF
		[JsonProperty("joinPartyMode")]
		public bool JoinPartyMode { get; set; }

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000091 RID: 145 RVA: 0x000059C8 File Offset: 0x00003BC8
		// (set) Token: 0x06000092 RID: 146 RVA: 0x000059D0 File Offset: 0x00003BD0
		[JsonProperty("spawnPartyMode")]
		public bool SpawnPartyMode { get; set; }

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000093 RID: 147 RVA: 0x000059D9 File Offset: 0x00003BD9
		// (set) Token: 0x06000094 RID: 148 RVA: 0x000059E1 File Offset: 0x00003BE1
		[JsonProperty("battleSimulationMode")]
		public bool BattleSimulationMode { get; set; }

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000095 RID: 149 RVA: 0x000059EA File Offset: 0x00003BEA
		// (set) Token: 0x06000096 RID: 150 RVA: 0x000059F2 File Offset: 0x00003BF2
		[JsonProperty("spawnPartyMinUnit")]
		public int SpawnPartyMinUnit { get; set; }

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000097 RID: 151 RVA: 0x000059FB File Offset: 0x00003BFB
		// (set) Token: 0x06000098 RID: 152 RVA: 0x00005A03 File Offset: 0x00003C03
		[JsonProperty("immuneInfectTroop")]
		public string[] ImmuneInfectTroop { get; set; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000099 RID: 153 RVA: 0x00005A0C File Offset: 0x00003C0C
		// (set) Token: 0x0600009A RID: 154 RVA: 0x00005A14 File Offset: 0x00003C14
		[JsonProperty("enableBuildTroopFromPart")]
		public bool EnableBuildTroopFromPart { get; set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600009B RID: 155 RVA: 0x00005A1D File Offset: 0x00003C1D
		// (set) Token: 0x0600009C RID: 156 RVA: 0x00005A25 File Offset: 0x00003C25
		[JsonProperty("enableRaiseCrimeRating")]
		public bool EnableRaiseCrimeRating { get; set; }
	}
}

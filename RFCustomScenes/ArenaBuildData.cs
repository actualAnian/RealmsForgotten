using RealmsForgotten;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using System;
using Z.Expressions;

namespace RFCustomSettlements
{
    public class ArenaBuildData
    {
        public class StageData
        {
            public Equipment PlayerEquipment { get; private set; }
            public List<ArenaTeam> ArenaTeams { get; private set; }
            public string ArenaSceneId { get; private set; }
            public StageData(List<ArenaTeam> teams, Equipment playerEquipment, string arenaId)
            {
                ArenaTeams = teams;
                this.PlayerEquipment = playerEquipment;
                ArenaSceneId = arenaId;
            }
        }
        public List<ArenaChallenge> Challenges { get; private set; }
        ArenaBuildData(List<ArenaChallenge> challengesData)
        {
            Challenges = challengesData;
        }

        public class ArenaChallenge
        {
            public ArenaChallenge(string challengeName, List<StageData> stagedatas, string? conditionToParse = null)
            {
                ChallengeName = challengeName;
                StageDatas = stagedatas;
                ChallengeCondition = CreateCondition(challengeName, conditionToParse);
            }

            private Func<CharacterObject, int, bool> CreateCondition(string challengeName, string? conditionToParse = null)
            {
                if (conditionToParse == null) return (character, clanTier) => true;
                var context = new EvalContext
                {
                    SafeMode = true
                };
                context.UnregisterAll();
                context.RegisterExtensionMethod(typeof(Globals));
                try
                {
                    return context.Compile<Func<CharacterObject, int, bool>>(conditionToParse, "Player", "PlayerClanTier");
                }
                catch (Exception)
                {
                    RealmsForgotten.HuntableHerds.SubModule.PrintDebugMessage($"Error parsing the condition for arena challenge {challengeName}");
                    return (character, clanTier) => false;
                };
                //return context.Compile<Func<CharacterObject, int, bool>>("Player.IsGiant() && PlayerClanTier >= 1 && PlayerClanTier <= 2", "Player", "PlayerClanTier");
            }


            public Func<CharacterObject, int, bool> ChallengeCondition { get; private set; }
            public string ChallengeName { get; private set; }
            public List<StageData> StageDatas { get; private set; }
        }
        public static ArenaBuildData BuildArenaData()
        {
            string mainPath = Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location);

            string xmlFileName = Path.Combine(mainPath, "arena_config.xml");
            List<ArenaChallenge> challenges = new ();
            XElement arenaConfig = XElement.Load(xmlFileName);

            foreach (XElement element in arenaConfig.Descendants("ArenaChallenge"))
            {
                string challengeName = element.Element("ChallengeName").Value;
                XElement challengeCon = element.Element("ChallengeCondition");
                string? challengeCondition = default;
                if (challengeCon != null) challengeCondition = challengeCon.Value;
                //else challengeCondition = "";
                List<StageData> stageDatas = new();

                for (int i = 1; ;i++)
                {
                    XElement stage = element.Element("Stage" + i);
                    if (stage == null) break;
                    string arenaId = stage.Element("ArenaId").Value;
                    List<ArenaTeam> teamList = new();
                    Equipment equipment = new Equipment();
                    for (int j = 1; ; j++)
                    {
                        XElement team = stage.Element("Team" + j);
                        if (team == null) break;
                        int color = int.Parse(team.Element("color").Value);


                        string equipmentId;
                        var playerEquipmentId = team.Element("PlayerEquipment");
                        if (playerEquipmentId != null)
                        {
                            equipmentId = playerEquipmentId.Value == "" ? "rf_looter" : playerEquipmentId.Value;
                            equipment = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(equipmentId).DefaultEquipment;
                        }
                        ArenaTeam arenaTeam = new(color, playerEquipmentId != null);
                        foreach (XElement participant in team.Descendants("Participant"))
                        {
                            var aha = int.Parse(participant.Element("amount").Value);
                            for(int _ = 0; _ < aha; _++)
                                arenaTeam.AddMember(MBObjectManager.Instance.GetObject<CharacterObject>(participant.Element("id").Value));
                        }
                        teamList.Add(arenaTeam);
                    }
                    stageDatas.Add(new StageData(teamList, equipment, arenaId));
                }
                challenges.Add(new(challengeName, stageDatas, challengeCondition));
            }
            return new ArenaBuildData(challenges);
        }
    }
}

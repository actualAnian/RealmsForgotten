using RealmsForgotten;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;

namespace RFCustomSettlements
{

    public interface IBuildData
    {

    }
    public class ArenaBuildData : IBuildData
    {
        public class StageData
        {
            public Equipment playerEquipment;
            public List<ArenaTeam> ArenaTeams;
            public StageData(List<ArenaTeam> teams, Equipment playerEquipment)
            {
                ArenaTeams = teams;
                this.playerEquipment = playerEquipment;
            }
        }
        public List<ArenaChallenge> Challenges { get; private set; }
        ArenaBuildData(List<ArenaChallenge> challengesData)
        {
            Challenges = challengesData;
        }

        public class ArenaChallenge
        {
            public ArenaChallenge(string challengeName, List<StageData> stagedatas)
            {
                ChallengeName = challengeName;
                StageDatas = stagedatas;
            }
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
                string ChallengeName = element.Element("ChallengeName").Value;
                List<StageData> stageDatas = new();

                for (int i = 1; ;i++)
                {
                    XElement stage = element.Element("Stage" + i);
                    if (stage == null) break;
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
                    stageDatas.Add(new StageData(teamList, equipment));
                }
                challenges.Add(new(ChallengeName, stageDatas));
            }
            return new ArenaBuildData(challenges);
        }
    }
}

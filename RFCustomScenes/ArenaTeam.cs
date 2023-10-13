using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements
{
    public class ArenaTeam
    {
        public uint TeamColor { get; private set; }
        public Banner TeamBanner { get; private set; }
        public bool IsPlayerTeam { get; private set; }
        public List<CharacterObject> members;
        public Team? MissionTeam { get; set; }
        private int membersCount;
        public ArenaTeam(int _membersCount, uint teamColor, Banner teamBanner, Team _team)
        {
            TeamColor = teamColor;
            TeamBanner = teamBanner;
            membersCount = _membersCount;
            members = new();
            MissionTeam = _team;
        }
        public ArenaTeam(int teamColor, bool isPlayerTeam = false)
        {
            IsPlayerTeam = isPlayerTeam;
            TeamColor = BannerManager.GetColor(teamColor);
            TeamBanner = Banner.CreateOneColoredEmptyBanner(teamColor);
            membersCount = 0;
            members = new();
        }
        public void AddMember(CharacterObject member) { members.Add(member); membersCount += 1; }
        public void RemoveMember() { membersCount -= 1; }

        internal void SetIsEnemyOf(ArenaTeam arenaTeam)
        {
            if (MissionTeam != null) MissionTeam.SetIsEnemyOf(arenaTeam.MissionTeam, true);
        }

        internal bool hasNoMembers()
        {
            return membersCount == 0;
        }
    }
}

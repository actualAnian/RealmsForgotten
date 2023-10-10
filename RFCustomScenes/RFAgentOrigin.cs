using System;
using System.IO.Ports;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using static RFCustomSettlements.ExploreSettlementStateHandler;

namespace RealmsForgotten.RFCustomSettlements.AgentOrigins
{
    public class RFAgentOrigin : IAgentOriginBase
    {
        internal RFAgentOrigin(PartyBase party, UniqueTroopDescriptor descriptor, int rank, CharacterObject character, bool isUnderPlayersCommand = false)
        {
            this.characterObject = character;
            this._party = party;
            this._descriptor = descriptor;
            this._isUnderPlayerCommand = isUnderPlayersCommand;
            _rank = rank;
        }
        public PartyBase Party
        {
            get
            {
                return _party;
                //                return this._supplier.GetParty(this._descriptor);
            }
        }
        public IBattleCombatant BattleCombatant
        {
            get
            {
                return this.Party;
            }
        }
        public Banner Banner
        {
            get
            {
                if (this.Party.LeaderHero == null)
                {
                    return this.Party.MapFaction.Banner;
                }
                return this.Party.LeaderHero.ClanBanner;
            }
        }
        public int UniqueSeed
        {
            get
            {
                return this._descriptor.UniqueSeed;
            }
        }
        public CharacterObject Troop
        {
            get
            {
                return characterObject;
//                return MBObjectManager.Instance.GetObject<CharacterObject>("looter");
//                return this._supplier.GetTroop(this._descriptor);
            }
        }
        BasicCharacterObject IAgentOriginBase.Troop
        {
            get
            {
                return this.Troop;
            }
        }
        public UniqueTroopDescriptor TroopDesc
        {
            get
            {
                return this._descriptor;
            }
        }
        public bool IsUnderPlayersCommand
        {
            get
            {
                return _isUnderPlayerCommand;
              //  return this.Troop == Hero.MainHero.CharacterObject || RFAgentOrigin.IsPartyUnderPlayerCommand(this.Party);
            }
        }
        public uint FactionColor
        {
            get
            {
                return this.Party.MapFaction.Color;
            }
        }
        public uint FactionColor2
        {
            get
            {
                return this.Party.MapFaction.Color2;
            }
        }
        public int Seed
        {
            get
            {
                return CharacterHelper.GetPartyMemberFaceSeed(this.Party, this.Troop, this.Rank);
            }
        }

        public int Rank
        {
            get
            {
                return this._rank;
            }
        }

        public void SetWounded()
        {
            if (!this._isRemoved)
            {
                if (Party == MobileParty.MainParty.Party)
                { 
                    this.Party.MemberRoster.AddToCounts(Troop, 0, false, 1, 0, true, -1);
                    NextSceneData.Instance.OnTroopWounded(Troop);
                }
                //               this._supplier.OnTroopWounded(this._descriptor);
                this._isRemoved = true;
            }
        }
        public void SetKilled()
        {
            if (!this._isRemoved)
            {
                if (Party == MobileParty.MainParty.Party)
                { 
                    Party.MemberRoster.AddToCounts(Troop, -1, false, 0, 0, true, -1);
                    NextSceneData.Instance.OnTroopKilled(Troop);
                }
                if (this.Troop.IsHero)
                {
                    KillCharacterAction.ApplyByBattle(this.Troop.HeroObject, null, true);
                }
                this._isRemoved = true;
            }
        }
        public void SetRouted()
        {
            if (!this._isRemoved)
            {
 //               this._supplier.OnTroopRouted(this._descriptor);
                this._isRemoved = true;
            }
        }
        public void OnAgentRemoved(float agentHealth)
        {
            if (this.Troop.IsHero)
            {
                this.Troop.HeroObject.HitPoints = MathF.Max(1, MathF.Round(agentHealth));
            }
        }
        void IAgentOriginBase.OnScoreHit(BasicCharacterObject victim, BasicCharacterObject captain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
        {
//            this._supplier.OnTroopScoreHit(this._descriptor, victim, damage, isFatal, isTeamKill, attackerWeapon);
        }
        public void SetBanner(Banner banner)
        {
            throw new NotImplementedException();
        }
        public static bool IsPartyUnderPlayerCommand(PartyBase party)
        {
            return party == PartyBase.MainParty;
        }

        private readonly UniqueTroopDescriptor _descriptor;
        private readonly bool _isUnderPlayerCommand;
        private bool _isRemoved;
        private readonly CharacterObject characterObject;
        private readonly PartyBase _party;
        private readonly int _rank;
    }
}

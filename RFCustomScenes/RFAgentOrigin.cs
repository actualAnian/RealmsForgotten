using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using static RealmsForgotten.RFCustomSettlements.ExploreSettlementStateHandler;

namespace RFCustomSettlements
{
    public class RFAgentOrigin : IAgentOriginBase
    {
        internal RFAgentOrigin(PartyBase party, UniqueTroopDescriptor descriptor, int rank, CharacterObject character, bool isUnderPlayersCommand = false)
        {
            characterObject = character;
            _party = party;
            _descriptor = descriptor;
            _isUnderPlayerCommand = isUnderPlayersCommand;
            _rank = rank; 
            AgentOriginUtilities.GetDefaultTroopTraits(this.Troop, out this._hasThrownWeapon, out this._hasSpear, out this._hasShield, out this._hasHeavyArmor);

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
                return Party;
            }
        }
        public Banner Banner
        {
            get
            {
                if (Party.LeaderHero == null)
                {
                    return Party.MapFaction.Banner;
                }
                return Party.LeaderHero.ClanBanner;
            }
        }
        public int UniqueSeed
        {
            get
            {
                return _descriptor.UniqueSeed;
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
                return Troop;
            }
        }
        public UniqueTroopDescriptor TroopDesc
        {
            get
            {
                return _descriptor;
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
                return Party.MapFaction.Color;
            }
        }
        public uint FactionColor2
        {
            get
            {
                return Party.MapFaction.Color2;
            }
        }
        public int Seed
        {
            get
            {
                return CharacterHelper.GetPartyMemberFaceSeed(Party, Troop, Rank);
            }
        }

        public int Rank
        {
            get
            {
                return _rank;
            }
        }

        public bool HasThrownWeapon => _hasThrownWeapon;
        public bool HasHeavyArmor => _hasHeavyArmor;
        public bool HasShield => _hasShield;
        public bool HasSpear => _hasSpear;

        public void SetWounded()
        {
            if (!_isRemoved)
            {
                if (Party == MobileParty.MainParty.Party)
                {
                    Party.MemberRoster.AddToCounts(Troop, 0, false, 1, 0, true, -1);
                    NextSceneData.Instance.OnTroopWounded(Troop);
                }
                //               this._supplier.OnTroopWounded(this._descriptor);
                _isRemoved = true;
            }
        }
        public void SetKilled()
        {
            if (!_isRemoved)
            {
                if (Party == MobileParty.MainParty.Party)
                { 
                    Party.MemberRoster.AddToCounts(Troop, -1, false, 0, 0, true, -1);
                    NextSceneData.Instance.OnTroopKilled(Troop);
                }
                if (Troop.IsHero)
                {
                    KillCharacterAction.ApplyByBattle(Troop.HeroObject, null, true);
                }
                _isRemoved = true;
            }
        }
        public void OnAgentRemoved(float agentHealth)
        {
            if (Troop.IsHero)
            {
                Troop.HeroObject.HitPoints = MathF.Max(1, MathF.Round(agentHealth));
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

        public TroopTraitsMask GetTraitsMask()
        {
            return AgentOriginUtilities.GetDefaultTraitsMask(this);
        }

        public void SetRouted(bool isOrderRetreat)
        {
            if (!_isRemoved)
            {
                //               this._supplier.OnTroopRouted(this._descriptor);
                _isRemoved = true;
            }
        }

        private bool _hasThrownWeapon;
        private bool _hasHeavyArmor;
        private bool _hasShield;
        private bool _hasSpear;

        private readonly UniqueTroopDescriptor _descriptor;
        private readonly bool _isUnderPlayerCommand;
        private bool _isRemoved;
        private readonly CharacterObject characterObject;
        private readonly PartyBase _party;
        private readonly int _rank;
    }
}

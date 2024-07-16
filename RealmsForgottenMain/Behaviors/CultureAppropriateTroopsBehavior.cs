using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;

namespace CampaignBehaviors
{
    internal class CultureAppropriateTroopsBehavior : CampaignBehaviorBase
    {
        private readonly Dictionary<Clan, List<CharacterObject>> _stackCache = new();

        private readonly Dictionary<CultureObject, List<CharacterObject>> _troopTreeCache = new();

        public override void RegisterEvents()
        {
            CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnTroopRecruited);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
        }

        private void DailyTickSettlement(Settlement settlement)
        {
            if (settlement.Town?.GarrisonParty?.MemberRoster == null || settlement.Owner == null) return;

            if (settlement.IsUnderSiege || settlement.InRebelliousState) return;

            if (settlement.OwnerClan.Equals(Clan.PlayerClan)) return;

            foreach (var e in settlement.Town.GarrisonParty.MemberRoster.GetTroopRoster().ToList())
                ExchangeTroops(settlement.Owner, settlement.Town.GarrisonParty.MemberRoster, e.Character, e.Number);
        }

        private void OnTroopRecruited(Hero? recruiter,
                                      Settlement settlement,
                                      Hero recruitmentSource,
                                      CharacterObject? troop,
                                      int count)
        {
            if (recruiter is not { PartyBelongedTo.MemberRoster: not null }) return;

            ExchangeTroops(recruiter, recruiter.PartyBelongedTo.MemberRoster, troop, count);
        }

        private void ExchangeTroops(Hero? owner, TroopRoster roster, CharacterObject? troop, int count)
        {
            if (owner?.Clan == null || owner.Equals(Hero.MainHero) || troop?.Culture == null || troop.IsHero) return;

            if (!_stackCache.TryGetValue(owner.Clan, out var templateCharacters))
            {
                if (owner.Clan.DefaultPartyTemplate != null)
                {
                    templateCharacters = owner.Clan.DefaultPartyTemplate.Stacks.Select(stack => stack.Character).ToList();
                    _stackCache.Add(owner.Clan, templateCharacters);
                }
                else { templateCharacters = new List<CharacterObject>(); }
            }

            if (templateCharacters.Contains(troop)) return;

            var replacement = !IsEliteTroop(troop) && owner.Clan.DefaultPartyTemplate != null
                                  ? DetermineReplacement(templateCharacters, troop.Tier)
                                  : null;

            if (replacement == null && !troop.Culture.Equals(owner.Clan.Culture))
            {
                var basic = DetermineBasicTroop(owner, IsEliteTroop(troop));
                replacement = DetermineReplacement(basic, troop.Tier);
            }

            if (replacement == null || !roster.Contains(troop)) return;

            roster.RemoveTroop(troop, count);
            _ = roster.AddToCounts(replacement, count);
            roster.RemoveZeroCounts();
        }

        private static CharacterObject DetermineReplacement(List<CharacterObject> templateCharacters, int tier)
        {
            if (tier == 1 && !templateCharacters.AnyQ(t => t.Tier == 1)) tier++;

            return templateCharacters.Where(t => t.Tier == tier).ToList().GetRandomElement();
        }

        private static CharacterObject? DetermineReplacement(CharacterObject? basic, int tier)
        {
            if (basic == null) return null;

            var replacement = basic;
            while (replacement.Tier < tier && replacement.UpgradeTargets != null && replacement.UpgradeTargets.Length != 0)
                replacement = replacement.UpgradeTargets.GetRandomElement();

            return replacement;
        }

        private static CharacterObject? DetermineBasicTroop(Hero? owner, bool elite)
        {
            CultureObject?[] cultures = { owner?.Clan?.Culture, owner?.Clan?.Kingdom?.Culture, owner?.Culture };

            return cultures.Where(static culture => culture != null)
                           .Select(culture => elite ? culture?.EliteBasicTroop : culture?.BasicTroop)
                           .FirstOrDefault(static basic => basic?.Name != null);
        }

        private bool IsEliteTroop(CharacterObject unit)
        {
            if (_troopTreeCache.TryGetValue(unit.Culture, out var characterObjectList))
                return characterObjectList.Contains(unit);

            characterObjectList = new List<CharacterObject> { unit.Culture.EliteBasicTroop };
            Stack<CharacterObject> characterObjectStack = new();
            characterObjectStack.Push(unit.Culture.EliteBasicTroop);

            while (characterObjectStack.Count > 0)
            {
                var characterObject = characterObjectStack.Pop();
                if (characterObject.UpgradeTargets is not { Length: > 0 }) continue;

                List<CharacterObject> newTargets = characterObject.UpgradeTargets
                                                                  .Where(target => !characterObjectList.Contains(target))
                                                                  .ToList();

                foreach (var target in newTargets)
                {
                    characterObjectList.Add(target);
                    characterObjectStack.Push(target);
                }
            }

            _troopTreeCache.Add(unit.Culture, characterObjectList);
            return characterObjectList.Contains(unit);
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
using MCM.Abstractions.Base.Global;
using RealmsForgotten.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Behaviors
{
    public class HealOnKillMissionBehavior : MissionLogic
    {
        private readonly List<CharacterObject> _characterCache;
        private readonly List<MBGUID> _nullCharacterCache;
        private readonly HashSet<string> healingRaceIds = new HashSet<string> { "bark", "sillok", "nurh", "daimo" };

        public HealOnKillMissionBehavior()
        {
            this._characterCache = new List<CharacterObject>();
            this._nullCharacterCache = new List<MBGUID>();
        }

        public override void OnAgentRemoved(
            Agent affectedAgent,
            Agent affectorAgent,
            AgentState agentState,
            KillingBlow killingBlow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);

            if (affectedAgent == null || affectorAgent == null || affectorAgent.Team == null || affectedAgent.IsMount || (affectedAgent.State != AgentState.Unconscious && affectedAgent.State != AgentState.Killed) || !affectedAgent.IsEnemyOf(affectorAgent))
                return;

            float amount = 0.0f;

            // Check if the affectorAgent's race ID is in the healingRaceIds set
            if (affectorAgent.Character != null && healingRaceIds.Contains(affectorAgent.Character.Race.ToString()))
            {
                amount = 10.0f; // Example: Heal 10 hit points
            }

            if (amount > 0)
            {
                HealAgent(affectorAgent, amount);
                if (GlobalSettings<HoKSettings>.Instance.healHorsesToo && affectorAgent.MountAgent != null)
                {
                    HealAgent(affectorAgent.MountAgent, amount);
                }
            }
        }

        private int HealAgent(Agent a, float amount)
        {
            amount = (amount < 1.0f) ? 1f : (float)(int)amount;
            float health = a.Health;
            float healthLimit = a.HealthLimit;
            a.Health = (a.Health + amount < healthLimit) ? a.Health + amount : healthLimit;
            return (int)(a.Health - health);
        }

        private void DoMedicineSkillup(Agent a, float amount)
        {
            if (!GlobalSettings<HoKSettings>.Instance.enableMedicineSkillGain || a.Character == null)
                return;
            if (a.IsHero)
            {
                this.LookupCharacter(a.Character.Id)?.HeroObject.AddSkillXp(DefaultSkills.Medicine, amount);
            }
            else
            {
                Agent generalAgent = a.Team?.GeneralAgent;
                if (generalAgent?.Character != null)
                {
                    CharacterObject characterObject = this.LookupCharacter(generalAgent.Character.Id);
                    if (characterObject != null)
                    {
                        float num = (float)(a.Team.ActiveAgents.Count * 0.2f);
                        characterObject.HeroObject.AddSkillXp(DefaultSkills.Medicine, amount / num);
                    }
                }
            }
        }

        private CharacterObject LookupCharacter(MBGUID id)
        {
            if (this._nullCharacterCache.Contains(id))
                return null;
            CharacterObject characterObject = this._characterCache.FirstOrDefault(x => x.Id == id);
            if (characterObject == null && Campaign.Current?.Characters != null)
            {
                characterObject = Campaign.Current.Characters.FirstOrDefault(x => x.Id == id);
                this._characterCache.Add(characterObject);
                if (characterObject == null)
                    this._nullCharacterCache.Add(id);
            }
            return characterObject;
        }
    }
}
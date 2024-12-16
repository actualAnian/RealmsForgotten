using RealmsForgotten.RFCustomSettlements;
using SandBox.BoardGames.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace RFCustomSettlements.Quests
{
    public class CustomSettlementQuestData
    {
        public CustomSettlementQuestData(string text, Dictionary<string, int> enemiesToKill)
        {
            EnemiesToKill = enemiesToKill;
            Text = text;
        }
        [SaveableProperty(0)]
        public Dictionary<string, int> EnemiesToKill { get; set; }
        [SaveableProperty(1)]
        public string Text { get; set; }
    }

    public class CustomSettlementQuestSync : CampaignBehaviorBase
    {
        public static void AddNewQuest(string questId, string text, Dictionary<string, int> enemiesToKill)
        {
            CustomSettlementQuestSync behavior = Campaign.Current.GetCampaignBehavior<CustomSettlementQuestSync>();
            behavior.questSaveableData.Add(questId, new(text, enemiesToKill));
        }
        public static void Update(string questId, Dictionary<string, int> enemiesToKill)
        {
            CustomSettlementQuestSync behavior = Campaign.Current.GetCampaignBehavior<CustomSettlementQuestSync>();
            CustomSettlementQuestData oldData = behavior.questSaveableData[questId];
            oldData.EnemiesToKill = enemiesToKill;
        }
        [SaveableField(1)]
        private Dictionary<string, CustomSettlementQuestData> questSaveableData = new();
        public override void RegisterEvents()
        {
            //CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, ReloadQuests);
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                foreach (var quest in CustomSettlementQuest.GetAllQuest())
                    Update(quest.StringId, quest.enemiesToKill);
            }
            dataStore.SyncData< Dictionary<string, CustomSettlementQuestData>>("questSaveableData", ref questSaveableData);
            questSaveableData ??= new();
            if (dataStore.IsLoading)
            {
                foreach (KeyValuePair<string, CustomSettlementQuestData> item in questSaveableData)
                {
                    CustomSettlementQuest? quest = CustomSettlementQuest.GetQuest(item.Key);
                    if (quest == null) continue;
                    quest.LoadData(item.Value.Text, item.Value.EnemiesToKill);
                }
            }
        }
    }
    public class CustomSettlementQuest : QuestBase
    {
        readonly Func<bool> _completeCondition;
        readonly Action _completeConsequence;
        public Dictionary<string, int> enemiesToKill = new();
        private string tasksString;
        [SaveableField(0)]
        JournalLog tasksLog;
        private string _title;
        public static List<CustomSettlementQuest> QuestsListeningToActorRemoved { get; } = new();
        private CustomSettlementQuest(QuestData data) : base(data.QuestId, CharacterObject.PlayerCharacter.HeroObject, CampaignTime.Never, 0)
        {

            tasksString = AnalyseConditions(data);
            _completeCondition = CreateCondition(data.CompleteCondition);
            _completeConsequence = CreateConsequence(data.CompleteConsequence);

            _title = data.QuestLogText;
            //tasksLog = AddLog(new("a"));
            tasksLog = AddDiscreteLog(
                            new TextObject($"{data.QuestLogText}"),
                            new TextObject($"{tasksString}"), 0, 1);
            if (data.CompleteCondition.HasKilledList.Count() > 0 && Mission.Current != null)
            {
                CustomSettlementMissionLogic logic = Mission.Current.GetMissionBehavior<CustomSettlementMissionLogic>();
                if (logic != null) logic.UnitKilled += OnEnemyKilledInCustomSettlement;
            }
            CustomSettlementQuestSync.AddNewQuest(data.QuestId, _title, enemiesToKill);
        }
        public static bool IsQuestActive(string questId)
        {
            foreach (QuestBase? quest in Campaign.Current.QuestManager.Quests)
                if (quest?.StringId == questId) return true;
            return false;
        }
        public static CustomSettlementQuest? GetQuest(string questId)
        {

            foreach (QuestBase? quest in Campaign.Current.QuestManager.Quests)
                if (quest?.StringId == questId) return (quest as CustomSettlementQuest);
            return null;
        }
        public static List<CustomSettlementQuest> GetAllQuest()
        {
            List<CustomSettlementQuest> list = new();
            CustomSettlementQuest? csQuest;
            foreach (QuestBase? quest in Campaign.Current.QuestManager.Quests)
                if ((csQuest = quest as CustomSettlementQuest) != null) list.Add(csQuest);
            return list;
        }

        private string AnalyseConditions(QuestData data)
        {
            StringBuilder sb = new();
            CharacterObject? questGiver = MBObjectManager.Instance.GetObject<CharacterObject>($"{data.QuestGiverId}");
            sb.Append(questGiver != null ? questGiver.StringId : $"Error, npc with id{data.QuestGiverId} not found.");
            sb.AppendLine(" wants you to:");
            CompletedWhen condition = data.CompleteCondition;
            if (condition.InInventoryList?.Count > 0)
            {
                int ItemsNeeded = condition.InInventoryList.Count();
                foreach (KeyValuePair<string, int> item in condition.InInventoryList)
                {
                    string stringId = item.Key;
                    ItemObject? good = MBObjectManager.Instance.GetObject<ItemObject>(stringId);
                    if (good == null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Error, no item with id {stringId}"));
                        continue;
                    }
                    sb.AppendLine($" AND give {item.Value} {good.Name}");
                    if (ItemsNeeded == 0) break;
                }
            }
            if (condition.HasPrisonersList?.Count > 0)
            {
                foreach (KeyValuePair<string, int> item in condition.HasPrisonersList)
                {
                    CharacterObject? troop = MBObjectManager.Instance.GetObject<CharacterObject>(item.Key);
                    if (troop == null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Error, no troop with id {item.Key}"));
                        continue;
                    }
                    sb.AppendLine($" AND give {item.Value} prisoners: {troop.Name}");
                }
            }
            if (condition.HasKilledList?.Count > 0)
            {
                foreach (KeyValuePair<string, int> killed in condition.HasKilledList)
                {
                    CharacterObject? troop = MBObjectManager.Instance.GetObject<CharacterObject>(killed.Key);
                    if (troop == null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Error, no troop with id {killed.Key}"));
                        continue;
                    }
                    sb.AppendLine($" AND kill {killed.Value} enemies: {troop.Name}");
                    enemiesToKill.Add(killed.Key, killed.Value);
                }
            }
            if (enemiesToKill.Count > 0)
                QuestsListeningToActorRemoved.Add(this);
            string tasks = sb.ToString();
            int index = tasks.IndexOf("AND");
            if (index != -1)
            {
                tasks = tasks.Remove(index, 3);
            }
            return tasks;
        }
        public static bool Start(string questId)
        {
            if (!CustomSettlementsCampaignBehavior.AllQuests.ContainsKey(questId))
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error, no quest with id {questId}"));
                return false;
            }
            if (IsQuestActive(questId))
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error, the quest with id {questId} is already active."));
                return false;
            }
            else
            {
                var quest = new CustomSettlementQuest(CustomSettlementsCampaignBehavior.AllQuests[questId]);
                quest.StartQuest();
                return true;
            }
        }
        public override TextObject Title => new(_title);
        public override bool IsSpecialQuest => true;

        public override bool IsRemainingTimeHidden => true;

        protected override void HourlyTick() {}

        protected override void InitializeQuestOnGameLoad() {}
        public bool Evaluate()
        {
            return _completeCondition();
        }
        protected override void SetDialogs() {}
        public void CompleteQuest()
        {
            tasksLog.UpdateCurrentProgress(1);
            _completeConsequence();
            CompleteQuestWithSuccess();
        }
        public bool EvaluateCompleteConditions()
        {
            return _completeCondition();
        }
        public void OnEnemyKilledInCustomSettlement(string killedId)
        {
            if (enemiesToKill.ContainsKey(killedId))
            {
                enemiesToKill[killedId] -= 1;
                if (enemiesToKill[killedId] <= 0) enemiesToKill.Remove(killedId);
            }
            if (enemiesToKill.Count() <= 0) CustomSettlementQuest.QuestsListeningToActorRemoved.Remove(this);
        }
        internal static void SubscribeEligibleQuests(CustomSettlementMissionLogic customSettlementMissionLogic)
        {
            foreach (CustomSettlementQuest quest in CustomSettlementQuest.QuestsListeningToActorRemoved)
            {
                customSettlementMissionLogic.UnitKilled += quest.OnEnemyKilledInCustomSettlement;
            }
        }
        private Action CreateConsequence(QuestCompleteConsequence consequence)
        {
            return () =>
            {
                if (consequence.RemoveItemList?.Count > 0 || consequence.AddItemList?.Count > 0)
                {
                    int allItems = MobileParty.MainParty.ItemRoster.Count;
                    for (int i = 0; i < allItems; i++)
                    {
                        ItemRosterElement item = MobileParty.MainParty.ItemRoster[i];
                        string stringId = item.EquipmentElement.ToString();
                        if (consequence.RemoveItemList != null && consequence.RemoveItemList.ContainsKey(stringId))
                        {
                            item.Amount -= consequence.RemoveItemList[stringId];
                        }
                        if (consequence.AddItemList != null && consequence.AddItemList.ContainsKey(stringId))
                        {
                            item.Amount += consequence.AddItemList[stringId];
                        }
                    }
                }
                if (consequence.RemovePrisonersList != null)
                {
                    foreach (KeyValuePair<string, int> prisoner in consequence.RemovePrisonersList)
                    {
                        CharacterObject character = MBObjectManager.Instance.GetObject<CharacterObject>(prisoner.Key);
                        try
                        {
                            MobileParty.MainParty.PrisonRoster.RemoveTroop(character, prisoner.Value);
                        }
                        catch (Exception)
                        {
                            InformationManager.DisplayMessage(new InformationMessage($"Error completing the quest, party has not enough prisoners with id {prisoner.Key}"));
                        }
                    }

                }
                if (consequence.RemoveTroopList != null)
                {
                    foreach (KeyValuePair<string, int> troop in consequence.RemoveTroopList)
                    {
                        CharacterObject character = MBObjectManager.Instance.GetObject<CharacterObject>(troop.Key);
                        try
                        {
                            MobileParty.MainParty.MemberRoster.RemoveTroop(character, troop.Value);
                        }
                        catch (Exception)
                        {
                            InformationManager.DisplayMessage(new InformationMessage($"Error completing the quest, party has not enough troops with id {troop.Key}"));
                        }
                    }
                }
                if (consequence.AddTroopList != null)
                {

                    foreach (KeyValuePair<string, int> troop in consequence.AddTroopList)
                    {
                        CharacterObject character = MBObjectManager.Instance.GetObject<CharacterObject>(troop.Key);
                        if (character == null) InformationManager.DisplayMessage(new InformationMessage($"Error completing the quest, could not find a troop with id {troop.Key}"));
                        else  MobileParty.MainParty.MemberRoster.AddToCounts(character, troop.Value);
                    }
                }
                Clan.PlayerClan.AddRenown(consequence.RenownAmount);
                MobileParty.MainParty.PartyTradeGold += consequence.ReceiveGoldAmount;
                MobileParty.MainParty.PartyTradeGold -= consequence.LoseGoldAmount;
            };
        }

        private Func<bool> CreateCondition(CompletedWhen condition)
        {
            return () =>
            {
                if (condition.InInventoryList?.Count > 0)
                {
                    int ItemsNeeded = condition.InInventoryList.Count();
                    foreach (ItemRosterElement item in MobileParty.MainParty.ItemRoster)
                    {
                        string stringId = item.EquipmentElement.ToString();
                        if (condition.InInventoryList.ContainsKey(stringId)
                        && condition.InInventoryList[stringId] >= item.Amount)
                        {
                            ItemsNeeded -= 1;
                            ItemObject? good = MBObjectManager.Instance.GetObject<ItemObject>(stringId);
                            if (good == null)
                            {
                                InformationManager.DisplayMessage(new InformationMessage($"Error, no item with id {stringId}"));
                                continue;
                            }
                            tasksString.Add($"AND give {item.Amount} {good.Name}");
                            if (ItemsNeeded == 0) break;
                        }
                    }
                    if (ItemsNeeded > 0) return false;
                }
                if (condition.HasPrisonersList?.Count > 0)
                {
                    int PrisonerTypesNeeded = condition.HasPrisonersList.Count();
                    using List<TroopRosterElement>.Enumerator enumerator = MobileParty.MainParty.PrisonRoster.GetTroopRoster().GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        if (condition.HasPrisonersList.ContainsKey(enumerator.Current.Character.StringId)
                            && enumerator.Current.Number >= condition.HasPrisonersList[enumerator.Current.Character.StringId])
                        {
                            PrisonerTypesNeeded -= 1;
                            CharacterObject? troop = MBObjectManager.Instance.GetObject<CharacterObject>(enumerator.Current.Character.StringId);
                            if (troop == null)
                            {
                                InformationManager.DisplayMessage(new InformationMessage($"Error, no troop with id {enumerator.Current.Character.StringId}"));
                                continue;
                            }
                            tasksString.Add($"AND give {condition.HasPrisonersList[troop.StringId]} prisoners: {troop.Name}");
                            if (PrisonerTypesNeeded == 0) break;
                        }
                    }
                    if (PrisonerTypesNeeded > 0) return false;

                }
                if (enemiesToKill.Count() != 0)
                    return false;
                return true;
            };
        }

        internal void LoadData(string item2, Dictionary<string, int> tasks)
        {
            _title = CustomSettlementsCampaignBehavior.AllQuests[StringId].QuestLogText;
            tasksString = item2;
            enemiesToKill = tasks;
            //this.StartQuest();
            if (tasks.Count() != 0) QuestsListeningToActorRemoved.Add(this);
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade
{
    public class KingsguardSaveDataBehavior : CampaignBehaviorBase
    {
        private List<Hero> selectedKingsguard = new List<Hero>();

        public bool IsKingsguard(Hero hero) => selectedKingsguard.Contains(hero);

        public void AddKingsguard(Hero hero)
        {
            if (!selectedKingsguard.Contains(hero))
                selectedKingsguard.Add(hero);
        }

        public void RemoveKingsguard(Hero hero) => selectedKingsguard.Remove(hero);

        public IEnumerable<Hero> GetKingsguard() => selectedKingsguard.AsEnumerable();

        public override void RegisterEvents()
        {
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("selectedKingsguard", ref selectedKingsguard);
        }

        internal class KingsguardSaveDataTypeDefiner : SaveableTypeDefiner
        {
            public KingsguardSaveDataTypeDefiner() : base(91358462) { }

            protected override void DefineContainerDefinitions()
            {
                ConstructContainerDefinition(typeof(List<Hero>));
            }
        }
    }
    public class ProtectTheRulerAgentBehavior : BehaviorComponent
    {
        public bool IsDisabled { get; set; } = false;
        public Agent TheRulerAgent { get; set; }

        public override float NavmeshlessTargetPositionPenalty => 1f;

        public ProtectTheRulerAgentBehavior(Formation formation) : base(formation) { }

        public bool IsTheRulerAlive() => TheRulerAgent != null && TheRulerAgent.IsActive();

        public override void TickOccasionally()
        {
            if (!IsTheRulerAlive())
                return;

            Formation.SetMovementOrder(MovementOrder.MovementOrderFollow(TheRulerAgent));
            CurrentOrder = MovementOrder.MovementOrderFollow(TheRulerAgent);
        }

        protected override float GetAiWeight()
        {
            return IsTheRulerAlive() && !IsDisabled ? 100f : 0f;
        }

        protected override void OnBehaviorActivatedAux()
        {
            IsDisabled = false;
        }
    }
    public class KingsguardMissionBehavior : MissionLogic
    {
        private readonly KingsguardSaveDataBehavior _saveDataBehavior;
        private Formation _kingsguardFormation;

        public KingsguardMissionBehavior()
        {
            _saveDataBehavior = Campaign.Current?.GetCampaignBehavior<KingsguardSaveDataBehavior>();
        }

        public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            base.OnMissionModeChange(oldMissionMode, atStart);

            if (Mission.Mode == MissionMode.Battle)
            {
                CreatePlayerKingsguard();
            }
        }
        private void CreatePlayerKingsguard()
        {
            var playerTeam = Mission.PlayerTeam;
            if (playerTeam == null || Mission.MainAgent == null)
                return;

            var selectedHeroes = _saveDataBehavior?.GetKingsguard();
            if (selectedHeroes == null || !selectedHeroes.Any())
                return;

            var agents = playerTeam.ActiveAgents
                .Where(agent => agent != Mission.MainAgent && agent.Character != null && agent.IsHuman && agent.IsHero);

            var kingsguardAgents = agents
                .Where(agent => IsKingsguardHero(agent))
                .Take(7)
                .ToList();

            if (kingsguardAgents.Count == 0)
                return;

            _kingsguardFormation = playerTeam.GetFormation(FormationClass.NumberOfDefaultFormations);

            _kingsguardFormation.SetMovementOrder(MovementOrder.MovementOrderMove(Mission.MainAgent.GetWorldPosition()));
            _kingsguardFormation.SetControlledByAI(true);

            foreach (var agent in kingsguardAgents)
            {
                agent.Formation = _kingsguardFormation;
            }

            var protectTheRulerBehavior = new ProtectTheRulerAgentBehavior(_kingsguardFormation)
            {
                TheRulerAgent = Mission.MainAgent
            };
            _kingsguardFormation.AI.ResetBehaviorWeights();
            _kingsguardFormation.AI.AddAiBehavior(protectTheRulerBehavior);
            _kingsguardFormation.AI.SetBehaviorWeight<ProtectTheRulerAgentBehavior>(100f);
        }

        private bool IsKingsguardHero(Agent agent)
        {
            if (_saveDataBehavior == null)
                return false;

            var characterObject = agent.Character as CharacterObject;
            if (characterObject != null && characterObject.HeroObject != null)
            {
                return _saveDataBehavior.IsKingsguard(characterObject.HeroObject);
            }

            return false;
        }
    }

    [PrefabExtension("EncyclopediaHeroPage", "descendant::RichTextWidget[@Text='@InformationText']")]
    internal class HeroPagePrefabExtension : PrefabExtensionInsertPatch
    {
        private IEnumerable<XmlNode> _nodes;

        public override InsertType Type => InsertType.Append;

        [PrefabExtensionInsertPatch.PrefabExtensionXmlNodes]
        public IEnumerable<XmlNode> GetNodes()
        {
            if (_nodes == null)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(@"
            <DiscardedRoot>
                <ListPanel HorizontalAlignment='Center' HeightSizePolicy='CoverChildren' WidthSizePolicy='CoverChildren' MarginTop='10'>
                    <Children>
                        <Widget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='227' SuggestedHeight='40' MarginLeft='5' MarginRight='5' HorizontalAlignment='Left'>
                            <Children>
                                <ButtonWidget DoNotPassEventsToChildren='true' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='ButtonBrush2' UpdateChildrenStates='true' Command.Click='ToggleKingsguard' IsVisible='true' IsEnabled='@IsAddToKingsguardEnabled'>
                                    <Children>
                                        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='Kingdom.GeneralButtons.Text' Text='@ToggleKingsguardActionName' />
                                    </Children>
                                </ButtonWidget>
                                <HintWidget DataSource='{ToggleKingsguardHint}' DoNotAcceptEvents='true' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Command.HoverBegin='ExecuteBeginHint' Command.HoverEnd='ExecuteEndHint' IsEnabled='false' />
                            </Children>
                        </Widget>
                    </Children>
                </ListPanel>
            </DiscardedRoot>");
                _nodes = doc.DocumentElement.ChildNodes.Cast<XmlNode>();
            }
            return _nodes;
        }
    }

    [ViewModelMixin("RefreshValues")]
    internal class HeroPageVMMixin : BaseViewModelMixin<EncyclopediaHeroPageVM>
    {
        private readonly Hero _hero;
        private readonly KingsguardSaveDataBehavior _saveDataBehavior;
        private readonly TextObject _hintText = new TextObject("{=ADOD_KingsguardHint}{?ADDING}Add{?}Remove{\\?} {HERONAME} {?ADDING}to{?}from{\\?} your Kingsguard");

        private bool _isAddToKingsguardEnabled;
        private HintViewModel _toggleKingsguardHint;

        public HeroPageVMMixin(EncyclopediaHeroPageVM vm) : base(vm)
        {
            _hero = vm.Obj as Hero;
            _saveDataBehavior = Campaign.Current?.GetCampaignBehavior<KingsguardSaveDataBehavior>();

            if (_hero == null || _saveDataBehavior == null)
            {
                return;
            }

            vm.RefreshValues();
            OnRefresh();
        }

        public override void OnRefresh()
        {
            if (_hero == null)
            {
                return;
            }

            UpdateIsAddToKingsguardEnabled();
            OnPropertyChanged(nameof(CanBeKingsguard));
            OnPropertyChanged(nameof(IsAddToKingsguardEnabled));
            OnPropertyChanged(nameof(ToggleKingsguardActionName));
            OnPropertyChanged(nameof(ToggleKingsguardHint));
        }

        private void UpdateIsAddToKingsguardEnabled()
        {
            IsAddToKingsguardEnabled = CanAddToKingsguard(out var exception);
            if (!IsAddToKingsguardEnabled)
            {
                ToggleKingsguardHint = new HintViewModel(exception);
            }
            else
            {
                var hintText = new TextObject("{=ADOD_KingsguardHint}{?ADDING}Add{?}Remove{\\?} {HERONAME} {?ADDING}to{?}from{\\?} your Kingsguard");
                ToggleKingsguardHint = new HintViewModel(
                    hintText.SetTextVariable("ADDING", _saveDataBehavior.IsKingsguard(_hero) ? 0 : 1)
                            .SetTextVariable("HERONAME", _hero.Name.ToString()));
            }
        }
        private bool CanAddToKingsguard(out TextObject exception)
        {
            exception = TextObject.Empty;

            if (!(Hero.MainHero.MapFaction is Kingdom playerKingdom) || playerKingdom.Leader != Hero.MainHero)
            {
                exception = new TextObject("You must be a ruler to have a Kingsguard.");
                return false;
            }

            if (_saveDataBehavior.GetKingsguard().Count() >= 7 && !_saveDataBehavior.IsKingsguard(_hero))
            {
                exception = new TextObject("You already have 7 Kingsguard.");
                return false;
            }

            if (!CanBeKingsguard)
            {
                if (_hero == Hero.MainHero)
                    exception = new TextObject("You cannot add yourself to your own Kingsguard.");
                else if (!_hero.IsAlive)
                    exception = new TextObject("The dead cannot serve as Kingsguard.");
                else if (_hero.IsChild)
                    exception = new TextObject("Children cannot be Kingsguard");
                else if (!Clan.PlayerClan.Heroes.Contains(_hero))
                    exception = new TextObject("You can only add members of your House to your Kingsguard.");
                else
                    exception = new TextObject("You cannot add this person to your Kingsguard.");
                return false;
            }

            return true;
        }

        [DataSourceMethod]
        public void ToggleKingsguard()
        {
            if (_saveDataBehavior == null || !IsAddToKingsguardEnabled)
                return;

            if (_saveDataBehavior.IsKingsguard(_hero))
                _saveDataBehavior.RemoveKingsguard(_hero);
            else
                _saveDataBehavior.AddKingsguard(_hero);

            OnRefresh();
        }

        [DataSourceProperty]
        public bool CanBeKingsguard
        {
            get
            {
                return _hero != Hero.MainHero && Clan.PlayerClan.Heroes.Contains(_hero) && _hero.IsAlive && !_hero.IsChild;
            }
        }

        [DataSourceProperty]
        public bool IsAddToKingsguardEnabled
        {
            get { return _isAddToKingsguardEnabled; }
            set
            {
                if (value != _isAddToKingsguardEnabled)
                {
                    _isAddToKingsguardEnabled = value;
                    OnPropertyChangedWithValue(value, nameof(IsAddToKingsguardEnabled));
                }
            }
        }

        [DataSourceProperty]
        public string ToggleKingsguardActionName
        {
            get
            {
                return _saveDataBehavior.IsKingsguard(_hero)
                    ? new TextObject("Remove from Kingsguard").ToString()
                    : new TextObject("Add to Kingsguard").ToString();
            }
        }

        [DataSourceProperty]
        public HintViewModel ToggleKingsguardHint
        {
            get { return _toggleKingsguardHint; }
            set
            {
                if (value != _toggleKingsguardHint)
                {
                    _toggleKingsguardHint = value;
                    OnPropertyChangedWithValue(value, nameof(ToggleKingsguardHint));
                }
            }
        }
    }
    [HarmonyPatch(typeof(Agent), "Formation", MethodType.Setter)]
    internal class FormationPatch
    {
        private static bool Prefix(Agent __instance, Formation value)
        {
            if (__instance?.Formation?.AI == null || !__instance.IsActive() || Mission.Current == null || Mission.Current.IsMissionEnding)
                return true;

            var behavior = __instance.Formation.AI.GetBehavior<ProtectTheRulerAgentBehavior>();

            return behavior == null || behavior.IsDisabled || !behavior.IsTheRulerAlive();
        }
    }
}

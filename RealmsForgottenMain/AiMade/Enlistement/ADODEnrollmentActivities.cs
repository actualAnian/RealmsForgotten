using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Helpers;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;


namespace RealmsForgotten.AiMade.Enlistement
{
    public class ADODEnrollmentActivities 
    {
        private readonly Dictionary<string, List<SkillObject>> _activitySets;
        private readonly List<string> _careers;

        private const string Mage = "tremerid";
        private const string Giant = "Ixtlaliel";
        private const string Dugrast = "dwarf_kingdom";
        private const string SouthRealm = "empire_s";
        private const string NorthRealm = "empire";
        private const string WestRealm = "empire_w";
        private const string Dreadlands = "sturgia";
        private const string Athas = "aserai";
        private const string Nasoria = "vlandia";
        private const string Elvean = "battania";
        private const string Allkhuur = "khuzait";
        public ADODEnrollmentActivities()
        {
            _activitySets = new Dictionary<string, List<SkillObject>>();
            _careers = new List<string>
            {
                "Footman",
                "Archer",
                "Knight",
                "Mason",
                "Maester"
            };

            foreach (var career in _careers)
            {
                if (career == "Footman")
                {
                    _activitySets.Add(career, new List<SkillObject>
                    {
                        DefaultSkills.OneHanded,
                        DefaultSkills.TwoHanded,
                        DefaultSkills.Athletics,
                        DefaultSkills.Polearm,
                        DefaultSkills.Tactics
                    });
                }

                if (career == "Archer")
                {
                    _activitySets.Add(career, new List<SkillObject>
                    {
                        DefaultSkills.Bow,
                        DefaultSkills.Crossbow,
                        DefaultSkills.Athletics,
                        DefaultSkills.Scouting,
                        DefaultSkills.Tactics
                    });
                }

                if (career == "Knight")
                {
                    _activitySets.Add(career, new List<SkillObject>
                    {
                        DefaultSkills.Riding,
                        DefaultSkills.OneHanded,
                        DefaultSkills.Polearm,
                        DefaultSkills.Leadership,
                        DefaultSkills.Tactics
                    });
                }

                if (career == "Mason")
                {
                    _activitySets.Add(career, new List<SkillObject>
                    {
                        DefaultSkills.Engineering,
                        DefaultSkills.Trade,
                        DefaultSkills.Steward,
                        DefaultSkills.Crafting,
                        DefaultSkills.Tactics
                    });
                }

                if (career == "Maester")
                {
                    _activitySets.Add(career, new List<SkillObject>
                    {
                        DefaultSkills.Medicine,
                        DefaultSkills.Steward,
                        DefaultSkills.Charm,
                        DefaultSkills.Athletics,
                        DefaultSkills.Tactics
                    });
                }
            }
        }

        public List<SkillObject> GetEnrollmentActivities(string career)
        {
            if (_activitySets.TryGetValue(career, out var activities))
            {
                return activities;
            }

            return new List<SkillObject>();
        }

        public List<string> GetCareers()
        {
            return _careers;
        }
    }

    public class ADODEnrollmentCampaignBehavior : CampaignBehaviorBase
    {
        private const float MinimumServeDays = 20f;
        private const float LordshipDays = 100f;
        private const float RatioPartyAgainstEnemyStrength = 0.30f;

        private float _durationInDays;
        private bool _enrollmentEnrolled;
        private Hero _enrollmentEnrollingLord;
        private bool _enrollmentEnrollingLordIsAttacking;
        private bool _enrollmentLordIsFightingWithoutPlayer;

        private readonly bool _debugSkipBattles = false;
        private bool _pauseModeToggle;
        private int _manuallyFoughtBattles;

        private bool _startBattle;
        private bool _siegeBattleMissionStarted;

        private bool _enrollmentWaitMenuShown;

        private float _entryServiceTimeStamp;

        private SkillObject _currentTrainedSkill;
        private int _currentActivityIndex;

        private bool _enrollInquiryDeclined;

        private ADODEnrollmentActivities _activities;

        private string _playerCareer;

        private bool _hasOfferedLordship = false;

        public float DurationInDays => _durationInDays;

        public int ManuallyFoughtBattles => _manuallyFoughtBattles;

        public bool IsEnrolled()
        {
            return _enrollmentEnrolled;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, Initialize);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, EnrollingLordPartyEntersSettlement);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnPartyLeavesSettlement);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, ControlPlayerLoot);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, MenuOpened);
            CampaignEvents.GameMenuOptionSelectedEvent.AddNonSerializedListener(this, ContinueTimeAfterLeftSettlementWhileEnrolled);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, DailyRenownGain);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, SkillGain);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, LeaveKingdomEvent);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
        }

        private void Initialize(CampaignGameStarter campaignGameStarter)
        {
            _activities = new ADODEnrollmentActivities();

            InitializeDialogs(campaignGameStarter);
            SetupSoldierMenu(campaignGameStarter);
            SetupBattleMenu(campaignGameStarter);
        }

        private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase attackingParty)
        {
            if (_enrollmentEnrolled)
            {
                if (destroyedParty.LeaderHero == _enrollmentEnrollingLord || destroyedParty == MobileParty.MainParty)
                {
                    LeaveLordPartyAction();
                }
            }
        }

        private void LeaveKingdomEvent(Clan clan, Kingdom kingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail arg4, bool arg5)
        {
            if (clan == Clan.PlayerClan && IsEnrolled())
            {
                LeaveLordPartyAction();
            }
        }

        private void SkillGain()
        {
            if (_enrollmentEnrolled)
            {
                if (_currentTrainedSkill == null)
                {
                    if (string.IsNullOrEmpty(_playerCareer))
                    {
                        _playerCareer = DeterminePlayerCareer();
                    }
                    var activities = _activities.GetEnrollmentActivities(_playerCareer);
                    if (activities.Count > 0)
                    {
                        _currentTrainedSkill = activities[0];
                        _currentActivityIndex = 0;
                    }
                }

                if (_currentTrainedSkill != null && Hero.MainHero.IsHealthFull())
                {
                    Hero.MainHero.AddSkillXp(_currentTrainedSkill, 25);
                }
            }
        }

        private void DailyRenownGain()
        {
            var gain = 1;
            var clanTier = Hero.MainHero.Clan.Tier;
            gain += clanTier;

            Hero.MainHero.Clan.AddRenown(gain);
        }

        private void ContinueTimeAfterLeftSettlementWhileEnrolled(GameMenuOption obj)
        {
            if (_enrollmentEnrolled && obj.IdString == "town_leave")
            {
                GameMenu.ActivateGameMenu("enrollment_menu");
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
            }
        }

        private float GetEnrollingLordEventStrengthRatio(MapEvent mapEvent)
        {
            var t = mapEvent.GetMapEventSide(BattleSideEnum.Attacker);
            BattleSideEnum side;
            if (t.Parties.Any(x => x.Party == _enrollmentEnrollingLord.PartyBelongedTo.Party))
            {
                side = BattleSideEnum.Attacker;
            }
            else
            {
                side = BattleSideEnum.Defender;
            }
            mapEvent.GetStrengthsRelativeToParty(side, out float enrollingLordStrength, out float enemyStrength);

            if (enemyStrength > 0)
            {
                return enrollingLordStrength / enemyStrength;
            }

            return 1;
        }

        private void MenuOpened(MenuCallbackArgs obj)
        {
            if (_startBattle && obj.MenuContext.GameMenu.StringId == "encounter" && !_debugSkipBattles)
            {
                _startBattle = false;

                MenuHelper.EncounterAttackConsequence(obj);
            }
            if (_debugSkipBattles && _enrollmentEnrollingLordIsAttacking)
            {
                _startBattle = false;
            }
        }

        private void LeaveEnrollingParty(string menuToReturn, bool desertion = false)
        {
            desertion = desertion || _durationInDays < MinimumServeDays;

            if (desertion)
            {
                var damage = new TextObject("This will harm your relations with the entire faction.");
                GameTexts.SetVariable("ENROLLMENT_DESERT_TEXT", damage);
            }
            else
            {
                GameTexts.SetVariable("ENROLLMENT_DESERT_TEXT", "");
            }

            InformationManager.ShowInquiry(new InquiryData(
                "Abandon Party",
                $"Are you sure you want to abandon the party? {(desertion ? "This will harm your relations with the entire faction." : "")}",
                true,
                true,
                "Yes",
                "No",
                delegate
                {
                    if (desertion)
                    {
                        ChangeCrimeRatingAction.Apply(_enrollmentEnrollingLord.MapFaction, 55f);
                        foreach (Clan clan in _enrollmentEnrollingLord.Clan.Kingdom.Clans)
                        {
                            if (!clan.IsUnderMercenaryService)
                            {
                                ChangeRelationAction.ApplyPlayerRelation(clan.Leader, -10);
                            }
                        }
                    }
                    LeaveLordPartyAction();
                    GameMenu.ExitToLast();
                },
                delegate
                {
                    GameMenu.ActivateGameMenu(menuToReturn);
                }
            ));
        }

        private void InitializeDialogs(CampaignGameStarter campaignGameStarter)
        {
            // Quit Enrollment Dialogues
            campaignGameStarter.AddPlayerLine("convincelord_quit", "lord_talk_speak_diplomacy_2", "enrollparty_quit_sure", "I would like to quit my service.", QuitCondition, null);
            campaignGameStarter.AddDialogLine("enrollparty_quit_sure", "enrollparty_quit_sure", "enrollparty_quit_choice", "Are you sure?", null, null);
            campaignGameStarter.AddPlayerLine("enrollparty_quit_choice_yes", "enrollparty_quit_choice", "enrollparty_quit", "Yes, I want to leave.", null, LeaveLordPartyAction);
            campaignGameStarter.AddPlayerLine("enrollparty_quit_choice_no", "enrollparty_quit_choice", "lord_pretalk", "I have to think about this.", null, null);
            campaignGameStarter.AddDialogLine("enrollment_prompt", "enrollment_prompt", "end", "Are you sure you want to quit your service?", null, null);

            // General Enrollment Dialogues
            campaignGameStarter.AddPlayerLine("convincelord_join", "lord_talk_speak_diplomacy_2", "enrollparty_explain", "I would like to join your retinue, my lord.", () => SanityCheck() && !IsEnrolled() && ADODEnrollmentHelpers.SoldierServiceConditions(), null);
            campaignGameStarter.AddDialogLine("enrollparty_explain", "enrollparty_explain", "enrollment_decide_player", "{ENROLLMENT_EXPLAIN_TEXT}", EnrollmentExplainCondition, null, 200);
            campaignGameStarter.AddPlayerLine("enrollment_decide_player_yes", "enrollment_decide_player", "enrollment_confirm", "Yes, my lord.", ADODEnrollmentHelpers.SoldierServiceConditions, () => DisplayPrompt(EnrollPlayer));
            campaignGameStarter.AddDialogLine("enrollment_confirm", "enrollment_confirm", "enrollment_final", "{ENROLLMENT_CONFIRM_TEXT}", EnrollmentConfirmCondition, null);
            campaignGameStarter.AddPlayerLine("enrollment_final_yes", "enrollment_final", "enrollment_final2", "Yes, my lord!", null, null);
            campaignGameStarter.AddDialogLine("enrollment_final_response", "enrollment_final2", "end", "{ENROLLMENT_FINAL_TEXT}", EnrollmentFinalCondition, null);
            campaignGameStarter.AddPlayerLine("enrollment_decide_player_no", "enrollment_decide_player", "lord_pretalk", "Hm. I need some time to think, if you don't mind.", null, null);
            campaignGameStarter.AddDialogLine("enrollment_decision", "enrollment_decision", "end", "{ENROLLMENT_DECISION_TEXT}", EnrollmentDecisionCondition, null);

            // Dialogues for Offering Lordship
            campaignGameStarter.AddDialogLine("enrollment_full_membership_offer", "lord_start", "enrollment_full_membership_response", "{LORDSHIP_OFFER_TEXT}", LordshipOfferCondition, null);
            campaignGameStarter.AddPlayerLine("enrollment_full_membership_accept", "enrollment_full_membership_response", "enrollment_full_membership_accept_response", "I am honored, my lord. I accept.", null, AcceptLordship);
            campaignGameStarter.AddPlayerLine("enrollment_full_membership_decline", "enrollment_full_membership_response", "enrollment_full_membership_declined", "I must decline, my lord.", null, DeclineLordship);
            campaignGameStarter.AddDialogLine("enrollment_full_membership_accept_response", "enrollment_full_membership_accept_response", "close_window", "Excellent. Welcome as a full member of our Realm.", null, null);
            campaignGameStarter.AddDialogLine("enrollment_full_membership_declined_response", "enrollment_full_membership_declined", "close_window", "I see. Perhaps another time.", null, null);
        }

        private bool EnrollmentExplainCondition()
        {
            var lordHero = Campaign.Current.ConversationManager.OneToOneConversationHero;
            if (lordHero == null) return false;

            var lordKingdom = lordHero.MapFaction as Kingdom;
            string kingdomId = lordKingdom != null ? lordKingdom.StringId : "default";

            string enrollmentExplainText;

            switch (kingdomId)
            {
                case "mage":
                    // The Stormlands
                    enrollmentExplainText = "The Stormlands are beset by enemies and storms alike. What is our House but a word, if we do not stand for it? Will you fight under the stag's banner?";
                    break;

                case "giants":
                    // The Riverlands
                    enrollmentExplainText = "Our lands have long been a battlefield for greater powers. The blood of the river runs deep, and now it calls to you. Will you fight for our homes?";
                    break;

                case "dwarf_kingdom":
                    // The Iron Throne (Blacks)
                    enrollmentExplainText = "We fight for Queen Rhaenyra, the realms rightful ruler. What is this brief, mortal life, if not the pursuit of legacy? Will you stand with the Blacks?";
                    break;

                case "empire":
                    // The Vale
                    enrollmentExplainText = "The Vale stands proud and unyielding, as immovable as the mountains themselves. Honor is all, and our honor is at stake. Will you fight with us?";
                    break;

                case "empire_w":
                    // The Reach
                    enrollmentExplainText = "The green fields of the Reach are at risk. The price of loyalty is not always gold. Will you join us to protect our lands from Rhaenyra and her dragons?";
                    break;

                case "empire_s":
                    // The Iron Throne (Greens)
                    enrollmentExplainText = "King Aegon II, the true ruler, calls upon his loyal subjects. A king should never sit easy while his realm is divided. Do you swear fealty to him?";
                    break;

                case "sturgia":
                    // The Iron Islands
                    enrollmentExplainText = "We are Ironborn, feared raiders of the seas. What is dead may never die, but rises again, harder and stronger. Will you sail with us?";
                    break;

                case "aserai":
                    // Dorne
                    enrollmentExplainText = "Dorne stands unbowed, unbent, unbroken. A Dornishman never forgets. Will you fight with us to defend our sands from those who dare threaten us?";
                    break;

                case "vlandia":
                    // The Westerlands
                    enrollmentExplainText = "The gold of the Westerlands must be defended. The more you give a king, the more he needs. Will you serve, and share in our wealth?";
                    break;

                case "battania":
                    // The North
                    enrollmentExplainText = "Winter is coming, and the North remembers. The lone wolf dies, but the pack survives. Will you join us in our duty to defend the North?";
                    break;

                default:
                    // Default dialogues
                    enrollmentExplainText = "Joining my retinue is no small matter. You will serve me faithfully, follow my orders, and fight by my side. The road ahead is fraught with danger, but loyalty will be rewarded. Do you have the courage to pledge yourself to my cause?";
                    break;
            }

            GameTexts.SetVariable("ENROLLMENT_EXPLAIN_TEXT", enrollmentExplainText);
            return true;
        }

        private bool EnrollmentConfirmCondition()
        {
            var lordHero = Campaign.Current.ConversationManager.OneToOneConversationHero;
            if (lordHero == null) return false;

            var lordKingdom = lordHero.MapFaction as Kingdom;
            string kingdomId = lordKingdom != null ? lordKingdom.StringId : "default";

            string enrollmentConfirmText;

            switch (kingdomId)
            {
                case "mage":
                    enrollmentConfirmText = "Then swear it by the old gods and the new.";
                    break;

                case "giant":
                    enrollmentConfirmText = "Your sword strengthens our cause.";
                    break;

                case "dwarf_kingdom":
                    enrollmentConfirmText = "Then pledge fealty to Queen Rhaenyra Targaryen.";
                    break;

                case "empire":
                    enrollmentConfirmText = "Then honor us with your sword.";
                    break;

                case "empire_w":
                    enrollmentConfirmText = "Then fight with us, and protect the Reach.";
                    break;

                case "empire_s":
                    enrollmentConfirmText = "Then swear it: For King Aegon II, and the Realm.";
                    break;

                case "sturgia":
                    enrollmentConfirmText = "Then let us plunder the seas together.";
                    break;

                case "aserai":
                    enrollmentConfirmText = "Your loyalty honors Dorne.";
                    break;

                case "vlandia":
                    enrollmentConfirmText = "Wise choice. Let us begin.";
                    break;

                case "battania":
                    enrollmentConfirmText = "Your loyalty will not be forgotten.";
                    break;

                default:
                    enrollmentConfirmText = "Sign this and we'll be off.";
                    break;
            }

            GameTexts.SetVariable("ENROLLMENT_CONFIRM_TEXT", enrollmentConfirmText);
            return true;
        }

        private bool EnrollmentFinalCondition()
        {
            var lordHero = Campaign.Current.ConversationManager.OneToOneConversationHero;
            if (lordHero == null) return false;

            var lordKingdom = lordHero.MapFaction as Kingdom;
            string kingdomId = lordKingdom != null ? lordKingdom.StringId : "default";

            string enrollmentFinalText;

            switch (kingdomId)
            {
                case "mage":
                    enrollmentFinalText = "Welcome. We march at dawn. 'The storms may come, but we shall endure.'";
                    break;

                case "giant":
                    enrollmentFinalText = "Welcome to the Riverlands. We defend what is ours.";
                    break;

                case "dwarf_kingdom":
                    enrollmentFinalText = "Welcome to our ranks. Fire and Blood will see us through.";
                    break;

                case "empire":
                    enrollmentFinalText = "Welcome to the Knights of the Vale. As High as Honor.";
                    break;

                case "empire_w":
                    enrollmentFinalText = "Welcome. Together, we shall preserve our harvests and homes.";
                    break;

                case "empire_s":
                    enrollmentFinalText = "Welcome to the Greens. We will make the world anew.";
                    break;

                case "sturgia":
                    enrollmentFinalText = "Welcome. We answer only to the salt and sea.";
                    break;

                case "aserai":
                    enrollmentFinalText = "Welcome. The sands of Dorne await.";
                    break;

                case "vlandia":
                    enrollmentFinalText = "Welcome. Gold and glory are yours to claim.";
                    break;

                case "battania":
                    enrollmentFinalText = "Welcome. We march as one.";
                    break;

                default:
                    enrollmentFinalText = "Excellent. Prepare yourself; we march soon. We shall see how you fare in battle!";
                    break;
            }

            GameTexts.SetVariable("ENROLLMENT_FINAL_TEXT", enrollmentFinalText);
            return true;
        }

        private bool EnrollmentDecisionCondition()
        {
            var lordHero = Campaign.Current.ConversationManager.OneToOneConversationHero;
            if (lordHero == null) return false;

            var lordKingdom = lordHero.MapFaction as Kingdom;
            string kingdomId = lordKingdom != null ? lordKingdom.StringId : "default";

            string enrollmentDecisionText;

            switch (kingdomId)
            {
                case "mage":
                    enrollmentDecisionText = "Prepare yourselves! For House Baratheon and King Aegon!";
                    break;

                case "giant":
                    enrollmentDecisionText = "To arms! For the Riverlands, for family, for home, for the Dragon Queen!";
                    break;

                case "dwarf_kingdom":
                    enrollmentDecisionText = "We fight for our Queen, onward!";
                    break;

                case "empire":
                    enrollmentDecisionText = "Mount up! For the Vale, for honor, and for the Dragon Queen!";
                    break;

                case "empire_w":
                    enrollmentDecisionText = "For the one true King, Aegon! Forward!";
                    break;

                case "empire_s":
                    enrollmentDecisionText = "For King Aegon II, rightful heir to the Iron Throne! Forward!";
                    break;

                case "sturgia":
                    enrollmentDecisionText = "Raise the sails! For the Iron Islands!";
                    break;

                case "aserai":
                    enrollmentDecisionText = "For Dorne! Unbowed, Unbent, Unbroken!";
                    break;

                case "vlandia":
                    enrollmentDecisionText = "For the Westerlands! Fight with fury!";
                    break;

                case "battania":
                    enrollmentDecisionText = "For the North! Prepare for battle!";
                    break;

                default:
                    enrollmentDecisionText = "Men, we march!";
                    break;
            }

            GameTexts.SetVariable("ENROLLMENT_DECISION_TEXT", enrollmentDecisionText);
            return true;
        }

        private bool LordshipOfferCondition()
        {
            if (!_hasOfferedLordship) return false;
            if (Campaign.Current.ConversationManager.OneToOneConversationHero != _enrollmentEnrollingLord) return false;

            var lordHero = Campaign.Current.ConversationManager.OneToOneConversationHero;
            if (lordHero == null) return false;

            var lordKingdom = lordHero.MapFaction as Kingdom;
            string kingdomId = lordKingdom != null ? lordKingdom.StringId : "default";

            string lordshipOfferText;

            switch (kingdomId)
            {
                case "mage":
                    lordshipOfferText = "You have served The Stormlands well. The storms you have weathered prove your worth. I would like to grant you Lordship within our faction. Will you accept this honor?";
                    break;

                case "giant":
                    lordshipOfferText = "Your dedication to the Riverlands has been exemplary. The rivers flow strong because of warriors like you. We would be honored to grant you the title of Lord. Will you accept this responsibility?";
                    break;

                case "dwarf_kingdom":
                    lordshipOfferText = "Your unwavering support for Her Grace Queen Rhaenyra has not gone unnoticed. The realm needs champions like you. I offer you the title of Lord and a castle in the Seven Kingdoms. Will you stand with us?";
                    break;

                case "empire":
                    lordshipOfferText = "Your service to the Vale reflects the highest honor. As stalwart as our mountains, you have proven yourself. I invite you to join our ranks as a Lord. Will you accept this duty?";
                    break;

                case "empire_w":
                    lordshipOfferText = "The Reach thrives because of loyal subjects like you. Our lands are fertile, and our people prosper. We would be pleased to bestow upon you the title of Lord. Will you join us in this noble endeavor?";
                    break;

                case "empire_s":
                    lordshipOfferText = "His Grace King Aegon II recognizes your valor and loyalty. The Iron Throne needs leaders like you. I extend an offer for you to become a Lord in our realm. Will you accept this privilege and serve the realm?";
                    break;

                case "sturgia":
                    lordshipOfferText = "You have proven yourself a true Ironborn. The sea sings of your deeds. I offer you the title of Lord among our ranks. Will you accept and rise, harder and stronger?";
                    break;

                case "aserai":
                    lordshipOfferText = "Your actions have honored Dorne. Unbowed, Unbent, Unbrokenyou embody our words. I offer you the title of Lord within our realm. Will you stand with us and accept this honor?";
                    break;

                case "vlandia":
                    lordshipOfferText = "Your contributions have added to the wealth and glory of the Westerlands. The lion's roar echoes louder because of you. I offer you Lordship over a castle in our realm. Will you accept this duty?";
                    break;

                case "battania":
                    lordshipOfferText = "Winter may be coming, but with warriors like you, the North remains strong. You have earned the right to be called one of us. I offer you the title of Lord within the North. Do you accept?";
                    break;

                default:
                    lordshipOfferText = "Your service has not gone unnoticed. You have proven yourself over these past months. I would like to offer you the title of Lord within our faction. What say you?";
                    break;
            }

            GameTexts.SetVariable("LORDSHIP_OFFER_TEXT", lordshipOfferText);
            return true;
        }

        private bool SanityCheck()
        {
            return Clan.PlayerClan.MapFaction == Clan.PlayerClan &&
                MobileParty.MainParty.Army == null;
        }

        private bool QuitCondition()
        {
            return Campaign.Current.ConversationManager.OneToOneConversationHero == _enrollmentEnrollingLord && IsEnrolled() && _durationInDays > MinimumServeDays;
        }

        private void DisplayPrompt(Action enrollPlayer)
        {
            string inquiryText = $"You are about to enroll in a party. Are you ready to depart? You must serve for at least {MinimumServeDays} days.";

            var inquiry = new InquiryData(
                "Enroll",
                inquiryText,
                true,
                true,
                "Yes",
                "No",
                enrollPlayer,
                () => _enrollInquiryDeclined = true
            );
            InformationManager.ShowInquiry(inquiry);
        }

        private void SetupSoldierMenu(CampaignGameStarter campaignGameStarter)
        {
            var infotext = new TextObject("{ENROLLING_TEXT}");

            campaignGameStarter.AddGameMenuOption("town", "town_back_to_enrollment", "Back", args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return IsEnrolled();
            }, args => GameMenu.SwitchToMenu("enrollment_menu"), true);

            campaignGameStarter.AddWaitGameMenu("enrollment_menu", infotext.ToString(), party_wait_talk_to_other_members_on_init, wait_on_condition,
                null, wait_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            campaignGameStarter.AddGameMenuOption("enrollment_menu", "visit_armory", "Visit the Armorer", args =>
            {
                if (IsEnrolled())
                {
                    var playerKingdom = Hero.MainHero.Clan?.Kingdom;

                    if (playerKingdom != null)
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Craft;
                        args.Tooltip = new TextObject($"Access the Armory of {playerKingdom.Name} through the Armorer of the party.");
                        return true;
                    }
                    else
                    {
                        args.Tooltip = new TextObject("The Armorer needs to restock his supplies. Your Lord must be in a town.");
                        args.IsEnabled = false;
                        return true;
                    }
                }
                return false;
            }, HouseArmoryConsequence, true);

            var textObjectSoldierEnterSettlement = new TextObject("Enter the settlement");
            campaignGameStarter.AddGameMenuOption("enrollment_menu", "enter_town", textObjectSoldierEnterSettlement.ToString(), args =>
            {
                if (!IsEnrolled())
                {
                    return false;
                }
                args.optionLeaveType = GameMenuOption.LeaveType.Continue;

                return _enrollmentEnrollingLord.PartyBelongedTo.CurrentSettlement != null &&
                _enrollmentEnrollingLord.PartyBelongedTo.CurrentSettlement == PlayerEncounter.EncounterSettlement &&
                PlayerEncounter.EncounterSettlement.IsTown;
            }, args =>
            {
                GameMenu.SwitchToMenu("town");
            }, true);

            campaignGameStarter.AddGameMenuOption("enrollment_menu", "talk_to_lord", "Speak to your lord", args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                return _enrollmentEnrolled && _enrollmentEnrollingLord != null;
            }, args =>
            {
                StartDialog();
            }, false, 1);

            campaignGameStarter.AddGameMenuOption("enrollment_menu", "pause_time_option", "",
                args =>
                {
                    var onOffText = _pauseModeToggle ? "On" : "Off";
                    var text = new TextObject("Pause Time: {PAUSE_ONOFF}");
                    text.SetTextVariable("PAUSE_ONOFF", onOffText);
                    args.Text = text; // Assign the TextObject directly
                    return true;
                },
                PauseModeToggle);

            for (int i = 0; i < 5; i++)
            {
                int index = i;
                campaignGameStarter.AddGameMenuOption("enrollment_menu", $"activity{index}_option", "",
                    args =>
                    {
                        var text = new TextObject("Activity " + (index + 1));
                        if (_currentActivityIndex == index)
                        {
                            text = new TextObject("[" + text.ToString() + "]");
                        }
                        args.Text = text; // Assign the TextObject directly
                        return HoverActivity(index, args);
                    },
                    args => ToggleActivity(index, args));
            }

            campaignGameStarter.AddGameMenuOption("enrollment_menu", "party_wait_leave", "Desert", args =>
            {
                var infoText = new TextObject("This will damage your reputation with {FACTION}");
                string factionName = _enrollmentEnrollingLord != null ? _enrollmentEnrollingLord.MapFaction.Name.ToString() : "ERROR";
                infoText.SetTextVariable("FACTION", factionName);
                args.Tooltip = infoText;
                args.optionLeaveType = GameMenuOption.LeaveType.Escape;
                return true;
            }, args =>
            {
                LeaveEnrollingParty("enrollment_menu");
            }, true);
        }

        private void HouseArmoryConsequence(MenuCallbackArgs args)
        {
            var playerKingdom = Hero.MainHero.Clan?.Kingdom;
            if (playerKingdom != null)
            {
                // Get the culture of the player's kingdom
                var kingdomCulture = playerKingdom.Culture;

                // Collect items where the item's culture matches the kingdom's culture
                List<ItemObject> armoryItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                    .Where(item => item.Culture == kingdomCulture)
                    .ToList();

                // Display the armory items to the player
                ShowArmoryItemMenu(armoryItems);
            }
        }

        private void ShowArmoryItemMenu(List<ItemObject> armoryItems)
        {
            List<InquiryElement> inquiryElements = new List<InquiryElement>();
            foreach (ItemObject item in armoryItems)
            {
                string itemName = item.Name.ToString();
                int itemPrice = item.Value;
                inquiryElements.Add(new InquiryElement(item, itemName, new ImageIdentifier(item), true, $"Price: {itemPrice} Gold Coins"));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Choose Items to Purchase",
                "Select items to buy from the armory:",
                inquiryElements,
                true,
                1,
                inquiryElements.Count,
                "Purchase",
                "Cancel",
                selectedItems =>
                {
                    if (selectedItems != null && selectedItems.Count > 0)
                    {
                        ProcessSelectedItems(selectedItems);
                    }
                },
                null,
                "",
                true));
        }

        private void ProcessSelectedItems(List<InquiryElement> selectedItems)
        {
            ProcessItemPurchase(selectedItems, 0);
        }

        private void ProcessItemPurchase(List<InquiryElement> items, int index)
        {
            if (index >= items.Count)
            {
                InformationManager.DisplayMessage(new InformationMessage("Purchase complete."));
                return;
            }

            ItemObject selectedItem = (ItemObject)items[index].Identifier;
            int itemPrice = selectedItem.Value;
            string itemName = selectedItem.Name.ToString();

            InformationManager.ShowTextInquiry(new TextInquiryData(
                "Quantity",
                $"How many {itemName} would you like to buy? (Price: {itemPrice} Gold Dragons each)",
                true,
                false,
                "Buy",
                "Cancel",
                response =>
                {
                    if (int.TryParse(response, out int quantity) && quantity > 0)
                    {
                        int totalCost = itemPrice * quantity;
                        if (Hero.MainHero.Gold >= totalCost)
                        {
                            Hero.MainHero.ChangeHeroGold(-totalCost);
                            MobileParty.MainParty.ItemRoster.AddToCounts(selectedItem, quantity);
                            InformationManager.DisplayMessage(new InformationMessage($"You purchased {quantity} {itemName}(s) for {totalCost} Gold Dragons."));
                        }
                        else
                        {
                            InformationManager.DisplayMessage(new InformationMessage("You do not have enough Gold Dragons."));
                        }

                        ProcessItemPurchase(items, index + 1);
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage("Invalid quantity entered."));
                        ProcessItemPurchase(items, index);
                    }
                },
                () =>
                {
                    ProcessItemPurchase(items, index + 1);
                }), true);
        }

        private void PauseModeToggle(MenuCallbackArgs args)
        {
            _pauseModeToggle = !_pauseModeToggle;

            var onOffText = _pauseModeToggle ? "On" : "Off";

            var text = new TextObject("Pause Time: {PAUSE_ONOFF}");
            text.SetTextVariable("PAUSE_ONOFF", onOffText);

            args.Text = text;
            args.MenuContext.Refresh();
        }

        private void StartDialog()
        {
            ConversationCharacterData characterData = new(_enrollmentEnrollingLord.CharacterObject, _enrollmentEnrollingLord.PartyBelongedTo.Party);
            ConversationCharacterData playerData = new(Hero.MainHero.CharacterObject, Hero.MainHero.PartyBelongedTo.Party);
            Campaign.Current.CurrentConversationContext = ConversationContext.Default;
            Campaign.Current.ConversationManager.OpenMapConversation(playerData, characterData);
        }

        private void SetActivities()
        {
            var activities = _activities.GetEnrollmentActivities(_playerCareer);
            for (var i = 0; i < 5; i++)
            {
                var text = new TextObject("Activity " + (i + 1));
                if (_currentActivityIndex == i)
                {
                    text = new TextObject("[" + text.ToString() + "]");
                }
                GameTexts.SetVariable("ENROLLMENTACTIVITYTEXT" + i, text.ToString());
            }
        }

        private bool HoverActivity(int i, MenuCallbackArgs args)
        {
            args.Tooltip = new TextObject("Perform Activity " + (i + 1));
            return true;
        }

        private void ToggleActivity(int i, MenuCallbackArgs args)
        {
            _currentActivityIndex = i;
            SetActivities();

            var activities = _activities.GetEnrollmentActivities(_playerCareer);
            if (i >= 0 && i < activities.Count)
            {
                _currentTrainedSkill = activities[i];
            }
            else
            {
                _currentTrainedSkill = DefaultSkills.OneHanded;
            }
            args.Tooltip = new TextObject("Perform Activity " + (i + 1));
            args.MenuContext.Refresh();
        }

        private void SetupBattleMenu(CampaignGameStarter campaignGameStarter)
        {
            TextObject enrollmentBattleTextMenu = new("To arms men!");
            campaignGameStarter.AddGameMenu("enrollment_battle_menu", enrollmentBattleTextMenu.ToString(), party_wait_talk_to_other_members_on_init, GameOverlays.MenuOverlayType.Encounter);

            campaignGameStarter.AddGameMenuOption("enrollment_battle_menu", "enrollment_join_battle", "Join the battle!",
                enrollment_battle_menu_join_battle_on_condition,
                delegate
                {
                    while (Campaign.Current.CurrentMenuContext != null)
                    {
                        GameMenu.ExitToLast();
                    }
                    if (_enrollmentEnrollingLord.PartyBelongedTo.MapEvent != null)
                    {
                        if (_enrollmentEnrollingLordIsAttacking)
                        {
                            var mapEvent = _enrollmentEnrollingLord.PartyBelongedTo.MapEvent;
                            StartBattleAction.Apply(PartyBase.MainParty, mapEvent.DefenderSide.LeaderParty);

                            MobileParty.MainParty.CurrentSettlement = _enrollmentEnrollingLord.PartyBelongedTo.MapEvent.MapEventSettlement;

                            if (mapEvent.IsSiegeAssault)
                            {
                                Game.Current.AfterTick += InitializeSiegeBattle;
                                _siegeBattleMissionStarted = true;
                            }
                        }
                        else
                        {
                            var eventparty = _enrollmentEnrollingLord.PartyBelongedTo;
                            if (_enrollmentEnrollingLord.PartyBelongedTo.Army != null && _enrollmentEnrollingLord.PartyBelongedTo.Army.LeaderParty != eventparty)
                            {
                                eventparty = _enrollmentEnrollingLord.PartyBelongedTo.Army.LeaderParty;
                            }

                            StartBattleAction.Apply(PartyBase.MainParty, eventparty.MapEvent.AttackerSide.LeaderParty);
                        }
                        _startBattle = true;
                    }
                }
                , false, 4);

            campaignGameStarter.AddGameMenuOption("enrollment_battle_menu", "enrollment_avoid_combat", "Rest in your tent.",
               enrollment_battle_menu_avoid_combat_on_condition,
               delegate (MenuCallbackArgs args)
               {
                   _enrollmentLordIsFightingWithoutPlayer = true;
                   _startBattle = false;
                   args.MenuContext.GameMenu.StartWait();
               }
               , false, 4);

            campaignGameStarter.AddGameMenuOption("enrollment_battle_menu", "enrollment_flee", "Flee for your life!",
               enrollment_battle_menu_desert_on_condition,
               delegate
               {
                   LeaveEnrollingParty("enrollment_battle_menu", true);
               }
               , false, 4);
        }

        public void LeaveLordPartyAction()
        {
            var dummy = PlayerEncounter.Current;
            _enrollmentEnrolled = false;
            _enrollmentEnrollingLord = null;
            _enrollmentWaitMenuShown = false;
            PlayerEncounter.Finish();
            UndoDiplomacy();
            ShowPlayerParty();

            _durationInDays = 0;
            _manuallyFoughtBattles = 0;
            _playerCareer = null;
        }


        private void InitializeSiegeBattle(float tick)
        {
            if (!_enrollmentEnrolled) return;
            if (!_siegeBattleMissionStarted) return;
            if (MobileParty.MainParty == null) return;
            var mainPartyMapEvent = MobileParty.MainParty.MapEvent;
            if (mainPartyMapEvent == null || mainPartyMapEvent.StringId == null) return;

            StartBattleAction.Apply(PartyBase.MainParty, mainPartyMapEvent.DefenderSide.LeaderParty);
            _siegeBattleMissionStarted = false;
            Game.Current.AfterTick -= InitializeSiegeBattle;
        }

        private void OnRaidCompleted(BattleSideEnum side, RaidEventComponent component)
        {
            if (component.IsPlayerMapEvent)
            {
                GameMenu.ActivateGameMenu("enrollment_menu");
            }
        }

        private bool enrollment_battle_menu_join_battle_on_condition(MenuCallbackArgs args)
        {
            var maxHitPointsHero = Hero.MainHero.MaxHitPoints;
            var hitPointsHero = Hero.MainHero.HitPoints;
            return hitPointsHero > maxHitPointsHero * 0.2;
        }

        private bool enrollment_battle_menu_desert_on_condition(MenuCallbackArgs args)
        {
            return _enrollmentEnrollingLord.CurrentSettlement == null;
        }

        private bool enrollment_battle_menu_avoid_combat_on_condition(MenuCallbackArgs args)
        {
            var maxHitPointsHero = Hero.MainHero.MaxHitPoints;
            var hitPointsHero = Hero.MainHero.HitPoints;

            var lordEvent = _enrollmentEnrollingLord.PartyBelongedTo.MapEvent;

            if (lordEvent == null) return false;

            var partyStrength = GetEnrollingLordEventStrengthRatio(lordEvent);

            var combatstregthThreshold = partyStrength > RatioPartyAgainstEnemyStrength;

            return hitPointsHero < maxHitPointsHero * 0.2 || combatstregthThreshold;
        }

        private bool wait_on_condition(MenuCallbackArgs args)
        {
            return true;
        }

        private void wait_on_tick(MenuCallbackArgs args, CampaignTime time)
        {
            bool flag = _enrollmentEnrollingLord == null || _enrollmentEnrollingLord.PartyBelongedTo == null || !_enrollmentEnrolled;
            if (flag)
            {
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }
            }
            else
            {
                if (args.MenuContext?.GameMenu == null) return;

                string simpleStatus;

                if (!string.IsNullOrEmpty(_playerCareer))
                {
                    simpleStatus = $"You are a {_playerCareer}.\nYou are enrolled and traveling with the party.";
                }
                else
                {
                    simpleStatus = "You are enrolled and traveling with the party.";
                }

                var enrollingText = new TextObject(simpleStatus); // Create a TextObject and set the text directly
                GameTexts.SetVariable("ENROLLING_TEXT", enrollingText); // Assign to a variable that can be used elsewhere if needed
            }
        }


        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_enrolled", ref _enrollmentEnrolled);
            dataStore.SyncData("_enrollingLord", ref _enrollmentEnrollingLord);
            dataStore.SyncData("_entryServiceTimeStamp", ref _entryServiceTimeStamp);
            dataStore.SyncData("_manuallyFoughtBattles", ref _manuallyFoughtBattles);
            dataStore.SyncData("_durationInDays", ref _durationInDays);
            dataStore.SyncData("_playerCareer", ref _playerCareer);
            dataStore.SyncData("_hasOfferedLordship", ref _hasOfferedLordship);
        }

        private void party_wait_talk_to_other_members_on_init(MenuCallbackArgs args) { }

        private void ControlPlayerLoot(MapEvent mapEvent)
        {
            if (mapEvent.PlayerSide == mapEvent.WinningSide && IsEnrolled())
            {
                if (!_enrollmentLordIsFightingWithoutPlayer)
                {
                    _manuallyFoughtBattles++;
                }

                PlayerEncounter.Current.RosterToReceiveLootItems.Clear();
                PlayerEncounter.Current.RosterToReceiveLootMembers.Clear();
                PlayerEncounter.Current.RosterToReceiveLootPrisoners.Clear();
            }

            _enrollmentWaitMenuShown = false;
        }

        private void OnPartyLeavesSettlement(MobileParty mobileParty, Settlement settlement)
        {
            if (!IsEnrolled() || _enrollmentEnrollingLord == null) return;

            if (_enrollmentEnrollingLord.PartyBelongedTo == mobileParty || MobileParty.MainParty == mobileParty && mobileParty.CurrentSettlement == null)
            {
                while (Campaign.Current.CurrentMenuContext != null)
                    GameMenu.ExitToLast();
                GameMenu.ActivateGameMenu("enrollment_menu");
                if (PartyBase.MainParty.MobileParty.CurrentSettlement != null)
                    LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
            }
        }

        private void EnrollingLordPartyEntersSettlement(MobileParty mobileParty, Settlement settlement, Hero arg3)
        {
            if (!_enrollmentEnrolled || !settlement.IsTown) return;
            if (MobileParty.MainParty.CurrentSettlement == settlement && PlayerEncounter.EncounterSettlement == settlement) return;
            if (_enrollmentEnrollingLord != null && _enrollmentEnrollingLord.PartyBelongedTo == mobileParty)
            {
                EnterSettlementAction.ApplyForParty(MobileParty.MainParty, _enrollmentEnrollingLord.CurrentSettlement);
                EncounterManager.StartSettlementEncounter(MobileParty.MainParty, settlement);
                GameMenu.SwitchToMenu("enrollment_menu");

                if (_pauseModeToggle)
                {
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                }
            }
        }

        private void MapEventEnded(MapEvent mapEvent)
        {
            if (_enrollmentEnrollingLord == null || !IsEnrolled()) return;

            if (_enrollmentEnrollingLord != null && !mapEvent.IsPlayerMapEvent && GetEnrollingLordisInMapEvent(mapEvent))
            {
                GameMenu.ActivateGameMenu("enrollment_menu");
                _enrollmentLordIsFightingWithoutPlayer = false;
            }
            if (mapEvent.IsPlayerMapEvent)
            {
                GameMenu.ActivateGameMenu("enrollment_menu");
            }
        }

        private void OnTick(float dt)
        {
            if (_enrollmentEnrolled && _enrollmentEnrollingLord != null && _enrollmentEnrollingLord.PartyBelongedTo != null)
            {
                if (_enrollmentLordIsFightingWithoutPlayer || _enrollmentEnrollingLord.PartyBelongedTo?.BesiegerCamp != null || _enrollmentEnrollingLord.PartyBelongedTo.CurrentSettlement != null)
                {
                    if (!MobileParty.MainParty.ShouldBeIgnored)
                    {
                        MobileParty.MainParty.IgnoreForHours(1);
                    }
                }

                var menu = Campaign.Current.GameMenuManager.GetGameMenu("enrollment_menu");
                _durationInDays = Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow - _entryServiceTimeStamp;
                menu?.RunOnTick(Campaign.Current.CurrentMenuContext, dt);

                if (!_enrollmentWaitMenuShown)
                {
                    GameMenu.ActivateGameMenu("enrollment_menu");
                    _enrollmentWaitMenuShown = true;
                    SetActivities();
                    Campaign.Current.CurrentMenuContext.Refresh();
                }

                HidePlayerParty();
                MobileParty.MainParty.Position2D = _enrollmentEnrollingLord.PartyBelongedTo.Position2D;

                if (_enrollmentEnrollingLord.PartyBelongedTo.MapEvent != null && MobileParty.MainParty.MapEvent == null)
                {
                    var mapEvent = _enrollmentEnrollingLord.PartyBelongedTo.MapEvent;
                    _enrollmentEnrollingLordIsAttacking = false;

                    foreach (var party in mapEvent.AttackerSide.Parties)
                    {
                        if (party.Party == _enrollmentEnrollingLord.PartyBelongedTo.Party)
                        {
                            _enrollmentEnrollingLordIsAttacking = true;
                            break;
                        }
                    }

                    if (!_enrollmentLordIsFightingWithoutPlayer && mapEvent.DefenderSide.TroopCount > 0)
                    {
                        GameMenu.ActivateGameMenu("enrollment_battle_menu");
                    }
                }

                if (_durationInDays >= LordshipDays && !_hasOfferedLordship)
                {
                    _hasOfferedLordship = true;
                    StartLordshipDialogue();
                }
            }
            else if (_enrollmentEnrolled && _enrollmentEnrollingLord?.PartyBelongedTo == null)
            {
                LeaveLordPartyAction();
            }
        }

        private void UndoDiplomacy()
        {
            ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(Hero.MainHero.Clan, false);
        }

        public void EnrollPlayer()
        {
            HidePlayerParty();
            DisbandParty();
            _enrollmentEnrollingLord = CharacterObject.OneToOneConversationCharacter.HeroObject;
            ChangeKingdomAction.ApplyByJoinFactionAsMercenary(Hero.MainHero.Clan, _enrollmentEnrollingLord.Clan.Kingdom, 25, false);
            GameTexts.SetVariable("ENROLLINGLORDNAME", _enrollmentEnrollingLord.EncyclopediaLinkWithName);

            while (Campaign.Current.CurrentMenuContext != null)
                GameMenu.ExitToLast();
            _enrollmentEnrolled = true;

            _entryServiceTimeStamp = Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow;

            _enrollmentEnrollingLord.PartyBelongedTo.MemberRoster.AddToCounts(Hero.MainHero.CharacterObject, 1);

            // Determine and store the player's career
            _playerCareer = DeterminePlayerCareer();

            CampaignEvents.TickEvent.AddNonSerializedListener(this, UpdatePlayerPartyPosition);

            GameMenu.ActivateGameMenu("enrollment_menu");
        }

        private void UpdatePlayerPartyPosition(float dt)
        {
            if (_enrollmentEnrolled && _enrollmentEnrollingLord?.PartyBelongedTo != null)
            {
                MobileParty.MainParty.Position2D = _enrollmentEnrollingLord.PartyBelongedTo.Position2D;
            }
            else
            {
                CampaignEvents.TickEvent.ClearListeners(this);
            }
        }

        private void ShowPlayerParty()
        {
            MobileParty.MainParty.IsVisible = true;
            MobileParty.MainParty.IsActive = true;
            MobileParty.MainParty.Party.SetAsCameraFollowParty();
        }

        private void HidePlayerParty()
        {
            MobileParty.MainParty.IsVisible = false;
            MobileParty.MainParty.IsActive = false;
            MobileParty.MainParty.Party.SetAsCameraFollowParty();
        }

        private void DisbandParty()
        {
            if (MobileParty.MainParty.MemberRoster.TotalManCount <= 1)
                return;
            List<TroopRosterElement> troopRosterElementList = new List<TroopRosterElement>();
            foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.MemberRoster.GetTroopRoster())
            {
                if (troopRosterElement.Character != Hero.MainHero.CharacterObject && troopRosterElement.Character.HeroObject == null)
                    troopRosterElementList.Add(troopRosterElement);
            }
            if (troopRosterElementList.Count == 0)
                return;
            foreach (TroopRosterElement troopRosterElement in troopRosterElementList)
            {
                MobileParty.MainParty.MemberRoster.AddToCounts(troopRosterElement.Character, -1 * troopRosterElement.Number);
            }
        }

        private bool GetEnrollingLordisInMapEvent(MapEvent mapEvent)
        {
            var lordIsInMapEvent = false;
            if (_enrollmentEnrollingLord == null)
                return false;

            if (_enrollmentEnrollingLord.PartyBelongedTo == null)
            {
                return false;
            }

            foreach (var party in mapEvent.AttackerSide.Parties)
            {
                if (party.Party == _enrollmentEnrollingLord.PartyBelongedTo.Party)
                {
                    lordIsInMapEvent = true;
                }
            }
            foreach (var party in mapEvent.DefenderSide.Parties)
            {
                if (party.Party == _enrollmentEnrollingLord.PartyBelongedTo.Party)
                {
                    lordIsInMapEvent = true;
                }
            }

            return lordIsInMapEvent;
        }

        private string DeterminePlayerCareer()
        {
            Dictionary<string, int> careerScores = new Dictionary<string, int>();

            foreach (var career in _activities.GetCareers())
            {
                int totalSkillLevel = 0;
                var skills = _activities.GetEnrollmentActivities(career);
                foreach (var skill in skills)
                {
                    totalSkillLevel += Hero.MainHero.GetSkillValue(skill);
                }
                careerScores[career] = totalSkillLevel;
            }

            // Find the career with the highest score
            string bestCareer = careerScores.OrderByDescending(kvp => kvp.Value).First().Key;
            return bestCareer;
        }

        private void OnMissionStarted(IMission mission)
        {
            if (_enrollmentEnrolled && !string.IsNullOrEmpty(_playerCareer))
            {
                Mission currentMission = Mission.Current;
                if (currentMission != null && currentMission.CombatType == Mission.MissionCombatType.Combat)
                {
                    currentMission.AddMissionBehavior(new EnrollmentMissionBehavior(_playerCareer));
                }
            }
        }

        private void StartLordshipDialogue()
        {
            InformationManager.DisplayMessage(new InformationMessage("Your lord wishes to speak with you."));

            StartDialog();
        }

        private void AcceptLordship()
        {
            LeaveLordPartyAction();

            ChangeKingdomAction.ApplyByJoinToKingdom(Hero.MainHero.Clan, _enrollmentEnrollingLord.Clan.Kingdom);

            GivePlayerSettlement();

            InformationManager.DisplayMessage(new InformationMessage("You have become a vassal of " + _enrollmentEnrollingLord.Clan.Kingdom.Name.ToString()));
        }

        private void DeclineLordship()
        {
            _hasOfferedLordship = false;
        }

        private void GivePlayerSettlement()
        {
            var kingdom = _enrollmentEnrollingLord.Clan.Kingdom;

            var settlements = kingdom.Settlements
                .Where(s => s.OwnerClan == kingdom.RulingClan && s.IsCastle)
                .ToList();

            if (settlements.Count == 0)
            {
                settlements = kingdom.Settlements
                    .Where(s => s.IsCastle)
                    .ToList();
            }

            if (settlements.Count > 0)
            {
                var settlement = settlements.First();

                ChangeOwnerOfSettlementAction.ApplyByKingDecision(Hero.MainHero, settlement);
                InformationManager.DisplayMessage(new InformationMessage("You have been granted the Castle of " + settlement.Name.ToString()));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("No castle available to grant."));
            }
        }
    }

    public class EnrollmentMissionBehavior : MissionBehavior
    {
        private string _playerCareer;

        public EnrollmentMissionBehavior(string playerCareer)
        {
            _playerCareer = playerCareer;
        }

        public override void OnAgentCreated(Agent agent)
        {
            if (agent.IsPlayerControlled)
            {
                FormationClass formationClass = GetFormationClassForCareer(_playerCareer);

                if (formationClass != FormationClass.NumberOfDefaultFormations)
                {
                    agent.Formation = agent.Team.GetFormation(formationClass);
                }
            }
        }

        private FormationClass GetFormationClassForCareer(string career)
        {
            switch (career)
            {
                case "Footman":
                    return FormationClass.Infantry;
                case "Archer":
                    return FormationClass.Ranged;
                case "Knight":
                    return FormationClass.Cavalry;
                case "Mason":
                    return FormationClass.Infantry;
                case "Maester":
                    return FormationClass.Ranged;
                default:
                    return FormationClass.Infantry;
            }
        }

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    }

    public static class ADODEnrollmentPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PartyNameplateVM), "RefreshBinding")]
        private static void HideMainParty(PartyNameplateVM __instance)
        {
            if (__instance.Party == MobileParty.MainParty)
            {
                var enrollmentBehavior = Campaign.Current.GetCampaignBehavior<ADODEnrollmentCampaignBehavior>();
                bool isEnrolled = enrollmentBehavior?.IsEnrolled() ?? false;

                __instance.IsMainParty = !isEnrolled;
                __instance.IsVisibleOnMap = !isEnrolled;
            }
        }

        [HarmonyPatch(typeof(PlayerEncounter), "DoPlayerVictory")]
        public class ADODPostBattleDiplomaticEffectsPatch
        {
            [HarmonyPrefix]
            private static bool AdjustPostBattleActions(ref PlayerEncounterState ____mapEventState, List<TroopRosterElement> ____freedHeroes, List<TroopRosterElement> ____capturedHeroes)
            {
                var enrollmentBehavior = Campaign.Current?.GetCampaignBehavior<ADODEnrollmentCampaignBehavior>();
                if (enrollmentBehavior != null && enrollmentBehavior.IsEnrolled())
                {
                    if (____capturedHeroes != null || ____freedHeroes != null)
                    {
                        foreach (var heroElement in (____capturedHeroes ?? new List<TroopRosterElement>()).Concat(____freedHeroes ?? new List<TroopRosterElement>()).ToList())
                        {
                            if (heroElement.Character?.HeroObject != null)
                            {
                                EndCaptivityAction.ApplyByReleasedAfterBattle(heroElement.Character.HeroObject);
                            }
                        }
                    }

                    ____mapEventState = PlayerEncounterState.LootParty;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RecruitmentCampaignBehavior), "buy_mercenaries_condition")]
        public class ADODRestrictRecruitmentPatch
        {
            [HarmonyPrefix]
            private static bool PreventRecruitment(MenuCallbackArgs args)
            {
                var enrollmentBehavior = Campaign.Current?.GetCampaignBehavior<ADODEnrollmentCampaignBehavior>();
                if (enrollmentBehavior != null && enrollmentBehavior.IsEnrolled())
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(VassalAndMercenaryOfferCampaignBehavior), "DailyTick")]
        public class ADODRestrictVassalOffersPatch
        {
            [HarmonyPrefix]
            private static bool PreventVassalOffers()
            {
                var enrollmentBehavior = Campaign.Current?.GetCampaignBehavior<ADODEnrollmentCampaignBehavior>();
                if (enrollmentBehavior != null && enrollmentBehavior.IsEnrolled())
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_hero_main_options_discussions")]
        public class ADODDisableDiplomaticDiscussionsPatch
        {
            [HarmonyPrefix]
            private static bool DisableDiscussions(ref bool __result)
            {
                var enrollmentBehavior = Campaign.Current?.GetCampaignBehavior<ADODEnrollmentCampaignBehavior>();
                if (enrollmentBehavior != null && enrollmentBehavior.IsEnrolled())
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_lord_is_threated_neutral_on_condition")]
        public class ADODDisableThreatenOptionsPatch
        {
            [HarmonyPrefix]
            private static bool DisableThreats(ref bool __result)
            {
                var enrollmentBehavior = Campaign.Current?.GetCampaignBehavior<ADODEnrollmentCampaignBehavior>();
                if (enrollmentBehavior != null && enrollmentBehavior.IsEnrolled())
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
    }

    public static class ADODEnrollmentHelpers
    {
        public static void AddSoldierCustomResourceBenefits(Hero hero, ref ExplainedNumber number)
        {
            var enrollmentCampaignBehavior = Campaign.Current.GetCampaignBehavior<ADODEnrollmentCampaignBehavior>();

            if (enrollmentCampaignBehavior == null) return;

            var duration = enrollmentCampaignBehavior.DurationInDays;
            var battles = enrollmentCampaignBehavior.ManuallyFoughtBattles;
            number.AddFactor(enrollmentCampaignBehavior.DurationInDays / 10);
        }

        public static void AddSoldierWage(Hero hero, ref ExplainedNumber number)
        {
            var enrollmentBehavior = Campaign.Current.GetCampaignBehavior<ADODEnrollmentCampaignBehavior>();
            if (enrollmentBehavior == null) return;
            if (hero.PartyBelongedTo == null) return;

            var duration = enrollmentBehavior.DurationInDays;
            var battles = enrollmentBehavior.ManuallyFoughtBattles;
            var baseWage = 25 * hero.Level;

            baseWage += (int)(0.1f * battles * baseWage);
            baseWage += (int)((duration / 20) * baseWage);

            number.Add(baseWage, new TextObject("Enrollment Wage"));
        }

        public static bool SoldierServiceConditions()
        {
            var dialogPartner = Campaign.Current.ConversationManager.OneToOneConversationHero;
            return dialogPartner.Culture.StringId == Hero.MainHero.Culture.StringId;
        }

        public static bool IsEnrolled(this Hero hero)
        {
            var enrollmentCampaignBehavior = Campaign.Current.GetCampaignBehavior<ADODEnrollmentCampaignBehavior>();
            if (enrollmentCampaignBehavior != null)
            {
                return enrollmentCampaignBehavior.IsEnrolled();
            }
            return false;
        }

        public static List<Hero> GetMemberHeroes(this MobileParty party)
        {
            List<Hero> heroes = new List<Hero>();
            foreach (var member in party.MemberRoster.GetTroopRoster())
            {
                if (member.Character.HeroObject != null)
                {
                    heroes.Add(member.Character.HeroObject);
                }
            }
            return heroes;
        }
    }
}

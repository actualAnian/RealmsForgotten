using Bannerlord.Module1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using RealmsForgotten.CustomSkills;

namespace RealmsForgotten
{
    public class VisitLibrary : CampaignBehaviorBase
    {
        private const int BaseSkillXpAmount = 50; // XP amount for demonstration
        private const int BaseGoldCost = 100; // Base gold cost for book education
        private const int BasePowderAmount = 1; // Base amount of alchemical powder needed

        private static readonly TextObject EducationTitleText = new TextObject("{=education_title}Education Progress");
        private static readonly TextObject ContinueText = new TextObject("{=continue}CONTINUE");
        private static readonly TextObject NotEnoughGoldText = new TextObject("{=not_enough_gold_text}You do not have enough gold to pay for the book.");
        private static readonly TextObject NotEnoughPowderText = new TextObject("{=not_enough_powder_text}You do not have enough alchemical powder to learn this skill.");

        private static readonly TextObject BookDescriptionMedicine = new TextObject("{=book_description_medicine}Dive into the ancient and evolving world of healing arts. This tome explores the vast knowledge of medicinal herbs, advanced treatment techniques, and the secrets of holistic health practiced by renowned healers and shamans.");
        private static readonly TextObject BookDescriptionTactics = new TextObject("{=book_description_tactics}Unlock the art of warfare through comprehensive strategies and intricate battle formations. From the phalanx of old to modern skirmish tactics, this book provides a deep understanding of battlefield maneuvers and command strategies.");
        private static readonly TextObject BookDescriptionSteward = new TextObject("{=book_description_steward}Master the management of grand estates and the intricate logistics of resource distribution. Learn how to efficiently run households, oversee vast lands, and ensure the prosperity of your holdings through meticulous planning and administration.");
        private static readonly TextObject BookDescriptionEngineering = new TextObject("{=book_description_engineering}Embark on a journey through the marvels of siegecraft, construction, and fortification. This book covers the principles of building impregnable castles, designing war machines, and implementing the most advanced techniques in military engineering.");
        private static readonly TextObject BookDescriptionLeadership = new TextObject("{=book_description_leadership}Discover the principles that forge great leaders. This text delves into the nuances of charisma, inspiring troop morale, and the qualities that command loyalty and respect from followers. Learn how to lead with wisdom, courage, and vision.");
        private static readonly TextObject BookDescriptionCharm = new TextObject("{=book_description_charm}Explore the subtle art of charm through the lenses of etiquette, persuasion, and negotiation. This book teaches the delicate balance of social graces, effective communication, and the skills to influence and win over allies and adversaries alike.");
        private static readonly TextObject BookDescriptionTrade = new TextObject("{=book_description_trade}Navigate the complex world of economics and trade with insights into market trends, profitable trade routes, and the art of negotiation. Gain the knowledge needed to build a commercial empire and make astute financial decisions.");
        private static readonly TextObject BookDescriptionRoguery = new TextObject("{=book_description_roguery}Step into the shadows with this guide to stealth, thievery, and deception. Learn the techniques of master thieves, the art of disguise, and the cunning needed to outwit foes and navigate the underworld with finesse.");
        private static readonly TextObject BookDescriptionArcane = new TextObject("{=book_description_arcane}Unveil the mysteries of ancient and mystical arcane knowledge. This tome offers a deep dive into spellcraft, rituals, and the hidden lore of the arcane arts, drawing from the wisdom of legendary sorcerers and ancient scrolls.");


        private static ItemObject AlchemicalPowderItem => Game.Current.ObjectManager.GetObject<ItemObject>("alchemical_powder");

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent data required for this example
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            Initialize(campaignGameStarter);
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            Initialize(campaignGameStarter);
        }

        private void Initialize(CampaignGameStarter campaignGameStarter)
        {
            AddGameMenus(campaignGameStarter);
            InformationManager.DisplayMessage(new InformationMessage("EDUCATION BEHAVIOR INITIALIZED SUCCESSFULLY.", Colors.Green));
        }

        private void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption("town", "visit_scholar", "Visit Scholar", GameMenuOptionCondition, VisitScholarConsequence, false, 4, false);
            campaignGameStarter.AddGameMenuOption("town", "visit_library", "Visit Library", GameMenuOptionCondition, VisitLibraryConsequence, false, 4, false);
        }

        private bool GameMenuOptionCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            return true;
        }

        private void VisitScholarConsequence(MenuCallbackArgs args)
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Scholar",
                "What would you like to learn?",
                true,
                true,
                "Learn Alchemy",
                "Back",
                () => LearnAlchemy(),
                null
            ));
        }

        private void VisitLibraryConsequence(MenuCallbackArgs args)
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Library",
                "Welcome to the library. Here you can study to improve your skills. Choose the book you want to read:",
                true,
                true,
                "Next",
                "Medicine",
                () => ShowNextBookOptions(),
                () => ShowBookDescriptions(BookType.Medicine)
            ));
        }

        private void ShowNextBookOptions()
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Library",
                "Welcome to the library. Choose the book you want to read:",
                true,
                true,
                "Next",
                "Tactics",
                () => ShowNextBookOptionsPage2(),
                () => ShowBookDescriptions(BookType.Tactics)
            ));
        }

        private void ShowNextBookOptionsPage2()
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Library",
                "Welcome to the library. Choose the book you want to read:",
                true,
                true,
                "Next",
                "Steward",
                () => ShowNextBookOptionsPage3(),
                () => ShowBookDescriptions(BookType.Steward)
            ));
        }

        private void ShowNextBookOptionsPage3()
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Library",
                "Welcome to the library. Choose the book you want to read:",
                true,
                true,
                "Next",
                "Engineering",
                () => ShowNextBookOptionsPage4(),
                () => ShowBookDescriptions(BookType.Engineering)
            ));
        }

        private void ShowNextBookOptionsPage4()
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Library",
                "Welcome to the library. Choose the book you want to read:",
                true,
                true,
                "Next",
                "Leadership",
                () => ShowNextBookOptionsPage5(),
                () => ShowBookDescriptions(BookType.Leadership)
            ));
        }

        private void ShowNextBookOptionsPage5()
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Library",
                "Welcome to the library. Choose the book you want to read:",
                true,
                true,
                "Next",
                "Charm",
                () => ShowNextBookOptionsPage6(),
                () => ShowBookDescriptions(BookType.Charm)
            ));
        }

        private void ShowNextBookOptionsPage6()
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Library",
                "Welcome to the library. Choose the book you want to read:",
                true,
                true,
                "Next",
                "Trade",
                () => ShowNextBookOptionsPage7(),
                () => ShowBookDescriptions(BookType.Trade)
            ));
        }

        private void ShowNextBookOptionsPage7()
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Library",
                "Welcome to the library. Choose the book you want to read:",
                true,
                true,
                "Next",
                "Roguery",
                () => ShowNextBookOptionsPage8(),
                () => ShowBookDescriptions(BookType.Roguery)
            ));
        }

        private void ShowNextBookOptionsPage8()
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Library",
                "Welcome to the library. Choose the book you want to read:",
                true,
                true,
                "Leave",
                "Arcane",
                () => InformationManager.HideInquiry(),
                () => ShowBookDescriptions(BookType.Arcane)
            ));
        }

        private void ShowBookDescriptions(BookType bookType)
        {
            switch (bookType)
            {
                case BookType.Medicine:
                    InformationManager.ShowInquiry(new InquiryData(
                        "Medicine",
                        BookDescriptionMedicine.ToString(),
                        true,
                        false,
                        "Read the Book",
                        "Back",
                        () => ReadMedicineBook(),
                        () => VisitLibraryConsequence(null)
                    ));
                    break;
                case BookType.Tactics:
                    InformationManager.ShowInquiry(new InquiryData(
                        "Tactics",
                        BookDescriptionTactics.ToString(),
                        true,
                        false,
                        "Read the Book",
                        "Back",
                        () => ReadTacticsBook(),
                        () => VisitLibraryConsequence(null)
                    ));
                    break;
                case BookType.Steward:
                    InformationManager.ShowInquiry(new InquiryData(
                        "Steward",
                        BookDescriptionSteward.ToString(),
                        true,
                        false,
                        "Read the Book",
                        "Back",
                        () => ReadStewardBook(),
                        () => VisitLibraryConsequence(null)
                    ));
                    break;
                case BookType.Engineering:
                    InformationManager.ShowInquiry(new InquiryData(
                        "Engineering",
                        BookDescriptionEngineering.ToString(),
                        true,
                        false,
                        "Read the Book",
                        "Back",
                        () => ReadEngineeringBook(),
                        () => VisitLibraryConsequence(null)
                    ));
                    break;
                case BookType.Leadership:
                    InformationManager.ShowInquiry(new InquiryData(
                        "Leadership",
                        BookDescriptionLeadership.ToString(),
                        true,
                        false,
                        "Read the Book",
                        "Back",
                        () => ReadLeadershipBook(),
                        () => VisitLibraryConsequence(null)
                    ));
                    break;
                case BookType.Charm:
                    InformationManager.ShowInquiry(new InquiryData(
                        "Charm",
                        BookDescriptionCharm.ToString(),
                        true,
                        false,
                        "Read the Book",
                        "Back",
                        () => ReadCharmBook(),
                        () => VisitLibraryConsequence(null)
));
                    break;
                case BookType.Trade:
                    InformationManager.ShowInquiry(new InquiryData(
                        "Trade",
                        BookDescriptionTrade.ToString(),
                        true,
                        false,
                        "Read the Book",
                        "Back",
                        () => ReadTradeBook(),
                        () => VisitLibraryConsequence(null)
                    ));
                    break;
                case BookType.Roguery:
                    InformationManager.ShowInquiry(new InquiryData(
                        "Roguery",
                        BookDescriptionRoguery.ToString(),
                        true,
                        false,
                        "Read the Book",
                        "Back",
                        () => ReadRogueryBook(),
                        () => VisitLibraryConsequence(null)
                    ));
                    break;
                case BookType.Arcane:
                    if (Hero.MainHero.CurrentSettlement.StringId == "town_EM1")
                    {
                        InformationManager.ShowInquiry(new InquiryData(
                            "Arcane",
                            BookDescriptionArcane.ToString(),
                            true,
                            false,
                            "Read the Book",
                            "Back",
                            () => ReadArcaneBook(),
                            () => VisitLibraryConsequence(null)
                        ));
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage("Arcane books are only available in the Arcane Library.", Colors.Red));
                        VisitLibraryConsequence(null);
                    }
                    break;
            }
        }

        private void LearnAlchemy()
        {
            var hero = Hero.MainHero;
            int currentSkillLevel = hero.GetSkillValue(RFSkills.Alchemy);
            int dynamicGoldCost = CalculateDynamicCost(currentSkillLevel);
            int dynamicPowderAmount = CalculateDynamicPowderAmount(currentSkillLevel);

            if (hero.Gold >= dynamicGoldCost && hero.PartyBelongedTo.ItemRoster.GetItemNumber(AlchemicalPowderItem) >= dynamicPowderAmount)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, hero, dynamicGoldCost, false);
                hero.PartyBelongedTo.ItemRoster.AddToCounts(AlchemicalPowderItem, -dynamicPowderAmount);
                hero.AddSkillXp(RFSkills.Alchemy, CalculateDynamicXpAmount(currentSkillLevel));

                InformationManager.ShowInquiry(new InquiryData(
                    EducationTitleText.ToString(),
                    "You study with the scholar and improve your Alchemy skills.",
                    true,
                    false,
                    ContinueText.ToString(),
                    null,
                    null,
                    null
                ));
                InformationManager.DisplayMessage(new InformationMessage($"Your Alchemy skill has improved! You spent {dynamicGoldCost} gold and used {dynamicPowderAmount} alchemical powder.", Colors.Green));
            }
            else if (hero.Gold < dynamicGoldCost)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    EducationTitleText.ToString(),
                    NotEnoughGoldText.ToString(),
                    true,
                    false,
                    ContinueText.ToString(),
                    null,
                    null,
                    null
                ));
                InformationManager.DisplayMessage(new InformationMessage("You do not have enough gold to pay the scholar.", Colors.Red));
            }
            else
            {
                InformationManager.ShowInquiry(new InquiryData(
                    EducationTitleText.ToString(),
                    NotEnoughPowderText.ToString(),
                    true,
                    false,
                    ContinueText.ToString(),
                    null,
                    null,
                    null
                ));
                InformationManager.DisplayMessage(new InformationMessage("You do not have enough alchemical powder to learn this skill.", Colors.Red));
            }
        }

        private void ReadMedicineBook() => ReadBook(DefaultSkills.Medicine);
        private void ReadTacticsBook() => ReadBook(DefaultSkills.Tactics);
        private void ReadStewardBook() => ReadBook(DefaultSkills.Steward);
        private void ReadEngineeringBook() => ReadBook(DefaultSkills.Engineering);
        private void ReadLeadershipBook() => ReadBook(DefaultSkills.Leadership);
        private void ReadCharmBook() => ReadBook(DefaultSkills.Charm);
        private void ReadTradeBook() => ReadBook(DefaultSkills.Trade);
        private void ReadRogueryBook() => ReadBook(DefaultSkills.Roguery);
        private void ReadArcaneBook() => ReadBook(RFSkills.Arcane);

        private void ReadBook(SkillObject skill)
        {
            var hero = Hero.MainHero;
            int currentSkillLevel = hero.GetSkillValue(skill);
            int dynamicGoldCost = CalculateDynamicCost(currentSkillLevel);

            if (hero.Gold >= dynamicGoldCost)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, hero, dynamicGoldCost, false);
                hero.AddSkillXp(skill, CalculateDynamicXpAmount(currentSkillLevel));

                InformationManager.ShowInquiry(new InquiryData(
                    EducationTitleText.ToString(),
                    $"You study the {skill.Name} book and improve your skills.",
                    true,
                    false,
                    ContinueText.ToString(),
                    null,
                    null,
                    null
                ));
                InformationManager.DisplayMessage(new InformationMessage($"Your {skill.Name} skill has improved! You spent {dynamicGoldCost} gold.", Colors.Green));
            }
            else
            {
                InformationManager.ShowInquiry(new InquiryData(
                    EducationTitleText.ToString(),
                    NotEnoughGoldText.ToString(),
                    true,
                    false,
                    ContinueText.ToString(),
                    null,
                    null,
                    null
                ));
                InformationManager.DisplayMessage(new InformationMessage("You do not have enough gold to buy the book.", Colors.Red));
            }
        }

        private int CalculateDynamicCost(int skillLevel)
        {
            return BaseGoldCost + (skillLevel * 10);
        }

        private int CalculateDynamicPowderAmount(int skillLevel)
        {
            return BasePowderAmount + skillLevel; // Example calculation
        }

        private int CalculateDynamicXpAmount(int skillLevel)
        {
            return BaseSkillXpAmount + (skillLevel * 2);
        }

        public enum BookType
        {
            Medicine,
            Tactics,
            Steward,
            Engineering,
            Leadership,
            Charm,
            Trade,
            Roguery,
            Arcane
        }
    }
}
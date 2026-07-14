using System.Collections.Generic;
using System.Linq;
using Helpers;
using RealmsForgotten.Career;
using RealmsForgotten.CustomSkills;
using RealmsForgotten.RFReligions.Helper;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.RFReligions.Behavior;

internal class ClericConversionDialogueBehavior : CampaignBehaviorBase
{
    private const int MinimumFaith = 50;
    private const float AttemptCooldownDays = 7f;
    private readonly Dictionary<Hero, CampaignTime> _nextAttemptTime = new();
    private PersuasionTask _persuasionTask = new(0);

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        Campaign.Current.ConversationManager.AddDialogFlow(CreateConversionDialogFlow(), this);
    }

    private DialogFlow CreateConversionDialogFlow()
    {
        DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("hero_main_options", 125)
            .PlayerLine("I would speak with you about the true faith.")
            .Condition(CanAttemptConversion)
            .Consequence(StartPersuasion)
            .GotoDialogState("rf_cleric_conversion_start");

        dialogFlow.AddDialogLine(
            "rf_cleric_conversion_response",
            "rf_cleric_conversion_start",
            "rf_cleric_conversion_options_1",
            "Faith is not changed by a single sentence. Speak, and I will judge your words.",
            () => !ConversationManager.GetPersuasionProgressSatisfied(),
            null,
            this);

        dialogFlow.AddPlayerLine(
            "rf_cleric_conversion_option_1",
            "rf_cleric_conversion_options_1",
            "rf_cleric_conversion_outcome_1",
            "{=!}{RF_CLERIC_CONVERT_ATTEMPT_1}",
            () => SetPersuasionOptionText(0, "RF_CLERIC_CONVERT_ATTEMPT_1"),
            () => BlockPersuasionOption(0),
            this,
            100,
            null,
            () => _persuasionTask.Options.ElementAt(0));

        dialogFlow.AddPlayerLine(
            "rf_cleric_conversion_option_1b",
            "rf_cleric_conversion_options_1",
            "rf_cleric_conversion_outcome_1",
            "{=!}{RF_CLERIC_CONVERT_ATTEMPT_1B}",
            () => SetPersuasionOptionText(1, "RF_CLERIC_CONVERT_ATTEMPT_1B"),
            () => BlockPersuasionOption(1),
            this,
            100,
            null,
            () => _persuasionTask.Options.ElementAt(1));

        dialogFlow.AddDialogLine(
            "rf_cleric_conversion_stage_2",
            "rf_cleric_conversion_outcome_1",
            "rf_cleric_conversion_options_2",
            "Your words have weight. Continue, if your faith can bear scrutiny.",
            () => !ConversationManager.GetPersuasionProgressSatisfied(),
            null,
            this);

        dialogFlow.AddDialogLine(
            "rf_cleric_conversion_early_success_1",
            "rf_cleric_conversion_outcome_1",
            "close_window",
            "Your words strike deeper than I expected. I will walk this new road.",
            ConversationManager.GetPersuasionProgressSatisfied,
            () => CompleteConversion(true),
            this);

        dialogFlow.AddPlayerLine(
            "rf_cleric_conversion_option_2",
            "rf_cleric_conversion_options_2",
            "rf_cleric_conversion_outcome_2",
            "{=!}{RF_CLERIC_CONVERT_ATTEMPT_2}",
            () => SetPersuasionOptionText(2, "RF_CLERIC_CONVERT_ATTEMPT_2"),
            () => BlockPersuasionOption(2),
            this,
            100,
            null,
            () => _persuasionTask.Options.ElementAt(2));

        dialogFlow.AddPlayerLine(
            "rf_cleric_conversion_option_2b",
            "rf_cleric_conversion_options_2",
            "rf_cleric_conversion_outcome_2",
            "{=!}{RF_CLERIC_CONVERT_ATTEMPT_2B}",
            () => SetPersuasionOptionText(3, "RF_CLERIC_CONVERT_ATTEMPT_2B"),
            () => BlockPersuasionOption(3),
            this,
            100,
            null,
            () => _persuasionTask.Options.ElementAt(3));

        dialogFlow.AddDialogLine(
            "rf_cleric_conversion_stage_3",
            "rf_cleric_conversion_outcome_2",
            "rf_cleric_conversion_options_3",
            "One final answer, then. Why should I abandon the path I know?",
            () => !ConversationManager.GetPersuasionProgressSatisfied(),
            null,
            this);

        dialogFlow.AddDialogLine(
            "rf_cleric_conversion_early_success_2",
            "rf_cleric_conversion_outcome_2",
            "close_window",
            "Enough. I hear the truth in this. Let your faith be mine.",
            ConversationManager.GetPersuasionProgressSatisfied,
            () => CompleteConversion(true),
            this);

        dialogFlow.AddPlayerLine(
            "rf_cleric_conversion_option_3",
            "rf_cleric_conversion_options_3",
            "rf_cleric_conversion_outcome_3",
            "{=!}{RF_CLERIC_CONVERT_ATTEMPT_3}",
            () => SetPersuasionOptionText(4, "RF_CLERIC_CONVERT_ATTEMPT_3"),
            () => BlockPersuasionOption(4),
            this,
            100,
            null,
            () => _persuasionTask.Options.ElementAt(4));

        dialogFlow.AddPlayerLine(
            "rf_cleric_conversion_option_3b",
            "rf_cleric_conversion_options_3",
            "rf_cleric_conversion_outcome_3",
            "{=!}{RF_CLERIC_CONVERT_ATTEMPT_3B}",
            () => SetPersuasionOptionText(5, "RF_CLERIC_CONVERT_ATTEMPT_3B"),
            () => BlockPersuasionOption(5),
            this,
            100,
            null,
            () => _persuasionTask.Options.ElementAt(5));


        dialogFlow.AddDialogLine(
            "rf_cleric_conversion_success",
            "rf_cleric_conversion_outcome_3",
            "close_window",
            "I will walk this new road. Let your faith be mine.",
            ConversationManager.GetPersuasionProgressSatisfied,
            () => CompleteConversion(true),
            this);

        dialogFlow.AddDialogLine(
            "rf_cleric_conversion_failure",
            "rf_cleric_conversion_outcome_3",
            "close_window",
            "No. My faith remains my own.",
            () => !ConversationManager.GetPersuasionProgressSatisfied(),
            () => CompleteConversion(false),
            this);

        return dialogFlow;
    }

    private bool CanAttemptConversion()
    {
        Hero target = Hero.OneToOneConversationHero;
        if (target == null || target == Hero.MainHero || !target.IsAlive)
            return false;

        if (PlayerCareerExtension.GetCareer()?.StringId != "cleric")
            return false;

        if (Hero.MainHero.GetSkillValue(RFSkills.Faith) < MinimumFaith)
            return false;

        if (ReligionBehavior.Instance == null)
            return false;

        if (!ReligionBehavior.Instance._heroes.TryGetValue(Hero.MainHero, out var playerReligion))
            return false;

        if (!ReligionBehavior.Instance._heroes.TryGetValue(target, out var targetReligion))
            return false;

        if (playerReligion.Religion == targetReligion.Religion)
            return false;

        return !_nextAttemptTime.TryGetValue(target, out CampaignTime nextAttempt) || CampaignTime.Now >= nextAttempt;
    }

    private void StartPersuasion()
    {
        Hero target = Hero.OneToOneConversationHero;
        _persuasionTask = GetPersuasionTask(target);
        ConversationManager.StartPersuasion(3f, 1f, 1f, 1f, 1f, 0f, GetPersuasionDifficulty(target));
    }

    private void CompleteConversion(bool success)
    {
        ConversationManager.EndPersuasion();

        Hero target = Hero.OneToOneConversationHero;
        if (target == null || ReligionBehavior.Instance == null)
            return;

        _nextAttemptTime[target] = CampaignTime.DaysFromNow(AttemptCooldownDays);

        if (!ReligionBehavior.Instance._heroes.TryGetValue(Hero.MainHero, out var playerReligion))
            return;

        if (!ReligionBehavior.Instance._heroes.TryGetValue(target, out var targetReligion))
            return;

        if (success)
        {
            TextObject playerReligionName = ReligionUIHelper.GetReligionName(playerReligion.Religion);
            targetReligion.ConvertReligion(playerReligion.Religion);
            targetReligion.AddDevotion(25f, target);
            Hero.MainHero.AddSkillXp(RFSkills.Faith, 25f);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, target, 5);

            InformationManager.DisplayMessage(new InformationMessage(
                $"{target.Name} accepted {playerReligionName}.", Colors.Green));
        }
        else
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, target, -2);
            TextObject targetReligionName = ReligionUIHelper.GetReligionName(targetReligion.Religion);
            InformationManager.DisplayMessage(new InformationMessage(
                $"{target.Name} remains loyal to {targetReligionName}.", Colors.Red));
        }
    }

    private PersuasionTask GetPersuasionTask(Hero target)
    {
        PersuasionTask persuasionTask = new(0)
        {
            FinalFailLine = new TextObject("No. My faith remains my own."),
            TryLaterLine = null,
            SpokenLine = new TextObject("Faith is not changed by a single sentence. Speak, and I will judge your words.")
        };

        PersuasionArgumentStrength strength = GetArgumentStrength(target);

        persuasionTask.AddOptionToTask(new PersuasionOptionArgs(
            DefaultSkills.Charm,
            DefaultTraits.Mercy,
            TraitEffect.Positive,
            strength,
            false,
            new TextObject("Appeal to mercy and the good their soul could still do."),
            null,
            false,
            false,
            false));

        persuasionTask.AddOptionToTask(new PersuasionOptionArgs(
            RFSkills.Faith,
            DefaultTraits.Honor,
            TraitEffect.Positive,
            strength,
            false,
            new TextObject("Speak of doctrine, sacred duty, and the signs of the divine."),
            null,
            false,
            false,
            false));

        persuasionTask.AddOptionToTask(new PersuasionOptionArgs(
            DefaultSkills.Charm,
            DefaultTraits.Calculating,
            TraitEffect.Positive,
            strength,
            false,
            new TextObject("Argue that shared faith brings peace between houses and safer roads for their people."),
            null,
            false,
            false,
            false));

        persuasionTask.AddOptionToTask(new PersuasionOptionArgs(
            RFSkills.Faith,
            DefaultTraits.Generosity,
            TraitEffect.Positive,
            strength,
            false,
            new TextObject("Offer guidance, protection, and a place among the faithful."),
            null,
            false,
            false,
            false));

        persuasionTask.AddOptionToTask(new PersuasionOptionArgs(
            DefaultSkills.Charm,
            DefaultTraits.Honor,
            TraitEffect.Positive,
            strength,
            false,
            new TextObject("Call on their honor: a ruler should seek truth even when tradition resists it."),
            null,
            false,
            false,
            false));

        persuasionTask.AddOptionToTask(new PersuasionOptionArgs(
            RFSkills.Faith,
            DefaultTraits.Mercy,
            TraitEffect.Positive,
            strength,
            false,
            new TextObject("Promise that the new faith will temper judgment with mercy, not merely demand obedience."),
            null,
            false,
            false,
            false));

        return persuasionTask;
    }

    private PersuasionDifficulty GetPersuasionDifficulty(Hero target)
    {
        int faith = Hero.MainHero.GetSkillValue(RFSkills.Faith);
        int charm = Hero.MainHero.GetSkillValue(DefaultSkills.Charm);
        float relation = target?.GetRelationWithPlayer() ?? 0f;
        float score = faith + charm * 0.6f + relation;

        if (target?.IsLord == true)
            score -= 40f;

        if (target?.Clan?.Kingdom?.Leader == target)
            score -= 45f;

        if (score >= 260f)
            return PersuasionDifficulty.Easy;

        if (score >= 190f)
            return PersuasionDifficulty.Medium;

        if (score >= 120f)
            return PersuasionDifficulty.MediumHard;

        return PersuasionDifficulty.Hard;
    }

    private PersuasionArgumentStrength GetArgumentStrength(Hero target)
    {
        int faith = Hero.MainHero.GetSkillValue(RFSkills.Faith);
        int charm = Hero.MainHero.GetSkillValue(DefaultSkills.Charm);
        float relation = target.GetRelationWithPlayer();
        float score = faith + charm * 0.6f + relation;

        if (target.IsLord)
            score -= 35f;

        if (target.Clan?.Kingdom?.Leader == target)
            score -= 40f;

        if (score >= 260f)
            return PersuasionArgumentStrength.VeryEasy;

        if (score >= 190f)
            return PersuasionArgumentStrength.Easy;

        if (score >= 120f)
            return PersuasionArgumentStrength.Normal;

        return PersuasionArgumentStrength.Hard;
    }

    private bool SetPersuasionOptionText(int index, string variableName)
    {
        if (_persuasionTask == null || _persuasionTask.Options.Count <= index)
            return false;

        TextObject textObject = new("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
        textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_persuasionTask.Options.ElementAt(index), false));
        textObject.SetTextVariable("PERSUASION_OPTION_LINE", _persuasionTask.Options.ElementAt(index).Line);
        MBTextManager.SetTextVariable(variableName, textObject);
        return true;
    }

    private void BlockPersuasionOption(int index)
    {
        if (_persuasionTask != null && _persuasionTask.Options.Count > index)
            _persuasionTask.Options[index].BlockTheOption(true);
    }
}

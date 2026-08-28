using System;
using System.Collections.Generic;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;
using TaleWorlds.Library;

namespace RealmsForgotten.Quest
{
    /// <summary>
    /// Pagina MCM da ferramenta de capitulos (filmagem). So faz algo com o arquivo-flag
    /// de dev presente (RFQuestChapterJumper checa) — em maquina de jogador o botao
    /// responde "desativada". Console equivalente: rf.quest.jump N.
    /// </summary>
    public sealed class RFQuestChapterMcm : AttributeGlobalSettings<RFQuestChapterMcm>
    {
        public override string Id => "RF_QuestChapters";
        public override string DisplayName => "RF Quest Chapters";
        public override string FolderName => "RF_QuestChapters";
        public override string FormatType => "json";

        [SettingPropertyDropdown("{=rf_qc_chapter}Chapter", Order = 0, RequireRestart = false,
            HintText = "{=rf_qc_chapter_hint}Which main-quest chapter to start when pressing the button below.")]
        [SettingPropertyGroup("{=rf_qc_group}Chapter Jump (dev/filming)")]
        public Dropdown<string> Chapter { get; set; } = new Dropdown<string>(new List<string>(RFQuestChapterJumper.ChapterNames), 0);

        [SettingPropertyButton("{=rf_qc_jump}Start selected chapter", Content = "{=rf_qc_jump_btn}Jump", Order = 1, RequireRestart = false,
            HintText = "{=rf_qc_jump_hint}Cancels the active RF main quests and starts the selected chapter. FILMING TOOL — use on a COPY of your save. Requires the local dev flag file; inert on player machines.")]
        [SettingPropertyGroup("{=rf_qc_group}Chapter Jump (dev/filming)")]
        public Action JumpAction { get; set; } = () =>
        {
            string result = RFQuestChapterJumper.JumpTo(Instance?.Chapter?.SelectedIndex ?? -1);
            InformationManager.DisplayMessage(new InformationMessage("[RF Chapters] " + result, Colors.Yellow));
        };
    }
}

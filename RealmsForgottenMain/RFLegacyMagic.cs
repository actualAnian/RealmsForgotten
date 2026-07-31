namespace RealmsForgotten
{
    /// <summary>
    /// Interruptor do motor de magia LEGADO do RealmsForgotten — o de
    /// musket/cartridge/bullets, em que o cajado era uma arma de fogo e o feitiço
    /// era a munição.
    ///
    /// Existe porque o motor de magia passou a ser o do SOTOR (módulo RF_Magic).
    /// Manter os dois somando efeito ao mesmo tempo daria dano duplicado, XP
    /// duplicado e dois recursos de mana concorrentes. Este flag desliga o legado
    /// num lugar só, sem apagar nada: o código continua no repositório e volta
    /// ligando o interruptor.
    ///
    /// Efeito colateral desejado: com o legado desligado, as classes de arma
    /// Musket / Cartridge / Pistol ficam LIVRES para armas de fogo de verdade no
    /// futuro, em vez de serem o veículo dos feitiços.
    ///
    /// ── O QUE NUNCA ENTRA NESTE GATE ────────────────────────────────────────
    /// Requisito explícito do autor. Estas mecânicas são identidade do mod e
    /// continuam funcionando com <see cref="Enabled" /> em false:
    ///
    ///   • <c>NecromancerStaffMissionBehavior</c> — invocar undead
    ///     (necromancer_staff, witch_skull) E o ramo <c>isDruidWand</c>, que
    ///     invoca werewolf/werebear com o druid_necromancer_staff.
    ///   • <c>GandalfStaffMissionBehavior</c> — curar.
    ///   • <c>MeteorMissionLogic</c> — o meteoro. É conteúdo de QUEST (a Winged
    ///     Witch depende dele via MeteorLogic.FireMeteor). Regra do projeto:
    ///     conteúdo de quest nunca entra em gate de sistema.
    ///   • <c>RFEnchantedWeaponsMissionBehavior</c>, <c>WeaponParticlesBehavior</c>,
    ///     <c>MagicEffectsBehavior</c> — servem a tabela INTEIRA de
    ///     weapons_effects.xml, que inclui as 15 armas encantadas (espadas
    ///     flamejantes, flechas mágicas, lâminas envenenadas). Gateá-las apagaria
    ///     armas físicas que não têm nada a ver com o sistema de feitiços.
    ///
    /// ── O QUE ENTRA ─────────────────────────────────────────────────────────
    ///   • <c>SpellAmmoMissionBehavior</c> / <c>RFSpellAmmo</c> — seleção e
    ///     munição de feitiço pelo cajado-musket.
    ///   • <c>GetCartridgeSkillPatch</c> — Cartridge/Musket passando a contar como
    ///     skill de magia.
    ///   • <c>RFCombatXpModel</c> — XP de magia por acerto de cartridge.
    ///   • <c>RFAgentStatCalculateModel</c> — bônus de agente por musket/cartridge.
    ///   • <c>DamagePatch</c> / <c>RFAgentApplyDamageModel</c> — dano de cartridge
    ///     tratado como dano mágico.
    /// </summary>
    public static class RFLegacyMagic
    {
        /// <summary>
        /// FASE 5b: <c>false</c> — o motor de magia agora é o do SOTOR (RF_Magic).
        ///
        /// Reverter é trocar por <c>true</c>: nada foi apagado. As mecânicas listadas
        /// acima como intocáveis (undead, werebear do druida, cura, meteoro, armas
        /// encantadas) continuam funcionando nos dois estados.
        /// </summary>
        public static bool Enabled = false;
    }
}

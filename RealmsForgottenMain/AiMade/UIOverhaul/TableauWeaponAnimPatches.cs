using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;

namespace RealmsForgotten.AiMade.UIOverhaul
{
    /// <summary>
    /// Animações "de verdade" no boneco 3D das telas de UI (party screen, inventário,
    /// enciclopédia...): sacar a arma ao trocar de equipamento e poses idle com a arma na
    /// mão, por classe de arma — em vez do idle genérico de braços caídos.
    ///
    /// Origem: feature do ArtemsBetterUIVisuals (1.2.x), que reimplementava o
    /// CharacterTableau inteiro. No 1.4.8 o tableau vanilla já tem quase tudo; o que ele NÃO
    /// tem é exatamente isto: o bloco de equip só anima luvas/roupa
    /// (act_inventory_glove/cloth_equip) e o idle é sempre act_inventory_idle. Estes dois
    /// postfixes cirúrgicos completam a lacuna sem clonar classe nenhuma.
    ///
    /// Segurança (regra do canário + RF_Races): TODO SetAgentActionChannel é precedido de
    /// MBActionSet.CheckActionAnimationClipExists no action set do próprio boneco — raça sem
    /// o clipe simplesmente não anima, nunca indexa animação inexistente. Os 93 nomes de
    /// ação usados existem no action_types.xml 1.4.8 (verificado 2026-08-18).
    /// </summary>
    internal static class TableauWeaponAnims
    {
        // ---- caches de ação (mesmas do ArtemsBetterUIVisuals, nomes verificados) ----
        private static readonly ActionIndexCache EquipSword = ActionIndexCache.Create("act_equip_sword");
        private static readonly ActionIndexCache UnequipSword = ActionIndexCache.Create("act_unequip_sword");
        private static readonly ActionIndexCache Equip2h = ActionIndexCache.Create("act_equip_2h");
        private static readonly ActionIndexCache Unequip2h = ActionIndexCache.Create("act_unequip_2h");
        private static readonly ActionIndexCache Equip2hAxe = ActionIndexCache.Create("act_equip_2h_axe");
        private static readonly ActionIndexCache Unequip2hAxe = ActionIndexCache.Create("act_unequip_2h_axe");
        private static readonly ActionIndexCache EquipAxe = ActionIndexCache.Create("act_equip_axe_left_hip");
        private static readonly ActionIndexCache UnequipAxe = ActionIndexCache.Create("act_unequip_axe_left_hip");
        private static readonly ActionIndexCache EquipSpear = ActionIndexCache.Create("act_equip_spear");
        private static readonly ActionIndexCache UnequipSpear = ActionIndexCache.Create("act_unequip_spear");
        private static readonly ActionIndexCache EquipDagger = ActionIndexCache.Create("act_equip_dagger_front_left");
        private static readonly ActionIndexCache UnequipDagger = ActionIndexCache.Create("act_unequip_dagger_front_left");
        private static readonly ActionIndexCache EquipBow = ActionIndexCache.Create("act_equip_bow_left_hip");
        private static readonly ActionIndexCache UnequipBow = ActionIndexCache.Create("act_unequip_bow_left_hip");
        private static readonly ActionIndexCache EquipCrossbow = ActionIndexCache.Create("act_equip_crossbow");
        private static readonly ActionIndexCache UnequipCrossbow = ActionIndexCache.Create("act_unequip_crossbow");
        private static readonly ActionIndexCache EquipJavelin = ActionIndexCache.Create("act_equip_javelin");
        private static readonly ActionIndexCache UnequipJavelin = ActionIndexCache.Create("act_unequip_javelin");
        private static readonly ActionIndexCache EquipThrowingAxe = ActionIndexCache.Create("act_equip_throwing_axe");
        private static readonly ActionIndexCache UnequipThrowingAxe = ActionIndexCache.Create("act_unequip_throwing_axe");
        private static readonly ActionIndexCache EquipStone = ActionIndexCache.Create("act_equip_stone");

        private static readonly ActionIndexCache[] OneHandedIdles =
        {
            ActionIndexCache.Create("act_idle_1h_with_shield_1"),
            ActionIndexCache.Create("act_idle_1h_with_shield_2"),
            ActionIndexCache.Create("act_idle_1h_with_shield_3"),
            ActionIndexCache.Create("act_idle_1h_with_shield_4"),
            ActionIndexCache.Create("act_idle_1h_with_shield_5"),
            ActionIndexCache.Create("act_idle_1h_with_shield_6"),
        };

        private static readonly ActionIndexCache[] TwoHandedIdles =
        {
            ActionIndexCache.Create("act_idle_2h_1"),
            ActionIndexCache.Create("act_idle_2h_2"),
            ActionIndexCache.Create("act_idle_2h_3"),
            ActionIndexCache.Create("act_idle_2h_4"),
            ActionIndexCache.Create("act_idle_2h_5"),
            ActionIndexCache.Create("act_idle_2h_6"),
        };

        private static readonly ActionIndexCache[] SpearIdles =
        {
            ActionIndexCache.Create("act_idle_spear_1"),
            ActionIndexCache.Create("act_idle_spear_2"),
            ActionIndexCache.Create("act_idle_spear_3"),
            ActionIndexCache.Create("act_idle_spear_4"),
            ActionIndexCache.Create("act_idle_spear_5"),
            ActionIndexCache.Create("act_idle_spear_6"),
        };

        private static readonly ActionIndexCache[] BowIdles =
        {
            ActionIndexCache.Create("act_idle_bow_1"),
            ActionIndexCache.Create("act_idle_bow_2"),
            ActionIndexCache.Create("act_idle_bow_3"),
            ActionIndexCache.Create("act_idle_bow_4"),
        };

        private static readonly ActionIndexCache[] CrossbowIdles =
        {
            ActionIndexCache.Create("act_idle_crossbow_1"),
            ActionIndexCache.Create("act_idle_crossbow_2"),
            ActionIndexCache.Create("act_idle_crossbow_3"),
        };

        private static readonly ActionIndexCache[] HorseSpearIdles =
        {
            ActionIndexCache.Create("act_rider_idle_spear_1"),
            ActionIndexCache.Create("act_rider_idle_spear_2"),
            ActionIndexCache.Create("act_rider_idle_spear_3"),
            ActionIndexCache.Create("act_rider_idle_spear_4"),
        };

        private static readonly ActionIndexCache[] HorseBowIdles =
        {
            ActionIndexCache.Create("act_rider_idle_bow_1"),
            ActionIndexCache.Create("act_rider_idle_bow_2"),
            ActionIndexCache.Create("act_rider_idle_bow_3"),
            ActionIndexCache.Create("act_rider_idle_bow_4"),
        };

        private static readonly ActionIndexCache[] HorseCrossbowIdles =
        {
            ActionIndexCache.Create("act_rider_idle_crossbow_1"),
            ActionIndexCache.Create("act_rider_idle_crossbow_2"),
            ActionIndexCache.Create("act_rider_idle_crossbow_3"),
            ActionIndexCache.Create("act_rider_idle_crossbow_4"),
        };

        private static readonly ActionIndexCache[] CamelSpearIdles =
        {
            ActionIndexCache.Create("act_camel_rider_idle_spear_1"),
            ActionIndexCache.Create("act_camel_rider_idle_spear_2"),
            ActionIndexCache.Create("act_camel_rider_idle_spear_3"),
            ActionIndexCache.Create("act_camel_rider_idle_spear_4"),
            ActionIndexCache.Create("act_camel_rider_idle_spear_5"),
        };

        private static readonly ActionIndexCache[] CamelBowIdles =
        {
            ActionIndexCache.Create("act_camel_rider_idle_bow_1"),
            ActionIndexCache.Create("act_camel_rider_idle_bow_2"),
            ActionIndexCache.Create("act_camel_rider_idle_bow_3"),
            ActionIndexCache.Create("act_camel_rider_idle_bow_4"),
            ActionIndexCache.Create("act_camel_rider_idle_bow_5"),
        };

        private static readonly ActionIndexCache[] CamelCrossbowIdles =
        {
            ActionIndexCache.Create("act_camel_rider_idle_crossbow_1"),
            ActionIndexCache.Create("act_camel_rider_idle_crossbow_2"),
            ActionIndexCache.Create("act_camel_rider_idle_crossbow_3"),
            ActionIndexCache.Create("act_camel_rider_idle_crossbow_4"),
            ActionIndexCache.Create("act_camel_rider_idle_crossbow_5"),
        };

        /// <summary>Todas as poses que ESTE patch pode ter tocado — para reconhecer e limpar.</summary>
        private static readonly List<ActionIndexCache> AllIdleActions = BuildAllIdleActions();

        private static List<ActionIndexCache> BuildAllIdleActions()
        {
            var all = new List<ActionIndexCache>();
            all.AddRange(OneHandedIdles);
            all.AddRange(TwoHandedIdles);
            all.AddRange(SpearIdles);
            all.AddRange(BowIdles);
            all.AddRange(CrossbowIdles);
            all.AddRange(HorseSpearIdles);
            all.AddRange(HorseBowIdles);
            all.AddRange(HorseCrossbowIdles);
            all.AddRange(CamelSpearIdles);
            all.AddRange(CamelBowIdles);
            all.AddRange(CamelCrossbowIdles);
            return all;
        }

        internal static bool IsOurIdleAction(ActionIndexCache action)
        {
            for (int i = 0; i < AllIdleActions.Count; i++)
            {
                if (AllIdleActions[i] == action)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Visual em que o empunhar foi aplicado por ultimo, por tableau. O refresh do
        /// tableau recria o AgentVisuals reaproveitando pose do esqueleto — quando a
        /// instancia muda, o empunhar precisa ser re-pedido.
        /// </summary>
        internal static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CharacterTableau, AgentVisuals> LastWieldedVisuals =
            new System.Runtime.CompilerServices.ConditionalWeakTable<CharacterTableau, AgentVisuals>();

        private static readonly ActionIndexCache MountIdleHorse = ActionIndexCache.Create("act_hero_mount_idle_horse");
        private static readonly ActionIndexCache MountIdleCamel = ActionIndexCache.Create("act_hero_mount_idle_camel");
        private static readonly ActionIndexCache InventoryIdle = ActionIndexCache.Create("act_inventory_idle");
        private static readonly ActionIndexCache InventoryIdleStart = ActionIndexCache.Create("act_inventory_idle_start");

        // ---- acesso aos privados do CharacterTableau (nomes verificados no 1.4.8) ----
        private static readonly AccessTools.FieldRef<CharacterTableau, Equipment> EquipmentRef =
            AccessTools.FieldRefAccess<CharacterTableau, Equipment>("_equipment");
        private static readonly AccessTools.FieldRef<CharacterTableau, AgentVisuals> AgentVisualsRef =
            AccessTools.FieldRefAccess<CharacterTableau, AgentVisuals>("_agentVisuals");
        private static readonly AccessTools.FieldRef<CharacterTableau, AgentVisuals> MountVisualsRef =
            AccessTools.FieldRefAccess<CharacterTableau, AgentVisuals>("_mountVisuals");
        private static readonly AccessTools.FieldRef<CharacterTableau, bool> EquipAnimActiveRef =
            AccessTools.FieldRefAccess<CharacterTableau, bool>("_isEquipmentAnimActive");
        private static readonly AccessTools.FieldRef<CharacterTableau, float> AnimationGapRef =
            AccessTools.FieldRefAccess<CharacterTableau, float>("_animationGap");
        private static readonly AccessTools.FieldRef<CharacterTableau, float> AnimationThresholdRef =
            AccessTools.FieldRefAccess<CharacterTableau, float>("_animationFrequencyThreshold");
        private static readonly AccessTools.FieldRef<CharacterTableau, MBActionSet> ActionSetRef =
            AccessTools.FieldRefAccess<CharacterTableau, MBActionSet>("_characterActionSet");
        private static readonly AccessTools.FieldRef<CharacterTableau, int> RightHandIndexRef =
            AccessTools.FieldRefAccess<CharacterTableau, int>("_rightHandEquipmentIndex");
        private static readonly AccessTools.FieldRef<CharacterTableau, int> LeftHandIndexRef =
            AccessTools.FieldRefAccess<CharacterTableau, int>("_leftHandEquipmentIndex");
        private static readonly AccessTools.FieldRef<CharacterTableau, bool> MountSwappedRef =
            AccessTools.FieldRefAccess<CharacterTableau, bool>("_isCharacterMountPlacesSwapped");
        private static readonly AccessTools.FieldRef<CharacterTableau, bool> EquipIndicesDirtyRef =
            AccessTools.FieldRefAccess<CharacterTableau, bool>("_isEquipmentIndicesDirty");

        /// <summary>Toca a ação só se o action set do boneco tiver o clipe (canário).</summary>
        internal static bool TryPlay(CharacterTableau tableau, AgentVisuals visuals, int channel, ActionIndexCache action, float blendPeriod)
        {
            try
            {
                MBActionSet actionSet = ActionSetRef(tableau);
                if (!actionSet.IsValid || !MBActionSet.CheckActionAnimationClipExists(actionSet, action))
                {
                    return false;
                }
                Skeleton skeleton = visuals?.GetVisuals()?.GetSkeleton();
                if (skeleton == null)
                {
                    return false;
                }
                MBSkeletonExtensions.SetAgentActionChannel(skeleton, channel, in action, 0f, blendPeriod, true, 0f);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static ActionIndexCache PickEquipAction(ItemObject newItem)
        {
            switch (newItem.PrimaryWeapon.WeaponClass)
            {
                case WeaponClass.OneHandedSword: return EquipSword;
                case WeaponClass.TwoHandedSword: return Equip2h;
                case WeaponClass.OneHandedAxe:
                case WeaponClass.Mace:
                case WeaponClass.Pick: return EquipAxe;
                case WeaponClass.TwoHandedAxe:
                case WeaponClass.TwoHandedMace: return Equip2hAxe;
                case WeaponClass.OneHandedPolearm:
                case WeaponClass.TwoHandedPolearm:
                case WeaponClass.LowGripPolearm: return EquipSpear;
                case WeaponClass.Dagger:
                case WeaponClass.ThrowingKnife: return EquipDagger;
                case WeaponClass.Bow: return EquipBow;
                case WeaponClass.Crossbow: return EquipCrossbow;
                case WeaponClass.Javelin: return EquipJavelin;
                case WeaponClass.ThrowingAxe: return EquipThrowingAxe;
                case WeaponClass.Stone: return EquipStone;
                default: return ActionIndexCache.act_none;
            }
        }

        internal static ActionIndexCache PickUnequipAction(ItemObject oldItem)
        {
            switch (oldItem.PrimaryWeapon.WeaponClass)
            {
                case WeaponClass.OneHandedSword: return UnequipSword;
                case WeaponClass.TwoHandedSword: return Unequip2h;
                case WeaponClass.OneHandedAxe:
                case WeaponClass.Mace:
                case WeaponClass.Pick: return UnequipAxe;
                case WeaponClass.TwoHandedAxe:
                case WeaponClass.TwoHandedMace: return Unequip2hAxe;
                case WeaponClass.OneHandedPolearm:
                case WeaponClass.TwoHandedPolearm:
                case WeaponClass.LowGripPolearm: return UnequipSpear;
                case WeaponClass.Dagger:
                case WeaponClass.ThrowingKnife: return UnequipDagger;
                case WeaponClass.Bow: return UnequipBow;
                case WeaponClass.Crossbow: return UnequipCrossbow;
                case WeaponClass.Javelin: return UnequipJavelin;
                case WeaponClass.ThrowingAxe: return UnequipThrowingAxe;
                default: return ActionIndexCache.act_none;
            }
        }

        internal static ActionIndexCache PickIdleAction(CharacterTableau tableau, ItemObject weapon)
        {
            // O caller garante que o personagem esta EM PE (montado retorna antes);
            // mesmo com cavalo na cena, pose de pe e a correta.
            {
                switch (weapon.PrimaryWeapon.WeaponClass)
                {
                    case WeaponClass.OneHandedSword:
                    case WeaponClass.OneHandedAxe:
                    case WeaponClass.Mace:
                    case WeaponClass.Pick:
                    case WeaponClass.Dagger:
                    case WeaponClass.ThrowingKnife:
                    case WeaponClass.ThrowingAxe:
                    case WeaponClass.Javelin:
                    case WeaponClass.Stone:
                        return OneHandedIdles[MBRandom.RandomInt(OneHandedIdles.Length)];
                    case WeaponClass.TwoHandedSword:
                    case WeaponClass.TwoHandedAxe:
                    case WeaponClass.TwoHandedMace:
                        return TwoHandedIdles[MBRandom.RandomInt(TwoHandedIdles.Length)];
                    case WeaponClass.OneHandedPolearm:
                    case WeaponClass.TwoHandedPolearm:
                    case WeaponClass.LowGripPolearm:
                        return SpearIdles[MBRandom.RandomInt(SpearIdles.Length)];
                    case WeaponClass.Bow:
                        return BowIdles[MBRandom.RandomInt(BowIdles.Length)];
                    case WeaponClass.Crossbow:
                        return CrossbowIdles[MBRandom.RandomInt(CrossbowIdles.Length)];
                    default:
                        return ActionIndexCache.act_none;
                }
            }

            // (o ramo de poses montadas foi removido: com o guard IsRiderMounted no
            // caller, ninguem em pe recebe pose de cavaleiro — era o boneco flutuante.)
        }

        internal static Equipment GetEquipment(CharacterTableau t) => EquipmentRef(t);
        internal static AgentVisuals GetMountVisuals(CharacterTableau t) => MountVisualsRef(t);
        /// <summary>True quando o personagem esta DE FATO montado (botao de trocar de lugar).</summary>
        internal static bool IsRiderMounted(CharacterTableau t) => MountSwappedRef(t);
        internal static bool AreEquipIndicesDirty(CharacterTableau t) => EquipIndicesDirtyRef(t);
        internal static int GetRightHandIndex(CharacterTableau t) => RightHandIndexRef(t);
        internal static AgentVisuals GetAgentVisuals(CharacterTableau t) => AgentVisualsRef(t);
        internal static bool IsEquipAnimActive(CharacterTableau t) => EquipAnimActiveRef(t);
        internal static float GetAnimationGap(CharacterTableau t) => AnimationGapRef(t);
        internal static float GetAnimationThreshold(CharacterTableau t) => AnimationThresholdRef(t);
        internal static void ResetAnimationGap(CharacterTableau t) => AnimationGapRef(t) = 0f;
        internal static ActionIndexCache Idle => InventoryIdle;
        internal static ActionIndexCache IdleStart => InventoryIdleStart;

        /// <summary>Primeira ARMA de verdade nos slots 0-3 (pula escudo e estandarte).</summary>
        internal static int FindWieldableWeaponSlot(Equipment equipment, out ItemObject weapon)
        {
            weapon = null;
            for (EquipmentIndex i = EquipmentIndex.Weapon0; i <= EquipmentIndex.Weapon3; i++)
            {
                ItemObject item = equipment[i].Item;
                if (item?.PrimaryWeapon == null)
                {
                    continue;
                }
                WeaponClass wc = item.PrimaryWeapon.WeaponClass;
                if (wc == WeaponClass.SmallShield || wc == WeaponClass.LargeShield || wc == WeaponClass.Banner)
                {
                    continue;
                }
                weapon = item;
                return (int)i;
            }
            return -1;
        }

        internal static int FindShieldSlot(Equipment equipment)
        {
            for (EquipmentIndex i = EquipmentIndex.Weapon0; i <= EquipmentIndex.Weapon3; i++)
            {
                ItemObject item = equipment[i].Item;
                if (item?.PrimaryWeapon == null)
                {
                    continue;
                }
                WeaponClass wc = item.PrimaryWeapon.WeaponClass;
                if (wc == WeaponClass.SmallShield || wc == WeaponClass.LargeShield)
                {
                    return (int)i;
                }
            }
            return -1;
        }

        internal static void Wield(CharacterTableau t, int weaponSlot, int shieldSlot)
        {
            RightHandIndexRef(t) = weaponSlot;
            LeftHandIndexRef(t) = shieldSlot;
            EquipIndicesDirtyRef(t) = true;
        }

        internal static void Unwield(CharacterTableau t)
        {
            if (RightHandIndexRef(t) >= 0 || LeftHandIndexRef(t) >= 0)
            {
                RightHandIndexRef(t) = -1;
                LeftHandIndexRef(t) = -1;
                EquipIndicesDirtyRef(t) = true;
            }
        }
    }

    /// <summary>
    /// Sacar/embainhar por classe de arma quando o Weapon0 muda. Roda DEPOIS do bloco
    /// vanilla (que só anima luvas/roupa); se o vanilla já tocou algo neste refresh
    /// (_animationGap zerado), cede a vez.
    /// </summary>
    [HarmonyPatch(typeof(CharacterTableau), "RefreshCharacterTableau")]
    internal static class CharacterTableauRefreshWeaponAnimPatch
    {
        private static void Postfix(CharacterTableau __instance, Equipment oldEquipment)
        {
            try
            {
                if (oldEquipment == null || !TableauWeaponAnims.IsEquipAnimActive(__instance))
                {
                    return;
                }
                if (TableauWeaponAnims.GetAnimationGap(__instance) < TableauWeaponAnims.GetAnimationThreshold(__instance))
                {
                    return; // vanilla acabou de animar luva/roupa neste refresh
                }

                Equipment equipment = TableauWeaponAnims.GetEquipment(__instance);
                AgentVisuals visuals = TableauWeaponAnims.GetAgentVisuals(__instance);
                if (equipment == null || visuals == null)
                {
                    return;
                }

                ItemObject newWeapon = null;
                ItemObject oldWeapon = null;
                for (EquipmentIndex i = EquipmentIndex.Weapon0; i <= EquipmentIndex.Weapon3; i++)
                {
                    if (equipment[i].Item != oldEquipment[i].Item)
                    {
                        newWeapon = equipment[i].Item;
                        oldWeapon = oldEquipment[i].Item;
                        break;
                    }
                }
                if (newWeapon == null && oldWeapon == null)
                {
                    return; // nenhum slot de arma mudou
                }

                ActionIndexCache action = newWeapon != null && newWeapon.PrimaryWeapon != null
                    ? TableauWeaponAnims.PickEquipAction(newWeapon)
                    : (oldWeapon != null && oldWeapon.PrimaryWeapon != null
                        ? TableauWeaponAnims.PickUnequipAction(oldWeapon)
                        : ActionIndexCache.act_none);

                if (action != ActionIndexCache.act_none &&
                    TableauWeaponAnims.TryPlay(__instance, visuals, 0, action, -0.2f))
                {
                    TableauWeaponAnims.ResetAnimationGap(__instance);
                }
            }
            catch (Exception e)
            {
                Debug.Print("[RF_TableauAnims] Refresh postfix: " + e.Message);
            }
        }
    }

    /// <summary>
    /// Pose idle com a arma na mão. Quando o canal de ação está no idle genérico de
    /// inventário (ou vazio) e há arma no slot 0, empunha a arma e toca a pose idle da
    /// classe dela — o "special" que o vanilla nunca toca.
    /// </summary>
    [HarmonyPatch(typeof(CharacterTableau), "OnTick")]
    internal static class CharacterTableauIdleAnimPatch
    {
        private static void Postfix(CharacterTableau __instance)
        {
            try
            {
                AgentVisuals visuals = TableauWeaponAnims.GetAgentVisuals(__instance);
                Equipment equipment = TableauWeaponAnims.GetEquipment(__instance);
                if (visuals == null || equipment == null)
                {
                    return;
                }

                // Personagem em pe ao lado do cavalo: empunha e usa pose DE PE normal.
                // So quando ele esta de fato montado (troca de lugar do inventario) deixamos
                // o vanilla em paz — pose em cima do cavalo era o cavaleiro flutuante.
                if (TableauWeaponAnims.IsRiderMounted(__instance))
                {
                    return;
                }

                Skeleton earlySkeleton = visuals.GetVisuals()?.GetSkeleton();
                int weaponSlot = TableauWeaponAnims.FindWieldableWeaponSlot(equipment, out ItemObject weapon);
                if (weaponSlot < 0)
                {
                    // Set sem arma (ex.: aba Members com traje civil): se a pose de arma da
                    // aba anterior sobreviveu a troca de equipamento (o refresh reaproveita o
                    // esqueleto), limpa a pose e desempunha — senao fica o boneco segurando o
                    // nada (feedback 2026-08-18).
                    if (earlySkeleton != null)
                    {
                        ActionIndexCache stale = MBSkeletonExtensions.GetActionAtChannel(earlySkeleton, 1);
                        if (TableauWeaponAnims.IsOurIdleAction(stale))
                        {
                            ActionIndexCache none = ActionIndexCache.act_none;
                            MBSkeletonExtensions.SetAgentActionChannel(earlySkeleton, 1, in none, 0f, 0f, true, 0f);
                        }
                    }
                    TableauWeaponAnims.Unwield(__instance);
                    return;
                }

                // Refresh recriou o AgentVisuals? O empunhar morreu com o visual antigo —
                // pede de novo e espera aplicar antes de tocar pose.
                TableauWeaponAnims.LastWieldedVisuals.TryGetValue(__instance, out AgentVisuals lastWielded);
                if (!ReferenceEquals(lastWielded, visuals))
                {
                    TableauWeaponAnims.Wield(__instance, weaponSlot, TableauWeaponAnims.FindShieldSlot(equipment));
                    TableauWeaponAnims.LastWieldedVisuals.Remove(__instance);
                    TableauWeaponAnims.LastWieldedVisuals.Add(__instance, visuals);
                    return;
                }

                Skeleton skeleton = visuals.GetVisuals()?.GetSkeleton();
                if (skeleton == null)
                {
                    return;
                }

                // So assume o controle quando o boneco esta no idle generico — nunca por
                // cima de animacao de equipar, de anim customizada do VM ou de pose ja nossa.
                ActionIndexCache channel0 = MBSkeletonExtensions.GetActionAtChannel(skeleton, 0);
                ActionIndexCache channel1 = MBSkeletonExtensions.GetActionAtChannel(skeleton, 1);
                bool channel0IsPlainIdle = channel0 == ActionIndexCache.act_none
                                           || channel0 == TableauWeaponAnims.Idle
                                           || channel0 == TableauWeaponAnims.IdleStart;
                if (!channel0IsPlainIdle || channel1 != ActionIndexCache.act_none)
                {
                    return;
                }

                if (TableauWeaponAnims.GetRightHandIndex(__instance) < 0)
                {
                    TableauWeaponAnims.Wield(__instance, weaponSlot, TableauWeaponAnims.FindShieldSlot(equipment));
                    return; // o vanilla aplica o empunhar no proximo tick
                }
                if (TableauWeaponAnims.AreEquipIndicesDirty(__instance))
                {
                    return; // empunhar pedido mas ainda nao aplicado — pose sairia de mao vazia
                }

                ActionIndexCache idle = TableauWeaponAnims.PickIdleAction(__instance, weapon);
                if (idle == ActionIndexCache.act_none)
                {
                    return;
                }

                TableauWeaponAnims.TryPlay(__instance, visuals, 1, idle, 0f);
            }
            catch (Exception e)
            {
                Debug.Print("[RF_TableauAnims] OnTick postfix: " + e.Message);
            }
        }
    }
}

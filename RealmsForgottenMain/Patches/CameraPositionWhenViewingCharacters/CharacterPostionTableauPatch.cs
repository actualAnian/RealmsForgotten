// @TODO
//using HarmonyLib;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using static RealmsForgotten.Patches.CameraPositionWhenViewingCharacters.JsonPatchConfig;
//using TaleWorlds.Core.ViewModelCollection;
//using TaleWorlds.Core;
//using TaleWorlds.Engine;
//using TaleWorlds.Library;
//using TaleWorlds.MountAndBlade.View.Tableaus;
//using TaleWorlds.MountAndBlade.View;
//using TaleWorlds.MountAndBlade;

//namespace RealmsForgotten.Patches.CameraPositionWhenViewingCharacters
//{
//    [HarmonyPatch(typeof(CharacterTableau), "FirstTimeInit")]
//    public class CharacterTableauPatchConfig
//    {
//        public static JsonPatchConfig Config;

//        public static void Postfix(CharacterTableau __instance)
//        {
//            Config = JsonPatchConfig.LoadConfig(__instance);  //load config only once on scene init
//        }
//    }

//    [HarmonyPatch(typeof(CharacterTableau), "RefreshCharacterTableau")]
//    public class CharacterTableauPatchRefresh
//    {
//        private static JsonPatchConfig _config;

//        public static void Postfix(CharacterTableau __instance, Equipment oldEquipment = null)
//        {
//            _config = CharacterTableauPatchConfig.Config ?? JsonPatchConfig.LoadConfig(__instance);
//            RefreshCharacterTableau(__instance, oldEquipment);
//        }



//        private static void UpdateMount(CharacterTableau tableau, bool isRiderAgentMounted = false)
//        {
//            /* start local variables to replace properties and fields of original object */
//            AgentVisuals _agentVisuals = PatchHelper.GetFieldValue<CharacterTableau, AgentVisuals>(tableau, "_agentVisuals");
//            AgentVisuals _oldMountVisuals = PatchHelper.GetFieldValue<CharacterTableau, AgentVisuals>(tableau, "_oldMountVisuals");
//            AgentVisuals _mountVisuals = PatchHelper.GetFieldValue<CharacterTableau, AgentVisuals>(tableau, "_mountVisuals");
//            Equipment _equipment = PatchHelper.GetFieldValue<CharacterTableau, Equipment>(tableau, "_equipment");
//            bool _isCharacterMountPlacesSwapped = PatchHelper.GetFieldValue<CharacterTableau, bool>(tableau, "_isCharacterMountPlacesSwapped");
//            uint _clothColor1 = PatchHelper.GetFieldValue<CharacterTableau, uint>(tableau, "_clothColor1");
//            uint _clothColor2 = PatchHelper.GetFieldValue<CharacterTableau, uint>(tableau, "_clothColor2");

//            ActionIndexCache act_camel_stand = PatchHelper.GetFieldValue<CharacterTableau, ActionIndexCache>(tableau, "act_camel_stand");
//            ActionIndexCache act_horse_stand = PatchHelper.GetFieldValue<CharacterTableau, ActionIndexCache>(tableau, "act_horse_stand");

//            MatrixFrame _mountSpawnPoint = PatchHelper.GetFieldValue<CharacterTableau, MatrixFrame>(tableau, "_mountSpawnPoint");
//            MatrixFrame _mountCharacterPositionFrame = PatchHelper.GetFieldValue<CharacterTableau, MatrixFrame>(tableau, "_mountCharacterPositionFrame");
//            Banner _banner = PatchHelper.GetFieldValue<CharacterTableau, Banner>(tableau, "_banner");
//            float _mainCharacterRotation = PatchHelper.GetFieldValue<CharacterTableau, float>(tableau, "_mainCharacterRotation");

//            int _mountVisualLoadingCounter = PatchHelper.GetFieldValue<CharacterTableau, int>(tableau, "_mountVisualLoadingCounter");
//            Scene _tableauScene = PatchHelper.GetFieldValue<CharacterTableau, Scene>(tableau, "_tableauScene");
//            string _mountCreationKey = PatchHelper.GetFieldValue<CharacterTableau, string>(tableau, "_mountCreationKey");

//            int _race = PatchHelper.GetFieldValue<CharacterTableau, int>(tableau, "_race");
//            /* end local variables to replace properties and fields of original object */

//            if (_equipment[EquipmentIndex.ArmorItemEndSlot].Item?.HorseComponent != null)
//            {
//                ItemObject item = _equipment[EquipmentIndex.ArmorItemEndSlot].Item;
//                Monster monster = item.HorseComponent.Monster;
//                Equipment equipment = new Equipment
//                {
//                    [EquipmentIndex.ArmorItemEndSlot] = _equipment[EquipmentIndex.ArmorItemEndSlot],
//                    [EquipmentIndex.HorseHarness] = _equipment[EquipmentIndex.HorseHarness]
//                };

//                /* start patch */
//                JsonPatchConfigItem configitem = _config.GetConfigItem(_race, false);
//                JsonPatchConfigItem mountconfigitem = _config.GetConfigItem(_race, true);

//                MatrixFrame charframe = _mountCharacterPositionFrame;
//                MatrixFrame mountframe = _mountSpawnPoint;

//                if (configitem != null)
//                {
//                    charframe = new MatrixFrame(_mountCharacterPositionFrame.rotation, _mountCharacterPositionFrame.origin);

//                    charframe.origin.y = charframe.origin.y + configitem.Horizontal;
//                    charframe.origin.z = charframe.origin.z + configitem.Vertical; //PATCHED: fix position to set camera little down to bring into view
//                    charframe.origin.x = charframe.origin.x + configitem.Zoom; //PATCHED: yotthani zoom out a little
//                }

//                if (mountconfigitem != null)
//                {
//                    mountframe = new MatrixFrame(_mountSpawnPoint.rotation, _mountSpawnPoint.origin);
//                    mountframe.origin.y = mountframe.origin.y + mountconfigitem.Horizontal;
//                    mountframe.origin.z = mountframe.origin.z + mountconfigitem.Vertical; //PATCHED: fix position to set camera little down to bring into view
//                    mountframe.origin.x = mountframe.origin.x + mountconfigitem.Zoom; //PATCHED: yotthani zoom out a little
//                }

//                /* end patch */


//                MatrixFrame frame = _isCharacterMountPlacesSwapped ? charframe : mountframe;
//                if (_isCharacterMountPlacesSwapped)
//                {
//                    frame.rotation.RotateAboutUp(_mainCharacterRotation);
//                }

//                if (_oldMountVisuals != null)
//                {
//                    _oldMountVisuals.ResetNextFrame();
//                }

//                _oldMountVisuals = _mountVisuals;
//                _mountVisualLoadingCounter = 3;
//                AgentVisualsData agentVisualsData = new AgentVisualsData();
//                agentVisualsData.Banner(_banner).Equipment(equipment).Frame(frame)
//                    .Scale(item.ScaleFactor)
//                    .ActionSet(MBGlobals.GetActionSet(monster.ActionSetCode))
//                    .ActionCode(!isRiderAgentMounted ? (ActionIndexCache)PatchHelper.CallPrivateMethod(tableau, "GetIdleAction", new object[] { }) : monster.MonsterUsage == "camel" ? act_camel_stand : act_horse_stand)
//                    .Scene(_tableauScene)
//                    .Monster(monster)
//                    .PrepareImmediately(prepareImmediately: false)
//                    .ClothColor1(_clothColor1)
//                    .ClothColor2(_clothColor2)
//                    .MountCreationKey(_mountCreationKey);
//                _mountVisuals = AgentVisuals.Create(agentVisualsData, "MountTableau", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
//                _mountVisuals.SetAgentLodZeroOrMaxExternal(makeZero: true);
//                _mountVisuals.SetVisible(value: false);
//                _mountVisuals.GetEntity().CheckResources(addToQueue: true, checkFaceResources: true);
//            }
//            else if (_mountVisuals != null)
//            {
//                _mountVisuals.Reset();
//                _mountVisuals = null;
//                _mountVisualLoadingCounter = 0;
//            }

//            PatchHelper.SetFieldValue(tableau, "_mountVisualLoadingCounter", _mountVisualLoadingCounter);
//            PatchHelper.SetFieldValue(tableau, "_mountVisuals", _mountVisuals);
//            PatchHelper.SetFieldValue(tableau, "_oldMountVisuals", _oldMountVisuals);
//        }

//        private static void RefreshCharacterTableau(CharacterTableau tableau, Equipment oldEquipment = null)
//        {
//            CharacterViewModel.StanceTypes _stanceIndex = PatchHelper.GetFieldValue<CharacterTableau, CharacterViewModel.StanceTypes>(tableau, "_stanceIndex");
//            UpdateMount(tableau, _stanceIndex == CharacterViewModel.StanceTypes.OnMount);
//            PatchHelper.CallPrivateMethod(tableau, "UpdateBannerItem", new object[] { }); //little trickery callin private method

//            /* start local variables to replace properties and fields of original object */
//            AgentVisuals _agentVisuals = PatchHelper.GetFieldValue<CharacterTableau, AgentVisuals>(tableau, "_agentVisuals");
//            AgentVisuals _mountVisuals = PatchHelper.GetFieldValue<CharacterTableau, AgentVisuals>(tableau, "_mountVisuals");
//            AgentVisuals _oldAgentVisuals = PatchHelper.GetFieldValue<CharacterTableau, AgentVisuals>(tableau, "_oldAgentVisuals");
//            int _agentVisualLoadingCounter = PatchHelper.GetFieldValue<CharacterTableau, int>(tableau, "_agentVisualLoadingCounter");
//            MatrixFrame _initialSpawnFrame = PatchHelper.GetFieldValue<CharacterTableau, MatrixFrame>(tableau, "_initialSpawnFrame");
//            MatrixFrame _characterMountPositionFrame = PatchHelper.GetFieldValue<CharacterTableau, MatrixFrame>(tableau, "_characterMountPositionFrame");
//            bool _isCharacterMountPlacesSwapped = PatchHelper.GetFieldValue<CharacterTableau, bool>(tableau, "_isCharacterMountPlacesSwapped");
//            float _mainCharacterRotation = PatchHelper.GetFieldValue<CharacterTableau, float>(tableau, "_mainCharacterRotation");
//            MBActionSet _characterActionSet = PatchHelper.GetFieldValue<CharacterTableau, MBActionSet>(tableau, "_characterActionSet");
//            BodyProperties _bodyProperties = PatchHelper.GetFieldValue<CharacterTableau, BodyProperties>(tableau, "_bodyProperties");
//            bool _isFemale = PatchHelper.GetFieldValue<CharacterTableau, bool>(tableau, "_isFemale");
//            Equipment _equipment = PatchHelper.GetFieldValue<CharacterTableau, Equipment>(tableau, "_equipment");
//            Banner _banner = PatchHelper.GetFieldValue<CharacterTableau, Banner>(tableau, "_banner");
//            uint _clothColor1 = PatchHelper.GetFieldValue<CharacterTableau, uint>(tableau, "_clothColor1");
//            uint _clothColor2 = PatchHelper.GetFieldValue<CharacterTableau, uint>(tableau, "_clothColor2");
//            int _initialLoadingCounter = PatchHelper.GetFieldValue<CharacterTableau, int>(tableau, "_initialLoadingCounter");
//            float _animationFrequencyThreshold = PatchHelper.GetFieldValue<CharacterTableau, float>(tableau, "_animationFrequencyThreshold");
//            float _animationGap = PatchHelper.GetFieldValue<CharacterTableau, float>(tableau, "_animationGap");
//            bool _isEquipmentAnimActive = PatchHelper.GetFieldValue<CharacterTableau, bool>(tableau, "_isEquipmentAnimActive");
//            ActionIndexCache act_inventory_glove_equip = PatchHelper.GetFieldValue<CharacterTableau, ActionIndexCache>(tableau, "act_inventory_glove_equip");
//            ActionIndexCache act_inventory_cloth_equip = PatchHelper.GetFieldValue<CharacterTableau, ActionIndexCache>(tableau, "act_inventory_cloth_equip");
//            int _race = PatchHelper.GetFieldValue<CharacterTableau, int>(tableau, "_race");
//            /* end local variables to replace properties and fields of original object */

//            if (_mountVisuals == null && _isCharacterMountPlacesSwapped)
//            {
//                _isCharacterMountPlacesSwapped = false;
//                _mainCharacterRotation = 0f;
//                PatchHelper.SetFieldValue(tableau, "_isCharacterMountPlacesSwapped", _isCharacterMountPlacesSwapped);
//                PatchHelper.SetFieldValue(tableau, "_mainCharacterRotation", _mainCharacterRotation);
//            }

//            if (_agentVisuals != null)
//            {
//                bool visibilityExcludeParents = _oldAgentVisuals.GetEntity().GetVisibilityExcludeParents();
//                AgentVisuals agentVisuals = _agentVisuals;
//                _agentVisuals = _oldAgentVisuals;
//                _oldAgentVisuals = agentVisuals;
//                _agentVisualLoadingCounter = 1;
//                AgentVisualsData copyAgentVisualsData = _agentVisuals.GetCopyAgentVisualsData();

//                /* start patch */
//                JsonPatchConfigItem configitem = _config.GetConfigItem(_race, false);
//                JsonPatchConfigItem mountconfigitem = _config.GetConfigItem(_race, true);


//                MatrixFrame charframe = _initialSpawnFrame;
//                MatrixFrame mountframe = _characterMountPositionFrame;

//                if (configitem != null)
//                {
//                    charframe = new MatrixFrame(_initialSpawnFrame.rotation, _initialSpawnFrame.origin);
//                    charframe.origin.y = charframe.origin.y + configitem.Horizontal;
//                    charframe.origin.z = charframe.origin.z + configitem.Vertical; //PATCHED: fix position to set camera little down to bring into view
//                    charframe.origin.x = charframe.origin.x + configitem.Zoom; //PATCHED: yotthani zoom out a little
//                }

//                if (mountconfigitem != null)
//                {
//                    mountframe = new MatrixFrame(_characterMountPositionFrame.rotation, _characterMountPositionFrame.origin);
//                    mountframe.origin.y = mountframe.origin.y + mountconfigitem.Horizontal;
//                    mountframe.origin.z = mountframe.origin.z + mountconfigitem.Vertical; //PATCHED: fix position to set camera little down to bring into view
//                    mountframe.origin.x = mountframe.origin.x + mountconfigitem.Zoom; //PATCHED: yotthani zoom out a little
//                }

//                /* end patch */

//                MatrixFrame frame = _isCharacterMountPlacesSwapped ? mountframe : charframe;
//                if (!_isCharacterMountPlacesSwapped)
//                {
//                    frame.rotation.RotateAboutUp(_mainCharacterRotation);
//                }

//                _characterActionSet = MBGlobals.GetActionSetWithSuffix(copyAgentVisualsData.MonsterData, _isFemale, "_warrior");
//                copyAgentVisualsData.BodyProperties(_bodyProperties).SkeletonType(_isFemale ? SkeletonType.Female : SkeletonType.Male).Frame(frame)
//                    .ActionSet(_characterActionSet)
//                    .Equipment(_equipment)
//                    .Banner(_banner)
//                    .UseMorphAnims(useMorphAnims: true)
//                    .ClothColor1(_clothColor1)
//                    .ClothColor2(_clothColor2)
//                    .Race(_race);
//                if (_initialLoadingCounter > 0)
//                {
//                    _initialLoadingCounter--;
//                    PatchHelper.SetFieldValue(tableau, "_initialLoadingCounter", _initialLoadingCounter);
//                }

//                _agentVisuals.Refresh(needBatchedVersionForWeaponMeshes: false, copyAgentVisualsData);
//                _agentVisuals.SetVisible(value: false);

//                if (_initialLoadingCounter == 0)
//                {
//                    _oldAgentVisuals.SetVisible(visibilityExcludeParents);
//                }

//                if (oldEquipment != null && _animationFrequencyThreshold <= _animationGap && _isEquipmentAnimActive)
//                {
//                    if (_equipment[EquipmentIndex.Gloves].Item != null && oldEquipment[EquipmentIndex.Gloves].Item != _equipment[EquipmentIndex.Gloves].Item)
//                    {
//                        _agentVisuals.GetVisuals().GetSkeleton().SetAgentActionChannel(0, act_inventory_glove_equip);
//                        PatchHelper.SetFieldValue(tableau, "_animationGap", 0f);
//                    }
//                    else if (_equipment[EquipmentIndex.Body].Item != null && oldEquipment[EquipmentIndex.Body].Item != _equipment[EquipmentIndex.Body].Item)
//                    {
//                        _agentVisuals.GetVisuals().GetSkeleton().SetAgentActionChannel(0, act_inventory_cloth_equip);
//                        PatchHelper.SetFieldValue(tableau, "_animationGap", 0f);
//                    }
//                }

//                _agentVisuals.GetEntity().CheckResources(addToQueue: true, checkFaceResources: true);
//            }

//            PatchHelper.SetFieldValue(tableau, "_agentVisuals", _agentVisuals);
//            PatchHelper.SetFieldValue(tableau, "_mountVisuals", _mountVisuals);
//            PatchHelper.SetFieldValue(tableau, "_oldAgentVisuals", _oldAgentVisuals);
//            PatchHelper.SetFieldValue(tableau, "_characterActionSet", _characterActionSet);

//            PatchHelper.CallPrivateMethod(tableau, "AdjustCharacterForStanceIndex", new object[] { }); //little trickery callin private method
//        }
//    }
//}

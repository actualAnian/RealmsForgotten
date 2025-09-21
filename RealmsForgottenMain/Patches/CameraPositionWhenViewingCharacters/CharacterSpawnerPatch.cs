// @TODO
//using HarmonyLib;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using static RealmsForgotten.Patches.CameraPositionWhenViewingCharacters.JsonPatchConfig;
//using TaleWorlds.Core;
//using TaleWorlds.Engine;
//using TaleWorlds.Library;
//using TaleWorlds.MountAndBlade.View.Scripts;
//using TaleWorlds.MountAndBlade.View;
//using TaleWorlds.MountAndBlade;

//namespace RealmsForgotten.Patches.CameraPositionWhenViewingCharacters
//{
//    [HarmonyPatch(typeof(CharacterSpawner), "InitWithCharacter")]
//    public class CharacterSpawnerPatch
//    {
//        private static JsonPatchConfig _config;

//        public static void Postfix(CharacterSpawner __instance, CharacterCode characterCode, bool useBodyProperties = false)
//        {
//            _config = JsonPatchConfig.LoadConfig(__instance);
//            InitWithCharacter(__instance, characterCode, useBodyProperties);
//        }
//        private static void InitWithCharacter(CharacterSpawner spawner, CharacterCode characterCode, bool useBodyProperties = false)
//        {
//            /* start local variables to replace properties and fields of original object */
//            GameEntity _agentEntity = PatchHelper.GetFieldValue<CharacterSpawner, GameEntity>(spawner, "_agentEntity");
//            GameEntity _horseEntity = PatchHelper.GetFieldValue<CharacterSpawner, GameEntity>(spawner, "_horseEntity");
//            AgentVisuals _agentVisuals = PatchHelper.GetFieldValue<CharacterSpawner, AgentVisuals>(spawner, "_agentVisuals");
//            MatrixFrame _spawnFrame = PatchHelper.GetFieldValue<CharacterSpawner, MatrixFrame>(spawner, "_spawnFrame");
//            bool CreateFaceImmediately = PatchHelper.GetFieldValue<CharacterSpawner, bool>(spawner, "CreateFaceImmediately");
//            /* end local variables to replace properties and fields of original object */

//            spawner.GameEntity.BreakPrefab();
//            if (_agentEntity != null && _agentEntity.Parent == spawner.GameEntity)
//            {
//                spawner.GameEntity.RemoveChild(_agentEntity, keepPhysics: false, keepScenePointer: false, callScriptCallbacks: true, 35);
//            }

//            _agentVisuals?.Reset();
//            _agentVisuals?.GetVisuals()?.ManualInvalidate();
//            if (_horseEntity != null && _horseEntity.Parent == spawner.GameEntity)
//            {
//                _horseEntity.Scene.RemoveEntity(_horseEntity, 98);
//            }

//            _agentEntity = GameEntity.CreateEmpty(spawner.GameEntity.Scene, isModifiableFromEditor: false);
//            _agentEntity.Name = "TableauCharacterAgentVisualsEntity";
//            _spawnFrame = _agentEntity.GetFrame();
//            _agentEntity.SetFrame(ref _spawnFrame);
//            PatchHelper.SetFieldValue(spawner, "_spawnFrame", _spawnFrame); //set original object the new value created here
//            PatchHelper.SetFieldValue(spawner, "_agentEntity", _agentEntity); //set original object the new value created here

//            BodyProperties bodyProperties = characterCode.BodyProperties;

//            if (useBodyProperties)
//            {
//                BodyProperties.FromString(spawner.BodyPropertiesString, out bodyProperties);
//            }

//            if (characterCode.Color1 != uint.MaxValue)
//            {
//                PatchHelper.SetPropertyValue(spawner, "ClothColor1", characterCode.Color1);
//            }

//            if (characterCode.Color2 != uint.MaxValue)
//            {
//                PatchHelper.SetPropertyValue(spawner, "ClothColor2", characterCode.Color2);
//            }

//            Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(characterCode.Race);

//            _agentVisuals = AgentVisuals.Create(new AgentVisualsData().Equipment(characterCode.CalculateEquipment()).BodyProperties(bodyProperties).Race(characterCode.Race)
//                .Frame(_spawnFrame)
//                .Scale(1f)
//                .SkeletonType(characterCode.IsFemale ? SkeletonType.Female : SkeletonType.Male)
//                .Entity(_agentEntity)
//                .ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, characterCode.IsFemale, spawner.ActionSetSuffix))
//                .ActionCode(ActionIndexCache.Create("act_inventory_idle_start"))
//                .Scene(spawner.GameEntity.Scene)
//                .Monster(baseMonsterFromRace)
//                .PrepareImmediately(CreateFaceImmediately)
//                .Banner(characterCode.Banner)
//                .ClothColor1(spawner.ClothColor1)
//                .ClothColor2(spawner.ClothColor2)
//                .UseMorphAnims(useMorphAnims: true), "TableauCharacterAgentVisuals", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);

//            _agentVisuals.SetAction(ActionIndexCache.Create(spawner.PoseAction), MBMath.ClampFloat(spawner.AnimationProgress, 0f, 1f));
//            spawner.GameEntity.AddChild(_agentEntity);

//            PatchHelper.SetFieldValue(spawner, "_agentVisuals", _agentVisuals); //set original object the new value created here

//            PatchHelper.CallPrivateMethod(spawner, "WieldWeapon", new object[] { characterCode }); //little trickery callin private method
//            _agentVisuals = PatchHelper.GetFieldValue<CharacterSpawner, AgentVisuals>(spawner, "_agentVisuals"); //update agent to be secure against outside manipulation
//            MatrixFrame frame = MatrixFrame.Identity;

//            /*start patch*/
//            JsonPatchConfigItem configitem = _config.GetConfigItem(characterCode.Race, false);


//            if (configitem != null)
//            {
//                frame.origin.x = frame.origin.x + configitem.Horizontal;
//                frame.origin.y = frame.origin.y + configitem.Zoom; //PATCHED: yotthani zoom out chars a little
//                frame.origin.z = frame.origin.z + configitem.Vertical; //PATCHED: yotthani fix position of dwarf faces to be in view
//            }
//            /*end patch*/

//            _agentVisuals.GetVisuals().SetFrame(ref frame);

//            if (spawner.HasMount)
//            {
//                PatchHelper.SetFieldValue(spawner, "_horseEntity", _horseEntity);
//                PatchHelper.CallPrivateMethod(spawner, "SpawnMount", new object[] { characterCode }); // little trickery callin private method
//                _horseEntity = PatchHelper.GetFieldValue<CharacterSpawner, GameEntity>(spawner, "_horseEntity"); // update entity to be secure against outside manipulation
//            }

//            spawner.GameEntity.SetVisibilityExcludeParents(visible: true);
//            _agentEntity.SetVisibilityExcludeParents(visible: true);
//            if (_horseEntity != null)
//            {
//                _horseEntity.SetVisibilityExcludeParents(visible: true);
//            }

//            Skeleton skeleton = _agentVisuals.GetVisuals().GetSkeleton();
//            skeleton.Freeze(p: false);
//            skeleton.TickAnimationsAndForceUpdate(0.001f, _agentVisuals.GetVisuals().GetGlobalFrame(), tickAnimsForChildren: false);
//            skeleton.SetUptoDate(value: false);
//            skeleton.Freeze(p: true);
//            _agentEntity.SetBoundingboxDirty();
//            skeleton.Freeze(p: false);
//            skeleton.TickAnimationsAndForceUpdate(0.001f, _agentVisuals.GetVisuals().GetGlobalFrame(), tickAnimsForChildren: false);
//            skeleton.SetAnimationParameterAtChannel(0, MBMath.ClampFloat(spawner.AnimationProgress, 0f, 1f));
//            skeleton.SetUptoDate(value: false);
//            skeleton.Freeze(p: true);
//            skeleton.ManualInvalidate();

//            if (_horseEntity != null)
//            {
//                _horseEntity.Skeleton.Freeze(p: false);
//                _horseEntity.Skeleton.TickAnimationsAndForceUpdate(0.001f, _horseEntity.GetGlobalFrame(), tickAnimsForChildren: false);
//                _horseEntity.Skeleton.SetUptoDate(value: false);
//                _horseEntity.Skeleton.Freeze(p: true);
//                _horseEntity.SetBoundingboxDirty();
//            }

//            if (_horseEntity != null)
//            {
//                _horseEntity.Skeleton.Freeze(p: false);
//                _horseEntity.Skeleton.TickAnimationsAndForceUpdate(0.001f, _horseEntity.GetGlobalFrame(), tickAnimsForChildren: false);
//                _horseEntity.Skeleton.SetAnimationParameterAtChannel(0, MBMath.ClampFloat(spawner.HorseAnimationProgress, 0f, 1f));
//                _horseEntity.Skeleton.SetUptoDate(value: false);
//                _horseEntity.Skeleton.Freeze(p: true);
//            }

//            spawner.GameEntity.SetBoundingboxDirty();
//            if (!spawner.GameEntity.Scene.IsEditorScene())
//            {
//                if (_agentEntity != null)
//                {
//                    _agentEntity.ManualInvalidate();
//                }

//                if (_horseEntity != null)
//                {
//                    _horseEntity.ManualInvalidate();
//                }
//            }

//            //take care that all objects are in the right state
//            PatchHelper.SetFieldValue(spawner, "_agentEntity", _agentEntity);
//            PatchHelper.SetFieldValue(spawner, "_horseEntity", _horseEntity);
//            PatchHelper.SetFieldValue(spawner, "_agentVisuals", _agentVisuals);
//            PatchHelper.SetFieldValue(spawner, "_spawnFrame", _spawnFrame);
//        }
//    }
//}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using SandBox.GauntletUI.AutoGenerated1;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using Path = System.IO.Path;

namespace RealmsForgotten.RFEffects
{
	// Token: 0x02000002 RID: 2
	public class RFEffectsSubModule : MBSubModuleBase
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		protected override void OnSubModuleLoad()
		{
			new Harmony("RFEffectsPatcher").PatchAll();
            ReadConfigFile();
        }


		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
            this.ReplaceModel<DefaultDamageParticleModel, AnoritDamageParticleModel>(gameStarterObject);
            if (game.GameType is Campaign)
			{
                gameStarterObject.AddModel(new RFVolunteerModel());
                gameStarterObject.AddModel(new NasoriaWageModel());
                gameStarterObject.AddModel(new ElveanMoraleModel());
                gameStarterObject.AddModel(new AthasBuildingConstructionModel());
                gameStarterObject.AddModel(new BerserkerAgentApplyDamageModel());
            }
            


        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            base.InitializeGameStarter(game, starterObject);
            CampaignGameStarter campaignGameStarter = starterObject as CampaignGameStarter;
            if (campaignGameStarter != null)
            {
                campaignGameStarter.AddBehavior(new RFCampaignBehavior());
                campaignGameStarter.AddBehavior(new CulturesCampaignBehavior());
            }
        }
        private void ReplaceModel<TBaseType, TChildType>(IGameStarter gameStarterObject) where TBaseType : GameModel where TChildType : TBaseType
		{
			IList<GameModel> list = gameStarterObject.Models as IList<GameModel>;
			if (list == null)
			{
				return;
			}
			bool flag = false;
			for (int i = 0; i < list.Count; i++)
			{

				if (list[i] is TBaseType)
				{
					flag = true;
					if (!(list[i] is TChildType))
					{
						list[i] = Activator.CreateInstance<TChildType>();
					}
				}
			}
			if (!flag)
			{
				gameStarterObject.AddModel(Activator.CreateInstance<TChildType>());
			}
		}

		public override void OnMissionBehaviorInitialize(Mission mission)
		{
            RFMissionBehaviour missionBehavior = new RFMissionBehaviour();
			mission.AddMissionBehavior(missionBehavior);
        }
        public static Dictionary<string, int> undeadRespawnConfig { get; private set; }
		private void ReadConfigFile()
		{
            string jsonFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "undead_respawn_config.json");
            JObject jsonObject = JObject.Parse(File.ReadAllText(jsonFilePath));

            if (jsonObject.TryGetValue("characters", out JToken charactersToken))
            {
                JObject charactersObject = (JObject)charactersToken;
                undeadRespawnConfig = new();
                foreach (var character in charactersObject)
                {

                    string characterName = character.Key;
                    int characterValue = character.Value.Value<int>();
                    if (characterValue > 100)
                        characterValue = 100;
                    if (characterValue < 1)
                        characterValue = 1;
                    undeadRespawnConfig.Add(characterName, characterValue);
                }

            }
            else
            {
                Console.WriteLine("Error in undead_respawn_config.json");
            }
        }
	}
    class RFVolunteerModel : DefaultVolunteerModel
    {

        public override int MaximumIndexHeroCanRecruitFromHero(Hero buyerHero, Hero sellerHero, int useValueAsRelation = -101)
        {
            int baseValue = base.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero, useValueAsRelation);

            IFaction buyerKingdom = buyerHero.MapFaction;
            if (buyerKingdom == null || buyerHero.Clan != null && buyerHero.Clan.IsClanTypeMercenary && buyerHero.Clan.IsMinorFaction)
                return baseValue;
            if (buyerKingdom.IsAtWarWith(sellerHero.HomeSettlement.MapFaction))
                return 0;

            return baseValue;
        }
    }
}

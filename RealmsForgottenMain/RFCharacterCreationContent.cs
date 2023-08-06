using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using RealmsForgotten.Managers;
using static RealmsForgotten.Globals;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten
{
	public class RFCharacterCreationContent : SandboxCharacterCreationContent
	{
		public CulturedStartManager Manager
		{
			get
			{
				return CulturedStartManager.Current;
			}
		}

		public override TextObject ReviewPageDescription
		{
			get
			{
				return new("{=W6pKpEoT}You prepare to set off for a grand adventure! Here is your character. Continue if you are ready, or go back to make changes.", null);
			}
		}

		public override IEnumerable<Type> CharacterCreationStages
		{
			get
			{
				yield return typeof(CharacterCreationCultureStage);
				yield return typeof(CharacterCreationFaceGeneratorStage);
				yield return typeof(CharacterCreationGenericStage);
				yield return typeof(CharacterCreationBannerEditorStage);
				yield return typeof(CharacterCreationClanNamingStage);
				yield return typeof(CharacterCreationReviewStage);
				yield return typeof(CharacterCreationOptionsStage);
				yield break;
			}
		}

		protected override void OnCultureSelected()
		{
			base.SelectedTitleType = 1;
			base.SelectedParentType = 0;
			TextObject textObject = FactionHelper.GenerateClanNameforPlayer();
			Clan.PlayerClan.ChangeClanName(textObject, textObject);
			CharacterObject playerCharacter = CharacterObject.PlayerCharacter;
			string bodyPropString = null;
			string stringId = playerCharacter.Culture.StringId;
			string cultureId = stringId;
			switch (cultureId)
            {
				case "aserai":
					bodyPropString = AthasBodyPropString;
					break;
				case "battania":
					bodyPropString = ElveanBodyPropString;
					break;
				case "empire":
					bodyPropString = HumanBodyPropString;
					break;
				case "khuzait":
					bodyPropString = AllKhuurBodyPropString;
					break;
				case "sturgia":
					bodyPropString = UndeadBodyPropString;
					break;
				case "vlandia":
					bodyPropString = NasoriaBodyPropString;
					break;
				default:
					Debug.FailedAssert("Selected culture is invalid!", "RFCharacterCreationContent.cs", "OnCultureSelected", 80);
					bodyPropString = HumanBodyPropString;
					break;
			}
			BodyProperties.FromString(bodyPropString, out BodyProperties properties);
			playerCharacter.UpdatePlayerCharacterBodyProperties(properties, playerCharacter.Race, playerCharacter.IsFemale);
            

        }

		public override int GetSelectedParentType()
		{
			return base.SelectedParentType;
		}

		public override void OnCharacterCreationFinalized()
		{
			if (this._startingPoints.TryGetValue(CharacterObject.PlayerCharacter.Culture.StringId, out Vec2 position2D))
			{
				MobileParty.MainParty.Position2D = position2D;
			}
			else
			{
				MobileParty.MainParty.Position2D = Campaign.Current.DefaultStartingPosition;
				Debug.FailedAssert("Selected culture is not in the dictionary!", "RFCharacterCreationContent.cs", "OnCharacterCreationFinalized", 102);
			}
			MapState mapState = GameStateManager.Current.ActiveState as MapState;
			mapState?.Handler.ResetCamera(true, true);
			mapState?.Handler.TeleportCameraToMainParty();
			base.SetHeroAge((float)this._startingAge);
		}

		protected override void OnInitialized(CharacterCreation characterCreation)
		{
			this.AddParentsMenu(characterCreation);
			this.AddChildhoodMenu(characterCreation);
			this.AddEducationMenu(characterCreation);
			this.AddYouthMenu(characterCreation);
			this.AddAdulthoodMenu(characterCreation);
			base.AddAgeSelectionMenu(characterCreation);
			this.AddCultureStartMenu(characterCreation);
			this.AddCultureLocationMenu(characterCreation);

			RFCulturalFeats culturalFeats = new RFCulturalFeats();


            foreach (CultureObject cultureObject in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
			{
				cultureObject.CultureFeats.Clear();
				string cultureId = cultureObject.StringId;
				switch (cultureId)
				{
					case "vlandia":
                        cultureObject.CultureFeats.Add(culturalFeats.nasoriaCheaperMercenaries);
                        break;
                    case "sturgia":
                        cultureObject.CultureFeats.Add(culturalFeats.dreadrealmSoldiersRevive);
                        break;
                    case "battania":
                        cultureObject.CultureFeats.Add(culturalFeats.elveanForestMorale);
                        break;
                    case "aserai":
                        cultureObject.CultureFeats.Add(culturalFeats.athasFasterConstructions);
                        break;
                    case "empire":
                        cultureObject.CultureFeats.Add(culturalFeats.empireAdittionalTier);
                        break;
                    case "khuzait":
                        cultureObject.CultureFeats.Add(culturalFeats.allkhuurPrisonersJoinMilitia);
                        break;
                }
				
			}
			
		}

		protected new void AddParentsMenu(CharacterCreation characterCreation)
		{
			// FAMILY MENU
			CharacterCreationMenu parentsMenu = new(new("{=b4lDDcli}Family", null), new("{=XgFU1pCx}You were born into a family of...", null), new CharacterCreationOnInit(base.ParentsOnInit), CharacterCreationMenu.MenuTypes.MultipleChoice);

			// FAMILY MENU -> HUMAN (EMPIRE)
			CharacterCreationCategory humanParentsCategory = parentsMenu.AddMenuCategory(base.EmpireParentsOnCondition);
			humanParentsCategory.AddCategoryOption(new("Direct Descendants of the first people."), new() { DefaultSkills.Riding, DefaultSkills.Polearm }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.EmpireLandlordsRetainerOnConsequence, base.EmpireLandlordsRetainerOnApply, new("{=ivKl4mV2}Descending from the ruler´s bloodline of the First People - the ancestors that made the pilgrimage to Aeurth - your father was a leader among his village and the cousin of the King of his Realm. He rode with the lord´s cavalry, fighting as an armored lancer."), null, 0, 0, 0, 0, 0);
			humanParentsCategory.AddCategoryOption(new("{=651FhzdR}Urban merchants"), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.EmpireMerchantOnConsequence, base.EmpireMerchantOnApply, new("{=FQntPChs}Your family were merchants in one of the main cities of the Kingdoms of Man. They sometimes organized caravans to nearby towns, and discussed issues in the town council."), null, 0, 0, 0, 0, 0);
			humanParentsCategory.AddCategoryOption(new("Free Farmers"), new() { DefaultSkills.Athletics, DefaultSkills.Polearm }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.EmpireFreeholderOnConsequence, base.EmpireFreeholderOnApply, new("{=09z8Q08f}Your family were small farmers with just enough land to feed themselves and make a small profit. People like them were the pillars of the realm rural economy, as well as the backbone of the levy."), null, 0, 0, 0, 0, 0);
			humanParentsCategory.AddCategoryOption(new("{=v48N6h1t}Urban artisans"), new() { DefaultSkills.Crafting, DefaultSkills.Crossbow }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.EmpireArtisanOnConsequence, base.EmpireArtisanOnApply, new("{=ZKynvffv}Your family owned their own workshop in a city, making goods from raw materials brought in from the countryside. Your father played an active if minor role in the town council, and also served in the militia."), null, 0, 0, 0, 0, 0);
			humanParentsCategory.AddCategoryOption(new("Forestcaretakers"), new() { DefaultSkills.Scouting, DefaultSkills.Bow }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.EmpireWoodsmanOnConsequence, base.EmpireWoodsmanOnApply, new("Your family lived in a village, but did not own their own land. Instead, your father supplemented paid jobs with long trips in the woods, hunting and trapping, always keeping a wary eye for the lord's game wardens."), null, 0, 0, 0, 0, 0);
			humanParentsCategory.AddCategoryOption(new("{=aEke8dSb}Urban vagabonds"), new() { DefaultSkills.Roguery, DefaultSkills.Throwing }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.EmpireVagabondOnConsequence, base.EmpireVagabondOnApply, new("{=Jvf6K7TZ}Your family numbered among the many poor migrants living in the slums that grow up outside the walls of cities, making whatever money they could from a variety of odd jobs. Sometimes they did service for one of the many criminal gangs, and you had an early look at the dark side of life."), null, 0, 0, 0, 0, 0);

			// FAMILY MENU -> NASORIA (VLANDIAN)
			CharacterCreationCategory nasorianParentsCategory = parentsMenu.AddMenuCategory(base.VlandianParentsOnCondition);
			nasorianParentsCategory.AddCategoryOption(new("Retainers of the Qairth"), new() { DefaultSkills.Riding, DefaultSkills.Polearm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.VlandiaBaronsRetainerOnConsequence, base.VlandiaBaronsRetainerOnApply, new("Your father was a bailiff for a local Qairth. He looked after his Qairth's estates, resolved disputes in the village, and helped train the village levy. He rode with the Qairth's cavalry, fighting as an armored knight."), null, 0, 0, 0, 0, 0);
			nasorianParentsCategory.AddCategoryOption(new("Guildmerchants"), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.VlandiaMerchantOnConsequence, base.VlandiaMerchantOnApply, new("{=qNZFkxJb}Your family were merchants in one of the main cities of the kingdom. They organized caravans to nearby towns and were active in the local merchant's guild."), null, 0, 0, 0, 0, 0);
			nasorianParentsCategory.AddCategoryOption(new("Aldenari"), new MBList<SkillObject> { DefaultSkills.Polearm, DefaultSkills.Crossbow }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.VlandiaYeomanOnConsequence, base.VlandiaYeomanOnApply, new("{=BLZ4mdhb}Your family were small farmers with just enough land to feed themselves and make a small profit. People like them were the pillars of the kingdom's economy, as well as the backbone of the levy."), null, 0, 0, 0, 0, 0);
			nasorianParentsCategory.AddCategoryOption(new("{=p2KIhGbE}Urban blacksmith"), new MBList<SkillObject> { DefaultSkills.Crafting, DefaultSkills.TwoHanded }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.VlandiaBlacksmithOnConsequence, base.VlandiaBlacksmithOnApply, new("{=btsMpRcA}Your family owned a smithy in a city. Your father played an active if minor role in the town council, and also served in the militia."), null, 0, 0, 0, 0, 0);
			nasorianParentsCategory.AddCategoryOption(new("{=YcnK0Thk}Hunters"), new MBList<SkillObject> { DefaultSkills.Scouting, DefaultSkills.Crossbow }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.VlandiaHunterOnConsequence, base.VlandiaHunterOnApply, new("{=yRFSzSDZ}Your family lived in a village, but did not own their own land. Instead, your father supplemented paid jobs with long trips in the woods, hunting and trapping, always keeping a wary eye for the lord's game wardens."), null, 0, 0, 0, 0, 0);
			nasorianParentsCategory.AddCategoryOption(new("{=ipQP6aVi}Mercenaries"), new MBList<SkillObject> { DefaultSkills.Roguery, DefaultSkills.Crossbow }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.VlandiaMercenaryOnConsequence, base.VlandiaMercenaryOnApply, new("Your father joined one of the East many mercenary companies, composed of men who got such a taste for war in their clan's service that they never took well to peace. Their crossbowmen were much valued across the world. Your mother was a camp follower, taking you along in the wake of bloody campaigns."), null, 0, 0, 0, 0, 0);

			// FAMILY MENU -> UNDEAD (STURGIAN)
			CharacterCreationCategory undeadParentsCategory = parentsMenu.AddMenuCategory(base.SturgianParentsOnCondition);
			undeadParentsCategory.AddCategoryOption(new("Servants of the Undead"), new() { DefaultSkills.Riding, DefaultSkills.TwoHanded }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.SturgiaBoyarsCompanionOnConsequence, base.SturgiaBoyarsCompanionOnApply, new("Your family served the Undead."), null, 0, 0, 0, 0, 0);
			undeadParentsCategory.AddCategoryOption(new("{=HqzVBfpl}Urban traders"), new MBList<SkillObject> { DefaultSkills.Trade, DefaultSkills.Tactics }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.SturgiaTraderOnConsequence, base.SturgiaTraderOnApply, new("Your family were merchants who lived in one of the land's great river ports, organizing the shipment of goods to faraway lands."), null, 0, 0, 0, 0, 0);
			undeadParentsCategory.AddCategoryOption(new("Farmers"), new() { DefaultSkills.Athletics, DefaultSkills.Polearm }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.SturgiaFreemanOnConsequence, base.SturgiaFreemanOnApply, new("{=Mcd3ZyKq}Your family had just enough land to feed themselves and make a small profit. People like them were the pillars of the kingdom's economy, as well as the backbone of the levy."), null, 0, 0, 0, 0, 0);
			undeadParentsCategory.AddCategoryOption(new("{=v48N6h1t}Urban artisans"), new() { DefaultSkills.Crafting, DefaultSkills.OneHanded }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.SturgiaArtisanOnConsequence, base.SturgiaArtisanOnApply, new("{=ueCm5y1C}Your family owned their own workshop in a city, making goods from raw materials brought in from the countryside. Your father played an active if minor role in the town council, and also served in the militia."), null, 0, 0, 0, 0, 0);
			undeadParentsCategory.AddCategoryOption(new("Forestfolk"), new() { DefaultSkills.Scouting, DefaultSkills.Bow }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.SturgiaHunterOnConsequence, base.SturgiaHunterOnApply, new("Your family had no taste for authority of others. They made their living deep in the woods, slashing and burning fields which they tended for a year or two before moving on. They hunted and trapped fox, hare, ermine, and other fur-bearing animals."), null, 0, 0, 0, 0, 0);
			undeadParentsCategory.AddCategoryOption(new("{=TPoK3GSj}Vagabonds"), new() { DefaultSkills.Roguery, DefaultSkills.Throwing }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.SturgiaVagabondOnConsequence, base.SturgiaVagabondOnApply, new("{=2SDWhGmQ}Your family numbered among the poor migrants living in the slums that grow up outside the walls of the river cities, making whatever money they could from a variety of odd jobs. Sometimes they did services for one of the region's many criminal gangs."), null, 0, 0, 0, 0, 0);

			// FAMILY MENU -> ATHAS (ASERAI)
			CharacterCreationCategory athassianParentsCategory = parentsMenu.AddMenuCategory(base.AseraiParentsOnCondition);
			athassianParentsCategory.AddCategoryOption(new("The inner circle of Atha's rulers"), new() { DefaultSkills.Riding, DefaultSkills.Throwing }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.AseraiTribesmanOnConsequence, base.AseraiTribesmanOnApply, new("You were a family of some importance in the inner circle of Athas."), null, 0, 0, 0, 0, 0);
			athassianParentsCategory.AddCategoryOption(new("{=ngFVgwDD}Warrior-slaves"), new() { DefaultSkills.Riding, DefaultSkills.Polearm }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.AseraiWariorSlaveOnConsequence, base.AseraiWariorSlaveOnApply, new("{=GsPC2MgU}Your father was part of one of the slave-bodyguards maintained by the rulers. He fought by his master's side with tribe's armored cavalry, and was freed - perhaps for an act of valor, or perhaps he paid for his freedom with his share of the spoils of battle. He then married your mother."), null, 0, 0, 0, 0, 0);
			athassianParentsCategory.AddCategoryOption(new("{=651FhzdR}Urban merchants"), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.AseraiMerchantOnConsequence, base.AseraiMerchantOnApply, new("{=1zXrlaav}Your family were respected traders in an oasis town. They ran caravans across the desert, and were experts in the finer points of negotiating passage through the desert tribes' territories."), null, 0, 0, 0, 0, 0);
			athassianParentsCategory.AddCategoryOption(new("Slave-farmers"), new() { DefaultSkills.Athletics, DefaultSkills.OneHanded }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.AseraiOasisFarmerOnConsequence, base.AseraiOasisFarmerOnApply, new("{=5P0KqBAw}Your family tilled the soil in one of the oases of the Kalikhr tribe and tended the palm orchards that produced the desert's famous dates. Your father was a member of the main foot levy of his tribe, fighting with his kinsmen under the emir's banner."), null, 0, 0, 0, 0, 0);
			athassianParentsCategory.AddCategoryOption(new("Free men"), new() { DefaultSkills.Scouting, DefaultSkills.Bow }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.AseraiBedouinOnConsequence, base.AseraiBedouinOnApply, new("{=PKhcPbBX}Your family were part of a nomadic clan, crisscrossing the wastes between wadi beds and wells to feed their herds of goats and camels on the scraggly scrubs of the Kalikhr."), null, 0, 0, 0, 0, 0);
			athassianParentsCategory.AddCategoryOption(new("Urban Orfans"), new() { DefaultSkills.Roguery, DefaultSkills.Polearm }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.AseraiBackAlleyThugOnConsequence, base.AseraiBackAlleyThugOnApply, new("{=6bUSbsKC}Your father was not your biological father, but took you under his protection to one day strenghten his army of thugs. He worked for a fitiwi , one of the strongmen who keep order in the poorer quarters of the oasis towns. He resolved disputes over land, dice and insults, imposing his authority with the fitiwi's traditional staff."), null, 0, 0, 0, 0, 0);

			// FAMILY MENU -> ELVEAN (BATTANIAN)
			CharacterCreationCategory elveanParentsCategory = parentsMenu.AddMenuCategory(new(base.BattanianParentsOnCondition));
			elveanParentsCategory.AddCategoryOption(new("Elvean Highborn"), new() { DefaultSkills.TwoHanded, DefaultSkills.Bow }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.BattaniaChieftainsHearthguardOnConsequence, base.BattaniaChieftainsHearthguardOnApply, new("Your family were the trusted kinfolk of an Elvean lord, and sat at his table in his great hall. Your father assisted his chief in running the affairs and trained with the traditional weapons of the warrior elite, the two-handed sword or falx and the bow."), null, 0, 0, 0, 0, 0);
			elveanParentsCategory.AddCategoryOption(new("Druids"), new() { DefaultSkills.Medicine, DefaultSkills.Charm }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.BattaniaHealerOnConsequence, base.BattaniaHealerOnApply, new("Your parents were healers who gathered herbs and treated the sick. As a living reservoir of elvean tradition, they were also asked to adjudicate many disputes between the clans."), null, 0, 0, 0, 0, 0);
			elveanParentsCategory.AddCategoryOption(new("Elvean Folk"), new() { DefaultSkills.Athletics, DefaultSkills.Throwing }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.BattaniaTribesmanOnConsequence, base.BattaniaTribesmanOnApply, new("Your family were middle-ranking members of a society, who tilled their own land. Your father fought with the kern, the main body of his people's warriors, joining in the screaming charges for which the Elveans were famous."), null, 0, 0, 0, 0, 0);
			elveanParentsCategory.AddCategoryOption(new("{=BCU6RezA}Smiths"), new() { DefaultSkills.Crafting, DefaultSkills.TwoHanded }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.BattaniaSmithOnConsequence, base.BattaniaSmithOnApply, new("Your family were smiths, a revered profession. They crafted everything from fine filigree jewelry in geometric designs to the well-balanced longswords favored by the Elvean aristocracy."), null, 0, 0, 0, 0, 0);
			elveanParentsCategory.AddCategoryOption(new("{=7eWmU2mF}Foresters"), new() { DefaultSkills.Scouting, DefaultSkills.Tactics }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.BattaniaWoodsmanOnConsequence, base.BattaniaWoodsmanOnApply, new("{=7jBroUUQ}Your family had little land of their own, so they earned their living from the woods, hunting and trapping. They taught you from an early age that skills like finding game trails and killing an animal with one shot could make the difference between eating and starvation."), null, 0, 0, 0, 0, 0);
			elveanParentsCategory.AddCategoryOption(new("{=SpJqhEEh}Bards"), new() { DefaultSkills.Roguery, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.BattaniaBardOnConsequence, base.BattaniaBardOnApply, new("Your Father was a Bard, a sacred duty for the Elvean Folk. Responsible to keep the Song alive, he went from halls to festivities, from rituals to war camps, to teach and inspire the people into the sacred ways. Your learned from him the cleverness of the tongue and the hability to tap into your people´s soul."), null, 0, 0, 0, 0, 0);

			// FAMILY MENU -> ALL KHUUR (KHUZAIT)
			CharacterCreationCategory allKhuurParentsCategory = parentsMenu.AddMenuCategory(new(base.KhuzaitParentsOnCondition));
			allKhuurParentsCategory.AddCategoryOption(new("Al-Kahuur Kinsfolk"), new() { DefaultSkills.Riding, DefaultSkills.Polearm }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.KhuzaitNoyansKinsmanOnConsequence, base.KhuzaitNoyansKinsmanOnApply, new("Your family were the trusted kinsfolk of a ruler, and shared his meals in the chieftain's yurt. Your father assisted his chief in running the affairs of the clan and fought in the core of armored lancers in the center of a battle line."), null, 0, 0, 0, 0, 0);
			allKhuurParentsCategory.AddCategoryOption(new("{=TkgLEDRM}Merchants"), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.KhuzaitMerchantOnConsequence, base.KhuzaitMerchantOnApply, new("Your family came from one of the merchant clans that dominated the cities in the northwestern part of the world."), null, 0, 0, 0, 0, 0);
			allKhuurParentsCategory.AddCategoryOption(new("{=tGEStbxb}Tribespeople"), new() { DefaultSkills.Bow, DefaultSkills.Riding }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.KhuzaitTribesmanOnConsequence, base.KhuzaitTribesmanOnApply, new("Your family were middle-ranking members of one of the clans. They had some herds of thier own, but were not rich. "), null, 0, 0, 0, 0, 0);
			allKhuurParentsCategory.AddCategoryOption(new("{=gQ2tAvCz}Farmers"), new() { DefaultSkills.Polearm, DefaultSkills.Throwing }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.KhuzaitFarmerOnConsequence, base.KhuzaitFarmerOnApply, new("Your family tilled one of the small patches of arable land in the steppes for generations."), null, 0, 0, 0, 0, 0);
			allKhuurParentsCategory.AddCategoryOption(new("Spirit-Chatchers"), new() { DefaultSkills.Medicine, DefaultSkills.Charm }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.KhuzaitShamanOnConsequence, base.KhuzaitShamanOnApply, new("Your family were guardians of the sacred traditions, channelling the spirits of the wilderness and of the ancestors. They tended the sick and dispensed wisdom, resolving disputes and providing practical advice."), null, 0, 0, 0, 0, 0);
			allKhuurParentsCategory.AddCategoryOption(new("{=Xqba1Obq}Nomads"), new() { DefaultSkills.Scouting, DefaultSkills.Riding }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.KhuzaitNomadOnConsequence, base.KhuzaitNomadOnApply, new("{=9aoQYpZs}Your family's clan never pledged its loyalty to the khan and never settled down, preferring to live out in the deep steppe away from his authority. They remain some of the finest trackers and scouts in the grasslands, as the ability to spot an enemy coming and move quickly is often all that protects their herds from their neighbors' predations."), null, 0, 0, 0, 0, 0);

			characterCreation.AddNewMenu(parentsMenu);
		}

		protected new void AddChildhoodMenu(CharacterCreation characterCreation)
		{
			// CHILDHOOD MENU
			CharacterCreationMenu childhoodMenu = new(new("{=8Yiwt1z6}Early Childhood", null), new("{=character_creation_content_16}As a child you were noted for...", null), new CharacterCreationOnInit(base.ChildhoodOnInit), CharacterCreationMenu.MenuTypes.MultipleChoice);

			CharacterCreationCategory childhoodCategory = childhoodMenu.AddMenuCategory(null);
			childhoodCategory.AddCategoryOption(new("{=kmM68Qx4}your leadership skills."), new() { DefaultSkills.Leadership, DefaultSkills.Tactics}, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, SandboxCharacterCreationContent.ChildhoodYourLeadershipSkillsOnConsequence, SandboxCharacterCreationContent.ChildhoodGoodLeadingOnApply, new("{=FfNwXtii}If the wolf pup gang of your early childhood had an alpha, it was definitely you. All the other kids followed your lead as you decided what to play and where to play, and led them in games and mischief."), null, 0, 0, 0, 0, 0);
			childhoodCategory.AddCategoryOption(new("{=5HXS8HEY}your brawn."), new() { DefaultSkills.TwoHanded, DefaultSkills.Throwing }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, SandboxCharacterCreationContent.ChildhoodYourBrawnOnConsequence, SandboxCharacterCreationContent.ChildhoodGoodAthleticsOnApply, new("{=YKzuGc54}You were big, and other children looked to have you around in any scrap with children from a neighboring village. You pushed a plough and throw an axe like an adult."), null, 0, 0, 0, 0, 0);
			childhoodCategory.AddCategoryOption(new("{=QrYjPUEf}your attention to detail."), new() { DefaultSkills.Athletics, DefaultSkills.Bow }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, SandboxCharacterCreationContent.ChildhoodAttentionToDetailOnConsequence, SandboxCharacterCreationContent.ChildhoodGoodMemoryOnApply, new("{=JUSHAPnu}You were quick on your feet and attentive to what was going on around you. Usually you could run away from trouble, though you could give a good account of yourself in a fight with other children if cornered."), null, 0, 0, 0, 0, 0);
			childhoodCategory.AddCategoryOption(new("{=Y3UcaX74}your aptitude for numbers."), new() { DefaultSkills.Engineering, DefaultSkills.Trade }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, SandboxCharacterCreationContent.ChildhoodAptitudeForNumbersOnConsequence, SandboxCharacterCreationContent.ChildhoodGoodMathOnApply, new("{=DFidSjIf}Most children around you had only the most rudimentary education, but you lingered after class to study letters and mathematics. You were fascinated by the marketplace - weights and measures, tallies and accounts, the chatter about profits and losses."), null, 0, 0, 0, 0, 0);
			childhoodCategory.AddCategoryOption(new("{=GEYzLuwb}your way with people."), new() { DefaultSkills.Charm, DefaultSkills.Leadership }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, SandboxCharacterCreationContent.ChildhoodWayWithPeopleOnConsequence, SandboxCharacterCreationContent.ChildhoodGoodMannersOnApply, new("{=w2TEQq26}You were always attentive to other people, good at guessing their motivations. You studied how individuals were swayed, and tried out what you learned from adults on your friends."), null, 0, 0, 0, 0, 0);
			childhoodCategory.AddCategoryOption(new("{=MEgLE2kj}your skill with horses."), new() { DefaultSkills.Riding, DefaultSkills.Medicine }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, SandboxCharacterCreationContent.ChildhoodSkillsWithHorsesOnConsequence, SandboxCharacterCreationContent.ChildhoodAffinityWithAnimalsOnApply, new("{=ngazFofr}You were always drawn to animals, and spent as much time as possible hanging out in the village stables. You could calm horses, and were sometimes called upon to break in new colts. You learned the basics of veterinary arts, much of which is applicable to humans as well."), null, 0, 0, 0, 0, 0);

			characterCreation.AddNewMenu(childhoodMenu);
		}

		protected new void AddEducationMenu(CharacterCreation characterCreation)
		{
			// EDUCATION MENU
			CharacterCreationMenu characterCreationMenu = new(new("{=rcoueCmk}Adolescence", null), this._educationIntroductoryText, new CharacterCreationOnInit(base.EducationOnInit), CharacterCreationMenu.MenuTypes.MultipleChoice);

			// EDUCATION MENU -> RURAL
			CharacterCreationCategory educationCategory = characterCreationMenu.AddMenuCategory(null);
			educationCategory.AddCategoryOption(new("{=RKVNvimC}herded the sheep.", null), new() { DefaultSkills.Athletics, DefaultSkills.Throwing }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.RuralAdolescenceOnCondition, base.RuralAdolescenceHerderOnConsequence, SandboxCharacterCreationContent.RuralAdolescenceHerderOnApply, new("{=KfaqPpbK}You went with other fleet-footed youths to take the villages' sheep, goats or cattle to graze in pastures near the village. You were in charge of chasing down stray beasts, and always kept a big stone on hand to be hurled at lurking predators if necessary."), null, 0, 0, 0, 0, 0);
			educationCategory.AddCategoryOption(new("learned the elvean ways of smithing."), new() { DefaultSkills.TwoHanded, DefaultSkills.Crafting }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.BattanianParentsOnCondition, base.RuralAdolescenceSmithyOnConsequence, SandboxCharacterCreationContent.RuralAdolescenceSmithyOnApply, new("{=y6j1bJTH}You were apprenticed to the local smith. You learned how to heat and forge metal, hammering for hours at a time until your muscles ached."), null, 0, 0, 0, 0, 0);
			educationCategory.AddCategoryOption(new("{=tI8ZLtoA}repaired projects."), new() { DefaultSkills.Crafting, DefaultSkills.Engineering }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.RuralAdolescenceOnCondition, base.RuralAdolescenceRepairmanOnConsequence, SandboxCharacterCreationContent.RuralAdolescenceRepairmanOnApply, new("{=6LFj919J}You helped dig wells, rethatch houses, and fix broken plows. You learned about the basics of construction, as well as what it takes to keep a farming community prosperous."), null, 0, 0, 0, 0, 0);
			educationCategory.AddCategoryOption(new("{=TRwgSLD2}gathered herbs in the wild."), new() { DefaultSkills.Medicine, DefaultSkills.Scouting }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.RuralAdolescenceOnCondition, base.RuralAdolescenceGathererOnConsequence, SandboxCharacterCreationContent.RuralAdolescenceGathererOnApply, new("{=9ks4u5cH}You were sent by the village healer up into the hills to look for useful medicinal plants. You learned which herbs healed wounds or brought down a fever, and how to find them."), null, 0, 0, 0, 0, 0);
			educationCategory.AddCategoryOption(new("{=T7m7ReTq}hunted small game."), new() { DefaultSkills.Bow, DefaultSkills.Tactics }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.RuralAdolescenceOnCondition, base.RuralAdolescenceHunterOnConsequence, SandboxCharacterCreationContent.RuralAdolescenceHunterOnApply, new("{=RuvSk3QT}You accompanied a local hunter as he went into the wilderness, helping him set up traps and catch small animals."), null, 0, 0, 0, 0, 0);
			educationCategory.AddCategoryOption(new("{=qAbMagWq}sold produce at the market."), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.RuralAdolescenceOnCondition, base.RuralAdolescenceHelperOnConsequence, SandboxCharacterCreationContent.RuralAdolescenceHelperOnApply, new("{=DIgsfYfz}You took your family's goods to the nearest town to sell your produce and buy supplies. It was hard work, but you enjoyed the hubbub of the marketplace."), null, 0, 0, 0, 0, 0);

			// EDUCATION MENU -> URBAN
			educationCategory.AddCategoryOption(new("{=nOfSqRnI}at the town watch's training ground."), new() { DefaultSkills.Crossbow, DefaultSkills.Tactics }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanAdolescenceOnCondition, base.UrbanAdolescenceWatcherOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceWatcherOnApply, new("{=qnqdEJOv}You watched the town's watch practice shooting and perfect their plans to defend the walls in case of a siege."), null, 0, 0, 0, 0, 0);
			educationCategory.AddCategoryOption(new("{=8a6dnLd2}with the alley gangs."), new() { DefaultSkills.Roguery, DefaultSkills.OneHanded }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanAdolescenceOnCondition, base.UrbanAdolescenceGangerOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceGangerOnApply, new("{=1SUTcF0J}The gang leaders who kept watch over the slums of Athas' cities were always in need of poor youth to run messages and back them up in turf wars, while thrill-seeking merchants' sons and daughters sometimes slummed it in their company as well."), null, 0, 0, 0, 0, 0);
			educationCategory.AddCategoryOption(new("{=7Hv984Sf}at docks and building sites."), new() { DefaultSkills.Athletics, DefaultSkills.Crafting }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanAdolescenceOnCondition, base.UrbanAdolescenceDockerOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceDockerOnApply, new("{=bhdkegZ4}All towns had their share of projects that were constantly in need of both skilled and unskilled labor. You learned how hoists and scaffolds were constructed, how planks and stones were hewn and fitted, and other skills."), null, 0, 0, 0, 0, 0);
			educationCategory.AddCategoryOption(new("{=kbcwb5TH}in the markets and merchant caravans."), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanPoorAdolescenceOnCondition, base.UrbanAdolescenceMarketerOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceMarketerOnApply, new("{=lLJh7WAT}You worked in the marketplace, selling trinkets and drinks to busy shoppers."), null, 0, 0, 0, 0, 0);
			educationCategory.AddCategoryOption(new("{=kbcwb5TH}in the markets and merchant caravans."), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanRichAdolescenceOnCondition, base.UrbanAdolescenceMarketerOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceMarketerOnApply, new("{=rmMcwSn8}You helped your family handle their business affairs, going down to the marketplace to make purchases and oversee the arrival of caravans."), null, 0, 0, 0, 0, 0);
			educationCategory.AddCategoryOption(new("{=mfRbx5KE}reading and studying."), new() { DefaultSkills.Engineering, DefaultSkills.Leadership }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanPoorAdolescenceOnCondition, base.UrbanAdolescenceTutorOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceDockerOnApply, new("{=elQnygal}Your family scraped up the money for a rudimentary schooling and you took full advantage, reading voraciously on history, mathematics, and philosophy and discussing what you read with your tutor and classmates."), null, 0, 0, 0, 0, 0);
			educationCategory.AddCategoryOption(new("{=etG87fB7}with your tutor."), new() { DefaultSkills.Engineering, DefaultSkills.Leadership }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanRichAdolescenceOnCondition, base.UrbanAdolescenceTutorOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceDockerOnApply, new("{=hXl25avg}Your family arranged for a private tutor and you took full advantage, reading voraciously on history, mathematics, and philosophy and discussing what you read with your tutor and classmates."), null, 0, 0, 0, 0, 0);
			educationCategory.AddCategoryOption(new("{=FKpLEamz}caring for horses."), new() { DefaultSkills.Riding, DefaultSkills.Steward }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanRichAdolescenceOnCondition, base.UrbanAdolescenceHorserOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceDockerOnApply, new("{=Ghz90npw}Your family owned a few horses at the town stables and you took charge of their care. Many evenings you would take them out beyond the walls and gallup through the fields, racing other youth."), null, 0, 0, 0, 0, 0);
			educationCategory.AddCategoryOption(new("{=vH7GtuuK}working at the stables."), new() { DefaultSkills.Riding, DefaultSkills.Steward }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanPoorAdolescenceOnCondition, base.UrbanAdolescenceHorserOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceDockerOnApply, new("{=csUq1RCC}You were employed as a hired hand at the town's stables. The overseers recognized that you had a knack for horses, and you were allowed to exercise them and sometimes even break in new steeds."), null, 0, 0, 0, 0, 0);

			characterCreation.AddNewMenu(characterCreationMenu);
		}

		protected new void AddYouthMenu(CharacterCreation characterCreation)
		{
			// CLEANUP FROM HERE
			CharacterCreationMenu characterCreationMenu = new(new("{=ok8lSW6M}Youth", null), this._youthIntroductoryText, new CharacterCreationOnInit(base.YouthOnInit), CharacterCreationMenu.MenuTypes.MultipleChoice);
			CharacterCreationCategory characterCreationCategory = characterCreationMenu.AddMenuCategory(new(base.AseraiParentsOnCondition));
			CharacterCreationCategory characterCreationCategory2 = characterCreationCategory;
			TextObject textObject = new("{=h2KnarLL}trained with the cavalry.", null);
			MBList<SkillObject> list = new();
			list.Add(DefaultSkills.Riding);
			list.Add(DefaultSkills.Polearm);
			CharacterAttribute endurance = DefaultCharacterAttributes.Endurance;
			int focusToAdd = this.FocusToAdd;
			int skillLevelToAdd = this.SkillLevelToAdd;
			int attributeLevelToAdd = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect = new(base.YouthCavalryOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects = new(base.YouthCavalryOnApply);
			TextObject textObject2 = new("{=7cHsIMLP}You could never have bought the equipment on your own but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null);
			characterCreationCategory2.AddCategoryOption(textObject, list, endurance, focusToAdd, skillLevelToAdd, attributeLevelToAdd, null, characterCreationOnSelect, characterCreationApplyFinalEffects, textObject2, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory3 = characterCreationCategory;
			TextObject textObject3 = new("partrolled the cities.", null);
			MBList<SkillObject> list2 = new();
			list2.Add(DefaultSkills.Crossbow);
			list2.Add(DefaultSkills.Engineering);
			CharacterAttribute intelligence = DefaultCharacterAttributes.Intelligence;
			int focusToAdd2 = this.FocusToAdd;
			int skillLevelToAdd2 = this.SkillLevelToAdd;
			int attributeLevelToAdd2 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect2 = new(base.YouthGarrisonOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects2 = new(base.YouthGarrisonOnApply);
			TextObject textObject4 = new("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null);
			characterCreationCategory3.AddCategoryOption(textObject3, list2, intelligence, focusToAdd2, skillLevelToAdd2, attributeLevelToAdd2, null, characterCreationOnSelect2, characterCreationApplyFinalEffects2, textObject4, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory4 = characterCreationCategory;
			TextObject textObject5 = new("joined the desert scouts.", null);
			MBList<SkillObject> list3 = new();
			list3.Add(DefaultSkills.Riding);
			list3.Add(DefaultSkills.Bow);
			CharacterAttribute endurance2 = DefaultCharacterAttributes.Endurance;
			int focusToAdd3 = this.FocusToAdd;
			int skillLevelToAdd3 = this.SkillLevelToAdd;
			int attributeLevelToAdd3 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect3 = new(base.YouthOtherOutridersOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects3 = new(base.YouthOtherOutridersOnApply);
			TextObject textObject6 = new("You couted ahead of the army.", null);
			characterCreationCategory4.AddCategoryOption(textObject5, list3, endurance2, focusToAdd3, skillLevelToAdd3, attributeLevelToAdd3, null, characterCreationOnSelect3, characterCreationApplyFinalEffects3, textObject6, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory5 = characterCreationCategory;
			TextObject textObject7 = new("{=a8arFSra}trained with the infantry.", null);
			MBList<SkillObject> list4 = new();
			list4.Add(DefaultSkills.Polearm);
			list4.Add(DefaultSkills.OneHanded);
			CharacterAttribute vigor = DefaultCharacterAttributes.Vigor;
			int focusToAdd4 = this.FocusToAdd;
			int skillLevelToAdd4 = this.SkillLevelToAdd;
			int attributeLevelToAdd4 = this.AttributeLevelToAdd;
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects4 = new(base.YouthInfantryOnApply);
			TextObject textObject8 = new("{=afH90aNs}Young Tribesmen armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Athas.", null);
			characterCreationCategory5.AddCategoryOption(textObject7, list4, vigor, focusToAdd4, skillLevelToAdd4, attributeLevelToAdd4, null, null, characterCreationApplyFinalEffects4, textObject8, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory6 = characterCreationCategory;
			TextObject textObject9 = new("{=oMbOIPc9}joined the skirmishers.", null);
			MBList<SkillObject> list5 = new();
			list5.Add(DefaultSkills.Throwing);
			list5.Add(DefaultSkills.OneHanded);
			CharacterAttribute control = DefaultCharacterAttributes.Control;
			int focusToAdd5 = this.FocusToAdd;
			int skillLevelToAdd5 = this.SkillLevelToAdd;
			int attributeLevelToAdd5 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect4 = new(base.YouthSkirmisherOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects5 = new(base.YouthSkirmisherOnApply);
			TextObject textObject10 = new("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces.", null);
			characterCreationCategory6.AddCategoryOption(textObject9, list5, control, focusToAdd5, skillLevelToAdd5, attributeLevelToAdd5, null, characterCreationOnSelect4, characterCreationApplyFinalEffects5, textObject10, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory7 = characterCreationCategory;
			TextObject textObject11 = new("{=GFUggps8}marched with the free people.", null);
			MBList<SkillObject> list6 = new();
			list6.Add(DefaultSkills.Roguery);
			list6.Add(DefaultSkills.Throwing);
			CharacterAttribute cunning = DefaultCharacterAttributes.Cunning;
			int focusToAdd6 = this.FocusToAdd;
			int skillLevelToAdd6 = this.SkillLevelToAdd;
			int attributeLevelToAdd6 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect5 = new(base.YouthCamperOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects6 = new(base.YouthCamperOnApply);
			TextObject textObject12 = new("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery.", null);
			characterCreationCategory7.AddCategoryOption(textObject11, list6, cunning, focusToAdd6, skillLevelToAdd6, attributeLevelToAdd6, null, characterCreationOnSelect5, characterCreationApplyFinalEffects6, textObject12, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory8 = characterCreationMenu.AddMenuCategory(new(base.BattanianParentsOnCondition));
			CharacterCreationCategory characterCreationCategory9 = characterCreationCategory8;
			TextObject textObject13 = new("trained with the noble guard.", null);
			MBList<SkillObject> list7 = new();
			list7.Add(DefaultSkills.Riding);
			list7.Add(DefaultSkills.Polearm);
			CharacterAttribute endurance3 = DefaultCharacterAttributes.Endurance;
			int focusToAdd7 = this.FocusToAdd;
			int skillLevelToAdd7 = this.SkillLevelToAdd;
			int attributeLevelToAdd7 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect6 = new(base.YouthCavalryOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects7 = new(base.YouthCavalryOnApply);
			TextObject textObject14 = new("{=7cHsIMLP}You could never have bought the equipment on your own but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null);
			characterCreationCategory9.AddCategoryOption(textObject13, list7, endurance3, focusToAdd7, skillLevelToAdd7, attributeLevelToAdd7, null, characterCreationOnSelect6, characterCreationApplyFinalEffects7, textObject14, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory10 = characterCreationCategory8;
			TextObject textObject15 = new("joined the folks guard", null);
			MBList<SkillObject> list8 = new();
			list8.Add(DefaultSkills.Crossbow);
			list8.Add(DefaultSkills.Engineering);
			CharacterAttribute intelligence2 = DefaultCharacterAttributes.Intelligence;
			int focusToAdd8 = this.FocusToAdd;
			int skillLevelToAdd8 = this.SkillLevelToAdd;
			int attributeLevelToAdd8 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect7 = new(base.YouthGarrisonOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects8 = new(base.YouthGarrisonOnApply);
			TextObject textObject16 = new("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null);
			characterCreationCategory10.AddCategoryOption(textObject15, list8, intelligence2, focusToAdd8, skillLevelToAdd8, attributeLevelToAdd8, null, characterCreationOnSelect7, characterCreationApplyFinalEffects8, textObject16, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory11 = characterCreationCategory8;
			TextObject textObject17 = new("rode with the scouts.", null);
			MBList<SkillObject> list9 = new();
			list9.Add(DefaultSkills.Riding);
			list9.Add(DefaultSkills.Bow);
			CharacterAttribute endurance4 = DefaultCharacterAttributes.Endurance;
			int focusToAdd9 = this.FocusToAdd;
			int skillLevelToAdd9 = this.SkillLevelToAdd;
			int attributeLevelToAdd9 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect8 = new(base.YouthOtherOutridersOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects9 = new(base.YouthOtherOutridersOnApply);
			TextObject textObject18 = new("You couted ahead of the army.", null);
			characterCreationCategory11.AddCategoryOption(textObject17, list9, endurance4, focusToAdd9, skillLevelToAdd9, attributeLevelToAdd9, null, characterCreationOnSelect8, characterCreationApplyFinalEffects9, textObject18, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory12 = characterCreationCategory8;
			TextObject textObject19 = new("trained with the Akh'Velahr.", null);
			MBList<SkillObject> list10 = new();
			list10.Add(DefaultSkills.Polearm);
			list10.Add(DefaultSkills.OneHanded);
			CharacterAttribute vigor2 = DefaultCharacterAttributes.Vigor;
			int focusToAdd10 = this.FocusToAdd;
			int skillLevelToAdd10 = this.SkillLevelToAdd;
			int attributeLevelToAdd10 = this.AttributeLevelToAdd;
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects10 = new(base.YouthInfantryOnApply);
			TextObject textObject20 = new("{=afH90aNs}Armed with Spear and shield, the Akh´Velahr is the bulk of the Elvean forces, drawned from the smallholding farmers.", null);
			characterCreationCategory12.AddCategoryOption(textObject19, list10, vigor2, focusToAdd10, skillLevelToAdd10, attributeLevelToAdd10, null, null, characterCreationApplyFinalEffects10, textObject20, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory13 = characterCreationCategory8;
			TextObject textObject21 = new("joined the Arakhora.", null);
			MBList<SkillObject> list11 = new();
			list11.Add(DefaultSkills.Roguery);
			list11.Add(DefaultSkills.Throwing);
			CharacterAttribute cunning2 = DefaultCharacterAttributes.Cunning;
			int focusToAdd11 = this.FocusToAdd;
			int skillLevelToAdd11 = this.SkillLevelToAdd;
			int attributeLevelToAdd11 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect9 = new(base.YouthCamperOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects11 = new(base.YouthCamperOnApply);
			TextObject textObject22 = new("{=64rWqBLN}Arakhor, an Elvean term that translates loosely as - one who protects the forest -, were the Scounts sent to the borders of the Realm to watch over possible treats. Often you needed  trick your way into foreign armies and cities, cheating, entertaining, whatever disguise was at your disposal.", null);
			characterCreationCategory13.AddCategoryOption(textObject21, list11, cunning2, focusToAdd11, skillLevelToAdd11, attributeLevelToAdd11, null, characterCreationOnSelect9, characterCreationApplyFinalEffects11, textObject22, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory14 = characterCreationMenu.AddMenuCategory(new(base.EmpireParentsOnCondition));
			CharacterCreationCategory characterCreationCategory15 = characterCreationCategory14;
			TextObject textObject23 = new("trained with the cavalry.", null);
			MBList<SkillObject> list12 = new();
			list12.Add(DefaultSkills.Riding);
			list12.Add(DefaultSkills.Polearm);
			CharacterAttribute endurance5 = DefaultCharacterAttributes.Endurance;
			int focusToAdd12 = this.FocusToAdd;
			int skillLevelToAdd12 = this.SkillLevelToAdd;
			int attributeLevelToAdd12 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect10 = new(base.YouthCavalryOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects12 = new(base.YouthCavalryOnApply);
			TextObject textObject24 = new("{=7cHsIMLP}You could never have bought the equipment on your own but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null);
			characterCreationCategory15.AddCategoryOption(textObject23, list12, endurance5, focusToAdd12, skillLevelToAdd12, attributeLevelToAdd12, null, characterCreationOnSelect10, characterCreationApplyFinalEffects12, textObject24, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory16 = characterCreationCategory14;
			TextObject textObject25 = new("served in the Hall of Men.", null);
			MBList<SkillObject> list13 = new();
			list13.Add(DefaultSkills.Crossbow);
			list13.Add(DefaultSkills.Engineering);
			CharacterAttribute intelligence3 = DefaultCharacterAttributes.Intelligence;
			int focusToAdd13 = this.FocusToAdd;
			int skillLevelToAdd13 = this.SkillLevelToAdd;
			int attributeLevelToAdd13 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect11 = new(base.YouthGarrisonOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects13 = new(base.YouthGarrisonOnApply);
			TextObject textObject26 = new("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null);
			characterCreationCategory16.AddCategoryOption(textObject25, list13, intelligence3, focusToAdd13, skillLevelToAdd13, attributeLevelToAdd13, null, characterCreationOnSelect11, characterCreationApplyFinalEffects13, textObject26, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory17 = characterCreationCategory14;
			TextObject textObject27 = new("stood guard with the garrisons.", null);
			MBList<SkillObject> list14 = new();
			list14.Add(DefaultSkills.Throwing);
			list14.Add(DefaultSkills.OneHanded);
			CharacterAttribute control2 = DefaultCharacterAttributes.Control;
			int focusToAdd14 = this.FocusToAdd;
			int skillLevelToAdd14 = this.SkillLevelToAdd;
			int attributeLevelToAdd14 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect12 = new(base.YouthSkirmisherOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects14 = new(base.YouthSkirmisherOnApply);
			TextObject textObject28 = new("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces.", null);
			characterCreationCategory17.AddCategoryOption(textObject27, list14, control2, focusToAdd14, skillLevelToAdd14, attributeLevelToAdd14, null, characterCreationOnSelect12, characterCreationApplyFinalEffects14, textObject28, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory18 = characterCreationCategory14;
			TextObject textObject29 = new("rode with the scouts.", null);
			MBList<SkillObject> list15 = new();
			list15.Add(DefaultSkills.Riding);
			list15.Add(DefaultSkills.Bow);
			CharacterAttribute endurance6 = DefaultCharacterAttributes.Endurance;
			int focusToAdd15 = this.FocusToAdd;
			int skillLevelToAdd15 = this.SkillLevelToAdd;
			int attributeLevelToAdd15 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect13 = new(base.YouthOtherOutridersOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects15 = new(base.YouthOtherOutridersOnApply);
			TextObject textObject30 = new("You couted ahead of the army.", null);
			characterCreationCategory18.AddCategoryOption(textObject29, list15, endurance6, focusToAdd15, skillLevelToAdd15, attributeLevelToAdd15, null, characterCreationOnSelect13, characterCreationApplyFinalEffects15, textObject30, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory19 = characterCreationCategory14;
			TextObject textObject31 = new("joined the spear bearers.", null);
			MBList<SkillObject> list16 = new();
			list16.Add(DefaultSkills.Polearm);
			list16.Add(DefaultSkills.OneHanded);
			CharacterAttribute vigor3 = DefaultCharacterAttributes.Vigor;
			int focusToAdd16 = this.FocusToAdd;
			int skillLevelToAdd16 = this.SkillLevelToAdd;
			int attributeLevelToAdd16 = this.AttributeLevelToAdd;
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects16 = new(base.YouthInfantryOnApply);
			TextObject textObject32 = new("{=afH90aNs}Levy armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Athas.", null);
			characterCreationCategory19.AddCategoryOption(textObject31, list16, vigor3, focusToAdd16, skillLevelToAdd16, attributeLevelToAdd16, null, null, characterCreationApplyFinalEffects16, textObject32, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory20 = characterCreationCategory14;
			TextObject textObject33 = new("trained with the infantry.", null);
			MBList<SkillObject> list17 = new();
			list17.Add(DefaultSkills.Roguery);
			list17.Add(DefaultSkills.Throwing);
			CharacterAttribute cunning3 = DefaultCharacterAttributes.Cunning;
			int focusToAdd17 = this.FocusToAdd;
			int skillLevelToAdd17 = this.SkillLevelToAdd;
			int attributeLevelToAdd17 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect14 = new(base.YouthCamperOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects17 = new(base.YouthCamperOnApply);
			TextObject textObject34 = new("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery.", null);
			characterCreationCategory20.AddCategoryOption(textObject33, list17, cunning3, focusToAdd17, skillLevelToAdd17, attributeLevelToAdd17, null, characterCreationOnSelect14, characterCreationApplyFinalEffects17, textObject34, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory21 = characterCreationMenu.AddMenuCategory(new(base.VlandianParentsOnCondition));
			CharacterCreationCategory characterCreationCategory22 = characterCreationCategory21;
			TextObject textObject35 = new("trained with the Nasoria cavalry.", null);
			MBList<SkillObject> list18 = new();
			list18.Add(DefaultSkills.Riding);
			list18.Add(DefaultSkills.Polearm);
			CharacterAttribute endurance7 = DefaultCharacterAttributes.Endurance;
			int focusToAdd18 = this.FocusToAdd;
			int skillLevelToAdd18 = this.SkillLevelToAdd;
			int attributeLevelToAdd18 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect15 = new(base.YouthCavalryOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects18 = new(base.YouthCavalryOnApply);
			TextObject textObject36 = new("{=7cHsIMLP}You could never have bought the equipment on your own but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null);
			characterCreationCategory22.AddCategoryOption(textObject35, list18, endurance7, focusToAdd18, skillLevelToAdd18, attributeLevelToAdd18, null, characterCreationOnSelect15, characterCreationApplyFinalEffects18, textObject36, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory23 = characterCreationCategory21;
			TextObject textObject37 = new("patrolled the cities.", null);
			MBList<SkillObject> list19 = new();
			list19.Add(DefaultSkills.Crossbow);
			list19.Add(DefaultSkills.Engineering);
			CharacterAttribute intelligence4 = DefaultCharacterAttributes.Intelligence;
			int focusToAdd19 = this.FocusToAdd;
			int skillLevelToAdd19 = this.SkillLevelToAdd;
			int attributeLevelToAdd19 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect16 = new(base.YouthGarrisonOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects19 = new(base.YouthGarrisonOnApply);
			TextObject textObject38 = new("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null);
			characterCreationCategory23.AddCategoryOption(textObject37, list19, intelligence4, focusToAdd19, skillLevelToAdd19, attributeLevelToAdd19, null, characterCreationOnSelect16, characterCreationApplyFinalEffects19, textObject38, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory24 = characterCreationCategory21;
			TextObject textObject39 = new("trained with the infantry.", null);
			MBList<SkillObject> list20 = new();
			list20.Add(DefaultSkills.Throwing);
			list20.Add(DefaultSkills.OneHanded);
			CharacterAttribute control3 = DefaultCharacterAttributes.Control;
			int focusToAdd20 = this.FocusToAdd;
			int skillLevelToAdd20 = this.SkillLevelToAdd;
			int attributeLevelToAdd20 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect17 = new(base.YouthSkirmisherOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects20 = new(base.YouthSkirmisherOnApply);
			TextObject textObject40 = new("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces.", null);
			characterCreationCategory24.AddCategoryOption(textObject39, list20, control3, focusToAdd20, skillLevelToAdd20, attributeLevelToAdd20, null, characterCreationOnSelect17, characterCreationApplyFinalEffects20, textObject40, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory25 = characterCreationCategory21;
			TextObject textObject41 = new("rode with the scouts.", null);
			MBList<SkillObject> list21 = new();
			list21.Add(DefaultSkills.Riding);
			list21.Add(DefaultSkills.Bow);
			CharacterAttribute endurance8 = DefaultCharacterAttributes.Endurance;
			int focusToAdd21 = this.FocusToAdd;
			int skillLevelToAdd21 = this.SkillLevelToAdd;
			int attributeLevelToAdd21 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect18 = new(base.YouthOtherOutridersOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects21 = new(base.YouthOtherOutridersOnApply);
			TextObject textObject42 = new("You couted ahead of the army.", null);
			characterCreationCategory25.AddCategoryOption(textObject41, list21, endurance8, focusToAdd21, skillLevelToAdd21, attributeLevelToAdd21, null, characterCreationOnSelect18, characterCreationApplyFinalEffects21, textObject42, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory26 = characterCreationCategory21;
			TextObject textObject43 = new("joined the spearman front.", null);
			MBList<SkillObject> list22 = new();
			list22.Add(DefaultSkills.Polearm);
			list22.Add(DefaultSkills.OneHanded);
			CharacterAttribute vigor4 = DefaultCharacterAttributes.Vigor;
			int focusToAdd22 = this.FocusToAdd;
			int skillLevelToAdd22 = this.SkillLevelToAdd;
			int attributeLevelToAdd22 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect19 = new(base.YouthInfantryOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects22 = new(base.YouthInfantryOnApply);
			TextObject textObject44 = new("{=afH90aNs}Levy armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Athas.", null);
			characterCreationCategory26.AddCategoryOption(textObject43, list22, vigor4, focusToAdd22, skillLevelToAdd22, attributeLevelToAdd22, null, characterCreationOnSelect19, characterCreationApplyFinalEffects22, textObject44, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory27 = characterCreationCategory21;
			TextObject textObject45 = new("marched with the camp followers.", null);
			MBList<SkillObject> list23 = new();
			list23.Add(DefaultSkills.Roguery);
			list23.Add(DefaultSkills.Throwing);
			CharacterAttribute cunning4 = DefaultCharacterAttributes.Cunning;
			int focusToAdd23 = this.FocusToAdd;
			int skillLevelToAdd23 = this.SkillLevelToAdd;
			int attributeLevelToAdd23 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect20 = new(base.YouthCamperOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects23 = new(base.YouthCamperOnApply);
			TextObject textObject46 = new("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery.", null);
			characterCreationCategory27.AddCategoryOption(textObject45, list23, cunning4, focusToAdd23, skillLevelToAdd23, attributeLevelToAdd23, null, characterCreationOnSelect20, characterCreationApplyFinalEffects23, textObject46, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory28 = characterCreationMenu.AddMenuCategory(new(base.KhuzaitParentsOnCondition));
			CharacterCreationCategory characterCreationCategory29 = characterCreationCategory28;
			TextObject textObject47 = new("served the Al-Khuur cavalry.", null);
			MBList<SkillObject> list24 = new();
			list24.Add(DefaultSkills.Riding);
			list24.Add(DefaultSkills.Polearm);
			CharacterAttribute endurance9 = DefaultCharacterAttributes.Endurance;
			int focusToAdd24 = this.FocusToAdd;
			int skillLevelToAdd24 = this.SkillLevelToAdd;
			int attributeLevelToAdd24 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect21 = new(base.YouthCavalryOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects24 = new(base.YouthCavalryOnApply);
			TextObject textObject48 = new("{=7cHsIMLP}You could never have bought the equipment on your own but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null);
			characterCreationCategory29.AddCategoryOption(textObject47, list24, endurance9, focusToAdd24, skillLevelToAdd24, attributeLevelToAdd24, null, characterCreationOnSelect21, characterCreationApplyFinalEffects24, textObject48, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory30 = characterCreationCategory28;
			TextObject textObject49 = new("patrolled the villages and cities.", null);
			MBList<SkillObject> list25 = new();
			list25.Add(DefaultSkills.Crossbow);
			list25.Add(DefaultSkills.Engineering);
			CharacterAttribute intelligence5 = DefaultCharacterAttributes.Intelligence;
			int focusToAdd25 = this.FocusToAdd;
			int skillLevelToAdd25 = this.SkillLevelToAdd;
			int attributeLevelToAdd25 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect22 = new(base.YouthGarrisonOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects25 = new(base.YouthGarrisonOnApply);
			TextObject textObject50 = new("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null);
			characterCreationCategory30.AddCategoryOption(textObject49, list25, intelligence5, focusToAdd25, skillLevelToAdd25, attributeLevelToAdd25, null, characterCreationOnSelect22, characterCreationApplyFinalEffects25, textObject50, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory31 = characterCreationCategory28;
			TextObject textObject51 = new("trained with the infrantry.", null);
			MBList<SkillObject> list26 = new();
			list26.Add(DefaultSkills.Throwing);
			list26.Add(DefaultSkills.OneHanded);
			CharacterAttribute control4 = DefaultCharacterAttributes.Control;
			int focusToAdd26 = this.FocusToAdd;
			int skillLevelToAdd26 = this.SkillLevelToAdd;
			int attributeLevelToAdd26 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect23 = new(base.YouthSkirmisherOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects26 = new(base.YouthSkirmisherOnApply);
			TextObject textObject52 = new("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces.", null);
			characterCreationCategory31.AddCategoryOption(textObject51, list26, control4, focusToAdd26, skillLevelToAdd26, attributeLevelToAdd26, null, characterCreationOnSelect23, characterCreationApplyFinalEffects26, textObject52, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory32 = characterCreationCategory28;
			TextObject textObject53 = new("rode with the scouts.", null);
			MBList<SkillObject> list27 = new();
			list27.Add(DefaultSkills.Riding);
			list27.Add(DefaultSkills.Bow);
			CharacterAttribute endurance10 = DefaultCharacterAttributes.Endurance;
			int focusToAdd27 = this.FocusToAdd;
			int skillLevelToAdd27 = this.SkillLevelToAdd;
			int attributeLevelToAdd27 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect24 = new(base.YouthOtherOutridersOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects27 = new(base.YouthOtherOutridersOnApply);
			TextObject textObject54 = new("You couted ahead of the army.", null);
			characterCreationCategory32.AddCategoryOption(textObject53, list27, endurance10, focusToAdd27, skillLevelToAdd27, attributeLevelToAdd27, null, characterCreationOnSelect24, characterCreationApplyFinalEffects27, textObject54, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory33 = characterCreationCategory28;
			TextObject textObject55 = new("joined the spearmen.", null);
			MBList<SkillObject> list28 = new();
			list28.Add(DefaultSkills.Polearm);
			list28.Add(DefaultSkills.OneHanded);
			CharacterAttribute vigor5 = DefaultCharacterAttributes.Vigor;
			int focusToAdd28 = this.FocusToAdd;
			int skillLevelToAdd28 = this.SkillLevelToAdd;
			int attributeLevelToAdd28 = this.AttributeLevelToAdd;
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects28 = new(base.YouthInfantryOnApply);
			TextObject textObject56 = new("{=afH90aNs}Levy armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Athas.", null);
			characterCreationCategory33.AddCategoryOption(textObject55, list28, vigor5, focusToAdd28, skillLevelToAdd28, attributeLevelToAdd28, null, null, characterCreationApplyFinalEffects28, textObject56, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory34 = characterCreationCategory28;
			TextObject textObject57 = new("marched with the campfollowers.", null);
			MBList<SkillObject> list29 = new();
			list29.Add(DefaultSkills.Roguery);
			list29.Add(DefaultSkills.Throwing);
			CharacterAttribute cunning5 = DefaultCharacterAttributes.Cunning;
			int focusToAdd29 = this.FocusToAdd;
			int skillLevelToAdd29 = this.SkillLevelToAdd;
			int attributeLevelToAdd29 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect25 = new(base.YouthCamperOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects29 = new(base.YouthCamperOnApply);
			TextObject textObject58 = new("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery.", null);
			characterCreationCategory34.AddCategoryOption(textObject57, list29, cunning5, focusToAdd29, skillLevelToAdd29, attributeLevelToAdd29, null, characterCreationOnSelect25, characterCreationApplyFinalEffects29, textObject58, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory35 = characterCreationMenu.AddMenuCategory(new(base.SturgianParentsOnCondition));
			CharacterCreationCategory characterCreationCategory36 = characterCreationCategory35;
			TextObject textObject59 = new("served in the Dreadlords bodyguard.", null);
			MBList<SkillObject> list30 = new();
			list30.Add(DefaultSkills.Riding);
			list30.Add(DefaultSkills.Polearm);
			CharacterAttribute endurance11 = DefaultCharacterAttributes.Endurance;
			int focusToAdd30 = this.FocusToAdd;
			int skillLevelToAdd30 = this.SkillLevelToAdd;
			int attributeLevelToAdd30 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect26 = new(base.YouthCavalryOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects30 = new(base.YouthCavalryOnApply);
			TextObject textObject60 = new("Protecting your dreadlord was your main duty.", null);
			characterCreationCategory36.AddCategoryOption(textObject59, list30, endurance11, focusToAdd30, skillLevelToAdd30, attributeLevelToAdd30, null, characterCreationOnSelect26, characterCreationApplyFinalEffects30, textObject60, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory37 = characterCreationCategory35;
			TextObject textObject61 = new("stood guard with the garrisons.", null);
			MBList<SkillObject> list31 = new();
			list31.Add(DefaultSkills.Crossbow);
			list31.Add(DefaultSkills.Engineering);
			CharacterAttribute intelligence6 = DefaultCharacterAttributes.Intelligence;
			int focusToAdd31 = this.FocusToAdd;
			int skillLevelToAdd31 = this.SkillLevelToAdd;
			int attributeLevelToAdd31 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect27 = new(base.YouthGarrisonOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects31 = new(base.YouthGarrisonOnApply);
			TextObject textObject62 = new("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null);
			characterCreationCategory37.AddCategoryOption(textObject61, list31, intelligence6, focusToAdd31, skillLevelToAdd31, attributeLevelToAdd31, null, characterCreationOnSelect27, characterCreationApplyFinalEffects31, textObject62, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory38 = characterCreationCategory35;
			TextObject textObject63 = new("trained with the infrantry.", null);
			MBList<SkillObject> list32 = new();
			list32.Add(DefaultSkills.Throwing);
			list32.Add(DefaultSkills.OneHanded);
			CharacterAttribute control5 = DefaultCharacterAttributes.Control;
			int focusToAdd32 = this.FocusToAdd;
			int skillLevelToAdd32 = this.SkillLevelToAdd;
			int attributeLevelToAdd32 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect28 = new(base.YouthSkirmisherOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects32 = new(base.YouthSkirmisherOnApply);
			TextObject textObject64 = new("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces.", null);
			characterCreationCategory38.AddCategoryOption(textObject63, list32, control5, focusToAdd32, skillLevelToAdd32, attributeLevelToAdd32, null, characterCreationOnSelect28, characterCreationApplyFinalEffects32, textObject64, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory39 = characterCreationCategory35;
			TextObject textObject65 = new("rode with the scouts.", null);
			MBList<SkillObject> list33 = new();
			list33.Add(DefaultSkills.Riding);
			list33.Add(DefaultSkills.Bow);
			CharacterAttribute endurance12 = DefaultCharacterAttributes.Endurance;
			int focusToAdd33 = this.FocusToAdd;
			int skillLevelToAdd33 = this.SkillLevelToAdd;
			int attributeLevelToAdd33 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect29 = new(base.YouthOtherOutridersOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects33 = new(base.YouthOtherOutridersOnApply);
			TextObject textObject66 = new("You couted ahead of the army.", null);
			characterCreationCategory39.AddCategoryOption(textObject65, list33, endurance12, focusToAdd33, skillLevelToAdd33, attributeLevelToAdd33, null, characterCreationOnSelect29, characterCreationApplyFinalEffects33, textObject66, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory40 = characterCreationCategory35;
			TextObject textObject67 = new("joined the skirmishers.", null);
			MBList<SkillObject> list34 = new();
			list34.Add(DefaultSkills.Polearm);
			list34.Add(DefaultSkills.OneHanded);
			CharacterAttribute vigor6 = DefaultCharacterAttributes.Vigor;
			int focusToAdd34 = this.FocusToAdd;
			int skillLevelToAdd34 = this.SkillLevelToAdd;
			int attributeLevelToAdd34 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect30 = new(base.YouthInfantryOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects34 = new(base.YouthInfantryOnApply);
			TextObject textObject68 = new("{=afH90aNs}Levy armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Athas.", null);
			characterCreationCategory40.AddCategoryOption(textObject67, list34, vigor6, focusToAdd34, skillLevelToAdd34, attributeLevelToAdd34, null, characterCreationOnSelect30, characterCreationApplyFinalEffects34, textObject68, null, 0, 0, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory41 = characterCreationCategory35;
			TextObject textObject69 = new("marched with the army retinue.", null);
			MBList<SkillObject> list35 = new();
			list35.Add(DefaultSkills.Roguery);
			list35.Add(DefaultSkills.Throwing);
			CharacterAttribute cunning6 = DefaultCharacterAttributes.Cunning;
			int focusToAdd35 = this.FocusToAdd;
			int skillLevelToAdd35 = this.SkillLevelToAdd;
			int attributeLevelToAdd35 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect31 = new(base.YouthCamperOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects35 = new(base.YouthCamperOnApply);
			TextObject textObject70 = new("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery.", null);
			characterCreationCategory41.AddCategoryOption(textObject69, list35, cunning6, focusToAdd35, skillLevelToAdd35, attributeLevelToAdd35, null, characterCreationOnSelect31, characterCreationApplyFinalEffects35, textObject70, null, 0, 0, 0, 0, 0);
			characterCreation.AddNewMenu(characterCreationMenu);
		}

		protected new void AddAdulthoodMenu(CharacterCreation characterCreation)
		{
			MBTextManager.SetTextVariable("EXP_VALUE", this.SkillLevelToAdd);
			CharacterCreationMenu characterCreationMenu = new(new("{=MafIe9yI}Young Adulthood", null), new("{=4WYY0X59}Before you set out for a life of adventure, your biggest achievement was...", null), new CharacterCreationOnInit(base.AccomplishmentOnInit), CharacterCreationMenu.MenuTypes.MultipleChoice);
			CharacterCreationCategory characterCreationCategory = characterCreationMenu.AddMenuCategory(null);
			CharacterCreationCategory characterCreationCategory2 = characterCreationCategory;
			TextObject textObject = new("{=8bwpVpgy}you defeated an enemy in battle.", null);
			MBList<SkillObject> list = new();
			list.Add(DefaultSkills.OneHanded);
			list.Add(DefaultSkills.TwoHanded);
			CharacterAttribute vigor = DefaultCharacterAttributes.Vigor;
			int focusToAdd = this.FocusToAdd;
			int skillLevelToAdd = this.SkillLevelToAdd;
			int attributeLevelToAdd = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect = new(base.AccomplishmentDefeatedEnemyOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects = new(base.AccomplishmentDefeatedEnemyOnApply);
			TextObject textObject2 = new("{=1IEroJKs}Not everyone who musters for the levy marches to war, and not everyone who goes on campaign sees action. You did both, and you also took down an enemy warrior in direct one-to-one combat, in the full view of your comrades.", null);
			characterCreationCategory2.AddCategoryOption(textObject, list, vigor, focusToAdd, skillLevelToAdd, attributeLevelToAdd, null, characterCreationOnSelect, characterCreationApplyFinalEffects, textObject2, new MBList<TraitObject>
			{
				DefaultTraits.Valor
			}, 1, 20, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory3 = characterCreationCategory;
			TextObject textObject3 = new("{=mP3uFbcq}you led a successful manhunt.", null);
			MBList<SkillObject> list2 = new();
			list2.Add(DefaultSkills.Tactics);
			list2.Add(DefaultSkills.Leadership);
			CharacterAttribute cunning = DefaultCharacterAttributes.Cunning;
			int focusToAdd2 = this.FocusToAdd;
			int skillLevelToAdd2 = this.SkillLevelToAdd;
			int attributeLevelToAdd2 = this.AttributeLevelToAdd;
			CharacterCreationOnCondition characterCreationOnCondition = new(base.AccomplishmentPosseOnConditions);
			CharacterCreationOnSelect characterCreationOnSelect2 = new(base.AccomplishmentExpeditionOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects2 = new(base.AccomplishmentExpeditionOnApply);
			TextObject textObject4 = new("{=4f5xwzX0}When your community needed to organize a posse to pursue horse thieves, you were the obvious choice. You hunted down the raiders, surrounded them and forced their surrender, and took back your stolen property.", null);
			characterCreationCategory3.AddCategoryOption(textObject3, list2, cunning, focusToAdd2, skillLevelToAdd2, attributeLevelToAdd2, characterCreationOnCondition, characterCreationOnSelect2, characterCreationApplyFinalEffects2, textObject4, new MBList<TraitObject>
			{
				DefaultTraits.Calculating
			}, 1, 10, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory4 = characterCreationCategory;
			TextObject textObject5 = new("{=wfbtS71d}you led a caravan.", null);
			MBList<SkillObject> list3 = new();
			list3.Add(DefaultSkills.Tactics);
			list3.Add(DefaultSkills.Leadership);
			CharacterAttribute cunning2 = DefaultCharacterAttributes.Cunning;
			int focusToAdd3 = this.FocusToAdd;
			int skillLevelToAdd3 = this.SkillLevelToAdd;
			int attributeLevelToAdd3 = this.AttributeLevelToAdd;
			CharacterCreationOnCondition characterCreationOnCondition2 = new(base.AccomplishmentMerchantOnCondition);
			CharacterCreationOnSelect characterCreationOnSelect3 = new(base.AccomplishmentMerchantOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects3 = new(base.AccomplishmentExpeditionOnApply);
			TextObject textObject6 = new("{=joRHKCkm}Your family needed someone trustworthy to take a caravan to a neighboring town. You organized supplies, ensured a constant watch to keep away bandits, and brought it safely to its destination.", null);
			characterCreationCategory4.AddCategoryOption(textObject5, list3, cunning2, focusToAdd3, skillLevelToAdd3, attributeLevelToAdd3, characterCreationOnCondition2, characterCreationOnSelect3, characterCreationApplyFinalEffects3, textObject6, new MBList<TraitObject>
			{
				DefaultTraits.Calculating
			}, 1, 10, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory5 = characterCreationCategory;
			TextObject textObject7 = new("{=x1HTX5hq}you saved your village from a flood.", null);
			MBList<SkillObject> list4 = new();
			list4.Add(DefaultSkills.Tactics);
			list4.Add(DefaultSkills.Leadership);
			CharacterAttribute cunning3 = DefaultCharacterAttributes.Cunning;
			int focusToAdd4 = this.FocusToAdd;
			int skillLevelToAdd4 = this.SkillLevelToAdd;
			int attributeLevelToAdd4 = this.AttributeLevelToAdd;
			CharacterCreationOnCondition characterCreationOnCondition3 = new(base.AccomplishmentSavedVillageOnCondition);
			CharacterCreationOnSelect characterCreationOnSelect4 = new(base.AccomplishmentSavedVillageOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects4 = new(base.AccomplishmentExpeditionOnApply);
			TextObject textObject8 = new("{=bWlmGDf3}When a sudden storm caused the local stream to rise suddenly, your neighbors needed quick-thinking leadership. You provided it, directing them to build levees to save their homes.", null);
			characterCreationCategory5.AddCategoryOption(textObject7, list4, cunning3, focusToAdd4, skillLevelToAdd4, attributeLevelToAdd4, characterCreationOnCondition3, characterCreationOnSelect4, characterCreationApplyFinalEffects4, textObject8, new MBList<TraitObject>
			{
				DefaultTraits.Calculating
			}, 1, 10, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory6 = characterCreationCategory;
			TextObject textObject9 = new("{=s8PNllPN}you saved your city quarter from a fire.", null);
			MBList<SkillObject> list5 = new();
			list5.Add(DefaultSkills.Tactics);
			list5.Add(DefaultSkills.Leadership);
			CharacterAttribute cunning4 = DefaultCharacterAttributes.Cunning;
			int focusToAdd5 = this.FocusToAdd;
			int skillLevelToAdd5 = this.SkillLevelToAdd;
			int attributeLevelToAdd5 = this.AttributeLevelToAdd;
			CharacterCreationOnCondition characterCreationOnCondition4 = new(base.AccomplishmentSavedStreetOnCondition);
			CharacterCreationOnSelect characterCreationOnSelect5 = new(base.AccomplishmentSavedStreetOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects5 = new(base.AccomplishmentExpeditionOnApply);
			TextObject textObject10 = new("{=ZAGR6PYc}When a sudden blaze broke out in a back alley, your neighbors needed quick-thinking leadership and you provided it. You organized a bucket line to the nearest well, putting the fire out before any homes were lost.", null);
			characterCreationCategory6.AddCategoryOption(textObject9, list5, cunning4, focusToAdd5, skillLevelToAdd5, attributeLevelToAdd5, characterCreationOnCondition4, characterCreationOnSelect5, characterCreationApplyFinalEffects5, textObject10, new MBList<TraitObject>
			{
				DefaultTraits.Calculating
			}, 1, 10, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory7 = characterCreationCategory;
			TextObject textObject11 = new("{=xORjDTal}you invested some money in a workshop.", null);
			MBList<SkillObject> list6 = new();
			list6.Add(DefaultSkills.Trade);
			list6.Add(DefaultSkills.Crafting);
			CharacterAttribute intelligence = DefaultCharacterAttributes.Intelligence;
			int focusToAdd6 = this.FocusToAdd;
			int skillLevelToAdd6 = this.SkillLevelToAdd;
			int attributeLevelToAdd6 = this.AttributeLevelToAdd;
			CharacterCreationOnCondition characterCreationOnCondition5 = new(base.AccomplishmentUrbanOnCondition);
			CharacterCreationOnSelect characterCreationOnSelect6 = new(base.AccomplishmentWorkshopOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects6 = new(base.AccomplishmentWorkshopOnApply);
			TextObject textObject12 = new("{=PyVqDLBu}Your parents didn't give you much money, but they did leave just enough for you to secure a loan against a larger amount to build a small workshop. You paid back what you borrowed, and sold your enterprise for a profit.", null);
			characterCreationCategory7.AddCategoryOption(textObject11, list6, intelligence, focusToAdd6, skillLevelToAdd6, attributeLevelToAdd6, characterCreationOnCondition5, characterCreationOnSelect6, characterCreationApplyFinalEffects6, textObject12, new MBList<TraitObject>
			{
				DefaultTraits.Calculating
			}, 1, 10, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory8 = characterCreationCategory;
			TextObject textObject13 = new("{=xKXcqRJI}you invested some money in land.", null);
			MBList<SkillObject> list7 = new();
			list7.Add(DefaultSkills.Trade);
			list7.Add(DefaultSkills.Crafting);
			CharacterAttribute intelligence2 = DefaultCharacterAttributes.Intelligence;
			int focusToAdd7 = this.FocusToAdd;
			int skillLevelToAdd7 = this.SkillLevelToAdd;
			int attributeLevelToAdd7 = this.AttributeLevelToAdd;
			CharacterCreationOnCondition characterCreationOnCondition6 = new(base.AccomplishmentRuralOnCondition);
			CharacterCreationOnSelect characterCreationOnSelect7 = new(base.AccomplishmentWorkshopOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects7 = new(base.AccomplishmentWorkshopOnApply);
			TextObject textObject14 = new("{=cbF9jdQo}Your parents didn't give you much money, but they did leave just enough for you to purchase a plot of unused land at the edge of the village. You cleared away rocks and dug an irrigation ditch, raised a few seasons of crops, than sold it for a considerable profit.", null);
			characterCreationCategory8.AddCategoryOption(textObject13, list7, intelligence2, focusToAdd7, skillLevelToAdd7, attributeLevelToAdd7, characterCreationOnCondition6, characterCreationOnSelect7, characterCreationApplyFinalEffects7, textObject14, new MBList<TraitObject>
			{
				DefaultTraits.Calculating
			}, 1, 10, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory9 = characterCreationCategory;
			TextObject textObject15 = new("{=TbNRtUjb}you hunted a dangerous animal.", null);
			MBList<SkillObject> list8 = new();
			list8.Add(DefaultSkills.Polearm);
			list8.Add(DefaultSkills.Crossbow);
			CharacterAttribute control = DefaultCharacterAttributes.Control;
			int focusToAdd8 = this.FocusToAdd;
			int skillLevelToAdd8 = this.SkillLevelToAdd;
			int attributeLevelToAdd8 = this.AttributeLevelToAdd;
			CharacterCreationOnCondition characterCreationOnCondition7 = new(base.AccomplishmentRuralOnCondition);
			CharacterCreationOnSelect characterCreationOnSelect8 = new(base.AccomplishmentSiegeHunterOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects8 = new(base.AccomplishmentSiegeHunterOnApply);
			TextObject textObject16 = new("{=I3PcdaaL}Wolves, bears are a constant menace to the flocks of northern Athas, while hyenas and leopards trouble the south. You went with a group of your fellow villagers and fired the missile that brought down the beast.", null);
			characterCreationCategory9.AddCategoryOption(textObject15, list8, control, focusToAdd8, skillLevelToAdd8, attributeLevelToAdd8, characterCreationOnCondition7, characterCreationOnSelect8, characterCreationApplyFinalEffects8, textObject16, null, 0, 5, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory10 = characterCreationCategory;
			TextObject textObject17 = new("{=WbHfGCbd}you survived a siege.", null);
			MBList<SkillObject> list9 = new();
			list9.Add(DefaultSkills.Bow);
			list9.Add(DefaultSkills.Crossbow);
			CharacterAttribute control2 = DefaultCharacterAttributes.Control;
			int focusToAdd9 = this.FocusToAdd;
			int skillLevelToAdd9 = this.SkillLevelToAdd;
			int attributeLevelToAdd9 = this.AttributeLevelToAdd;
			CharacterCreationOnCondition characterCreationOnCondition8 = new(base.AccomplishmentUrbanOnCondition);
			CharacterCreationOnSelect characterCreationOnSelect9 = new(base.AccomplishmentSiegeHunterOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects9 = new(base.AccomplishmentSiegeHunterOnApply);
			TextObject textObject18 = new("{=FhZPjhli}Your hometown was briefly placed under siege, and you were called to defend the walls. Everyone did their part to repulse the enemy assault, and everyone is justly proud of what they endured.", null);
			characterCreationCategory10.AddCategoryOption(textObject17, list9, control2, focusToAdd9, skillLevelToAdd9, attributeLevelToAdd9, characterCreationOnCondition8, characterCreationOnSelect9, characterCreationApplyFinalEffects9, textObject18, null, 0, 5, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory11 = characterCreationCategory;
			TextObject textObject19 = new("{=kNXet6Um}you had a famous escapade in town.", null);
			MBList<SkillObject> list10 = new();
			list10.Add(DefaultSkills.Athletics);
			list10.Add(DefaultSkills.Roguery);
			CharacterAttribute endurance = DefaultCharacterAttributes.Endurance;
			int focusToAdd10 = this.FocusToAdd;
			int skillLevelToAdd10 = this.SkillLevelToAdd;
			int attributeLevelToAdd10 = this.AttributeLevelToAdd;
			CharacterCreationOnCondition characterCreationOnCondition9 = new(base.AccomplishmentRuralOnCondition);
			CharacterCreationOnSelect characterCreationOnSelect10 = new(base.AccomplishmentEscapadeOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects10 = new(base.AccomplishmentEscapadeOnApply);
			TextObject textObject20 = new("{=DjeAJtix}Maybe it was a love affair, or maybe you cheated at dice, or maybe you just chose your words poorly when drinking with a dangerous crowd. Anyway, on one of your trips into town you got into the kind of trouble from which only a quick tongue or quick feet get you out alive.", null);
			characterCreationCategory11.AddCategoryOption(textObject19, list10, endurance, focusToAdd10, skillLevelToAdd10, attributeLevelToAdd10, characterCreationOnCondition9, characterCreationOnSelect10, characterCreationApplyFinalEffects10, textObject20, new MBList<TraitObject>
			{
				DefaultTraits.Valor
			}, 1, 5, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory12 = characterCreationCategory;
			TextObject textObject21 = new("{=qlOuiKXj}you had a famous escapade.", null);
			MBList<SkillObject> list11 = new();
			list11.Add(DefaultSkills.Athletics);
			list11.Add(DefaultSkills.Roguery);
			CharacterAttribute endurance2 = DefaultCharacterAttributes.Endurance;
			int focusToAdd11 = this.FocusToAdd;
			int skillLevelToAdd11 = this.SkillLevelToAdd;
			int attributeLevelToAdd11 = this.AttributeLevelToAdd;
			CharacterCreationOnCondition characterCreationOnCondition10 = new(base.AccomplishmentUrbanOnCondition);
			CharacterCreationOnSelect characterCreationOnSelect11 = new(base.AccomplishmentEscapadeOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects11 = new(base.AccomplishmentEscapadeOnApply);
			TextObject textObject22 = new("{=lD5Ob3R4}Maybe it was a love affair, or maybe you cheated at dice, or maybe you just chose your words poorly when drinking with a dangerous crowd. Anyway, you got into the kind of trouble from which only a quick tongue or quick feet get you out alive.", null);
			characterCreationCategory12.AddCategoryOption(textObject21, list11, endurance2, focusToAdd11, skillLevelToAdd11, attributeLevelToAdd11, characterCreationOnCondition10, characterCreationOnSelect11, characterCreationApplyFinalEffects11, textObject22, new MBList<TraitObject>
			{
				DefaultTraits.Valor
			}, 1, 5, 0, 0, 0);
			CharacterCreationCategory characterCreationCategory13 = characterCreationCategory;
			TextObject textObject23 = new("{=Yqm0Dics}you treated people well.", null);
			MBList<SkillObject> list12 = new();
			list12.Add(DefaultSkills.Charm);
			list12.Add(DefaultSkills.Steward);
			CharacterAttribute social = DefaultCharacterAttributes.Social;
			int focusToAdd12 = this.FocusToAdd;
			int skillLevelToAdd12 = this.SkillLevelToAdd;
			int attributeLevelToAdd12 = this.AttributeLevelToAdd;
			CharacterCreationOnSelect characterCreationOnSelect12 = new(base.AccomplishmentTreaterOnConsequence);
			CharacterCreationApplyFinalEffects characterCreationApplyFinalEffects12 = new(base.AccomplishmentTreaterOnApply);
			TextObject textObject24 = new("{=dDmcqTzb}Yours wasn't the kind of reputation that local legends are made of, but it was the kind that wins you respect among those around you. You were consistently fair and honest in your business dealings and helpful to those in trouble. In doing so, you got a sense of what made people tick.", null);
			characterCreationCategory13.AddCategoryOption(textObject23, list12, social, focusToAdd12, skillLevelToAdd12, attributeLevelToAdd12, null, characterCreationOnSelect12, characterCreationApplyFinalEffects12, textObject24, new MBList<TraitObject>
			{
				DefaultTraits.Mercy,
				DefaultTraits.Generosity,
				DefaultTraits.Honor
			}, 1, 5, 0, 0, 0);
			characterCreation.AddNewMenu(characterCreationMenu);
		}

		public void AddCultureStartMenu(CharacterCreation characterCreation)
		{

            CharacterCreationMenu characterCreationMenu = new(new("{=CulturedStart07}Start Options", null), new("{=CulturedStart08}Who are you in Aeurth...", null), new CharacterCreationOnInit(this.StartOnInit), CharacterCreationMenu.MenuTypes.MultipleChoice);
			CharacterCreationCategory characterCreationCategory = characterCreationMenu.AddMenuCategory(null);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart09}A commoner (Default Start)", null), new(), null, 0, 0, 0, null, new(this.DefaultStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart10}Setting off with your Father, Mother, Brother and your two younger siblings to a new town you'd heard was safer. But you did not make it.", null), null, 0, 0, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart11}A budding caravanner", null), new MBList<SkillObject>
			{
				DefaultSkills.Trade
			}, null, 1, 25, 0, null, new(this.MerchantStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart12}With what savings you could muster you purchased some mules and mercenaries." + $"\n{startingSkillMult[StartType.Merchant]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 0, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart13}A noble of {CULTURE} in exile", null), new MBList<SkillObject>
			{
				DefaultSkills.Leadership
			}, null, 1, 50, 0, null, new(this.ExiledStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart14}Forced into exile after your parents were executed for suspected treason. With only your family's bodyguard you set off. Should you return you'd be viewed as a criminal." + $"\n{startingSkillMult[StartType.Exiled]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 150, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart15}In a failing mercenary company", null), new MBList<SkillObject>
			{
				DefaultSkills.Tactics
			}, null, 1, 50, 0, null, new(this.MercenaryStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart16}With men deserting over lack of wages, your company leader was found dead, and you decided to take your chance and lead." + $"\n{startingSkillMult[StartType.Mercenary]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 50, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart17}A cheap outlaw", null), new MBList<SkillObject>
			{
				DefaultSkills.Roguery
			}, null, 1, 25, 0, null, new(this.LooterStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart18}Left impoverished from war, you found a group of like-minded ruffians who were desperate to get by." + $"\n{startingSkillMult[StartType.Looter]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 0, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart19}A new vassal of {CULTURE}", null), new MBList<SkillObject>
			{
				DefaultSkills.Steward
			}, null, 1, 50, 0, null, new(this.VassalStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart20}A young noble who came into an arrangement with the king for a chance at land." + $"\n{startingSkillMult[StartType.VassalNoFief]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 150, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart21}Leading part of {CULTURE}", null), new MBList<SkillObject>
			{
				DefaultSkills.Leadership,
				DefaultSkills.Steward
			}, DefaultCharacterAttributes.Social, 1, 50, 1, null, new(this.KingdomStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart22}With the support of companions you have gathered an army. With limited funds and food you decided it's time for action." + $"\n{startingSkillMult[StartType.KingdomRuler]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 900, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart23}You acquired a castle", null), new MBList<SkillObject>
			{
				DefaultSkills.Leadership,
				DefaultSkills.Steward
			}, DefaultCharacterAttributes.Social, 1, 25, 1, null, new(this.HoldingStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart24}You acquired a castle through your own means and declared yourself a kingdom for better or worse." + $"\n{startingSkillMult[StartType.CastleRuler]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 900, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart25}A landed vassal of {CULTURE}", null), new MBList<SkillObject>
			{
				DefaultSkills.Steward
			}, null, 1, 50, 0, null, new(this.LandedVassalStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart26}A young noble who came into an arrangement with the king for land." + $"\n{startingSkillMult[StartType.VassalFief]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 150, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart27}An escaped prisoner of a lord of {CULTURE}", null), new MBList<SkillObject>
			{
				DefaultSkills.Roguery
			}, null, 1, 10, 0, null, new(this.EscapedStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart28}A poor prisoner of petty crimes who managed to break their shackles with a rock and fled." + $"\n{startingSkillMult[StartType.EscapedPrisoner]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 0, 0, 0, 0);
			characterCreation.AddNewMenu(characterCreationMenu);
		}

		public void AddCultureLocationMenu(CharacterCreation characterCreation)
		{
			CharacterCreationMenu characterCreationMenu = new(new("{=CulturedStart29}Location Options", null), new("{=CulturedStart30}Beginning your new adventure...", null), null, CharacterCreationMenu.MenuTypes.MultipleChoice);
			CharacterCreationCategory characterCreationCategory = characterCreationMenu.AddMenuCategory(null);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart31}Near your home in the city where your journey began", null), new(), null, 0, 0, 0, null, new(this.HometownLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart32}Back to where you started", null), null, 0, 0, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart33}In a strange new city (Random)", null), new(), null, 0, 0, 0, null, new(this.RandomLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart34}Travelling far and wide you arrive at an unknown city", null), null, 0, 0, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart35}In a caravan to the Athas city of Drakar", null), new(), null, 0, 0, 0, null, new(this.QasariLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart36}You leave the caravan right at the gates", null), null, 0, 0, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart37}In a caravan to the Elvean city of Cormanthor", null), new(), null, 0, 0, 0, null, new(this.DunglanysLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart36}You leave the caravan right at the gates", null), null, 0, 0, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart38}On a ship to the Realm city of Zehentil", null), new(), null, 0, 0, 0, null, new(this.ZeonicaLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart39}You leave the ship and arrive right at the gates", null), null, 0, 0, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart40}In a caravan to the Dread city of Nippura", null), new(), null, 0, 0, 0, null, new(this.BalgardLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart36}You leave the caravan right at the gates", null), null, 0, 0, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart41}In a caravan to the All Khuur city of Ortongard", null), new(), null, 0, 0, 0, null, new(this.OrtongardLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart36}You leave the caravan right at the gates", null), null, 0, 0, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart42}On a river boat to the Nasoria city of Valendia", null), new(), null, 0, 0, 0, null, new(this.PravendLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart43}You leave the boat and arrive right at the gates", null), null, 0, 0, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart44}At your castle", null), new(), null, 0, 0, 0, new(this.CastleLocationOnCondition), new(this.CastleLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart45}At your newly acquired castle", null), null, 0, 0, 0, 0, 0);
			characterCreationCategory.AddCategoryOption(new("{=CulturedStart46}Escaping from your captor", null), new(), null, 0, 0, 0, new(this.EscapingLocationOnCondition), new(this.EscapingLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart47}Having just escaped", null), null, 0, 0, 0, 0, 0);
			characterCreation.AddNewMenu(characterCreationMenu);
		}

		protected void StartOnInit(CharacterCreation characterCreation)
		{
			MBTextManager.SetTextVariable("CULTURE", CharacterCreationContentBase.Instance.GetSelectedCulture().Name, false);
		}

		protected void DefaultQuestOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetQuestOption(0);
		}

		protected void SkipQuestOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetQuestOption(1);
		}

		protected void DefaultStartOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetStoryOption(0);
		}

		protected void MerchantStartOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetStoryOption(1);
		}

		protected void ExiledStartOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetStoryOption(2);
		}

		protected void MercenaryStartOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetStoryOption(3);
		}

		protected void LooterStartOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetStoryOption(4);
		}

		protected void VassalStartOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetStoryOption(5);
		}

		protected void KingdomStartOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetStoryOption(6);
		}

		protected void HoldingStartOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetStoryOption(7);
		}

		protected void LandedVassalStartOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetStoryOption(8);
		}

		protected void EscapedStartOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetStoryOption(9);
		}

		protected void HometownLocationOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetLocationOption(0);
		}

		protected void RandomLocationOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetLocationOption(1);
		}

		protected void QasariLocationOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetLocationOption(2);
		}

		protected void DunglanysLocationOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetLocationOption(3);
		}

		protected void ZeonicaLocationOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetLocationOption(4);
		}

		protected void BalgardLocationOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetLocationOption(5);
		}

		protected void OrtongardLocationOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetLocationOption(6);
		}

		protected void PravendLocationOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetLocationOption(7);
		}

		protected void CastleLocationOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetLocationOption(8);
		}

		protected void EscapingLocationOnConsequence(CharacterCreation characterCreation)
		{
			this.Manager.SetLocationOption(9);
		}

		protected void DoNothingOnApply(CharacterCreation characterCreation)
		{
		}

		protected bool CastleLocationOnCondition()
		{
			return this.Manager.StoryOption == 7 || this.Manager.StoryOption == 8;
		}

		protected bool EscapingLocationOnCondition()
		{
			return this.Manager.StoryOption == 9;
		}

		protected new readonly Dictionary<string, Vec2> _startingPoints = new()
        {
			{
				"aserai",
				new Vec2(642.16f, 240.77f)
			},
			{
				"battania",
				new Vec2(446.58f, 460.19f)
			},
			{
				"empire",
				new Vec2(296.03f, 646.27f)
			},
			{
				"khuzait",
				new Vec2(43.49f, 733.3f)
			},
			{
				"sturgia",
				new Vec2(697.85f, 721.67f)
			},
			{
				"vlandia",
				new Vec2(667.2f, 442.22f)
			}
		};

		private const string AthasBodyPropString = "<BodyProperties version=\"4\" age=\"22.23\" weight=\"0.0448\" build=\"0.6065\"  key=\"003FB40FCE001016AF9E6DFC6B0756871FF2FD9D8031BB1327CCC0244CAB9C060069160306EC96D8000000000000000000000000000000000000000010CC1004\"  />";
		private const string NasoriaBodyPropString = "<BodyProperties version=\"4\" age=\"40\" weight=\"0.8288\" build=\"0.4213\"  key=\"001EAC0B80000004FFC53FE76E83CCEA36A3EC6D8174DF4070129ADF3E13E54B0366C6350684B8A7000000000000000000000000000000000000000026CC7002\"  />";
		private const string AllKhuurBodyPropString = "<BodyProperties version=\"4\" age=\"22.49\" weight=\"0.9599\" build=\"0.3611\"  key=\"001EF80D8000200AB8708BB6CDC85229D3698B3ABDFE344CD22D3DD5388988680355E6350596723B0000000000000000000000000000000000000000609C1005\"  />";
		private const string ElveanBodyPropString = "<BodyProperties version=\"4\" age=\"22.49\" weight=\"0.0262\" build=\"0.5108\"  key=\"00000400000000038788080F07757777F0F887F8F88008888E068A89808D80060078060307883F10000000000000000000000000000000000000000052F47145\"  />";
		private const string HumanBodyPropString = "<BodyProperties version=\"4\" age=\"22.35\" weight=\"0.5417\" build=\"0.5231\"  key=\"000DF00FC00033CD8771188F38770F8801F188778888888888888888546AF0F90088860308888888000000000000000000000000000000000000000043044144\"  />";
		private const string UndeadBodyPropString = "<BodyProperties version=\"4\" age=\"40\" weight=\"0.2978\" build=\"0.9522\"  key=\"000004001900178D18E0788057F760886F8707E84EA8E18174414A490D1100E803BE46350BA7B7A50000000000000000000000000000000000000000016430C6\"  />";
	}
}

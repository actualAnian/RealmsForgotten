using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten
{
    public static class Globals
    {
        public static Assembly realmsForgottenAssembly = Assembly.GetExecutingAssembly();

        public static ICustomSettingsProvider Settings { get { return RFSettings.Instance; } }
        public enum StartType
        {
            Other = -1,
            Default,
            Merchant,
            Exiled,
            Mercenary,
            Looter,
            VassalNoFief,
            KingdomRuler,
            CastleRuler,
            VassalFief,
            EscapedPrisoner
        }

        public static Dictionary<StartType, double> startingSkillMult = new()
        {
            [StartType.Default] = 1,
            [StartType.Merchant] = 1,
            [StartType.Exiled] = 2,
            [StartType.Mercenary] = 1.5,
            [StartType.Looter] = 1,
            [StartType.VassalNoFief] = 2,
            [StartType.KingdomRuler] = 3.5,
            [StartType.CastleRuler] = 3,
            [StartType.VassalFief] = 2.5,
            [StartType.EscapedPrisoner] = 1,
        };

        public static int GiantsRaceId {get; private set;}

        public static void SetGiantRaceId()
        {
            try
            {
                CharacterObject giant = TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<CharacterObject>("cs_troll_raiders_raider");
                GiantsRaceId = giant.Race;
            }
            catch
            {
                GiantsRaceId = -1;
                string text = "error trying to set the race id of half giants in RealmsForgotten.Globals.SetGiantRaceId";
                InformationManager.DisplayMessage(new InformationMessage(text, new Color(1, 0, 0)));
            }
        }
    }
}

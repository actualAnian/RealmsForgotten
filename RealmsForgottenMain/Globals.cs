using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;


namespace RealmsForgotten
{
    public static class Globals
    {
        public static Assembly realmsForgottenAssembly = Assembly.GetExecutingAssembly();

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
            [StartType.EscapedPrisoner] = 1
        };
    }
}

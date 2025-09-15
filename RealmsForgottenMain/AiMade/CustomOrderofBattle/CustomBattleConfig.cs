using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealmsForgotten.AiMade.CustomOrderofBattle.Config
{
    public class OOBConfigBlock
    {
        public List<OOBFormationConfig> formations { get; set; } = new List<OOBFormationConfig>();
    }

    // Config raiz do JSON
    public class AutoOOBRootConfig
    {
        public OOBConfigBlock defaults { get; set; } = new OOBConfigBlock();
        public Dictionary<string, OOBConfigBlock> cultures { get; set; } = new Dictionary<string, OOBConfigBlock>();
        public Dictionary<string, OOBConfigBlock> factions { get; set; } = new Dictionary<string, OOBConfigBlock>();
    }

    // Config por formação (índice do OOB 0..7)
    public class OOBFormationConfig
    {
        public int index { get; set; } = 0;                   // Slot da formação
        public string @class { get; set; } = "Unset";         // Infantry/Ranged/Cavalry/HorseArcher/Unset
        public int primaryWeight { get; set; } = 100;         // Peso primário
        public int secondaryWeight { get; set; } = 0;         // Peso secundário (se quiser combinar)
        public List<string> filters { get; set; } = new List<string>(); // Heavy/Spear/Shield/HighTier/LowTier/Thrown
        public string commanderStringId { get; set; }         // opcional: comandante fixo
        public List<string> heroTroopStringIds { get; set; }  // opcional: heróis fixos na formação
        public bool enabled { get; set; } = true;             // pode desligar a formação
    }
}

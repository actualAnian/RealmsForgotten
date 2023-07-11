using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace HuntableHerds
{
    public class Settings
    {
        private static Settings? _instance;
        public static Settings Instance
        {
            get
            {
                _instance ??= new Settings();
                return _instance;
            }
        }
        public float DailyChanceOfSpottingHerd { get;} = 0.3f;
        public bool CrouchNeededEnabled { get;} = true;
    }
}

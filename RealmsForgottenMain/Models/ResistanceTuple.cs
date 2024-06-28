using SandBox.GameComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealmsForgotten.Models
{
    public class ResistanceTuple
    {
        public string ResistedDamageType { get; set; }
        public float ReductionPercent { get; set; }

        public ResistanceTuple(string resistedDamageType, float reductionPercent)
        {
            ResistedDamageType = resistedDamageType;
            ReductionPercent = reductionPercent;
        }
    }
}

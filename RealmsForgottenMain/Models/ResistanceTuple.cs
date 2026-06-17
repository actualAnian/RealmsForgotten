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

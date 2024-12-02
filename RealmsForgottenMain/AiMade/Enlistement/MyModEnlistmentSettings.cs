
namespace RealmsForgotten.Behaviors
{
    public class MyModEnlistmentSettings
    {
        public float GlobalXPBonus { get; set; } = 1.0f;
        public int RenownPerBattle { get; set; } = 5;
        public int RelationPerXBattles { get; set; } = 3;
        public int PartyRoleChangeCooldown { get; set; } = 24; // hours
        public int EnlistWithEnemiesCooldown { get; set; } = 72; // hours
        public int RelationshipPenaltiesForLeaving { get; set; } = -10;
        public int RelationshipRequirements { get; set; } = 10;
        public string FormationMarker { get; set; } = "default";
        public float MarkerHeight { get; set; } = 1.0f;
        public string MarkerColor { get; set; } = "red";
    }
}

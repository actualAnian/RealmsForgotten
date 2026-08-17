namespace RF_LivingWorld
{
    public sealed class LivingWorldPartyDefinition
    {
        public string Id = string.Empty;
        public LivingWorldPartyType Type;
        public LivingWorldHerdVariant HerdVariant;
        public int MaxInstances = 1;
        public int MinimumSize = 3;
        public int MaximumSize = 6;
        public float RumorReliability = 0.65f;
        public bool IsAmbient;
        public int AmbientBaseline;
    }
}

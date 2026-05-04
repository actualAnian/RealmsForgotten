using TaleWorlds.SaveSystem;

namespace RF_AIDialog
{
    /// <summary>
    /// Registers RF_AIDialog types with the Bannerlord save system.
    /// The save system auto-discovers SaveableTypeDefiner subclasses by scanning
    /// all loaded assemblies — no SubModule registration needed.
    ///
    /// Base ID 456789012 — no conflict with any existing definer in the project.
    /// </summary>
    public class RF_AIDialogSaveDefiner : SaveableTypeDefiner
    {
        public RF_AIDialogSaveDefiner() : base(456789012)
        {
            RFAIDebug.Log("RF_AIDialogSaveDefiner: constructed");
        }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(AIDialogQuest), 1);
        }
    }
}

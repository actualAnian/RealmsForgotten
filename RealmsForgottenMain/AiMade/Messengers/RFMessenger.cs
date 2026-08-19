using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.Messengers
{
    /// <summary>
    /// Um mensageiro em viagem. Portado do sistema de Messengers do LOTRAOM (código MIT).
    /// Registrado no SaveDefiner do RealmsForgotten.
    /// </summary>
    public class RFMessenger
    {
        [SaveableProperty(1)]
        public CampaignTime DispatchTime { get; private set; }

        [SaveableProperty(2)]
        public Hero TargetHero { get; private set; }

        [SaveableProperty(3)]
        public Vec2 CurrentPosition { get; set; }

        [SaveableProperty(4)]
        public bool Arrived { get; set; }

        public RFMessenger(Hero targetHero, CampaignTime dispatchTime)
        {
            TargetHero = targetHero;
            DispatchTime = dispatchTime;
            CurrentPosition = Hero.MainHero.GetMapPoint().Position.ToVec2();
            Arrived = false;
        }

        public RFMessenger()
        {
        }
    }
}

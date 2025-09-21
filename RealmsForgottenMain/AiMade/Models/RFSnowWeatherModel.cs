// @TODO probably obsolete, with new taleworlds changes

//using TaleWorlds.CampaignSystem;
//using TaleWorlds.CampaignSystem.GameComponents;
//using TaleWorlds.CampaignSystem.Party;
//using TaleWorlds.Library;

//namespace RealmsForgotten.AiMade.Models
//{
//    public class RFSnowWeatherModel : DefaultMapWeatherModel
//    {
//        // ---------------------------------------------------- editable line
//        private const float SnowLineY = 1040f;   // everything Y ≥ this ⇒ winter

//        // 32 × 32 grid cache (same size vanilla uses)
//        private readonly WeatherEvent[] _cache = new WeatherEvent[1024];

//        /* ───────────────────────────────────────────────────────────── */
//        /* Helpers                                                      */
//        /* ───────────────────────────────────────────────────────────── */

//        private static bool InSnowBelt(Vec2 p) => p.y >= SnowLineY;

//        private void GetNodeCoord(Vec2 pos, out int xi, out int yi)
//        {
//            Vec2 size = Campaign.Current.MapSceneWrapper.GetTerrainSize();
//            float cellX = size.X / DefaultWeatherNodeDimension;
//            float cellY = size.Y / DefaultWeatherNodeDimension;
//            xi = (int)(pos.x / cellX);
//            yi = (int)(pos.y / cellY);
//        }

//        private void SetCache(Vec2 pos, WeatherEvent e)
//        {
//            GetNodeCoord(pos, out int xi, out int yi);
//            _cache[yi * DefaultWeatherNodeDimension + xi] = e;
//        }

//        private WeatherEvent GetCache(Vec2 pos)
//        {
//            GetNodeCoord(pos, out int xi, out int yi);
//            return _cache[yi * DefaultWeatherNodeDimension + xi];
//        }

//        /* ───────────────────────────────────────────────────────────── */
//        /* REQUIRED OVERRIDES                                           */
//        /* ───────────────────────────────────────────────────────────── */

//        // 1. hourly writer – engine calls this for every node once per hr
//        public override WeatherEvent UpdateWeatherForPosition(Vec2 pos, CampaignTime ct)
//        {
//            WeatherEvent e = InSnowBelt(pos) ? WeatherEvent.Blizzard
//                                             : base.UpdateWeatherForPosition(pos, ct);
//            SetCache(pos, e);
//            return e;
//        }

//        // 2. fast accessor – most systems use the cached value
//        public override WeatherEvent GetWeatherEventInPosition(Vec2 pos)
//            => InSnowBelt(pos) ? WeatherEvent.Blizzard
//                               : GetCache(pos);

//        // 3. season factor – force winter scenes north of the line
//        public override void GetSeasonTimeFactorOfCampaignTime(
//            CampaignTime t,
//            out float snowF,
//            out float rainF,
//            bool snap = false)
//        {
//            base.GetSeasonTimeFactorOfCampaignTime(t, out snowF, out rainF, snap);

//            if (MobileParty.MainParty != null && InSnowBelt(MobileParty.MainParty.Position2D))
//            {
//                snowF = 1f;
//                rainF = 0f;
//            }
//        }

//        /* ───────────────────────────────────────────────────────────── */
//        /* OPTIONAL ─ Atmosphere preset (ADOD feature)                  */
//        /* ───────────────────────────────────────────────────────────── */

//        public override AtmosphereInfo GetAtmosphereModel(Vec3 pos)
//        {
//            AtmosphereInfo info = base.GetAtmosphereModel(pos);
//            if (InSnowBelt(pos.AsVec2))
//            {
//                info.TimeInfo.Season = (int)CampaignTime.Seasons.Winter;
//                info.InterpolatedAtmosphereName = "dense_snowy";
//            }
//            return info;
//        }
//    }
//}
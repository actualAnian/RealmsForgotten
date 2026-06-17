using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace RealmsForgotten.UI.FloatingText
{
    public class FloatingTextListPanel : ListPanel
    {
        Vec2 _position;
        public FloatingTextListPanel(UIContext context) : base(context) { }
        protected override void OnLateUpdate(float dt)
        {
            ScaledPositionYOffset = Position.y - Size.Y / 2f;
            ScaledPositionXOffset = Position.x - Size.X / 2f;
        }
        [DataSourceProperty]
        public Vec2 Position
        {
            get
            {
                return _position;
            }
            set
            {
                if (_position != value)
                {
                    _position = value;
                    OnPropertyChanged(value, "Position");
                }
            }
        }
        //[DataSourceProperty]
        //public int Distance
        //{
        //    get
        //    {
        //        return this._distance;
        //    }
        //    set
        //    {
        //        if (this._distance != value)
        //        {
        //            this._distance = value;
        //            base.OnPropertyChanged(value, "Distance");
        //        }
        //    }
        //}
    }
}

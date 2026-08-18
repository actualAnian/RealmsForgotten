using TaleWorlds.Library;

namespace RF_IsoCam
{
    /// <summary>
    /// Data source for the facing-arrow HUD (GUI/Prefabs/RFIsoCamFacingArrow.xml).
    /// The arrow sits at screen center (where the camera keeps the player) and rotates to
    /// show where the mouse-look is aiming. Rotation is in DEGREES: Widget.Rotation feeds
    /// Rectangle2D.CreateMatrixFrame, which multiplies by PI/180 (verified in 1.4.8).
    /// </summary>
    public sealed class FacingArrowVM : ViewModel
    {
        private bool _isArrowVisible;
        private float _arrowRotation;

        [DataSourceProperty]
        public bool IsArrowVisible
        {
            get => _isArrowVisible;
            set
            {
                if (value != _isArrowVisible)
                {
                    _isArrowVisible = value;
                    OnPropertyChangedWithValue(value, "IsArrowVisible");
                }
            }
        }

        [DataSourceProperty]
        public float ArrowRotation
        {
            get => _arrowRotation;
            set
            {
                if (value != _arrowRotation)
                {
                    _arrowRotation = value;
                    OnPropertyChangedWithValue(value, "ArrowRotation");
                }
            }
        }
    }
}

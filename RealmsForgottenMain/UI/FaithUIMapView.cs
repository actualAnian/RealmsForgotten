using System;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace RealmsForgotten.UI;
internal class LTUIManager
{
    private static LTUIManager? _instance;
    private FaithUIMapView? _mapView;

    private string _menuOnClose = "";

    public static LTUIManager Instance
    {
        get
        {
            if (_instance == null) _instance = new LTUIManager();
            return _instance;
        }
        set
        {
            _instance = value;
        }
    }

    public void ShowWindow()
    {
        if (_mapView != null) _mapView.Close();
        _mapView = new FaithUIMapView();
        _mapView.Refresh();
    }

    public void CloseUI()
    {
        if (_mapView != null)
        {
            _mapView.Close();
            _mapView = null;
        }
        if (_menuOnClose != "") GameMenu.SwitchToMenu(_menuOnClose);
    }

    public void Refresh()
    {
        _mapView?.Refresh();
    }

}
public class FaithUIMapView : MapView
{

        private SpriteCategory? _categoryDeveloper;
        private SpriteCategory? _categoryEncyclopedia;

        private new GauntletLayer? Layer { get; set; }
        private FaithUIVM? VM { get; set; }

        private GauntletMovie? _gauntletMovie;

        public FaithUIMapView()
        {
            CreateLayout();
        }


        protected override void CreateLayout()
        {
            base.CreateLayout();
            Layer = new GauntletLayer(-1);
            VM = new FaithUIVM(40, () =>
            {
                Layer.InputRestrictions.ResetInputRestrictions();
                Layer.IsFocusLayer = false;
                ScreenManager.TopScreen.RemoveLayer(Layer);
                ScreenManager.TryLoseFocus(Layer);
                VM = null;
            });
            Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("PartyHotKeyCategory"));
            Layer.LoadMovie("FaithUI", VM);
            Layer.IsFocusLayer = true;
            
            Layer.InputRestrictions.SetInputRestrictions();
            MapScreen.Instance.AddLayer(Layer);
            ScreenManager.TrySetFocus(Layer);
        }

        public void Close()
        {
            if (Layer == null) return;
            Layer.InputRestrictions.ResetInputRestrictions();
            this.Layer.IsFocusLayer = false;
            if (_gauntletMovie != null) this.Layer.ReleaseMovie(_gauntletMovie);
            
            MapScreen.Instance.RemoveLayer(this.Layer);
            
            this.Layer = null;
            _gauntletMovie = null;
            this.VM = null;
        }
        
        public void Refresh()
        {
            this.VM?.RefreshValues();
        }
}

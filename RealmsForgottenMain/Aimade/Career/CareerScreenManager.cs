using RealmsForgotten.UI;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.ScreenSystem;

namespace RealmsForgotten.AiMade.Career;

public static class CareerScreenManager
{
    private static GauntletLayer? _layer;
    private static CareerInfoVM? _dataSource;
    
    public static void OpenCareerInfoScreen()
    {
        _layer = new GauntletLayer(-1);
        _dataSource = new CareerInfoVM(() =>
        {
            ScreenManager.PopScreen();
        });

        _layer.LoadMovie("CareerInfoScreen", _dataSource);
        _layer.InputRestrictions.SetInputRestrictions();
        ScreenManager.TopScreen.AddLayer(_layer);
        ScreenManager.TrySetFocus(_layer);
        _dataSource.RefreshValues();
    }
}
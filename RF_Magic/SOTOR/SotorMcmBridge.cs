using System.ComponentModel;
using MCM.Abstractions.Base.Global;

namespace SOTOR;

internal static class SotorMcmBridge
{
	public static void Initialize()
	{
		SotorMcmSettings instance = GlobalSettings<SotorMcmSettings>.Instance;
		if (instance == null)
		{
			SotorLog.Info("MCM: SOTOR settings instance not available (MCM not installed?) — using SotorSettings defaults.");
			return;
		}
		instance.SyncToStore();
		SotorLog.Info($"MCM: SOTOR settings bound. UseThrownAmberSpear={SotorSettings.UseThrownAmberSpear}.");
		instance.PropertyChanged -= OnChanged;
		instance.PropertyChanged += OnChanged;
	}

	private static void OnChanged(object sender, PropertyChangedEventArgs e)
	{
		if (sender is SotorMcmSettings sotorMcmSettings)
		{
			sotorMcmSettings.SyncToStore();
			SotorLog.Info($"MCM: setting '{e.PropertyName}' changed → UseThrownAmberSpear={SotorSettings.UseThrownAmberSpear}.");
		}
	}
}

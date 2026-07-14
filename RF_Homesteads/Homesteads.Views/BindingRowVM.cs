using TaleWorlds.Library;

namespace Homesteads.Views;

public class BindingRowVM : ViewModel
{
	private readonly string _action;

	private readonly string _keyboard;

	private readonly string _controller;

	[DataSourceProperty]
	public string ActionName => _action;

	[DataSourceProperty]
	public string KeyboardKey => _keyboard;

	[DataSourceProperty]
	public string ControllerButton => _controller;

	public BindingRowVM(string action, string keyboard, string controller)
	{
		_action = action;
		_keyboard = keyboard;
		_controller = controller;
	}
}

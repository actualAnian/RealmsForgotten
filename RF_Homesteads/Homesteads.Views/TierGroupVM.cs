using TaleWorlds.Library;

namespace Homesteads.Views;

public class TierGroupVM : ViewModel
{
	private readonly string _headerText;

	private MBBindingList<BuildingCardVM> _cards = new MBBindingList<BuildingCardVM>();

	[DataSourceProperty]
	public string HeaderText => _headerText;

	[DataSourceProperty]
	public MBBindingList<BuildingCardVM> Cards
	{
		get
		{
			return _cards;
		}
		set
		{
			if (value != _cards)
			{
				_cards = value;
				OnPropertyChangedWithValue(value, "Cards");
			}
		}
	}

	public TierGroupVM(string header)
	{
		_headerText = header;
	}
}

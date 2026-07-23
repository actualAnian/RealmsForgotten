using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace Homesteads.MissionLogics;

public class HomesteadRaceProgressView : MissionView
{
	private sealed class PassiveGauntletLayer : GauntletLayer
	{
		public PassiveGauntletLayer(string name, int localOrder, bool shouldClear = false)
			: base(name, localOrder, shouldClear)
		{
		}

		public override bool HitTest()
		{
			return false;
		}
	}

	private sealed class RaceProgressVM : ViewModel
	{
		private const int BarCells = 12;

		private MBBindingList<RaceBarVM> _bars = new MBBindingList<RaceBarVM>();

		private string _countdownText = "";

		private string _promptText = "";

		private string _boostText = "";

		[DataSourceProperty]
		public string TitleText => "Race";

		[DataSourceProperty]
		public string CountdownText
		{
			get
			{
				return _countdownText;
			}
			set
			{
				if (value != _countdownText)
				{
					_countdownText = value;
					OnPropertyChangedWithValue(value, "CountdownText");
				}
			}
		}

		[DataSourceProperty]
		public string PromptText
		{
			get
			{
				return _promptText;
			}
			set
			{
				if (value != _promptText)
				{
					_promptText = value;
					OnPropertyChangedWithValue(value, "PromptText");
				}
			}
		}

		[DataSourceProperty]
		public string BoostText
		{
			get
			{
				return _boostText;
			}
			set
			{
				if (value != _boostText)
				{
					_boostText = value;
					OnPropertyChangedWithValue(value, "BoostText");
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<RaceBarVM> Bars
		{
			get
			{
				return _bars;
			}
			set
			{
				if (value != _bars)
				{
					_bars = value;
					OnPropertyChangedWithValue(value, "Bars");
				}
			}
		}

		public void Update(HomesteadRaceMissionLogic? logic)
		{
			if (logic != null)
			{
				CountdownText = logic.GetCountdownText();
				PromptText = logic.GetRecoverPrompt();
				BoostText = logic.GetBoostStatus();
				List<HomesteadRaceMissionLogic.RaceProgress> progressSnapshot = logic.GetProgressSnapshot();
				while (_bars.Count < progressSnapshot.Count)
				{
					_bars.Add(new RaceBarVM());
				}
				while (_bars.Count > progressSnapshot.Count)
				{
					_bars.RemoveAt(_bars.Count - 1);
				}
				for (int i = 0; i < progressSnapshot.Count; i++)
				{
					_bars[i].Set(progressSnapshot[i], 12);
				}
			}
		}
	}

	private sealed class RaceBarVM : ViewModel
	{
		private const int TrackWidth = 110;

		private string _labelText = "";

		private Color _barColor = Color.White;

		private Color _spurColor = Color.White;

		private string _spurText = "";

		private string _speedText = "";

		private float _fillWidth;

		[DataSourceProperty]
		public string LabelText
		{
			get
			{
				return _labelText;
			}
			set
			{
				if (value != _labelText)
				{
					_labelText = value;
					OnPropertyChangedWithValue(value, "LabelText");
				}
			}
		}

		[DataSourceProperty]
		public Color SpurColor
		{
			get
			{
				return _spurColor;
			}
			set
			{
				if (value != _spurColor)
				{
					_spurColor = value;
					OnPropertyChangedWithValue(value, "SpurColor");
				}
			}
		}

		[DataSourceProperty]
		public string SpurText
		{
			get
			{
				return _spurText;
			}
			set
			{
				if (value != _spurText)
				{
					_spurText = value;
					OnPropertyChangedWithValue(value, "SpurText");
				}
			}
		}

		[DataSourceProperty]
		public string SpeedText
		{
			get
			{
				return _speedText;
			}
			set
			{
				if (value != _speedText)
				{
					_speedText = value;
					OnPropertyChangedWithValue(value, "SpeedText");
				}
			}
		}

		[DataSourceProperty]
		public Color BarColor
		{
			get
			{
				return _barColor;
			}
			set
			{
				if (value != _barColor)
				{
					_barColor = value;
					OnPropertyChangedWithValue(value, "BarColor");
				}
			}
		}

		[DataSourceProperty]
		public float FillWidth
		{
			get
			{
				return _fillWidth;
			}
			set
			{
				if (value != _fillWidth)
				{
					_fillWidth = value;
					OnPropertyChangedWithValue(value, "FillWidth");
				}
			}
		}

		public void Set(HomesteadRaceMissionLogic.RaceProgress p, int cells)
		{
			float num = p.Fraction;
			if (num < 0f)
			{
				num = 0f;
			}
			if (num > 1f)
			{
				num = 1f;
			}
			FillWidth = num * 110f;
			BarColor = Color.FromUint(p.Color);
			SpurColor = Color.FromUint(p.SpurColor);
			SpurText = p.SpurText;
			SpeedText = p.Speed.ToString("0.0") + " m/s";
			LabelText = ((p.Place > 0) ? Ordinal(p.Place) : ((int)(num * 100f) + "%")) + "  " + p.Name;
		}

		private static string Ordinal(int n)
		{
			int num = n % 100;
			string text = ((num < 11 || num > 13) ? ((n % 10) switch
			{
				1 => "st", 
				2 => "nd", 
				3 => "rd", 
				_ => "th", 
			}) : "th");
			string text2 = text;
			return n + text2;
		}
	}

	private GauntletLayer? _layer;

	private RaceProgressVM? _dataSource;

	private HomesteadRaceMissionLogic? _logic;

	private float _timer;

	private const float UpdateInterval = 0.2f;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_logic = ((MissionBehavior)this).Mission.GetMissionBehavior<HomesteadRaceMissionLogic>();
		_dataSource = new RaceProgressVM();
		_layer = new PassiveGauntletLayer("HomesteadRaceHUD", 1)
		{
			IsFocusLayer = false
		};
		_layer.LoadMovie("HomesteadRaceHUD", _dataSource);
		((ScreenBase)(object)((MissionView)this).MissionScreen).AddLayer((ScreenLayer)_layer);
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		_timer += dt;
		if (!(_timer < 0.2f))
		{
			_timer = 0f;
			_dataSource?.Update(_logic);
		}
	}

	public override void OnMissionScreenFinalize()
	{
		if (_layer != null)
		{
			((ScreenBase)(object)((MissionView)this).MissionScreen).RemoveLayer((ScreenLayer)_layer);
		}
		_dataSource = null;
		_layer = null;
		base.OnMissionScreenFinalize();
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace Homesteads.MissionLogics;

public class HomesteadSparringOverlayView : MissionView
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

	private sealed class SparringVM : ViewModel
	{
		private string _playerWhiteText = "";

		private string _playerYellowText = "";

		private string _playerOrangeText = "";

		private string _playerKOText = "";

		private string _enemyWhiteText = "";

		private string _enemyYellowText = "";

		private string _enemyOrangeText = "";

		private string _enemyKOText = "";

		[DataSourceProperty]
		public string TitleText => "Sparring Match";

		[DataSourceProperty]
		public string CurrentMatchLabel => "— Current Match —";

		[DataSourceProperty]
		public string PlayerTeamLabel => "Your Side";

		[DataSourceProperty]
		public string EnemyTeamLabel => "Homestead";

		[DataSourceProperty]
		public string PlayerWhiteText
		{
			get
			{
				return _playerWhiteText;
			}
			set
			{
				if (value != _playerWhiteText)
				{
					_playerWhiteText = value;
					OnPropertyChangedWithValue(value, "PlayerWhiteText");
				}
			}
		}

		[DataSourceProperty]
		public string PlayerYellowText
		{
			get
			{
				return _playerYellowText;
			}
			set
			{
				if (value != _playerYellowText)
				{
					_playerYellowText = value;
					OnPropertyChangedWithValue(value, "PlayerYellowText");
				}
			}
		}

		[DataSourceProperty]
		public string PlayerOrangeText
		{
			get
			{
				return _playerOrangeText;
			}
			set
			{
				if (value != _playerOrangeText)
				{
					_playerOrangeText = value;
					OnPropertyChangedWithValue(value, "PlayerOrangeText");
				}
			}
		}

		[DataSourceProperty]
		public string PlayerKOText
		{
			get
			{
				return _playerKOText;
			}
			set
			{
				if (value != _playerKOText)
				{
					_playerKOText = value;
					OnPropertyChangedWithValue(value, "PlayerKOText");
				}
			}
		}

		[DataSourceProperty]
		public string EnemyWhiteText
		{
			get
			{
				return _enemyWhiteText;
			}
			set
			{
				if (value != _enemyWhiteText)
				{
					_enemyWhiteText = value;
					OnPropertyChangedWithValue(value, "EnemyWhiteText");
				}
			}
		}

		[DataSourceProperty]
		public string EnemyYellowText
		{
			get
			{
				return _enemyYellowText;
			}
			set
			{
				if (value != _enemyYellowText)
				{
					_enemyYellowText = value;
					OnPropertyChangedWithValue(value, "EnemyYellowText");
				}
			}
		}

		[DataSourceProperty]
		public string EnemyOrangeText
		{
			get
			{
				return _enemyOrangeText;
			}
			set
			{
				if (value != _enemyOrangeText)
				{
					_enemyOrangeText = value;
					OnPropertyChangedWithValue(value, "EnemyOrangeText");
				}
			}
		}

		[DataSourceProperty]
		public string EnemyKOText
		{
			get
			{
				return _enemyKOText;
			}
			set
			{
				if (value != _enemyKOText)
				{
					_enemyKOText = value;
					OnPropertyChangedWithValue(value, "EnemyKOText");
				}
			}
		}

		private static void BuildTeamTexts(IReadOnlyList<Agent> agents, HomesteadSparringMissionLogic logic, out string whiteText, out string yellowText, out string orangeText, out string koText)
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			StringBuilder stringBuilder3 = new StringBuilder();
			StringBuilder stringBuilder4 = new StringBuilder();
			foreach (Agent agent in agents)
			{
				if (agent?.Character == null)
				{
					continue;
				}
				string text = agent.Character.Name.ToString();
				float health = agent.Health;
				float healthLimit = agent.HealthLimit;
				bool num = health <= 0f;
				int damageDealtBy = logic.GetDamageDealtBy(agent);
				int damageTakenBy = logic.GetDamageTakenBy(agent);
				if (num)
				{
					stringBuilder4.AppendLine($"{text}  0%  D:{damageDealtBy} T:{damageTakenBy}");
					continue;
				}
				float num2 = ((healthLimit > 0f) ? (Math.Max(0f, health) / healthLimit * 100f) : 100f);
				int num3 = (int)num2;
				string value = $"{text}  {num3}%  D:{damageDealtBy} T:{damageTakenBy}";
				if (num2 >= 80f)
				{
					stringBuilder.AppendLine(value);
				}
				else if (num2 > 30f)
				{
					stringBuilder2.AppendLine(value);
				}
				else
				{
					stringBuilder3.AppendLine(value);
				}
			}
			whiteText = stringBuilder.ToString().TrimEnd(Array.Empty<char>());
			yellowText = stringBuilder2.ToString().TrimEnd(Array.Empty<char>());
			orangeText = stringBuilder3.ToString().TrimEnd(Array.Empty<char>());
			koText = stringBuilder4.ToString().TrimEnd(Array.Empty<char>());
		}

		public void Update(HomesteadSparringMissionLogic? logic, IReadOnlyList<Agent> playerAgents, IReadOnlyList<Agent> enemyAgents)
		{
			if (logic != null)
			{
				BuildTeamTexts(playerAgents, logic, out string whiteText, out string yellowText, out string orangeText, out string koText);
				BuildTeamTexts(enemyAgents, logic, out string whiteText2, out string yellowText2, out string orangeText2, out string koText2);
				PlayerWhiteText = whiteText;
				PlayerYellowText = yellowText;
				PlayerOrangeText = orangeText;
				PlayerKOText = koText;
				EnemyWhiteText = whiteText2;
				EnemyYellowText = yellowText2;
				EnemyOrangeText = orangeText2;
				EnemyKOText = koText2;
			}
		}
	}

	private GauntletLayer? _layer;

	private SparringVM? _dataSource;

	private HomesteadSparringMissionLogic? _logic;

	private List<Agent> _playerAgents = new List<Agent>();

	private List<Agent> _enemyAgents = new List<Agent>();

	private bool _agentsCached;

	private float _updateTimer;

	private const float UpdateInterval = 0.25f;

	public override void OnMissionScreenInitialize()
	{
		((MissionView)this).OnMissionScreenInitialize();
		_logic = ((MissionBehavior)this).Mission.GetMissionBehavior<HomesteadSparringMissionLogic>();
		_dataSource = new SparringVM();
		_layer = new PassiveGauntletLayer("HomesteadSparringHUD", 1);
		_layer.IsFocusLayer = false;
		_layer.LoadMovie("HomesteadSparringHUD", _dataSource);
		((ScreenBase)(object)((MissionView)this).MissionScreen).AddLayer((ScreenLayer)_layer);
	}

	public override void OnMissionScreenTick(float dt)
	{
		((MissionView)this).OnMissionScreenTick(dt);
		if (!_agentsCached)
		{
			TryCacheAgents();
		}
		_updateTimer += dt;
		if (_updateTimer >= 0.25f)
		{
			_updateTimer = 0f;
			_dataSource?.Update(_logic, _playerAgents, _enemyAgents);
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
		((MissionView)this).OnMissionScreenFinalize();
	}

	private void TryCacheAgents()
	{
		if (((MissionBehavior)this).Mission?.Agents == null || ((MissionBehavior)this).Mission.Agents.Count == 0)
		{
			return;
		}
		List<Agent> list = ((MissionBehavior)this).Mission.Agents.Where(delegate(Agent a)
		{
			if (a.IsHuman)
			{
				Team team = a.Team;
				if (team == null)
				{
					return false;
				}
				return team.Side == BattleSideEnum.Defender;
			}
			return false;
		}).ToList();
		List<Agent> list2 = ((MissionBehavior)this).Mission.Agents.Where(delegate(Agent a)
		{
			if (a.IsHuman)
			{
				Team team = a.Team;
				if (team == null)
				{
					return false;
				}
				return team.Side == BattleSideEnum.Attacker;
			}
			return false;
		}).ToList();
		if (list.Count != 0 || list2.Count != 0)
		{
			_playerAgents = list;
			_enemyAgents = list2;
			_agentsCached = true;
		}
	}
}

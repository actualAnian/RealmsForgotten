//using MCM.Abstractions.Base.Global;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using TaleWorlds.Core;
//using TaleWorlds.InputSystem;
//using TaleWorlds.Localization;
//using TaleWorlds.MountAndBlade;

//namespace Marching
//{
//  public class MarchMissionBehavior : MissionBehavior
//  {
//    private InputKey _marchKey;
//    private OrderController _orderController;
//    public static List<Formation> MarchingFormations;

//    public virtual MissionBehaviorType BehaviorType => (MissionBehaviorType) 1;

//    private void AddMarchingFormation(Formation formation)
//    {
//      if (MarchMissionBehavior.MarchingFormations.Contains(formation))
//        return;
//      MarchMissionBehavior.MarchingFormations.Add(formation);
//    }

//    public MarchMissionBehavior()
//    {
//      MarchMissionBehavior.MarchingFormations = new List<Formation>();
//      this._marchKey = (InputKey) Enum.Parse(typeof (InputKey), GlobalSettings<MarchGlobalConfig>.Instance.MarchingHotKey.SelectedValue);
//    }

//    public virtual void OnMissionTick(float dt)
//    {
//      base.OnMissionTick(dt);
//      if (!Input.IsReleased(this._marchKey) || Mission.Current == null || Agent.Main == null || (double) Agent.Main.Health <= 0.0 || !Agent.Main.Team.IsPlayerGeneral)
//        return;
//      this._orderController = Agent.Main.Team.PlayerOrderController;
//      if (!Mission.Current.IsOrderMenuOpen || !((IEnumerable<Formation>) this._orderController.SelectedFormations).Any<Formation>())
//      {
//        MBInformationManager.AddQuickInformation(new TextObject("{=no_formations_selected}No formations selected to march!", (Dictionary<string, object>) null), 0, (BasicCharacterObject) null, "");
//      }
//      else
//      {
//        if (((List<Formation>) this._orderController.SelectedFormations).Count == 1)
//        {
//          if (MarchMissionBehavior.MarchingFormations.Contains(((List<Formation>) this._orderController.SelectedFormations)[0]))
//          {
//            TextObject textObject = new TextObject("{=dismiss_single_formation}{FORMATION}, dismiss march!", (Dictionary<string, object>) null);
//            textObject.SetTextVariable("FORMATION", ((List<Formation>) this._orderController.SelectedFormations)[0].GetFormationName());
//            MBInformationManager.AddQuickInformation(textObject, 0, (BasicCharacterObject) null, "");
//            MarchMissionBehavior.MarchingFormations.Remove(((List<Formation>) this._orderController.SelectedFormations)[0]);
//          }
//          else
//          {
//            TextObject textObject = new TextObject("{=march_single_formation}{FORMATION}, march!", (Dictionary<string, object>) null);
//            textObject.SetTextVariable("FORMATION", ((List<Formation>) this._orderController.SelectedFormations)[0].GetFormationName());
//            MBInformationManager.AddQuickInformation(textObject, 0, (BasicCharacterObject) null, "");
//            this.AddMarchingFormation(((List<Formation>) this._orderController.SelectedFormations)[0]);
//          }
//        }
//        else if (((List<Formation>) this._orderController.SelectedFormations).Count != ((IEnumerable<Formation>) this.Mission.PlayerTeam.FormationsIncludingEmpty).Count<Formation>((Func<Formation, bool>) (f => this._orderController.IsFormationSelectable(f))))
//        {
//          if (!((IEnumerable<Formation>) this._orderController.SelectedFormations).All<Formation>((Func<Formation, bool>) (f => MarchMissionBehavior.MarchingFormations.Contains(f))))
//          {
//            TextObject textObject = new TextObject("{=march_multiple_formations}{FORMATIONS}, march!", (Dictionary<string, object>) null);
//            textObject.SetTextVariable("FORMATIONS", string.Join(", ", ((IEnumerable<Formation>) this._orderController.SelectedFormations).Select<Formation, string>((Func<Formation, string>) (f => f.GetFormationName()))));
//            MBInformationManager.AddQuickInformation(textObject, 0, (BasicCharacterObject) null, "");
//            foreach (Formation selectedFormation in (List<Formation>) this._orderController.SelectedFormations)
//              this.AddMarchingFormation(selectedFormation);
//          }
//          else
//          {
//            TextObject textObject = new TextObject("{=dismiss_multiple_formations}{FORMATIONS}, dismiss march!", (Dictionary<string, object>) null);
//            textObject.SetTextVariable("FORMATIONS", string.Join(", ", ((IEnumerable<Formation>) this._orderController.SelectedFormations).Select<Formation, string>((Func<Formation, string>) (f => f.GetFormationName()))));
//            MBInformationManager.AddQuickInformation(textObject, 0, (BasicCharacterObject) null, "");
//            foreach (Formation selectedFormation in (List<Formation>) this._orderController.SelectedFormations)
//              MarchMissionBehavior.MarchingFormations.Remove(selectedFormation);
//          }
//        }
//        else if (!MarchMissionBehavior.MarchingFormations.Any<Formation>())
//        {
//          MBInformationManager.AddQuickInformation(new TextObject("{=everyone_march}Everyone! March!", (Dictionary<string, object>) null), 0, (BasicCharacterObject) null, "");
//          MarchMissionBehavior.MarchingFormations.Clear();
//          foreach (Formation selectedFormation in (List<Formation>) this._orderController.SelectedFormations)
//            this.AddMarchingFormation(selectedFormation);
//        }
//        else
//        {
//          MBInformationManager.AddQuickInformation(new TextObject("{everyone_dismiss}Everyone! Dismiss march!", (Dictionary<string, object>) null), 0, (BasicCharacterObject) null, "");
//          MarchMissionBehavior.MarchingFormations.Clear();
//        }
//        this.OnMarch();
//      }
//    }

//    public virtual void OnAgentHit(
//      Agent affectedAgent,
//      Agent affectorAgent,
//      in MissionWeapon affectorWeapon,
//      in Blow blow,
//      in AttackCollisionData attackCollisionData)
//    {
//      base.OnAgentHit(affectedAgent, affectorAgent, ref affectorWeapon, ref blow, ref attackCollisionData);
//      if (!affectedAgent.IsMainAgent || (double) affectedAgent.Health > 0.0)
//        return;
//      MarchMissionBehavior.MarchingFormations.Clear();
//      this.OnMarch();
//    }

//    private void OnMarch()
//    {
//      foreach (Agent activeAgent in (List<Agent>) this.Mission.Teams.Player.ActiveAgents)
//      {
//        activeAgent.UpdateAgentProperties();
//        if (activeAgent.HasMount)
//          activeAgent.MountAgent.UpdateAgentProperties();
//      }
//    }
//  }
//}

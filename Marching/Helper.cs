//using System;
//using System.Linq;
//using TaleWorlds.Core;
//using TaleWorlds.Localization;
//using TaleWorlds.MountAndBlade;

//namespace Marching
//{
//  internal static class Helper
//  {
//    public static TBaseModel GetExistingModel<TBaseModel>(this IGameStarter campaignGameStarter) where TBaseModel : GameModel => (TBaseModel) campaignGameStarter.Models.Last<GameModel>((Func<GameModel, bool>) (model => model.GetType().IsSubclassOf(typeof (TBaseModel))));

//    public static string GetFormationName(this Formation formation) => GameTexts.FindAllTextVariations("str_troop_group_name").ElementAt<TextObject>((int) formation.RepresentativeClass).ToString() + " " + (formation.Index + 1).ToString();
//  }
//}

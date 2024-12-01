// Decompiled with JetBrains decompiler
// Type: Marching.Helper
// Assembly: Marching, Version=0.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: FAB07C52-9EF1-4E87-B983-D3A51612112E
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Marching\bin\Win64_Shipping_Client\Marching.dll

using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;


#nullable enable
namespace Marching
{
  internal static class Helper
  {
    public static TBaseModel GetExistingModel<TBaseModel>(this IGameStarter campaignGameStarter) where TBaseModel : GameModel => (TBaseModel) campaignGameStarter.Models.Last<GameModel>((Func<GameModel, bool>) (model => model.GetType().IsSubclassOf(typeof (TBaseModel))));

    public static string GetFormationName(this Formation formation) => GameTexts.FindAllTextVariations("str_troop_group_name").ElementAt<TextObject>((int) formation.RepresentativeClass).ToString() + " " + (formation.Index + 1).ToString();
  }
}

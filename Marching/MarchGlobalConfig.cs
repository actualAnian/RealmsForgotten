﻿//using MCM.Abstractions.Attributes.v2;
//using MCM.Abstractions.Base.Global;
//using MCM.Common;
//using System;
//using System.Collections.Generic;
//using TaleWorlds.InputSystem;
//using TaleWorlds.Library;

//namespace Marching
//{
//  internal sealed class MarchGlobalConfig : AttributeGlobalSettings<MarchGlobalConfig>
//  {
//    public override string Id => "march_config";

//    public override string DisplayName => "Marching";

//    public override string FolderName => "Marching";

//    public override string FormatType => "json";

//    public MarchGlobalConfig() => this.MarchingHotKey = new Dropdown<string>((IEnumerable<string>) MarchGlobalConfig.GetKeyNames(), Extensions.IndexOf<string>(MarchGlobalConfig.GetKeyNames(), "M"));

//    private static string[] GetKeyNames() => Enum.GetNames(typeof (InputKey));

//    [SettingPropertyDropdown("{=march_order_key}March order key", Order = 0, RequireRestart = true)]
//    public Dropdown<string> MarchingHotKey { get; set; }

//    [SettingPropertyBool("{=artemis_support}Artemis spear animation support", Order = 1, RequireRestart = true)]
//    public bool ArtemisSupport { get; set; } = true;

//    [SettingPropertyFloatingInteger("{=marching_speed}Marching speed", 0.1f, 0.8f, "0.00", Order = 2, RequireRestart = false)]
//    public float MarchingSpeed { get; set; } = 0.25f;
//  }
//}

namespace RealmsForgotten.RFReligions.Helper;

public static class ReligionMapHelper
{
    public static Core.RFReligions GetCultureReligion(string cultureString)
    {
        switch (cultureString)
        {
            case "khuzait":
                return Core.RFReligions.TengralorOrkhai;
            case "vlandia":
                return Core.RFReligions.KharazDrathar;
            case "darshi":
                return Core.RFReligions.VyralethAmara;
            case "empire":
            case "empire_w":
            case "empire_s":
                return Core.RFReligions.AeternaFide;
            case "battania":
                return Core.RFReligions.Faelora;
            case "anorite":
                return Core.RFReligions.Anorites;
            case "aserai":
                return Core.RFReligions.PharunAegis;
            case "aqarun":
                return Core.RFReligions.PharunAegis;
            case "giant":
                return Core.RFReligions.Xochxinti;
            case "wulf":
                return Core.RFReligions.KharazDrathar;
            case "tharnmar":
                return Core.RFReligions.KharazDrathar;
            case "valthorne":
                return Core.RFReligions.Anorites;
            case "katogai":
                return Core.RFReligions.TengralorOrkhai;
            case "dwarf":
                return Core.RFReligions.KharazDrathar;
            case "urkhai":
                return Core.RFReligions.KharazDrathar;
            default:
                return Core.RFReligions.AeternaFide;
        }
    }
}
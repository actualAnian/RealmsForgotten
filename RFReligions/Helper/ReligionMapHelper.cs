namespace RealmsForgotten.RFReligions.Helper;

public static class ReligionMapHelper
{
    public static Core.RFReligions MapCultureToReligion(string cultureString)
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
            case "giant":
                return Core.RFReligions.Xochxinti;
            default:
                return Core.RFReligions.AeternaFide;
        }
    }
}
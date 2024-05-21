using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.RFEffects;

public static class ExceptionHandler
{
    private static Exception lastException;
    public static void HandleMethod(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            
            if(exception != lastException)
                InformationManager.ShowInquiry(new InquiryData(new TextObject("{=rf_error}Error").ToString(), exception.Message, 
                    true, false, new TextObject("{=str_done}Done").ToString(), null, null, null), true);
            lastException = exception;
        }
    }
}
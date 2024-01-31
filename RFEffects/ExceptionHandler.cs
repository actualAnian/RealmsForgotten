using System;
using TaleWorlds.Library;

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
                InformationManager.ShowInquiry(new InquiryData("{=rf_error}Error", exception.Message, 
                    true, false, "{=str_done}Done", null, null, null), true);
            lastException = exception;
        }
    }
}
using System;

namespace SeaIce.ImageServices;

internal static class Common
{
    public static ChooseDate.Date[]? SelectDates(DateTime startDate, DateTime? endDate = null)
    {
        var dialog = new ChooseDate(startDate, endDate ?? DateTime.Now.AddDays(-1));
        if (dialog.ShowDialog() == true)
        {
            return dialog.Dates;
        }

        return null;
    }
}

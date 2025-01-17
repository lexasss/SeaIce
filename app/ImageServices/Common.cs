namespace SeaIce.ImageServices;

internal static class Common
{
    public static ChooseDate.Date[]? SelectDates()
    {
        var dialog = new ChooseDate();
        if (dialog.ShowDialog() == true)
        {
            return dialog.Dates;
        }

        return null;
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SeaIce;

public partial class Calendar : Window, INotifyPropertyChanged
{
    public class Date(int year, int month, int day)
    {
        public int Year { get; init; } = year;
        public int Month { get; init; } = month;
        public int Day { get; init; } = day;

        public override string ToString() => $"{Year} {Month:D2} {Day:D2}";
    }

    public static string[] Monthes => ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    public static int[] DayCount => [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    public Date[] Dates => _dates.ToArray();

    public bool HasDates => _dates.Count > 0;
    public bool CanReturn => _stage == Stage.Month || _stage == Stage.Day;
    public string StageName => _stage.ToString();
    public string Path
    {
        get
        {
            List<string> result = [];

            if (_year > 0)
                result.Add(_year.ToString());
            if (_month > 0)
                result.Add(Monthes[_month - 1]);
            result.Add(""); // to get the closing slash

            return @"\ " + string.Join(@" \ ", result);
        }
    }

    public readonly ObservableCollection<ListViewItem> CalendarItems = [];

    public Calendar(DateTime startDate, DateTime endDate)
    {
        InitializeComponent();
        DataContext = this;

        lsvList.ItemsSource = CalendarItems;

        _startDate = startDate;
        _endDate = endDate;

        for (var year = startDate.Year; year <= endDate.Year; year += 1)
        {
            _years.Add(year);
        }

        SetStage(Stage.Year);
    }

    // Internal

    enum Stage
    {
        Year,
        Month,
        Day,
    }

    readonly DateTime _startDate;
    readonly DateTime _endDate;

    readonly List<int> _years = [];
    readonly List<Date> _dates = [];

    Stage _stage;
    int _year = 0;
    int _month = 0;
    int _day = 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetStage(Stage stage)
    {
        _stage = stage;

        CalendarItems.Clear();

        var firstMonth = _year == _startDate.Year ? _startDate.Month - 1 : 0;
        var lastMonth = _year == _endDate.Year ? _endDate.Month : 12;

        var list = _stage switch
        {
            Stage.Year => _years.Select(year => new ListViewItem() { Content = year.ToString() }),
            Stage.Month => Monthes[firstMonth..lastMonth].Select(month => new ListViewItem() { Content = month }),
            Stage.Day => GetDays().Select(day => new ListViewItem() { Content = day.ToString() }),
            _ => throw new Exception("Invalid stage")
        };

        foreach (var lvi in list)
        {
            CalendarItems.Add(lvi);
        }

        if (_stage <= Stage.Day) _day = 0;
        if (_stage <= Stage.Month) _month = 0;
        if (_stage <= Stage.Year) _year = 0;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StageName)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanReturn)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Path)));
    }

    private List<int> GetDays()
    {
        var firstDay = _year == _startDate.Year && _month == _startDate.Month ? _startDate.Day : 1;
        var lastDay = DayCount[_month - 1];
        if (_year == _endDate.Year && _month == _endDate.Month)
        {
            lastDay = _endDate.Day;
        }
        else
        {
            if (_month == 2 && (_year % 4) == 0 && ((_year % 100) != 0 || (_year % 400) == 0))
                lastDay += 1;
        }
        var days = new List<int>();
        for (var day = firstDay; day <= lastDay; day += 1)
        {
            days.Add(day);
        }

        return days;
    }

    // UI

    private void Calendar_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.OriginalSource.GetType() != typeof(TextBlock) && e.OriginalSource.GetType() != typeof(Border))
        {
            return;
        }
        if (lsvList.SelectedItem == null)
        {
            return;
        }

        string selectedValue = (string)((ListViewItem)lsvList.SelectedItem).Content;

        if (_stage == Stage.Year)
        {
            _year = int.Parse(selectedValue);
            SetStage(Stage.Month);
        }
        else if (_stage == Stage.Month)
        {
            _month = Monthes.TakeWhile(month => month != selectedValue).Count() + 1;
            SetStage(Stage.Day);
        }
        else if (_stage == Stage.Day)
        {
            _day = int.Parse(selectedValue);
            if (_dates.Any(date => date.Year == _year && date.Month == _month && date.Day == _day))
            {
                MessageBox.Show("This date was selected already", Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                _dates.Add(new Date(_year, _month, _day));
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Dates)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasDates)));

            //SetStage(Stage.Year);
        }
    }

    private void Dates_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Delete)
        {
            _dates.Remove((Date)lsvDates.SelectedItem);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Dates)));
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        SetStage(_stage switch {
            Stage.Day => Stage.Month,
            Stage.Month => Stage.Year,
            _ => Stage.Day
        });
    }
}

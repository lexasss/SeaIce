using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SeaIce;

public partial class ChooseDate : Window
{
    public int[]? Date { get; private set; } = null;

    public ChooseDate()
    {
        InitializeComponent();

        var years = new List<int>();
        var todayYear = DateTime.Now.Year;
        for (var year = FIRST_YEAR; year <= todayYear; year += 1)
        {
            years.Add(year);
        }

        lsvList.ItemsSource = years.Select(year => new ListViewItem()
        {
            Content = year.ToString(),
        });
    }

    // Internal

    const int FIRST_YEAR = 1978;
    const int FIRST_MONTH = 10;
    const int FIRST_DAY = 26;

    enum Stage
    {
        Year,
        Month,
        Day,
    }

    Stage _stage = Stage.Year;
    int _year = 0;
    int _month = 0;
    int _day = 0;

    private void lsvList_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (lsvList.SelectedItem == null)
        {
            return;
        }

        string selectedValue = (string)((ListViewItem)lsvList.SelectedItem).Content;

        var todayYear = DateTime.Now.Year;
        var todayMonth = DateTime.Now.Month;
        var todayDay = DateTime.Now.Day;

        if (_stage == Stage.Year)
        {
            _year = int.Parse(selectedValue);
            var firstMonth = _year == FIRST_YEAR ? FIRST_MONTH - 1 : 0;
            var lastMonth = _year == todayYear ? todayMonth : 12;
            lsvList.ItemsSource = ExtensionImageService.Monthes[firstMonth..lastMonth].Select(month => new ListViewItem()
            {
                Content = month,
            });

            _stage = Stage.Month;
        }
        else if (_stage == Stage.Month)
        {
            _month = ExtensionImageService.Monthes.TakeWhile(month => month != selectedValue).Count() + 1;
            var firstDay = _year == FIRST_YEAR && _month == FIRST_MONTH ? FIRST_DAY : 1;
            var lastDay = ExtensionImageService.Days[_month - 1];
            if (_year == todayYear && _month == todayMonth)
            {
                lastDay = todayDay;
            }
            else
            {
                if (_month == 1 && (_year % 4) == 0)
                    lastDay += 1;
            }

            var days = new List<int>();
            for (var day = firstDay; day <= lastDay; day += 1)
            {
                days.Add(day);
            }

            lsvList.ItemsSource = days.Select(day => new ListViewItem()
            {
                Content = day.ToString(),
            });

            _stage = Stage.Day;
        }
        else if (_stage == Stage.Day)
        {
            _day = int.Parse(selectedValue);

            Date = new int[] { _year, _month, _day };
            DialogResult = true;
        }
    }
}

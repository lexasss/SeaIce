using ListView = System.Windows.Controls.ListView;

namespace SeaIce;

internal class Player
{
    public double Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    public Player()
    {
        _timer.Interval = 1000;
        _timer.Elapsed += Timer_Elapsed;
    }

    public void Play(ListView? listView)
    {
        if (listView == null)
        {
            return;
        }

        _listView = listView;
        index = 0;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    // Internal

    readonly System.Timers.Timer _timer = new();

    ListView? _listView;
    int index = -1;

    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_listView != null && index >= 0 && index < _listView.Items.Count)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _listView.SelectedIndex = index;
                if (++index >= _listView.Items.Count)
                {
                    index = 0;
                }
            });
        }
    }
}

using System.ComponentModel;
using System.Windows;

namespace CAPETF.Desktop;

public sealed class TerminalOperationState : INotifyPropertyChanged
{
    private readonly object _gate = new();
    private string _operationName = "";
    private string? _errorMessage;
    private int _current;
    private int? _total;
    private bool _isBusy;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get { lock (_gate) return _isBusy; }
    }

    public string OperationName
    {
        get { lock (_gate) return _operationName; }
    }

    public string? ErrorMessage
    {
        get { lock (_gate) return _errorMessage; }
    }

    public int Current
    {
        get { lock (_gate) return _current; }
    }

    public int? Total
    {
        get { lock (_gate) return _total; }
    }

    public decimal Percent
    {
        get
        {
            lock (_gate)
            {
                return _total is > 0 ? Math.Round(_current * 100m / _total.Value, 2) : 0m;
            }
        }
    }

    public bool IsIndeterminate => IsBusy && Total is not > 0;

    public Visibility ProgressVisibility => IsBusy || !string.IsNullOrWhiteSpace(ErrorMessage)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string Label
    {
        get
        {
            lock (_gate)
            {
                if (!string.IsNullOrWhiteSpace(_errorMessage)) return _errorMessage;
                return _total is > 0 ? $"{_operationName} ({_current}/{_total})" : _operationName;
            }
        }
    }

    public bool TryBegin(string operationName, int? total = null)
    {
        lock (_gate)
        {
            if (_isBusy) return false;
            _operationName = operationName;
            _errorMessage = null;
            _current = 0;
            _total = NormalizeTotal(total);
            _isBusy = true;
        }

        NotifyStateChanged();
        return true;
    }

    public void Report(int current) => Report(OperationName, current, Total);

    public void BeginStage(string operationName, int? total = null)
    {
        lock (_gate)
        {
            if (!_isBusy) return;
            _operationName = operationName;
            _current = 0;
            _total = NormalizeTotal(total);
        }

        NotifyStateChanged();
    }

    public void Report(string operationName, int current, int? total = null)
    {
        lock (_gate)
        {
            if (!_isBusy) return;
            _operationName = operationName;
            _total = NormalizeTotal(total) ?? _total;
            _current = _total is > 0 ? Math.Clamp(current, 0, _total.Value) : Math.Max(0, current);
        }

        NotifyStateChanged();
    }

    public void Complete(string? message = null)
    {
        lock (_gate)
        {
            if (!_isBusy) return;
            if (!string.IsNullOrWhiteSpace(message)) _operationName = message;
            if (_total is > 0) _current = _total.Value;
            _errorMessage = null;
            _isBusy = false;
        }

        NotifyStateChanged();
    }

    public void Fail(string message)
    {
        lock (_gate)
        {
            if (!_isBusy) return;
            _errorMessage = message;
            _isBusy = false;
        }

        NotifyStateChanged();
    }

    private static int? NormalizeTotal(int? total) => total is > 0 ? total : null;

    private void NotifyStateChanged()
    {
        foreach (var name in new[]
        {
            nameof(IsBusy), nameof(OperationName), nameof(ErrorMessage), nameof(Current), nameof(Total),
            nameof(Percent), nameof(IsIndeterminate), nameof(ProgressVisibility), nameof(Label),
        })
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

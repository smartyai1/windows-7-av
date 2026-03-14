using Av.Core;

namespace Av.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void TrackDashboardOpened(ITelemetryCollector telemetry)
    {
        telemetry.TrackEvent("ui.dashboard_opened", new Dictionary<string, object?>
        {
            ["view"] = "virus-and-threat-protection"
        });
    }
}

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace OfficialPlugins.Common.ViewModels;

public interface ICameraZoomViewModel : INotifyPropertyChanged
{
    float CurrentZoomScale { get; }

    public float CurrentZoomPercent
    {
        get;
        set;
    }

    List<int> ZoomPercentages { get; set; }

    public void ZoomIn()
    {
        var zooms = ZoomPercentages.Where(x => x > CurrentZoomPercent);
        if (zooms.Count() == 0) return;
        CurrentZoomPercent = zooms.Last();
    }

    public void ZoomOut()
    {
        var zooms = ZoomPercentages.Where(x => x < CurrentZoomPercent);
        if (zooms.Count() == 0) return;
        CurrentZoomPercent = zooms.First();
    }
}

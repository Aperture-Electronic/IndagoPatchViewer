using System.ComponentModel;
using ReactiveUI;

namespace IndagoPatchViewer.ViewModels;

public class PixelDataMapViewModel : ViewModelBase, INotifyPropertyChanged
{
    private int pixelBitWidth = 8;
    private int patchHeight = 8;
    private int patchWidth = 8;
    
    public int PixelBitWidth
    {
        get => pixelBitWidth;
        set
        {
            this.RaiseAndSetIfChanged(ref pixelBitWidth, value);
            this.RaisePropertyChanged(nameof(TotalBitWidth));
        }
    }
    
    public int PatchHeight
    {
        get => patchHeight;
        set
        {
            this.RaiseAndSetIfChanged(ref patchHeight, value);
            this.RaisePropertyChanged(nameof(TotalBitWidth));
        }
    }
    
    public int PatchWidth
    {
        get => patchWidth;
        set
        {
            this.RaiseAndSetIfChanged(ref patchWidth, value);
            this.RaisePropertyChanged(nameof(TotalBitWidth));
        }
    }

    public int TotalBitWidth => PixelBitWidth * PatchHeight * PatchWidth;
}
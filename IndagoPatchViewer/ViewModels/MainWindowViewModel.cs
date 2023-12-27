using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Indago;
using Indago.Analyzable;
using Indago.DataTypes;
using Indago.Events;
using Indago.Server;
using IndagoPatchViewer.Models;
using ReactiveUI;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace IndagoPatchViewer.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private IndagoServer? IndagoServer { get; set; } = null;

    private ObservableCollection<ScopeNode> scopesTree;

    private ObservableCollection<ScopeNode> ScopesTree
    {
        get => scopesTree;
        set => this.RaiseAndSetIfChanged(ref scopesTree, value);
    }

    private ObservableCollection<Signal> availableSignals;
    
    private ObservableCollection<Signal> AvailableSignals
    {
        get => availableSignals;
        set => this.RaiseAndSetIfChanged(ref availableSignals, value);
    }

    public ScopeNode? SelectedScope { get; set; }
    public Signal? SelectedSignal { get; set; } 
    
    public ICommand ConnectIndagoCommand { get; }
    public ICommand RefreshSignalsCommand { get; }
    
    public int IndagoPort { get; set; } = 33681;

    public PixelDataMapViewModel PixelDataMap { get; } = new();

    public bool IsIndagoConnected { get; private set; } = false;

    private bool isMonitorEnabled = false;

    private string currentTimeString;
    
    private Bitmap currentBitmap;

    public Bitmap CurrentBitmap
    {
        get => currentBitmap;
        set => this.RaiseAndSetIfChanged(ref currentBitmap, value);
    }

    public string CurrentTimeString
    {
        get => currentTimeString;
        set => this.RaiseAndSetIfChanged(ref currentTimeString, value);
    }

    public bool IsMonitorEnabled
    {
        get => isMonitorEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref isMonitorEnabled, value);
            if (IndagoServer is not { } indagoServer) return;
            if (value)
            {
                indagoServer.EventSystem.CurrentDebugLocationChanged += CDLEventHandler;
            }
            else
            {
                indagoServer.EventSystem.CurrentDebugLocationChanged -= CDLEventHandler;
            }
        }
    }
    
    private void CDLEventHandler(object? sender, CurrentDebugLocationChangeEventArgs e)
    {
        if (sender is not IndagoServer indagoServer)
        {
            return;
        }
        
        // Get the current debug location
        var currentTime = indagoServer.CurrentTime; 
        
        Console.WriteLine($"Current debug time has been changed to {currentTime}");

        CurrentTimeString = currentTime.ConvertUnitTo(TimeUnit.Picoseconds).ToString();
        
        // Get the signal
        if (SelectedSignal == null) return;

        var value = SelectedSignal.ValueAtTime(currentTime);
        try
        {
            AnalyzableIntegerValue analyzableValue = value;
            
            // Get the pixel values
            Console.WriteLine($"Pixel value is: {analyzableValue.HexadecimalString.TrimStart('0')}");

            var bitmapData = new int[PixelDataMap.PatchWidth * PixelDataMap.PatchHeight];
            for (var y = 0; y < PixelDataMap.PatchWidth; y++)
            {
                for (var x = 0; x < PixelDataMap.PatchHeight; x++)
                {
                    int pixelOffset = y * PixelDataMap.PatchWidth + x;
                    var grayscale = (int)analyzableValue[(8 * pixelOffset, 8)].Value;
                    int argb = (0xFF << 24) | (grayscale << 16) | (grayscale << 8) | grayscale;
                    bitmapData[pixelOffset] = argb;
                }
            }

            var dpi = new Vector(96, 96);
            var bitmap = new WriteableBitmap(
                new(PixelDataMap.PatchWidth, PixelDataMap.PatchHeight), dpi, Avalonia.Platform.PixelFormat.Rgba8888, AlphaFormat.Unpremul);

            using (var lockedBitmap = bitmap.Lock())
            {
                Marshal.Copy(bitmapData, 0, lockedBitmap.Address, bitmapData.Length);
            }

            CurrentBitmap = bitmap;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
    
    public MainWindowViewModel()
    {
        ConnectIndagoCommand = ReactiveCommand.CreateFromTask(ConnectIndago);
        RefreshSignalsCommand = ReactiveCommand.CreateFromTask(RefreshSignals);
    }
    
    private async Task ConnectIndago()
    {
        Console.WriteLine($"Connecting to Indago with port {IndagoPort}");

        await Task.Run(delegate
        {
            var args = new IndagoArgs(isLaunchNeeded: false, port: IndagoPort);
            var clientPerf = new ClientPerferences();

            IndagoServer?.Dispose();
            IndagoServer = new(args, clientPerf);

            RefreshHirearchy();
        });

        Console.WriteLine("Connected to Indago Server");
        IsIndagoConnected = true;
    }

    private void RefreshHirearchy()
    {
        Console.WriteLine("Refreshing scopes tree");
        
        if (IndagoServer is null)
        {
            Console.WriteLine("Error: Indago server is not connected.");
            return;
        }
        
        // Get all scopes
        var scopes = (from s in IndagoServer.Scopes()
                where s.Depth >= 0
                select s).ToList();

        Console.WriteLine($"There are {scopes.Count} scopes in the server.");
        
        // Sort the scopes by depth
        scopes.Sort((s1, s2) => s1.Depth.CompareTo(s2.Depth));

        // Analyze each depth of scope
        ScopeNode rootScope = new(scopes[0]);
        ObservableCollection<ScopeNode> scopeTree = new();
        scopeTree.Add(rootScope);
        foreach (var scope in scopes)
        {
            if (scope.Depth == -1)
            {
                continue;
            }

            var child = rootScope.GetChildByPath(ScopeNode.RemoveParentPath(scope.Path));
            child.SubScopes.Add(new (scope));
        }

        Console.WriteLine("Scopes tree refreshed");

        ScopesTree = scopeTree;
    }

    private async Task RefreshSignals()
    {
        if (SelectedScope is null)
        {
            Console.WriteLine("Error: No scope is selected.");
            return;
        }
        
        Console.WriteLine($"Refreshing signals in scope {SelectedScope.Name} with {PixelDataMap.TotalBitWidth}-bit width");
        
        // Fetch the signals
        await Task.Run(delegate
        {
            var bitWidth = (uint)PixelDataMap.TotalBitWidth;
            var signals =
                (from s in SelectedScope.Scope.Signals()
                where s.Size == bitWidth
                select s).ToList();
            
            Console.WriteLine($"There are {signals.Count} signals matched the criteria in the scope.");
            
            // Sort the signals by name
            signals.Sort((s1, s2) => string.Compare(s1.Name, s2.Name, StringComparison.Ordinal));
            
            // Create a observed collection for it
            ObservableCollection<Signal> signalCollection = new(signals);

            AvailableSignals = signalCollection;
        });

        Console.WriteLine("Signals refreshed");
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OSMemoryAllocationSimulator.ViewModels
{
    /// <summary>
    /// ViewModel for SimulationConfigView. Only one property for demo.
    /// Inherits from BaseViewModel (ObservableObject) so that changes raise
    /// PropertyChanged events and the UI updates automatically.
    /// </summary>
    public partial class SimulationConfigViewModel : BaseViewModel
    {
        // The [ObservableProperty] attribute generates a backing field,
        // the public property, and the raise of PropertyChanged.
        [ObservableProperty]
        private int memorySize = 4096; // default value matching the XAML placeholder
    }
}

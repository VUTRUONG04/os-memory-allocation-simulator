# MVVM Architecture & Coding Conventions

_Document for **OSMemoryAllocationSimulator** (WPF, .NET 8, CommunityToolkit.Mvvm)_

---

## 1. Overview

The project follows the classic **MVVM** pattern to keep UI, state, and business logic separate.

```
View  → XAML + minimal code‑behind
│
└─► DataContext = ViewModel

ViewModel  → Observable state, Commands, coordination of Services
│
└─► Calls Model / Service / Algorithm (no direct UI references)

Model / Service / Algorithm  → Pure C# objects, testable in isolation
```

### Layer responsibilities

| Layer                     | Primary responsibility                                                                    | Typical contents (current repo)                                                      |
| ------------------------- | ----------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| **View**                  | Layout, visual appearance, static resources.                                              | `Views/Simulation/SimulationConfigView.xaml`, `Views/Evaluation/EvaluationView.xaml` |
| **ViewModel**             | UI state (`INotifyPropertyChanged`), commands (`RelayCommand`), coordination of services. | `ViewModels/BaseViewModel.cs`, `ViewModels/SimulationConfigViewModel.cs`             |
| **Model**                 | Domain objects that describe the simulation (memory blocks, processes, configuration).    | _Not yet implemented_ – suggested names in §5.                                       |
| **Service**               | Use‑case orchestration, e.g., run a simulation run.                                       | _No Services folder yet_ – see §5.                                                   |
| **Algorithm**             | Pure allocation strategies, unit‑testable.                                                | _No Algorithms folder yet_ – see §5.                                                 |
| **Repository** (optional) | Persistence (CSV read/write) when needed.                                                 | _Not required at the moment_                                                         |

> **Note:** The code base currently contains **View**, **ViewModel**, and **Resources**. `Models`, `Services`, and `Algorithms` are planned layers and are introduced in this document as placeholders.

---

## 2. View & Code‑behind Conventions

| Rule                                                            | Rationale                                          |
| --------------------------------------------------------------- | -------------------------------------------------- |
| **XAML only** for layout, visual styling, and static resources. | Keeps UI declarative and testable via visual tree. |

| **Code‑behind** is limited to:

- `InitializeComponent()`
- Hooking framework events that have no MVVM alternative (e.g., `Loaded`, `Unloaded` for resource cleanup)
- Interaction with third‑party controls that expose no bindable API |
  | **Never** place business logic, data manipulation, or algorithm calls in code‑behind. | Guarantees that the logic can be unit‑tested via the ViewModel. |
  | Use **StaticResource** / **DynamicResource** for colors, brushes, dimensions defined in `Resources/*.xaml`. |
  | Name controls in **PascalCase** (`BtnStartSimulation`) only when they need to be referenced in code‑behind; otherwise rely on bindings. |

_Example (SimulationConfigView.xaml):_

```xml
<TextBox Text="{Binding MemorySize, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
<Button x:Name="BtnStart" Command="{Binding StartSimulationCommand}" />
```

No code‑behind logic is required.

---

## 3. ViewModel Conventions

### Base class

All ViewModels inherit from `BaseViewModel` which itself derives from `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`.

```csharp
public abstract class BaseViewModel : ObservableObject {}
```

### Observable properties

Use the `[ObservableProperty]` attribute – the source‑generator creates the backing field and raises `PropertyChanged`.

```csharp
public partial class SimulationConfigViewModel : BaseViewModel
{
    [ObservableProperty]
    private int memorySize = 4096; // default MB
}
```

### Commands

Prefer `[RelayCommand]` for synchronous actions and `AsyncRelayCommand` for async work.

```csharp
[RelayCommand]
private void StartSimulation()
{
    // call service, update UI state
}

[RelayCommand(CanExecute = nameof(CanRun))]
private async Task ExportCsvAsync()
{
    await _simulationService.ExportAsync(...);
}

private bool CanRun()
    => MemorySize > 0 && SelectedWorkload != null;
```

`CanExecute` is automatically re‑evaluated when any property used in the condition raises `PropertyChanged`.

### Collections

Expose mutable UI collections as `ObservableCollection<T>`.

```csharp
public ObservableCollection<ProcessRequest> Workload { get; } = new();
```

### Validation (optional)

Implement `IDataErrorInfo` or use `CommunityToolkit.Mvvm.ComponentModel.ObservableValidator` for simple property‑level validation.

### Naming

- **Properties** – PascalCase (`MemorySize`).
- **Commands** – Verb + optional noun (`StartSimulationCommand` generated automatically from method name `StartSimulation`).
- **Async methods** – end with `Async` (`LoadDataAsync`).

---

## 4. Data‑Binding Conventions

| Scenario                                           | Recommended Binding                                                                            |
| -------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| **One‑way display**                                | `Binding Path=PropertyName` (default `Mode=OneWay`).                                           |
| **Two‑way edit**                                   | `Binding Path=PropertyName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged`.                 |
| **Command**                                        | `Command={Binding StartSimulationCommand}`.                                                    |
| **Control‑specific values** (e.g., `SelectedItem`) | Bind directly to a ViewModel property of matching type.                                        |
| **Visibility**                                     | Use a `bool` property with a `BooleanToVisibilityConverter` or expose a `Visibility` property. |

### Common pitfalls

- Miss‑spelling the property name – results in a binding error in the Output window.
- Forgetting `INotifyPropertyChanged` – UI will not update.
- Binding a collection without `ObservableCollection<T>` – UI does not reflect adds/removes.
- Using the default `UpdateSourceTrigger=LostFocus` on `TextBox`; usually you want `PropertyChanged` for immediate feedback.

---

## 5. Model, Service & Algorithm Conventions

### Suggested domain models (currently **not** present)

| Class              | Purpose                                                                  |
| ------------------ | ------------------------------------------------------------------------ |
| `SimulationConfig` | Holds memory size, selected algorithm, workload list.                    |
| `ProcessRequest`   | Describes a memory allocation request (size, arrival time, duration).    |
| `MemoryBlock`      | Represents a contiguous memory region (start, size, occupied flag).      |
| `SimulationStep`   | Optional – a snapshot of the memory map after each allocation.           |
| `SimulationResult` | Final report: fragmentation, allocation statistics, per‑process outcome. |

### Service layer (to be added under `Services/`)

- **`SimulationService`** – orchestrates a run:
  1. Accepts a `SimulationConfig`.
  2. Instantiates the chosen `IFitAlgorithm` implementation.
  3. Iterates through `ProcessRequest`s, producing a `SimulationResult`.
  4. Returns the result (or raises events for progress).
- All services should depend on **interfaces** and be injected (simple constructor injection is sufficient for this small project). No static singletons.

### Algorithms (to be added under `Algorithms/`)

```csharp
public interface IFitAlgorithm
{
    MemoryBlock? FindFit(IEnumerable<MemoryBlock> freeBlocks, int requestSize);
}

public class FirstFitAlgorithm : IFitAlgorithm { … }
public class BestFitAlgorithm  : IFitAlgorithm { … }
public class WorstFitAlgorithm : IFitAlgorithm { … }
```

These classes contain **no WPF references** and therefore can be unit‑tested with pure data.

### Repository (optional)

Only needed if you persist workloads or results to CSV/JSON. Provide a small wrapper such as `ICsvRepository` with `ReadWorkloadAsync(string path)` and `WriteResultAsync(string path, SimulationResult result)`. Keep it out of the ViewModel – the Service should call the repository.

---

## 6. Naming & Folder Conventions

| Item                     | Convention                                                                                        |
| ------------------------ | ------------------------------------------------------------------------------------------------- |
| **Namespaces**           | `OSMemoryAllocationSimulator.<Layer>.<Feature>` (e.g., `OSMemoryAllocationSimulator.ViewModels`). |
| **Classes / Interfaces** | PascalCase; interfaces prefixed with `I` (`IFitAlgorithm`).                                       |
| **Properties**           | PascalCase; plain data (`int MemorySize`).                                                        |
| **Fields**               | Private instance fields: `_camelCase`.                                                            |
| **Commands**             | Method name in PascalCase; generated command property ends with `Command`.                        |
| **Files**                | One file per public type, file name matches the type name (`SimulationConfigViewModel.cs`).       |
| **Folders**              | Current layout matches the layers:                                                                |

- `Views/`
- `ViewModels/`
- `Models/` (planned)
- `Services/` (planned)
- `Algorithms/` (planned)
- `Resources/`
- `docs/architecture/` – architecture‑related markdown files.

---

## 7. End‑to‑End Feature Workflow (example: _Run Simulation_)

1. **Define acceptance criteria** – user selects memory size, algorithm, workload and clicks **Run**.
2. **Create/Update model** – `SimulationConfig` is built from the bound ViewModel properties.
3. **Select algorithm** – `SimulationService` receives the config and resolves the appropriate `IFitAlgorithm` implementation.
4. **Unit‑test algorithm** – write a test that feeds a list of `MemoryBlock`s and a `ProcessRequest` and verifies the returned block.
5. **Service implementation** – orchestrates the allocation loop, returns a `SimulationResult`.
6. **ViewModel wiring** – `StartSimulationCommand` calls the service, stores the result in an observable property (`SimulationResult`).
7. **Bind UI** – a `DataGrid` or custom chart binds to `SimulationResult` (read‑only). UI updates automatically via `INotifyPropertyChanged`.
8. **Integration test** – instantiate the ViewModel (no UI) and verify that executing the command updates the `SimulationResult` property.
9. **Documentation update** – add the new feature to the README screenshots and to this conventions document if new patterns are introduced.

---

## 8. Testing Conventions

- **Algorithms** – pure unit tests (`Xunit` / `MSTest`). No WPF dependencies.
- **Services** – test orchestration logic with mock algorithms (`Moq` or handwritten fakes).
- **ViewModels** – test property changes, command `CanExecute` logic, and that the service is called with the expected parameters. Use a mock service to avoid heavy work.
- **UI (optional)** – use `Microsoft.VisualStudio.TestTools.UITesting` or `XAML` snapshot tests if needed, but not required for this small project.
- **CSV / I/O** – test repository methods with a temporary file in the test folder.

---

## 9. Anti‑patterns to Avoid

- Placing allocation logic in a View or ViewModel code‑behind.
- Over‑loading a ViewModel with unrelated responsibilities (e.g., file I/O, navigation). Use separate Service classes.
- Directly creating concrete algorithm instances inside a ViewModel (`new FirstFitAlgorithm()`). Prefer injection via the Service.
- Accessing UI controls from a ViewModel (`myButton.IsEnabled = …`). Use command `CanExecute` or bind `IsEnabled`.
- Excessive use of `new` for dependencies – makes unit‑testing painful.
- Using `async` wrappers for synchronous code without I/O; keep the method synchronous unless it truly performs asynchronous work.
- Updating UI manually (e.g., `myLabel.Content = …`) instead of relying on bindings.
- Assuming every property will raise change notifications; always implement `INotifyPropertyChanged` (or use `[ObservableProperty]`).

---

## 10. References within the Repository

- ViewModel example: `ViewModels/SimulationConfigViewModel.cs`
- Base ViewModel: `ViewModels/BaseViewModel.cs`
- XAML layout: `Views/Simulation/SimulationConfigView.xaml`
- Resources: `Resources/*.xaml`
- Project file specifies `CommunityToolkit.Mvvm` version `8.4.2` (see `OSMemoryAllocationSimulator.csproj`).

---

_This document is stored at `docs/architecture/mvvm-conventions.md` and should be kept up‑to‑date as the architecture evolves._

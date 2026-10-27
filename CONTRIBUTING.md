# Contributing to OSMemoryAllocationSimulator

## Introduction
We welcome any contribution that improves the UI, adds MVVM bindings, or refactors code. This project follows a **UI‑first, MVVM** approach and is planned for a 4‑week sprint.

## Development workflow
1. **Fork** the repository.
2. **Create a feature branch**:
   ```bash
   git checkout -b feature/simulation-ui
   ```
3. Make your changes.
4. **Commit** using Conventional Commits, e.g.:
   ```
   feat(ui): add memory‑size textbox to SimulationConfigView
   ```
5. **Push** to your fork and open a **Pull Request** targeting `main`.

## Prerequisites
- .NET 8 SDK (`dotnet --list-sdks`)
- Visual Studio 2022 (or any editor that supports WPF)
- CommunityToolkit.Mvvm (installed via NuGet)

## Setup
```bash
git clone https://github.com/VUTRUONG04/os-memory-allocation-simulator.git
cd os-memory-allocation-simulator
dotnet restore
dotnet build
```

## Branch naming
- `feature/*` – new UI or MVVM features
- `bugfix/*` – bug fixes
- `chore/*` – tooling / CI changes

## Pull‑request checklist
- [ ] **Build passes** (`dotnet build`)
- [ ] **Tests pass** (`dotnet test`) – (if any)
- [ ] No lint/formatting errors (`dotnet format`)
- [ ] Updated documentation / screenshots when UI changes
- [ ] Linked issue (e.g. `Fixes #12`)

## Issue workflow
1. Open an *Issue* and select a label (`ui`, `vm`, `bug`, `enhancement`).
2. Assign the issue to a team member.
3. Reference the issue in your PR description.

## Local UI testing
Use the **Live Visual Tree** (Debug → Windows → Live Visual Tree) to verify bindings. If you see binding errors in the Output window, check property names and `INotifyPropertyChanged` implementation.

## Style guide
- **XAML**: 4‑space indent, use `StaticResource` for colors/styles, name controls in PascalCase (`BtnStartSimulation`).
- **C#**: PascalCase for public members, `_camelCase` for private fields, XML docs (`///`).
- **ViewModel**: inherit from `BaseViewModel` (CommunityToolkit.ObservableObject). Use `[ObservableProperty]` and `[RelayCommand]`.

## Running the app
```bash
dotnet run
# or press F5 in Visual Studio
```

## Contact
If you have questions, reach out on the project’s GitHub Discussions or Slack channel.

Happy coding!
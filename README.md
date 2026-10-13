# OS Memory Allocation Simulator

A Windows desktop application developed for an Operating Systems course project. The application aims to simulate and visualize memory allocation strategies and compare their behavior under different scenarios.

**Project status:** Initial WPF application created. Core simulation features are planned and under development.

## Objectives

- Understand contiguous memory allocation in operating systems.
- Implement and compare First Fit, Best Fit, and Worst Fit.
- Simulate memory allocation and deallocation.
- Visualize memory blocks and allocation states.
- Analyze memory fragmentation.
- Validate algorithm correctness through test cases.
- Import simulation scenarios and export results using CSV files.

## Planned Features

- **Memory Allocation:** Simulate First Fit, Best Fit, and Worst Fit.
- **Memory Deallocation:** Release allocated memory and update the memory state.
- **Memory Visualization:** Display allocated and free memory blocks.
- **Simulation Trace:** Track allocation and deallocation operations.
- **Fragmentation Analysis:** Evaluate fragmentation according to the project requirements.
- **Algorithm Comparison:** Compare allocation results using the same input scenarios.
- **CSV Import and Export:** Load simulation inputs and export execution traces and results.
- **Testing:** Compare expected and actual results and evaluate performance on different input sizes.

These features are planned and should not be considered implemented until they are available in the application.

## Technology Stack

- **Language:** C#
- **Desktop UI:** Windows Presentation Foundation (WPF)
- **Framework:** .NET 8 (`net8.0-windows`)
- **IDE:** Visual Studio 2022
- **Version Control:** Git and GitHub

## Getting Started

## Prerequisites

- Windows.
- Visual Studio 2022 with the .NET 8 SDK and the required Windows desktop development components.
- Git.

### Run the Application

1. Clone the repository.
2. Open `OSMemoryAllocationSimulator.sln` in Visual Studio 2022.
3. Restore NuGet dependencies if prompted.
4. Build the solution.
5. Press `F5` to run the application.

The required target framework is defined in the project file (`.csproj`). Make sure the corresponding .NET SDK is installed.

## Input and Output

The application is planned to support CSV input scenarios and CSV output for simulation traces and results. The exact schemas and validation rules will be documented when they are finalized.

## Testing and Evaluation

Planned verification activities include:

- Normal allocation scenarios.
- Requests that cannot be satisfied by available memory.
- Memory deallocation and state updates.
- Invalid input handling.
- Expected-versus-actual result comparison.
- Performance testing with different input sizes.

## Project Structure

The directory structure will be documented as the architecture and implementation evolve.

## Team and Contributions

Team members, responsibilities, and individual contributions will be documented as the project progresses.

## References and AI Usage

Relevant academic and technical references will be documented during development.

AI tools may be used to support research, explain concepts, review code, and assist development. AI-assisted work must be reviewed and validated by the team, and AI usage will be disclosed in accordance with the course requirements.
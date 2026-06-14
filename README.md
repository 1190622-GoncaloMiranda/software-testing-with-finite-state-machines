# Software Testing with Finite State Machines

This repository contains the implementation artefacts developed for the dissertation *Software Testing with Finite State Machines*. The work is organised around two C#/.NET 8 projects that cover the research and delivery stages of the approach.

## Repository Structure

### `ExperimentalPrototype`

The experimental prototype used to evaluate FSM-based automated test generation and optimisation strategies under controlled conditions.

It supports comparative experimentation across benchmark FSM models, combining:

- coverage strategies
- reduction strategies
- prioritisation strategies
- mutation-testing evaluation

Main entry points:

- [ExperimentalPrototype README](/Users/goncalomiranda/Documents/MEI/2ano/PREPD/PREPD/ExperimentalPrototype/README.md)
- `ExperimentalPrototype/ExperimentalPrototype.sln`

### `FinalArtefact`

The final configurable FSM test-suite generator produced from the dissertation work.

It executes a single configured pipeline over a target FSM model and exports:

- `test_suite.json`
- `metrics_report.json`

Main entry points:

- [FinalArtefact README](/Users/goncalomiranda/Documents/MEI/2ano/PREPD/PREPD/FinalArtefact/README.md)
- `FinalArtefact/FinalArtefact.sln`

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

Verify installation:

```bash
dotnet --version
```

## Getting Started

Run the experimental prototype:

```bash
cd ExperimentalPrototype
dotnet run
```

Run the final artefact:

```bash
cd FinalArtefact
dotnet run
```

## Continuous Integration

The repository includes a GitHub Actions workflow at `.github/workflows/generate-tests.yml` that builds the final artefact, runs the configured FSM pipeline, and uploads the generated JSON outputs as workflow artifacts.

## Notes

Each project has its own dedicated README with architecture, workflow diagrams, configuration details, supported strategies, and output formats.

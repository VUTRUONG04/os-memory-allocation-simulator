# Contributing Guidelines

Thank you for contributing to the OS Memory Allocation Simulator project.

This repository is maintained by a student team as part of an Operating Systems course project.

## 1. Development Workflow

- Make sure you understand the assigned task before starting implementation.
- Keep changes focused on one feature, bug fix, test, or documentation update.
- Avoid modifying unrelated files.
- Discuss significant design or architecture changes with the team before implementation.

## 2. Branching Strategy

- `master`: The shared, stable branch. Direct pushes are not allowed.
- `feature/<short-description>`: Used for new features.
- `fix/<short-description>`: Used for bug fixes.
- `docs/<short-description>`: Used for documentation changes.

Examples:

- `feature/first-fit`
- `feature/memory-visualization`
- `fix/invalid-memory-input`
- `docs/algorithm-specification`

All contributors must create a separate branch for their work and submit a Pull Request (PR) to merge changes into `master`.

Do not push feature, fix, or unfinished work directly to `master`.

## 3. Commit Messages

Write clear commit messages that describe the purpose of each change.

Suggested format:

`<type>: <short description>`

Common types:

- `feat`: Add a feature.
- `fix`: Fix a bug.
- `docs`: Update documentation.
- `test`: Add or update tests.
- `refactor`: Improve code structure without changing intended behavior.
- `chore`: Perform project maintenance.

Examples:

- `feat: implement first-fit allocation`
- `test: add best-fit boundary cases`
- `docs: document CSV input format`

## 4. Pull Requests and Code Review

Every change must be submitted through a Pull Request targeting `master`.

- **Required reviewer:** The repository maintainer (project lead).
- **Approval:** The PR must be reviewed and approved by the project lead before merging.
- **Author responsibility:** The author must build the solution, run relevant tests, review the changed files, and explain how the change was verified.
- **Review responsibility:** The reviewer checks correctness, readability, separation between algorithm and UI, test coverage, and unintended changes.
- **Merge policy:** Do not merge a PR before the required approval has been received.

The author should be able to explain the submitted code and its expected behavior.

## 5. Algorithm and UI Separation

Keep the memory allocation logic separate from WPF event handlers and presentation code.

Algorithm changes should be testable independently of the graphical interface.

## 6. Testing

- Include expected results for representative test cases.
- Cover normal cases, boundary cases, and cases that are easy to implement incorrectly.
- Validate input before running a simulation.
- Record relevant test results and performance measurements.

## 7. Documentation and AI Usage

- Update the README and relevant documentation when behavior or setup instructions change.
- Cite external references where required by the course.
- AI tools may assist development, but contributors must review, verify, and understand the code they submit.
- Disclose AI usage in accordance with the course requirements.
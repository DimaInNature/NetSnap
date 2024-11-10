# NetSnap

NetSnap is a CLI tool designed for creating detailed snapshots of the structure and content of .NET projects. It simplifies project analysis and documentation by capturing all relevant files and their contents, while ignoring unnecessary files and directories.

## Features

- Scans a .NET project directory for files and captures their content.
- Automatically ignores common build artifacts (`bin`, `obj`) and irrelevant files (`.gitignore`, images, etc.).
- Supports customizable output file paths.
- Provides clear, human-readable snapshots for easy sharing or documentation.

## Installation

Install the `NetSnap` tool globally using the .NET CLI:

```bash
dotnet tool install --global NetSnap
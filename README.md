# NetSnap

NetSnap is a CLI tool designed for creating detailed snapshots of the structure and content of .NET projects. It simplifies project analysis and documentation by capturing all relevant files and their contents, while ignoring unnecessary files and directories.

## Features

- Scans a .NET project directory for files and captures their content.
- Automatically ignores common build artifacts (`bin`, `obj`) and irrelevant files (`.gitignore`, images, etc.).
- Supports customizable output file paths.
- Provides clear, human-readable snapshots for easy sharing or documentation.
- Optional output splitting (`--split`) to avoid huge snapshot files.
- Optional per-`csproj` snapshots (`--by-csproj`) to generate one file per project.

## Installation

Install the `NetSnap` tool globally using the .NET CLI:

```bash
dotnet tool install --global NetSnap
```

Update to the latest version:

```bash
dotnet tool update --global NetSnap
```

After installation, the command will be available as:

```bash
netsnap
```

## Usage

```bash
netsnap [sourcePath] [outputPathOrDir] [--split <size>] [--by-csproj]
```

## Arguments

- `sourcePath` (optional)

  Directory to scan.
  If omitted, uses the current directory (Release) or the project directory (Debug).

- `outputPathOrDir` (optional)

  Where to write the snapshot(s).

  - If it looks like a file path (has an extension), NetSnap writes to that file (default: `snapshot.txt` inside `sourcePath`).
  - If it looks like a directory (exists as a directory, or has no extension), NetSnap writes results into that directory.

## Options

- `--split <size>`

  Split output into multiple files with a maximum size.

  Supported formats:

  - Raw bytes: `500000`
  - With suffix: `500KB`, `5MB`, `1GB` (case-insensitive)

  Notes:

  - When splitting a single output file, NetSnap writes: `snapshot.txt`, `snapshot_2.txt`, `snapshot_3.txt`, ...
  - Splitting is based on UTF-8 byte size.

- `--by-csproj`

  Generate separate output per `*.csproj`.

  - Output file names are derived from the `.csproj` file name.
  - Example: `MyProject.csproj` → `MyProject.txt`
  - If used together with `--split`, large projects produce: `MyProject.txt`, `MyProject_2.txt`, `MyProject_3.txt`, ...

- `--help`, `-h`, `/?`

  Show help.

## Examples

Create a single snapshot file (default output is `snapshot.txt` in the source folder):

```bash
netsnap .
```

Create a single snapshot file at a custom path:

```bash
netsnap . .\snapshot.txt
```

Split a single snapshot into chunks of up to 5MB:

```bash
netsnap . .\snapshot.txt --split 5MB
```

Create one snapshot per `.csproj` in an output folder:

```bash
netsnap . .\out --by-csproj
```

Create one snapshot per `.csproj` and split each project snapshot into 2MB chunks:

```bash
netsnap . .\out --by-csproj --split 2MB
```

## Output Format

The output is a human-readable text file with:

- project headers per `.csproj`
- file paths (relative to `sourcePath`)
- file contents

Example (simplified):

```
Project Snapshot:

### Project: MyProject.csproj ###

File: src/Program.cs
-------- File Content --------
...
------------------------------

### End of Project: MyProject.csproj ###
```

## Ignored Content

NetSnap intentionally skips:

- directories: `bin`, `obj`, and hidden folders (starting with `.`)
- files: `.gitattributes`, `.gitignore`, and the default output file name `snapshot.txt`
- extensions: `.img`, `.jpeg`, `.webp`

To avoid recursively capturing generated snapshots, NetSnap also ignores the output file itself and split parts:

- `snapshot.txt`
- `snapshot_2.txt`, `snapshot_3.txt`, ...
- `<Project>.txt`, `<Project>_2.txt`, ... (when `--by-csproj` is used)

## Development

### Build

```bash
dotnet build -c Release
```

### Run locally

```bash
dotnet run --project .\NetSnap\NetSnap.csproj -- . .\snapshot.txt
```

### Tests

```bash
dotnet test
```

## Notes

- Snapshots are written using UTF-8 (without BOM).
- Large file contents are streamed line-by-line to reduce memory pressure.
- If you commit generated snapshot files into the repository, consider adding them to `.gitignore`.

## License

MIT.


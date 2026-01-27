# NoPowerShell AGENTS

This file guides coding agents working in this repository.

## Project overview
- NoPowerShell is a C# tool that implements PowerShell-like cmdlets without dependency on the `System.Management.Automation` library.
- Cmdlets live under `Source/NoPowerShell/Commands` and are discovered via reflection.
- The core runtime is .NET Framework (not .NET Core).

## Repo layout
- `Source/NoPowerShell/Commands`: cmdlet implementations, grouped by PowerShell module folder.
- `Source/NoPowerShell/Arguments`: argument types and parsing helpers.
- `Source/NoPowerShell/HelperClasses`: shared helpers (e.g., `RegistryHelper`, `WmiHelper`, `DNSHelper`).
- `Source/NoPowerShell.Tests`: xUnit tests (net481).
- `Source/NoPowerShell/NoPowerShell.csproj`: main project build settings and compile list.

## Build and test
- Open `Source/NoPowerShell.sln` in Visual Studio (full .NET Framework).
- Build with MSBuild: `msbuild Source/NoPowerShell/NoPowerShell.csproj /p:Configuration=Release`
- Run tests: `dotnet test Source/NoPowerShell.Tests/NoPowerShell.Tests.csproj`

## Cmdlet creation checklist (required)
- Start from `Source/NoPowerShell/Commands/TemplateCommand.cs` for any new cmdlet.
- Place the new file under the correct module folder and match namespace:
  - Official PowerShell cmdlet: `NoPowerShell.Commands.<ModuleName>`
  - Non-official cmdlet: `NoPowerShell.Commands.Additional`
- Keep C# syntax compatible with version 7.3 or lower.
- Keep implementation in .NET Framework only; do not use .NET Core APIs.
- If .NET Core has functionality missing in .NET Framework, reimplement it in C#.
- Only use built-in .NET namespaces (plus `NoPowerShell.*`). Do not add new NuGet packages.
- If reimplementation would exceed 1000 lines, stop and report that an external .NET library is required.
- If reimplementation is 101-1000 lines, add a helper class in the same `.cs` file.
- Use only `BoolArgument`, `IntegerArgument`, and `StringArgument`.
  - For string arrays, split with: `.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)`
- Aliases:
  - Only use aliases from official Microsoft PowerShell documentation.
  - The full cmdlet name must be the first alias.
- Implement remote functionality using the `ComputerName` string parameter and integrated authentication.
  - Do NOT add or use Username/Password parameters for new cmdlets.
- Order of preference for implementation tech (avoid WMI):
  1) Built-in .NET functionality
  2) Win32 APIs via P/Invoke
  3) Registry read/write
  4) Filesystem read/write
- Fully implement all code paths; do not ship simplified or stubbed behavior.
- If attributes returned by the PowerShell cmdlet to be implemented cannot be found, add the code for the `ResultRecord`, but commented out and with `string.Empty` as value.
- Place constants and helper functions at the bottom of the cmdlet source file.
- Add the new `.cs` file to `Source/NoPowerShell/NoPowerShell.csproj` if needed.

## Output and pipeline conventions
- Derive from `PSCommand` and return `_results` from `Execute` so pipeline output flows.
- Store output in `ResultRecord` entries (attribute/value pairs).
- In the output use the order of attributes that is also used in the original PowerShell cmdlet.
- Use `Program.WriteWarning` for non-fatal user-facing warnings.

## House rules
- When adding a new cmdlet file, also add it to `Source/NoPowerShell/NoPowerShell.csproj` under `<Compile Include=...>`.
- Every cmdlet must have a clear `Synopsis` and at least one `ExampleEntry` that reflects real usage.
- Update `CHEATSHEET.md` and the "Included NoPowerShell cmdlets" table in `README.md` when adding or renaming cmdlets.
- Prefer existing helpers in `Source/NoPowerShell/HelperClasses` before writing new helper code.
- If a cmdlet requires a higher .NET Framework feature, wrap it with `#if` as described in `CONTRIBUTING.md` and note the requirement in `Synopsis`.
- Keep output schema stable: `ResultRecord` keys should be consistent across runs; include `ComputerName` for remote results.
- Add or update tests in `Source/NoPowerShell.Tests` when introducing new argument parsing or complex logic.

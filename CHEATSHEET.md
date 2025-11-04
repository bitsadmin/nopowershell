# Cheatsheet
Cheatsheet of offensive PowerShell commands that are supported by NoPowerShell.

| Action | Command |
| - | - |
| List all properties of the Administrator domain user | `Get-ADUser -Identity Administrator -Properties *` |
| List all Administrative users in domain | `Get-ADUser -LDAPFilter "(admincount=1)"` |
| List all users in domain | `Get-ADUser -Filter *` |
| List specific attributes of user | `Get-ADUser -Identity Administrator -Properties SamAccountName,ObjectSID` |
| List all users in a specific OU | `Get-ADUser -SearchBase "CN=Users,DC=MyDomain,DC=local" -Filter *` |
| Get the sites from the configuration naming context | `Get-ADObject -LDAPFilter "(objectClass=site)" -SearchBase "CN=Configuration,DC=MyDomain,DC=local" -Properties whenCreated,cn` |
| Get specific object | `Get-ADObject -Identity "CN=Directory Service,CN=Windows NT,CN=Services,CN=Configuration,DC=MyDomain,DC=local" -Properties *` |
| List all global groups | `Get-ADObject -LDAPFilter "(GroupType:1.2.840.113556.1.4.803:=2)" -SearchBase "DC=MyDomain,DC=local"` |
| List only users that are directly in the OU (not in sub-OUs) | `Get-ADObject -SearchBase "CN=Users,DC=MyDomain,DC=local" -LDAPFilter "(objectClass=user)" -SearchScope OneLevel` |
| Obtain distinguishedname of domain | `Get-ADObject -LDAPFilter "(objectClass=*)" -SearchScope Base -Server MyServer` |
| Get all subnets | `Get-ADReplicationSubnet` |
| Get subnets with a specified name | `Get-ADReplicationSubnet -Identity "10.0.10.0/24"` |
| Get the properties of a specified subnet | `Get-ADReplicationSubnet -Identity "10.0.10.0/24" -Properties *` |
| List all user groups in domain | `Get-ADGroup -Filter *` |
| List all administrative groups in domain | `Get-ADGroup -LDAPFilter "(admincount=1)" \| select Name` |
| List all members of the "Domain Admins" group | `Get-ADGroupMember -Identity "Domain Admins"` |
| List all members of the "Domain Admins" group - Alternative | `Get-ADGroupMember "Domain Admins"` |
| List all properties of the DC1 domain computer | `Get-ADComputer -Identity DC1 -Properties *` |
| List all Domain Controllers | `Get-ADComputer -LDAPFilter "(msDFSR-ComputerReferenceBL=*)"` |
| List all computers in domain | `Get-ADComputer -Filter *` |
| List domain controllers | `Get-ADComputer -searchBase "OU=Domain Controllers,DC=mydomain,DC=local" -Filter *` |
| List specific attributes of the DC1 domain computer | `Get-ADComputer -Identity DC1 -Properties Name,operatingSystem` |
| List domain controllers | `Get-ADDomainController` |
| List all domain controllers, including read-only ones | `Get-ADDomainController -Filter *` |
| List all direct trusts | `Get-ADTrust -Filter *` |
| List trusts recursively till depth 3 | `Get-ADTrust -Filter * -Depth 3` |
| List all details of a certain trust | `Get-ADTrust -LDAPFilter "(Name=mydomain.com)"` |
| List specific details of a certain trust | `Get-ADTrust -LDAPFilter "(Name=mydomain.com)" -Properties Name,trustDirection,securityIdentifier` |
| Copy ACL from file to another file | `Copy-Acl C:\Data\file.txt C:\Data\destination.txt` |
| Recursively copy ACLs from source to destination folder | `Copy-Acl -Recurse -Path C:\SourceDir -Destination C:\DestinationDir` |
| List local SMB shares | `Get-RemoteSmbShare` |
| List SMB shares of MyServer | `Get-RemoteSmbShare \\MyServer` |
| List DNS zones | `Get-ADObject -SearchBase "CN=MicrosoftDNS,DC=DomainDnsZones,DC=AD,DC=bitsadmin,DC=com" -LDAPFilter "(ObjectClass=dnsZone)" -SearchScope OneLevel` |
| Obtain IP address of host W11 | `Resolve-AdiDnsName -ZoneName ad.bitsadmin.com -Name W11` |
| Obtain LDAP servers in domain | `Resolve-AdiDnsName -ZoneName ad.bitsadmin.com -Name _ldap._tcp` |
| Show the current user | `whoami` |
| List groups the current user is member of | `whoami -Groups` |
| Query sessions on local machine | `Get-WinStation` |
| Query sessions on a remote machine | `Get-WinStation -Server MyServer` |
| Query sessions on a remote machine - Alternative | `qwinsta MyServer` |
| Compress folder to zip | `Compress-Archive -Path C:\MyFolder -DestinationPath C:\MyFolder.zip` |
| Compress folder to zip - Alternative | `zip C:\MyFolder C:\MyFolder.zip` |
| Extract zip | `Expand-Archive -Path C:\MyArchive.zip -DestinationPath C:\Extracted` |
| Extract zip - Alternative | `unzip C:\MyArchive.zip C:\Extracted` |
| Extract zip into current directory | `unzip C:\MyArchive.zip` |
| Get help for a command | `Get-Help -Name Get-Process` |
| Get help for a command - Alternative | `man ps` |
| List all commands supported by NoPowerShell | `Get-Command` |
| List commands of a certain module | `Get-Command -Module ActiveDirectory` |
| List all processes containing PowerShell in the process name | `Get-Process \| ? Name -Like *PowerShell*` |
| List local drives | `Get-PSDrive \| ? Provider -EQ FileSystem` |
| Resolve domain name | `Resolve-DnsName microsoft.com` |
| Resolve domain name - Alternative | `host linux.org` |
| Lookup specific record | `Resolve-DnsName -Type MX pm.me` |
| Reverse DNS lookup | `Resolve-DnsName 1.1.1.1` |
| List members of the Administrators group | `Get-LocalGroupMember -Group Administrators` |
| List all local groups | `Get-LocalGroup` |
| List details of a specific group | `Get-LocalGroup Administrators` |
| List members of Administrators group on a remote computer using WMI | `Get-LocalGroup -ComputerName Myserver -Username MyUser -Password MyPassword -Name Administrators` |
| List members of Administrators group on a remote computer using WMI - Alternative | `Get-LocalGroup -ComputerName Myserver -Name Administrators` |
| Put string on clipboard | `Set-Clipboard -Value "You have been PWNED!"` |
| Put string on clipboard - Alternative | `scb "You have been PWNED!"` |
| Clear the clipboard | `Set-Clipboard ""` |
| Place output of command on clipboard | `Get-Process \| Set-Clipboard` |
| List drives | `Get-PSDrive` |
| List drives - Alternative | `gdr` |
| Get all hotfixes on the local computer | `Get-HotFix` |
| Get all hotfixes from a remote computer using WMI | `Get-HotFix -ComputerName MyServer -Username MyUser -Password MyPassword` |
| Get all hotfixes from a remote computer using WMI - Alternative | `Get-HotFix -ComputerName MyServer` |
| Gracefully stop processes | `Stop-Process -Id 4512,7241` |
| Kill process | `Stop-Process -Force -Id 4512` |
| Kill all cmd.exe processes | `Get-Process cmd \| Stop-Process -Force` |
| Locate KeePass files in the C:\Users\ directory | `Get-ChildItem -Recurse -Force C:\Users\ -Include *.kdbx` |
| Locate KeePass files in the C:\Users\ directory - Alternative | `ls -Recurse -Force C:\Users\ -Include *.kdbx` |
| List autoruns | `ls HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` |
| Search for files which can contain sensitive data on the C-drive | `ls -Recurse -Force C:\ -Include *.cmd,*.bat,*.ps1,*.psm1,*.psd1` |
| Create directory listing of SYSVOL | `ls -Recurse -FollowSymlinks \\DC1\SYSVOL` |
| Directory listing using LiteralPath | `Get-ChildItem -Recurse -LiteralPath \\?\C:\SomeVeryLongPath\ -Include *.pem` |
| Copy file from one location to another | `Copy-Item C:\Tmp\nc.exe C:\Windows\System32\nc.exe` |
| Copy file from one location to another - Alternative | `copy C:\Tmp\nc.exe C:\Windows\System32\nc.exe` |
| Copy folder | `copy C:\Tmp\MyFolder C:\Tmp\MyFolderBackup` |
| Launch process | `Invoke-WmiMethod -Class Win32_Process -Name Create "cmd /c calc.exe"` |
| Launch process on remote system | `Invoke-WmiMethod -ComputerName MyServer -Username MyUser -Password MyPassword -Class Win32_Process -Name Create "powershell -NoP -W H -E ZQBjAGgAbwAgACcASABlAGwAbABvACAATgBvAFAAbwB3AGUAcgBTAGgAZQBsAGwAIQAnAA=="` |
| Launch process on remote system - Alternative | `iwmi -ComputerName MyServer -Class Win32_Process -Name Create "powershell -NoP -W H -E ZQBjAGgAbwAgACcASABlAGwAbABvACAATgBvAFAAbwB3AGUAcgBTAGgAZQBsAGwAIQAnAA=="` |
| Delete a file | `Remove-Item C:\tmp\MyFile.txt` |
| Delete a file - Alternative | `rm C:\tmp\MyFile.txt` |
| Delete a read-only file | `Remove-Item -Force C:\Tmp\MyFile.txt` |
| Recursively delete a folder | `Remove-Item -Recurse C:\Tmp\MyTools\` |
| Show current user's PATH variable | `Get-ItemPropertyValue -Path HKCU:\Environment -Name Path` |
| Show current user's PATH variable - Alternative | `gpv HKCU:\Environment Path` |
| List autoruns in the registry | `Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Run \| ft` |
| Show information about the system | `Get-ComputerInfo` |
| Show information about the system - Alternative | `systeminfo` |
| Show information about the system not listing patches | `systeminfo -Simple` |
| Show information about a remote machine using WMI | `Get-ComputerInfo -ComputerName MyServer -Username MyUser -Password MyPassword` |
| Show information about a remote machine using WMI - Alternative | `Get-ComputerInfo -ComputerName MyServer` |
| List cached DNS entries on the local computer | `Get-DnsClientCache` |
| List cached DNS entries from a remote computer using WMI | `Get-DnsClientCache -ComputerName MyServer -Username MyUser -Password MyPassword` |
| List cached DNS entries from a remote computer using WMI - Alternative | `Get-DnsClientCache -ComputerName MyServer` |
| List processes | `Get-Process` |
| List processes - Alternative | `ps` |
| List processes on remote host using WMI | `Get-Process -ComputerName MyServer -Username MyUser -Password MyPassword` |
| List processes on remote host using WMI - Alternative | `ps -ComputerName MyServer` |
| List local shares | `Get-WmiObject -Namespace ROOT\CIMV2 -Query "Select * From Win32_Share Where Name LIKE '%$'"` |
| List local shares - Alternative | `gwmi -Class Win32_Share -Filter "Name LIKE '%$'"` |
| Obtain data of Win32_Process class from a remote system and apply a filter on the output | `Get-WmiObject "Select ProcessId,Name,CommandLine From Win32_Process" -ComputerName MyServer -Username MyUser -Password MyPassword \| ? Name -Like *PowerShell* \| select ProcessId,CommandLine` |
| Obtain data of Win32_Process class from a remote system and apply a filter on the output - Alternative | `gwmi "Select ProcessId,Name,CommandLine From Win32_Process" -ComputerName MyServer \| ? Name -Like *PowerShell* \| select ProcessId,CommandLine` |
| View details about a certain service | `Get-WmiObject -Class Win32_Service -Filter "Name = 'WinRM'"` |
| List installed antivirus products (on non-server OS) | `Get-WmiObject -Namespace root\SecurityCenter2 -Class AntiVirusProduct` |
| Show text contents of clipboard | `Get-Clipboard` |
| Show text contents of clipboard - Alternative | `gcb` |
| View contents of a file | `Get-Content C:\Windows\WindowsUpdate.log` |
| View contents of a file - Alternative | `cat C:\Windows\WindowsUpdate.log` |
| Send ICMP request to host | `Test-NetConnection 1.1.1.1` |
| Send ICMP request to host - Alternative | `tnc 1.1.1.1` |
| Send 2 ICMP requests to IP address 1.1.1.1 with half a second of timeout | `Test-NetConnection -Count 2 -Timeout 500 1.1.1.1` |
| Perform a traceroute with a timeout of 1 second and a maximum of 20 hops | `Test-NetConnection -TraceRoute -Timeout 1000 -Hops 20 bitsadmin.com` |
| Perform ping with maximum TTL specified | `ping -TTL 32 1.1.1.1` |
| Check for open port | `tnc bitsadmin.com -Port 80` |
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
| Get all services on the local computer | `Get-Service` |
| Get a specific service by name | `Get-Service -Name wuauserv` |
| Get services by display name | `Get-Service -DisplayName "Windows Update"` |
| Get services on a remote computer | `Get-Service -ComputerName MyServer` |
| Filter services using Include and Exclude | `Get-Service -Include "Win"` |
| Filter services using Include and Exclude - Alternative | `Get-Service -Exclude "WinRM"` |
| Gets all the local user accounts on the computer | `Get-LocalUser \| select Name,Enabled,Description` |
| Gets the local user account with the name Administrator | `Get-LocalUser -Name Administrator` |
| Gets all the local user accounts that match the name pattern | `Get-LocalUser -Name Admin* \| fl` |
| Gets the local user account that has the specified SID | `Get-LocalUser -SID S-1-5-21-222222222-3333333333-4444444444-5555` |
| Gets all the local user accounts on a remote computer | `Get-LocalUser -ComputerName MyServer \| select Name,Enabled,Description` |
| Gets all the local user accounts on a remote computer using WMI instead of Netapi32!NetUserEnum | `Get-LocalUser -UseWMI -ComputerName MyServer -Username LabAdmin -Password Password1!` |
| List oldest 10 events of Application log | `Get-WinEvent -LogName Application -MaxEvents 10 -Oldest` |
| Determine the IP address from where a specific user is authenticating to the DC | `Get-WinEvent -LogName Security -FilterXPath "*[System[(EventID=4624)]] and *[EventData[Data[@Name='TargetUserName']='bitsadmin']]" -ComputerName MyServer -MaxEvents 1` |
| Create basic shortcut | `New-Shortcut -Path C:\Users\Public\Desktop\Notepad.lnk -TargetPath C:\Windows\notepad.exe` |
| Create advanced shortcut | `New-Shortcut -Path "C:\Users\User1\Desktop\Microsoft Edge.lnk" -TargetPath C:\Windows\System32\cmd.exe -Arguments "/C echo PWNED>pwned.txt" -IconLocation "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe,0" -WorkingDirectory "%~dp0" -WindowStyle Minimized -Hotkey "Ctrl+Shift+C" -Force` |
| List ACLs of file | `Get-Acl C:\Windows\explorer.exe` |
| List ACLs of file - Alternative | `Get-Acl -Path C:\Windows\explorer.exe` |
| List ACLs of directory | `Get-Acl C:\Windows` |
| List ACLs of AD Object | `Get-Acl "AD:\CN=User One,CN=Users,DC=ad,DC=bitsadmin,DC=com"` |
| List mapped network drives | `Get-SmbMapping` |
| List mapped network drives - Alternative | `netuse` |
| List SMB shares on the computer | `Get-SmbShare` |
| List local SQL Server version | `Invoke-Sqlcmd -Query "SELECT @@version"` |
| Query specific server | `Invoke-Sqlcmd -Query "SELECT username,password FROM users" -ServerInstance SQL1` |
| Use explicit authentication | `Invoke-Sqlcmd -Query "exec xp_cmdshell 'dir C:\'" -ServerInstance SQL1 -Username sa -password Password1!` |
| Use encrypted connection | `Invoke-Sqlcmd -Query "INSERT INTO logins (username,password) VALUES ('newuser', 'MyPass')" -ServerInstance SQL1 -Database CRM -EncryptConnection` |
| Use connectionstring to named pipe | `Invoke-Sqlcmd -Query "SELECT * FROM transactions LIMIT 10'" -ConnectionString "Server=\\.\pipe\sql\query; Database=Sales; Integrated Security=True;"` |
| Calculate commonly used hashes (MD5,SHA1,SHA256) for file | `Get-FileHash C:\Windows\explorer.exe` |
| Calculate commonly used hashes (MD5,SHA1,SHA256) for file - Alternative | `Get-FileHash -Path C:\Windows\explorer.exe -Algorithm common` |
| Calculate SHA256 hash of a file | `Get-FileHash -Path C:\Windows\explorer.exe -Algorithm SHA256` |
| Calculate specific hashes for file | `Get-FileHash C:\file.bin -Algorithm MD5,SHA1` |
| Calculate all supported hashes (MD5,SHA1,SHA256,SHA384,SHA512,RIPEMD160) for file | `Get-FileHash C:\file.bin -Algorithm *` |
| Convert Active Directory SDDL (nTSecurityDescriptor) to readable format' command | `ConvertFrom-SddlString "D:(A;;CR;;;S-1-5-21-2137271609-6538894-3613171323-1144)" -Type ActiveDirectoryRights` |
| Convert filesystem SDDL to readable format | `ConvertFrom-SddlString "O:BAG:BAD:(A;;FA;;;BA)(A;;0x1200a9;;;SY)"` |
| Convert filesystem SDDL to readable format - Alternative | `ConvertFrom-SddlString "O:BAG:BAD:(A;;FA;;;BA)(A;;0x1200a9;;;SY)" -Type FileSystemrights` |
| Sort processes by name descending | `ps \| sort -d name` |
| Count number of results | `Get-Process \| Measure-Object` |
| Count number of results - Alternative | `Get-Process \| measure` |
| Count number of lines in file | `gc C:\Windows\WindowsUpdate.log \| measure` |
| View external IP address using custom user agent | `iwr ifconfig.io/ip -UserAgent "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36 Edg/142.0.0.0"` |
| View external IP using explicit proxy | `Invoke-WebRequest https://ifconfig.io/ip -Proxy http://proxy:8080` |
| Download file from the Internet to disk | `wget https://live.sysinternals.com/psexec.exe -OutFile C:\Tmp\psexec.exe` |
| Perform request ignoring invalid TLS certificates | `iwr https://74.242.189.11/about_this_site.txt -SkipCertificateCheck` |
| Show certificate chain details | `Invoke-WebRequest -Verbose -SkipCertificateCheck https://74.242.189.11/about_this_site.txt` |
| Show only the Name in a file listing | `ls C:\ \| select Name` |
| Show first 10 results of file listing | `ls C:\Windows\System32 -Include *.exe \| select -First 10 Name,Length` |
| Create file hello.txt on the C: drive containing the "Hello World!" ASCII string | `Write-Output "Hello World!" \| Out-File -Encoding ASCII C:\hello.txt` |
| Create file hello.txt on the C: drive containing the "Hello World!" ASCII string - Alternative | `echo "Hello World!" \| Out-File -Encoding ASCII C:\hello.txt` |
| Create file with newlines | `echo "@echo off`r`necho Hello World!" \| Out-File -Encoding ASCII C:\hello.cmd` |
| Format output as a table | `Get-Process \| Format-Table` |
| Format output as a table - Alternative | `Get-Process \| ft` |
| Format output as a table showing only specific attributes | `Get-Process \| ft ProcessId,Name` |
| Format output as a list | `Get-LocalUser \| Format-List` |
| Format output as a list - Alternative | `Get-LocalUser \| fl` |
| Format output as a list showing only specific attributes | `Get-LocalUser \| fl Name,Description` |
| Store list of commands as CSV | `Get-Command \| Export-Csv -Encoding ASCII -Path commands.csv` |
| Display process list as CSV | `Get-Process \| ConvertTo-Csv` |
| Use tab delimiter | `Get-Process \| ConvertTo-Csv -Delimiter "`t"` |
| Echo string to the console | `Write-Output "Hello World!"` |
| Echo string to the console - Alternative | `echo "Hello World!"` |
| Echo string with escaped characters | `Write-Output "backtick: ``; tab: `t; lf: `ncr: `rrc"` |

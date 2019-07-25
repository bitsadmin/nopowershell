@echo off
:: Copyright (c) 2016-2019  Denis Kuzmin [ entry.reg@gmail.com ]
:: https://github.com/3F/DllExport
if "%~1"=="/?" goto bl
set "aa=%~dpnx0"
set ab=%*
set ac=%*
if defined ab (
if defined __p_call (
set ac=%ac:^^=^%
) else (
set ab=%ab:^=^^%
)
)
set wMgrArgs=%ac%
set ad=%ab:!=^!%
setlocal enableDelayedExpansion
set "ae=^"
set "ad=!ad:%%=%%%%!"
set "ad=!ad:&=%%ae%%&!"
set "af=1.6.4"
set "wAction=Configure"
set "ag=DllExport"
set "ah=tools/net.r_eg.DllExport.Wizard.targets"
set "ai=packages"
set "aj=https://www.nuget.org/api/v2/package/"
set "ak=build_info.txt"
set "al=!aa!"
set "wRootPath=!cd!"
set "am="
set "an="
set "ao="
set "ap="
set "aq="
set "ar="
set "as="
set "at="
set "au="
set /a av=0
if not defined ab (
if defined wAction goto bm
goto bl
)
call :bn bg !ad! bh
goto bo
:bl
echo.
@echo DllExport - v1.6.4.15293 [ f864a40 ]
@echo Copyright (c) 2009-2015  Robert Giesecke
@echo Copyright (c) 2016-2019  Denis Kuzmin [ entry.reg@gmail.com ] GitHub/3F
echo.
echo Licensed under the MIT license
@echo https://github.com/3F/DllExport
echo.
echo Based on hMSBuild and includes GetNuTool core: https://github.com/3F
echo.
@echo.
@echo Usage: DllExport [args to DllExport] [args to GetNuTool core]
echo ------
echo.
echo Arguments:
echo ----------
echo  -action {type} - Specified action for Wizard. Where {type}:
echo       * Configure - To configure DllExport for specific projects.
echo       * Update    - To update pkg reference for already configured projects.
echo       * Restore   - To restore configured DllExport.
echo       * Export    - To export configured projects data.
echo       * Recover   - To re-configure projects via predefined/exported data.
echo       * Unset     - To unset all data from specified projects.
echo       * Upgrade   - Aggregates an Update action with additions for upgrading.
echo.
echo  -sln-dir {path}    - Path to directory with .sln files to be processed.
echo  -sln-file {path}   - Optional predefined .sln file to be processed.
echo  -metalib {path}    - Relative path from PkgPath to DllExport meta library.
echo  -dxp-target {path} - Relative path to entrypoint wrapper of the main core.
echo  -dxp-version {num} - Specific version of DllExport. Where {num}:
echo       * Versions: 1.6.0 ...
echo       * Keywords:
echo         `actual` - Unspecified local/latest remote version;
echo                    ( Only if you know what you are doing )
echo.
echo  -msb {path}           - Full path to specific msbuild.
echo  -packages {path}      - A common directory for packages.
echo  -server {url}         - Url for searching remote packages.
echo  -proxy {cfg}          - To use proxy. The format: [usr[:pwd]@]host[:port]
echo  -pkg-link {uri}       - Direct link to package from the source via specified URI.
echo  -force                - Aggressive behavior, e.g. like removing pkg when updating.
echo  -mgr-up               - Updates this manager to version from '-dxp-version'.
echo  -wz-target {path}     - Relative path to entrypoint wrapper of the main wizard.
echo  -pe-exp-list {module} - To list all available exports from PE32/PE32+ module.
echo  -eng                  - Try to use english language for all build messages.
echo  -GetNuTool {args}     - Access to GetNuTool core. https://github.com/3F/GetNuTool
echo  -debug                - To show additional information.
echo  -version              - Displays version for which (together with) it was compiled.
echo  -build-info           - Displays actual build information from selected DllExport.
echo  -help                 - Displays this help. Aliases: -help -h
echo.
echo ------
echo Flags:
echo ------
echo  __p_call - To use the call-type logic when invoking %~nx0
echo.
echo --------
echo Samples:
echo --------
echo  DllExport -action Configure
echo  DllExport -action Restore -sln-file "Conari.sln"
echo  DllExport -proxy guest:1234@10.0.2.15:7428 -action Configure
echo  DllExport -action Configure -force -pkg-link http://host/v1.6.1.nupkg
echo.
echo  DllExport -build-info
echo  DllExport -debug -restore -sln-dir ..\
echo  DllExport -mgr-up -dxp-version 1.6.1
echo  DllExport -action Upgrade -dxp-version 1.6.1
echo.
echo  DllExport -GetNuTool -unpack
echo  DllExport -GetNuTool /p:ngpackages="Conari;regXwild"
echo  DllExport -pe-exp-list bin\Debug\regXwild.dll
goto bp
:bo
set /a aw=0
:bq
set ax=!bg[%aw%]!
if [!ax!]==[-help] ( goto bl ) else if [!ax!]==[-h] ( goto bl ) else if [!ax!]==[-?] ( goto bl )
if [!ax!]==[-debug] (
set am=1
goto br
) else if [!ax!]==[-action] ( set /a "aw+=1" & call :bs bg[!aw!] v
set wAction=!v!
for %%g in (Restore, Configure, Update, Export, Recover, Unset, Upgrade, Default) do (
if "!v!"=="%%g" goto br
)
echo Unknown -action !v!
exit/B 1
) else if [!ax!]==[-sln-dir] ( set /a "aw+=1" & call :bs bg[!aw!] v
set wSlnDir=!v!
goto br
) else if [!ax!]==[-sln-file] ( set /a "aw+=1" & call :bs bg[!aw!] v
set wSlnFile=!v!
goto br
) else if [!ax!]==[-metalib] ( set /a "aw+=1" & call :bs bg[!aw!] v
set wMetaLib=!v!
goto br
) else if [!ax!]==[-dxp-target] ( set /a "aw+=1" & call :bs bg[!aw!] v
set wDxpTarget=!v!
goto br
) else if [!ax!]==[-dxp-version] ( set /a "aw+=1" & call :bs bg[!aw!] v
set af=!v!
goto br
) else if [!ax!]==[-msb] ( set /a "aw+=1" & call :bs bg[!aw!] v
set ao=!v!
goto br
) else if [!ax!]==[-packages] ( set /a "aw+=1" & call :bs bg[!aw!] v
set ai=!v!
goto br
) else if [!ax!]==[-server] ( set /a "aw+=1" & call :bs bg[!aw!] v
set aj=!v!
goto br
) else if [!ax!]==[-proxy] ( set /a "aw+=1" & call :bs bg[!aw!] v
set at=!v!
goto br
) else if [!ax!]==[-pkg-link] ( set /a "aw+=1" & call :bs bg[!aw!] v
set ap=!v!
goto br
) else if [!ax!]==[-force] (
set ar=1
goto br
) else if [!ax!]==[-mgr-up] (
set as=1
goto br
) else if [!ax!]==[-wz-target] ( set /a "aw+=1" & call :bs bg[!aw!] v
set ah=!v!
goto br
) else if [!ax!]==[-pe-exp-list] ( set /a "aw+=1" & call :bs bg[!aw!] v
set aq=!v!
goto br
) else if [!ax!]==[-eng] (
chcp 437 >nul
goto br
) else if [!ax!]==[-GetNuTool] (
call :bt "accessing to GetNuTool ..."
for /L %%p IN (0,1,8181) DO (
if "!ay:~%%p,10!"=="-GetNuTool" (
set az=!ay:~%%p!
call :bu !az:~10!
set /a av=%ERRORLEVEL%
goto bp
)
)
call :bt "!ax! is corrupted: !ay!"
set /a av=1
goto bp
) else if [!ax!]==[-version] (
@echo v1.6.4.15293 [ f864a40 ]
goto bp
) else if [!ax!]==[-build-info] (
set an=1
goto br
) else if [!ax!]==[-tests] ( set /a "aw+=1" & call :bs bg[!aw!] v
set au=!v!
goto br
) else (
echo Incorrect key: !ax!
set /a av=1
goto bp
)
:br
set /a "aw+=1" & if %aw% LSS !bh! goto bq
:bm
call :bt "dxpName = " ag
call :bt "dxpVersion = " af
call :bt "-sln-dir = " wSlnDir
call :bt "-sln-file = " wSlnFile
call :bt "-metalib = " wMetaLib
call :bt "-dxp-target = " wDxpTarget
call :bt "-wz-target = " ah
if defined af (
if "!af!"=="actual" (
set "af="
)
)
if z%wAction%==zUpgrade (
call :bt "Upgrade is on"
set as=1
set ar=1
)
call :bv ai
set "ai=!ai!\\"
set "a0=!ag!"
set "wPkgPath=!ai!!ag!"
if defined af (
set "a0=!a0!/!af!"
set "wPkgPath=!wPkgPath!.!af!"
)
if defined ar (
if exist "!wPkgPath!" (
call :bt "Removing old version before continue. '-force' key rule. " wPkgPath
rmdir /S/Q "!wPkgPath!"
)
)
set a1="!wPkgPath!\\!ah!"
call :bt "wPkgPath = " wPkgPath
if not exist !a1! (
if exist "!wPkgPath!" (
call :bt "Trying to replace obsolete version ... " wPkgPath
rmdir /S/Q "!wPkgPath!"
)
call :bt "-pkg-link = " ap
call :bt "-server = " aj
if defined ap (
set aj=!ap!
if "!aj::=!"=="!aj!" (
set aj=!cd!/!aj!
)
if "!wPkgPath::=!"=="!wPkgPath!" (
set "a2=../"
)
set "a0=:!a2!!wPkgPath!|"
)
if defined ao (
set a3=-msbuild "!ao!"
)
set a4=!a3! /p:ngserver="!aj!" /p:ngpackages="!a0!" /p:ngpath="!ai!" /p:proxycfg="!at!"
call :bt "GetNuTool call: " a4
if defined am (
call :bu !a4!
) else (
call :bu !a4! >nul
)
)
if defined aq (
"!wPkgPath!\\tools\\PeViewer.exe" -list -pemodule "!aq!"
set /a av=%ERRORLEVEL%
goto bp
)
if defined an (
call :bt "buildInfo = " wPkgPath ak
if not exist "!wPkgPath!\\!ak!" (
echo information about build is not available.
set /a av=2
goto bp
)
type "!wPkgPath!\\!ak!"
goto bp
)
if not exist !a1! (
echo Something went wrong. Try to use another keys.
set /a av=2
goto bp
)
call :bt "wRootPath = " wRootPath
call :bt "wAction = " wAction
call :bt "wMgrArgs = " wMgrArgs
if defined ao (
call :bt "Use specific MSBuild tools: " ao
set a5="!ao!"
goto bw
)
call :bx bi & set a5="!bi!"
if "!ERRORLEVEL!"=="0" goto bw
echo MSBuild tools was not found. Try with `-msb` key.
set /a av=2
goto bp
:bw
if not defined a5 (
echo Something went wrong. Use `-debug` key for details.
set /a av=2
goto bp
)
if not defined au (
call :bt "Target: " a5 a1
!a5! /nologo /v:m /m:4 !a1!
)
:bp
if defined au (
echo Running Tests ... "!au!"
call :bx bj
"!bj!" /nologo /v:m /m:4 "!au!"
exit/B 0
)
if defined as (
(copy /B/Y "!wPkgPath!\\DllExport.bat" "!al!" > nul) && ( echo Manager has been updated. & exit/B !av! ) || ( echo -mgr-up failed. & exit/B %ERRORLEVEL% )
)
exit/B !av!
:bx
call :bt "Searching from .NET Framework - .NET 4.0, ..."
for %%v in (4.0, 3.5, 2.0) do (
call :by %%v Y & if defined Y (
set %1=!Y!
exit/B 0
)
)
call :bt "msb -netfx: not found"
set "%1="
exit/B 2
:by
call :bt "check %1"
for /F "usebackq tokens=2* skip=2" %%a in (
`reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\%1" /v MSBuildToolsPath 2^> nul`
) do if exist %%b (
set a6=%%~b
call :bt ":msbfound " a6
call :bz a6 bk
set %2=!bk!
exit/B 0
)
set "%2="
exit/B 0
:bz
set %2=!%~1!\MSBuild.exe
exit/B 0
:bt
if defined am (
set a7=%1
set a7=!a7:~0,-1!
set a7=!a7:~1!
echo.[%TIME% ] !a7! !%2! !%3!
)
exit/B 0
:bv
call :b0 %1
call :b1 %1
exit/B 0
:b0
call :b2 %1 "-=1"
exit/B 0
:b1
call :b2 %1 "+=1"
exit/B 0
:b2
set a8=z!%1!z
if "%~2"=="-=1" (set "a9=1") else (set "a9=")
if defined a9 (
set /a "i=-2"
) else (
set /a "i=1"
)
:b3
if "!a8:~%i%,1!"==" " (
set /a "i%~2"
goto b3
)
if defined a9 set /a "i+=1"
if defined a9 (
set "%1=!a8:~1,%i%!"
) else (
set "%1=!a8:~%i%,-1!"
)
exit/B 0
:bn
set "a_=%~1"
set /a aw=-1
:b4
set /a aw+=1
set %a_%[!aw!]=%~2
shift & if not "%~3"=="" goto b4
set /a aw-=1
set %1=!aw!
exit/B 0
:bs
set %2=!%1!
exit/B 0
:bu
setlocal disableDelayedExpansion
@echo off
:: GetNuTool - Executable version
:: Copyright (c) 2015-2018  Denis Kuzmin [ entry.reg@gmail.com ]
:: https://github.com/3F/GetNuTool
set ba=gnt.core
set bb="%temp%\%random%%random%%ba%"
if "%~1"=="-unpack" goto b5
set bc=%*
if defined __p_call if defined bc set bc=%bc:^^=^%
set bd=%__p_msb%
if defined bd goto b6
if "%~1"=="-msbuild" goto b7
for %%v in (4.0, 14.0, 12.0, 3.5, 2.0) do (
for /F "usebackq tokens=2* skip=2" %%a in (
`reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\%%v" /v MSBuildToolsPath 2^> nul`
) do if exist %%b (
set bd="%%~b\MSBuild.exe"
goto b6
)
)
echo MSBuild was not found. Try -msbuild "fullpath" args 1>&2
exit/B 2
:b7
shift
set bd=%1
shift
set be=%bc:!= #__b_ECL## %
setlocal enableDelayedExpansion
set be=!be:%%=%%%%!
:b8
for /F "tokens=1* delims==" %%a in ("!be!") do (
if "%%~b"=="" (
call :b9 !be!
exit/B %ERRORLEVEL%
)
set be=%%a #__b_EQ## %%b
)
goto b8
:b9
shift & shift
set "bc="
:b_
set bc=!bc! %1
shift & if not "%~2"=="" goto b_
set bc=!bc: #__b_EQ## ==!
setlocal disableDelayedExpansion
set bc=%bc: #__b_ECL## =!%
:b6
call :ca
%bd% %bb% /nologo /p:wpath="%~dp0/" /v:m /m:4 %bc%
set "bd="
set bf=%ERRORLEVEL%
del /Q/F %bb%
exit/B %bf%
:b5
set bb="%~dp0\%ba%"
echo Generating minified version in %bb% ...
:ca
<nul set /P ="">%bb%
set a=PropertyGroup&set b=Condition&set c=ngpackages&set d=Target&set e=DependsOnTargets&set f=TaskCoreDllPath&set g=MSBuildToolsPath&set h=UsingTask&set i=CodeTaskFactory&set j=ParameterGroup&set k=Reference&set l=Include&set m=System&set n=Using&set o=Namespace&set p=IsNullOrEmpty&set q=return&set r=string&set s=delegate&set t=foreach&set u=WriteLine&set v=Combine&set w=Console.WriteLine&set x=Directory&set y=GetNuTool&set z=StringComparison&set _=EXT_NUSPEC
<nul set /P =^<!-- GetNuTool - github.com/3F/GetNuTool --^>^<!-- Copyright (c) 2015-2018  Denis Kuzmin [ entry.reg@gmail.com ] --^>^<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003"^>^<%a%^>^<ngconfig %b%="'$(ngconfig)'==''"^>packages.config^</ngconfig^>^<ngserver %b%="'$(ngserver)'==''"^>https://www.nuget.org/api/v2/package/^</ngserver^>^<%c% %b%="'$(%c%)'==''"^>^</%c%^>^<ngpath %b%="'$(ngpath)'==''"^>packages^</ngpath^>^</%a%^>^<%d% Name="get" BeforeTargets="Build" %e%="header"^>^<a^>^<Output PropertyName="plist" TaskParameter="Result"/^>^</a^>^<b plist="$(plist)"/^>^</%d%^>^<%d% Name="pack" %e%="header"^>^<c/^>^</%d%^>^<%a%^>^<%f% %b%="Exists('$(%g%)\Microsoft.Build.Tasks.v$(MSBuildToolsVersion).dll')"^>$(%g%)\Microsoft.Build.Tasks.v$(MSBuildToolsVersion).dll^</%f%^>^<%f% %b%="'$(%f%)'=='' and Exists('$(%g%)\Microsoft.Build.Tasks.Core.dll')"^>$(%g%)\Microsoft.Build.Tasks.Core.dll^</%f%^>^</%a%^>^<%h% TaskName="a" TaskFactory="%i%" AssemblyFile="$(%f%)"^>^<%j%^>^<Result Output="true"/^>^</%j%^>^<Task^>^<%k% %l%="%m%.Xml"/^>^<%k% %l%="%m%.Xml.Linq"/^>^<%n% %o%="%m%"/^>^<%n% %o%="%m%.Collections.Generic"/^>^<%n% %o%="%m%.IO"/^>^<%n% %o%="%m%.Xml.Linq"/^>^<Code Type="Fragment" Language="cs"^>^<![CDATA[var a=@"$(ngconfig)";var b=@"$(%c%)";var c=@"$(wpath)";if(!String.%p%(b)){Result=b;%q% true;}var d=Console.Error;Action^<%r%,Queue^<%r%^>^>e=%s%(%r% f,Queue^<%r%^>g){%t%(var h in XDocument.Load(f).Descendants("package")){var i=h.Attribute("id");var j=h.Attribute("version");var k=h.Attribute("output");if(i==null){d.%u%("'id' does not exist in '{0}'",f);%q%;}var l=i.Value;if(j!=null){l+="/"+j.Value;}if(k!=null){g.Enqueue(l+":"+k.Value);continue;}g.Enqueue(l);}};var m=new Queue^<%r%^>();%t%(var f in a.Split(new char[]{a.IndexOf('^|')!=-1?'^|':';'},(StringSplitOptions)1)){>>%bb%
<nul set /P =var n=Path.%v%(c,f);if(File.Exists(n)){e(n,m);}else{d.%u%(".config '{0}' was not found.",n);}}if(m.Count^<1){d.%u%("Empty list. Use .config or /p:%c%=\"...\"\n");}else{Result=%r%.Join("|",m.ToArray());}]]^>^</Code^>^</Task^>^</%h%^>^<%h% TaskName="b" TaskFactory="%i%" AssemblyFile="$(%f%)"^>^<%j%^>^<plist/^>^</%j%^>^<Task^>^<%k% %l%="WindowsBase"/^>^<%n% %o%="%m%"/^>^<%n% %o%="%m%.IO"/^>^<%n% %o%="%m%.IO.Packaging"/^>^<%n% %o%="%m%.Net"/^>^<Code Type="Fragment" Language="cs"^>^<![CDATA[var a=@"$(ngserver)";var b=@"$(wpath)";var c=@"$(ngpath)";var d=@"$(proxycfg)";var e=@"$(debug)"=="true";if(plist==null){%q% false;}ServicePointManager.SecurityProtocol^|=SecurityProtocolType.Tls11^|SecurityProtocolType.Tls12;var f=new %r%[]{"/_rels/","/package/","/[Content_Types].xml"};Action^<%r%,object^>g=%s%(%r% h,object i){if(e){%w%(h,i);}};Func^<%r%,WebProxy^>j=%s%(%r% k){var l=k.Split('@');if(l.Length^<=1){%q% new WebProxy(l[0],false);}var m=l[0].Split(':');%q% new WebProxy(l[1],false){Credentials=new NetworkCredential(m[0],(m.Length^>1)?m[1]:null)};};Func^<%r%,%r%^>n=%s%(%r% i){%q% Path.%v%(b,i??"");};Action^<%r%,%r%,%r%^>o=%s%(%r% p,%r% q,%r% r){var s=Path.GetFullPath(n(r??q));if(%x%.Exists(s)){%w%("`{0}` is already exists: \"{1}\"",q,s);%q%;}Console.Write("Getting `{0}` ... ",p);var t=Path.%v%(Path.GetTempPath(),Guid.NewGuid().ToString());using(var u=new WebClient()){try{if(!String.%p%(d)){u.Proxy=j(d);}u.Headers.Add("User-Agent","%y% $(%y%)");u.UseDefaultCredentials=true;u.DownloadFile(a+p,t);}catch(Exception v){Console.Error.%u%(v.Message);%q%;}}%w%("Extracting into \"{0}\"",s);using(var w=ZipPackage.Open(t,FileMode.Open,FileAccess.Read)){%t%(var x in w.GetParts()){var y=Uri.UnescapeDataString(x.Uri.OriginalString);if(f.Any(z=^>y.StartsWith(z,%z%.Ordinal))){continue;}var _=Path.%v%(s,y.TrimStart(>>%bb%
<nul set /P ='/'));g("- `{0}`",y);var aa=Path.GetDirectoryName(_);if(!%x%.Exists(aa)){%x%.CreateDirectory(aa);}using(Stream ab=x.GetStream(FileMode.Open,FileAccess.Read))using(var ac=File.OpenWrite(_)){try{ab.CopyTo(ac);}catch(FileFormatException v){g("[x]?crc: {0}",_);}}}}File.Delete(t);};%t%(var w in plist.Split(new char[]{plist.IndexOf('^|')!=-1?'^|':';'},(StringSplitOptions)1)){var ad=w.Split(new char[]{':'},2);var p=ad[0];var r=(ad.Length^>1)?ad[1]:null;var q=p.Replace('/','.');if(!String.%p%(c)){r=Path.%v%(c,r??q);}o(p,q,r);}]]^>^</Code^>^</Task^>^</%h%^>^<%h% TaskName="c" TaskFactory="%i%" AssemblyFile="$(%f%)"^>^<Task^>^<%k% %l%="%m%.Xml"/^>^<%k% %l%="%m%.Xml.Linq"/^>^<%k% %l%="WindowsBase"/^>^<%n% %o%="%m%"/^>^<%n% %o%="%m%.Collections.Generic"/^>^<%n% %o%="%m%.IO"/^>^<%n% %o%="%m%.Linq"/^>^<%n% %o%="%m%.IO.Packaging"/^>^<%n% %o%="%m%.Xml.Linq"/^>^<%n% %o%="%m%.Text.RegularExpressions"/^>^<Code Type="Fragment" Language="cs"^>^<![CDATA[var a=@"$(ngin)";var b=@"$(ngout)";var c=@"$(wpath)";var d=@"$(debug)"=="true";var %_%=".nuspec";var EXT_NUPKG=".nupkg";var TAG_META="metadata";var DEF_CONTENT_TYPE="application/octet";var MANIFEST_URL="http://schemas.microsoft.com/packaging/2010/07/manifest";var ID="id";var VER="version";Action^<%r%,object^>e=%s%(%r% f,object g){if(d){%w%(f,g);}};var h=Console.Error;a=Path.%v%(c,a);if(!%x%.Exists(a)){h.%u%("`{0}` was not found.",a);%q% false;}b=Path.%v%(c,b);var i=%x%.GetFiles(a,"*"+%_%,SearchOption.TopDirectoryOnly).FirstOrDefault();if(i==null){h.%u%("{0} was not found in `{1}`",%_%,a);%q% false;}%w%("Found {0}: `{1}`",%_%,i);var j=XDocument.Load(i).Root.Elements().FirstOrDefault(k=^>k.Name.LocalName==TAG_META);if(j==null){h.%u%("{0} does not contain {1}.",i,TAG_META);%q% false;}var l=new Dictionary^<%r%,%r%^>();%t%(var m in j.Elements()){l[m.Name.LocalName.ToL>>%bb%
<nul set /P =ower()]=m.Value;}if(l[ID].Length^>100^|^|!Regex.IsMatch(l[ID],@"^\w+([_.-]\w+)*$",RegexOptions.IgnoreCase^|RegexOptions.ExplicitCapture)){h.%u%("The format of `{0}` is not correct.",ID);%q% false;}var n=new %r%[]{Path.%v%(a,"_rels"),Path.%v%(a,"package"),Path.%v%(a,"[Content_Types].xml")};var o=%r%.Format("{0}.{1}{2}",l[ID],l[VER],EXT_NUPKG);if(!String.IsNullOrWhiteSpace(b)){if(!%x%.Exists(b)){%x%.CreateDirectory(b);}o=Path.%v%(b,o);}%w%("Started packing `{0}` ...",o);using(var p=Package.Open(o,FileMode.Create)){Uri q=new Uri(String.Format("/{0}{1}",l[ID],%_%),UriKind.Relative);p.CreateRelationship(q,TargetMode.Internal,MANIFEST_URL);%t%(var r in %x%.GetFiles(a,"*.*",SearchOption.AllDirectories)){if(n.Any(k=^>r.StartsWith(k,%z%.Ordinal))){continue;}%r% s;if(r.StartsWith(a,%z%.OrdinalIgnoreCase)){s=r.Substring(a.Length).TrimStart(Path.DirectorySeparatorChar);}else{s=r;}e("- `{0}`",s);var t=%r%.Join("/",s.Split('\\','/').Select(g=^>Uri.EscapeDataString(g)));Uri u=PackUriHelper.CreatePartUri(new Uri(t,UriKind.Relative));var v=p.CreatePart(u,DEF_CONTENT_TYPE,CompressionOption.Maximum);using(Stream w=v.GetStream())using(var x=new FileStream(r,FileMode.Open,FileAccess.Read)){x.CopyTo(w);}}Func^<%r%,%r%^>y=%s%(%r% z){%q%(l.ContainsKey(z))?l[z]:"";};var _=p.PackageProperties;_.Creator=y("authors");_.Description=y("description");_.Identifier=l[ID];_.Version=l[VER];_.Keywords=y("tags");_.Title=y("title");_.LastModifiedBy="%y% $(%y%)";}]]^>^</Code^>^</Task^>^</%h%^>^<%d% Name="Build" %e%="get"/^>^<%a%^>^<%y%^>1.7.0.29271_4bc1dfb^</%y%^>^<wpath %b%="'$(wpath)'==''"^>$(MSBuildProjectDirectory)^</wpath^>^</%a%^>^<%d% Name="header"^>^<Message Text="%%0D%%0A%y% $(%y%) - github.com/3F%%0D%%0A=========%%0D%%0A" Importance="high"/^>^</%d%^>^</Project^>>>%bb%
exit/B 0
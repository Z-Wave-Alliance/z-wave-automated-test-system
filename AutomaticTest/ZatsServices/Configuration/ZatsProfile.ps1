# SPDX-License-Identifier: BSD-3-Clause
# SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
&("C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\xsd.exe") ZatsProfile.xsd /c /l:CS /edb /n:Zats.Services
$file = gci "ZatsProfile.cs"
foreach ($str in $file) 
{
Start-Sleep -s 1
$cont = get-content -path $str
$cont | foreach {$_ -replace "using System.Xml.Serialization;", "using System.Xml.Serialization;
    using System.Collections.ObjectModel;"} | set-content $str

	Start-Sleep -s 1
$cont = get-content -path $str
$cont | foreach {$_ -replace "\[System.Diagnostics.DebuggerStepThroughAttribute\(\)\]", ""} | set-content $str

Start-Sleep -s 1
$cont = get-content -path $str
$cont | foreach {$_ -replace "/// <remarks/>", ""} | set-content $str

Start-Sleep -s 1
$cont = get-content -path $str
$cont | foreach {$_ -replace "\[System.Xml.Serialization.", "["} | set-content $str

Start-Sleep -s 1
$cont = get-content -path $str
$cont | foreach {$_ -replace "Field", ""} | set-content $str

Start-Sleep -s 1
$cont = get-content -path $str
$cont | foreach {$_ -replace "this\.", ""} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "ProgrammingResult\[\]", "Collection<ProgrammingResult>"} | set-content $str

}
write-host "After `n"
$file
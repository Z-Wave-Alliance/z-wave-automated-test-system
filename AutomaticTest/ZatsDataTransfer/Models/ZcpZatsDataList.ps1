# SPDX-License-Identifier: BSD-3-Clause
# SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
&("C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\xsd.exe") ZcpZatsDataList.xsd /c /l:CS /edb /n:Zats.Services

$file = gci "ZcpZatsDataList.cs"
foreach ($str in $file) 
{
$cont = get-content -path $str
$cont | foreach {$_ -replace "using System.Xml.Serialization;", "using System.Xml.Serialization;
    using System.Collections.Generic;"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "\[System.Diagnostics.DebuggerStepThroughAttribute\(\)\]", ""} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "ZcpZatsData\[\]", "List<ZcpZatsData>"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "ZatsValue\[\]", "List<ZatsValue>"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "string\[\]", "List<string>"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "int\[\]", "List<int>"} | set-content $str

}

# comment the following line in to prevent consol window is closed after script is finished
#[void][System.Console]::ReadKey($true)
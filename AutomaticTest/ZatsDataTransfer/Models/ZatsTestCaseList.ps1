# SPDX-License-Identifier: BSD-3-Clause
# SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
&("C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\xsd.exe") ZatsTestCaseList.xsd /c /l:CS /edb /n:Zats.Services

$file = gci "ZatsTestCaseList.cs"
foreach ($str in $file)
{
$cont = get-content -path $str
$cont | foreach {$_ -replace "using System.Xml.Serialization;", "using System.Xml.Serialization;
    using System.Collections.Generic;"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "\[System.Diagnostics.DebuggerStepThroughAttribute\(\)\]", ""} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "ZatsTestCaseTriggerDef\[\]", "List<ZatsTestCaseTriggerDef>"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "TriggerValueDef\[\]", "List<TriggerValueDef>"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "ZatsTestCase\[\]", "List<ZatsTestCase>"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "ZatsTestCaseFilter\[\]", "List<ZatsTestCaseFilter>"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "ZatsTestCaseTriggerRef\[\]", "List<ZatsTestCaseTriggerRef>"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "string\[\]", "List<string>"} | set-content $str

}

# comment the following line in to prevent consol window is closed after script is finished
[void][System.Console]::ReadKey($true)
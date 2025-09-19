# SPDX-License-Identifier: BSD-3-Clause
# SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
# SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
&("C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\xsd.exe") ZatsDefinition.xsd /c /l:CS /edb /n:Zats.Services

$content = get-content .\ZatsDefinition.cs
$content `
| where-object {$_ -notmatch '//'} `
| where-object {$_ -notmatch 'System.Diagnostics.DebuggerStepThroughAttribute'} `
| where-object {$_ -notmatch 'System.ComponentModel.DesignerCategoryAttribute'} `
| where-object {$_ -notmatch 'System.CodeDom.Compiler.GeneratedCodeAttribute'} `
| set-content ZatsDefinition.cs

$file = gci "ZatsDefinition.cs"

foreach ($str in $file) 
{
$cont = get-content -path $str
$cont | foreach {$_ -replace "using System.Xml.Serialization;", "using System.Xml.Serialization;
    using System.Collections.ObjectModel;
    using System.ComponentModel;"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "System.Xml.Serialization\.", ""} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "System.ComponentModel\.", ""} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "Attribute\(\)", ""} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "Attribute\(", "("} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "Field", ""} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "this\.", ""} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "DeviceHost\[\]", "ObservableCollection<DeviceHost>"} | set-content $str


$cont = get-content -path $str
$cont | foreach {$_ -replace "Profile\[\]", "ObservableCollection<Profile>"} | set-content $str


$cont = get-content -path $str
$cont | foreach {$_ -replace "DevKitPath\[\]", "ObservableCollection<DevKitPath>"} | set-content $str


$cont = get-content -path $str
$cont | foreach {$_ -replace "DevKitPack\[\]", "ObservableCollection<DevKitPack>"} | set-content $str
}

Write-Host -NoNewLine 'Press any key to continue...';
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');

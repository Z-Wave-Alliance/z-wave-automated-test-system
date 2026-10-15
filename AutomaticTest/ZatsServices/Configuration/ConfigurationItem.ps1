# SPDX-License-Identifier: BSD-3-Clause
# SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
&("C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\xsd.exe") ConfigurationItem.xsd /c /l:CS /nologo /n:Zats.Services.Configuration 
$file = gci "ConfigurationItem.cs"
foreach ($str in $file) 
{
$cont = get-content -path $str
$cont | foreach {$_ -replace "using System.Xml.Serialization;", "using System;`r`n    using System.Xml.Serialization;`r`n    using System.Collections.ObjectModel;`r`n    using ZWave.Configuration;"} | set-content $str

$cont = get-content -path $str
$cont | where {$_ -notmatch "System.ComponentModel.DesignerCategoryAttribute"} | set-content $str

$cont = get-content -path $str
$cont | where {$_ -notmatch "System.Diagnostics.DebuggerStepThroughAttribute"} | set-content $str

$cont = get-content -path $str
$cont | where {$_ -notmatch "System.CodeDom.Compiler.GeneratedCodeAttribute"} | set-content $str

$cont = get-content -path $str
$cont | where {$_ -notmatch "/// <remarks/>"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "\[System.Xml.Serialization.", "["} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "\[System.ComponentModel.", "["} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "\[System.SerializableAttribute\(\)\]", "[Serializable]"} | set-content $str

#-----------------------------------------------------------------------------------------------

$cont = get-content -path $str
$cont | foreach {$_ -replace "private string\[\] node", "private Node[] node"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "public string\[\] Node", "public Node[] Node"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "private string\[\] networkKey", "private NetworkKey[] networkKey"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "public string\[\] NetworkKey", "public NetworkKey[] NetworkKey"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "Node\[\]", "Collection<Node>"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "NetworkKey\[\]", "Collection<NetworkKey>"} | set-content $str
}

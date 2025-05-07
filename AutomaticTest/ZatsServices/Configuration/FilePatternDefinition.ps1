&("C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\xsd.exe") FilePatternDefinition.xsd /c /l:CS /edb /n:Zats.Services

$content = get-content .\FilePatternDefinition.cs
$content `
| where-object {$_ -notmatch '//'} `
| where-object {$_ -notmatch 'System.Diagnostics.DebuggerStepThroughAttribute'} `
| where-object {$_ -notmatch 'System.ComponentModel.DesignerCategoryAttribute'} `
| where-object {$_ -notmatch 'System.CodeDom.Compiler.GeneratedCodeAttribute'} `
| set-content FilePatternDefinition.cs

$file = gci "FilePatternDefinition.cs"

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
$cont | foreach {$_ -replace "BinaryPattern\[\]", "ObservableCollection<BinaryPattern>"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "LibToken\[\]", "ObservableCollection<LibToken>"} | set-content $str


$cont = get-content -path $str
$cont | foreach {$_ -replace "string\[\]", "ObservableCollection<string>"} | set-content $str

}


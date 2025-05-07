&("C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\xsd.exe") LogBuilder.xsd /c /l:CS /edb /n:ZatsTestConsole

$file = gci "LogBuilder.cs"
foreach ($str in $file) 
{
$cont = get-content -path $str
$cont | foreach {$_ -replace "using System.Xml.Serialization;", "using System.Xml.Serialization;
    using System.Collections.ObjectModel;"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "\[System.Diagnostics.DebuggerStepThroughAttribute\(\)\]", ""} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "testsuiteTestcase\[\]", "Collection<testsuiteTestcase>"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "testsuitesTestsuite\[\]", "Collection<testsuitesTestsuite>"} | set-content $str

$cont = get-content -path $str
$cont | foreach {$_ -replace "testsuiteProperty\[\]", "Collection<testsuiteProperty>"} | set-content $str




}

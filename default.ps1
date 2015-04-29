properties {
    $projectName            = "Cedar.Domain"
    $buildNumber            = 0
    $rootDir                = Resolve-Path .\
    $buildOutputDir         = "$rootDir\build"
    $mergedDir              = "$buildOutputDir\merged"
    $reportsDir             = "$buildOutputDir\reports"
    $srcDir                 = "$rootDir\src"
    $packagesDir            = "$srcDir\packages"
    $solutionFilePath       = "$srcDir\$projectName.sln"
    $assemblyInfoFilePath   = "$srcDir\SharedAssemblyInfo.cs"
    $ilmergePath            = FindTool "ILMerge.*\tools\ilmerge.exe" "$packagesDir"
    $xunitRunner            = FindTool "xunit.runner.console.*\tools\xunit.console.exe" "$packagesDir"
    $nugetPath              = "$srcDir\.nuget\nuget.exe"
}

task default -depends Clean, UpdateVersion, CreateNuGetPackages, RunTests

task Clean {
    Remove-Item $buildOutputDir -Force -Recurse -ErrorAction SilentlyContinue
    exec { msbuild /nologo /verbosity:quiet $solutionFilePath /t:Clean /p:platform="Any CPU"}
}

task RestoreNuget {
    Get-PackageConfigs |% {
        "Restoring " + $_
        &$nugetPath install $_ -o "$srcDir\packages" -configfile $_
    }
}

task UpdateVersion {
    $version = Get-Version $assemblyInfoFilePath
    $oldVersion = New-Object Version $version
    $newVersion = New-Object Version ($oldVersion.Major, $oldVersion.Minor, $oldVersion.Build, $buildNumber)
    Update-Version $newVersion $assemblyInfoFilePath
}

task Compile -depends RestoreNuget{
    exec { msbuild /nologo /verbosity:quiet $solutionFilePath /p:Configuration=Release /p:platform="Any CPU"}
}

task RunTests -depends Compile {
    $testReportDir = "$reportsDir\tests\"
    $testReportDirXmlFilePath = "$testReportDir\tests.xml"
    EnsureDirectory $testReportDir

    .$xunitRunner "$srcDir\Cedar.Domain.Tests\bin\Release\Cedar.Domain.Tests.dll" -html "$testReportDir\index.html" -xml "$testReportDirXmlFilePath"

    # Pretty-print the xml
    [Reflection.Assembly]::LoadWithPartialName("System.Xml.Linq")
    [System.Xml.Linq.XDocument]::Load("$testReportDirXmlFilePath").Save("$testReportDirXmlFilePath")
}

task ILMerge -depends Compile {
    New-Item $mergedDir -Type Directory -ErrorAction SilentlyContinue

    $mainDllName = "Cedar.Domain.dll"
    $dllDir = "$srcDir\Cedar.Domain\bin\Release"
    $inputDlls = "$dllDir\$mainDllName"
    @(  "EnsureThat" ) |% { $inputDlls = "$inputDlls $dllDir\$_.dll" }
    Invoke-Expression "$ilmergePath /targetplatform:v4 /internalize /allowDup /target:library /log /out:$mergedDir\$mainDllName.dll $inputDlls"
}

task CreateNuGetPackages -depends ILMerge {
    $versionString = Get-Version $assemblyInfoFilePath
    $version = New-Object Version $versionString
    $packageVersion = $version.Major.ToString() + "." + $version.Minor.ToString() + "." + $version.Build.ToString() + "-build" + $buildNumber.ToString().PadLeft(5,'0')
    $packageVersion
    gci $srcDir -Recurse -Include *.nuspec | % {
        exec { .$srcDir\.nuget\nuget.exe pack $_ -o $buildOutputDir -version $packageVersion }
    }
}

function FindTool {
	param(
		[string]$name,
		[string]$packageDir
	)

	$result = Get-ChildItem "$packageDir\$name" | Select-Object -First 1

	return $result.FullName
}

function Get-PackageConfigs {
    $packages = gci $srcDir -Recurse "packages.config" -ea SilentlyContinue
    $customPachage = gci $srcDir -Recurse "packages.*.config" -ea SilentlyContinue
    $packages + $customPachage  | foreach-object { $_.FullName }
}

function EnsureDirectory {
    param($directory)

    if(!(test-path $directory))	{
        mkdir $directory
    }
}


function Get-Version
{
	param
	(
		[string]$assemblyInfoFilePath
	)
	Write-Host "path $assemblyInfoFilePath"
	$pattern = '(?<=^\[assembly\: AssemblyVersion\(\")(?<versionString>\d+\.\d+\.\d+\.\d+)(?=\"\))'
	$assmblyInfoContent = Get-Content $assemblyInfoFilePath
	return $assmblyInfoContent | Select-String -Pattern $pattern | Select -expand Matches |% {$_.Groups['versionString'].Value}
}

function Update-Version
{
	param
    (
		[string]$version,
		[string]$assemblyInfoFilePath
	)

	$newVersion = 'AssemblyVersion("' + $version + '")';
	$newFileVersion = 'AssemblyFileVersion("' + $version + '")';
	$tmpFile = $assemblyInfoFilePath + ".tmp"

	Get-Content $assemblyInfoFilePath |
		%{$_ -replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', $newFileVersion }  | Out-File -Encoding UTF8 $tmpFile

	Move-Item $tmpFile $assemblyInfoFilePath -force
}

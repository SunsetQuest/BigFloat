# Source for this file: Jeremy Thompson Dec 2021 on stackoverflow - https://stackoverflow.com/a/70321555/495455
#===============================================================================================

# PURPOSE: Automates the running of Unit Tests and Code Coverage

# If running outside the test folder
#cd E:\Dev\XYZ\src\XYZTestProject

# This only needs to be installed once (globally), if installed it fails silently: 
#dotnet tool install -g dotnet-reportgenerator-globaltool

# Save current directory into a variable
$dir = pwd

# Delete previous test run results (there's a bunch of sub-folders named with GUIDs)
Remove-Item -Recurse -Force $dir/TestResults/

# Run the Coverlet.Collector - REPLACING YOUR SOLUTION NAME!!!
$output = [string] (& dotnet test ../BigFloat.sln --collect:"XPlat Code Coverage" 2>&1)
Write-Host "Last Exit Code: $lastexitcode"
Write-Host $output

# Delete previous test run reports - note if you're getting wrong results do a Solution Clean and Rebuild to remove stale DLLs in the bin folder
Remove-Item -Recurse -Force $dir/coveragereport/

# To keep a history of the Code Coverage we need to use the argument: -historydir:SOME_DIRECTORY 
if (!(Test-Path -path $dir/CoverageHistory)) {  
 New-Item -ItemType directory -Path $dir/CoverageHistory
}

# Generate the Code Coverage HTML Report
$exePath = Join-Path $env:USERPROFILE '.nuget\packages\reportgenerator\5.2.0\tools\net7.0\reportgenerator.exe'
& $exePath -reports:"$dir/**/coverage.cobertura.xml" -targetdir:"$dir/coveragereport" -reporttypes:Html -historydir:$dir/CoverageHistory


# Open the Code Coverage HTML Report (if running on a WorkStation)
$osInfo = Get-CimInstance -ClassName Win32_OperatingSystem
if ($osInfo.ProductType -eq 1) {
(& "$dir/coveragereport/index.html")
}
. .\log.ps1
. .\xrculture.ps1

###################################################################################################
$logFile = Get-Date -UFormat "$PSScriptRoot\openMVG-openMVS-workflow-log-%Y-%m-%d_%H-%M-%S.txt"
New-Item -Path $logFile -ItemType "file"
$LogFile = $logFile


if (!(openMVG-openMVS-Run-Workflow -_inputPath "D:\Temp\xrculture\automation\Mouse")) {
	Exit -1
}

exit 0




###################################################################################################
# bag
if (!(openMVG-openMVS-Run-Workflow -_inputPath "D:\Temp\xrculture\automation\bag")) {
	Exit -1
}

###################################################################################################
# angel
if (!(openMVG-openMVS-Run-Workflow -_inputPath "D:\Temp\xrculture\automation\angel")) {
	Exit -1
}

###################################################################################################
# rabbit
if (!(openMVG-openMVS-Run-Workflow -_inputPath "D:\Temp\xrculture\automation\rabbit")) {
	Exit -1
}

###################################################################################################
# Industrial_A
<#if (!(openMVG-openMVS-Run-Workflow -_inputPath "D:\Temp\xrculture\automation\Industrial_A")) {
	Exit -1
}

###################################################################################################
# SceauxCastle
if (!(openMVG-openMVS-Run-Workflow -_inputPath "D:\Temp\xrculture\automation\SceauxCastle")) {
	Exit -1
}

###################################################################################################
# south-building
if (!(openMVG-openMVS-Run-Workflow -_inputPath "D:\Temp\xrculture\automation\south-building")) {
	Exit -1
}

###################################################################################################
# gerrard-hall
if (!(openMVG-openMVS-Run-Workflow -_inputPath "D:\Temp\xrculture\automation\gerrard-hall")) {
	Exit -1
}#>

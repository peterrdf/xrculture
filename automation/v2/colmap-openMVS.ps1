. .\log.ps1
. .\xrculture.ps1

###################################################################################################
$logFile = Get-Date -UFormat "$PSScriptRoot\colmap-openMVS-workflow-log-%Y-%m-%d_%H-%M-%S.txt"
New-Item -Path $logFile -ItemType "file"
$LogFile = $logFile

###################################################################################################
# bag
if (!(COLMAP-openMVS-Run-Workflow -_inputPath "D:\Temp\COLMAP\automation\bag")) {
	Exit -1
}

###################################################################################################
# angel
if (!(COLMAP-openMVS-Run-Workflow -_inputPath "D:\Temp\COLMAP\automation\angel")) {
	Exit -1
}

###################################################################################################
# rabbit
if (!(COLMAP-openMVS-Run-Workflow -_inputPath "D:\Temp\COLMAP\automation\rabbit")) {
	Exit -1
}

###################################################################################################
# Industrial_A
<#if (!(COLMAP-openMVS-Run-Workflow -_inputPath "D:\Temp\COLMAP\automation\Industrial_A")) {
	Exit -1
}

###################################################################################################
# SceauxCastle
if (!(COLMAP-openMVS-Run-Workflow -_inputPath "D:\Temp\COLMAP\automation\SceauxCastle")) {
	Exit -1
}

###################################################################################################
# south-building
if (!(COLMAP-openMVS-Run-Workflow -_inputPath "D:\Temp\COLMAP\automation\south-building")) {
	Exit -1
}

###################################################################################################
# gerrard-hall
if (!(COLMAP-openMVS-Run-Workflow -_inputPath "D:\Temp\COLMAP\automation\gerrard-hall")) {
	Exit -1
}#>

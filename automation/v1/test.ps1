. .\log.ps1
. .\colmap-openMVS.ps1

###################################################################################################
$logFile = Get-Date -UFormat "$PSScriptRoot\colmap-openMVS-workflow-log-%Y-%m-%d_%H-%M-%S.txt"
New-Item -Path $logFile -ItemType "file"
$LogFile = $logFile

###################################################################################################
if (!(COLMAP-openMVS-Cleanup)) {
	Exit -1
}

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
if (!(COLMAP-openMVS-Run-Workflow -_inputPath "D:\Temp\COLMAP\automation\Industrial_A")) {
	Exit -1
}

###################################################################################################
# south-building
<#if (!(COLMAP-openMVS-Run-Workflow -_inputPath "D:\Temp\COLMAP\automation\south-building")) {
	Exit -1
}

###################################################################################################
# gerrard-hall
if (!(COLMAP-openMVS-Run-Workflow -_inputPath "D:\Temp\COLMAP\automation\gerrard-hall")) {
	Exit -1
}#>

###################################################################################################
if (!(COLMAP-openMVS-Cleanup)) {
	Exit -1
}
$LogFile

function Show-Message {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$_message,

        [ValidateSet("Red", "Yellow", "Green")]
        [string]$_color = "Green"
    )
	
    Write-Host $_message -ForegroundColor $_color
}

function Log-Message {
    [CmdletBinding()]
    param (		
		[Parameter(Mandatory=$true)]
        [string]$_message,

        [Parameter(Mandatory=$true)]
        [string]$_color
    )
	
	$timeStamp = Get-Date -UFormat "%Y-%m-%d_%H-%M-%S"
	
	# Host 
    Show-Message -_message ($timeStamp + " " + $_message)-_color $_color
	
	# File
	If ($LogFile -ne $null) {
		Add-Content -Path $LogFile ($timeStamp + " " + $_message)
	}	
}

function Log-Info {
    [CmdletBinding()]
    param (		
		[Parameter(Mandatory=$true)]
        [string]$_message
    )
	
    Log-Message -_message "INFO: $_message" -_color "Green"
}

function Log-Warn {
    [CmdletBinding()]
    param (		
		[Parameter(Mandatory=$true)]
        [string]$_message
    )
	
    Log-Message -_message "WARN: $_message" -_color "Yellow"
}

function Log-Err {
    [CmdletBinding()]
    param (		
		[Parameter(Mandatory=$true)]
        [string]$_message
    )
	
    Log-Message -_message "ERR: $_message" -_color "Red"
}

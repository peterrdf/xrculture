. .\log.ps1
. .\settings.ps1

function COLMAP-Create-Sparse-Model {
	[CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$_inputPath = ""
    )
    try
	{
		Log-Info -_message "** COLMAP: START..."
				
		Log-Info -_message "*** Feature extractor..."
		$startDateTime = Get-Date
			& "$global:COLMAP_DIR\colmap.exe" feature_extractor --database_path $_inputPath\databse.db --image_path $_inputPath\images
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Log-Info -_message "*** Exhaustive Matcher..."
		$startDateTime = Get-Date
			& "$global:COLMAP_DIR\colmap.exe" exhaustive_matcher --database_path $_inputPath\databse.db
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		if (Test-Path "$_inputPath\sparse") {
			Remove-Item -Force -Recurse -Path "$_inputPath\sparse"
		}
		New-Item -ItemType Directory -Force -Path "$_inputPath\sparse"
		
		Log-Info -_message "*** Mapper..."
		$startDateTime = Get-Date
			& "$global:COLMAP_DIR\colmap.exe" mapper --database_path $_inputPath\databse.db --image_path $_inputPath\images --output_path $_inputPath\sparse
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Log-Info -_message "*** Image undistorter..."
		$startDateTime = Get-Date
			& "$global:COLMAP_DIR\colmap.exe" image_undistorter --image_path $_inputPath\images --input_path $_inputPath\sparse\0 --output_path $_inputPath\dense --output_type COLMAP --max_image_size 2000
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Log-Info -_message "** COLMAP: END."
		
		return $true
	}
	catch
	{
		Log-Err -_message "Exception: $_"
	}
	
	return $false
}

function openMVS-Create-OBJ {
	[CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$_inputPath = ""
    )
    try
	{
		Log-Info -_message "** openMVS: START..."
		
		Log-Info -_message "*** Import 3D reconstruction from COLMAP..."
		$startDateTime = Get-Date
			& "$global:openMVS_DIR\InterfaceColmap.exe" --input-file $_inputPath\dense --binary=1 --image-folder $_inputPath\images --output-file model.mvs
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Log-Info -_message "*** Densify Point Cloud..."
		$startDateTime = Get-Date
			& "$global:openMVS_DIR\DensifyPointCloud.exe" model.mvs
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Log-Info -_message "*** Reconstruct Mesh..."
		$startDateTime = Get-Date
			& "$global:openMVS_DIR\ReconstructMesh.exe" --archive-type 2 model_dense.mvs
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Log-Info -_message "*** Refine Mesh..."
		$startDateTime = Get-Date
			& "$global:openMVS_DIR\RefineMesh.exe" --resolution-level 1 model_dense_mesh.mvs
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		if (Test-Path "$_inputPath\obj") {
			Remove-Item -Force -Recurse -Path "$_inputPath\obj"
		}
		New-Item -ItemType Directory -Force -Path "$_inputPath\obj"
		
		Log-Info -_message "*** Texture Mesh..."
		$startDateTime = Get-Date
		$model = (Get-Item $_inputPath).Name
			& "$global:openMVS_DIR\TextureMesh.exe" --export-type=obj --output-file $_inputPath\obj\$model.obj model_dense_mesh_refine.mvs
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Remove-Item -Force -Path "$_inputPath\obj\$model.mvs"
		
		Log-Info -_message "*** OBJ2BIN..."
		$startDateTime = Get-Date
			& "./obj2bin.exe" -convert $_inputPath\obj $_inputPath\obj
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Log-Info -_message "*** BINZ..."
		$startDateTime = Get-Date
			& Get-ChildItem -Path "$_inputPath\obj\*.bin", "$_inputPath\obj\*.jpg" | Compress-Archive -DestinationPath "$_inputPath\obj\$model.binz" -Update
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Log-Info -_message "** openMVS: END."
		
		return $true
	}
	catch
	{
		Log-Err -_message "Exception: $_"
	}
	
	return $false
}

function COLMAP-openMVS-Run-Workflow {
	[CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$_inputPath = ""
    )
    try
	{
		Log-Info -_message "* COLMAP-openMVS Workflow: START..."
		Log-Info -_message "* Input: '$_inputPath'"
		$startDateTime = Get-Date
		
		if (!(COLMAP-Create-Sparse-Model -_inputPath $_inputPath)) {
			return $false
		}
		
		if (!(openMVS-Create-OBJ -_inputPath $_inputPath)) {
			return $false
		}		
		
		Copy-Item -Path ".\*.ply" -Destination $_inputPath -Force		
		
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "* Done in $duration"
		Log-Info -_message "* COLMAP-openMVS Workflow: END."
		
		if (!(COLMAP-openMVS-Cleanup)) {
			return $false
		}
		
		return $true
	}
	catch
	{
		Log-Err -_message "Exception: $_"
	}
	
	return $false
}

function COLMAP-openMVS-Cleanup {
	try
	{
		Remove-Item -Force  -Path ".\*.mvs"
		Remove-Item -Force  -Path ".\*.dmap"
		Remove-Item -Force  -Path ".\*.ply"
		Remove-Item -Force  -Path ".\*.log"
		
		return $true
	}
	catch
	{
		Log-Err -_message "Exception: $_"
	}
	
	return $false
}
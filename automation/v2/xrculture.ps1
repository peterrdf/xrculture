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
		
		if (!(COLMAP-openMVS-Cleanup)) {
			return $false
		}
		
		if (!(COLMAP-Create-Sparse-Model -_inputPath $_inputPath)) {
			return $false
		}
		
		Log-Info -_message "*** Import 3D reconstruction from COLMAP..."
		$startDateTime = Get-Date
			& "$global:openMVS_DIR\InterfaceColmap.exe" --input-file $_inputPath\dense --binary=1 --image-folder $_inputPath\images --output-file model.mvs
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
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

function openMVG-Create-SfM {
	[CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$_inputPath = ""
    )
    try
	{
		Log-Info -_message "** openMVG: START..."
				
		Log-Info -_message "*** Intrinsics analysis..."
		$startDateTime = Get-Date
			& "$global:openMVG_DIR\openMVG_main_SfMInit_ImageListing.exe" --imageDirectory $_inputPath\images --outputDirectory $_inputPath\matches -f 1920 #--sensorWidthDatabase $global:openMVG_DIR\exif\sensor_width_database\sensor_width_camera_database.txt
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Log-Info -_message "*** Compute features..."
		$startDateTime = Get-Date
			& "$global:openMVG_DIR\openMVG_main_ComputeFeatures.exe" --input_file $_inputPath\matches\sfm_data.json --outdir $_inputPath\matches --describerMethod "SIFT" --describerPreset "ULTRA"
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Log-Info -_message "*** Compute matching pairs..."
		$startDateTime = Get-Date
			& "$global:openMVG_DIR\openMVG_main_PairGenerator.exe" --input_file $_inputPath\matches\sfm_data.json --output_file $_inputPath\matches\pairs.bin
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Log-Info -_message "*** Compute matches..."
		$startDateTime = Get-Date
			& "$global:openMVG_DIR\openMVG_main_ComputeMatches.exe" --input_file $_inputPath\matches\sfm_data.json --pair_list $_inputPath\matches\pairs.bin --output_file $_inputPath\matches\matches.putative.bin
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Log-Info -_message "*** Filter matches: INCREMENTAL..."
		$startDateTime = Get-Date
			& "$global:openMVG_DIR\openMVG_main_GeometricFilter.exe" --input_file $_inputPath\matches\sfm_data.json --matches $_inputPath\matches\matches.putative.bin -g f --output_file $_inputPath\matches\matches.f.bin
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		<#
		Log-Info -_message "*** Filter matches: GLOBAL..."
		$startDateTime = Get-Date
			& "$global:openMVG_DIR\openMVG_main_GeometricFilter.exe" --input_file $_inputPath\matches\sfm_data.json --matches $_inputPath\matches.putative.bin -g e --output_file $_inputPath\matches\matches.e.bin
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		#>
		
		Log-Info -_message "*** Sequential/Incremental reconstruction..."
		$startDateTime = Get-Date
			& "$global:openMVG_DIR\openMVG_main_SfM.exe" --sfm_engine "INCREMENTAL" --input_file $_inputPath\matches\sfm_data.json --match_dir $_inputPath\matches --output_dir $_inputPath\reconstruction
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		<#
		Log-Info -_message "*** Global reconstruction..."
		$startDateTime = Get-Date
			& "$global:openMVG_DIR\openMVG_main_SfM.exe" --sfm_engine "GLOBAL" --input_file $_inputPath\matches\sfm_data.json --match_file $_inputPath\matches.e.bin --output_dir $_inputPath\reconstruction
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		#>
		
		Log-Info -_message "*** Colorize Structure..."
		$startDateTime = Get-Date
			& "$global:openMVG_DIR\openMVG_main_ComputeSfM_DataColor.exe" --input_file $_inputPath\reconstruction\sfm_data.bin --output_file $_inputPath\reconstruction\colorized.ply
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		Log-Info -_message "** openMVG: END."
		
		return $true
	}
	catch
	{
		Log-Err -_message "Exception: $_"
	}
	
	return $false
}

function openMVG-openMVS-Run-Workflow {
	[CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$_inputPath = ""
    )
    try
	{
		Log-Info -_message "* openMVG-openMVS Workflow: START..."
		Log-Info -_message "* Input: '$_inputPath'"
		$startDateTime = Get-Date
		
		if (!(openMVG-Create-SfM -_inputPath $_inputPath)) {
			return $false
		}
		
		Log-Info -_message "*** Import 3D reconstruction from openMVG..."
		$startDateTime = Get-Date
			& "$global:openMVG_DIR\openMVG_main_openMVG2openMVS.exe" --sfmdata $_inputPath\reconstruction\sfm_data.bin --outfile model.mvs --outdir $_inputPath\undistored
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "*** Done in $duration"
		
		if (!(openMVS-Create-OBJ -_inputPath $_inputPath)) {
			return $false
		}		
		
		Copy-Item -Path ".\*.ply" -Destination $_inputPath -Force		
		
		$endDateTime = Get-Date
		$duration = New-TimeSpan -Start $startDateTime -End $endDateTime
		Log-Info -_message "* Done in $duration"
		Log-Info -_message "* openMVG-openMVS Workflow: END."
		
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
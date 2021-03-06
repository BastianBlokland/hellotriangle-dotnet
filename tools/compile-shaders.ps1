[cmdletbinding()]
param
(
    [Parameter(Mandatory=$true)][string]$SHADER_SOURCE_DIR, 
    [Parameter(Mandatory=$true)][string]$SHADER_OUTPUT_DIR
)

#Compiles all shaders in a given input dir to spir-v using the glslc compiler 
#and puts the results in the given output dir.

#Check if 'glslc' is installed
where.exe /Q glslc
if ($LASTEXITCODE -gt 0)
{
    Write-Output "Error: 'glslc' is not installed"
    exit 1
}

#Create the output directory if it does not exist
New-Item -ItemType Directory -Force -Path $SHADER_OUTPUT_DIR
#Clear output directory
Remove-Item "$SHADER_OUTPUT_DIR/*.spv"

#Make the output paths absolute
$SHADER_SOURCE_DIR=$(Resolve-Path $SHADER_SOURCE_DIR)
$SHADER_OUTPUT_DIR=$(Resolve-Path $SHADER_OUTPUT_DIR)

Write-Output "Begin shader compiliation (input: '$SHADER_SOURCE_DIR', output: '$SHADER_OUTPUT_DIR')"

$files = Get-ChildItem "$SHADER_SOURCE_DIR"
for ($i=0; $i -lt $files.Count; $i++)
{
    $file=$files[$i]
    if (!$file.Name.StartsWith("include_"))
    {
        Write-Output "Compiling $($file.Name)"
        & glslc -o "$SHADER_OUTPUT_DIR/$($file.Name).spv" "$($file.FullName)"
    }
}

Write-Output "Finished compiling shaders"
exit 0
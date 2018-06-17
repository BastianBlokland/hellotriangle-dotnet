#!/bin/bash
set -e

#Compiles all shaders in a given input dir to spir-v using the glslangValidator 
#and puts the results in the given output dir.

SHADER_SOURCE_DIR="${1%/}" #Note: %/ removes the trailing slash if there is one
SHADER_OUTPUT_DIR="${2%/}"

#Check that inputs are provided and that 'glslangValidator' is installed
if [[ -z $SHADER_SOURCE_DIR ]]; then
    echo "Error: No source-dir provided in argument 1";
    exit 1
fi
if [[ -z $SHADER_OUTPUT_DIR ]]; then 
    echo "Error: No output-dir provided in argument 2";
    exit 1
fi
if [[ ! -x "$(command -v glslangValidator)" ]]; then
    echo "Error: 'glslangValidator' is not installed"
    exit 1
fi

#If the given paths are relative then make them absolute
if [[ ! "$SHADER_SOURCE_DIR" = /* ]]; then
    SHADER_SOURCE_DIR="$(pwd)/$SHADER_SOURCE_DIR"
fi
if [[ ! "$SHADER_OUTPUT_DIR" = /* ]]; then
    SHADER_OUTPUT_DIR="$(pwd)/$SHADER_OUTPUT_DIR"
fi

echo "Begin shader compiliation (input: '$SHADER_SOURCE_DIR', output: '$SHADER_OUTPUT_DIR')"

#Clear output directory
rm -f "$SHADER_OUTPUT_DIR"/*.spv

#Compile each file in the input directory
for filePath in $SHADER_SOURCE_DIR/*; do
    fileName="$(basename $filePath)"
    echo "Compiling: $fileName"
    glslangValidator -V "$filePath" -o "$SHADER_OUTPUT_DIR/$fileName.spv"
done

echo "Finished compiling shaders"
exit 0
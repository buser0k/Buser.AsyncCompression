#!/bin/bash

# Bash script to run Cake build
TARGET="Default"
CONFIGURATION="Release"
VERBOSITY="Verbose"
EXPERIMENTAL=""
WHATIF=""

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -t|--target)
            TARGET="$2"
            shift 2
            ;;
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -v|--verbosity)
            VERBOSITY="$2"
            shift 2
            ;;
        -e|--experimental)
            EXPERIMENTAL="--experimental"
            shift
            ;;
        -w|--whatif)
            WHATIF="--dryrun"
            shift
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "dotnet CLI is not installed. Please install .NET SDK."
    exit 1
fi

# Install Cake if not already installed
if ! dotnet tool list --global | grep -q "Cake.Tool"; then
    echo "Installing Cake.Tool..."
    dotnet tool install --global Cake.Tool --version 3.0.0
fi

# Build arguments
CAKE_ARGS=(
    "cake"
    "build.cake"
    "--target=$TARGET"
    "--configuration=$CONFIGURATION"
    "--verbosity=$VERBOSITY"
)

if [ -n "$EXPERIMENTAL" ]; then
    CAKE_ARGS+=("$EXPERIMENTAL")
fi

if [ -n "$WHATIF" ]; then
    CAKE_ARGS+=("$WHATIF")
fi

# Run Cake
echo "Running Cake with arguments: ${CAKE_ARGS[*]}"
dotnet "${CAKE_ARGS[@]}"

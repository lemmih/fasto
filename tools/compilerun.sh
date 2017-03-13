#!/usr/bin/env sh
#
# Compile and run a FASTO program.  This script should work on Linux, Mac, and
# Microsoft Windows with Cygwin <https://cygwin.com/>.
#
# The Mars4_5.jar simulator must be in your Fasto "lib" directory, or you must
# export its location into the environment variable named MARS.
#
# If '-o' is given as the first argument, the program will be optimised.
#
# Usage: compilerun [-o] FASTO_PROGRAM

set -e # Exit on first error.

base_dir="$(dirname "$0")"

if [ "$1" = -o ]; then
    flags=-o
    shift
else
    flags=-c
fi

prog_input="$1"

# Determine location of MARS.
if ! [ "$MARS" ]; then
    MARS="$base_dir/../lib/Mars4_5.jar"
    if [ $(uname -o 2> /dev/null) = "Cygwin" ]; then
        MARS="$(cygpath -w "$MARS")"
    fi
fi

# Compile.
"$base_dir/../src/fasto" $flags "$1"

# Verify that Java is installed.
java -version &> /dev/null || (echo "Could not find java" && exit 1)

# Run.
java -jar "$MARS" nc \
"$(dirname "$prog_input")/$(basename "$prog_input" .fo).asm" 2> /dev/null

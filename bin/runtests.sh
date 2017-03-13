#!/usr/bin/env bash
#
# Run all tests.
#
# Use -o to optimise the test programs before compiling them.
# Use -i to interpret the test programs instead of compiling them.
#
# For example if you are in directory src, you can run it with:
#   $ ../tools/runtests.sh -o ../tests
#
# The path to fasto is simply taken as "$base_dir/../bin/fasto",
#   hence you should run this script from an imediate subfolder
#   of `fasto`, e.g., `src` or `bin` or `tools`...
#
# Test programs (those ending with '.fo') are given their corresponding
# '.in' files as standard in when running, and are expected to produce
# the contents of the corresponding '.out' files, or the error of the
# corresponding '.err' files.  If no corresponding '.in' file exists,
# the program is expected to fail at compile time.
#
# The Mars4_5.jar simulator must be in your Fasto "lib" directory, or
# you must export its location into the environment variable named MARS,
# unless you're using the '-i' option, in which case MARS is not used.
#
# If no argument is given, the script will run the tests in the current
# directory; otherwise it will use the first argument as a directory,
# and run the tests in that directory.
#
# Authors through the ages:
#   Troels Henriksen <athas@sigkill.dk>.
#   Rasmus Wriedt Larsen
#   Mathias Grymer <mathias1292@gmail.com>
#   Niels G. W. Serup <ngws@metanohi.name>

set -e # Die on first error.

base_dir="$(dirname "$0")"
fasto="$base_dir/../bin/fasto"

# Determine fasto command-line flags.
if [ "$1" = -o ]; then
    flags=-o
    shift
elif [ "$1" = -i ]; then
    flags=''
    shift
else
    flags=-c
fi

# Determine location of MARS.
if ! [ "$MARS" ]; then
    MARS="$base_dir/../lib/Mars4_5.jar"
    if [ $(uname -o 2> /dev/null) = "Cygwin" ]; then
        MARS="$(cygpath -w "$MARS")"
    fi
fi

# Verify that Java is installed.
java -version &> /dev/null || (echo "Could not find java" && exit 1)

# Find the directory containing the test programs.
tests_dir="$1"
if ! [ "$tests_dir" ]; then
    tests_dir="$base_dir/../tests"
fi
tests_dir="$(echo "$tests_dir" | sed 's/\/*$//')"

# Remove all whitespace and NUL bytes when comparing results, because
# Mars and the interpreter puts different amounts -- and to handle
# Windows/OSX/Unix line ending differences.
fix_whitespace() {
    cat "$1" | tr -d '\000' | tr -d ' \t\n\r\f' 1>&1
}

check_equal() {
    if [ -f $tests_dir/$OUTPUT ]; then

        EXPECTED=$(fix_whitespace "$tests_dir/$OUTPUT")
        ACTUAL=$(fix_whitespace "$TESTOUT")
        if [ "$EXPECTED" = "$ACTUAL" ]; then
            rm -f $TESTOUT
        else
            echo "Output for $PROG does not match expected output."
            echo "Compare $TESTOUT and $tests_dir/$OUTPUT."
            return 1
        fi
    fi
}

make -C "$base_dir/.."

file_len=0
for FO in $tests_dir/*fo; do
    L=$(basename "$FO")
    if ((${#L} > $file_len)); then
        file_len=${#L}
    fi
done
file_len=$(($file_len+4))

echo
echo "=========== Running Fasto test programs ==========="
echo
for FO in $tests_dir/*fo; do
    FO=$(basename "$FO")
    PROG=$(echo $FO|sed 's/.fo$//')
    INPUT=$(echo $FO|sed 's/fo$/in/')
    OUTPUT=$(echo $FO|sed 's/fo$/out/')
    ERROUT=$(echo $FO|sed 's/fo$/err/')
    ASM=$(echo $FO|sed 's/fo$/asm/')
    TESTOUT=$tests_dir/$OUTPUT-testresult

    if [ -f $tests_dir/$INPUT ]; then
        # Is positive test.
        echo -n "Testing"
        printf "%*s" $file_len " $FO:  "
        if [ "$flags" ]; then
            # Compile.
            if $fasto $flags $tests_dir/$PROG; then
                java -jar "$MARS" nc $tests_dir/$ASM < $tests_dir/$INPUT > $TESTOUT 2>/dev/null
                if check_equal; then
                    echo -e "\033[92mSuccess.\033[0m"
                else
                    echo -e "\033[91mCompilation error.\033[0m"
                fi
            else
                echo -e "\033[91mCompilation error.\033[0m"
            fi
        else
            # Interpret.
            cat $tests_dir/$INPUT | $fasto -r $tests_dir/$PROG | grep -v "Result of 'main'" > $TESTOUT 2>&1
            if check_equal; then
                echo -e "\033[92mSuccess.\033[0m"
            else
                echo -e "\033[91mInterpretation error.\033[0m"
            fi
        fi
    else
        # Is negative test.
        echo -n "Testing"
        printf "%*s" $file_len "$FO:  "
        if $fasto -c $tests_dir/$PROG > $TESTOUT 2>&1; then
            echo -e "\033[91mCompiled but should result in compile error.\033[0m"
        elif [ -f $tests_dir/$ERROUT ]; then
            EXPECTED=$(fix_whitespace $tests_dir/$ERROUT)
            ACTUAL=$(fix_whitespace $TESTOUT)
            if [ "$EXPECTED" = "$ACTUAL" ]; then
                rm -f $TESTOUT
                echo -e "\033[92mSuccess.\033[0m"
            else
                echo -e "\033[91mThe error for $PROG does not match the expected error.  Compare $TESTOUT and $tests_dir/$ERROUT.\033[0m"
            fi
        fi
    fi
done

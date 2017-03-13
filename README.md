# The Fasto Compiler

This is the compiler for the Fasto programming language.  The source
code resides in the 'src' directory.

To build the compiler, run 'make'.

To print, compile or interpret a Fasto program, run 'bin/fasto'.

To run all tests from the 'tests' directory, run 'bin/runtests.sh'.

## Test output

    =========== Running Fasto test programs ===========
    
    Testing             assgn1.fo:  Success.
    Testing          basic_div.fo:  Success.
    Testing    basic_div_rules.fo:  Success.
    Testing         basic_mult.fo:  Success.
    Testing   basic_mult_rules.fo:  Success.
    Testing           bool_ops.fo:  Runtime error.
    Testing copyConstPropFold0.fo:  Success.
    Testing copyConstPropFold1.fo:  Success.
    Testing         fail_parse.fo:  Success.
    Testing                fib.fo:  Success.
    Testing         inline_map.fo:  Compilation error.
    Testing            io_mssp.fo:  Compilation error.
    Testing               iota.fo:  Success
    Testing             lambda.fo:  Compilation error.
    Testing         map_red_io.fo:  Compilation error.
    Testing             negate.fo:  Success.
    Testing         neg_simple.fo:  Success.
    Testing             ordchr.fo:  Compilation error.
    Testing       proj_figure3.fo:  Compilation error.
    Testing             reduce.fo:  Success.
    Testing          replicate.fo:  Compilation error.
    Testing               scan.fo:  Compilation error.
    Testing      short_circuit.fo:  Compilation error.

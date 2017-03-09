# Makefile for compiling and cleaning up the F# Fasto compiler.
#
# Completely non-clever explicit make targets for every file in the
# Fasto compiler.

OS=$(shell uname -s)

ifeq ($(OS),Darwin)
  export AS=as -arch i386
  export CC=cc -arch i386 -framework CoreFoundation -lobjc -liconv
endif

.PHONY: all clean

fslex=mono lib/fslex.exe
fsyacc=mono lib/fsyacc.exe
powerpack=lib/FSharp.PowerPack.dll
fsharpc=fsharpc --nologo

LexerGen=bin/Lexer.fs
ParserGen=bin/Parser.fs
AbSynLib=bin/AbSyn.dll
ParserLib=bin/Parser.dll
LexerLib=bin/Lexer.dll
SymTabLib=bin/SymTab.dll
InterpreterLib=bin/Interpreter.dll
TypeCheckerLib=bin/TypeChecker.dll
MipsLib=bin/Mips.dll
RegAllocLib=bin/RegAlloc.dll
CodeGenLib=bin/CodeGen.dll
CallGraphLib=bin/CallGraph.dll
InliningLib=bin/Inlining.dll
DeadBindingRemovalLib=bin/DeadBindingRemoval.dll
DeadFunctionRemovalLib=bin/DeadFunctionRemoval.dll
CopyConstPropFoldLib=bin/CopyConstPropFold.dll
FastoExe=bin/Fasto.exe

all: bin/fasto

bin/fasto: $(FastoExe)
	mkbundle bin/Fasto.exe bin/*.dll lib/*.dll -o bin/fasto

$(LexerGen): src/Lexer.fsl
	$(fslex) src/Lexer.fsl -o $(LexerGen)

$(ParserGen): src/Parser.fsp
	$(fsyacc) -v --module Parser src/Parser.fsp -o $(ParserGen)

$(AbSynLib): src/AbSyn.fs
	$(fsharpc) -a src/AbSyn.fs -o $(AbSynLib)

$(ParserLib): $(ParserGen) $(AbSynLib)
	$(fsharpc) -a $(ParserGen) -r $(AbSynLib) -r $(powerpack) -o $(ParserLib)

$(LexerLib): $(LexerGen) $(AbSynLib) $(ParserLib)
	$(fsharpc) -a $(LexerGen) -r $(AbSynLib) -r $(ParserLib) -r $(powerpack) -o $(LexerLib)

$(SymTabLib): src/SymTab.fs
	$(fsharpc) -a src/SymTab.fs -o $(SymTabLib)

$(InterpreterLib): src/Interpreter.fs $(AbSynLib) $(SymTabLib)
	$(fsharpc) -a src/Interpreter.fs -r $(AbSynLib) -r $(SymTabLib) -o $(InterpreterLib)

$(TypeCheckerLib): src/TypeChecker.fs $(AbSynLib) $(SymTabLib)
	$(fsharpc) -a src/TypeChecker.fs -r $(AbSynLib) -r $(SymTabLib) -o $(TypeCheckerLib)

$(MipsLib): src/Mips.fs $(AbSynLib)
	$(fsharpc) -a src/Mips.fs -r $(AbSynLib) -o $(MipsLib)

$(RegAllocLib): src/RegAlloc.fs $(MipsLib) $(AbSynLib)
	$(fsharpc) -a src/RegAlloc.fs -r $(MipsLib) -r $(AbSynLib) -o $(RegAllocLib)

$(CodeGenLib): src/CodeGen.fs $(AbSynLib) $(SymTabLib) $(MipsLib) $(RegAllocLib)
	$(fsharpc) -a src/CodeGen.fs -r $(AbSynLib) -r $(SymTabLib) -r $(MipsLib) -r $(RegAllocLib) -o $(CodeGenLib)

$(CallGraphLib): src/CallGraph.fs $(AbSynLib)
	$(fsharpc) -a src/CallGraph.fs -r $(AbSynLib) -o $(CallGraphLib)

$(InliningLib): src/Inlining.fs $(AbSynLib) $(CallGraphLib)
	$(fsharpc) -a src/Inlining.fs -r $(AbSynLib) -r $(CallGraphLib) -o $(InliningLib)

$(DeadBindingRemovalLib): src/DeadBindingRemoval.fs $(AbSynLib) $(CallGraphLib)
	$(fsharpc) -a src/DeadBindingRemoval.fs -r $(AbSynLib) -r $(CallGraphLib) -o $(DeadBindingRemovalLib)

$(DeadFunctionRemovalLib): src/DeadFunctionRemoval.fs $(AbSynLib) $(CallGraphLib)
	$(fsharpc) -a src/DeadFunctionRemoval.fs -r $(AbSynLib) -r $(CallGraphLib) -o $(DeadFunctionRemovalLib)

$(CopyConstPropFoldLib): src/CopyConstPropFold.fs $(AbSynLib) $(SymTabLib)
	$(fsharpc) -a src/CopyConstPropFold.fs -r $(AbSynLib) -r $(SymTabLib) -o $(CopyConstPropFoldLib)

$(FastoExe): src/Fasto.fsx $(AbSynLib) $(ParserLib) $(LexerLib) $(SymTabLib) $(InterpreterLib) $(TypeCheckerLib)  $(MipsLib) $(RegAllocLib) $(CodeGenLib) $(CallGraphLib) $(InliningLib) $(DeadFunctionRemovalLib) $(DeadBindingRemovalLib) $(CopyConstPropFoldLib)
	$(fsharpc) src/Fasto.fsx -o $(FastoExe) -r $(AbSynLib) -r $(SymTabLib) -r $(ParserLib) -r $(LexerLib) -r $(InterpreterLib) -r $(TypeCheckerLib) -r $(MipsLib) -r $(RegAllocLib) -r $(CodeGenLib) -r $(CallGraphLib) -r  $(InliningLib) -r $(DeadFunctionRemovalLib) -r $(DeadBindingRemovalLib) -r $(CopyConstPropFoldLib) -r $(powerpack) -o $(FastoExe)

clean:
	rm -f bin/*.dll bin/*.fs bin/Parser.fsyacc.output bin/Parser.fsi bin/Fasto.exe bin/fasto
	rm -f tests/*.asm tests/*.out-testresult

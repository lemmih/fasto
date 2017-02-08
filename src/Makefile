# Makefile for compiling and cleaning up the F# Fasto compiler.
#
# Completely non-clever explicit make targets for every file in the
# Fasto compiler.

.PHONY: all clean

fslex=mono ../lib/fslex.exe
fsyacc=mono ../lib/fsyacc.exe
powerpack=../lib/FSharp.PowerPack.dll
fsharpc=fsharpc --nologo

LexerGen=../bin/Lexer.fs
ParserGen=../bin/Parser.fs
AbSynLib=../bin/AbSyn.dll
ParserLib=../bin/Parser.dll
LexerLib=../bin/Lexer.dll
SymTabLib=../bin/SymTab.dll
InterpreterLib=../bin/Interpreter.dll
TypeCheckerLib=../bin/TypeChecker.dll
MipsLib=../bin/Mips.dll
RegAllocLib=../bin/RegAlloc.dll
CodeGenLib=../bin/CodeGen.dll
CallGraphLib=../bin/CallGraph.dll
InliningLib=../bin/Inlining.dll
DeadBindingRemovalLib=../bin/DeadBindingRemoval.dll
DeadFunctionRemovalLib=../bin/DeadFunctionRemoval.dll
CopyConstPropFoldLib=../bin/CopyConstPropFold.dll
FastoExe=../bin/Fasto.exe

all: $(FastoExe)

$(LexerGen): Lexer.fsl
	$(fslex) Lexer.fsl -o $(LexerGen)

$(ParserGen): Parser.fsp
	$(fsyacc) -v --module Parser Parser.fsp
	mv Parser.fs ../bin

$(AbSynLib): AbSyn.fs
	$(fsharpc) -a AbSyn.fs
	mv AbSyn.dll ../bin

$(ParserLib): $(ParserGen) $(AbSynLib)
	$(fsharpc) -a $(ParserGen) -r $(AbSynLib) -r $(powerpack)
	mv Parser.dll ../bin
	rm Parser.fsyacc.output
	rm Parser.fsi

$(LexerLib): $(LexerGen) $(AbSynLib) $(ParserLib)
	$(fsharpc) -a $(LexerGen) -r $(AbSynLib) -r $(ParserLib) -r $(powerpack)
	mv Lexer.dll ../bin

$(SymTabLib): SymTab.fs
	$(fsharpc) -a SymTab.fs
	mv SymTab.dll ../bin

$(InterpreterLib): Interpreter.fs $(AbSynLib) $(SymTabLib)
	$(fsharpc) -a Interpreter.fs -r $(AbSynLib) -r $(SymTabLib)
	mv Interpreter.dll ../bin

$(TypeCheckerLib): TypeChecker.fs $(AbSynLib) $(SymTabLib)
	$(fsharpc) -a TypeChecker.fs -r $(AbSynLib) -r $(SymTabLib)
	mv TypeChecker.dll ../bin

$(MipsLib): Mips.fs $(AbSynLib)
	$(fsharpc) -a Mips.fs -r $(AbSynLib)
	mv Mips.dll ../bin

$(RegAllocLib): RegAlloc.fs $(MipsLib) $(AbSynLib)
	$(fsharpc) -a RegAlloc.fs -r $(MipsLib) -r $(AbSynLib)
	mv RegAlloc.dll ../bin

$(CodeGenLib): CodeGen.fs $(AbSynLib) $(SymTabLib) $(MipsLib) $(RegAllocLib)
	$(fsharpc) -a CodeGen.fs -r $(AbSynLib) -r $(SymTabLib) -r $(MipsLib) -r $(RegAllocLib)
	mv CodeGen.dll ../bin

$(CallGraphLib): CallGraph.fs $(AbSynLib)
	$(fsharpc) -a CallGraph.fs -r $(AbSynLib)
	mv CallGraph.dll ../bin

$(InliningLib): Inlining.fs $(AbSynLib) $(CallGraphLib)
	$(fsharpc) -a Inlining.fs -r $(AbSynLib) -r $(CallGraphLib)
	mv Inlining.dll ../bin

$(DeadBindingRemovalLib): DeadBindingRemoval.fs $(AbSynLib) $(CallGraphLib)
	$(fsharpc) -a DeadBindingRemoval.fs -r $(AbSynLib) -r $(CallGraphLib)
	mv DeadBindingRemoval.dll ../bin

$(DeadFunctionRemovalLib): DeadFunctionRemoval.fs $(AbSynLib) $(CallGraphLib)
	$(fsharpc) -a DeadFunctionRemoval.fs -r $(AbSynLib) -r $(CallGraphLib)
	mv DeadFunctionRemoval.dll ../bin

$(CopyConstPropFoldLib): CopyConstPropFold.fs $(AbSynLib) $(SymTabLib)
	$(fsharpc) -a CopyConstPropFold.fs -r $(AbSynLib) -r $(SymTabLib)
	mv CopyConstPropFold.dll ../bin

$(FastoExe): Fasto.fsx $(AbSynLib) $(ParserLib) $(LexerLib) $(SymTabLib) $(InterpreterLib) $(TypeCheckerLib)  $(MipsLib) $(RegAllocLib) $(CodeGenLib) $(CallGraphLib) $(InliningLib) $(DeadFunctionRemovalLib) $(DeadBindingRemovalLib) $(CopyConstPropFoldLib)
	$(fsharpc) Fasto.fsx -o $(FastoExe) -r $(AbSynLib) -r $(SymTabLib) -r $(ParserLib) -r $(LexerLib) -r $(InterpreterLib) -r $(TypeCheckerLib) -r $(MipsLib) -r $(RegAllocLib) -r $(CodeGenLib) -r $(CallGraphLib) -r  $(InliningLib) -r $(DeadFunctionRemovalLib) -r $(DeadBindingRemovalLib) -r $(CopyConstPropFoldLib) -r $(powerpack)

clean:
	rm -f ../bin/*.dll ../bin/*.fs ../bin/Fasto.exe

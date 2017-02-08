// The Fasto compiler command-line interface.
//
// This is the main program for when this compiler is turned into an executable.
// It ties together all the compiler modules.  You can build the compiler by
// running 'make' in the 'src' directory.

open System.Text
open Microsoft.FSharp.Text.Lexing
open Microsoft.FSharp.Text.Parsing
open System.IO

open AbSyn
open Inlining
open DeadFunctionRemoval
open DeadBindingRemoval
open CopyConstPropFold


// YOU DO NOT NEED TO UNDERSTAND THIS; IT IS A HACK: State machine for getting
// line and position numbers from a Parser error string.  This is really nasty.
// The problem is that we can only define the needed 'parse_error_rich' function
// in Parser.fsp at the top of the file, which means that we have not defined
// the actual tokens yet, so we cannot pattern match on them for extracting
// their source code positions, although we *can* print them.  An alternative
// solution is to inject a proper 'parse_error_rich' function in the bottom of
// the generated Parser.fs.
exception SyntaxError of int * int
let printPos (errString : string) : unit =
    let rec state3 (s : string) (p : int) (lin : string) (col : int) =
        (* read digits until not *)
        let c = s.[p]
        if System.Char.IsDigit c
        then state3 s (p-1) (System.Char.ToString c + lin) col
        else raise (SyntaxError (System.Int32.Parse lin, col))

    let rec state2 (s : string) (p : int) (col : string) =
        (* skip from position until digit *)
        let c = s.[p]
        if System.Char.IsDigit c
        then state3 s (p-1) (System.Char.ToString c) (System.Int32.Parse col)
        else state2 s (p-1) col

    let rec state1 (s : string) (p : int) (col : string) =
        (* read digits until not *)
        let c = s.[p]
        if System.Char.IsDigit c
        then state1 s (p-1) (System.Char.ToString c + col)
        else state2 s (p-1) col

    let rec state0 (s : string) (p : int) =
        (* skip from end until digit *)
        let c = s.[p]
        if System.Char.IsDigit c
        then state1 s (p-1) (System.Char.ToString c)
        else state0 s (p-1)

    state0 errString (String.length errString - 1)

// Parse program from string.
let parseString (s : string) : AbSyn.UntypedProg =
    Parser.Prog Lexer.Token
    <| LexBuffer<_>.FromBytes (Encoding.UTF8.GetBytes s)

////////////////////
/// Usage helper ///
////////////////////
let usage =
    [ "   fasto -i tests/fib.fo\n"
    ; "     Run 'fib.fo' in the 'tests' directory in interpreted mode.\n"
    ; "     and print the result.\n"
    ; "\n"
    ; "   fasto -r tests/fib.fo\n"
    ; "     Run 'fib.fo' in interpreted mode, but do not print the result.\n"
    ; "\n"
    ; "   fasto -c tests/fib.fo\n"
    ; "     Compile 'tests/fib.fo' into the MIPS program 'tests/fib.asm'.\n"
    ; "\n"
    ; "   fasto -o [opts] tests/fib.fo\n"
    ; "     Compile the optimised 'tests/fib.fo' into 'tests/fib.asm'.\n"
    ; "\n"
    ; "   fasto -p [opts] tests/fib.fo\n"
    ; "     Optimise 'tests/fib.fo' and print the result on standard output.\n"
    ; "     <opts> is a sequence of characters corresponding to optimisation\n"
    ; "     passes, where: \n"
    ; "       i                     - Inline functions.\n"
    ; "       c                     - Copy propagation and constant folding.\n"
    ; "       d                     - Remove dead bindings.\n"
    ; "       D                     - Remove dead functions.\n"
    ]


// Print error message to the standard error channel.
let errorMessage (message : string) : Unit =
    printfn "%s\n" message

let errorMessage' (errorType : string, message : string, line : int, col : int) =
    printfn "%s: %s at line %d, column %d" errorType message line col

let bad () : Unit =
    errorMessage "Unknown command-line arguments. Usage:\n"
    errorMessage (usage |> List.fold (+) "")

exception FileProblem of string

// Remove trailing .fo from filename.
let sanitiseFilename (argFilename : string) : string =
  if argFilename.EndsWith ".fo"
  then argFilename.Substring(0, (String.length argFilename)-3)
  else argFilename

// Save the content of a string to file.
let saveFile (filename : string) (content : string) : Unit =
  try
    let outFile = File.CreateText filename
    // Generate code here.
    outFile.Write content
    outFile.Close()
  with
    | ex ->
        printfn "Problem writing file named: %s, error: %s,\n where content is:\n %s\n" filename ex.Message content
        System.Environment.Exit 1


let parseFastoFile (filename : string) : AbSyn.UntypedProg =
  let txt = try  // read text from file given as parameter with added extension
              let inStream = File.OpenText (filename + ".fo")
              let txt = inStream.ReadToEnd()
              inStream.Close()
              txt
            with  // or return empty string
              | ex -> ""
  if txt <> "" then // valid file content
    let program =
      try
        parseString txt
      with
        | Lexer.LexicalError (info,(line,col)) ->
            printfn "%s at line %d, position %d\n" info line col
            System.Environment.Exit 1
            []
        | ex ->
            if ex.Message = "parse error"
            then printPos Parser.ErrorContextDescriptor
            else printfn "%s" ex.Message
            System.Environment.Exit 1
            []
    program
  else failwith "Invalid file name or empty file"

let compile (filename : string) optimiser : Unit =
    let pgm = parseFastoFile filename
    let pgm_decorated  = TypeChecker.checkProg pgm
    let pgm_optimised  = optimiser pgm_decorated
    let mips_code      = CodeGen.compile pgm_decorated //pgm_optimised
    let mips_code_text = Mips.ppMipsProg mips_code
    saveFile (filename + ".asm") mips_code_text

let interpret (filename : string) : Unit =
    let pgm = parseFastoFile filename
    printfn "Program is:\n\n%s" (AbSyn.ppProg pgm)
    printfn "\n+-----------------------------------------+"
    printfn "\n| You might need to enter some input now. |"
    printfn "\n+-----------------------------------------+"
    printfn "\n"
    let res = Interpreter.evalProg pgm
    printfn "\n\nResult of 'main': %s\n" (AbSyn.ppVal 0 res)

let interpretSimple (filename : string) : AbSyn.Value =
    let pgm = parseFastoFile filename
    Interpreter.evalProg pgm

let printOptimised (argFilename : string) optimiser : Unit =
    let pgm = parseFastoFile argFilename
    let pgm_decorated = TypeChecker.checkProg pgm
    let pgm_optimised = optimiser pgm_decorated
    printfn "%s\n" (ppProg pgm_optimised)

let defaultOptimisations (prog : TypedProg) =
    (removeDeadFunction <<
     removeDeadBindings <<
     optimiseProgram <<
     inlineOptimiseProgram) prog

type opt = char

let rec extractOpts (opts : opt list) =
    match opts with
        | [] -> Some (fun x -> x)
        | opt::opls ->
            let extractOpt (op : opt) =
                match op with
                    | 'i' -> Some inlineOptimiseProgram
                    | 'c' -> Some optimiseProgram
                    | 'd' -> Some removeDeadBindings
                    | 'D' -> Some removeDeadFunction
                    | _   -> None
            match (extractOpt opt, extractOpts opls) with
                | (Some opt', Some opts') -> Some (fun x -> opts' (opt' x))
                | _                       -> None

let explode (s:string) =
    [for c in s -> c]

[<EntryPoint>]
let main (paramList: string[]) : int =
  try
    match paramList with
      | [|"-i"; file|] -> interpret (sanitiseFilename file)
      | [|"-r"; file|] -> let res = interpretSimple (sanitiseFilename file)
                          printfn "\n\nResult of 'main': %s\n" (AbSyn.ppVal 0 res)
      | [|"-c"; file|] -> compile  (sanitiseFilename file) (fun x -> x)
      | [|"-o"; file|] -> compile  (sanitiseFilename file) defaultOptimisations
      | [|"-o"; opts; file|] ->
          match extractOpts (explode opts) with
            | Some (opts') -> compile (sanitiseFilename file) opts'
            | None         -> bad ()
      | [|"-p"; file|] ->
          printOptimised (sanitiseFilename file) defaultOptimisations
      | [|"-p"; opts; file|] ->
          match extractOpts (explode opts) with
              | Some (opts') -> printOptimised (sanitiseFilename file) opts'
              | None         -> bad ()
      | _ -> bad ()
    0
  with
    | SyntaxError (line, col) ->
        errorMessage' ("Parse error", "Error", line, col)
        System.Environment.Exit 1
        1
    | Lexer.LexicalError (message, (line, col)) ->
        errorMessage' ("Lexical error", message, line, col)
        System.Environment.Exit 1
        1
    | Interpreter.MyError (message, (line, col)) ->
        errorMessage' ("Interpreter error", message, line, col)
        System.Environment.Exit 1
        1
    | CodeGen.MyError (message, (line, col)) ->
        errorMessage' ("Code generator error", message, line, col)
        System.Environment.Exit 1
        1
    | TypeChecker.MyError (message, (line, col)) ->
        errorMessage' ("Type error", message, line, col)
        System.Environment.Exit 1
        1
    | FileProblem filename ->
        errorMessage ("There was a problem with the file: " + filename)
        System.Environment.Exit 1
        1

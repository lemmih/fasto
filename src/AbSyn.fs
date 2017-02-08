module  AbSyn

(*
 Types and utilities for the abstract syntax tree (AbSyn) of Fasto.
 Fasto er et funktionelt array-sprog til oversættelse, F-A-S-T-O.
 Fasto er også et spansk ord, der betyder "pomp" eller "pragt".
 Derfor skal vi programmere en "pragtfuld" oversætter for Fasto.

 The abstract syntax of a (Fasto) program is a representation of the (Fasto)
 program in terms of a data type in another programming language (F#).

 Some expressions in Fasto (e.g. array constants, indexing operations, map,
 reduce) are implicitly typed, their types are not explicitly stated in the
 program text. Their types are infered at run-time by an interpreter, or at
 compile-time by a type-checker.

 In support of this, this module defines type-parameterized datatypes for
 expressions "Exp<'T>", let declarations "Dec<'T>", function arguments
 "FunArg<'T>", function declaration "FunDec<'T>", and program "Prog<'T>".
 These datatypes are instantiated over the "unit" and "Type" types to provide
 an abstract syntax tree without type information, e.g., "UntypedProg = Prog<unit>",
 and another abstract syntax tree in which the inferred type information
 is made explicit in the representation "TypedProg = Prog<Type>".

 For example:
 - interpretation uses the untyped version "TypedProg",
 - the type checking phase receives as input an untype program ("UntypedProg")
   and produces a typed program ("TypedProg")
 - the other compiler phases work on the typed program.

 Note that semantically we use two different AbSyns, but we avoid code duplication
 by means of the afore-mentioned type parameterization.

 Also our AbSyn stores not just the program structure, but also the positions of
 the program substructures in the original program text. This is useful for
 reporting errors at later passes of the compiler, e.g. type errors.

 This module also provides pretty printing functionality, printing a valid Fasto
 program given its abstract syntax. "pp" is used in this module as a shorthand
 for "prettyPrint".
*)

(*** Helper Functions ***)
let toCString (s : string) : string =
    let escape c =
        match c with
            | '\\' -> "\\\\"
            | '"'  -> "\\\""
            | '\n' -> "\\n"
            | '\t' -> "\\t"
            | _    -> System.String.Concat [c]
    String.collect escape s

// Doesn't actually support all escapes.  Too badefacilliteter.
let fromCString (s : string) : string =
    let rec unescape l: char list =
        match l with
            | []                -> []
            | '\\' :: 'n' :: l' -> '\n' :: unescape l'
            | '\\' :: 't' :: l' -> '\t' :: unescape l'
            | '\\' :: c   :: l' -> c    :: unescape l'
            | c           :: l' -> c    :: unescape l'
    Seq.toList s |> unescape |> System.String.Concat

(* position: (line, column) *)
type Position = int * int

type Type =
    Int
  | Bool
  | Char
  | Array of Type

type Value =
    IntVal   of int
  | BoolVal  of bool
  | CharVal  of char
  | ArrayVal of Value list * Type
    (* Type corresponds to the element-type of the array *)

(* Indentifies value types (for type checking) *)
let valueType = function
  | (IntVal _)         -> Int
  | (BoolVal _)        -> Bool
  | (CharVal _)        -> Char
  | (ArrayVal (_,tp))  -> Array tp

(* pretty printing types *)
let rec ppType = function
  | Int      -> "int"
  | Char     -> "char"
  | Bool     -> "bool"
  | Array tp -> "[" + ppType tp + "]"

(* Parameter declaration *)
type Param =
  Param of string * Type

type Exp<'T> =
    Constant  of Value * Position
  | StringLit of string * Position
  | ArrayLit  of Exp<'T> list * 'T * Position
  | Var   of string * Position
  | Plus  of Exp<'T> * Exp<'T> * Position
  | Minus of Exp<'T> * Exp<'T> * Position
  | Equal of Exp<'T> * Exp<'T> * Position
  | Less  of Exp<'T> * Exp<'T> * Position
  | If    of Exp<'T> * Exp<'T> * Exp<'T> * Position
  | Apply of string * Exp<'T> list * Position
  | Let   of Dec<'T> * Exp<'T> * Position
  | Index of string * Exp<'T> * 'T * Position

  (* dirty I/O *)
  | Read  of Type * Position
  | Write of Exp<'T> * 'T * Position

  (* Project implementations *)
  | Times  of Exp<'T> * Exp<'T> * Position
  | Divide of Exp<'T> * Exp<'T> * Position
  | Negate of Exp<'T> * Position
  | And of Exp<'T> * Exp<'T> * Position
  | Or  of Exp<'T> * Exp<'T> * Position
  | Not of Exp<'T> * Position

  (* Array constructors/combinators implementations *)
  | Iota   of Exp<'T> * Position

  (* replicate(n, a); the 'T argument is the type of the
     the second expression (i.e., a's type)
  *)
  | Replicate of Exp<'T> * Exp<'T> * 'T * Position

  (* map (f, array)
      the first 'T corresponds to the mapped array element type,
       which is the same as the f's input type;
      the second 'T corresponds to the result-array element type,
       which is the same as the f's result type.
  *)
  | Map    of FunArg<'T> * Exp<'T> * 'T * 'T * Position

  (* reduce (f, acc, array)
      the 'T argument corresponds to the array element type,
      which is the same as the f's result type.
  *)
  | Reduce of FunArg<'T> * Exp<'T> * Exp<'T> * 'T * Position

  (* scan (f, acc, array); the 'T argument is as in reduce's case *)
  | Scan   of FunArg<'T> * Exp<'T> * Exp<'T> * 'T * Position

and Dec<'T> =
    Dec of string * Exp<'T> * Position

and FunArg<'T> =
    FunName of string
  | Lambda of Type * Param list * Exp<'T> * Position

(* A function declaration is a tuple of:
(i) function name,
(ii) return type,
(iii) formal arguments names & types,
(iv) function's body,
(v) Position. *)
type FunDec<'T> =
  FunDec of string * Type * Param list * Exp<'T> * Position


(* Functions for extracting function properties *)
let getFunName (FunDec(fid, _, _, _, _)) = fid
let getFunRTP  (FunDec(_, rtp, _, _, _)) = rtp
let getFunArgs (FunDec(_, _, arg, _, _)) = arg
let getFunBody (FunDec(_, _, _, bdy, _)) = bdy
let getFunPos  (FunDec(_, _, _, _, pos)) = pos

type Prog<'T> = FunDec<'T> list

(****************************************************)
(********** Pretty-Printing Functionality ***********)
(****************************************************)

let rec indent = function
  | 0 -> ""
  | n -> "  " + indent (n-1)

let ppParam = function
  | Param(id, tp) -> ppType tp + " " + id

let rec ppParams = function
  | [] -> ""
  | [bd] -> ppParam bd
  | bd::l -> ppParam bd + ", " + ppParams l

let rec ppVal d = function
  | IntVal n           -> sprintf "%i" n
  | BoolVal b          -> sprintf "%b" b
  | CharVal c          -> "'" + toCString (string c) + "'"
  | ArrayVal (vals, t) -> "{ " + (String.concat ", " (List.map (ppVal d) vals)) + " }"

let newLine exp = match exp with
                     | Let _ -> ""
                     | _     -> "\n"

let rec ppExp d = function
  | Constant(v, _)              -> ppVal d v
  | StringLit(s,_)              -> "\"" + toCString s + "\""
  | ArrayLit(es, t, _)          -> "{ " + (String.concat ", " (List.map (ppExp d) es)) + " }"
  | Var (id, _)                 -> id
  | Plus (e1, e2, _)            -> "(" + ppExp d e1 + " + "  + ppExp d e2 + ")"
  | Minus (e1, e2, _)           -> "(" + ppExp d e1 + " - "  + ppExp d e2 + ")"
  | Times (e1, e2, _)           -> "(" + ppExp d e1 + " * "  + ppExp d e2 + ")"
  | Divide (e1, e2, _)          -> "(" + ppExp d e1 + " / "  + ppExp d e2 + ")"
  | And (e1, e2, _)             -> "(" + ppExp d e1 + " && " + ppExp d e2 + ")"
  | Or (e1, e2, _)              -> "(" + ppExp d e1 + " || " + ppExp d e2 + ")"
  | Not (e, _)                  -> "not("+ppExp d e + ")"
  | Negate (e, _)               -> "~(" + ppExp d e + ")"
  | Equal (e1, e2, _)           -> "(" + ppExp d e1 + " == " + ppExp d e2 + ")"
  | Less (e1, e2, _)            -> "(" + ppExp d e1 + " < " + ppExp d e2 + ")"
  | If (e1, e2, e3, _)          -> ("if (" + ppExp d e1 + ")\n" +
                                    indent (d+2) + "then " + ppExp (d+2) e2 + "\n" +
                                    indent (d+2) + "else " + ppExp (d+2) e3 + "\n" +
                                    indent d)
  | Apply (id, args, _)         -> (id + "(" +
                                    (String.concat ", " (List.map (ppExp d) args)) + ")")
  | Let (Dec(id, e1, _), e2, _) -> ("\n" + indent (d+1) + "let " + id + " = " +
                                    ppExp (d+2) e1 + " in" + newLine e2 +
                                    indent (d+1) + ppExp d e2)
  | Index (id, e, t, _)         -> id + "[" + ppExp d e + "]"
  | Iota (e, _)                 -> "iota(" + ppExp d e + ")"
  | Map (f, e, _, _, _)         -> "map(" + ppFunArg d f + ", " + ppExp d e + ")"
  | Reduce (f, el, lst, _, _)   ->
      "reduce(" + ppFunArg d f + ", " + ppExp d el + ", " + ppExp d lst + ")"
  | Replicate (n, e, _, _)         -> "replicate(" + ppExp d n + ", " + ppExp d e + ")"
  | Scan (f, el, lst, _, _) ->
      "scan(" + ppFunArg d f + ", " + ppExp d el + ", " + ppExp d lst + ")"
  | Read (t, _)                 -> "read(" + ppType t + ")"
  | Write (e, t, _)             -> "write(" + ppExp d e + ")"

and ppFunArg d = function
  | FunName s                     -> s
  | Lambda (rtp, args, body, _)   -> ("fn " + ppType rtp + " (" +
                                      ppParams args + ") => " + ppExp (d+2) body)

(* pretty prints a function declaration *)
let ppFun d = function
  | FunDec(id, rtp, args, body, _) -> ( "fun " + ppType rtp + " " + id +
                                        "(" + ppParams args + ") =" +
                                        indent (d+1) + ppExp(d+1) body )

(* Pretty pringint a program *)
let ppProg (p : Prog<'T>) = (String.concat "\n\n" (List.map (ppFun 0) p)) + "\n"

let expPos = function
  | Constant (_, p) -> p
  | StringLit (_, p) -> p
  | ArrayLit (_, _, p) -> p
  | Var (_, p) -> p
  | Plus (_, _, p) -> p
  | Minus (_, _, p) -> p
  | Equal (_, _, p) -> p
  | Less (_, _, p) -> p
  | If (_, _, _, p) -> p
  | Apply (_, _, p) -> p
  | Let (_, _, p) -> p
  | Index (_, _, _, p) -> p
  | Iota (_, p) -> p
  | Map (_, _, _, _, p) -> p
  | Reduce (_, _, _, _, p) -> p
  | Replicate (_, _, _, p) -> p
  | Scan (_, _, _, _, p) -> p
  | Read (_, p) -> p
  | Write (_, _, p) -> p
  | Times (_, _, p) -> p
  | Divide (_, _, p) -> p
  | And (_, _, p) -> p
  | Or (_, _, p) -> p
  | Not (_, p) -> p
  | Negate (_, p) -> p



type UntypedExp = Exp<unit>
type TypedExp = Exp<Type>

type UntypedDec = Dec<unit>
type TypedDec = Dec<Type>

type UntypedFunDec = FunDec<unit>
type TypedFunDec = FunDec<Type>

type UntypedFunArg = FunArg<unit>
type TypedFunArg = FunArg<Type>

type UntypedProg = Prog<unit>
type TypedProg = Prog<Type>

(* An interpreter for Fasto. *)

module Interpreter

(*

An interpreter executes a (Fasto) program by inspecting the abstract syntax
tree of the program, and doing what needs to be done in another programming
language (F#).

As mentioned in AbSyn.fs, some Fasto expressions are implicitly typed. The
interpreter infers the missing types, and checks the types of the operands
before performing any Fasto operation. Some type errors might still occur though.

Any valid Fasto program must contain a "main" function, which is the entry
point of the program. The return value of this function is the result of the
Fasto program.

The main function of interest in this module is:

  val evalProg : AbSyn.UntypedProg -> AbSyn.Value

*)

open System
open AbSyn

(* An exception for reporting run-time errors. *)
exception MyError of string * Position

type FunTable = SymTab.SymTab<UntypedFunDec>
type VarTable = SymTab.SymTab<Value>

(* Build a function table, which associates a function names with function
   declarations. *)
let rec buildFtab (fdecs : UntypedFunDec list) : FunTable =
  match fdecs with
  | [] -> let p  = (0, 0)
          let ch = 'a'
          let fdec_chr = FunDec ("chr", Char, [Param ("n", Int) ], Constant (CharVal ch, p), p)
          let fdec_ord = FunDec ("ord", Int,  [Param ("c", Char)], Constant (IntVal   1, p), p)
          SymTab.fromList [("chr", fdec_chr); ("ord", fdec_ord)]
  | ( fdcl::fs ) ->
    (* Bind the user-defined functions, in reverse order. *)
    let fid   = getFunName fdcl
    let pos   = getFunPos fdcl
    let ftab  = buildFtab fs
    match SymTab.lookup fid ftab with
      | None        -> SymTab.bind fid fdcl ftab
      | Some ofdecl ->
          (* Report the first occurrence of the name. *)
          raise (MyError ("Already defined function: "+fid, getFunPos ofdecl))

(* Check whether a value matches a type. *)
let rec typeMatch (tpval : Type * Value) : bool =
    match tpval with
      | ( Int,  IntVal _ ) -> true
      | ( Bool, BoolVal _) -> true
      | ( Char, CharVal _) -> true
      | ( Array t, ArrayVal (vals, tp) ) ->
        (t = tp) && (List.map (fun value -> typeMatch (t, value)) vals |> List.fold (&&) true)
      | (_, _) -> false


let invalidOperand (str : string)
                   (tp  : Type)
                   (e   : Value)
                   (pos : Position) =
    let msg = str + "; expected " + (ppType tp) + ", got "
                  + (ppType (valueType e)) + " (" + (ppVal 0 e) + ") instead"
    raise (MyError(msg, pos))

let invalidOperands (str : string)
                    (tps : (Type*Type) list)
                    (e0  : Value)
                    (e1  : Value)
                    (pos : Position) =
    let types = List.map (fun (tp0, tp1) -> (ppType tp0) + " and " + (ppType tp1)) tps
    let msg = str + "; expected " + (String.concat ", or " types) + ", got "
                  + (ppType (valueType e0)) + " (" + (ppVal 0 e0) + ") and "
                  + (ppType (valueType e1)) + " (" + (ppVal 0 e1) + ") instead"
    raise (MyError(msg, pos))

(* Index into an array. Check that the index is not out of bounds. *)
let applyIndexing = function
  | ( ArrayVal(lst, tp), IntVal ind, pos ) ->
        let len = List.length(lst)
        if( len > ind && ind >= 0 )
        then lst.Item(ind)
        else let msg = sprintf "Array index out of bounds! Array length: %i Index: %i" len ind
             raise (MyError(msg, pos))
  | ( arr, IntVal ind, pos ) ->
    raise (MyError("Indexing Error: " + (ppVal 0 arr) + " is not an array", pos))
  | ( arr, e, pos ) -> (* Order of clauses is important here. *)
    invalidOperand "Indexing error, non-integral index" Int e pos

(* Bind the formal parameters of a function declaration to actual parameters in
   a new vtab. *)
let rec bindParams = function
  | ([], [], fid, pd, pc) -> SymTab.empty()
  | ([], a,  fid, pd, pc) ->
        raise (MyError("Number of formal and actual params do not match in call to "+fid, pc))
  | (b,  [], fid, pd, pc) ->
        raise (MyError("Number of formal and actual params do not match in call to "+fid, pc))
  | ( Param (faid, fatp)::fargs, a::aargs, fid, pd, pc ) ->
        let vtab = bindParams( fargs, aargs, fid, pd, pc )
        if( typeMatch(fatp, a) )
        then match SymTab.lookup faid vtab with
               None   -> SymTab.bind faid a vtab
             | Some m -> raise (MyError( "Formal argument is already in symbol table!"+
                                         " In function: "+fid+" formal argument: "+faid
                                       , pd ))
        else raise (MyError( "Actual and formal argument type do not match"+
                             " in function: "+fid+"; formal argument: "+faid+
                             " of type: "+ppType(fatp)+
                             " does not matches actual argument: "+ppVal 0 a
                           , pc ))


(* Interpreter for Fasto expressions:
    1. vtab holds bindings between variable names and
       their interpreted value (Fasto.Value).
    2. ftab holds bindings between function names and
       function declarations (Fasto.FunDec).
    3. Returns the interpreted value. *)
let rec evalExp (e : UntypedExp, vtab : VarTable, ftab : FunTable) : Value =
  match e with
  | Constant (v,_) -> v
  | ArrayLit (l, t, pos) ->
        let els = (List.map (fun x -> evalExp(x, vtab, ftab)) l)
        let elt = match els with
                    | []   -> Int (* Arbitrary *)
                    | v::_ -> valueType v
        ArrayVal (els, elt)
  | StringLit(s, pos) ->
        let exps = List.map (fun c -> CharVal c) (Seq.toList s)
        ArrayVal (exps, Char)
  | Var(id, pos) ->
        let res = SymTab.lookup id vtab
        match res with
          | None   -> raise (MyError("Unknown variable "+id, pos))
          | Some m -> m
  | Plus(e1, e2, pos) ->
        let res1   = evalExp(e1, vtab, ftab)
        let res2   = evalExp(e2, vtab, ftab)
        match (res1, res2) with
          | (IntVal n1, IntVal n2) -> IntVal (n1+n2)
          | _ -> invalidOperands "Plus on non-integral args: " [(Int, Int)] res1 res2 pos
  | Minus(e1, e2, pos) ->
        let res1   = evalExp(e1, vtab, ftab)
        let res2   = evalExp(e2, vtab, ftab)
        match (res1, res2) with
          | (IntVal n1, IntVal n2) -> IntVal (n1-n2)
          | _ -> invalidOperands "Minus on non-integral args: " [(Int, Int)] res1 res2 pos

  (* TODO: project task 1:
     Look in `AbSyn.fs` for the arguments of the `Times`
     (`Divide`,...) expression constructors.
        Implementation similar to the cases of Plus/Minus.
        Try to pattern match the code above.
        For `And`/`Or`: make sure to implement the short-circuit semantics,
        e.g., `And (e1, e2, pos)` should not evaluate `e2` if `e1` already
              evaluates to false.
  *)
  | Times(e1, e2, pos) ->
        let res1   = evalExp(e1, vtab, ftab)
        let res2   = evalExp(e2, vtab, ftab)
        match (res1, res2) with
          | (IntVal n1, IntVal n2) -> IntVal (n1 * n2)
          | _ -> invalidOperands "Times on non-integral args: " [(Int, Int)] res1 res2 pos
  | Divide(e1, e2, pos) ->
        let res1   = evalExp(e1, vtab, ftab)
        let res2   = evalExp(e2, vtab, ftab)
        match (res1, res2) with
          | (IntVal n1, IntVal n2) -> IntVal (n1 / n2)
          | _ -> invalidOperands "Divide on non-integral args: " [(Int, Int)] res1 res2 pos
  | And (e1, e2, pos) ->
        let res1   = evalExp(e1, vtab, ftab)
        let res2   = evalExp(e2, vtab, ftab)
        match (res1, res2) with
          | (BoolVal b1, BoolVal b2) -> BoolVal (b1 && b2)
          | _ -> invalidOperands "AND on non-boolean args: " [(Bool, Bool)] res1 res2 pos
  | Or (e1, e2, pos) ->
        let res1   = evalExp(e1, vtab, ftab)
        let res2   = evalExp(e2, vtab, ftab)
        match (res1, res2) with
          | (BoolVal b1, BoolVal b2) -> BoolVal (b1 || b2)
          | _ -> invalidOperands "OR on non-boolean args: " [(Bool, Bool)] res1 res2 pos
  | Not(e1, pos) ->
        let res1   = evalExp(e1, vtab, ftab)
        match res1 with
          | (BoolVal b1) -> BoolVal (not b1)
          | _ -> invalidOperand "NOT on non-boolean arg: " Bool res1 pos
  | Negate(e1, pos) ->
        let res1   = evalExp(e1, vtab, ftab)
        match res1 with
          | (IntVal n1) -> IntVal (-n1)
          | _ -> invalidOperand "NEGATE on non-integral arg: " Int res1 pos

  | Equal(e1, e2, pos) ->
        let r1 = evalExp(e1, vtab, ftab)
        let r2 = evalExp(e2, vtab, ftab)
        match (r1, r2) with
          | (IntVal  n1, IntVal  n2) -> BoolVal (n1 = n2)
          | (BoolVal b1, BoolVal b2) -> BoolVal (b1 = b2)
          | (CharVal c1, CharVal c2) -> BoolVal (c1 = c2)
          | (_, _) -> invalidOperands "Invalid equality operand types" [(Int, Int); (Bool, Bool); (Char, Char)] r1 r2 pos
  | Less(e1, e2, pos) ->
        let r1 = evalExp(e1, vtab, ftab)
        let r2 = evalExp(e2, vtab, ftab)
        match (r1, r2) with
          | (IntVal  n1,    IntVal  n2  ) -> BoolVal (n1 < n2)
          | (BoolVal false, BoolVal true) -> BoolVal true
          | (BoolVal _,     BoolVal _   ) -> BoolVal false
          | (CharVal c1,    CharVal c2  ) -> BoolVal ( (int c1) < (int c2) )
          | (_, _) -> invalidOperands "Invalid less-than operand types" [(Int, Int); (Bool, Bool); (Char, Char)] r1 r2 pos
  | If(e1, e2, e3, pos) ->
        let cond = evalExp(e1, vtab, ftab)
        match cond with
          | BoolVal true  -> evalExp(e2, vtab, ftab)
          | BoolVal false -> evalExp(e3, vtab, ftab)
          | other         -> raise (MyError("If condition is not a boolean", pos))
  | Apply(fid, args, pos) ->
        let evargs = List.map (fun e -> evalExp(e, vtab, ftab)) args
        match (SymTab.lookup fid ftab) with
          | Some f -> callFunWithVtable(f, evargs, SymTab.empty(), ftab, pos)
          | None   -> raise (MyError("Call to unkwn function "+fid, pos))
  | Let(Dec(id,e,p), exp, pos) ->
        let res   = evalExp(e, vtab, ftab)
        let nvtab = SymTab.bind id res vtab
        evalExp(exp, nvtab, ftab)
  | Index(id, e, tp, pos) ->
        let indv= evalExp(e, vtab, ftab)
        let arr = SymTab.lookup id vtab
        match (arr, indv) with
          | (None, _) -> raise (MyError("Unknown array "+id, pos))
          | (Some (ArrayVal(lst, tp)), IntVal ind) ->
               let len = List.length(lst)
               if( len > ind && ind >= 0 )
               then lst.Item(ind)
               else let msg = sprintf "Array index out of bounds! Array length: %i, index: %i" len ind
                    raise (MyError( msg, pos ))
          | (Some m, IntVal _) -> raise (MyError("Indexing error: " + (ppVal 0 m) + " is not an array", pos))
          | (_, _) -> invalidOperand "Indexing error, non-integral index" Int indv pos
  | Iota (e, pos) ->
        let sz = evalExp(e, vtab, ftab)
        match sz with
          | IntVal size ->
              if size >= 0
              then ArrayVal( List.map (fun x -> IntVal x) [0..size-1], Int )
              else let msg = sprintf "Error: In iota call, size is negative: %i" size
                   raise (MyError(msg, pos))
          | _ -> raise (MyError("Iota argument is not a number: "+ppVal 0 sz, pos))
  | Reduce (farg, ne, arrexp, tp, pos) ->
        let farg_ret_type = rtpFunArg farg ftab pos
        let arr  = evalExp(arrexp, vtab, ftab)
        let nel  = evalExp(ne, vtab, ftab)
        match arr with
          | ArrayVal (lst,tp1) ->
               List.fold (fun acc x -> evalFunArg (farg, vtab, ftab, pos, [acc;x])) nel lst
          | otherwise -> raise (MyError("Third argument of reduce is not an array: "+ppVal 0 arr
                                       , pos))
  (* TODO project task 2: `replicate(n, a)`
     Look in `AbSyn.fs` for the arguments of the `Replicate`
     (`Map`,`Scan`) expression constructors.
       - evaluate `n` then evaluate `a`,
       - check that `n` evaluates to an integer value >= 0
       - If so then create an array containing `n` replicas of
         the value of `a`; otherwise raise an error (containing
         a meaningful message).
  *)
  | Replicate (nExp, eltExp, _, pos) ->
        let n    = evalExp(nExp, vtab, ftab)
        let elt  = evalExp(eltExp, vtab, ftab)
        match n with
          | IntVal size -> ArrayVal (List.replicate size elt, valueType elt)
          | otherwise -> raise (MyError("Expected number as argument of replicate: "+ppVal 0 n, pos))

  (* TODO project task 2: `map(f, arr)`
       pattern match the implementation of reduce:
       - get the result type of `f`  (use `rtpFunArg` defined below)
       - evaluate `arr` and check that the (value) result corresponds to an array,
       - use F# `List.map` to evaluate `f(a)` for every element (value) `a` of `arr`,
       - create an `ArrayVal` from the (list) result of the previous step.
  *)
  | Map (farg, arrexp, aTy, bTy, pos) ->
        let farg_ret_type = rtpFunArg farg ftab pos
        let arr  = evalExp(arrexp, vtab, ftab)
        match arr with
          | ArrayVal (lst,tp1) ->
               ArrayVal (List.map (fun x -> evalFunArg (farg, vtab, ftab, pos, [x])) lst, farg_ret_type)
          | otherwise -> raise (MyError("Second argument of map is not an array: "+ppVal 0 arr
                                       , pos))

  (* TODO project task 2: `scan(f, ne, arr)`
     Implementation similar to reduce, except that it produces an array
     of the same type and length to the input array `arr`.
  *)
  | Scan (_, _, _, _, _) ->
        failwith "Unimplemented interpretation of scan"

  | Read (t,p) ->
        let str = Console.ReadLine()
        match t with
          | Int -> let v : int = int str
                   IntVal v
(*
                   match v with
                     | Some n    -> IntVal n
                     | otherwise -> raise (MyError("read(int) Failed! ", p))
*)
           | Bool -> let v : int = int str (* Bool.fromString(str) *)
                     if( v=0 ) then BoolVal false else BoolVal true
           | Char -> let v : char = char str
                     CharVal v
           | otherwise -> raise (MyError("Read operation is valid only on basic types ", p))

  | Write(exp,t,p) ->
        let v  = evalExp(exp, vtab, ftab)
        match v with
          | IntVal  n -> printfn "%i " n
          | BoolVal b -> let res = if(b) then "true " else "false "
                         printfn "%s" res
          | CharVal c -> printfn "%c " c
          | ArrayVal (a, Char) ->
             let mapfun = function
                   | CharVal c -> c
                   | otherwise -> raise (MyError("Write argument " +
                                                   ppVal 0 v +
                                                   " should have been evaluated to string", p))
             printfn "%s" ( System.String.Concat (List.map mapfun a) )
          | otherwise -> raise (MyError("Write can be called only on basic and array(char) types ", p))
        v



(* finds the return type of a function argument *)
and rtpFunArg  (funarg  : UntypedFunArg)
               (ftab    : FunTable)
               (callpos : Position)
             : Type =
  match funarg with
    | FunName fid ->
        match SymTab.lookup fid ftab with
          | None   -> raise (MyError("Call to unknown function "+fid, callpos))
          | Some (FunDec (_, rettype, _, _, _)) -> rettype
    | Lambda (rettype, _, _, _) -> rettype

(* evalFunArg takes as argument a FunArg, a vtable, an ftable, the
position where the call is performed, and the list of actual arguments.
It returns the result of calling the (lambda) function.
 *)
and evalFunArg  ( funarg  : UntypedFunArg
                , vtab    : VarTable
                , ftab    : FunTable
                , callpos : Position
                , aargs   : Value list
                ) : Value =
  match funarg with
  | (FunName fid) ->
    let fexp = SymTab.lookup fid ftab
    match fexp with
      | None   -> raise (MyError("Call to known function "+fid, callpos))
      | Some f -> callFunWithVtable(f, aargs, SymTab.empty(), ftab, callpos)
  | Lambda (rettype, parms, body, fpos) ->
    callFunWithVtable ( FunDec ("<anonymous>", rettype, parms, body, fpos)
                      , aargs, vtab, ftab, callpos )

(* Interpreter for Fasto function calls:
    1. f is the function declaration.
    2. args is a list of (already interpreted) arguments.
    3. vtab is the variable symbol table
    4. ftab is the function symbol table (containing f itself).
    5. pcall is the position of the function call. *)
and callFunWithVtable (fundec : UntypedFunDec
                      , aargs : Value list
                      , vtab  : VarTable
                      , ftab  : FunTable
                      , pcall : Position
                      ) : Value =
    let (FunDec (fid, rtp, fargs, body, pdcl)) = fundec
    match fid with
      (* treat the special functions *)
      | "ord" -> match aargs with
                   | [CharVal c] -> IntVal (int c)
                   | otherwise -> raise (MyError("Argument of \"ord\" does not evaluate to Char: "+
                                                  (String.concat "" (List.map (ppVal 0) aargs)), pcall))
      | "chr" -> match aargs with
                    | [IntVal n] -> CharVal (char n)
                    | otherwise -> raise (MyError("Argument of \"chr\" does not evaluate to Num: "+
                                               (String.concat "" (List.map (ppVal 0) aargs)), pcall))

      | _ ->
        let vtab' = SymTab.combine (bindParams (fargs, aargs, fid, pdcl, pcall)) vtab
        let res  = evalExp (body, vtab', ftab)
        if typeMatch (rtp, res)
        then res
        else raise (MyError("Result type does not match the return type"+
                             " in function: "+fid+" return type: "+ppType(rtp)+
                             " result: "+ppVal 0 res, pcall))

(* Interpreter for Fasto programs:
    1. builds the function symbol table,
    2. interprets the body of "main", and
    3. returns its result. *)
and evalProg (prog : UntypedProg) : Value =
    let ftab  = buildFtab prog
    let mainf = SymTab.lookup "main" ftab
    match mainf with
      | None   -> raise (MyError("Could not find the main function", (0,0)))
      | Some m ->
          match getFunArgs m with
            | [] -> callFunWithVtable(m, [], SymTab.empty(), ftab, (0,0))
            | _  -> raise (MyError("The main function is not allowed to have parameters", getFunPos m))

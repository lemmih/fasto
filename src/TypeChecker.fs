(* A type-checker for Fasto. *)

module TypeChecker

(*

A type-checker checks that all operations in a (Fasto) program are performed on
operands of an appropriate type. Furthermore, a type-checker infers any types
missing in the original program text, necessary for well-defined machine code
generation.

The main function of interest in this module is:

  val checkProg : Fasto.UnknownTypes.Prog -> Fasto.KnownTypes.Prog

*)

open AbSyn

(* An exception for reporting type errors. *)
exception MyError of string * Position

type FunTable = SymTab.SymTab<(Type * Type list * Position)>
type VarTable = SymTab.SymTab<Type>


(* Table of predefined conversion functions *)
let initFunctionTable : FunTable =
    SymTab.fromList
        [( "chr", (Char, [Int], (0,0)));
         ( "ord", (Int, [Char], (0,0)))
        ]

(* Pretty-printer for function types, for error messages *)
let showFunType (args : Type list, res : Type) : string =
  match args with
    | []   -> " () -> " + ppType res
    | args -> (String.concat " * " (List.map ppType args))
                             + " -> " + ppType res

(* Type comparison that returns the type, raising an exception upon mismatch *)
let checkTypesEqualOrError (pos : Position) (t1 : Type) (t2 : Type) : Type =
    if t1 = t2 then t1 else
    raise (MyError ("Cannot match types "+ppType t1+" and "+ppType t2, pos))

(* Determine if a value of some type can be printed with write() *)
let printable (tp : Type) : bool =
  match tp with
    | (Int)  -> true
    | (Bool) -> true
    | (Char) -> true
    | (Array Char) -> true
    | _ -> false  (* For all other array types *)



(* Determine the type of an expression.  On the way, decorate each node in the
   syntax tree with inferred types.  The result consist of a pair: the result
   type tupled with the type-decorated expression. An exception is raised
   immediately on the first type mismatch - this happens in "checkTypesEqualOrError".
   (It could instead collect each error as part of the result of checkExp and
    report all errors at the end.)
*)
let rec checkExp  (ftab : FunTable)
              (vtab : VarTable)
              (exp  : UntypedExp)
            : (Type * TypedExp) =

    (* Type-check the two operands to a binary operator - they must both be of type 't' *)
    let checkBinOp   op
                        (t : Type)
                        (e1 : UntypedExp) (e2 : UntypedExp) (pos : Position)
                      : (Type * TypedExp) =
        let (t1, e1') = checkExp ftab vtab e1
        let (t2, e2') = checkExp ftab vtab e2
        if (t = t1 && t = t2)
        then (t, op (e1', e2', pos))
        else raise (MyError ("In checkBinOp: types not equal "+ppType t+" and "+ppType t1+" and "+ppType t2, pos))

    // Added by Lemmih
    let checkUniOp   op
                        (t : Type)
                        (e1 : UntypedExp) (pos : Position)
                      : (Type * TypedExp) =
        let (t1, e1') = checkExp ftab vtab e1
        if (t = t1)
        then (t, op (e1', pos))
        else raise (MyError ("In checkUniOp: types not equal "+ppType t+" and "+ppType t1, pos))

    match exp with
    | Constant  (v, pos) -> (valueType v, Constant (v, pos))
    | StringLit (s, pos) -> (Array Char, StringLit (s, pos))
    | ArrayLit  ([], _, pos) -> raise (MyError("Impossible empty array", pos))
    | ArrayLit  (exp::exps, _, pos) ->
      let (type_exp, exp_dec) = checkExp ftab vtab exp
      let (types_exps, exps_dec) = List.unzip (List.map (checkExp ftab vtab) exps)
      let same_type = List.fold (checkTypesEqualOrError pos) type_exp types_exps
      (* join will raise an exception if types do not match *)
      (Array same_type, ArrayLit (exp_dec::exps_dec, same_type, pos))

    | Var (s, pos) ->
        match SymTab.lookup s vtab with
          | None   -> raise (MyError ("Unknown variable "+s, pos))
          | Some t -> (t, Var (s, pos))

    | Plus (e1, e2, pos) ->
        let (t1, e1_dec) = checkExp ftab vtab e1
        let (t2, e2_dec) = checkExp ftab vtab e2
        if (Int = t1 && Int = t2)
        then (Int, Plus (e1_dec, e2_dec, pos))
        else raise (MyError ("In Plus: one of subexpression types is not Int: "+ppType t1+" and "+ppType t2, pos))

    | Minus (e1, e2, pos) ->
        let (t1, e1_dec) = checkExp ftab vtab e1
        let (t2, e2_dec) = checkExp ftab vtab e2
        if (Int = t1 && Int = t2)
        then (Int, Minus (e1_dec, e2_dec, pos))
        else raise (MyError ("In Minus: one of subexpression types is not Int: "+ppType t1+" and "+ppType t2, pos))

    (* TODO project task 1:
        Implement by pattern matching Plus/Minus above.
        See `AbSyn.fs` for the expression constructors of `Times`, ...
    *)
    // Added by Lemmih: Times, Divide, And, Or, Not, Negate.
    | Times (e1,e2,pos) -> checkBinOp Times Int e1 e2 pos

    | Divide (e1,e2,pos) -> checkBinOp Divide Int e1 e2 pos

    | And (e1,e2,pos) -> checkBinOp And Bool e1 e2 pos

    | Or (e1,e2,pos) -> checkBinOp Or Bool e1 e2 pos

    | Not (e1,pos) -> checkUniOp Not Bool e1 pos

    | Negate (e1,pos) -> checkUniOp Negate Int e1 pos

    (* The types for e1, e2 must be the same. The result is always a Bool. *)
    | Equal (e1, e2, pos) ->
        let  (t1, e1') = checkExp ftab vtab e1
        let  (t2, e2') = checkExp ftab vtab e2
        match (t1 = t2, t1) with
          | (false, _) -> raise (MyError ( "Cannot compare "+ ppType t1 +
                                           "and "+ppType t2+"for equality"
                                         , pos ))
          | (true, Array _) -> raise (MyError ("Cannot compare arrays", pos))
          | _ -> (Bool, Equal (e1', e2', pos))

    | Less (e1, e2, pos) ->
        let  (t1, e1') = checkExp ftab vtab e1
        let  (t2, e2') = checkExp ftab vtab e2
        match (t1 = t2, t1) with
          | (false, _) -> raise (MyError( "Cannot compare "+ ppType t1 +
                                          "and "+ppType t2+"for equality"
                                        , pos))
          | (true, Array _) -> raise (MyError ("Cannot compare arrays", pos))
          | _ -> (Bool, Less (e1', e2', pos))

    | If (pred, e1, e2, pos) ->
        let (pred_t, pred') = checkExp ftab vtab pred
        let (t1, e1') = checkExp ftab vtab e1
        let (t2, e2') = checkExp ftab vtab e2
        let target_type = checkTypesEqualOrError pos t1 t2
        match pred_t with
          | Bool -> (target_type, If (pred', e1', e2', pos))
          | otherwise -> raise (MyError ("Non-boolean predicate", pos))

    (* Look up f in function table, get a list of expected types for each
       function argument and an expected type for the return value. Check
       each actual argument.  Ensure that each actual argument type has the
       expected type. *)
    | Apply (f, args, pos) ->
        let (result_type, expected_arg_types, _) =
            match SymTab.lookup f ftab with
              | Some tup -> tup  (* 2-tuple *)
              | None     -> raise (MyError ("Unknown function " + f, pos))
        let (arg_types, args_dec) = List.unzip (List.map (checkExp ftab vtab) args)
        let _ = ( List.map (fun (t1,t2) -> checkTypesEqualOrError pos t1 t2)
                           (List.zip arg_types expected_arg_types) )
                |> ignore
        (result_type, Apply (f, args_dec, pos))

    | Let (Dec (name, exp, pos1), exp_body, pos2) ->
        let (t1, exp_dec)      = checkExp ftab vtab exp
        let new_vtab           = SymTab.bind name t1 vtab
        let (t2, exp_body_dec) = checkExp ftab new_vtab exp_body
        (t2, Let (Dec (name, exp_dec, pos1), exp_body_dec, pos2))

    | Read (t, pos) -> (t, Read (t, pos))

    | Write (e, _, pos) ->
        let (t, e') = checkExp ftab vtab e
        if printable t
        then (t, Write (e', t, pos))
        else raise (MyError ("Cannot write type " + ppType t, pos))

    | Index (s, i_exp, t, pos) ->
        let (e_type, i_exp_dec) = checkExp ftab vtab i_exp
        let arr_type =
            match SymTab.lookup s vtab with
              | Some (Array t) -> t
              | None       -> raise (MyError ("Unknown identifier " + s, pos))
              | Some other -> raise (MyError (s+" has type "+ppType other+": not an array", pos))
        (arr_type, Index (s, i_exp_dec, arr_type, pos))

    | Iota (n_exp, pos) ->
        let  (e_type, n_exp_dec) = checkExp ftab vtab n_exp
        if e_type = Int
        then (Array Int, Iota (n_exp_dec, pos))
        else raise (MyError ("Iota: wrong argument type "+ppType e_type, pos))

    | Reduce (f, n_exp, arr_exp, _, pos) ->
        let (n_type  , n_dec  ) = checkExp ftab vtab n_exp
        let (arr_type, arr_dec) = checkExp ftab vtab arr_exp
        let elem_type =
            match arr_type with
              | Array t -> t
              | other -> raise (MyError ("Reduce: Argument not an array", pos))
        let (f', f_arg_type) =
            match checkFunArg ftab vtab pos f with
              | (f', res, [a1; a2]) ->
                  if a1 = a2 && a2 = res
                  then (f', res)
                  else raise (MyError( "Reduce: incompatible function type of " +
                                       (ppFunArg 0 f) + ": " + showFunType ([a1; a2], res)
                                     , pos))
              | (_, res, args) ->
                  raise (MyError ( "Reduce: incompatible function type of " +
                                   ppFunArg 0 f + ": " + showFunType (args, res)
                                 , pos))
        let err (s, t) = MyError ( "Reduce: unexpected " + s + " type " + ppType t +
                                   ", expected " + ppType f_arg_type
                                 , pos)
        if   elem_type = f_arg_type && elem_type = n_type then
             (elem_type, Reduce (f', n_dec, arr_dec, elem_type, pos))
        elif elem_type = f_arg_type then
             raise (err ("neutral element", n_type))
        else raise (err ("array element", elem_type))

    (* TODO project task 2:
        See `AbSyn.fs` for the expression constructors of
        `Replicate`, `Map`, `Scan`.

        Hints for `replicate(n, a)`:
        - recursively type check `n` and `a`
        - check that `n` has integer type
        - assuming `a` is of type `t` the result type
          of replicate is `[t]`
    *)
    (* replicate :: Int -> a -> [a] *)
    // Added by Lemmih: Replicate
    | Replicate (n_exp, val_exp, _, pos) ->
        let (n_type  , n_dec  ) = checkExp ftab vtab n_exp
        let (val_type, val_dec  ) = checkExp ftab vtab val_exp
        match n_type with
          | Int -> (Array val_type, Replicate (n_dec, val_dec, val_type, pos))
          | otherwise -> failwith "Type error in replicate"

    (* TODO project task 2: Hint for `map(f, arr)`
        Look into the type-checking lecture slides for the type rule of `map`.
        Use `checkFunArg` to get the signature of the function argument `f`.
        If `f` has type `ta -> tb` then
         - `arr` should be of type `[ta]`
         - the result of `map` should have type `[tb]`
    *)
    // Added by Lemmih: Map
    | Map (fun_exp, arr_exp, _, _, pos) ->
        let (arr_type  , arr_dec  ) = checkExp ftab vtab arr_exp
        match arr_type with
          | Array aTy ->
              let (typed_fun, retTy, [fun_arg_type]) = checkFunArg ftab vtab pos fun_exp
              if fun_arg_type <> aTy
                then failwith "invalid arg type in map"
              (Array retTy, Map (typed_fun, arr_dec, fun_arg_type, retTy, pos))
          | otherwise -> failwith ("type error in map: " + ppType arr_type)

    (* TODO project task 2: `scan(f, ne, arr)`
        Hint: Implementation is very similar to `reduce(f, ne, arr)`.
              (The difference between `scan` and `reduce` is that
              scan's return type is the same as the type of `arr`,
              while reduce's return type is that of an element of `arr`).
    *)
    | Scan (f, n_exp, arr_exp, _, pos) ->
        let (n_type  , n_dec  ) = checkExp ftab vtab n_exp
        let (arr_type, arr_dec) = checkExp ftab vtab arr_exp
        let elem_type =
            match arr_type with
              | Array t -> t
              | other -> raise (MyError ("Scan: Argument not an array", pos))
        let (f', f_arg_type) =
            match checkFunArg ftab vtab pos f with
              | (f', res, [a1; a2]) ->
                  if a1 = a2 && a2 = res
                  then (f', res)
                  else raise (MyError( "Scan: incompatible function type of " +
                                       (ppFunArg 0 f) + ": " + showFunType ([a1; a2], res)
                                     , pos))
              | (_, res, args) ->
                  raise (MyError ( "Scan: incompatible function type of " +
                                   ppFunArg 0 f + ": " + showFunType (args, res)
                                 , pos))
        let err (s, t) = MyError ( "Scan: unexpected " + s + " type " + ppType t +
                                   ", expected " + ppType f_arg_type
                                 , pos)
        if   elem_type = f_arg_type && elem_type = n_type then
             (Array elem_type, Scan (f', n_dec, arr_dec, elem_type, pos))
        elif elem_type = f_arg_type then
             raise (err ("neutral element", n_type))
        else raise (err ("array element", elem_type))


and checkFunArg  (ftab : FunTable)
                 (vtab : VarTable)
                 (pos  : Position)
                 (ff   : UntypedFunArg)
               : (TypedFunArg * Type * Type list) =
  match ff with
  | FunName fname ->
    match SymTab.lookup fname ftab with
      | None -> raise (MyError ("Unknown identifier " + fname, pos))
      | Some (ret_type, arg_types, _) -> (FunName fname, ret_type, arg_types)
  | Lambda (rettype, parms, body, funpos) ->
    let lambda = FunDec ("<lambda>", rettype, parms, body, funpos)
    let (FunDec (_, _, _, body', _)) =
        checkFunWithVtable ftab vtab pos lambda
    ( Lambda (rettype, parms, body', pos)
    , rettype
    , List.map (fun (Param (_, ty)) -> ty) parms)


(* Check a function declaration, but using a given vtable rather
than an empty one. *)
and checkFunWithVtable  (ftab : FunTable)
                        (vtab : VarTable)
                        (pos  : Position)
                        (fdec : UntypedFunDec)
                      : TypedFunDec =
    let (FunDec (fname, rettype, parms, body, funpos)) = fdec
    (* Expand vtable by adding the parameters to vtab. *)
    let addParam ptable (Param (pname, ty)) =
            match SymTab.lookup pname ptable with
              | Some _ -> raise (MyError ("Multiple definitions of parameter name " + pname, funpos))
              | None   -> SymTab.bind pname ty ptable
    let paramtable = List.fold addParam (SymTab.empty()) parms
    let vtab' = SymTab.combine paramtable vtab
    let (body_type, body') = checkExp ftab vtab' body
    if body_type = rettype
    then (FunDec (fname, rettype, parms, body', pos))
    else raise (MyError ("Function declared to return type "
                         + ppType rettype
                         + " but body has type "
                         + ppType body_type, funpos))


(* Convert a funDec into the (fname, ([arg types], result type),
   pos) entries that the function table, ftab, consists of, and
   update the function table with that entry. *)
let updateFunctionTable  (ftab   : FunTable)
                         (fundec : UntypedFunDec)
                       : FunTable =
    let (FunDec (fname, ret_type, args, _, pos)) = fundec
    let arg_types = List.map (fun (Param (_, ty)) -> ty) args
    match SymTab.lookup fname ftab with
      | Some (_, _, old_pos) -> raise (MyError ("Duplicate function " + fname, pos))
      | None -> SymTab.bind fname (ret_type, arg_types, pos) ftab

(* Functions are guaranteed by syntax to have a known declared type.  This
   type is checked against the type of the function body, taking into
   account declared argument types and types of other functions called.
 *)
let checkFun  (ftab   : FunTable)
              (fundec : UntypedFunDec)
            : TypedFunDec =
    let (FunDec (_, _, _, _, pos)) = fundec
    checkFunWithVtable ftab (SymTab.empty()) pos fundec

let checkProg (funDecs : UntypedFunDec list) : TypedFunDec list =
    let ftab = List.fold updateFunctionTable initFunctionTable funDecs
    let decorated_funDecs = List.map (checkFun ftab) funDecs
    match SymTab.lookup "main" ftab with
      | None         -> raise (MyError ("No main function defined", (0,0)))
      | Some (_, [], _) -> decorated_funDecs  (* all fine! *)
      | Some (ret_type, args, mainpos) ->
        raise ( MyError("Unexpected argument to main: "+showFunType (args, ret_type)+
                        " (should be () -> <anything>)", mainpos) )

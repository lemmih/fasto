module DeadBindingRemoval

(*
    val removeDeadBindings : Fasto.KnownTypes.Prog -> Fasto.KnownTypes.Prog
*)

open AbSyn

type Usages = string list

let isUsed (name   : string)
           (usages : Usages) =
               List.exists (fun x -> x = name) usages

let used (name   : string)
         (usages : Usages) = name :: usages

let rec unzip3 = function
    | []         -> ([], [], [])
    | (x,y,z)::l ->
        let (xs, ys, zs) = unzip3 l
        (x::xs, y::ys, z::zs)

let anytrue = List.exists (fun x -> x)

let rec removeDeadBindingsInExp (e : TypedExp) =
    match e with
        | Constant  (x, pos) -> (false, [],  Constant (x, pos))
        | StringLit (x, pos) -> (false, [], StringLit (x, pos))
        | ArrayLit  (es, t, pos) ->
            let (ios, uses, es') = unzip3 (List.map removeDeadBindingsInExp es)
            (anytrue ios,
             List.concat uses,
             ArrayLit (es', t, pos))
        | Var (name, pos) ->
            (false, [name], Var (name, pos))
        | Plus (x, y, pos) ->
            let (xios, xuses, x') = removeDeadBindingsInExp x
            let (yios, yuses, y') = removeDeadBindingsInExp y
            (xios || yios,
             xuses @ yuses,
             Plus (x', y', pos))
        | Minus (x, y, pos) ->
            let (xios, xuses, x') = removeDeadBindingsInExp x
            let (yios, yuses, y') = removeDeadBindingsInExp y
            (xios || yios,
             xuses @ yuses,
             Minus (x', y', pos))
        | Equal (x, y, pos) ->
            let (xios, xuses, x') = removeDeadBindingsInExp x
            let (yios, yuses, y') = removeDeadBindingsInExp y
            (xios || yios,
             xuses @ yuses,
             Equal (x', y', pos))
        | Less (x, y, pos) ->
            let (xios, xuses, x') = removeDeadBindingsInExp x
            let (yios, yuses, y') = removeDeadBindingsInExp y
            (xios || yios,
             xuses @ yuses,
             Less (x', y', pos))
        | If (e1, e2, e3, pos) ->
            let (ios1, uses1, e1') = removeDeadBindingsInExp e1
            let (ios2, uses2, e2') = removeDeadBindingsInExp e2
            let (ios3, uses3, e3') = removeDeadBindingsInExp e3
            (ios1 || ios2 || ios3,
             uses1 @ uses2 @ uses3,
             If (e1', e2', e3', pos))
        | Apply (fname, args, pos) ->
            let (ios, uses, args') = unzip3 (List.map removeDeadBindingsInExp args)
            (anytrue ios,
             List.concat uses,
             Apply (fname, args', pos))
        | Let (Dec (name, e, decpos), body, pos) ->
            let (eio, euses, e') = removeDeadBindingsInExp e
            let (bodyio, bodyuses, body') = removeDeadBindingsInExp body
            if isUsed name bodyuses || eio
            then (eio || bodyio,
                  euses @ bodyuses,
                  Let (Dec (name, e', decpos), body', pos))
            else (bodyio,
                  bodyuses,
                  body')
        | Index (name, e, t, pos) ->
            let (io, uses, e') = removeDeadBindingsInExp e
            (io,
             name::uses,
             Index (name, e', t, pos))
        | Iota (e, pos) ->
            let (io, uses, e') = removeDeadBindingsInExp e
            (io,
             uses,
             Iota (e', pos))
        | Map (farg, e, t1, t2, pos) ->
            let (eio, euses, e') = removeDeadBindingsInExp e
            let (fio, fuses, farg') = removeDeadBindingsInFunArg farg
            (eio || fio,
             euses @ fuses,
             Map (farg', e', t1, t2, pos))
        | Reduce (farg, e1, e2, t, pos) ->
            let (io1, uses1, e1') = removeDeadBindingsInExp e1
            let (io2, uses2, e2') = removeDeadBindingsInExp e2
            let (fio, fuses, farg') = removeDeadBindingsInFunArg farg
            (io1 || io2 || fio,
             uses1 @ uses2 @ fuses,
             Reduce(farg', e1', e2', t, pos))
        | Replicate (n, e, t, pos) ->
            let (nio, nuses, n') = removeDeadBindingsInExp n
            let (eio, euses, e') = removeDeadBindingsInExp e
            (nio || eio,
             nuses @ euses,
             Replicate (n', e', t, pos))
        | Scan (farg, e1, e2, t, pos) ->
            let (io1, uses1, e1') = removeDeadBindingsInExp e1
            let (io2, uses2, e2') = removeDeadBindingsInExp e2
            let (fio, fuses, farg') = removeDeadBindingsInFunArg farg
            (io1 || io2 || fio,
             uses1 @ uses2 @ fuses,
             Scan(farg', e1', e2', t, pos))
        | Times (x, y, pos) ->
            let (xios, xuses, x') = removeDeadBindingsInExp x
            let (yios, yuses, y') = removeDeadBindingsInExp y
            (xios || yios,
             xuses @ yuses,
             Times (x', y', pos))
        | Divide (x, y, pos) ->
            let (xios, xuses, x') = removeDeadBindingsInExp x
            let (yios, yuses, y') = removeDeadBindingsInExp y
            (xios || yios,
             xuses @ yuses,
             Divide (x', y', pos))
        | And (x, y, pos) ->
            let (xios, xuses, x') = removeDeadBindingsInExp x
            let (yios, yuses, y') = removeDeadBindingsInExp y
            (xios || yios,
             xuses @ yuses,
             And (x', y', pos))
        | Or (x, y, pos) ->
            let (xios, xuses, x') = removeDeadBindingsInExp x
            let (yios, yuses, y') = removeDeadBindingsInExp y
            (xios || yios,
             xuses @ yuses,
             Or (x', y', pos))
        | Not (e, pos) ->
            let (ios, uses, e') = removeDeadBindingsInExp e
            (ios, uses, Not (e', pos))
        | Negate (e, pos) ->
            let (ios, uses, e') = removeDeadBindingsInExp e
            (ios, uses, Negate (e', pos))
        | Read (x, pos) ->
            (true, [], Read (x, pos))
        | Write (e, t, pos) ->
            let (_, uses, e') = removeDeadBindingsInExp e
            (true, uses, Write (e', t, pos))

and removeDeadBindingsInFunArg (farg : TypedFunArg) =
    match farg with
        | FunName fname -> (false, [], FunName fname)
        | Lambda (rettype, paramls, body, pos) ->
            let (io, uses, body') = removeDeadBindingsInExp body
            let isParam (x : string) = List.exists (fun (Param (pname, _)) ->
                                                    pname = x) paramls
            let uses' = List.filter isParam uses
            (io,
             uses',
             Lambda (rettype, paramls, body', pos))

let removeDeadBindingsInFunDec (FunDec (fname, rettype, paramls, body, pos)) =
    let (_, _, body') = removeDeadBindingsInExp body
    FunDec (fname, rettype, paramls, body', pos)

let removeDeadBindings (prog : TypedProg) =
    List.map removeDeadBindingsInFunDec prog


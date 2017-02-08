module CallGraph

type CallGraph = (string * string list) list


let callsOf (caller : string)
            (graph  : CallGraph) =
                match List.tryFind (fun (x,_) -> x = caller) graph with
                    | None            -> []
                    | Some (_, calls) -> calls

let calls (caller : string)
          (callee : string)
          (graph  : CallGraph) =
              List.exists (fun x -> x=callee) (callsOf caller graph)

open AbSyn


(* Remove duplicate elements in a list. Quite slow - O(n^2) -
   but our lists here will be small. *)
let rec nub = function
    | []    -> []
    | x::xs -> if List.exists (fun y -> y = x) xs
               then nub xs
               else x :: nub xs

let rec expCalls = function
    | Constant _ -> []
    | StringLit _ -> []
    | ArrayLit (es, _, _) -> List.concat (List.map expCalls es)
    | Var _ -> []
    | Plus (e1, e2, _) -> expCalls e1 @ expCalls e2
    | Minus (e1, e2, _) -> expCalls e1 @ expCalls e2
    | Equal (e1, e2, _) -> expCalls e1 @ expCalls e2
    | Less (e1, e2, _) -> expCalls e1 @ expCalls e2
    | If (e1, e2, e3, _) -> expCalls e1 @ expCalls e2 @ expCalls e3
    | Apply (fname, es, _) -> fname :: List.concat (List.map expCalls es)
    | Let ( Dec(_, e, _), body, _) -> expCalls e @ expCalls body
    | Index (_, e, _, _) -> expCalls e
    | Iota (e, _) -> expCalls e
    | Map (farg, e, _, _, _) -> fargCalls farg @ expCalls e
    | Reduce (farg, e1, e2, _, _) -> fargCalls farg @ expCalls e1 @ expCalls e2
    | Replicate (n, e, _, _) -> expCalls n @ expCalls e
    | Scan (farg, e1, e2, _, _) -> fargCalls farg @ expCalls e1 @ expCalls e2
    | Times (e1, e2, _) -> expCalls e1 @ expCalls e2
    | Divide (e1, e2, _) -> expCalls e1 @ expCalls e2
    | And (e1, e2, _) -> expCalls e1 @ expCalls e2
    | Or (e1, e2, _) -> expCalls e1 @ expCalls e2
    | Not (e, _) -> expCalls e
    | Negate (e, _) -> expCalls e
    | Read _ -> []
    | Write (e, _, _) -> expCalls e

and fargCalls = function
    | Lambda (_, _, body, _) -> expCalls body
    | FunName s -> [s]

(* Get the direct function calls of a single function *)

let functionCalls = function
    | FunDec (fname, _, _, body, _) -> (fname, nub (expCalls body))

(* Expand the direct function call graph to its transitive closure. *)
let rec transitiveClosure (graph : CallGraph) =
    let grow ((caller  : string),
              (callees : string list)) =
                 let calleecalls =
                     List.concat (List.map (fun callee ->
                                            callsOf callee graph) callees)
                 let newCalls = (List.filter (fun call ->
                                              not (List.exists (fun x -> x = call) callees)
                                              ) calleecalls)
                 if List.isEmpty newCalls
                 then ((caller, callees),
                       false)
                 else ((caller, callees @ nub newCalls),
                       true)
    let (graph', changes) = List.unzip (List.map grow graph)
    let changed = List.exists (fun x -> x) changes
    if changed
    then transitiveClosure graph'
    else graph'

let callGraph (prog : TypedProg) =
    transitiveClosure (List.map functionCalls prog)

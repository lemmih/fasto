module DeadFunctionRemoval

open AbSyn
open CallGraph

let removeDeadFunction (prog : TypedProg) =
    let graph = callGraph prog
    let alive (FunDec (fname, _, _, _, _)) =
        fname = "main" || calls "main" fname graph
    List.filter alive prog

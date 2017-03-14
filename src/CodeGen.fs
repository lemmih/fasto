(* Code generator for Fasto *)

module CodeGen

(*
    compile : TypedProg -> Mips.Instruction list

    (* for debugging *)
    compileExp : TypedExp
              -> SymTab<Mips.reg>
              -> Mips.reg
              -> Mips.Instruction list
*)

open AbSyn

exception MyError of string * Position

type VarTable = SymTab.SymTab<Mips.reg>

(* Name generator for labels and temporary symbolic registers *)
(* Example usage: val tmp = newName "tmp"  (* might produce _tmp_5_ *) *)
let counter : int ref = ref 0

let newName reg_name =
    let _ = counter := !counter + 1
    let n = string (!counter)
    "_" + reg_name + "_" + n + "_"

(* Number to text with "normal" sign symbol *)
let makeConst n = if n >= 0 then string n
                  else "-" + string (-n)

(* Table storing all string literals, with labels given to them *)
let stringTable : ((Mips.addr*string) list) ref = ref []
(* could also contain "\n", ",", "Index out of bounds in line ", but the
   format is a bit different (length and dummy pointer come first) *)

(* Building a string in the heap, including initialisation code *)
let buildString  ( label : Mips.addr
                 , str   : string
                 ) : (Mips.Instruction list * Mips.Instruction list) =
    let data = [ Mips.ALIGN "2"   (* means: word-align *)
               ; Mips.LABEL label (* pointer *)
               ; Mips.SPACE "4"   (* size(int) *)
               ; Mips.ASCIIZ str]
    let initR = label + "_init"
    let addrR = label + "_addr"
    let initcode = [ Mips.LA(addrR, label)
                   ; Mips.LI(initR, makeConst (String.length str))
                   ; Mips.SW(initR, addrR, makeConst 0) ]
    (initcode, data)

(* Link register *)
let RA = "31"
(* Register for stack pointer *)
let SP = "29"
(* Register for heap pointer *)
let HP = "28"

(* Suggested register division *)
let minReg = 2       (* lowest usable register *)
let maxCaller = 15   (* highest caller-saves register *)
let maxReg = 25      (* highest allocatable register *)

(* Determine the size of an element in an array based on its type *)
type ElemSize = One | Four

let getElemSize (tp : Type) : ElemSize =
    match tp with
      | Char  -> One
      | Bool  -> One
      | other -> Four

let elemSizeToInt (elmsz : ElemSize) : int =
    match elmsz with
      | One  -> 1
      | Four -> 4

(* Pick the correct store instruction from the element size. *)
let mipsStore elem_size = match elem_size with
                            | One  -> Mips.SB
                            | Four -> Mips.SW

(* generates the code to check that the array index is within bounds *)
let checkBounds  ( arr_beg : Mips.reg
                 , ind_reg : Mips.reg
                 , (line : int, c : int)
                 ) : Mips.Instruction list =
    let size_reg = newName "size_reg"
    let tmp_reg  = newName "tmp_reg"
    let err_lab  = newName "error_lab"
    let safe_lab = newName "safe_lab"
    [ Mips.LW(size_reg, arr_beg, "0")
    ; Mips.BGEZ(ind_reg, safe_lab)
    ; Mips.LABEL(err_lab)
    ; Mips.LI("5", makeConst line)
    ; Mips.J "_IllegalArrSizeError_"
    ; Mips.LABEL(safe_lab)
    ; Mips.SUB(tmp_reg, ind_reg, size_reg)
    ; Mips.BGEZ(tmp_reg, err_lab)
    ]

(* dynalloc(size_reg, place, ty) generates code for allocating arrays of heap
   memory by incrementing the HP register (heap pointer) by a number of words.
   The arguments for this function are as follows:

     size_reg: contains the logical array size (number of array elements)
     place: will contain the address of new allocation (old HP)
     ty: char/bool elements take 1 byte, int elements take 4 bytes
 *)
let dynalloc (size_reg : Mips.reg,
              place    : Mips.reg,
              ty       : Type     )
            : Mips.Instruction list =
    let tmp_reg = newName "tmp"

    (* Use old HP as allocation address. *)
    let code1 = [ Mips.MOVE (place, HP) ]

    (* For char/bool: Align address to 4-byte boundary by rounding up. *)
    (*                (By adding 3 and rounding down using SRA/SLL.) *)
    (* For int and arrays: Multiply logical size by 4, no alignment. *)
    let code2 =
        if ty = Char || ty = Bool
        then [ Mips.ADDI(tmp_reg, size_reg, "3")
             ; Mips.SRA (tmp_reg, tmp_reg, "2")
             ; Mips.SLL (tmp_reg, tmp_reg, "2") ]
        else [ Mips.SLL (tmp_reg, size_reg, "2") ]

    (* Make space for array size (+4). Increase HP. *)
    (* Save size of allocation in header. *)
    let code3 =
        [ Mips.ADDI (tmp_reg, tmp_reg, "4")
        ; Mips.ADD (HP, HP, tmp_reg)
        ; Mips.SW (size_reg, place, "0") ]

    code1 @ code2 @ code3

(* Pushing arguments on the stack: *)
(* For each register 'r' in 'rs', copy them to registers from
'firstReg' and counting up. Return the full code and the next unused
register (firstReg + num_args).  *)
let applyRegs  ( fid  : Mips.addr
               , args : Mips.reg list
               , place: Mips.reg
               , pos  : Position )
             : Mips.Instruction list =
    let regs_num = List.length args
    let caller_regs = List.map (fun n -> makeConst (n + minReg)) [0..regs_num-1]
                   // List.tabulate (regs_num, fun n -> makeConst (n + minReg))
        (* zipWith Mips.MOVE =
           zipWith (fun (regDest, regSrc) -> Mips.MOVE (regDest, regSrc)) *)
    let move_code = List.map Mips.MOVE (List.zip caller_regs args)
    if regs_num > maxCaller - minReg
    then raise (MyError("Number of arguments passed to " + fid +
                        " exceeds number of caller registers", pos))
    else move_code @ [ Mips.JAL(fid,caller_regs); Mips.MOVE(place, "2") ]


(* Compile 'e' under bindings 'vtable', putting the result in its 'place'. *)
let rec compileExp  (e      : TypedExp)
                    (vtable : VarTable)
                    (place  : Mips.reg)
                  : Mips.Instruction list =
  // Added by Lemmih: compileBinOp
  let compileBinOp op e1 e2 =
        let t1 = newName "op_L"
        let t2 = newName "op_R"
        let code1 = compileExp e1 vtable t1
        let code2 = compileExp e2 vtable t2
        code1 @ code2 @ [op (place,t1,t2)]
  // Added by Lemmih: compileUniOp
  let compileUniOp op e1 =
        let t1 = newName "op"
        let code1 = compileExp e1 vtable t1
        code1 @ [op (place,t1)]

  match e with
  | Constant (IntVal n, pos) ->
      if n < 0 then
          compileExp (Negate (Constant (IntVal (-n), pos), pos)) vtable place
      else if n < 32768 then
          [ Mips.LI (place, makeConst n) ]
      else
        [ Mips.LUI (place, makeConst (n / 65536))
        ; Mips.ORI (place, place, makeConst (n % 65536)) ]
  // Added by Lemmih: Constant BoolVal
  | Constant (BoolVal true, _) -> [Mips.LI (place, makeConst 1)]
  | Constant (BoolVal false, _) -> [Mips.LI (place, makeConst 0)]
  | Constant (CharVal c, pos) -> [ Mips.LI (place, makeConst (int c)) ]

  (* Create/return a label here, collect all string literals of the program
     (in stringTable), and create them in the data section before the heap
     (Mips.ASCIIZ) *)
  | StringLit (strLit, pos) ->
    (* Convert string literal into label; only use valid characters. *)
    let normalChars0 = //String.filter System.Char.IsLetterOrDigit strLit
            String.map (fun c -> if System.Char.IsLetterOrDigit c then c else 'a') strLit
    let normalChars  = normalChars0 + "__str__"
    let label = newName (normalChars.Substring (0, 7))
    let ()    = stringTable := (label, strLit)::(!stringTable)
    [ Mips.LA (place, label)
    ; Mips.COMMENT (label + ": string \"" + toCString strLit + "\"") ]

  | Constant (ArrayVal (vs, tp), pos) ->
    (* Create corresponding ArrayLit expression to re-use code. *)
    let arraylit = ArrayLit (List.map (fun v -> Constant (v, pos)) vs, tp, pos)
    compileExp arraylit vtable place

  | ArrayLit (elems, tp, pos) ->
    let elem_size = getElemSize tp
    let size_reg  = newName "size_reg"
    let addr_reg  = newName "addr_reg"
    let tmp_reg   = newName "tmp_reg"

    (* Store size of literal in size_reg, dynamically allocate that. *)
    (* Let addr_reg contain the address for the first array element. *)
    let header  = [ Mips.LI (size_reg, makeConst (List.length elems)) ]  @
                  dynalloc (size_reg, place, tp) @
                  [ Mips.ADDI (addr_reg, place, "4") ]

    let compileElem elem_exp =
            let elem_code = compileExp elem_exp vtable tmp_reg
            let storeInst = match elem_size with
                              | One  -> Mips.SB
                              | Four -> Mips.SW
            elem_code @
            [ storeInst (tmp_reg, addr_reg, "0")
            ; Mips.ADDI (addr_reg, addr_reg,
                         makeConst (elemSizeToInt elem_size)) ]

    let elems_code = List.concat (List.map compileElem elems)
    header @ elems_code

  | Var (vname, pos) ->
      match SymTab.lookup vname vtable with
        | None          -> raise (MyError ("Name " + vname + " not found", pos))
        | Some reg_name -> [Mips.MOVE (place, reg_name)]

  | Plus (e1, e2, pos) -> compileBinOp Mips.ADD e1 e2  // Added by Lemmih

  | Minus (e1, e2, pos) -> compileBinOp Mips.SUB e1 e2 // Added by Lemmih

  (* TODO project task 1:
        Look in `AbSyn.fs` for the expression constructors `Times`, ...
        `Times` and `Divide` are very similar to `Plus`/`Minus`
        `Not` and `Negate` are simpler; you can use `Mips.XORI` for `Not`
  *)
  | Times (e1, e2, pos) -> compileBinOp Mips.MUL e1 e2  // Added by Lemmih

  | Divide (e1, e2, pos) -> compileBinOp Mips.DIV e1 e2 // Added by Lemmih

  | Not (e1, pos) -> compileUniOp (fun (p,reg) -> Mips.XORI (p, reg, makeConst 1)) e1

  | Negate (e1, pos) -> compileExp (Minus (Constant (IntVal 0, pos), e1, pos)) vtable place

  | Let (dec, e1, pos) ->
      let (code1, vtable1) = compileDec dec vtable
      let code2 = compileExp e1 vtable1 place
      code1 @ code2

  | If (e1, e2, e3, pos) ->
      let thenLabel = newName "then"
      let elseLabel = newName "else"
      let endLabel = newName "endif"
      let code1 = compileCond e1 vtable thenLabel elseLabel
      let code2 = compileExp e2 vtable place
      let code3 = compileExp e3 vtable place
      code1 @ [Mips.LABEL thenLabel] @ code2  @
        [ Mips.J endLabel; Mips.LABEL elseLabel ] @
        code3 @ [Mips.LABEL endLabel]

  | Apply (f, args, pos) ->
      (* Convention: args in regs (2..15), result in reg 2 *)
      let compileArg arg =
            let arg_reg = newName "arg"
            (arg_reg, compileExp arg vtable arg_reg)
      let (arg_regs, argcode) = List.unzip (List.map compileArg args)
      let applyCode = applyRegs(f, arg_regs, place, pos)
      List.concat argcode @  (* Evaluate args *)
                  applyCode  (* Jump to function and store result in place *)

  (* dirty I/O. Read and Write: supported for basic types: Int, Char,
     Bool via system calls. Write of an Array(Chars) is also
     supported. The others are the user's responsibility.
  *)
  | Read(tp, pos) ->
      match tp with
        | Int  -> [ Mips.JAL ("getint",["2"])
                  ; Mips.MOVE(place,"2")
                  ]
        | Char -> [ Mips.JAL ("getchar", ["2"])
                  ; Mips.MOVE(place, "2")
                  ]
        | Bool ->
          let tl = newName "true_lab"
          let fl = newName "false_lab"
          let ml = newName "merge_lab"
          let v  = newName "bool_var"
          [ Mips.JAL ("getint", ["2"])
          ; Mips.MOVE(v, "2")
          ; Mips.BEQ (v,"0",fl)
          ; Mips.J tl
          ; Mips.LABEL fl
          ; Mips.MOVE(place, "0")
          ; Mips.J ml
          ; Mips.LABEL tl
          ; Mips.LI (place, "1")
          ; Mips.J ml
          ; Mips.LABEL ml
          ]
        | _ -> raise (MyError("Read on an incompatible type: " + ppType tp, pos))

  | Write(e, tp, pos) ->
    let tmp = newName "tmp"
    let codeexp = compileExp e vtable tmp @ [ Mips.MOVE (place, tmp) ]
    match tp with
      | Int  -> codeexp @ [ Mips.MOVE("2",place); Mips.JAL("putint", ["2"]) ]
      | Char -> codeexp @ [ Mips.MOVE("2",place); Mips.JAL("putchar",["2"]) ]
      | Bool ->
          let tlab = newName "wBoolF"
          codeexp @
           [ Mips.LA ("2","_true")
           ; Mips.BNE (place,"0",tlab)
           ; Mips.LA ("2","_false")
           ; Mips.LABEL tlab
           ; Mips.JAL("putstring", ["2"])
           ]

      | Array Char ->
           codeexp @ [ Mips.MOVE ("2", tmp)
                     ; Mips.JAL("putstring", ["2"]) ]

      | Array elem_type -> (* for Array(Int) and Array(Array(Int)) *)
           let arr_reg   = newName "arr_reg"
           let size_reg  = newName "size_reg"
           let tmp_reg   = newName "tmp_reg"
           let i_reg     = newName "ind_var"
           let loop_beg  = newName "loop_beg"
           let loop_end  = newName "loop_end"

           let header1 = [ Mips.LW(size_reg, place, "0")
                         ; Mips.ADDI(arr_reg, place, "4")
                         ; Mips.MOVE(i_reg,"0")
                         ; Mips.LABEL(loop_beg)
                         ; Mips.SUB(tmp_reg,i_reg,size_reg)
                         ; Mips.BGEZ(tmp_reg, loop_end)
                         ]

           let header2 = match getElemSize elem_type with
                           | One  -> [ Mips.LB(tmp_reg,arr_reg,"0")
                                     ; Mips.ADDI(arr_reg,arr_reg,"1")
                                     ]
                           | Four -> [ Mips.LW(tmp_reg,arr_reg,"0")
                                     ; Mips.ADDI(arr_reg,arr_reg,"4")
                                     ]

           (* create a fake Fasto node corresponding to the array elem
              and call compileExp recursively to generate code to print the
              element *)
           let elem_reg  = newName "ft_elem_reg"
           let new_vtab  = SymTab.bind elem_reg tmp_reg vtable
           let fastoexp : TypedExp = Write(Var(elem_reg, pos), elem_type, pos)
           let elem_code = compileExp fastoexp new_vtab tmp_reg

           codeexp @ header1 @ header2 @ elem_code @
            [ Mips.ADDI(i_reg, i_reg, "1")
            ; Mips.J loop_beg
            ; Mips.LABEL loop_end
            ]

  (* Comparison checking, later similar code for And and Or. *)
  | Equal (e1, e2, pos) ->
      let t1 = newName "eq_L"
      let t2 = newName "eq_R"
      let code1 = compileExp e1 vtable t1
      let code2 = compileExp e2 vtable t2
      let falseLabel = newName "false"
      code1 @ code2 @
       [ Mips.LI (place,"0")
       ; Mips.BNE (t1,t2,falseLabel)
       ; Mips.LI (place,"1")
       ; Mips.LABEL falseLabel
       ]

  | Less (e1, e2, pos) ->
      let t1 = newName"lt_L"
      let t2 = newName"lt_R"
      let code1 = compileExp e1 vtable t1
      let code2 = compileExp e2 vtable t2
      code1 @ code2 @ [Mips.SLT (place,t1,t2)]

  (* TODO project task 1:
        Look in `AbSyn.fs` for the expression constructors of `And` and `Or`.
        The implementation of `And` and `Or` is more complicated than `Plus`
        because you need to ensure the short-circuit semantics, e.g.,
        in `e1 || e2` if the execution of `e1` will evaluate to `true` then
        the code of `e2` must not be executed. Similar for `And` (&&).
  *)
  | And (e1, e2, pos) -> compileBinOp Mips.AND e1 e2 // Added by Lemmih

  | Or (e1, e2, pos) -> compileBinOp Mips.OR e1 e2   // Added by Lemmih

  (* Indexing:
     1. generate code to compute the index
     2. check index within bounds
     3. add the start address with the index
     4. get the element at that address
   *)
  | Index (arr_name, i_exp, ty, pos) ->
      let ind_reg  = newName "arr_ind"
      let ind_code = compileExp i_exp vtable ind_reg
      let arr_reg  = newName "arr_reg"

      (* Let arr_reg be the start of the data segment *)
      let arr_beg =
            match SymTab.lookup arr_name vtable with
              | None -> raise (MyError ("Name " + arr_name + " not found", pos))
              | Some reg_name -> reg_name
      let init_code = [ Mips.ADDI(arr_reg, arr_beg, "4") ]

      (* code to check bounds *)
      let check_code = checkBounds(arr_beg, ind_reg, pos)

      (* for INT/ARRAY: ind *= 4 else ind is unchanged *)
      (* array_var += index; place = *array_var *)
      let load_code =
            if ty = Char || ty = Bool
            then [ Mips.ADD(arr_reg, arr_reg, ind_reg)
                 ; Mips.LB(place, arr_reg, "0")
                 ]
            else [ Mips.SLL(ind_reg, ind_reg, "2")
                 ; Mips.ADD(arr_reg, arr_reg, ind_reg)
                 ; Mips.LW(place, arr_reg, "0")
                 ]
      ind_code @ init_code @ check_code @ load_code

  (* Second-Order Array Combinators (SOACs):
     iota, map, reduce
  *)
  | Iota (n_exp, (line, _)) ->
      let size_reg = newName "size_reg"
      let n_code = compileExp n_exp vtable size_reg
      (* size_reg is now the integer n. *)

      (* Check that array size N >= 0:
         if N - 1 >= 0 then jumpto safe_lab
         jumpto "_IllegalArrSizeError_"
         safe_lab: ...
      *)
      let safe_lab = newName "safe_lab"
      let checksize = [ Mips.ADDI (size_reg, size_reg, "-1")
                      ; Mips.BGEZ (size_reg, safe_lab)
                      ; Mips.LI ("5", makeConst line)
                      ; Mips.J "_IllegalArrSizeError_"
                      ; Mips.LABEL (safe_lab)
                      ; Mips.ADDI (size_reg, size_reg, "1")
                      ]

      let addr_reg = newName "addr_reg"
      let i_reg = newName "i_reg"
      let init_regs = [ Mips.ADDI (addr_reg, place, "4")
                      ; Mips.MOVE (i_reg, "0") ]
      (* addr_reg is now the position of the first array element. *)

      (* Run a loop.  Keep jumping back to loop_beg until it is not the
         case that i_reg < size_reg, and then jump to loop_end. *)
      let loop_beg = newName "loop_beg"
      let loop_end = newName "loop_end"
      let tmp_reg = newName "tmp_reg"
      let loop_header = [ Mips.LABEL (loop_beg)
                        ; Mips.SUB (tmp_reg, i_reg, size_reg)
                        ; Mips.BGEZ (tmp_reg, loop_end)
                        ]
      (* iota is just 'arr[i] = i'.  arr[i] is addr_reg. *)
      let loop_iota   = [ Mips.SW (i_reg, addr_reg, "0") ]
      let loop_footer = [ Mips.ADDI (addr_reg, addr_reg, "4")
                        ; Mips.ADDI (i_reg, i_reg, "1")
                        ; Mips.J loop_beg
                        ; Mips.LABEL loop_end
                        ]
      n_code
       @ checksize
       @ dynalloc (size_reg, place, Int)
       @ init_regs
       @ loop_header
       @ loop_iota
       @ loop_footer

  (* reduce(f, acc, {x1, x2, ...}) = f(..., f(x2, f(x1, acc))) *)
  | Reduce (binop, acc_exp, arr_exp, tp, pos) ->
      let arr_reg  = newName "arr_reg"   (* address of array *)
      let size_reg = newName "size_reg"  (* size of input array *)
      let i_reg    = newName "ind_var"   (* loop counter *)
      let tmp_reg  = newName "tmp_reg"   (* several purposes *)
      let loop_beg = newName "loop_beg"
      let loop_end = newName "loop_end"

      let arr_code = compileExp arr_exp vtable arr_reg
      let header1 = [ Mips.LW(size_reg, arr_reg, "0") ]

      (* Compile initial value into place (will be updated below) *)
      let acc_code = compileExp acc_exp vtable place

      (* Set arr_reg to address of first element instead. *)
      (* Set i_reg to 0. While i < size_reg, loop. *)
      let loop_code =
              [ Mips.ADDI(arr_reg, arr_reg, "4")
              ; Mips.MOVE(i_reg, "0")
              ; Mips.LABEL(loop_beg)
              ; Mips.SUB(tmp_reg, i_reg, size_reg)
              ; Mips.BGEZ(tmp_reg, loop_end)
              ]
      (* Load arr[i] into tmp_reg *)
      let load_code =
              match getElemSize tp with
                | One  -> [ Mips.LB   (tmp_reg, arr_reg, "0")
                          ; Mips.ADDI (arr_reg, arr_reg, "1")
                          ]
                | Four -> [ Mips.LW   (tmp_reg, arr_reg, "0")
                          ; Mips.ADDI (arr_reg, arr_reg, "4")
                          ]
          (* place := binop(tmp_reg, place) *)
      let apply_code =
              applyFunArg(binop, [place; tmp_reg], vtable, place, pos)

      arr_code @ header1 @ acc_code @ loop_code @ load_code @ apply_code @
         [ Mips.ADDI(i_reg, i_reg, "1")
         ; Mips.J loop_beg
         ; Mips.LABEL loop_end
         ]

  (* TODO project task 2:
        `replicate(n, a)`
        `map (f, arr)`
        `scan(f, ne, arr)`
     Look in `AbSyn.fs` for the shape of expression constructors
        `Replicate`, `Map`, `Scan`.
     General Hint: write down on a piece of paper the C-like pseudocode
        for implementing them, then translate that to Mips pseudocode.
     To allocate heap space for an array you may use `dynalloc` defined
        above. For example, if `sz_reg` is a register containing an integer `n`,
        and `ret_type` is the element-type of the to-be-allocated array, then
        `dynalloc (sz_reg, arr_reg, ret_type)` will alocate enough space for
        an n-element array of element-type `ret_type` (including the first
        word that holds the length, and the necessary allignment padding), and
        will place in register `arr_reg` the start address of the new array.
        Since you need to allocate space for the result arrays of `Replicate`,
        `Map` and `Scan`, then `arr_reg` should probably be `place` ...

     `replicate(n,a)`: You should allocate a new (result) array, and execute a
        loop of count `n`, in which you store the value hold into the register
        corresponding to `a` into each memory location corresponding to an
        element of the result array.
        If `n` is less than `0` then remember to terminate the program with
        an error -- see implementation of `iota`.
  *)
  | Replicate (n_exp, elt_exp, tp, (line, _)) ->
      let size_reg = newName "size_reg"
      let elt_reg = newName "size_reg"
      let n_code = compileExp n_exp vtable size_reg
      let exp_code = compileExp elt_exp vtable elt_reg
      (* size_reg is now the integer n. *)

      (* Check that array size N >= 0:
         if N - 1 >= 0 then jumpto safe_lab
         jumpto "_IllegalArrSizeError_"
         safe_lab: ...
      *)
      let safe_lab = newName "safe_lab"
      let checksize = [ Mips.ADDI (size_reg, size_reg, "-1")
                      ; Mips.BGEZ (size_reg, safe_lab)
                      ; Mips.LI ("5", makeConst line)
                      ; Mips.J "_IllegalArrSizeError_"
                      ; Mips.LABEL (safe_lab)
                      ; Mips.ADDI (size_reg, size_reg, "1")
                      ]

      let addr_reg = newName "addr_reg"
      let i_reg = newName "i_reg"
      let init_regs = [ Mips.ADDI (addr_reg, place, "4")
                      ; Mips.MOVE (i_reg, "0") ]
      (* addr_reg is now the position of the first array element. *)

      (* Run a loop.  Keep jumping back to loop_beg until it is not the
         case that i_reg < size_reg, and then jump to loop_end. *)
      let loop_beg = newName "loop_beg"
      let loop_end = newName "loop_end"
      let tmp_reg = newName "tmp_reg"
      let loop_header = [ Mips.LABEL (loop_beg)
                        ; Mips.SUB (tmp_reg, i_reg, size_reg)
                        ; Mips.BGEZ (tmp_reg, loop_end)
                        ]
      (* replicate is just 'arr[i] = elt'. *)
      let loop_rep    = [ Mips.SW (elt_reg, addr_reg, "0") ]
      let loop_advance =
              match getElemSize tp with
                | One  -> [ Mips.ADDI (addr_reg, addr_reg, "1")
                          ]
                | Four -> [ Mips.ADDI (addr_reg, addr_reg, "4")
                          ]
      let loop_footer = [ Mips.ADDI (i_reg, i_reg, "1")
                        ; Mips.J loop_beg
                        ; Mips.LABEL loop_end
                        ]
      n_code
       @ checksize
       @ dynalloc (size_reg, place, Int)
       @ init_regs
       @ loop_header
       @ loop_rep
       @ loop_advance
       @ loop_footer

  (* TODO project task 2: see also the comment to replicate.
     `map(f, arr)`:  has some similarity with the implementation of iota.
     Use `applyFunArg` to call `f(a)` in a loop, for every element `a` of `arr`.
     It is useful to maintain two array iterators: one for the input array `arr`
     and one for the result array.
  *)
  (*
    n = size of lst
    allocate new array
    loop over array:
      tmp = old_arr[i]
      arr[i] = f(tmp)
  *)
  | Map (unop, arr_exp, arr_tp, ret_tp, pos) ->
      let tmp_elt_reg = newName "tmp_elt_reg"
      let tmp_ret_reg = newName "tmp_ret_reg"
      let size_reg = newName "size_reg"
      let arr_reg  = newName "arr_reg"   (* address of array *)
      let arr_code = compileExp arr_exp vtable arr_reg
      let header1 = [ Mips.LW(size_reg, arr_reg, "0")
                    ; Mips.ADDI (arr_reg, arr_reg, "4") ]
      (* size_reg is now the integer n. *)

      let addr_reg = newName "addr_reg"
      let i_reg = newName "i_reg"
      let init_regs = [ Mips.ADDI (addr_reg, place, "4")
                      ; Mips.MOVE (i_reg, "0") ]
      (* addr_reg is now the position of the first array element. *)

      (* Run a loop.  Keep jumping back to loop_beg until it is not the
         case that i_reg < size_reg, and then jump to loop_end. *)
      let loop_beg = newName "loop_beg"
      let loop_end = newName "loop_end"
      let tmp_reg = newName "tmp_reg"
      let loop_header = [ Mips.LABEL (loop_beg)
                        ; Mips.SUB (tmp_reg, i_reg, size_reg)
                        ; Mips.BGEZ (tmp_reg, loop_end)
                        ]
      (* iota is just 'arr[i] = i'.  arr[i] is addr_reg. *)
      let loop_map   = [ Mips.SW (i_reg, addr_reg, "0") ]
      let load_code dst src ty =
              match getElemSize ty with
                | One  -> [ Mips.LB   (dst, src, "0")
                          ; Mips.ADDI (src, src, "1")
                          ]
                | Four -> [ Mips.LW   (dst, src, "0")
                          ; Mips.ADDI (src, src, "4")
                          ]
      let store_code dst src ty =
              match getElemSize ty with
                | One  -> [ Mips.SB   (src, dst, "0")
                          ; Mips.ADDI (dst, dst, "1")
                          ]
                | Four -> [ Mips.SW   (src, dst, "0")
                          ; Mips.ADDI (dst, dst, "4")
                          ]
      (* tmp_elt := arr++;
         tmp_ret := binop(tmp_elt)
         addr++; := tmp_ret
      *)
      let loop_map =
            load_code tmp_elt_reg arr_reg arr_tp @
            applyFunArg(unop, [tmp_elt_reg], vtable, tmp_ret_reg, pos) @
            store_code addr_reg tmp_elt_reg ret_tp
      let loop_footer = [ Mips.ADDI (i_reg, i_reg, "1")
                        ; Mips.J loop_beg
                        ; Mips.LABEL loop_end
                        ]
      arr_code
       @ header1
       @ dynalloc (size_reg, place, ret_tp)
       @ init_regs
       @ loop_header
       @ loop_map
       @ loop_footer


  (* TODO project task 2: see also the comment to replicate.
     `scan(f, ne, arr)`: you can inspire yourself from the implementation of
        `reduce`, but in the case of `scan` you will need to also maintain
        an iterator through the result array, and write the accumulator in
        the current location of the result iterator at every iteration of
        the loop.
  *)
  | Scan (_, _, _, _, _) ->
      failwith "Unimplemented code generation of scan"

and applyFunArg ( ff     : TypedFunArg
                , args   : Mips.reg list
                , vtable : VarTable
                , place  : Mips.reg
                , pos    : Position
                ) : Mips.Instruction list =
  match ff with
    | FunName s ->
          let tmp_reg = newName "tmp_reg"
          applyRegs(s, args, tmp_reg, pos) @ [Mips.MOVE(place, tmp_reg)]

    | Lambda (_, parms, body, lampos) ->
          let rec bindParams parms args vtable' =
              match (parms, args) with
                | (Param (pname,_)::parms', arg::args') ->
                      bindParams parms' args' (SymTab.bind pname arg vtable')
                | _ -> vtable'
          let vtable' = bindParams parms args vtable
          let t = newName "fun_arg_res"
          compileExp body vtable' t @ [ Mips.MOVE(place, t) ]

(* compile condition *)
and compileCond  (c      : TypedExp)
                 (vtable : VarTable)
                 (tlab   : Mips.addr)
                 (flab   : Mips.addr)
               : Mips.Instruction list =
  let t1 = newName "cond"
  let code1 = compileExp c vtable t1
  code1 @ [Mips.BNE (t1, "0", tlab); Mips.J flab]

(* compile let declaration *)
and compileDec  (dec : TypedDec)
                (vtable : VarTable)
              : (Mips.Instruction list * VarTable) =
      let (Dec (s,e,pos)) = dec
      let t = newName "letBind"
      let code = compileExp e vtable t
      let new_vtable = SymTab.bind s t vtable
      (code, new_vtable)

(* code for saving and restoring callee-saves registers *)
let rec stackSave (currentReg  : int)
                  (maxReg      : int)
                  (savecode    : Mips.Instruction list)
                  (restorecode : Mips.Instruction list)
                  (offset      : int)
                : (Mips.Instruction list * Mips.Instruction list * int) =
    if currentReg > maxReg
    then (savecode, restorecode, offset)  (* done *)
    else stackSave (currentReg+1)
                   maxReg
                   (Mips.SW (makeConst currentReg,
                               SP,
                               makeConst offset)
                    :: savecode) (* save register *)
                   (Mips.LW (makeConst currentReg,
                               SP,
                               makeConst offset)
                    :: restorecode) (* restore register *)
                   (offset-4) (* adjust offset *)

(* add function arguments to symbol table *)
and getArgs  (parms   : Param list)
             (vtable  : VarTable)
             (nextReg : int)
           : (Mips.Instruction list * VarTable) =
  match parms with
    | [] -> ([], vtable)
    | (Param (v,_)::vs) ->
       if nextReg > maxCaller
       then raise (MyError ("Passing too many arguments!", (0,0)))
       else let vname = newName ("param_" + v)
            let vtable1 = SymTab.bind v vname vtable (*   (v,vname)::vtable   *)
            let (code2,vtable2) = getArgs vs vtable1 (nextReg + 1)
            ([Mips.MOVE (vname, makeConst nextReg)] @ code2, vtable2)

(* compile function declaration *)
and compileFun (fundec : TypedFunDec) : Mips.Prog =
    let (FunDec (fname, resty, args, exp, (line,col))) = fundec
    (* make a vtable from bound formal parameters,
         then evaluate expression in this context, return it *)
    (* arguments passed in registers, "move" into local vars. *)
    let (argcode, vtable_local) = getArgs args (SymTab.empty ()) minReg
    (* return value in register 2 *)
    let rtmp = newName (fname + "res")
    let returncode  = [Mips.MOVE ("2",rtmp)] (* move return val to R2 *)
    let body = compileExp exp vtable_local rtmp (* target expr *)
    let (body1, _, maxr, spilled) =
            RegAlloc.registerAlloc   (* call register allocator *)
                       (argcode @ body @ returncode)
                       (Set.singleton "2") 2 maxCaller maxReg 0
    let (savecode, restorecode, offset) = (* save/restore callee-saves *)
            stackSave (maxCaller+1) maxr [] [] (-8 + (-4 * spilled))
    [Mips.COMMENT ("Function " + fname);
     Mips.LABEL fname;       (* function label *)
     Mips.SW (RA, SP, "-4")] (* save return address *)
  @ savecode                 (* save callee-saves registers *)
  @ [Mips.ADDI (SP,SP,makeConst offset)]   (* SP adjustment *)
  @ body1                    (* code for function body *)
  @ [Mips.ADDI (SP,SP,makeConst (-offset))] (* move SP up *)
  @ restorecode              (* restore callee-saves registers *)
  @ [Mips.LW (RA, SP, "-4"); (* restore return addr *)
     Mips.JR (RA, [])]       (* return *)


(* compile program *)
let compile (funs : TypedProg) : Mips.Instruction list =
  let () = stringTable := [("_true","true"); ("_false","false")]
  let funsCode = List.concat (List.map compileFun funs)
  let (stringinit_sym, stringdata) =
      List.unzip (List.map buildString (!stringTable))
  let (stringinit,_,_,_) =
        match stringinit_sym with
            | [] -> ([],Set.empty,0,0)
            | _ -> RegAlloc.registerAlloc (* call register allocator *)
                     (List.concat stringinit_sym)
                     (Set.singleton "2") 2 maxCaller maxReg 0
  let mips_prog =
      [Mips.TEXT "0x00400000";
       Mips.GLOBL "main"]
      (* initialisation: heap pointer and string pointers *)
    @ (Mips.LA (HP, "_heap_"):: stringinit)
      (* jump to main (and stop after returning) *)
    @ [Mips.JAL ("main",[])]
    @ (* stop code *)
      [Mips.LABEL "_stop_";
       Mips.LI ("2","10"); (* syscall exit = 10 *)
       Mips.SYSCALL]
    @ (* code for functions *)
      funsCode
      (* pre-defined ord: char -> int and chr: int -> char *)
    @ [Mips.LABEL "ord"; (* char returned unmodified, interpreted as int *)
       Mips.JR (RA,[]);
       Mips.LABEL "chr"; (* int values are truncated to 8 bit (ASCII), *)
       Mips.ANDI ("2", "2", makeConst 255);
       Mips.JR (RA,[])]
      (* built-in read and write functions *)
    @ [Mips.LABEL "putint";     (* putint function *)
       Mips.ADDI(SP,SP,"-8");
       Mips.SW ("2",SP,"0");    (* save used registers *)
       Mips.SW ("4",SP,"4");
       Mips.MOVE ("4","2");     (* convention: number to be written in r2 *)
       Mips.LI ("2","1");       (* write_int syscall *)
       Mips.SYSCALL;
       Mips.LI ("2","4");       (* writestring syscall *)
       Mips.LA("4","_space_");
       Mips.SYSCALL;            (* write CR *)
       Mips.LW ("2",SP,"0");    (* reload used registers *)
       Mips.LW ("4",SP,"4");
       Mips.ADDI(SP,SP,"8");
       Mips.JR (RA,[]);

       Mips.LABEL "getint";     (* getint function *)
       Mips.LI ("2","5");       (* read_int syscall *)
       Mips.SYSCALL;
       Mips.JR (RA,[])]
    @ (* putchar *)
      [ Mips.LABEL "putchar";
        Mips.ADDI(SP,SP,"-8");   (* make space for 2 registers on the stack *)
        Mips.SW ("2",SP,"0");    (* save registers $2 and $4 to stack *)
        Mips.SW ("4",SP,"4");
        Mips.MOVE ("4","2");     (* put char in $4 for syscall to work on *)
        Mips.LI("2", "11");      (* put syscall 11 (= putchar) in $2 *)
        Mips.SYSCALL;
        Mips.LI ("2","4");       (* put syscall 4 (= putstring) in $2 *)
        Mips.LA("4","_space_");  (* the string we'll write is a space *)
        Mips.SYSCALL;
        Mips.LW ("2",SP,"0");    (* reload registers $2 and $4 from stack *)
        Mips.LW ("4",SP,"4");
        Mips.ADDI(SP,SP,"8");    (* free stack space again *)
        Mips.JR (RA,[])
      ]
    @ (* getchar *)
      [ Mips.LABEL "getchar";
        Mips.ADDI(SP,SP,"-8");   (* make space for 2 registers on the stack *)
        Mips.SW ("4",SP,"0");    (* save registers $4 and $5 to stack *)
        Mips.SW ("5",SP,"4");
        Mips.LI("2", "12");      (* put syscall 12 (= getchar) in $2 *)
        Mips.SYSCALL;
        Mips.MOVE("5","2");      (* temporarily move the result in reg $5*)
        Mips.LI ("2","4");       (* writestring syscall *)
        Mips.LA("4","_cr_");
        Mips.SYSCALL;            (* write CR *)
        Mips.MOVE("2", "5");     (* put the result back in $2*)
        Mips.LW ("4", SP, "0");  (* restore registers *)
        Mips.LW ("5", SP, "4");
        Mips.ADDI(SP,SP,"8");    (* free stack space again *)
        Mips.JR (RA,[])
      ]
    @ (* putstring *)
      [ Mips.LABEL "putstring";
        Mips.ADDI(SP,  SP, "-16");   (* make space on stack for registers *)
        Mips.SW  ("2", SP, "0");     (* save registers $2,$4,$5,$6 to stack *)
        Mips.SW  ("4", SP, "4");
        Mips.SW  ("5", SP, "8");
        Mips.SW  ("6", SP, "12");
        Mips.LW  ("4", "2", "0");    (* $4 := size($2)    *)
        Mips.ADDI("5", "2", "4");    (* $5 := $4 + 4      *)
        Mips.ADD ("6", "5", "4");    (* $6 := $4 + $5     *)
        Mips.LI  ("2", "11");        (* put syscall 11 (= putchar) in $2 *)
        Mips.LABEL "putstring_begin";
        Mips.SUB ("4", "5", "6");         (* $4 := $5 - $6     *)
        Mips.BGEZ("4", "putstring_done"); (* while ($4 > 0) {  *)
          Mips.LB("4", "5", "0");         (*   $4 := M[$5]     *)
          Mips.SYSCALL;                   (*   putchar($4)     *)
          Mips.ADDI("5", "5", "1");       (*   $5 := $5 + 1    *)
          Mips.J "putstring_begin";       (* }                 *)
        Mips.LABEL "putstring_done";
        Mips.LW ("2", SP, "0");      (* restore registers $2,$4,$5,$6 *)
        Mips.LW ("4", SP, "4");
        Mips.LW ("5", SP, "8");
        Mips.LW ("6", SP, "12");
        Mips.ADDI(SP, SP, "16");     (* free stack space again *)
        Mips.JR (RA,[])
      ]
    @ (* fixed error code for indexing errors *)
      [Mips.LABEL "_IllegalArrSizeError_";
       Mips.LA ("4","_IllegalArrSizeString_");
       Mips.LI ("2","4"); Mips.SYSCALL; (* print string *)
       Mips.MOVE ("4","5");
       Mips.LI ("2","1"); Mips.SYSCALL; (* print line number *)
       Mips.LA ("4","_cr_");
       Mips.LI ("2","4"); Mips.SYSCALL; (* print CR *)
       Mips.J "_stop_";
      (* Fixed data (for error message) *)
       Mips.DATA "";
       Mips.LABEL "_cr_";       (* carriage return string *)
       Mips.ASCIIZ "\n";
       Mips.LABEL "_space_";
       Mips.ASCIIZ " ";
       Mips.LABEL "_IllegalArrSizeString_";
       Mips.ASCIIZ "Error: Array size less or equal to 0 at line "]
      (* String literals *)
     @ (Mips.COMMENT "String Literals" ::
        List.concat stringdata)
      (* Heap (to allocate arrays in, word-aligned) *)
     @ [Mips.ALIGN "2";
        Mips.LABEL "_heap_";
        Mips.SPACE "100000"]
  mips_prog

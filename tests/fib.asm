	.text	0x00400000
	.globl	main
	la	$28, _heap_
	la	$11, _true
# was:	la	_true_addr, _true
	ori	$10, $0, 4
# was:	ori	_true_init, 0, 4
	sw	$10, 0($11)
# was:	sw	_true_init, 0(_true_addr)
	la	$10, _false
# was:	la	_false_addr, _false
	ori	$11, $0, 5
# was:	ori	_false_init, 0, 5
	sw	$11, 0($10)
# was:	sw	_false_init, 0(_false_addr)
	jal	main
_stop_:
	ori	$2, $0, 10
	syscall
# Function fibo
fibo:
	sw	$31, -4($29)
	sw	$17, -12($29)
	sw	$16, -8($29)
	addi	$29, $29, -16
	ori	$16, $2, 0
# was:	ori	_param_n_1_, 2, 0
# 	ori	_eq_L_7_,_param_n_1_,0
	ori	$11, $0, 0
# was:	ori	_eq_R_8_, 0, 0
	ori	$10, $0, 0
# was:	ori	_cond_6_, 0, 0
	bne	$16, $11, _false_9_
# was:	bne	_eq_L_7_, _eq_R_8_, _false_9_
	ori	$10, $0, 1
# was:	ori	_cond_6_, 0, 1
_false_9_:
	bne	$10, $0, _then_3_
# was:	bne	_cond_6_, 0, _then_3_
	j	_else_4_
_then_3_:
	ori	$2, $0, 0
# was:	ori	_fibores_2_, 0, 0
	j	_endif_5_
_else_4_:
# 	ori	_eq_L_14_,_param_n_1_,0
	ori	$11, $0, 1
# was:	ori	_eq_R_15_, 0, 1
	ori	$10, $0, 0
# was:	ori	_cond_13_, 0, 0
	bne	$16, $11, _false_16_
# was:	bne	_eq_L_14_, _eq_R_15_, _false_16_
	ori	$10, $0, 1
# was:	ori	_cond_13_, 0, 1
_false_16_:
	bne	$10, $0, _then_10_
# was:	bne	_cond_13_, 0, _then_10_
	j	_else_11_
_then_10_:
	ori	$2, $0, 1
# was:	ori	_fibores_2_, 0, 1
	j	_endif_12_
_else_11_:
# 	ori	_op_L_20_,_param_n_1_,0
	ori	$10, $0, 1
# was:	ori	_op_R_21_, 0, 1
	sub	$2, $16, $10
# was:	sub	_arg_19_, _op_L_20_, _op_R_21_
# 	ori	2,_arg_19_,0
	jal	fibo
# was:	jal	fibo, 2
	ori	$17, $2, 0
# was:	ori	_op_L_17_, 2, 0
# 	ori	_op_L_23_,_param_n_1_,0
	ori	$10, $0, 2
# was:	ori	_op_R_24_, 0, 2
	sub	$2, $16, $10
# was:	sub	_arg_22_, _op_L_23_, _op_R_24_
# 	ori	2,_arg_22_,0
	jal	fibo
# was:	jal	fibo, 2
# 	ori	_op_R_18_,2,0
	add	$2, $17, $2
# was:	add	_fibores_2_, _op_L_17_, _op_R_18_
_endif_12_:
_endif_5_:
# 	ori	2,_fibores_2_,0
	addi	$29, $29, 16
	lw	$17, -12($29)
	lw	$16, -8($29)
	lw	$31, -4($29)
	jr	$31
# Function main
main:
	sw	$31, -4($29)
	sw	$16, -8($29)
	addi	$29, $29, -12
	jal	getint
# was:	jal	getint, 2
# 	ori	_letBind_26_,2,0
# 	ori	_arg_28_,_letBind_26_,0
# 	ori	2,_arg_28_,0
	jal	fibo
# was:	jal	fibo, 2
	ori	$16, $2, 0
# was:	ori	_tmp_27_, 2, 0
# 	ori	_mainres_25_,_tmp_27_,0
	ori	$2, $16, 0
# was:	ori	2, _mainres_25_, 0
	jal	putint
# was:	jal	putint, 2
	ori	$2, $16, 0
# was:	ori	2, _mainres_25_, 0
	addi	$29, $29, 12
	lw	$16, -8($29)
	lw	$31, -4($29)
	jr	$31
ord:
	jr	$31
chr:
	andi	$2, $2, 255
	jr	$31
putint:
	addi	$29, $29, -8
	sw	$2, 0($29)
	sw	$4, 4($29)
	ori	$4, $2, 0
	ori	$2, $0, 1
	syscall
	ori	$2, $0, 4
	la	$4, _space_
	syscall
	lw	$2, 0($29)
	lw	$4, 4($29)
	addi	$29, $29, 8
	jr	$31
getint:
	ori	$2, $0, 5
	syscall
	jr	$31
putchar:
	addi	$29, $29, -8
	sw	$2, 0($29)
	sw	$4, 4($29)
	ori	$4, $2, 0
	ori	$2, $0, 11
	syscall
	ori	$2, $0, 4
	la	$4, _space_
	syscall
	lw	$2, 0($29)
	lw	$4, 4($29)
	addi	$29, $29, 8
	jr	$31
getchar:
	addi	$29, $29, -8
	sw	$4, 0($29)
	sw	$5, 4($29)
	ori	$2, $0, 12
	syscall
	ori	$5, $2, 0
	ori	$2, $0, 4
	la	$4, _cr_
	syscall
	ori	$2, $5, 0
	lw	$4, 0($29)
	lw	$5, 4($29)
	addi	$29, $29, 8
	jr	$31
putstring:
	addi	$29, $29, -16
	sw	$2, 0($29)
	sw	$4, 4($29)
	sw	$5, 8($29)
	sw	$6, 12($29)
	lw	$4, 0($2)
	addi	$5, $2, 4
	add	$6, $5, $4
	ori	$2, $0, 11
putstring_begin:
	sub	$4, $5, $6
	bgez	$4, putstring_done
	lb	$4, 0($5)
	syscall
	addi	$5, $5, 1
	j	putstring_begin
putstring_done:
	lw	$2, 0($29)
	lw	$4, 4($29)
	lw	$5, 8($29)
	lw	$6, 12($29)
	addi	$29, $29, 16
	jr	$31
_IllegalArrSizeError_:
	la	$4, _IllegalArrSizeString_
	ori	$2, $0, 4
	syscall
	ori	$4, $5, 0
	ori	$2, $0, 1
	syscall
	la	$4, _cr_
	ori	$2, $0, 4
	syscall
	j	_stop_
	.data	
_cr_:
	.asciiz	"\n"
_space_:
	.asciiz	" "
_IllegalArrSizeString_:
	.asciiz	"Error: Array size less or equal to 0 at line "
# String Literals
	.align	2
_true:
	.space	4
	.asciiz	"true"
	.align	2
_false:
	.space	4
	.asciiz	"false"
	.align	2
_heap_:
	.space	100000
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
# Function incr
incr:
	sw	$31, -4($29)
	addi	$29, $29, -8
# 	ori	_param_a_1_,2,0
# 	ori	_param_b_2_,3,0
# 	ori	_op_L_4_,_param_a_1_,0
# 	ori	_op_R_5_,_param_b_2_,0
	add	$2, $2, $3
# was:	add	_incrres_3_, _op_L_4_, _op_R_5_
# 	ori	2,_incrres_3_,0
	addi	$29, $29, 8
	lw	$31, -4($29)
	jr	$31
# Function main
main:
	sw	$31, -4($29)
	sw	$18, -16($29)
	sw	$17, -12($29)
	sw	$16, -8($29)
	addi	$29, $29, -20
	ori	$11, $0, 3
# was:	ori	_size_reg_14_, 0, 3
	ori	$16, $28, 0
# was:	ori	_arr_reg_8_, 28, 0
	sll	$10, $11, 2
# was:	sll	_tmp_17_, _size_reg_14_, 2
	addi	$10, $10, 4
# was:	addi	_tmp_17_, _tmp_17_, 4
	add	$28, $28, $10
# was:	add	28, 28, _tmp_17_
	sw	$11, 0($16)
# was:	sw	_size_reg_14_, 0(_arr_reg_8_)
	addi	$11, $16, 4
# was:	addi	_addr_reg_15_, _arr_reg_8_, 4
	ori	$10, $0, 1
# was:	ori	_tmp_reg_16_, 0, 1
	sw	$10, 0($11)
# was:	sw	_tmp_reg_16_, 0(_addr_reg_15_)
	addi	$11, $11, 4
# was:	addi	_addr_reg_15_, _addr_reg_15_, 4
	ori	$10, $0, 2
# was:	ori	_tmp_reg_16_, 0, 2
	sw	$10, 0($11)
# was:	sw	_tmp_reg_16_, 0(_addr_reg_15_)
	addi	$11, $11, 4
# was:	addi	_addr_reg_15_, _addr_reg_15_, 4
	ori	$10, $0, 3
# was:	ori	_tmp_reg_16_, 0, 3
	sw	$10, 0($11)
# was:	sw	_tmp_reg_16_, 0(_addr_reg_15_)
	addi	$11, $11, 4
# was:	addi	_addr_reg_15_, _addr_reg_15_, 4
	lw	$17, 0($16)
# was:	lw	_size_reg_9_, 0(_arr_reg_8_)
	ori	$2, $0, 0
# was:	ori	_letBind_7_, 0, 0
	addi	$16, $16, 4
# was:	addi	_arr_reg_8_, _arr_reg_8_, 4
	ori	$18, $0, 0
# was:	ori	_ind_var_10_, 0, 0
_loop_beg_12_:
	sub	$3, $18, $17
# was:	sub	_tmp_reg_11_, _ind_var_10_, _size_reg_9_
	bgez	$3, _loop_end_13_
# was:	bgez	_tmp_reg_11_, _loop_end_13_
	lw	$3, 0($16)
# was:	lw	_tmp_reg_11_, 0(_arr_reg_8_)
	addi	$16, $16, 4
# was:	addi	_arr_reg_8_, _arr_reg_8_, 4
# 	ori	2,_letBind_7_,0
# 	ori	3,_tmp_reg_11_,0
	jal	incr
# was:	jal	incr, 2 3
# 	ori	_tmp_reg_18_,2,0
# 	ori	_letBind_7_,_tmp_reg_18_,0
	addi	$18, $18, 1
# was:	addi	_ind_var_10_, _ind_var_10_, 1
	j	_loop_beg_12_
_loop_end_13_:
	ori	$16, $2, 0
# was:	ori	_tmp_19_, _letBind_7_, 0
# 	ori	_mainres_6_,_tmp_19_,0
	ori	$2, $16, 0
# was:	ori	2, _mainres_6_, 0
	jal	putint
# was:	jal	putint, 2
	ori	$2, $16, 0
# was:	ori	2, _mainres_6_, 0
	addi	$29, $29, 20
	lw	$18, -16($29)
	lw	$17, -12($29)
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
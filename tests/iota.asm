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
# Function main
main:
	sw	$31, -4($29)
	sw	$16, -8($29)
	addi	$29, $29, -12
	ori	$10, $0, 7
# was:	ori	_size_reg_3_, 0, 7
	addi	$10, $10, -1
# was:	addi	_size_reg_3_, _size_reg_3_, -1
	bgez	$10, _safe_lab_4_
# was:	bgez	_size_reg_3_, _safe_lab_4_
	ori	$5, $0, 2
# was:	ori	5, 0, 2
	j	_IllegalArrSizeError_
_safe_lab_4_:
	addi	$10, $10, 1
# was:	addi	_size_reg_3_, _size_reg_3_, 1
	ori	$16, $28, 0
# was:	ori	_letBind_2_, 28, 0
	sll	$11, $10, 2
# was:	sll	_tmp_10_, _size_reg_3_, 2
	addi	$11, $11, 4
# was:	addi	_tmp_10_, _tmp_10_, 4
	add	$28, $28, $11
# was:	add	28, 28, _tmp_10_
	sw	$10, 0($16)
# was:	sw	_size_reg_3_, 0(_letBind_2_)
	addi	$12, $16, 4
# was:	addi	_addr_reg_5_, _letBind_2_, 4
	ori	$11, $0, 0
# was:	ori	_i_reg_6_, 0, 0
_loop_beg_7_:
	sub	$13, $11, $10
# was:	sub	_tmp_reg_9_, _i_reg_6_, _size_reg_3_
	bgez	$13, _loop_end_8_
# was:	bgez	_tmp_reg_9_, _loop_end_8_
	sw	$11, 0($12)
# was:	sw	_i_reg_6_, 0(_addr_reg_5_)
	addi	$12, $12, 4
# was:	addi	_addr_reg_5_, _addr_reg_5_, 4
	addi	$11, $11, 1
# was:	addi	_i_reg_6_, _i_reg_6_, 1
	j	_loop_beg_7_
_loop_end_8_:
	ori	$10, $0, 0
# was:	ori	_arr_ind_13_, 0, 0
	addi	$11, $16, 4
# was:	addi	_arr_reg_14_, _letBind_2_, 4
	lw	$12, 0($16)
# was:	lw	_size_reg_15_, 0(_letBind_2_)
	bgez	$10, _safe_lab_18_
# was:	bgez	_arr_ind_13_, _safe_lab_18_
_error_lab_17_:
	ori	$5, $0, 3
# was:	ori	5, 0, 3
	j	_IllegalArrSizeError_
_safe_lab_18_:
	sub	$12, $10, $12
# was:	sub	_tmp_reg_16_, _arr_ind_13_, _size_reg_15_
	bgez	$12, _error_lab_17_
# was:	bgez	_tmp_reg_16_, _error_lab_17_
	sll	$10, $10, 2
# was:	sll	_arr_ind_13_, _arr_ind_13_, 2
	add	$11, $11, $10
# was:	add	_arr_reg_14_, _arr_reg_14_, _arr_ind_13_
	lw	$10, 0($11)
# was:	lw	_tmp_12_, 0(_arr_reg_14_)
# 	ori	_letBind_11_,_tmp_12_,0
	ori	$2, $10, 0
# was:	ori	2, _letBind_11_, 0
	jal	putint
# was:	jal	putint, 2
	ori	$11, $0, 1
# was:	ori	_arr_ind_21_, 0, 1
	addi	$10, $16, 4
# was:	addi	_arr_reg_22_, _letBind_2_, 4
	lw	$12, 0($16)
# was:	lw	_size_reg_23_, 0(_letBind_2_)
	bgez	$11, _safe_lab_26_
# was:	bgez	_arr_ind_21_, _safe_lab_26_
_error_lab_25_:
	ori	$5, $0, 4
# was:	ori	5, 0, 4
	j	_IllegalArrSizeError_
_safe_lab_26_:
	sub	$12, $11, $12
# was:	sub	_tmp_reg_24_, _arr_ind_21_, _size_reg_23_
	bgez	$12, _error_lab_25_
# was:	bgez	_tmp_reg_24_, _error_lab_25_
	sll	$11, $11, 2
# was:	sll	_arr_ind_21_, _arr_ind_21_, 2
	add	$10, $10, $11
# was:	add	_arr_reg_22_, _arr_reg_22_, _arr_ind_21_
	lw	$10, 0($10)
# was:	lw	_tmp_20_, 0(_arr_reg_22_)
# 	ori	_letBind_19_,_tmp_20_,0
	ori	$2, $10, 0
# was:	ori	2, _letBind_19_, 0
	jal	putint
# was:	jal	putint, 2
	ori	$10, $0, 2
# was:	ori	_arr_ind_29_, 0, 2
	addi	$11, $16, 4
# was:	addi	_arr_reg_30_, _letBind_2_, 4
	lw	$12, 0($16)
# was:	lw	_size_reg_31_, 0(_letBind_2_)
	bgez	$10, _safe_lab_34_
# was:	bgez	_arr_ind_29_, _safe_lab_34_
_error_lab_33_:
	ori	$5, $0, 5
# was:	ori	5, 0, 5
	j	_IllegalArrSizeError_
_safe_lab_34_:
	sub	$12, $10, $12
# was:	sub	_tmp_reg_32_, _arr_ind_29_, _size_reg_31_
	bgez	$12, _error_lab_33_
# was:	bgez	_tmp_reg_32_, _error_lab_33_
	sll	$10, $10, 2
# was:	sll	_arr_ind_29_, _arr_ind_29_, 2
	add	$11, $11, $10
# was:	add	_arr_reg_30_, _arr_reg_30_, _arr_ind_29_
	lw	$10, 0($11)
# was:	lw	_tmp_28_, 0(_arr_reg_30_)
# 	ori	_letBind_27_,_tmp_28_,0
	ori	$2, $10, 0
# was:	ori	2, _letBind_27_, 0
	jal	putint
# was:	jal	putint, 2
	ori	$10, $0, 3
# was:	ori	_arr_ind_37_, 0, 3
	addi	$11, $16, 4
# was:	addi	_arr_reg_38_, _letBind_2_, 4
	lw	$12, 0($16)
# was:	lw	_size_reg_39_, 0(_letBind_2_)
	bgez	$10, _safe_lab_42_
# was:	bgez	_arr_ind_37_, _safe_lab_42_
_error_lab_41_:
	ori	$5, $0, 6
# was:	ori	5, 0, 6
	j	_IllegalArrSizeError_
_safe_lab_42_:
	sub	$12, $10, $12
# was:	sub	_tmp_reg_40_, _arr_ind_37_, _size_reg_39_
	bgez	$12, _error_lab_41_
# was:	bgez	_tmp_reg_40_, _error_lab_41_
	sll	$10, $10, 2
# was:	sll	_arr_ind_37_, _arr_ind_37_, 2
	add	$11, $11, $10
# was:	add	_arr_reg_38_, _arr_reg_38_, _arr_ind_37_
	lw	$2, 0($11)
# was:	lw	_tmp_36_, 0(_arr_reg_38_)
# 	ori	_letBind_35_,_tmp_36_,0
# 	ori	2,_letBind_35_,0
	jal	putint
# was:	jal	putint, 2
	ori	$10, $0, 4
# was:	ori	_arr_ind_45_, 0, 4
	addi	$11, $16, 4
# was:	addi	_arr_reg_46_, _letBind_2_, 4
	lw	$12, 0($16)
# was:	lw	_size_reg_47_, 0(_letBind_2_)
	bgez	$10, _safe_lab_50_
# was:	bgez	_arr_ind_45_, _safe_lab_50_
_error_lab_49_:
	ori	$5, $0, 7
# was:	ori	5, 0, 7
	j	_IllegalArrSizeError_
_safe_lab_50_:
	sub	$12, $10, $12
# was:	sub	_tmp_reg_48_, _arr_ind_45_, _size_reg_47_
	bgez	$12, _error_lab_49_
# was:	bgez	_tmp_reg_48_, _error_lab_49_
	sll	$10, $10, 2
# was:	sll	_arr_ind_45_, _arr_ind_45_, 2
	add	$11, $11, $10
# was:	add	_arr_reg_46_, _arr_reg_46_, _arr_ind_45_
	lw	$2, 0($11)
# was:	lw	_tmp_44_, 0(_arr_reg_46_)
# 	ori	_letBind_43_,_tmp_44_,0
# 	ori	2,_letBind_43_,0
	jal	putint
# was:	jal	putint, 2
	ori	$10, $0, 5
# was:	ori	_arr_ind_53_, 0, 5
	addi	$11, $16, 4
# was:	addi	_arr_reg_54_, _letBind_2_, 4
	lw	$12, 0($16)
# was:	lw	_size_reg_55_, 0(_letBind_2_)
	bgez	$10, _safe_lab_58_
# was:	bgez	_arr_ind_53_, _safe_lab_58_
_error_lab_57_:
	ori	$5, $0, 8
# was:	ori	5, 0, 8
	j	_IllegalArrSizeError_
_safe_lab_58_:
	sub	$12, $10, $12
# was:	sub	_tmp_reg_56_, _arr_ind_53_, _size_reg_55_
	bgez	$12, _error_lab_57_
# was:	bgez	_tmp_reg_56_, _error_lab_57_
	sll	$10, $10, 2
# was:	sll	_arr_ind_53_, _arr_ind_53_, 2
	add	$11, $11, $10
# was:	add	_arr_reg_54_, _arr_reg_54_, _arr_ind_53_
	lw	$2, 0($11)
# was:	lw	_tmp_52_, 0(_arr_reg_54_)
# 	ori	_letBind_51_,_tmp_52_,0
# 	ori	2,_letBind_51_,0
	jal	putint
# was:	jal	putint, 2
	ori	$10, $0, 6
# was:	ori	_arr_ind_61_, 0, 6
	addi	$11, $16, 4
# was:	addi	_arr_reg_62_, _letBind_2_, 4
	lw	$12, 0($16)
# was:	lw	_size_reg_63_, 0(_letBind_2_)
	bgez	$10, _safe_lab_66_
# was:	bgez	_arr_ind_61_, _safe_lab_66_
_error_lab_65_:
	ori	$5, $0, 9
# was:	ori	5, 0, 9
	j	_IllegalArrSizeError_
_safe_lab_66_:
	sub	$12, $10, $12
# was:	sub	_tmp_reg_64_, _arr_ind_61_, _size_reg_63_
	bgez	$12, _error_lab_65_
# was:	bgez	_tmp_reg_64_, _error_lab_65_
	sll	$10, $10, 2
# was:	sll	_arr_ind_61_, _arr_ind_61_, 2
	add	$11, $11, $10
# was:	add	_arr_reg_62_, _arr_reg_62_, _arr_ind_61_
	lw	$10, 0($11)
# was:	lw	_tmp_60_, 0(_arr_reg_62_)
# 	ori	_letBind_59_,_tmp_60_,0
	ori	$2, $10, 0
# was:	ori	2, _letBind_59_, 0
	jal	putint
# was:	jal	putint, 2
	ori	$2, $0, 0
# was:	ori	_mainres_1_, 0, 0
# 	ori	2,_mainres_1_,0
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
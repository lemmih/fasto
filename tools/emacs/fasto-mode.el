;;; fasto-mode.el --- major mode for editing Fasto source files

;; Copyright (C) DIKU 2014-2017, University of Copenhagen
;; Based on futhark-mode.el <https://github.com/HIPERFIT/futhark>

;;; Commentary:
;; This mode provides syntax highlighting and automatic indentation for
;; Fasto source files.
;;
;; This mode provides the following features for Fasto source files:
;;
;;   + syntax highlighting
;;   + automatic indentation
;;
;; To load fasto-mode automatically on Emacs startup, make sure this
;; file is in your load path and then require the mode, e.g. something
;; like this:
;;
;;   (add-to-list 'load-path "path/to/fasto/tools/emacs")
;;   (require 'fasto-mode)
;;
;; This will also tell your Emacs that ".fo" files are to be handled by
;; fasto-mode.
;;
;; Define your local keybindings in `fasto-mode-map', and add startup
;; functions to `fasto-mode-hook'.
;;
;; Report bugs to Niels.


;;; Basics

(require 'cl) ; `some'

;;;###autoload
(add-to-list 'auto-mode-alist '("\\.fo\\'" . fasto-mode))

(defvar fasto-mode-hook nil
  "Hook for fasto-mode.  Is run whenever the mode is entered.")

(defvar fasto-mode-map
  (make-keymap)
  "Keymap for fasto-mode.")

(defconst fasto-keywords
  '("if" "then" "else" "let" "in" "fun" "fn" "op")
  "All Fasto keywords.")

(defconst fasto-builtin-functions
  '("iota" "replicate" "map" "reduce" "scan" "read" "write" "not")
  "All Fasto builtin SOACs, functions and non-symbolic operators.")

(defconst fasto-builtin-operators
  '("+" "-" "==" "<" "~" "&&" "||")
  "All Fasto builtin symbolic operators.")

(defconst fasto-builtin-types
  '("int" "bool" "char")
  "All Fasto builtin primitive types.")

(defconst fasto-booleans
  '("true" "false")
  "All Fasto booleans.")

(defconst fasto-type
  (concat "\\[*" "\\<" (regexp-opt fasto-builtin-types 'nil) "\\>" "\\]*")
  "A regex describing a Fasto type.")

(defconst fasto-var
  (concat "\\(?:" "[_'[:alnum:]]+" "\\)")
  "A regex describing a Fasto variable.")


;;; Highlighting

(let (
      (ws "[[:space:]\n]*")
      (ws1 "[[:space:]\n]+")
      )
  (defvar fasto-font-lock
    `(

      ;; Function declarations.
      (,(concat "fun" ws1 fasto-type ws1 "\\(" fasto-var "\\)")
       . '(1 font-lock-function-name-face))

      ;; Function parameters.
      (,(concat "\\(?:" "(" "\\|" "," "\\)" ws
                fasto-type ws1 "\\(" fasto-var "\\)")
       . '(1 font-lock-variable-name-face))

      ;; Let declarations.
      (,(concat "let" ws1
                "\\(" fasto-var "\\)")
       . '(1 font-lock-variable-name-face))

      ;; Keywords.
      (,(regexp-opt fasto-keywords 'words)
       . font-lock-keyword-face)

      ;; Types.
      (,fasto-type
       . font-lock-type-face)

      ;; Builtins.
      ;;; Functions.
      (,(regexp-opt fasto-builtin-functions 'words)
       . font-lock-builtin-face)
      ;;; Operators.
      (,(regexp-opt fasto-builtin-operators)
       . font-lock-builtin-face)

      ;; Constants.
      ;;; Booleans.
      (,(regexp-opt fasto-booleans 'words)
       . font-lock-constant-face)

      )
    "Highlighting expressions for Fasto.")
  )

(defvar fasto-mode-syntax-table
  (let ((st (make-syntax-table)))
    ;; Define the // line comment syntax.
    (modify-syntax-entry ?/ ". 123" st)
    (modify-syntax-entry ?\n ">" st)
    ;; Make apostrophe and underscore be part of variable names.
    ;; Technically, they should probably be part of the symbol class,
    ;; but it works out better for some of the regexpes if they are part
    ;; of the word class.
    (modify-syntax-entry ?' "w" st)
    (modify-syntax-entry ?_ "w" st)
    (modify-syntax-entry ?\\ "_" st)
    st)
  "Syntax table used in `fasto-mode'.")


;;; Indentation

(defvar fasto-indent-level 2
  "The basic indent level for fasto-mode.")

(defun fasto-indent-line ()
  "Indent current line as Fasto code."
  (let ((savep (> (current-column) (current-indentation)))
        (indent (or (fasto-calculate-indentation)
                    (current-indentation))))
    (if savep ; The cursor is beyond leading whitespace.
        (save-excursion (indent-line-to indent))
      (indent-line-to indent))))

(defun fasto-calculate-indentation ()
  "Calculate the indentation for the current line.
In general, prefer as little indentation as possible."
  (let ((parse-sexp-lookup-properties t)
        (parse-sexp-ignore-comments t))

    (save-excursion
      (fasto-beginning-of-line-text)

      ;; The following code is fickle and deceptive.  Don't change it
      ;; unless you kind of know what you're doing!
      (or

       ;; Align comment to next non-comment line.
       (and (looking-at comment-start)
            (forward-comment (count-lines (point-min) (point)))
            (current-column))

       ;; Align global function definitions to column 0.
       (and (fasto-looking-at-word "fun")
            0)

       ;; Align closing parentheses and commas to the matching opening
       ;; parenthesis.
       (save-excursion
         (and (looking-at (regexp-opt '(")" "]" ",")))
              (ignore-errors
                (backward-up-list 1)
                (current-column))))

       ;; Align "in" or "let" to the closest previous "let".
       (save-excursion
         (and (or (fasto-looking-at-word "in")
                  (fasto-looking-at-word "let"))
              (let ((m
                     (save-excursion
                       (fasto-keyword-backward "let"))
                     ))
                (and (not (eq nil m))
                     (goto-char m)
                     (current-column)))))

       ;; Otherwise, if the previous code line ends with "in" align to
       ;; the matching "let" column.
       (save-excursion
         (and (fasto-backward-part)
              (looking-at "\\<in[[:space:]]*$")
              (let ((m
                     (save-excursion
                       (fasto-keyword-backward "let"))
                      ))
                (and (not (eq nil m))
                     (goto-char m)
                     (current-column)))))

       ;; If the previous code line ends with "=", align to the matching "fun"
       ;; or "let" column plus one indent level.
       (save-excursion
         (and (fasto-backward-part)
              (looking-at "=[[:space:]]*$")
              (let ((m
                     (fasto-max
                      (save-excursion
                        (fasto-keyword-backward "fun"))
                      (save-excursion
                        (fasto-keyword-backward "let")))
                     ))
                (and (not (eq nil m))
                     (goto-char m)
                     (+ (current-column) fasto-indent-level)))))

       ;; Align "then" to nearest "else if" or "if".
       (save-excursion
         (and (fasto-looking-at-word "then")
              (fasto-keyword-backward "if")
              (or
               (let ((curline (line-number-at-pos)))
                 (save-excursion
                   (and (fasto-backward-part)
                        (= (line-number-at-pos) curline)
                        (fasto-looking-at-word "else")
                        (current-column))))
               (current-column))))

       ;; Align "else" to nearest "then" or "else if" or "if".
       (save-excursion
         (and (fasto-looking-at-word "else")
              (let ((m
                     (fasto-max
                      (save-excursion
                        (and
                         (fasto-keyword-backward "then")
                         (fasto-is-beginning-of-line-text)
                         (point)))
                      (save-excursion
                        (let ((pos0 (fasto-keyword-backward "if")))
                          (or
                           (let ((curline (line-number-at-pos)))
                             (and (fasto-backward-part)
                                  (= (line-number-at-pos) curline)
                                  (fasto-looking-at-word "else")
                                  (point)))
                           pos0))))))
                (and (not (eq nil m))
                     (goto-char m)
                     (current-column)))))

       ;; Align general content inside parentheses to the first general
       ;; non-space content.
       (save-excursion
         (when (ignore-errors (backward-up-list 1) t)
              (forward-char 1)
              (fasto-goto-first-text)
              (and
               (not (fasto-is-looking-at-keyword))
               (current-column))))

       ;; Otherwise, keep the user-specified indentation level.
       ))))

(defun fasto-max (a b)
  "Like `max', but more accepting."
  (or (and (eq nil a) b)
      (and (eq nil b) a)
      (and (not (eq nil a))
           (not (eq nil b))
           (max a b))))

(defun fasto-beginning-of-line-text ()
  "Move to the beginning of the non-whitespace text on this line."
  (beginning-of-line)
  (fasto-goto-first-text))

(defun fasto-goto-first-text ()
  "Skip over whitespace."
  (while (looking-at "[[:space:]\n]")
    (forward-char)))

(defun fasto-is-beginning-of-line-text ()
  "Check if point is at the first word on a line."
  (=
   (point)
   (save-excursion
     (fasto-beginning-of-line-text)
     (point))))

(defun fasto-is-looking-at-keyword ()
  "Check if we are currently looking at a keyword."
  (some 'fasto-looking-at-word fasto-keywords))

(defun fasto-backward-part ()
  "Try to jump back one sexp.
The net effect seems to be that it works ok."
  (and (not (bobp))
       (ignore-errors (backward-sexp 1) t)))

(defun fasto-looking-at-word (word)
  "Do the same as `looking-at', but also check for blanks around WORD."
  (looking-at (concat "\\<" word "\\>")))

(defun fasto-keyword-backward (word)
  "Go to a keyword WORD before the current position.
Set mark and return t if found; return nil otherwise."
  (let (;; Only look in the current paren-delimited code if present.
        (startp (point))
        (topp (or (save-excursion (ignore-errors
                                    (backward-up-list 1)
                                    (point)))
                  (max
                   (or (save-excursion (fasto-keyword-backward-raw "fun"))
                       0)
                   (or (save-excursion (fasto-keyword-backward-raw "entry"))
                       0))))
        (result nil))

    (while (and (not result)
                (fasto-backward-part)
                (>= (point) topp))

      (if (fasto-looking-at-word word)
          (setq result (point))))

    (or result
        (progn
          (goto-char startp)
          nil))))

(defun fasto-keyword-backward-raw (word)
  "Go to a keyword WORD before the current position.
Ignore any program structure."
  (let ((pstart (point)))
    (while (and (fasto-backward-part)
                (not (fasto-looking-at-word word))))
    (and (fasto-looking-at-word word)
         (point))))


;;; Actual mode declaration

(define-derived-mode fasto-mode fundamental-mode "Fasto"
  "Major mode for editing Fasto source files."
  :syntax-table fasto-mode-syntax-table
  (set (make-local-variable 'font-lock-defaults) '(fasto-font-lock))
  (set (make-local-variable 'indent-line-function) 'fasto-indent-line)
  (set (make-local-variable 'indent-region-function) nil)
  (set (make-local-variable 'comment-start) "//")
  (set (make-local-variable 'comment-padding) " "))

(provide 'fasto-mode)

;;; fasto-mode.el ends here

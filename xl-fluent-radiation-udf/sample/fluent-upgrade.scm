; ----------------------------------------------------------------------------
; User defined functions (UDF)
; ----------------------------------------------------------------------------

; Set number of gray gases
(define n-gas-gray 5)
(define n-udm-slots (+ (* 2 n-gas-gray) 1))

; Enable user defined memory (UDM) for the gray gases
/define/user-defined/user-defined-memory n-udm-slots

; Use built-in C++ compiler? NO!
;; /define/user-defined/use-built-in-compiler? yes

; Compile UDF library
/define/user-defined/compiled-functions compile wsgglib
    yes              ; Continue? [yes]
    yes              ; Do you want to read new file(y/n): ["y"] y
    "src/wsggudf.c"  ; First file name: [""] src/udf.c
    "src/wsgglib.c"  ; Next  file name: [""]
    ""               ; Next  file name: [""]
    "src/wsgglib.h"  ; Give header file names: First file name: [""]
    ""

; Load UDF library
/define/user-defined/compiled-functions load wsgglib

; Unload if needed (before deleting the folder for updates)
; /define/user-defined/compiled-functions unload wsgglib

; Hook UDF to adjust
/define/user-defined/function-hooks/adjust
    "wsgg_eval::wsgglib"

; Hook UDF to emissivity weighting factor
/define/user-defined/function-hooks/emissivity-weighting-factor
    "wsgg_emiss_weighting::wsgglib"

; Material setup (adapt for UDF)
/define/materials/change-create "carbon-monoxide-air" "carbon-monoxide-air"
    yes ; change Mixture Species? [no] yes
    3   ; number of volumetric species [3] 3
    co2 ; volumetric species 1
    h2o ; volumetric species 2
    n2  ; volumetric species 3
    0   ; number of surface species [0] 0
    0   ; number of site species [0] 0
    no  ; change Density? [no]
    no  ; change Cp (Specific Heat)? [no]
    no  ; change Thermal Conductivity? [no]
    no  ; change Viscosity? [no]
    no  ; change Mass Diffusivity? [no]
    yes ; change Absorption Coefficient? [no]
    user-defined-wsggm "wsgg_abs_coeff::wsgglib"
    no  ; change Scattering Coefficient? [no]
    no  ; change Scattering Phase Function? [no]
    no  ; change Refractive Index? [no]
    no  ; change Speed of Sound? [no]

; ----------------------------------------------------------------------------
; EOF
; ----------------------------------------------------------------------------
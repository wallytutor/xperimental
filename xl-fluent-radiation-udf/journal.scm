; ----------------------------------------------------------------------------
; Set batch options and load mesh
; ----------------------------------------------------------------------------

/file/import/cgns/mesh geometry.cgns
/mesh/check
/mesh/mesh-info 0

; ----------------------------------------------------------------------------
; Patch imported types/names
; ----------------------------------------------------------------------------

; Global domain
/mesh/modify-zones/zone-name 1 "fluid" ()
/mesh/modify-zones/zone-name 3 "interior" ()

; Walls
/mesh/modify-zones/zone-name 2 "wall-lower" ()
/mesh/modify-zones/zone-name 5 "wall-upper" ()
/mesh/modify-zones/zone-type 2 "wall" ()
/mesh/modify-zones/zone-type 5 "wall" ()

; Inlet and outlet
/mesh/modify-zones/zone-name 4 "inlet" ()
/mesh/modify-zones/zone-name 6 "outlet" ()
/mesh/modify-zones/zone-type 4 "velocity-inlet" ()
/mesh/modify-zones/zone-type 6 "pressure-outlet" ()

; ----------------------------------------------------------------------------
; Basic flow models
; ----------------------------------------------------------------------------

/define/models/viscous/laminar? yes ()

/define/models/energy?
    yes ; Enable energy model? [no] yes
    no  ; Compute viscous energy work? [no] no
    no  ; Include pressure work in energy equation? [no] no
    no  ; Include kinetic energy in energy equation? [no] no
    yes ; Include diffusion at inlets? [yes] yes
    ()

; ----------------------------------------------------------------------------
; Radiation model
; ----------------------------------------------------------------------------

; Activate radiation before species!
/define/models/radiation/discrete-ordinates? yes
    2 ; Enter number of theta divisions [2] 2
    2 ; Enter number of phi divisions [2] 2
    1 ; Enter number of theta pixels [1] 1
    1 ; Enter number of phi pixels [1] 1

; This contains both CO2 and H2O for the test:
/define/models/species/species-transport?
    yes                   ; Enable the species transport model?
    "carbon-monoxide-air" ; Enter Mixture Material

; Redundant but make sure there are no reactions:
/define/models/species/volumetric-reactions? no ()

; ----------------------------------------------------------------------------
; Material setup
; ----------------------------------------------------------------------------

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
    wsggm-domain-based
    no  ; change Scattering Coefficient? [no]
    no  ; change Scattering Phase Function? [no]
    no  ; change Refractive Index? [no]
    no  ; change Speed of Sound? [no]

; ----------------------------------------------------------------------------
; Boundary conditions
; ----------------------------------------------------------------------------

/define/boundary-conditions/set/velocity-inlet "inlet" ()
    species-in-mole-fractions? yes
    vmag no 1.0
    temperature no 300
    mf
    no 0.25 ; co2
    no 0.25 ; h2o
    ()

/define/boundary-conditions/set/wall "wall-lower" ()
    thermal-bc yes temperature ()
/define/boundary-conditions/set/wall "wall-lower" ()
    temperature no 300 ()

/define/boundary-conditions/set/wall "wall-upper" ()
    thermal-bc yes temperature ()
/define/boundary-conditions/set/wall "wall-upper" ()
    temperature no 1000 ()

; ----------------------------------------------------------------------------
; Base solution
; ----------------------------------------------------------------------------

/solve/initialize/initialize-flow
/solve/iterate 200

; ----------------------------------------------------------------------------
; User defined functions (UDF)
; ----------------------------------------------------------------------------

; Set number of gray gases
(define n-gas-gray 5)

; Enable user defined memory (UDM) for the gray gases
/define/user-defined/user-defined-memory n-gas-gray

; Use built-in C++ compiler?
/define/user-defined/use-built-in-compiler? yes

; Compile UDF library
/define/user-defined/compiled-functions compile libudf
    yes                              ; Continue? [yes]
    yes                              ; Do you want to read new file(y/n): ["y"] y
    "src/wsgg_radlib_udf.c"          ; First file name: [""] src/udf.c
    "src/wsgg_radlib_bordbar_2020.c" ; Next  file name: [""]
    ""                               ; Next  file name: [""]
    "src/wsgg_radlib_bordbar_2020.h" ; Give header file names: First file name: [""]
    ""

; Load UDF library
/define/user-defined/compiled-functions load libudf

; Unload if needed (before deleting the folder for updates)
; /define/user-defined/compiled-functions unload libudf

; Hook UDF to adjust
/define/user-defined/function-hooks/adjust
    "evaluate_wsgg_model::libudf" ""

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
    user-defined-wsggm "user_wsggm_abs_coeff::libudf"
    no  ; change Scattering Coefficient? [no]
    no  ; change Scattering Phase Function? [no]
    no  ; change Refractive Index? [no]
    no  ; change Speed of Sound? [no]
program floatf
    use, intrinsic :: iso_c_binding, only: sp=>c_float, dp=>c_double
    implicit none

    real(sp) :: float32
    real(dp) :: float64

    float32 = 1.0_sp  ! Explicit suffix for literal constants
    float64 = 1.0_dp
    print*, float32, float64
end program floatf
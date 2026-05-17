module your_module
    implicit none
    integer :: n = 2
end module

program main
    implicit none
    real :: x

    x = 0.0

    block
        use your_module, only: n ! you can import modules within blocks
        real :: x, y ! local scope variable

        y = 2.0
        x = y ** n

        print *, x ! prints 4.00000000
    end block

    ! print *, y ! this is not allowed as y only exists during the block's scope
    print *, x  ! prints 0.00000000
end program
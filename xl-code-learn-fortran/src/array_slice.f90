program array_slice
    implicit none

    integer :: i
    integer :: array1(10)
    integer :: array2(10, 10)

    ! Array constructor
    array1 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]

    ! Print out elements at odd indices, start at element 1,
    ! go up to element 10, in strides of 2
    print *, "Array at odd indices"
    print *, array1(1:10:2)

    ! Print an array in reverse (-1 step!)
    print *, "Array in reverse!"
    print *, array1(10:1:-1)

    array1(:) = 0
    print *, "Reset array to 0!"
    print *, array1

    ! Implied do loop constructor
    array1 = [(i, i = 1, 10)]
    print *, "Now back to 1 to 10!"
    print *, array1

    array1(1:5) = 1
    print *, "Set first five elements to 1"
    print *, array1

    array1(6:) = 2
    print *, "Set all elements after 5 to 2"
    print *, array1

    array2(:, 1) = [(i**2, i = 1, 10)]
    print *, "First column of 2d array"
    print *, array2(:,1)
end program array_slice
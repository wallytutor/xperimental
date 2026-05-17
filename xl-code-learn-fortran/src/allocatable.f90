program allocatable
    implicit none

    integer :: i, n
    integer, allocatable :: array1(:)
    integer, allocatable :: array2(:,:)

    n = 10

    allocate(array1(n))
    allocate(array2(n,n))

    array1(:)    = [(i**2, i = 1, n)]
    array2(:, 1) = [(i**2, i = 1, n)]

    print *, array1
    print *, array2(:, 1)

    deallocate(array1)
    deallocate(array2)
end program allocatable
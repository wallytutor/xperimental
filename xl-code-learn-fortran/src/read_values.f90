program read_values
    implicit none
    real :: x, y
    real :: x_times_y

    print *, 'Please enter two numbers. '
    read(*,*) x, y

    x_times_y = x * y
    print *, 'The sum and product of the numbers are ', x+y, x_times_y
end program read_values
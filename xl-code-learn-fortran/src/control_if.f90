program control_if
    real :: angle

    angle = 30.0

    if (angle < 90.0) then
        print *, 'Single branch: Angle is acute'
    end if

    if (angle < 90.0) then
        print *, 'If-else: Angle is acute'
    else
        print *, 'If-else: Angle is obtuse'
    end if

    angle  = 145.0
    
    if (angle < 90.0) then
        print *, 'Angle is acute'
    else if (angle < 180.0) then
        print *, 'Angle is obtuse'
    else
        print *, 'Angle is reflex'
    end if
end program control_if
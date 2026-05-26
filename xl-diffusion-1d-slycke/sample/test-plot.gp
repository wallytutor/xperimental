set term pngcairo enhanced color font 'Arial,10' size 600,900
set output 'test-plot.png'

set title 'Test Plot'
set xlabel 'X'
set ylabel 'Y'
set linestyle 1 dt 1 lw 1 lc '#000000'
set linestyle 2 dt 1 lw 1 lc '#FF0000'

$xy << EOD
0.0 1.0 2.0
1.0 2.0 3.0
2.0 3.0 4.0
EOD

set grid
set key right top
plot $xy using 1:2 with lines linestyle 1 title 'Y1',\
     $xy using 1:3 with lines linestyle 2 title 'Y2'

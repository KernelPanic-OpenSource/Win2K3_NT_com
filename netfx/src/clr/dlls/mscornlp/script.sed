1,1 d
s/^0......0   //
s/  ................$//
s/^/0x/
s/ /,0x/g
s/$/,/
$,$ s/0x,//g
$,$ s/,$//

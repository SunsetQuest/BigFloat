# Threshold sweep (.NET 10.0.1)
Generated: 2025-12-22 06:03:33 UTC

## Karatsuba vs schoolbook multiplication
Bit length | Schoolbook mean (ms) | Karatsuba mean (ms)
---|---:|---:
64 | 0.182 | 0.196
128 | 0.301 | 0.353
256 | 0.719 | 0.788
512 | 2.131 | 2.171
1024 | 7.955 | 7.981
2048 | 15.951 | 32.637
4096 | 30.906 | 97.561
8192 | 104.032 | 300.727

## Division (BigFloat variants)
### Equal-size operands
Bit length | Small path mean (us) | Standard mean (us) | Large/BZ mean (us)
---|---:|---:|---:
32 | 1.033 | 0.549 | 0.688
64 | 0.543 | 0.477 | 0.804
128 | 0.768 | 0.618 | 0.826
256 | 0.882 | 0.751 | 0.988
512 | 1.395 | 1.409 | 13.230
1024 | 4.872 | 4.192 | 34.103
2048 | 10.706 | 10.236 | 732.960
4096 | 94.280 | 88.670 | 2764.020

Crossover summary:
- Small vs standard: 32 bits.
- Standard vs large/BZ: no crossover in sweep.


### Unbalanced operands
Numerator bits | Denominator bits | Small path mean (us) | Standard mean (us) | Large/BZ mean (us)
---|---|---:|---:|---:
1000 | 10 | 1.045 | 0.742 | 1.079
2048 | 32 | 1.248 | 0.888 | 1.244
4096 | 64 | 1.433 | 1.053 | 1.383
8192 | 128 | 2.223 | 1.750 | 2.500
16384 | 256 | 3.763 | 2.940 | 3.900
32768 | 512 | 5.933 | 4.650 | 38.497
65536 | 1024 | 11.583 | 10.570 | 103.923

### Random operand sizes
Case | Numerator bits | Denominator bits | Small path mean (us) | Standard mean (us) | Large/BZ mean (us)
---|---|---|---:|---:|---:
1 | 281 | 1774 | 12.579 | 7.880 | 181.796
2 | 498 | 669 | 6.065 | 4.659 | 51.047
3 | 1348 | 983 | 10.265 | 8.465 | 82.191
4 | 1035 | 1698 | 14.427 | 12.881 | 288.278
5 | 990 | 1553 | 12.419 | 10.904 | 306.161
6 | 3405 | 1571 | 17.552 | 15.328 | 400.474
7 | 1152 | 523 | 5.083 | 4.104 | 51.633
8 | 2546 | 280 | 3.632 | 2.790 | 3.738


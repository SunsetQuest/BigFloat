# A BigFloat Library in C#

**BigFloat is a C# struct/class library for large Floating-Point numbers**  
Ryan Scott White / [MIT](http://www.opensource.org/licenses/mit-license.php "The MIT License") License  
Updated: April 4th 2025 

**Originally posted on CodeProject**:  
![](https://raw.githubusercontent.com/SunsetQuest/BigFloat/master/Docs/Images/views32.png) 12.3K views, 
![](https://raw.githubusercontent.com/SunsetQuest/BigFloat/master/Docs/Images/download32.png) 183 downloads

**Quick Summary**: 
BigFloat is both a C# struct data type along with a library of functions. It offers an innovative solution for handling large floating-point numbers, that extend beyond the precision limitations of standard the standard IEEE floating-point. A flexible mantissa and a broad exponent range enable precise arithmetic operations and mathematical functions on large or small numbers. This library is ideal for high-precision applications. Available here on GitHub, BigFloat easily integrates into C# projects, providing a tool for developers needing advanced numerical computation capabilities.

**Links to Code**:  [GitHub Repo](https://github.com/SunsetQuest/BigFloat), [Just Show Me the Code](https://raw.githubusercontent.com/SunsetQuest/BigFloat/master/BigFloatLibrary/BigFloat.cs)

***Note**: This article was co-created with ChatGPT and Grammarly - details [here](https://www.codeproject.com/Articles/5375327/A-BigFloat-Library-in-Csharp#ArticleCreationProcess).*

## Introduction

BigFloat is a C# library tailored for handling substantially large floating-point numbers. It extends the capabilities of standard IEEE floating points like single and double by providing a flexible-sized mantissa and a large exponent range. This library offers a unique blend of precision and flexibility, making it an ideal choice for computations requiring high accuracy in large numbers like scientific computing. Recently published on [GitHub](https://github.com/SunsetQuest/BigFloat), BigFloat is now also featured in this detailed CodeProject article.

## Differences from IEEE Floating Points

BigFloat, while similar to IEEE standards in structure, introduces notable differences:

-   **Two's Complement Representation**: BigFloat employs two's complement for its sign as it uses a BigInteger under the hood. Two's complement arithmetic is generally more efficient.
-   **Scale vs. Exponent**: Unlike IEEE's left-measured exponent, BigFloat's "Scale" measures the radix point from the least significant digit on the right.
-   **Flexible Mantissa Size**: The mantissa, called `DataBits` in BigFloat, has an adjustable size ranging up to two billion bits.

## BigFloat Structure

`BigFloat`'s architecture consists of three core components:

-   **DataBits** *(of type `BigInteger`)*: The DataBits represent the mantissa, holding the binary form of the number.
-   **Scale** *(of type `int`)*: Scale dictates the position of the radix point, allowing for scalable precision. A positive value would move the radix point right, increasing the number size; a negative value would move the radix point left, creating a fractional part. A zero value would essentially represent an integer.
-   **Size** *(of type `int`)*: A cached value representing the size of `DataBit`s. It is added for optimizing frequent access. '`_size`' is equivalent to the function '`int _size = > ABS(dataBits).GetBitSize();`'

![BigFloatParts](https://raw.githubusercontent.com/SunsetQuest/BigFloat/master/Docs/Images/BigFloatParts.png)

## Using the Code

Incorporating BigFloat into your project is straightforward. The primary file, '*BigFloat.cs*', contains all necessary functions, while an optional '*Constants.cs*' file offers access to extended mathematical constants. Adding these files to your project and optional references is all that's required.

Also, because of some language features that are used, C# 11 / .NET 7 is required.

*Constants.cs* provides up to 5000 decimal digits, but some optionally included text files in the *values* folder extend this to 1,000,000 digits.

### Initializing and Basic Arithmetic Examples

A quick note on the output notation. Below, we will see outputs that look like 232XXXXXXXX. When we see this, BigFloat lets the user know that only the 232 part is in-precision.

```cs
// Initialize BigFloat numbers
BigFloat a = new("123456789.012345678901234"); // Initialize by String
BigFloat b = new(1234.56789012345678); // Initialize by Double

// Basic arithmetic
BigFloat sum = a + b;
BigFloat difference = a - b;
BigFloat product = a * b;
BigFloat quotient = a / b;

Console.WriteLine($"Sum: {sum}");
// Output: Sum: 123458023.5802358023581

Console.WriteLine($"Difference: {difference}");
// Output: Difference: 123455554.4444555554443

Console.WriteLine($"Product: {product}");
// Output: Product: 152415787532.38838

Console.WriteLine($"Quotient: {quotient}");
// Output: Quotient: 99999.99999999999
```

### Working with Mathematical Constants

```cs
// Access constants like Pi or E from Constants
BigFloat.Constants bigConstants = new(
   requestedAccuracyInBits: 1000,
   onInsufficientBitsThenSetToZero: true,
   cutOnTrailingZero: true);
BigFloat pi = bigConstants.Pi;
BigFloat e = bigConstants.E;

Console.WriteLine($"e to 1000 binary digits: {e.ToString()}");
// Output:
// e to 1000 binary digits: 2.71828182845904523536028747135266249775724709369995957496696
// 76277240766303535475945713821785251664274274663919320030599218174135966290435729003342
// 95260595630738132328627943490763233829880753195251019011573834187930702154089149934884
// 1675092447614606680822648001684774118537423454424371075390777449920696

// Use Pi in a calculation (Area of a circle with r = 100)
BigFloat radius = new("100.0000000000000000");
BigFloat area = pi * radius * radius;

Console.WriteLine($"Area of the circle: {area}");
// Output: Area of the circle: 31415.92653589793238
```

### Precision Manipulation
```cs
// Initialize a number with high precision
BigFloat preciseNumber = new("123.45678901234567890123");
BigFloat morePreciseNumber = BigFloat.ExtendPrecision(preciseNumber, bitsToAdd: 50);

Console.WriteLine($"Extend Precision result: {morePreciseNumber}");
// Output: Extend Precision result: 123.45678901234567890122999999999787243

// Initialize an integer with custom precision
BigFloat c = BigFloat.IntWithAccuracy(10, 100);

Console.WriteLine($"Int with specified accuracy: {c}");
// Output: Int with specified accuracy: 10.000000000000000000000000000000
```

### Comparing Numbers
```cs
// Initialize two BigFloat numbers
BigFloat num1 = new("12345.6790");
BigFloat num2 = new("12345.6789");

// Let's compare the numbers that are not equal...
bool areEqual = num1 == num2;
bool isFirstBigger = num1 > num2;

Console.WriteLine($"Are the numbers equal? {areEqual}");
// Output: Are the numbers equal? False

Console.WriteLine($"Is the first number bigger? {isFirstBigger}");
// Output: Is the first number bigger? True
```

Depending on the base, a number could either round up or down. In base 10, the following `12345.67896` would round up to `12345.6790`. However, in binary, it rounds down to `11000000111001.1010110111010`. Since `BigFloat` is base-2, this is correct, but it can cause odd side effects like the example below.
```cs
BigFloat num3 = new("12345.6789");
BigFloat num4 = new("12345.67896");

areEqual = num3 == num4;
isFirstBigger = num3 > num4;

Console.WriteLine($"Are the numbers equal? {areEqual}");
// Output: Are the numbers equal? True

Console.WriteLine($"Is the first number bigger? {isFirstBigger}");
// Output: Is the first number bigger? False
```

### Handling Very Large or Small Exponents

```cs
// Creating a large number
BigFloat largeNumber = new("1234e+7");

Console.WriteLine($"Large Number: {largeNumber}");
// Output: Large Number: 123XXXXXXXX

// Creating a very large number
BigFloat veryLargeNumber = new("1e+300");

Console.WriteLine($"Very Large Number: {veryLargeNumber}");
// Output: Very Large Number: 1 * 10^300

// Creating a very small number
BigFloat smallNumber = new("1e-300");

Console.WriteLine($"Small Number: {smallNumber}");
// Output: Small Number: 0.00000000000000000000000000000000000000000000000000000000000000
// 00000000000000000000000000000000000000000000000000000000000000000000000000000000000000
// 00000000000000000000000000000000000000000000000000000000000000000000000000000000000000
// 000000000000000000000000000000000000000000000000000000000000000001

BigFloat num5 = new("12121212.1212");
BigFloat num6 = new("1234");

Console.WriteLine($"{num5} * {num6} = {num5 * num6}");
// BigFloat   Output: 12121212.1212 * 1234 = 1496XXXXXXX

num5 = new("12121212.1212");
num6 = new("3");
BigFloat result = num5 * num6;

Console.WriteLine($"{num5} * {num6} = {result}");
// BigFloat Output: 12121212.1212 * 3 = 0XXXXXXXX
// Not Perfect, optimal output:          4XXXXXXX

num5 = new("121212.1212");
num6 = new("1234567");

Console.WriteLine($"{num5} * {num6} = {num5 * num6}");
// Output: 121212.1212 * 1234567 = 149644XXXXXX

Console.WriteLine($"GetPrecision: {num6.GetPrecision}");
// Output: GetPrecision: 21
```

## GuardBits

In BigFloat, the actual "data bits" are stored in a `BigInteger`. `BigFloat` designates the 32 least significant bits as "extra hidden guard bits." These bits are not generally considered precise but play a vital role in maintaining accuracy.

### The Role of Guard Bits

To help with the accuracy of the final result, BigFloat keeps some extra bits that act as an extended buffer during arithmetic operations. Think of them as extended precision that is partially accurate and holds the remnants of calculations. This might not be substantial, but it leads to a more precise outcome after several consecutive math operations.

### Example Illustration

Consider the following binary addition, where the pipe character '`|`' separates precise bits from non-precision GuardBits:
```
  101.01100|110011001100110011001100110011 (approximately 5.4)
+ 100.01001|100110011001100110011001100110 (approximately 4.3)
==========================================
 1001.1011|0011001100110011001100110011001 (approximately 9.7)
```
If we were only to add the precise bits, our result would be ``1001.101``, missing the crucial information that the actual result is closer to ``100.110``. These extra bits help with better rounding and accuracy during subsequent mathematical operations.

### Practical Implications

By carrying these extra 32 hidden guard bits, `BigFloat` can perform operations with higher accuracy. When multiple operations are chained, these "guard bits" help to correct cumulative rounding errors that would otherwise lead to significant inaccuracies. In essence, they serve as a "safety net" for precision.

## Decimal-to-Binary and Binary-to-Decimal Conversions

This section covers some essential points regarding converting decimal strings to binary and back.

### Conversion Precision Loss

There can be some precision loss when converting from a Decimal String to binary or vice versa, using `Parse()` and `ToString()`. This is because when most base-10 decimal numbers are converted to binary, they produce a repeating pattern.

#### Some Examples

-   `5.4` **→** `101.011001100110011001100... (repeats forever)`
-   `4.3` **→** `100.01001100110011001100.... (repeats forever)`
-   `0.25` **→** `0.01 (can be converted precisely)`

Infinitely repeating binary digits do not fit in an integer very well! We must cut it off or do some magic trickery - that I will get to later. In a nutshell, most decimal numbers cannot be accurately represented.

Guard bits to the rescue! Well, kind of. The advantage of keeping some extra hidden guard bits is that we can more accurately represent additional repeating bits. The bits are considered out-of-precision, but at the same time, more repeated bits can be stored for better accuracy. We can store more of those repeated binary digits. When we say 5.3 liters of water, we specify two decimal digits (or about seven binary digits, 101.0110). But at the same time, 5.3 can be better described with more bits, 101.0110011001100.

#### Accurate Representation of Repeating Bits - a Possible Future Feature

Earlier, I noted there was a better way. While not implemented in BigFloat, I wanted to mention it as it is a possible addition in the future or a suggestion for some other class. To store repeated digits, we could introduce a new attribute called '`_repeat`.' If there is a value, then it's the number of least significant digits in `DataBits` that repeat. If zero, there are no repeating numbers; hence, this feature is not used.

### Decimal to Binary - Selecting the Number of Target Bits

When converting from a real decimal number, for example, 4.3, to a binary number, we must figure out how many bits it should be encoded. The fact that each decimal digit translates to 3.32192809 bits makes this challenging! Our 4.3 example would translate to 6.64 bits. We need to put some thought into this.

When viewing things in binary precision, it becomes clearer because binary is the smallest base. The decimal numbers, like 1, 3, 4, or 9, all have one place in precision, but in binary, these numbers have anywhere from 1 to 4 bits of precision. In fact, we can calculate the number of binary digits by finding `Floor(log-base-2(x)+1)`, or programmatically `(int)Log2(n) + 1`. If we check out some results for just a single digit, `Floor(Log2(3)+1) `is 2 bits, and `Floor(Log2(9)+1)` is 4 bits. We can see the larger the number, the more binary places it will have and, thus, the more binary precision it will have. 19 has more significant binary digits than 11. So, the number of bits required to represent a number grows as the number grows in binary.

However, unexpected issues could arise. If we multiply 3 by 7, we expect to get 21. In multiplication, the output precision is the smaller one of the two factors. Since 3 is just 2 bits, the output should also have two bits (plus its shift). So instead of 3 x 7 = 21 (or 11 x 111 = 10101), we end up with a confusing 18 (or, 11 x 111 = 11 << 3 => 18). Here come the hidden guard bits to the rescue again. This oddity goes away with just a few extra hidden guard bits for this example. (11.000 x 111.000 = 10101. => 21). Hidden guard bits will prevent this, but only when the multipliers differ by less than 32 bits. If we take two multipliers with even more differences in size, it will exhaust the guard bits.

### Output Notation

When interpreting the outputs from the examples provided, you may encounter figures represented as "`**232XXXXXXXX**`". This format is utilized to differentiate between the segments of the output that are within the bounds of precision and those that are not. Specifically, the "`232`" portion signifies the digits that are precise and reliable. The sequence of "X" characters indicates the digits that fall beyond the scope of precision and, as such, are not displayed because their accuracy cannot be guaranteed.

For outputs where the imprecise portion extends significantly, BigFloat adopts scientific notation to convey the scale of these numbers. For instance, an output that might otherwise be shown as "`232XXXXXXXXXXX`" will be presented as "`232 x 10^11`". This shift to scientific notation aids in maintaining clarity, especially when dealing with large numbers where numerous X's can become hard to read.

While it's possible to display these numbers with trailing zeros, like "`232000000000`", doing so could misleadingly imply that the number is precise up to the last zero. This representation contrasts with the practices of many basic calculators and computational tools, which might display out-of-precision digits without clear distinction. More sophisticated calculators and tools prefer to use scientific notation to reflect the precision of the results, a practice `BigFloat` aligns with.

## Maintaining Precision - A Core Focus

Some of `BigFloat`'s recent developments have been focused on rounding to increase accuracy. Initially, `BigFloat` would drop the least significant bits. This is not a huge deal since these removed bits were past even the lower 32 sub-precision guard bits. However, after billions of math operations of constant rounding down, this could eat up all the 32 guard bits and cause an unfavorable result. As the project evolved, the importance of rounding these bits became evident. Rounding helps maintain precision, especially in sequential mathematical operations.

Many of the functions have been updated to round the last sub-precision guard bit, but not all. Some math functions still need updating.

### Rounding in BigFloat

-   **How it's Done**: After an operation, rounding is applied by checking the most significant digit removed. If set, we increment by one. This rounding method is a simple yet effective approach.
-   **Impact on Precision**: Proper rounding can reduce precision loss. After billions of operations, this would even use up the 32 `GuardBits`.\
    While this is more of a perfect-world example, here is an example:
    -   **With Dropping Bits**: With a trillion (or 2^40^) serial add operations and just dropping the extra bits, we would end up with a result 2^39^ too low. This is because half of the math operations would round down when they should be rounding up. This would translate into the bottom 39 bits being out-of-precision, depleting the 32 hidden guard bits and even going into the bits considered in-precision.
    -   **With Rounding:** If we round those dropped bits instead, those trillion serial operations would only see 18 bits affected on average, keeping us within the 32 guard bits, thus maintaining accuracy. We get to the 18 because the rounding is correct 75% of the time. So the standard deviation would be Sqrt(2^40^ / 4) / 2 => an average deviation of 262144 or 18 bits.
-   **Banker's Rounding Not Employed**

    In the realm of floating-point arithmetic, particularly with `Float`/`Double` data types, Banker's rounding plays a pivotal role in enhancing accuracy. This rounding technique is commonly applied in IEEE float operations, where, upon encountering extra bits that exceed the capacity of the representation---akin to encountering a situation where a decision must be made whether to round a number ending in 0.5000... up or down---Banker's rounding opts to round to the nearest even number, effectively rounding up only half of the time. This approach is crucial for IEEE floats, which maintain only a limited number of extra bits, making encounters with such borderline rounding decisions relatively frequent.

    However, in the case of BigFloat, Banker's rounding is not utilized. The reason behind this deviation lies in BigFloat's capacity to handle significantly more `GuardBits`. Given this enhanced bit capacity, the likelihood of a rounding decision falling precisely on the halfway mark is exceedingly rare. As such, the specific conditions that necessitate Banker's rounding in IEEE floats are not a concern for BigFloat, obviating the need for its implementation.

### Theoretical Limits of Precision

Over time, the 32 "guard bits" in our calculations will gradually be consumed. Each rounding operation—whether up or down—discards a fraction of a bit of precision. Through repeated operations, this loss accumulates. For operations on **BigFloat** that adhere to proper rounding rules (though not all functions do), it would take approximately (2$^{32}$ * 2)$^{2}$ * 4 or 1.5 x 10^21 operations, before all the guard bits are exhausted and we begin to lose precision in the main significant bits. 

While this suggests the loss of guard bits would take a considerable amount of time, the reality is that many BigFloat functions and operations do not strictly follow proper rounding rules. As a result, precision degradation will likely occur much sooner in practical use cases.

#### Rounding Example
```
  101.|11001011101101001000101100110100 (approximately 6)
x 100.|01011001101001011100101110110101 (approximately 4)
**============================================================**
  110.|11001100100110110010011001111111[101100...] (for reference, the true answer and bits to removed)
  110.|11001100100110110010011001111111 (if rounding down or dropping bits)
  110.|11001100100110110010011010000000 (if rounded to nearest)
```
^** "|" is the separator for the in-precision and out-of-precision guard bits.*\
*** "[ ]" bits even past the guard bits - the bits to be rounded*^

Even though these bits were in the hidden guard area and are considered out-of-precision, rounding helps with the loss of precision with successive math operations operating on it. The precision slowly decreases with chopping off the bits (i.e., rounding down). However, if rounding is done correctly for some math functions, the rounding up and down of the least significant digit will cancel each other out over time. This is equivalent to counting the number of heads when flipping a coin several times.

Here is an example where guard bits correct cumulative rounding errors. For Reference, the correct answer...
```
1000.110100|000000010000000001010110... (exact)
```

#### Dropping the Bits...
```
  11.101110|011001110100101011001011
 + 1.010001|011001100110110101100010 (add operation)
 **====================================**
 100.111111|110011011011100000101101 (subtotal)
 + 1.010001|011001100110110101100010 (add operation)
 **====================================**
 110.010001|001101000010010110001111 (subtotal)
 + 1.010001|011001100110110101100010 (add operation)
 **====================================**
 111.100010|100110101001001011110001 (subtotal)
 + 1.010001|011001100110110101100010 (add operation)
 **====================================**
1000.110100|000000010000000001010011 (total is off by 3)
```

#### Using Rounding...
```
  11.101110|011001110100101011001011
 + 1.010001|01100110011011010110001011 (round and add operation)
 **====================================**
 100.111111|110011011011100000101110   (subtotal)
 + 1.010001|01100110011011010110001011 (round and add operation)
 **====================================**
 110.010001|001101000010010110010001   (subtotal)
 + 1.010001|01100110011011010110001011 (round and add operation)
 **====================================**
 111.100010|100110101001001011110100   (subtotal)
 + 1.010001|01100110011011010110001011 (round and add operation)
**====================================**
1000.110100|000000010000000001010111   (total - off by 1)
```

## Background of BigFloat

In 2020, I encountered a challenge that required calculations on very large numbers that were not integers. To tackle this, I initially resorted to leveraging a `BigInteger` while manually managing the position of the decimal point. While functional, this makeshift solution proved to be unwieldy, leading to code cluttered, time-intensive to manage, and prone to errors. After a search for an existing tool that met my needs in 2020, I was compelled to create this BigFloat library.

BigFloat was conceived as a modest class, its primary function being to accurately track the position of the radix point---a term synonymous with 'decimal point' but applicable across any numerical base. As time progressed, the library underwent numerous enhancements, expanding its repertoire of functions and significantly improving its precision.

This journey from a simple utility to manage radix points in large-scale arithmetic to a comprehensive BigFloat library exemplifies the evolution of a tool designed to address a specific need, which, through continuous refinement and expansion, has grown to offer robust support for high-precision calculations across a wide array of applications.

## Questions and Answers

-   **Is BigFloat Complete?** While robust and functional, BigFloat is a never-ending project with ongoing enhancements and performance optimizations.
-   **How Long Has BigFloat Been Around?** Starting as a personal tool in November 2020, BigFloat has evolved since then.
-   **Dependencies:** `BigFloat` requires .NET 7 or later and has no other dependencies.
-   **Data Storage:** At its core, BigFloat has three items: (1) `BigInteger` for storing the actual `DataBits`. (2) a `Scale` showing how many binary places to shift the radix point. (3) The data bits size is accessed frequently. To facilitate quick access, this value, equivalent to `ABS(BigInteger).GetBitCount()` is cached.
-   **Why is it called BigFloat?**
    -   `BigFloat`: This would indicate a base-2 number with a floating decimal point.
    -   `BigRational`: This indicates the number is stored as an actual fraction with a numerator and denominator.
    -   `BigDecimal`: This indicates processing/storage is in base-10. However, this class is in base 2. Some projects use Base 2, however, with the name `BigDecimal`.

## Future Wish List

-   Add the `_repeat` for more exact results storage for rational numbers.
-   Finish the `NthRoot()` function. It works but needs to be converted to use `BigInteger` internally for better performance.

## History

-   29^th^ November, 2020: Initial version
-   6^th^ January, 2024: Public release
-   26^th^ February, 2024: Article posted

## Article Creation Process

The development of this article was a synergy of human creativity and artificial intelligence. Initially, Ryan White crafted a comprehensive draft, which was then refined using Word and Grammarly for initial edits. Subsequently, we leveraged the capabilities of ChatGPT 4 to restructure and condense the article. Initially, the extent of information reduction was a concern; however, we recognized the value in brevity, as ChatGPT's edits transformed the piece from a potentially dry technical narrative into a compelling and succinct read.

This iterative process involved continuous enhancements and refinements between manual inputs and AI suggestions. This collaboration streamlined the content and ensured the article maintained a lively and engaging tone. The final touches included meticulous proofreading with Grammarly and Word, underscoring our commitment to quality.

Additionally, the article features the BigFloat image, conceived by ChatGPT, with a minor manual adjustment to incorporate the term `Float` for clarity.

## License

This article, along with any associated source code and files, is licensed under [The MIT License](http://www.opensource.org/licenses/mit-license.php)

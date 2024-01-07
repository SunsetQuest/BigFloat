// Copyright Ryan Scott White. 11/29/2020, 12/26/2020, 1/3/2021, 1/9/2021, 1/13/2021, 1/17/2021, 3/22/2022, 3/28/2022, 7/10/2022, 12/2022, 1/2023, 2/2023, 3/2023, 6/2023, 11/2023, 12/2023, 1/2024

// Released under the MIT License. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

// As of the 1/6/2024 version this class was written by a human only. This will change soon.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using BigFloatLibrary;
//using static BigFloatLibrary.BigFloat;


#pragma warning disable IDE0051  // Ignore unused private members
//#pragma warning disable CS0162 // Ignore unreachable code in playground

namespace ShowCase;

public static class Program
{
    public static void Main()
    {
        //BigConstant_Stuff();
        //BigConstant_Stuff2();
        //Pow_Stuff();
        //PowMostSignificantBits_Stuff();
        //Pow_Stuff3();
        //Pow_Stuff4();
        //ToStringHexScientific_Stuff();
        //Compare_Stuff();
        //Unknown_Stuff();
        //GeneratePi_Stuff();
        //Parse_Stuff();
        //TruncateToAndRound_Stuff();
        //Remainder_Stuff();
        //TryParse_Stuff();
        //Sqrt_Stuff();
        //CastingFromFloatAndDouble_Stuff();
        //NthRoot_DRAFT_Stuff();
    }

    //////////////  BigConstants Play Area & Examples //////////////

    private static void BigConstant_Stuff() //added to test
    {
        BigFloat.BigConstants bigConstants = new(4000);
        BigFloat pi200ref = bigConstants.Pi;                        // 3.141592653589793238462643383279502884197169399375105820974945
        BigFloat pi200gen = BigFloat.BigConstants.GeneratePi(4000); // 3.141592653589793238462643383279502884197169399375105820974945
        Console.WriteLine(pi200ref == pi200gen);

        for (int i = 0; i < 500; i++)
            Console.WriteLine(pi200ref == BigFloat.BigConstants.GeneratePi(i));

        BigFloat.BigConstants c = new(10);
        Console.WriteLine(c);
        Console.WriteLine(c.Pi);
    }

    private static void BigConstant_Stuff2() //added to test
    {
        BigFloat[] bigFloats1000 = BigFloat.BigConstantBuilder.GenerateArrayOfCommonConstants();
        BigFloat[] bigFloats2000 = BigFloat.BigConstantBuilder.GenerateArrayOfCommonConstants();
        for (int i = 0; i < bigFloats1000.Length; i++)
        {
            BigFloat bf1000 = bigFloats1000[i];
            BigFloat bf2000 = bigFloats2000[i];
            if (bf1000 != bf2000)
                Console.WriteLine(bf2000-bf1000);
        }
    }



    //////////////  NthRoot Play Area & Examples //////////////
    private static void NthRoot_DRAFT_Stuff()
    {
        Console.WriteLine($"Ans: val^(1/3) -> 26260231.868889058659811670527341)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("77777777777777777777777777777777")>>32, 3)}");
        Console.WriteLine($"Ans: val^(1/3) -> 42685972166.249808508213684454450)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("77777777777777777777777777777777"), 3)}");
        Console.WriteLine($"Ans: val^(1/2) -> 8944271909999158.7856366946749251)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("80000000000000000000000000000000"), 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 43088693800.637674435185871330387)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("80000000000000000000000000000000"), 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 94574160.900317581330169611988722)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("80000000000000000000000000000000"), 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 2402248.8679628624664841997871983)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("80000000000000000000000000000000"), 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 207578.16311124268746614482713121)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("80000000000000000000000000000000"), 6)}");
        Console.WriteLine($"Ans: val^(1/7) -> 36106.407876409947138175505843180)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("80000000000000000000000000000000"), 7)}");
        Console.WriteLine($"Ans: val^(1/8) -> 9724.9247246607303150644442684673)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("80000000000000000000000000000000"), 8)}");
        Console.WriteLine($"Ans: val^(1/2) -> 1000000000000000.0000000000000000)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000000000"), 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 10000000000.000000000000000000000)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000000000"), 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 31622776.601683793319988935444327)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000000000"), 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 1000000.0000000000000000000000000)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000000000"), 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 100000.00000000000000000000000000)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000000000"), 6)}");
        Console.WriteLine($"Ans: val^(1/7) -> 19306.977288832501670070747998402)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000000000"), 7)}");
        Console.WriteLine($"Ans: val^(1/8) -> 5623.4132519034908039495103977648)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000000000"), 8)}");
        Console.WriteLine($"Ans: val^(1/2) -> 10000000000000.000000000000000000)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000000"), 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 464158883.36127788924100763509194)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000000"), 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 3162277.6601683793319988935444327)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000000"), 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 158489.31924611134852021013733915)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000000"), 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 21544.346900318837217592935665194)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000000"), 6)}");
        Console.WriteLine($"Ans: val^(1/7) -> 5179.4746792312111347551746779610)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000000"), 7)}");
        Console.WriteLine($"Ans: val^(1/8) -> 1778.2794100389228012254211951927)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000000"), 8)}");
        Console.WriteLine($"Ans: val^(1/2) -> 3162277660168.3793319988935444327)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("10000000000000000000000000"), 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 215443469.00318837217592935665194)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("10000000000000000000000000"), 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 1778279.4100389228012254211951927)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("10000000000000000000000000"), 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 100000.00000000000000000000000000)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("10000000000000000000000000"), 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 14677.992676220695409205171148169)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("10000000000000000000000000"), 6)}");
        Console.WriteLine($"Ans: val^(1/7) -> 3727.5937203149401661724906094730)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("10000000000000000000000000"), 7)}");
        Console.WriteLine($"Ans: val^(1/8) -> 1333.5214321633240256759317152953)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("10000000000000000000000000"), 8)}");
        Console.WriteLine($"Ans: val^(1/2) -> 1000000000000.0000000000000000000)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000"), 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 100000000.00000000000000000000000)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000"), 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 1000000.0000000000000000000000000)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000"), 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 63095.734448019324943436013662234)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000"), 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 10000.000000000000000000000000000)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000"), 6)}");
        Console.WriteLine($"Ans: val^(1/7) -> 2682.6957952797257476988026806276)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000"), 7)}");
        Console.WriteLine($"Ans: val^(1/8) -> 1000.0000000000000000000000000000)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000000"), 8)}");
        Console.WriteLine($"Ans: val^(1/2) -> 316227766016.83793319988935444327)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000"), 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 46415888.336127788924100763509194)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000"), 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 562341.32519034908039495103977648)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000"), 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 39810.717055349725077025230508775)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000"), 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 6812.9206905796128549798817963002)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000"), 6)}");
        Console.WriteLine($"Ans: val^(1/7) -> 1930.6977288832501670070747998402)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000"), 7)}");
        Console.WriteLine($"Ans: val^(1/8) -> 749.89420933245582730218427561514)"); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000000"), 8)}");
        Console.WriteLine($"Ans: val^(1/2) -> 100000000000.000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("10000000000000000000000"), 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 21544346.9003188)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("10000000000000000000000"), 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 316227.766016838)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("10000000000000000000000"), 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 25118.8643150958)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("10000000000000000000000"), 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 4641.58883361278)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("10000000000000000000000"), 6)}");
        Console.WriteLine($"Ans: val^(1/2) -> 31622776601.6838)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000"), 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 9999999.99999997)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000"), 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 177827.941003892)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000"), 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 15848.9319246111)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000"), 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 3162.27766016837)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("1000000000000000000000"), 6)}");
        Console.WriteLine($"Ans: val^(1/2) -> 10000000000.0000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000"), 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 4641588.83361278)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000"), 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 100000.000000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000"), 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 10000.0000000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000"), 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 2154.43469003188)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(BigFloat.Parse("100000000000000000000"), 6)}");
        Console.WriteLine($"Ans: val^(1/2) -> 3162277660.16838)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000000000, 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 2154434.69003188)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000000000, 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 56234.1325190350)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000000000, 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 6309.57344480194)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000000000, 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 1467.79926762207)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000000000, 6)}");
        Console.WriteLine($"Ans: val^(1/2) -> 1000000000.00000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000000000, 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 1000000.00000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000000000, 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 31622.7766016838)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000000000, 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 3981.07170553497)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000000000, 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 1000.00000000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000000000, 6)}");
        Console.WriteLine($"Ans: val^(1/2) -> 316227766.016838)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000000000, 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 464158.883361278)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000000000, 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 17782.7941003892)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000000000, 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 2511.88643150958)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000000000, 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 681.292069057961)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000000000, 6)}");
        Console.WriteLine($"Ans: val^(1/2) -> 100000000.000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000000, 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 215443.469003189)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000000, 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 10000.0000000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000000, 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 1584.89319246112)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000000, 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 464.158883361278)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000000, 6)}");
        Console.WriteLine($"Ans: val^(1/2) -> 31622776.6016838)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000000, 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 99999.9999999998)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000000, 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 5623.41325190349)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000000, 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 1000.00000000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000000, 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 316.227766016838)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000000, 6)}");
        Console.WriteLine($"Ans: val^(1/2) -> 10000000.0000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000000, 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 46415.8883361278)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000000, 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 3162.27766016838)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000000, 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 630.957344480194)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000000, 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 215.443469003188)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000000, 6)}");
        Console.WriteLine($"Ans: val^(1/2) -> 3162277.66016838)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000, 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 21544.3469003188)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000, 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 1778.27941003892)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000, 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 398.107170553497)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000, 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 146.779926762207)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000000, 6)}");
        Console.WriteLine($"Ans: val^(1/2) -> 1000000.00000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000, 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 9999.99999999999)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000, 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 1000.00000000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000, 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 251.188643150958)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000, 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 100.000000000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000000, 6)}");
        Console.WriteLine($"Ans: val^(1/2) -> 316227.766016838)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000, 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 4641.58883361278)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000, 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 562.341325190349)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000, 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 158.489319246111)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000, 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 68.1292069057961)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000, 6)}");
        Console.WriteLine($"Ans: val^(1/2) -> 100000.000000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000, 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 2154.43469003188)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000, 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 316.227766016838)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000, 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 100.000000000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000, 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 46.4158883361278)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(10000000000, 6)}");
        Console.WriteLine($"Ans: val^(1/2) -> 31622.7766016838)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000, 2)}");
        Console.WriteLine($"Ans: val^(1/3) -> 1000.00000000000)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000, 3)}");
        Console.WriteLine($"Ans: val^(1/4) -> 177.827941003892)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000, 4)}");
        Console.WriteLine($"Ans: val^(1/5) -> 63.0957344480193)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000, 5)}");
        Console.WriteLine($"Ans: val^(1/6) -> 31.6227766016838)                 "); Console.WriteLine($"  Res: {BigFloat.NthRoot_INCOMPLETE_DRAFT8(1000000000, 6)}");


        Stopwatch timer = Stopwatch.StartNew();
        BigFloat result = BigFloat.NthRoot_INCOMPLETE_DRAFT8(new BigFloat((ulong)3 << 60, -60), 3);
        Console.WriteLine($"NthRootDRAFT {result} (Correct: 3^(1/3) -> 1.4422495703074083823216383107801)");

        result = BigFloat.NthRoot_INCOMPLETE_DRAFT8(new BigFloat((BigInteger)3 << 200, -200), 3);
        Console.WriteLine($"NthRootDRAFT {result} (Correct: 3^(1/3) -> 1.4422495703074083823216383107801)");
        
        timer.Stop(); 
        timer.Reset();
        
        for (int i = 2; i >= 0; i--)
            for (int m = 7; m < 300; m *= 31)
                for (int e = 5; e < 10; e++)
                {
                    BigFloat bf = new((ulong)(m) << 60, -60);
                    //timer = Stopwatch.StartNew();
                    timer.Restart();
                    timer.Start();
                    BigFloat result2 = BigFloat.NthRoot_INCOMPLETE_DRAFT8(bf, e);
                    timer.Stop();
                    if (i == 0) Console.WriteLine($"{m}^(1/{e}) = {result2}  correct:{double.Pow((double)bf, 1 / (double)e)}  ticks {timer.ElapsedTicks}");
                }

        Console.WriteLine(BigFloat.NthRoot_INCOMPLETE_DRAFT8(100000000000, 5));
    }

    //////////////  Pow() Play Area & Examples //////////////
    private static void Pow_Stuff()
    {
        double ii = 1176490;
        int j = 3;

        BigFloat res3 = BigFloat.Pow((BigFloat)ii, j);
        BigFloat exp3 = (BigFloat)double.Pow(ii, j);
        IsTrue(res3 == exp3, $"Failed on: {ii}^{j}, result:{res3} exp:{exp3}");

        //// BigFloat.Zero  BigFloat.One
        //IsTrue(BigFloat.Pow(BigFloat.Zero, 0) == 1, $"Failed on: 0^0");
        //IsTrue(BigFloat.Pow(BigFloat.One, 0) == 1, $"Failed on: 1^0");
        //IsTrue(BigFloat.Pow(0, 0) == 1, $"Failed on: 0^0");
        //IsTrue(BigFloat.Pow(1, 0) == 1, $"Failed on: 1^0");
        //IsTrue(BigFloat.Pow(2, 0) == 1, $"Failed on: 2^0");
        //IsTrue(BigFloat.Pow(3, 0) == 1, $"Failed on: 3^0");

        //IsTrue(BigFloat.Pow(BigFloat.Zero, 1) == 0, $"Failed on: 0^1");
        //IsTrue(BigFloat.Pow(BigFloat.One, 1) == 1, $"Failed on: 1^1");
        //IsTrue(BigFloat.Pow(0, 1) == 0, $"Failed on: 0^1");
        //IsTrue(BigFloat.Pow(1, 1) == 1, $"Failed on: 1^1");
        //IsTrue(BigFloat.Pow(2, 1) == 2, $"Failed on: 2^1");
        //IsTrue(BigFloat.Pow(3, 1) == 3, $"Failed on: 3^1");

        //IsTrue(BigFloat.Pow(BigFloat.Zero, 2) == 0, $"Failed on: 0^2");
        //IsTrue(BigFloat.Pow(BigFloat.One, 2) == 1, $"Failed on: 1^2");
        IsTrue(BigFloat.Pow(0, 2) == 0, $"Failed on: 0^2");
        IsTrue(BigFloat.Pow(1, 2) == 1, $"Failed on: 1^2");
        IsTrue(BigFloat.Pow(2, 2) == 4, $"Failed on: 2^2");
        IsTrue(BigFloat.Pow(3, 2) == 9, $"Failed on: 3^2");

        //IsTrue(BigFloat.Pow(BigFloat.Zero, 3) == 0, $"Failed on: 0^3");
        //IsTrue(BigFloat.Pow(BigFloat.One, 3) == 1, $"Failed on: 1^3");
        IsTrue(BigFloat.Pow(0, 3) == 0, $"Failed on: 0^3");
        IsTrue(BigFloat.Pow(1, 3) == 1, $"Failed on: 1^3");
        IsTrue(BigFloat.Pow(2, 3) == 8, $"Failed on: 2^3");
        IsTrue(BigFloat.Pow(3, 3) == 27, $"Failed on: 3^3");

        IsTrue(BigFloat.Pow(BigFloat.Parse("0.5"), 2) == BigFloat.Parse("  0.25"), $"Failed on: 0.5^2");
        IsTrue(BigFloat.Pow(BigFloat.Parse("1.5"), 2) == BigFloat.Parse("  2.25"), $"Failed on: 1.5^2");
        IsTrue(BigFloat.Pow(BigFloat.Parse("2.5"), 2) == BigFloat.Parse("  6.25"), $"Failed on: 2.5^2");
        IsTrue(BigFloat.Pow(BigFloat.Parse("3.5"), 2) == BigFloat.Parse(" 12.25"), $"Failed on: 3.5^2");
        IsTrue(BigFloat.Pow(BigFloat.Parse("0.5"), 3) == BigFloat.Parse(" 0.125"), $"Failed on: 0.5^3");
        IsTrue(BigFloat.Pow(BigFloat.Parse("1.5"), 3) == BigFloat.Parse(" 3.375"), $"Failed on: 1.5^3");
        IsTrue(BigFloat.Pow(BigFloat.Parse("2.5"), 3) == BigFloat.Parse("15.625"), $"Failed on: 2.5^3");
        IsTrue(BigFloat.Pow(BigFloat.Parse("3.5"), 3) == BigFloat.Parse("42.875"), $"Failed on: 3.5^3");
        IsTrue(BigFloat.Pow(BigFloat.Parse("0.5"), 4) == BigFloat.Parse(" 0.0625"), $"Failed on: 0.5^4");
        IsTrue(BigFloat.Pow(BigFloat.Parse("1.5"), 4) == BigFloat.Parse(" 5.0625"), $"Failed on: 1.5^4");
        IsTrue(BigFloat.Pow(BigFloat.Parse("2.5"), 4) == BigFloat.Parse("39.0625"), $"Failed on: 2.5^4");
        IsTrue(BigFloat.Pow(BigFloat.Parse("3.5"), 4) == BigFloat.Parse("150.0625"), $"Failed on: 3.5^4");

        for (int k = 3; k < 20; k++)
        {
            for (double i = 1; i < 20; i = 1 + (i * 1.7))
            {
                BigFloat exp2 = (BigFloat)double.Pow(i, k);
                BigFloat res2 = BigFloat.Pow((BigFloat)i, k);
                IsTrue(res2 == exp2, $"Failed on: {i}^{k}, result:{res2} exp:{exp2}");
            }
        }

        BigFloat tbf = new(255, 0);
        _ = BigFloat.Pow(tbf, 3);
        tbf = new BigFloat(256, 0);
        _ = BigFloat.Pow(tbf, 3);
        tbf = new BigFloat(511, 0);
        _ = BigFloat.Pow(tbf, 3);
        tbf = new BigFloat(512, 0);
        _ = BigFloat.Pow(tbf, 3);
    }

    private static void PowMostSignificantBits_Stuff()
    {
        Stopwatch timer = Stopwatch.StartNew();
        long errorTotal = 0; // too high or low by 2 or more

        Parallel.For(2, 8192, bitSize =>
        //for (int bitSize = 2; bitSize < 1026; bitSize += 1)
        {

            int exp = 0;
            BigInteger miss = 0;
            long correctBits = 0;
            BigInteger ans = 0;

            long counter = 0;
            long oneTooHi = 0; // too high by 1
            long oneTooLo = 0; // too low  by 1
            Random random = new(bitSize + 1);
            for (int valTries = 0; valTries < 10; valTries++)
            {
                BigInteger val = GenerateRandomBigInteger(bitSize);
                int valSize = (int)val.GetBitLength();
                for (int expSize = 2; expSize < 12; expSize++)
                {
                    int wantedBits;
                    //int wantedBits = valSize;  
                    for (wantedBits = 1; wantedBits < (valSize + 2); wantedBits++)
                    {
                        exp = (int)GenerateRandomBigInteger(expSize);

                        if ((long)exp * Math.Max(valSize, wantedBits) >= int.MaxValue)
                        {
                            continue;
                        }

                        //if (/*exp < 3 ||*/ val < 3 /*|| wantedBits < 3*/)
                        //{
                        //    continue;
                        //}

                        // Answer Setup version 1
                        //BigInteger ans = BigFloat.PowMostSignificantBits(val, valSize, exp, out int shiftedAns, wantedBits, true);

                        // Answer Setup version 2
                        BigInteger p = BigInteger.Pow(val, exp);
                        int shiftedAns = Math.Max(0, (int)(p.GetBitLength() - Math.Min(wantedBits, valSize)));
                        bool overflowed = BigFloat.RightShiftWithRoundWithCarryDownsize(out ans, p, shiftedAns);
                        if (overflowed) shiftedAns++;
                        if (val.IsZero) shiftedAns = 0;

                        // Result Setup
                        timer.Restart();
                        BigInteger res = BigFloat.PowMostSignificantBits(val, exp, out int shiftedRes, valSize, wantedBits, false);
                        timer.Stop();
                        //int needToShiftAgainBy2 = (int)(res.GetBitLength() - wantedBits);
                        //res = BigFloat.RightShiftWithRound(res, needToShiftAgainBy2); shifted += needToShiftAgainBy2;
                        //BigInteger res4 = BigFloat.Pow4(val, valSize, exp, out int shifted4, workSize);

                        if (shiftedAns < 0)
                        {
                            Console.WriteLine($"Error - depending on the mode a negative shiftedAns is not supported ");
                        }

                        if (shiftedAns - shiftedRes == 1)  // res did not round up
                        {
                            ans <<= 1;
                        }
                        else if (shiftedAns - shiftedRes == -1)  // ans did not round up
                        {
                            res <<= 1;
                        }
                        else if (shiftedRes != shiftedAns)
                        {
                            if (shiftedRes > shiftedAns + 1)
                            {
                                ans = BigFloat.RightShiftWithRound(ans, shiftedRes - shiftedAns);
                            }
                            else
                            {
                                Console.WriteLine($"ERRORRRRR - Answer should always be at least 2 bits larger");
                            }
                        }

                        miss = ans - res;
                        correctBits = ans.GetBitLength() - miss.GetBitLength();
                        //Console.Write($", {valSize}, {(val.IsEven?1:0)}, {exp}, {(exp % 2 == 0 ? 1 : 0)}, {expSize}, wantedBits:{wantedBits}, got:{correctBits} ({ans.GetBitLength()} - {diff.GetBitLength()})  miss:({wantedBits- correctBits})\r\n");

                        Interlocked.Increment(ref counter);

                        if (BigInteger.Abs(miss) > 1)
                        {
                            Console.Write($"!!!!!!! diff:{miss,-2}[{miss.GetBitLength()}] valSize:{valSize,-4}exp:{exp,-7}[{expSize,-2}] wantedBits:{wantedBits,-4}got:{correctBits,-4}ansSz:{ans.GetBitLength(),-3} trails:{counter,-5}({(float)(errorTotal / (float)counter),-5}) val:{val}\r\n");
                            Interlocked.Increment(ref errorTotal);
                        }
                        else if (miss > 0)
                        {
                            //Console.Write($"OK but ans 1 too Low, valSize:{valSize,-4}exp:{exp,-7}[{expSize,-2}] wantedBits:{wantedBits,-4}got:{correctBits,-4}ansSz:{ans.GetBitLength(),-3} trails:{counter,-5}({(float)(errorTotal / (float)counter),-5}) val:{val}\r\n");
                            Interlocked.Increment(ref oneTooLo);
                        }
                        else if (miss < 0)
                        {
                            Console.Write($"OK but ans 1 too High,  valSize:{valSize,-4}exp:{exp,-7}[{expSize,-2}] wantedBits:{wantedBits,-4}got:{correctBits,-4}ansSz:{ans.GetBitLength(),-3} trails:{counter,-5}({(float)(errorTotal / (float)counter),-5}) val:{val}\r\n");
                            Interlocked.Increment(ref oneTooHi);
                        }
                    }
                    if (val % 8192 == 777)
                        Console.Write($"sz:{bitSize,3},diff:{miss,-3}[{miss.GetBitLength()}] valSize:{valSize,-4}exp:{exp,-9}[{expSize,-2}] wantedBits:{wantedBits,-4}" +
                                        $" got:{correctBits,-4}ansSz:{ans.GetBitLength(),-3} count:{counter,-7}(Lo({(float)oneTooLo / counter,-4:F5} hi{(float)(oneTooHi /*/ (float)counter*/),-1})" +
                                        $" time:{(timer.ElapsedTicks),3} val:{val}\r\n");

                }
            }

            //if (errorTotal > 0 || oneTooLo > 0 || oneTooHi > 0)
            //    Console.WriteLine($"errorTotal:{errorTotal} oneTooLo:{oneTooLo} oneTooHi:{oneTooHi}");
        });


        static BigInteger GenerateRandomBigInteger(int maxNumberOfBits)
        {
            byte[] data = new byte[(maxNumberOfBits / 8) + 1];
            Random.Shared.NextBytes(data);
            data[^1] >>= 8 - (maxNumberOfBits % 8);
            return new(data, true);
        }
    }

    private static void Pow_Stuff3()
    {
        Stopwatch timer = new();
        timer.Start();

        int countNotExact = 0, wayLow = 0, minus2 = 0, minus1 = 0, plus1 = 0, plus2 = 0, plus3 = 0, wayHigh = 0, total = 0;
        //for (BigInteger val = 3; val < 3000; val += 1)
        //int b = 0;
        for (int b = 0; b < 99999999; b += 7)
        {
            timer.Reset();
            for (BigInteger val = BigInteger.One << b; val < (BigInteger.One << (b + 1)); val += BigInteger.Max(BigInteger.One, BigInteger.One << (b - 6 + 1)))
            //for (int v = b * 1000; v < (b + 1) * 1000; v ++)
            //Parallel.For(b * 1000, (b + 1) * 1000, v =>
            {
                //BigInteger val = (BigInteger)v * (BigInteger)1 + 2;
                int valSize = (int)val.GetBitLength();
                uint maxPowSearch = 18000;// (uint)(3502.0 / (valSize - 0)) + 1;
                //for (BigInteger val = 3; val < (BigInteger)float.MaxValue; val = (BigInteger)((double)val * 2.13 + 1))
                //for (BigInteger val = (((BigInteger)1) <<b); val < (((BigInteger)1) << (b+1)); val = (BigInteger)((double)val * 1.02 + 1))
                //for (uint exp = 2; exp < maxPowSearch; exp++)
                for (uint exp = 1; exp < maxPowSearch; exp = (uint)((exp * 1.3) + 1))
                {
                    //if ((valSize * exp) >= 3502)
                    //    continue;
                    timer.Start();
                    //BigInteger res = BigFloat.PowAccurate(val, valSize, /*(int)*/exp, out int shifted);
                    BigInteger res = BigFloat.PowMostSignificantBits(val, (int)exp, out int shifted, valSize);
                    timer.Stop();
                    //BigInteger ans = 0; int shiftedAns =0;
                    BigInteger ans = BigFloat.PowMostSignificantBits(val, (int)exp, out int shiftedAns, valSize);
                    //BigInteger ans = BigFloat.PowAccurate(val, valSize, exp, out int shiftedAns);
                    //BigInteger exact = BigInteger.Pow(val, (int)exp);
                    if (res != ans || shifted != shiftedAns)
                    {
                        //Console.WriteLine($"{res} != {ans}  {val}^{exp}");
                        //Console.WriteLine($"Results for {ToBinaryString(val)}[{val.GetBitLength()}]^{exp}....\r\n" +
                        //    $"  Result: {ToBinaryString(res)}[{res.GetBitLength()}] << {shifted})  OR  {res} \r\n" +
                        //    $"  Answer: {ToBinaryString(ans)}[{ans.GetBitLength()}] << {shiftedAns}\r\n" +
                        //    $"  Exact:  {ToBinaryString(exact)}[{exact.GetBitLength()}]");
                        if ((res >> 1) + 1 == ans)
                        {
                            minus1++;
                            Console.WriteLine("resolved !!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res - 1 == ans)
                        {
                            plus1++;
                            Console.WriteLine($"plus1:{plus1} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res + 1 == ans)
                        {
                            minus1++;
                            Console.WriteLine($"minus1:{minus1} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res - 2 == ans)
                        {
                            plus2++;
                            Console.WriteLine($"plus2:{plus2} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res - 3 == ans)
                        {
                            plus3++;
                            Console.WriteLine($"plus3:{plus3} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res + 2 == ans)
                        {
                            minus2++;
                            Console.WriteLine($"minus2:{minus2} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res > ans)
                        {
                            wayHigh++;
                            Console.WriteLine($"wayHigh:{wayHigh} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else if (res < ans)
                        {
                            wayLow++;
                            Console.WriteLine($"wayLow:{wayLow} m= val={val} exp={exp}!!!!!!!!!!!!!!!!!!!!");
                        }
                        else
                        {
                            Console.WriteLine($"??????????? shifted({shifted}) != shiftedAns({shiftedAns}) ?????????????? m= val={val} pow={exp}");
                        }
                        countNotExact++;
                    }
                    total++;
                    if (total % (1 << 26) == 0)
                    {
                        Console.WriteLine($"b={b,3}  val={val} exp={exp}  maxPowSearch={maxPowSearch}");
                    }
                }
            }//);
            //if (countNotExact > 0)
            {
                //UInt128 count =  ((UInt128)1 << b);
                Console.WriteLine($"bits: {b,3} countNotExact:{countNotExact}  wayLow:{wayLow}  -2:{minus2}  -1:{minus1}  +1:{plus1}  +2:{plus2}  +3:{plus3}  wayHigh:{wayHigh}   total:{total}  ticks: {timer.ElapsedTicks / total}");
                countNotExact = 0;
                wayLow = 0;
                minus2 = 0;
                minus1 = 0;
                plus1 = 0;
                plus2 = 0;
                plus3 = 0;
                wayHigh = 0;
                total = 0;
            }
            //if (m % 1024 == 0)
            //    Console.WriteLine(m);
        }
    }

    private static void Pow_Stuff4()
    {
        for (BigFloat val = 777; val < 778; val++)
            for (int exp = 5; exp < 6; exp++)
            {
                BigFloat res = BigFloat.Pow(val, exp);
                double correct = Math.Pow((double)val, exp);
                Console.WriteLine($"{val,3}^{exp,2} = {res, 8} ({res.Int, 4} << {res.Scale, 2})  Correct: {correct,8}");
            }
    }

    private static void ToStringHexScientific_Stuff()
    {
        //todo: test
        //  "251134829809281403347287120873437924350329252743484439244628997274301027607406903709343370034928716748655001465051518787153237176334136103968388536906997846967216432222442913720806436056149323637764551144212026757427701748454658614667942436236181162060262417445778332054541324179358384066497007845376000000000, 0x596:82F00000[11+32=43],  << 1014"
        //  with BigFloat Pow(BigFloat value, 4)

        (new BigFloat("1.8814224e11")).DebugPrint("1.8814224e11"); //18814224____
        (new BigFloat("-10000e4")).DebugPrint("-10000e4");
        Console.WriteLine(new BigFloat("-32769").ToStringHexScientific(showInTwosComplement: true));
        Console.WriteLine(new BigFloat("-32769").ToStringHexScientific());
        new BigFloat("-32769").DebugPrint(); //100000000

        Console.WriteLine(new BigFloat("8814224057326597640e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597642e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597646e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597648e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597650e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597652e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597654e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597656e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597658e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597660e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597662e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597664e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597666e16").ToStringHexScientific());
        Console.WriteLine(new BigFloat("8814224057326597668e16").ToStringHexScientific());

        //10000000_
        new BigFloat("1.0000000e+8").DebugPrint(); //100000000
        new BigFloat("10000000e1").DebugPrint();   //10000000000
        
        Console.WriteLine(new BigFloat("10000e4").ToStringHexScientific());

        new BigFloat("1.0000000e+8").DebugPrint();
        new BigFloat("10000000e1").DebugPrint();
    }

    private static void Compare_Stuff()
    {
        IsTrue(new BigFloat("1.0e-1") == new BigFloat("0.10"), $"Failed on: 3^0");
        IsTrue(new BigFloat("0.10e2") == new BigFloat("10."), $"Failed on: 3^0");
        IsTrue(new BigFloat("0.0010e2") == new BigFloat("0.10"), $"Failed on: 3^0");
        IsTrue(new BigFloat("1.00e0") == new BigFloat("1.00"), $"Failed on: 3^0");
        IsTrue(new BigFloat("10.0e-1") == new BigFloat("1.00"), $"Failed on: 3^0");
        IsTrue(new BigFloat("100.0e-2") == new BigFloat("1.000"), $"Failed on: 3^0");
        IsTrue(new BigFloat("300.0e-2") == new BigFloat("3.000"), $"Failed on: 3^0");
        IsTrue(new BigFloat("300.0e-2") == new BigFloat("0.03000e+2"), $"Failed on: 3^0");
        IsTrue(new BigFloat("1.0000000e+8") == new BigFloat("10000000e1"), $"Failed on: 3^0");
        IsTrue(new BigFloat("10000e4").ToStringHexScientific() == "17D7 << 14");
    }

    private static void GeneratePi_Stuff()
    {
        for (int i = 400000; i <= 400000; i += 4)
        {
            Stopwatch timer = Stopwatch.StartNew();
            BigFloat res2 = BigFloat.BigConstants.GeneratePi(i);
            timer.Stop();
            Console.WriteLine($"{i,4} {res2.ToString("")} {timer.ElapsedTicks}");
        }
        Console.WriteLine($"perfect: 3.1415926535897932384626433832795028841971693993751058209749445923078164062862089986280348253421170679821480865132823066470938446095505822317253594081284811174502841027019385211055596446229");
    }

    private static void Parse_Stuff()
    {
        BigFloat aa = BigFloat.Parse("0b100000.0");
        BigFloat bb = BigFloat.Parse("0b100.0");
        BigFloat rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b100000");
        bb = BigFloat.Parse("0b100.0");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b100000.0");
        bb = BigFloat.Parse("0b100");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b10000.0");
        bb = BigFloat.Parse("0b1000");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b1000.00000");
        bb = BigFloat.Parse("0b10000");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b1000.0");
        bb = BigFloat.Parse("0b10000.000");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b1.0000");
        bb = BigFloat.Parse("0b10000000.");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");

        aa = BigFloat.Parse("0b1000.00000000000000000000000000");
        bb = BigFloat.Parse("0b10000.0000000");
        rr = aa * bb;
        Console.WriteLine($"[{aa.Size,2}] + [{bb.Size,2}] = {rr,8} {Math.Min(aa.Size, bb.Size)}={rr.Size}");
    }

    private static void TruncateToAndRound_Stuff()
    {
        Console.WriteLine(BigFloat.TruncateToAndRound((BigInteger)0b111, 1));
        Console.WriteLine(BigFloat.TruncateToAndRound((BigInteger)0b111, 2));
        Console.WriteLine(BigFloat.TruncateToAndRound((BigInteger)0b111, 3));
        Console.WriteLine(BigFloat.TruncateToAndRound((BigInteger)(-0b111), 1));
        Console.WriteLine(BigFloat.TruncateToAndRound((BigInteger)(-0b1000), 1));
    }

    private static void Remainder_Stuff()
    {
        //BigFloat bf = new BigFloat(1, -8);
        BigFloat bf = new("0.00390625");
        BigFloat res = BigFloat.Remainder(bf, new BigFloat("1.00000000")); // i.e. "bf % 1;"
        // Answer of 0.00390625 % 1 is:
        //   0.00390625 or 0.0039063 or 0.003906 or 0.00391 or 0.0039 or 0.004 or 0.00 or or 0.0 or or 0

        IsTrue(res == new BigFloat("0.004"), "0.004");
        IsTrue(res == new BigFloat("0.0039"), "0.0039");
        IsTrue(res == new BigFloat("0.00391"), "0.00391");
        IsTrue(res == new BigFloat("0.003906"), "0.003906");
        IsTrue(res == new BigFloat("0.0039063"), "0.0039063");
        IsTrue(res == new BigFloat("0.00390625"), "0.00390625");

        IsFalse(res == 0, "(Int)0");
        IsFalse(res == new BigFloat("0"), "0");
        IsFalse(res == new BigFloat("0.0"), "0.0");
        IsFalse(res == new BigFloat("0.00"), "0.00");

        IsFalse(res == new BigFloat("0.003"), "0.003");   // values that are a smudge to small
        IsFalse(res == new BigFloat("0.0038"), "0.0038");   // values that are a smudge to small
        IsFalse(res == new BigFloat("0.00390"), "0.00390");   // values that are a smudge to small
        IsFalse(res == new BigFloat("0.003905"), "0.003905");   // values that are a smudge to small
        IsFalse(res == new BigFloat("0.0039062"), "0.0039062");   // values that are a smudge to small
        IsFalse(res == new BigFloat("0.00390624"), "0.00390624");   // values that are a smudge to small

        IsFalse(res == new BigFloat("0.005"), "0.005");   // values that are a smudge to large
        IsFalse(res == new BigFloat("0.0040"), "0.0040");   // values that are a smudge to large
        IsFalse(res == new BigFloat("0.00392"), "0.00392");   // values that are a smudge to large
        IsFalse(res == new BigFloat("0.003907"), "0.003907");   // values that are a smudge to large
        IsFalse(res == new BigFloat("0.0039064"), "0.0039064");   // values that are a smudge to large
        IsFalse(res == new BigFloat("0.00390626"), "0.00390626");   // values that are a smudge to large
    }

    private static void TryParse_Stuff()
    {
        _ = BigInteger.TryParse("15.0",
                NumberStyles.AllowDecimalPoint,
                null, out BigInteger number);

        Console.WriteLine(number.ToString());

        //BigFloat bf0 = new BigFloat("1.1"); Console.WriteLine(bf0.DebuggerDisplay);
        BigFloat bf0 = new("-1.1"); bf0.DebugPrint("");
        //BigFloat bf0 = new BigFloat("-1.1"); Console.WriteLine(bf0.DebuggerDisplay);
    }

    private static void Sqrt_Stuff()
    {
        BigFloat inpParam = new("2.0000000000000000000000000000000");
        BigFloat fnOutput = BigFloat.Sqrt(inpParam);
        BigFloat expected = new("1.4142135623730950488016887242097");
        Console.WriteLine($"Calculate Sqrt( {inpParam} )\r\n" +
                          $"              = {fnOutput}\r\n" +
                          $"      expected: {expected}\r\n");
        BigFloat a = new(1);
        Console.WriteLine(inpParam < a);
    }

    private static void CastingFromFloatAndDouble_Stuff()
    {
        Console.WriteLine((BigFloat)(float)1.0);
        Console.WriteLine((BigFloat)1.0);
        Console.WriteLine((BigFloat)(float)0.5);
        Console.WriteLine((BigFloat)0.5);
        Console.WriteLine((BigFloat)(float)0.25);
        Console.WriteLine((BigFloat)2.0);
        Console.WriteLine((BigFloat)(float)2.0);

        Console.WriteLine("(BigFloat)1     (Double->BigFloat->String) -> " + (BigFloat)1);
        Console.WriteLine("(BigFloat)1.0   (Double->BigFloat->String) -> " + (BigFloat)1.0);
        Console.WriteLine("(BigFloat)1.01  (Double->BigFloat->String) -> " + (BigFloat)1.01);
        Console.WriteLine("(BigFloat)1.99  (Double->BigFloat->String) -> " + (BigFloat)1.99);
        Console.WriteLine("(BigFloat)2.00  (Double->BigFloat->String) -> " + (BigFloat)2.00);
        Console.WriteLine("(BigFloat)2.99  (Double->BigFloat->String) -> " + (BigFloat)2.99);
        Console.WriteLine("(BigFloat)3.00  (Double->BigFloat->String) -> " + (BigFloat)3.00);
        Console.WriteLine("(BigFloat)3.99  (Double->BigFloat->String) -> " + (BigFloat)3.99);
        Console.WriteLine("(BigFloat)4.00  (Double->BigFloat->String) -> " + (BigFloat)4.00);
        Console.WriteLine("(BigFloat)4.99  (Double->BigFloat->String) -> " + (BigFloat)4.99);
        Console.WriteLine("(BigFloat)5.00  (Double->BigFloat->String) -> " + (BigFloat)5.00);
        Console.WriteLine("(BigFloat)5.99  (Double->BigFloat->String) -> " + (BigFloat)5.99);
        Console.WriteLine("(BigFloat)6.00  (Double->BigFloat->String) -> " + (BigFloat)6.00);
        Console.WriteLine("(BigFloat)6.99  (Double->BigFloat->String) -> " + (BigFloat)6.99);
        Console.WriteLine("(BigFloat)7.00  (Double->BigFloat->String) -> " + (BigFloat)7.00);
        Console.WriteLine("(BigFloat)7.99  (Double->BigFloat->String) -> " + (BigFloat)7.99);
        Console.WriteLine("(BigFloat)8.00  (Double->BigFloat->String) -> " + (BigFloat)8.00);
        Console.WriteLine("(BigFloat)8.99  (Double->BigFloat->String) -> " + (BigFloat)8.99);
        Console.WriteLine("(BigFloat)9.00  (Double->BigFloat->String) -> " + (BigFloat)9.00);
        Console.WriteLine("(BigFloat)9.9   (Double->BigFloat->String) -> " + (BigFloat)9.9);
        Console.WriteLine("(BigFloat)10.0  (Double->BigFloat->String) -> " + (BigFloat)10.0);
        Console.WriteLine("(BigFloat)10.1  (Double->BigFloat->String) -> " + (BigFloat)10.1);
        Console.WriteLine("(BigFloat)99.9  (Double->BigFloat->String) -> " + (BigFloat)99.9);
        Console.WriteLine("(BigFloat)100.0 (Double->BigFloat->String) -> " + (BigFloat)100.0);

        Console.WriteLine($"Double: {(double)1234567.8901234567890123} --> BigFloat: {(BigFloat)(double)1234567.8901234567890123}");
        Console.WriteLine($"Double: {(double)887102364.1} --> BigFloat: {(BigFloat)(double)887102364.1}");
        Console.WriteLine($"Double: {(double)887102364.01} --> BigFloat: {(BigFloat)(double)887102364.01}");
        Console.WriteLine($"Double: {(double)887102364.001} --> BigFloat: {(BigFloat)(double)887102364.001}");
        //Console.WriteLine("887102364.0 --> " + (BigFloat)(float) 887102364.0);

        Console.WriteLine($"Double: {(double)9156478887102364.0} --> BigFloat: {(BigFloat)(double)9156478887102364.0}");

        Console.WriteLine($"Double: {(double)887102364.001} --> BigFloat: {(BigFloat)(double)887102364.001}");
        Console.WriteLine($"Double: {(double)9156478887102364.0} --> BigFloat: {(BigFloat)(double)9156478887102364.0}");
        Console.WriteLine($"Float:  {(long)9156478887102364.0} --> BigFloat: {(BigFloat)(long)9156478887102364.0}");

        Console.WriteLine($"Float:  {(long)(float)9156478887102364.0} --> BigFloat: {(BigFloat)(float)9156478887102364.0}");
        Console.WriteLine("9156478887102364.0 --> " + (BigFloat)(float)9156478887102364.0);

        for (double i = 0.000000000000001; i < 9999999999999999.9; i *= 1.1)
        {
            BigFloat bfi = (BigFloat)i;
            double bfd = (double)bfi;
            Console.WriteLine(i.ToString() + " => " + bfi.ToString() + " => " + bfd.ToString());
        }

        Console.WriteLine((BigFloat)0.000000012300000000000000);
        Console.WriteLine((BigFloat)0.011111);
        Console.WriteLine((BigFloat)0.11111111111);
        Console.WriteLine((BigFloat)0.1111111111111);
        Console.WriteLine((BigFloat)(float)0.11111);
        Console.WriteLine((BigFloat)(float)0.1111111);
        Console.WriteLine((BigFloat)(float)0.111111111);
        Console.WriteLine((BigFloat)(float)0.11111111111);
        Console.WriteLine((BigFloat)(float)0.1111111111111);
    }

    private static void CheckIt(BigFloat inputVal, BigFloat output, BigFloat expect)
    {
        Console.WriteLine($"{(output.ToString() == expect.ToString() ? "YES!" : "NO! ")}  Sqrt({inputVal}) " +
            $"\r\n  was      {output + " [" + output.Size + "]",20} " +
            $"\r\n  expected {expect + " [" + expect.Size + "]",20} ");
    }

    private static void PrintStringAsBigFloatAndAsDouble(string origStingValue)
    {
        Console.WriteLine($"Input As String:  {origStingValue,22}");
        Console.WriteLine($" ->Double  ->Str: {double.Parse(origStingValue).ToString("G17", CultureInfo.InvariantCulture),22}");
        bool success = BigFloat.TryParse(origStingValue, out BigFloat bf);
        if (success)
        {
            Console.WriteLine($" ->BigFloat->Str: {bf,22}");
        }
        else
        {
            Console.WriteLine($" ->BigFloat->Str: FAIL");
        }

        Console.WriteLine();
    }

    private static void IsNotEqual(object v1, object v2, string msg = null)
    {
        if (!v1.Equals(v2))
        {
            if (msg != null)
            {
                Console.WriteLine(msg, v1, v2);
            }

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
    }

    /// <summary>
    /// Takes an inputParam and inputFunc and then checks if the results matches the expectedOutput.
    /// </summary>
    /// <param name="inputParam">The input value to apply to the inputFunc.</param>
    /// <param name="inputFunc">The function that is being tested.</param>
    /// <param name="expectedOutput">What the output of inputFunc(inputParam) should be like.</param>
    /// <param name="msg">If they don't match, output this message. Use {0}= input, {1}=results of inputFunc(inputParam) {2}=the value it should be.
    /// Example: "The input value of {0} with the given function resulted in {1}, however the value of {2} was expected."</param>
    //[DebuggerHidden]
    private static void IsNotEqual(string inputParam, Func<string, object> inputFunc, string expectedOutput, string msg = null)
    {
        string a = inputFunc(inputParam).ToString();
        if (!a.Equals(expectedOutput))
        {
            if (msg != null)
            {
                Console.WriteLine(msg, inputParam, a, expectedOutput);
            }

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
    }
    
    [DebuggerHidden]
    private static void IsFalse(bool val, string msg = null)
    {
        IsTrue(!val, msg);
    }

    [DebuggerHidden]
    private static void IsTrue(bool val, string msg = null)
    {
        if (!val)
        {
            Fail(msg);
        }
    }

    [DebuggerHidden]
    private static void Fail(string msg)
    {
        Console.WriteLine(msg ?? "Failed");

        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }
    }
}

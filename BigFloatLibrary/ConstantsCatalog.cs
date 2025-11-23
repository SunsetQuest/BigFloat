// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System;
using System.Collections.Generic;

namespace BigFloatLibrary;

public readonly partial struct BigFloat
{
    /// <summary>
    /// Catalog of available mathematical constants.
    /// </summary>
    public static class Catalog
    {
        // Fundamental constants
        public const string Pi = "Pi";
        public const string E = "E";
        public const string Sqrt2 = "Sqrt2";
        public const string TheodorusConstant_Sqrt3 = "Sqrt3";
        public const string Sqrt_Pi = "SqrtPi";
        public const string GoldenRatio = "GoldenRatio";
        public const string EulerMascheroniConstant = "EulerMascheroni";

        // Number theory constants
        public const string TwinPrimeConstant = "TwinPrime";
        public const string PrimeConstant = "Prime";
        public const string RamanujanSoldnerConstant = "RamanujanSoldner";
        public const string AperyConstant = "Apery";
        public const string Const_1_2020 = "Apery";
        public const string ConwayConstant = "Conway";
        public const string Const_1_3035 = "Conway";

        // Analysis constants
        public const string CatalanConstant = "Catalan";
        public const string KhintchinesConstant = "Khintchine";
        public const string OmegaConstant = "Omega";
        public const string Const_0_5495 = "NegLogGamma";
        public const string GlaisherKinkelinConstant = "GlaisherKinkelin";
        public const string ExpEulerMascheroniConstant = "ExpEulerMascheroni";

        // Physics constants
        public const string FineStructureConstant = "FineStructure";

        // Derived constants
        public const string NaturalLogOfPhi = "NaturalLogarithmOfPhi";
        public const string PiSquared = "PiSquared";
        public const string ESquared = "ESquared";
        public const string PiTimesE = "PiTimesE";
        public const string EPowerPi = "EPowerPi";
        public const string PiPowerE = "PiPowerE";
        public const string PiPowerPi = "PiPowerPi";
        public const string EPowerE = "EPowerE";
        public const string PiDividedByE = "PiDividedByE";
        public const string EDividedByPi = "EDividedByPi";
        public const string LogPi = "LogPi";

        // Trigonometric constants
        public const string Sin2PiDiv5 = "Sin2PiDiv5";
        public const string CosPiDiv8 = "CosPiDiv8";
        public const string CosPiDiv16 = "CosPiDiv16";
        public const string CosPiDiv20 = "CosPiDiv20";
        public const string BuffonConstant = "Buffon";
        public const string SinPiDiv3 = "SinPiDiv3";

        // Misc constants
        public const string PlasticNumber = "Plastic";
        public const string PisotsConstant = "Pisot";
        public const string LemniscateConstant = "Lemniscate";
        public const string IPowerI = "IPowerI";
        public const string AGMMeanPiE = "AGMMeanPiE";
        public const string Const_0_2614 = "MeisselMertens";


        // All available constants
        public static readonly string[] AllConstants =
        [
            // Fundamental
            Pi, E, Sqrt2, TheodorusConstant_Sqrt3, Sqrt_Pi, GoldenRatio, EulerMascheroniConstant,
            
            // Number theory
            TwinPrimeConstant, PrimeConstant, RamanujanSoldnerConstant, Const_0_2614, AperyConstant, ConwayConstant,
            
            // Analysis
            NaturalLogOfPhi, CatalanConstant, KhintchinesConstant, OmegaConstant, Const_0_5495,
            GlaisherKinkelinConstant, ExpEulerMascheroniConstant,
            
            // Physics
            FineStructureConstant,
            
            // Derived
            PiSquared, ESquared, PiTimesE, EPowerPi, PiPowerE, PiPowerPi, EPowerE, PiDividedByE, EDividedByPi, LogPi,
            
            // Trigonometric
            Sin2PiDiv5, CosPiDiv8, CosPiDiv16, CosPiDiv20, BuffonConstant, SinPiDiv3,
            
            // Misc
            PlasticNumber, PisotsConstant, LemniscateConstant, IPowerI, AGMMeanPiE
        ];

        // Mapping from constant IDs to their ConstantInfo
        private static readonly Dictionary<string, ConstantInfo> ConstantInfoMap = new(StringComparer.OrdinalIgnoreCase);

        // Initialize the map with all available constants
        static Catalog()
        {
            // Register fundamental constants
            RegisterConstant(Pi, ConstantBuilder.Const_3_1415);
            RegisterConstant(E, ConstantBuilder.Const_2_7182);
            RegisterConstant(Sqrt2, ConstantBuilder.Const_1_4142);
            RegisterConstant(TheodorusConstant_Sqrt3, ConstantBuilder.Const_1_7320);
            RegisterConstant(Sqrt_Pi, ConstantBuilder.Const_1_7724);
            RegisterConstant(GoldenRatio, ConstantBuilder.Const_1_6180);
            RegisterConstant(EulerMascheroniConstant, ConstantBuilder.Const_0_5772);

            // Register number theory constants
            RegisterConstant(TwinPrimeConstant, ConstantBuilder.Const_0_6601);
            RegisterConstant(PrimeConstant, ConstantBuilder.Const_0_4146);
            RegisterConstant(RamanujanSoldnerConstant, ConstantBuilder.Const_262537);
            RegisterConstant(Const_0_2614, ConstantBuilder.Const_0_2614);
            RegisterConstant(AperyConstant, ConstantBuilder.Const_1_2020);
            RegisterConstant(ConwayConstant, ConstantBuilder.Const_1_3035);

            // Register analysis constants
            RegisterConstant(NaturalLogOfPhi, ConstantBuilder.Const_0_4812);
            RegisterConstant(CatalanConstant, ConstantBuilder.Const_0_9159);
            RegisterConstant(KhintchinesConstant, ConstantBuilder.Const_2_6854);
            RegisterConstant(OmegaConstant, ConstantBuilder.Const_0_5671);
            RegisterConstant(Const_0_5495, ConstantBuilder.Const_0_5495);
            RegisterConstant(GlaisherKinkelinConstant, ConstantBuilder.Const_1_2824);
            RegisterConstant(ExpEulerMascheroniConstant, ConstantBuilder.Const_1_7810);

            // Register physics constants
            RegisterConstant(FineStructureConstant, ConstantBuilder.Const_1_4603);

            // Register derived constants
            RegisterConstant(PiSquared, ConstantBuilder.Const_9_8696);
            RegisterConstant(ESquared, ConstantBuilder.Const_7_3890);
            RegisterConstant(PiTimesE, ConstantBuilder.Const_8_5397);
            RegisterConstant(EPowerPi, ConstantBuilder.Const_23_140);
            RegisterConstant(PiPowerE, ConstantBuilder.Const_22_459);
            RegisterConstant(PiPowerPi, ConstantBuilder.Const_3_6462);
            RegisterConstant(EPowerE, ConstantBuilder.Const_15_154);
            RegisterConstant(PiDividedByE, ConstantBuilder.Const_0_8652); // Pi/e
            RegisterConstant(EDividedByPi, ConstantBuilder.Const_0_8652); // Reusing same constant for e/Pi with different id
            RegisterConstant(LogPi, ConstantBuilder.Const_1_1447);

            // Register trigonometric constants
            RegisterConstant(Sin2PiDiv5, ConstantBuilder.Const_0_9510);
            RegisterConstant(CosPiDiv8, ConstantBuilder.Const_0_9238);
            RegisterConstant(CosPiDiv16, ConstantBuilder.Const_0_9807);
            RegisterConstant(CosPiDiv20, ConstantBuilder.Const_0_9876);
            RegisterConstant(BuffonConstant, ConstantBuilder.Const_0_6366);
            //RegisterConstant(SinPiDiv3, ConstantBuilder.Const_0_8660); // Assuming there's a constant for √3/2

            // Register miscellaneous constants
            RegisterConstant(PlasticNumber, ConstantBuilder.Const_1_3247);
            RegisterConstant(PisotsConstant, ConstantBuilder.Const_1_3802);
            RegisterConstant(LemniscateConstant, ConstantBuilder.Const_0_5990);
            RegisterConstant(IPowerI, ConstantBuilder.Const_0_2078);
            RegisterConstant(AGMMeanPiE, ConstantBuilder.Const_0_9261);
        }

        private static void RegisterConstant(string id, ConstantInfo info)
        {
            ConstantInfoMap[id] = info;
        }

        /// <summary>
        /// Tries to get the ConstantInfo for a given constant identifier.
        /// </summary>
        /// <param name="constantId">The identifier of the constant.</param>
        /// <param name="info">The ConstantInfo if found.</param>
        /// <returns>True if the constant was found, false otherwise.</returns>
        public static bool TryGetInfo(string constantId, out ConstantInfo info)
        {
            return ConstantInfoMap.TryGetValue(constantId, out info);
        }
    }
}
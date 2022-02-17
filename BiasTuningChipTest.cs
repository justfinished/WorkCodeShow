using Nokia.Tap.Driver.Interfaces.Common;
using Nokia.Tap.Driver.Interfaces.DUTControl;
using Nokia.Tap.Driver.Interfaces.PSU;
using Nokia.Tap.StdLib.Results;
using Nokia.Tap.StdLib;
using Nokia.Tap.TestSteps.Projects.Galvatron.BiasTuningLib;
using Nokia.Tap.TestSteps.Projects.Galvatron.CommonTest;
using Nokia.Tap.TestSteps.Projects.Galvatron.Extensions;
using Nokia.Tap.TestSteps.Projects.Galvatron.Utilities;
using OpenTap;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System;

namespace Nokia.Tap.TestSteps.Projects.Galvatron.TrxPaTest
{
    [Display("BiasTuningChipTest", Groups: new[] { "Nokia", "TestSteps", "Projects", "Galvatron", "TrxPaTest" }, Description: "Bias Tuning for one AMC7904 chip")]
    public class BiasTuningChipTest : GalvatronTestBase
    {
        #region Interface

        [Display("Dut Type", Group: TestParameterConstants.NokiaTestParameterGroups.Interfaces, Order: 101, Description: "Please define Dut interface type here")]
        public IDutControl Dut { get; set; }

        [Display("Psu Type", Group: TestParameterConstants.NokiaTestParameterGroups.Interfaces, Order: 102, Description: "Please define Psu interface type here")]
        public IPsu Psu { get; set; }

        //[Display("DutInformation Type", Group: TestParameterConstants.NokiaTestParameterGroups.Interfaces, Order: 199, Description: "Dut Information")]
        //public DutInformation DutInformation { get; set; }

        #endregion

        #region Parameters

        /// <summary>
        /// D32001_DAC0_Pipe3_Driver, etc
        /// </summary>
        [Display("Item Name List", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 201, Description: "The test item names, eg: D32001_DAC0_Pipe3_Driver， D33001_DAC0_Pipe1_Peak")]
        public List<string> ItemNameList { get; set; }

        /// <summary>
        /// 0x40u, 0x41u, 0x42u, 0x43u, 0x44u
        /// </summary>
        [Display("Amc Address List", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 202, Description: "AMC7904 Address like 0x40, 0x41")]
        public uint AmcAddress { get; set; }

        /// <summary>
        /// 0x08, 0x0A, 0X0C, 0X0E
        /// </summary>
        [Display("Dac Address List", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 203, Description: "Dac Address in one AMC7904, eg: 0x08 for DAC0, 0x0C for DAC2")]
        public List<uint> DacAddressList { get; set; }

        /// <summary>
        /// DAC0, DAC1, ...
        /// </summary>
        [Display("Dac Name List", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 204, Description: "Dac names in one AMC7904, eg: DAC0, DAC1, DAC2, DAC3")]
        public List<EDacName> DacNameList { get; set; }

        [Display("Min Dac List", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 205, Description: "Min Dac for all DACx in one AMC7904")]
        public List<int> MinDac { get; set; }

        [Display("Max Dac List", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 206, Description: "Max Dac for all DACx in one AMC7904")]
        public List<int> MaxDac { get; set; }

        [Display("Initial Dac List", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 207, Description: "Initial Dac for all DACx in one AMC7904")]
        public List<int> InitialDac { get; set; }

        [Display("Dac Step", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 208, Description: "Dac step for all DACx in one AMC7904")]
        public List<int> DacStep { get; set; }

        [Display("Target Current(mA)", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 209, Description: "Target current for all DACx in one AMC7904")]
        [Unit("mA", true)]
        public List<double> TargetCurrent { get; set; }

        [Display("Target Current Tolerance(mA)", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 210, Description: "Target current tolerance for all DACx in one AMC7904")]
        [Unit("mA", true)]
        public List<double> TargetCurrentTolerance { get; set; }

        [Display("Measure Device(Not in Use)", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 211, Description: "Not in use")]
        public ECurrentMeasureDevice MeasureDevice { get; set; }

        [Display("Dac Device(Not in Use)", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 212, Description: "Not in use")]
        public EBiasDacDevice DacDevice { get; set; }

        [Display("Bias Current Protection", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 213, Description: "Bias current protection for all DACx in one AMC7904, should be bigger than target limit")]
        [Unit("mA", true)]
        public List<double> BiasProtectCurrent { get; set; }

        [Display("Max Loop Count", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 214, Description: "Max loop count for all DACx in one AMC7904")]
        public List<int> MaxLoopCount { get; set; }

        [Display("Psu Channel used to Read PA Current", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 215, Description: "Psu channel, range: [1, 4], for Driver the channel same with 28V, for Final the channel same with 51V")]
        public int PsuChannel { get; set; }

        [Display("Stable Current Tolerance", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 216, Description: "the tolerance for measure stable current, the last three measured current range should be smaller than this value")]
        [Unit("mA")]
        public double StableCurrentTolerancemA { get; set; }

        [Display("Stable Current Timeout", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 217, Description: "the timeout for measure stable current")]
        [Unit("Ms")]
        public int StableCurrentTimeoutMs { get; set; }

        [Display("Pa Amplifier Stage", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 218, Description: "the pa amplifer stage, Driver or Final, which will be used in LUT")]
        public EPaAmplifierStage PaAmplifierStage { get; set; }

        [Display("Peak Dac Backward", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 219, Description: "Please check whether need to save baseline with backward value for Peak stage")]
        public int PeakStoreBackward { get; set; }

        [Display("Bias LUT Path", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 220, Description: "Please select the LUT file for bias tuning and storage.")]
        public string LutTable { get; set; }

        [Display("Offset direction of the LUT curve", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 221, Description: "Please select the polarity of LUT, MonotonicallyIncreasing => Polarity 0; MonotonicallyDecreasing => Polarity 1")]
        public EOffsetDirection Polarity { get; set; }

        [Display("Is Use Fix Value", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 222, Description: "Select if use fix value or tuned value for DACx, true means fix value")]
        public List<bool> IsUseFixDacValue { get; set; }

        [Display("Fix Value for DACx", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 223, Description: "Fix values for DACx for one AMC7904 if configed using a fix value in parameter: IsUseFixDacValue")]
        public List<int> FixDacValue { get; set; }

        #endregion

        #region Limits

        /// <summary>
        /// 30mA, 45mA, 250mA   -2mA
        /// </summary>
        [Display("Current Low Limit List", Group: TestParameterConstants.NokiaTestParameterGroups.TestLimits, Order: 301, Description: "Limit list for measured current with pa bias tuning, use \",\" as separator")]
        [Unit("mA", true)]
        public List<double> CurrentLowLimitList { get; set; }

        /// <summary>
        /// 30mA, 45mA, 250mA   +2mA
        /// </summary>
        [Display("Current High Limit List", Group: TestParameterConstants.NokiaTestParameterGroups.TestLimits, Order: 302, Description: "Limit list for measured current with pa bias tuning, use \",\" as separator")]
        [Unit("mA", true)]
        public List<double> CurrentHighLimitList { get; set; }

        /// <summary>
        /// Dac Low Limit List
        /// </summary>
        [Display("Dac Low Limit List", Group: TestParameterConstants.NokiaTestParameterGroups.TestLimits, Order: 303, Description: "Limit list for Dac with pa bias tuning, use \",\" as separator")]
        [Unit("Dac")]
        public List<int> DacLowLimitList { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Display("Dac High Limit List", Group: TestParameterConstants.NokiaTestParameterGroups.TestLimits, Order: 304, Description: "Limit list for Dac with pa bias tuning, use \",\" as separator")]
        [Unit("Dac")]
        public List<int> DacHighLimitList { get; set; }

        /// <summary>
        /// Temperature Low Limit List
        /// </summary>
        [Display("Temperature Low Limit List", Group: TestParameterConstants.NokiaTestParameterGroups.TestLimits, Order: 305, Description: "Limit list for Temperature with pa bias tuning, use \",\" as separator")]
        [Unit("°C")]
        public List<double> TemperatureLowLimitList { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Display("Temperature High Limit List", Group: TestParameterConstants.NokiaTestParameterGroups.TestLimits, Order: 306, Description: "Limit list for Temperature with pa bias tuning, use \",\" as separator")]
        [Unit("°C")]
        public List<double> TemperatureHighLimitList { get; set; }
        #endregion

        protected static object InstrumentLock = new object();
        protected volatile PaBiasTemperatureCorrectionData LutData;
        private volatile int _baseLineTemperature = 24;
        private volatile int _lutTemperatureStep = 4;

        private AmcInfo amcInfo;
        private bool isInitialCurrent = false;

        public BiasTuningChipTest()
        {
            ItemNameList = new List<string>() {
                "D32001_DAC0_Pipe3_G375_Driver",
                "D32001_DAC1_Pipe1_G375_Driver",
                "D32001_DAC2_Pipe2_G375_Driver",
                "D32001_DAC3_Pipe4_G375_Driver"
            };
            AmcAddress = 0x40u;
            DacAddressList = new List<uint>() { 0x08, 0x0A, 0X0C, 0X0E };
            DacNameList = new List<EDacName>() { EDacName.DAC0, EDacName.DAC1, EDacName.DAC2, EDacName.DAC3 };
            MinDac = new List<int>();
            MaxDac = new List<int>();
            InitialDac = new List<int>();
            DacStep = new List<int>();
            TargetCurrent = new List<double>();
            TargetCurrentTolerance = new List<double>();
            BiasProtectCurrent = new List<double>();
            StableCurrentTolerancemA = 0.2;
            StableCurrentTimeoutMs = 5000;
            MaxLoopCount = new List<int>() { 50, 50, 50, 50 };
            CurrentLowLimitList = new List<double>();
            CurrentHighLimitList = new List<double>();
            DacLowLimitList = new List<int>();
            DacHighLimitList = new List<int>();
            IsUseFixDacValue = new List<bool>() { false, false, false, false };
            FixDacValue = new List<int>() { 0, 0, 0, 0 };

            Polarity = EOffsetDirection.MonotonicallyIncreasing;

            Rules.Add(() => MinDac.Count == MaxDac.Count && MinDac.Count == InitialDac.Count && DacStep.Count == InitialDac.Count, $"{nameof(MinDac)}/{nameof(MaxDac)}/{nameof(InitialDac)}/{nameof(DacStep)} count must be equal", $"{nameof(MinDac)}/{nameof(MaxDac)}/{nameof(InitialDac)}/{nameof(DacStep)}");
            Rules.Add(() => TargetCurrent.Count == TargetCurrentTolerance.Count, "Current and tolerance count must be equal", $"{nameof(TargetCurrent)}/{nameof(TargetCurrentTolerance)}");
            Rules.Add(() => File.Exists(LutTable), "LUT file not found", $"{nameof(LutTable)}");
            Rules.Add(() => 1 <= PsuChannel && PsuChannel <= 4, "Psu channel range: [1, 4]", nameof(PsuChannel));
            Rules.Add(() => DacAddressList.Count == DacNameList.Count, $"{nameof(DacAddressList)}/{nameof(DacNameList)} count must be equal", $"{nameof(DacAddressList)}/{nameof(DacNameList)}");
            Rules.Add(() => DacAddressList.Count == IsUseFixDacValue.Count && IsUseFixDacValue.Count == FixDacValue.Count, $"{nameof(DacAddressList)}/{nameof(IsUseFixDacValue)}/{nameof(FixDacValue)} count must be equal", $"{nameof(DacAddressList)}/{nameof(IsUseFixDacValue)}/{nameof(FixDacValue)}");

            Rules.Add(() => DacAddressList.Count == CurrentLowLimitList.Count && CurrentLowLimitList.Count == CurrentHighLimitList.Count, $"{nameof(DacAddressList)}/{nameof(CurrentLowLimitList)}/{nameof(CurrentHighLimitList)} count must be equal", $"{nameof(DacAddressList)}/{nameof(CurrentLowLimitList)}/{nameof(CurrentHighLimitList)}");
            Rules.Add(() => DacAddressList.Count == DacLowLimitList.Count && DacLowLimitList.Count == DacHighLimitList.Count, $"{nameof(DacAddressList)}/{nameof(DacLowLimitList)}/{nameof(DacHighLimitList)} count must be equal", $"{nameof(DacAddressList)}/{nameof(DacLowLimitList)}/{nameof(DacHighLimitList)}");

        }

        public override void TestStepRun()
        {
            IResults iResults = new Results(Results);

            var tuningResults = new List<AmcInfo>();
            try
            {

                LutData = TempTableUtility.LoadPaCalibrationDataFromFile<PaBiasTemperatureCorrectionData>(LutTable);


                var tuningResult = BiasTuningAmc7904(Dut, PsuChannel);
                tuningResults.AddRange(tuningResult);

                Dut.WriteLutTable(AmcAddress, LutData, ETransistor.AKQJ_PaComp, PaAmplifierStage);
                Dut.WriteBaseLineAndSaveEeprom(AmcAddress, tuningResult, Polarity);

                amcInfo = null;

            }
            catch (Exception ex)
            {
                if (ex is AggregateException ae)
                {
                    foreach (var ie in ae.InnerExceptions)
                    {
                        Log.Error(ie);
                    }
                }
                else
                {
                    Log.Error(ex);
                }

                UpgradeVerdict(iResults.Publish(Name + "_Error_Check", false, true, true, "bool"));
            }
            finally
            {
                #region Print Results

                foreach (var tuningResult in tuningResults)
                {
                    var transisterName = GetTransisterName(tuningResult);
                    UpgradeVerdict(iResults.Publish(Name + $"_{transisterName}_current", tuningResult.ResultCurrent, tuningResult.CurrentLowLimit, tuningResult.CurrentHighLimit, "mA"));
                    UpgradeVerdict(iResults.Publish(Name + $"_{transisterName}_resultDac", tuningResult.ResultDac, tuningResult.DacLowLimit, tuningResult.DacHighLimit, "DAC"));
                    UpgradeVerdict(iResults.Publish(Name + $"_{transisterName}_paTemperature", tuningResult.PaTemperature, tuningResult.TemperatureLowLimit, tuningResult.TemperatureHighLimit, "°C"));
                    UpgradeVerdict(iResults.Publish(Name + $"_{transisterName}_baseLineDac", tuningResult.DacBaseLine, tuningResult.DacBaseLine, tuningResult.DacBaseLine, "DAC"));
                }
                #endregion
            }
            if (OnFaliAbort && (Verdict != Verdict.Pass) && GetParent<ResetDutOnFailParent>() == null)
            {
                // set psu output off according to the sequence of different Voltages
                foreach (var step in EnabledChildSteps)
                {
                    try
                    {
                        RunChildStep(step);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
                throw new OnFailAbortException(Name + " On Fail Abort");
            }
        }

        private string GetTransisterName(AmcInfo amcInfo)
        {
            var dacIndex = DacAddressList.FindIndex((item) => item == amcInfo.DacAddress);

            var name = ItemNameList[dacIndex];
            return name;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="dacIndex">取值0,1,2,3，对应于DAC0, DAC1, DAC2, DAC3</param>
        /// <returns></returns>
        private AmcInfo GetAmcInfo(int dacIndex)
        {
            var info = new AmcInfo();
            var amcAddr = AmcAddress;
            info.AmcAddress = amcAddr;

            info.DacAddress = DacAddressList[dacIndex];
            info.DacName = DacNameList[dacIndex];
            (var paStage, var paLevel) = DutAmc7904Control.GetPaStageByName((int)amcAddr, (int)info.DacAddress);
            info.PaLevel = paLevel;
            info.PaStage = paStage;

            info.InitialDac = InitialDac[dacIndex];
            info.DacStep = DacStep[dacIndex];
            info.MinDac = MinDac[dacIndex];
            info.MaxDac = MaxDac[dacIndex];

            info.TargetCurrent = TargetCurrent[dacIndex];
            info.TargetCurrentTorlerence = TargetCurrentTolerance[dacIndex];
            info.BiasProtectCurrent = BiasProtectCurrent[dacIndex];

            info.MaxLoopCount = MaxLoopCount[dacIndex];
            info.IsUseFixDacValue = IsUseFixDacValue[dacIndex];
            info.FixDacValue = FixDacValue[dacIndex];

            info.CurrentLowLimit = CurrentLowLimitList[dacIndex];
            info.CurrentHighLimit = CurrentHighLimitList[dacIndex];

            info.DacLowLimit = DacLowLimitList[dacIndex];
            info.DacHighLimit = DacHighLimitList[dacIndex];

            info.TemperatureLowLimit = TemperatureLowLimitList[dacIndex];
            info.TemperatureHighLimit = TemperatureHighLimitList[dacIndex];

            return info;
        }

        public override void PostStepRun()
        {

        }
        private List<AmcInfo> BiasTuningAmc7904(IDutControl dut, int psuChannel)
        {
            var tuningResults = new List<AmcInfo>();

            var amcAddress = AmcAddress;
            Log.Info($"Setup Amc7904 chip: 0x{amcAddress:X2} before tuning");
            dut.SetupAmc7904ChipForBiasTuning(amcAddress);

            bool isErrorHappened = false;
            try
            {
                var currentTestObject = string.Empty;
                for (int dacIndex = 0; dacIndex < DacAddressList.Count; dacIndex++)
                {
                    amcInfo = GetAmcInfo(dacIndex);
                    currentTestObject = $"{PaAmplifierStage}_{amcInfo.PaStage}(0x{amcAddress:X2}, {amcInfo.DacName})";
                    Log.Info($"Bias Tuning Start for {currentTestObject}");

                    var paBiasTuningCommandParam = new PaBiasTuningCommandParamChip(amcInfo.AmcAddress, amcInfo.DacAddress, psuChannel);

                    isInitialCurrent = true;
                    var initialCurrent = MeasureStableCurrent(MeasureDevice, paBiasTuningCommandParam);
                    isInitialCurrent = false;
                    amcInfo.InitalCurrent = initialCurrent;
                    Log.Info($"{currentTestObject} initial current is {amcInfo.InitalCurrent}mA.");

                    var paramObj = new BiasTuningParam(amcInfo.MinDac, amcInfo.MaxDac, amcInfo.InitialDac, amcInfo.DacStep, amcInfo.TargetCurrent, amcInfo.TargetCurrentTorlerence, MeasureDevice, DacDevice, paBiasTuningCommandParam, amcInfo.InitalCurrent, amcInfo.MaxLoopCount);
                    paramObj.TransistorProtectBiasCurrent = amcInfo.BiasProtectCurrent;
                    paramObj.TransistorOnBiasCurrent = 4;
                    paramObj.CurveInitialPointSearch = true;

                    var tuning = new BiasTuning();

                    var measuredCurrent = -999d;
                    var resultDac = -99;
                    if (amcInfo.IsUseFixDacValue)
                    {
                        resultDac = amcInfo.FixDacValue;
                        measuredCurrent = amcInfo.InitalCurrent;
                        Log.Info($"{currentTestObject} use fix dac value {amcInfo.FixDacValue} for {PaAmplifierStage} {amcInfo.PaStage}(0x{amcInfo.AmcAddress:X2} {amcInfo.DacName})");
                    }
                    else
                    {
                        if (amcInfo.PaStage == EPaStage.Driver || amcInfo.PaStage == EPaStage.Main /*|| true*/)
                        {
                            Log.Info($"Tuning using \"RegressionAnalysis\" method.");
                            measuredCurrent = tuning.RegressionAnalysis(paramObj, SetDacToAmc7904, MeasureStableCurrent, out resultDac);
                        }
                        else
                        {
                            Log.Info($"Tuning using \"OptimizedBinarySearch\" method.");
                            measuredCurrent = tuning.OptimizedBinarySearch(paramObj, SetDacToAmc7904, MeasureStableCurrent, out resultDac);
                        }
                    }

                    var resultCurrent = measuredCurrent - amcInfo.InitalCurrent;
                    amcInfo.ResultDac = resultDac;
                    amcInfo.ResultCurrent = Math.Round(resultCurrent, 3);
                    Log.Info($"{currentTestObject}, ResultDac: {amcInfo.ResultDac}, ResultCurrent: {amcInfo.ResultCurrent}mA = MesuredCurrent: {measuredCurrent} - InitialCurrent: {amcInfo.InitalCurrent}");

                    var paTemperature = dut.GetPaTemperature(amcAddress);
                    amcInfo.PaTemperature = Math.Round(paTemperature, 3);
                    Log.Info($"{currentTestObject}, Temperature: {amcInfo.PaTemperature}°C");

                    // Calculate base line if bias tuning PASSED, which will need LUT table feature
                    var dacBaseLine = 0;
                    if (amcInfo.IsUseFixDacValue)
                    {
                        dacBaseLine = amcInfo.ResultDac;
                        Log.Info($"{currentTestObject} use fix dac value, so BaseLine = FixValue = {amcInfo.ResultDac}");
                    }
                    else
                    {
                        if (Math.Abs(resultCurrent - amcInfo.TargetCurrent) <= amcInfo.TargetCurrentTorlerence)
                        {
                            Log.Info($"{currentTestObject} bias passed, calculate BiasOffset and BaseLine");

                            var biasOffset = Convert.ToInt32(Math.Round(TempTableUtility.GetAccumulatedOffsetValue(ETransistor.AKQJ_PaComp, PaAmplifierStage, amcInfo.DacName, LutData, paTemperature, _baseLineTemperature, _lutTemperatureStep)));
                            Log.Info($"{currentTestObject} calculated BiasOffset is {biasOffset}");

                            if (Polarity == EOffsetDirection.MonotonicallyIncreasing)
                            {
                                Log.Info($"{currentTestObject} MonotonicallyIncreasing, Polarity = {(int)Polarity}");
                                if (amcInfo.PaTemperature >= _baseLineTemperature)
                                {
                                    dacBaseLine = amcInfo.ResultDac - biasOffset;
                                    Log.Info($"PaTemperature({amcInfo.PaTemperature}°C) is higher than BaseLineTemperature({_baseLineTemperature}°C), so dacBaseLine = ResultDac - BiasOffset = {amcInfo.ResultDac} - {biasOffset} = {dacBaseLine}");
                                }
                                else
                                {
                                    dacBaseLine = amcInfo.ResultDac + biasOffset;
                                    Log.Info($"PaTemperature({amcInfo.PaTemperature}°C) is lower than BaseLineTemperature({_baseLineTemperature}°C), so dacBaseLine = ResultDac + BiasOffset = {amcInfo.ResultDac} + {biasOffset} = {dacBaseLine}");
                                }
                            }
                            else if (Polarity == EOffsetDirection.MonotonicallyDecreasing)
                            {
                                Log.Info($"{currentTestObject} MonotonicallyDecreasing, Polarity = {(int)Polarity}");
                                if (amcInfo.PaTemperature >= _baseLineTemperature)
                                {
                                    dacBaseLine = amcInfo.ResultDac + biasOffset;
                                    Log.Info($"PaTemperature({amcInfo.PaTemperature}°C) is higher than BaseLineTemperature({_baseLineTemperature}°C), so dacBaseLine = ResultDac + BiasOffset = {amcInfo.ResultDac} + {biasOffset} = {dacBaseLine}");
                                }
                                else
                                {
                                    dacBaseLine = amcInfo.ResultDac - biasOffset;
                                    Log.Info($"PaTemperature({amcInfo.PaTemperature}°C) is lower than BaseLineTemperature({_baseLineTemperature}°C), so dacBaseLine = ResultDac - BiasOffset = {amcInfo.ResultDac} - {biasOffset} = {dacBaseLine}");
                                }
                            }
                            else
                                throw new ApplicationException("Unknown Polarity status.");

                            if (amcInfo.PaStage == EPaStage.Peak)
                            {
                                var initValue = dacBaseLine;
                                dacBaseLine -= PeakStoreBackward;
                                Log.Info($"{currentTestObject}, re-calculate dacBaseLine using PeakStoreBackward: dacBaseLine = dacBaseLine - PeakStoreBackward = {initValue} - {PeakStoreBackward} = {dacBaseLine}");
                            }
                        }
                        else
                        {
                            Log.Info($"{currentTestObject} bias failed, set BaseLine to {dacBaseLine}");
                        }
                    }


                    amcInfo.DacBaseLine = dacBaseLine;
                    var paBiasTuningData = new PaBiasTuningResultChip(amcAddress, (EDacName)dacIndex, resultCurrent, resultDac)
                    {
                        DacBaseLine = dacBaseLine,
                        PaTemperature = paTemperature
                    };
                    tuningResults.Add(amcInfo);

                    Log.Info($"{currentTestObject} set Dac value to 0");
                    dut.SetPaBiasAmc7904(amcAddress, amcInfo.DacAddress, 0);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                isErrorHappened = true;
            }
            finally
            {
                if (isErrorHappened)
                {
                    Log.Info($"Set 0x{AmcAddress:X2} each DACs value to 0");
                    dut.SetPaBiasAmc7904(amcAddress, (uint)EDacName.DAC0, 0);
                    dut.SetPaBiasAmc7904(amcAddress, (uint)EDacName.DAC1, 0);
                    dut.SetPaBiasAmc7904(amcAddress, (uint)EDacName.DAC2, 0);
                    dut.SetPaBiasAmc7904(amcAddress, (uint)EDacName.DAC3, 0);
                }
            }

            return tuningResults;
        }


        private void SetDacToAmc7904(EBiasDacDevice device, int dacVal, object objParam)
        {
            if (!(objParam is PaBiasTuningCommandParamChip))
                throw new ArgumentException();

            var tuningInfo = (PaBiasTuningCommandParamChip)objParam;

            var pipeId = tuningInfo.DacAddress;
            var amcAddress = tuningInfo.AmcAddress;
            var dacAddress = tuningInfo.DacAddress;
            Dut.SetPaBiasAmc7904(amcAddress, dacAddress, dacVal);
            Log.Info($"Set Amc: 0x{amcAddress:X2}, Dac: {dacAddress}, Value: {dacVal}");
            Thread.Sleep(50);
        }
        private double MeasureStableCurrent(ECurrentMeasureDevice device, object objParam)
        {
            var tolerance = StableCurrentTolerancemA;
            var timeoutMs = StableCurrentTimeoutMs;
            var intervalMs = 1000;
            var ocpCurrent = 0.1; //mA  0.2

            if (!(objParam is PaBiasTuningCommandParamChip)) throw new ArgumentException();

            var currentTuningInfo = (PaBiasTuningCommandParamChip)objParam;

            EModule module;
            switch (currentTuningInfo.Channel)
            {
                case 1:
                    module = EModule.Module1;
                    break;
                case 2:
                    module = EModule.Module2;
                    break;
                case 3:
                    module = EModule.Module3;
                    break;
                case 4:
                    module = EModule.Module4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"No such channel");
            }
            var timer = new Stopwatch();
            var currentArray = new double[3] { 0, 0, 0 };

            timer.Start();
            while (true)
            {
                var current = 0.0;
                lock (InstrumentLock)
                {
                    current = Psu.MeasureCurrent(module) * 1000;
                    if (current < ocpCurrent && !isInitialCurrent)
                    {
                        Psu.ClearOutputProtection(module);
                        Thread.Sleep(1000); //5000
                        Psu.SetOutputState(EState.On, module);
                        Thread.Sleep(intervalMs);
                        current = Psu.MeasureCurrent(module) * 1000;

                        if (current < ocpCurrent)
                        {
                            Log.Info($"Measured current({current}mA) still low than ocpCurrent({ocpCurrent}mA), return BiasProtectCurrent: {amcInfo.BiasProtectCurrent}mA");
                            return amcInfo.BiasProtectCurrent;  //serial tuning, only one amcInfo is available
                        }
                    }
                }
                // for those transistor not opened, return directly, may need add parameter
                Log.Info($" {module} measured current: {current} mA.");

                currentArray[0] = currentArray[1];
                currentArray[1] = currentArray[2];
                currentArray[2] = current;
                if ((currentArray.Max() - currentArray.Min()) < tolerance)
                {
                    Log.Info($"Psu {module} measured stable current: {current} mA.");
                    return current;
                }

                if (timer.ElapsedMilliseconds > timeoutMs)
                {
                    Log.Warning($"Timeout {timeoutMs} ms trigger, return last current: {current} mA.");
                    return current;
                }
                Thread.Sleep(intervalMs);
            }
        }
    }
    public class PaBiasTuningCommandParamChip
    {
        public PaBiasTuningCommandParamChip(uint amcAddress, uint dacAddress, int channel)
        {
            AmcAddress = amcAddress;
            DacAddress = dacAddress;
            Channel = channel;
        }

        public uint AmcAddress { get; set; }
        public uint DacAddress { get; set; }
        public int Channel { get; set; }
    }
    public class PaBiasTuningResultChip
    {
        public PaBiasTuningResultChip(uint amcAddress, EDacName dacAddress, double resultCurrent, int resultDac)
        {
            this.AmcAddress = amcAddress;
            this.DacAddress = dacAddress;
            this.ResultCurrent = resultCurrent;
            this.ResultDac = resultDac;
        }

        public uint AmcAddress { get; set; }
        public EDacName DacAddress { get; private set; }
        public double ResultCurrent { get; private set; }
        public int ResultDac { get; private set; }
        public int OffsetDac { get; set; }          // Only for Amc7812 tuning
        public int DacBaseLine { get; set; }        // Only for Amc7904 tuning
        public double PaTemperature { get; set; }   // OptionalResult
    }
    public class AmcInfo
    {
        public uint AmcAddress { get; set; }
        public uint DacAddress { get; set; }
        public EDacName DacName { get; set; }
        public EPaLevel PaLevel { get; set; }
        public EPaStage PaStage { get; set; }
        public int InitialDac { get; set; }
        public int DacStep { get; set; }
        public int MinDac { get; set; }
        public int MaxDac { get; set; }
        public double InitalCurrent { get; set; }
        public double TargetCurrent { get; set; }
        public double TargetCurrentTorlerence { get; set; }
        public double BiasProtectCurrent { get; set; }
        public int MaxLoopCount { get; set; }

        public bool IsUseFixDacValue { get; set; }
        public int FixDacValue { get; set; }

        public int ResultDac { get; set; }
        public double ResultCurrent { get; set; }
        public int DacBaseLine { get; set; }
        public double PaTemperature { get; set; }

        public int DacLowLimit { get; set; }
        public int DacHighLimit { get; set; }
        public double CurrentLowLimit { get; set; }
        public double CurrentHighLimit { get; set; }
        public double TemperatureLowLimit { get; set; }
        public double TemperatureHighLimit { get; set; }
    }
}

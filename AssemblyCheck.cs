using Newtonsoft.Json;
using Nokia.Tap.Driver.Interfaces.DUTControl;
using Nokia.Tap.StdLib.Results;
using Nokia.Tap.StdLib.Xmc;
using Nokia.Tap.StdLib;
using Nokia.Tap.TestSteps.Projects.AHPF.Utilities;
using OpenTap;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

namespace Nokia.Tap.TestSteps.Projects.AHPF.UnitTest
{
    [Display("AssemblyCheck", Groups: new[] { "Nokia", "TestSteps", "Projects", "AHPF", "UnitTest" }, Description: "AssemblyCheck")]
    public class AssemblyCheck : TestStep
    {
        #region Interfaces

        [Display("Dut Type", Group: TestParameterConstants.NokiaTestParameterGroups.Interfaces, Order: 101, Description: "Define Dut Interface here")]
        public IDutControl Dut { get; set; }

        [Display("DutInformation Type", Group: TestParameterConstants.NokiaTestParameterGroups.Interfaces, Order: 102, Description: "Dut Information")]
        public DutInformation DutInformation { get; set; }

        [Display("Mac Address", Group: TestParameterConstants.NokiaTestParameterGroups.Interfaces, Order: 103,
            Description: "Define Mac Address Interface here")]
        public IMacAddress MacAddress { get; set; }

        #endregion

        #region Parameters

        [Display("Requset Web Site", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 201, Description: "")]
        public string RequestWebSite { get; set; }

        [Display("Ref Designator", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 202, Description: "")]
        public string RefDesignator { get; set; }

        [Display("Which Data Section", Group: TestParameterConstants.NokiaTestParameterGroups.TestParameters, Order: 203, Description: "component_code, component_id or ref_designator")]
        public string DataSection { get; set; }

        #endregion

        #region Limits

        #endregion

        private XmcSettings _xmcSettings;
        public AssemblyCheck()
        {
            RequestWebSite = @"http://10.126.4.22:8888/asik/{0}";
            RefDesignator = "TRX";
            DataSection = "ComponentId";

            _xmcSettings = ComponentSettings<XmcSettings>.Current;
        }

        public override void Run()
        {
            Log.Info("############################## Test Case: " + Name + " Start. ##############################");
            IResults iResults = new Results(Results);

            try
            {
                Log.Info("Used operating mode is: " + _xmcSettings.OperatingMode);

                if (!Dut.IsConnected)
                {
                    Dut.ConnectDut(10 * 1000, 2);
                }

                var macAddressFromDut = Dut.GetUnitInfo(EUnitInfoItem.MacAddress).Trim().ToUpper();
                Log.Info($"mac address from dut: {macAddressFromDut}");

                var macAddressFromServer = GetMacFromServer();
                Log.Info($"mac address from server: {macAddressFromServer}");

                if (_xmcSettings.OperatingMode == XmcSettings.EOperatingMode.Engineering)
                {
                    Log.Info($"Not to check mac address when using {_xmcSettings.OperatingMode} operation mode");
                    UpgradeVerdict(iResults.Publish(Name, true, true, true, "bool"));
                    //Log.Info("############################## Test Case: " + Name + " End. ##############################");
                    //return;
                }
                else
                {
                    UpgradeVerdict(iResults.Publish(Name, macAddressFromDut, macAddressFromServer, macAddressFromServer, "Mac"));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(iResults.Publish(Name, false, true, true, "bool"));
            }

            Log.Info("############################## Test Case: " + Name + " End. ##############################");
        }

        private string GetMacFromServer()
        {
            var scannerSN = DutInformation.SerialNumber;

            var requestUrl = string.Format(RequestWebSite, scannerSN);
            var response = WebRequest.Get(requestUrl, EContentType.Json);
            if (response == null)
            {
                throw new ApplicationException($"url have no response. url: {requestUrl}");
            }
            Log.Info($"web response: {response}");

            var data = JsonConvert.DeserializeObject<WebRequestDataModel>(response);
            if (data.Success)
            {
                var componentInfos = data.Data;
                var componentInfo = componentInfos.Find(item => item.RefDesignator.Equals(RefDesignator));
                if (componentInfo == null)
                {
                    throw new ApplicationException($"web response not contains \"{RefDesignator}\" section.");
                }

                var type = componentInfo.GetType();
                var properties = type.GetRuntimeProperties();
                var dic = new Dictionary<string, string>();
                var info = string.Empty;
                foreach (var item in properties)
                {
                    var key = item.Name;
                    var value = (string)item.GetValue(componentInfo);
                    dic.Add(key, value);
                    info += key + ": " + value + ";";
                }
                Log.Info($"{RefDesignator} info:{info}");

                var trxSN = dic[DataSection];
                Log.Info($"trx sn: {trxSN}");

                if (MacAddress.IsConnected) MacAddress.Open();
                MacAddress.RefreshSettings();
                var macAddressList = MacAddress.RequestMacAddress(trxSN, 1);
                var macAddressFromServer = AddSeparatorsToMacAddress(macAddressList.First().Trim(), ':').ToUpper();
                return macAddressFromServer;
            }
            else
            {
                Log.Error($"web response: {response}");
                throw new ApplicationException($"web response fail: {response}");
            }
        }

        private string AddSeparatorsToMacAddress(string macAddress, char separator)
        {
            const int macAddressLenghtNoColons = 12;
            var formatted = "";

            if (macAddress.Length != macAddressLenghtNoColons)
                throw new ArgumentException("Invalid argument length!", nameof(macAddress));

            for (var index = 0; index < macAddressLenghtNoColons; index++)
            {
                formatted += macAddress[index];
                if (index < macAddressLenghtNoColons - 1 && index % 2 != 0) formatted += separator;
            }

            return formatted;
        }
    }
}

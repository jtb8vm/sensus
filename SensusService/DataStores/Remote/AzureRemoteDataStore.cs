﻿using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json;
using SensusService.Exceptions;
using SensusService.Probes.Apps;
using SensusService.Probes.Communication;
using SensusService.Probes.Context;
using SensusService.Probes.Location;
using SensusService.Probes.Network;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SensusService.DataStores.Remote
{
    public class AzureRemoteDataStore : RemoteDataStore
    {
        private MobileServiceClient _client;
        private string _url;
        private string _key;

        private IMobileServiceTable<RunningAppsDatum> _runningAppsTable;
        private IMobileServiceTable<TelephonyDatum> _telephonyTable;
        private IMobileServiceTable<AltitudeDatum> _altitudeTable;
        private IMobileServiceTable<CompassDatum> _compassTable;
        private IMobileServiceTable<LocationDatum> _locationTable;
        private IMobileServiceTable<SmsDatum> _smsTable;
        private IMobileServiceTable<CellTowerDatum> _cellTowerTable;
        private IMobileServiceTable<BluetoothDeviceProximityDatum> _bluetoothTable;
        private IMobileServiceTable<ProtocolReport> _protocolReportTable;

        [EntryStringUiProperty("URL:", true, 2)]
        public string URL
        {
            get { return _url; }
            set
            {
                if (!value.Equals(_url, StringComparison.Ordinal))
                {
                    _url = value;
                    OnPropertyChanged();
                }
            }
        }

        [EntryStringUiProperty("Key:", true, 2)]
        public string Key
        {
            get { return _key; }
            set
            {
                if (!value.Equals(_key, StringComparison.Ordinal))
                {
                    _key = value;
                    OnPropertyChanged();
                }
            }
        }

        protected override string DisplayName
        {
            get { return "Azure"; }
        }

        [JsonIgnore]
        public override bool Clearable
        {
            get { return false; }
        }

        public override void Start()
        {
            _client = new MobileServiceClient(_url, _key);

            _runningAppsTable = _client.GetTable<RunningAppsDatum>();
            _smsTable = _client.GetTable<SmsDatum>();
            _telephonyTable = _client.GetTable<TelephonyDatum>();
            _bluetoothTable = _client.GetTable<BluetoothDeviceProximityDatum>();
            _altitudeTable = _client.GetTable<AltitudeDatum>();
            _compassTable = _client.GetTable<CompassDatum>();
            _locationTable = _client.GetTable<LocationDatum>();
            _cellTowerTable = _client.GetTable<CellTowerDatum>();

            _protocolReportTable = _client.GetTable<ProtocolReport>();

            base.Start();
        }

        protected override ICollection<Datum> CommitData(ICollection<Datum> data)
        {
            List<Datum> committedData = new List<Datum>();

            DateTime start = DateTime.Now;

            foreach (Datum datum in data)
            {
                try
                {
                    if (datum is RunningAppsDatum)
                        _runningAppsTable.InsertAsync(datum as RunningAppsDatum).Wait();
                    else if (datum is SmsDatum)
                        _smsTable.InsertAsync(datum as SmsDatum).Wait();
                    else if (datum is TelephonyDatum)
                        _telephonyTable.InsertAsync(datum as TelephonyDatum).Wait();
                    else if (datum is BluetoothDeviceProximityDatum)
                        _bluetoothTable.InsertAsync(datum as BluetoothDeviceProximityDatum).Wait();
                    else if (datum is AltitudeDatum)
                        _altitudeTable.InsertAsync(datum as AltitudeDatum).Wait();
                    else if (datum is CompassDatum)
                        _compassTable.InsertAsync(datum as CompassDatum).Wait();
                    else if (datum is LocationDatum)
                        _locationTable.InsertAsync(datum as LocationDatum).Wait();
                    else if (datum is CellTowerDatum)
                        _cellTowerTable.InsertAsync(datum as CellTowerDatum).Wait();
                    else if (datum is ProtocolReport)
                        _protocolReportTable.InsertAsync(datum as ProtocolReport).Wait();
                    else
                        throw new DataStoreException("Unrecognized Azure table:  " + datum.GetType().FullName);

                    committedData.Add(datum);
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Error: Could not insert the item because an item with that id already exists.")
                        committedData.Add(datum);
                    else
                        SensusServiceHelper.Get().Logger.Log("Failed to insert datum into Azure table:  " + ex.Message, LoggingLevel.Normal);
                }
            }

            SensusServiceHelper.Get().Logger.Log("Committed " + committedData.Count + " data items to Azure tables in " + (DateTime.Now - start).TotalSeconds + " seconds.", LoggingLevel.Verbose);

            return committedData;
        }

        public override void Stop()
        {
            base.Stop();

            _client.Dispose();
        }
    }
}

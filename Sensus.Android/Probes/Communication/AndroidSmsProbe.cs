using Android.App;
using Android.Content;
using Android.Telephony;
using SensusService;
using SensusService.Probes.Communication;
using System;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidSmsProbe : SmsProbe
    {
        private TelephonyManager _telephonyManager;
        private AndroidSmsOutgoingObserver _smsOutgoingObserver;
        private EventHandler<SmsDatum> _incomingSmsCallback;

        protected override bool Initialize()
        {
            try
            {
                _telephonyManager = Application.Context.GetSystemService(global::Android.Content.Context.TelephonyService) as TelephonyManager;
                if (_telephonyManager == null)
                    throw new Exception("No telephony present.");

                _smsOutgoingObserver = new AndroidSmsOutgoingObserver(this, Application.Context, outgoingSmsDatum => StoreDatum(outgoingSmsDatum));

                _incomingSmsCallback = (sender, incomingSmsDatum) =>
                    {
                        // the observer doesn't set the probe type or destination number (simply the device's primary number)
                        incomingSmsDatum.ProbeType = GetType().FullName;
                        incomingSmsDatum.ToNumber = _telephonyManager.Line1Number;

                        StoreDatum(incomingSmsDatum);
                    };

                return base.Initialize();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to initialize " + GetType().FullName + ":  " + ex.Message, LoggingLevel.Normal);
                return false;
            }
        }

        public override void StartListening()
        {
            Application.Context.ContentResolver.RegisterContentObserver(global::Android.Net.Uri.Parse("content://sms"), true, _smsOutgoingObserver);
            AndroidSmsIncomingBroadcastReceiver.IncomingSMS += _incomingSmsCallback;
        }

        public override void StopListening()
        {
            Application.Context.ContentResolver.UnregisterContentObserver(_smsOutgoingObserver);
            AndroidSmsIncomingBroadcastReceiver.IncomingSMS -= _incomingSmsCallback;
        }
    }
}
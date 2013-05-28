﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace StreetFoo.Client
{
    public class BackgroundSyncTask : TaskBase
    {
        private const string SyncExpirationKey = "SyncExpiration";
        internal const string SpoolFilename = "SpooledReports.json";

        protected override async Task DoRunAsync(IBackgroundTaskInstance instance)
        {
            // try and lock...
            if (!(await CreateLockFileAsync()))
            {
                this.Logger.Info("Locked - skipping...");
                return;
            }

            try
            {
                // should we run?
                if (!(await CanRunAsync()))
                    return;

                // send up changes...
                await ReportItem.PushServerUpdatesAsync();

                // still have connectivity?
                if (StreetFooRuntime.HasConnectivity)
                {
                    this.Logger.Info("Getting reports from server...");

                    // get...
                    var proxy = ServiceProxyFactory.Current.GetHandler<IGetReportsByUserServiceProxy>();
                    var reports = await proxy.GetReportsByUserAsync();

                    // errors?
                    if (!(reports.HasErrors))
                    {
                        this.Logger.Info("Stashing reports on disk...");

                        // save...
                        var json = JsonConvert.SerializeObject(reports.Reports);
                        var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(SpoolFilename, CreationCollisionOption.ReplaceExisting);
                        await FileIO.WriteTextAsync(file, json);
                    }
                }
            }
            finally
            {
                // reset the lock file...
                ResetLockFileAsync();
            }
        }

        private async Task<bool> CanRunAsync()
        {
            // do we have connectivity?
            if (!(StreetFooRuntime.HasConnectivity))
            {
                this.Logger.Info("No connectivity - skipping...");

                // clear the expiration period...
                await SettingItem.SetValueAsync(SyncExpirationKey, string.Empty);

                // return...
                return false;
            }

            // skip the check if we're debugging... (otherwise it's hard to see what's
            // going on...)
            if (!(Debugger.IsAttached))
            {
                // check the expiration...
                var asString = await SettingItem.GetValueAsync(SyncExpirationKey);
                if (!(string.IsNullOrEmpty(asString)))
                {
                    this.Logger.Info("Expiration time: {0}", asString);

                    // parse...
                    var expiration = DateTime.ParseExact(asString, "o", CultureInfo.InvariantCulture).ToUniversalTime();

                    // if the expiration time is in the future - do nothing...
                    if (expiration > DateTime.UtcNow)
                    {
                        this.Logger.Info("Not expired (expiration is '{0}') - skipping...", expiration);
                        return false;
                    }
                }
                else
                    this.Logger.Info("No expiration time available.");
            }

            // we're ok - set the new expiration period...
            var newExpiration = DateTime.UtcNow.AddMinutes(5);
            await SettingItem.SetValueAsync(SyncExpirationKey, newExpiration.ToString("o"));

            // try and log the user in...
            var model = new LogonPageViewModel(new NullViewModelHost());
            return await model.RestorePersistentLogonAsync();
        }

        public static async Task ConfigureAsync()
        {
            // setup the maintenance task...
            await TaskHelper.RegisterTaskAsync<BackgroundSyncTask>("BackgroundSyncMaintenance", (builder) =>
            {
                // every 15 minutes, continuous...
                builder.SetTrigger(new MaintenanceTrigger(15, false));
            });

            // setup the time task...
            await TaskHelper.RegisterTaskAsync<BackgroundSyncTask>("BackgroundSyncTime", (builder) =>
            {
                // every 15 minutes, continuous...
                builder.SetTrigger(new TimeTrigger(15, false));
            });

            // setup the connectivity task...
            await TaskHelper.RegisterTaskAsync<BackgroundSyncTask>("BackgroundSyncConnectivity", (builder) =>
            {
                // whenever we get connectivity...
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.InternetAvailable, false));
            });
        }
    }
}

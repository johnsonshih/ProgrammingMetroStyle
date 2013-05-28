﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using TinyIoC;
using Windows.Networking.Connectivity;

namespace StreetFoo.Client
{
    public static class StreetFooRuntime
    {
        // holds a reference to how we started...
        public static string Module { get; private set; }

        // holds a reference to the logon token...
        internal static string LogonToken { get; private set; }

        // holds a refrence to the database connections...
        internal const string SystemDatabaseConnectionString = "StreetFoo-system.db";
        internal static string UserDatabaseConnectionString = null;

        // defines the base URL of our services...
        internal const string ServiceUrlBase = "https://streetfoo.apphb.com/handlers/";

        // starts the application/sets up state...
        public static async Task Start(string module)
        {
            Module = module;

            // setup TinyIoC...
            TinyIoCContainer.Current.AutoRegister();

            // setup the default IoC handlers for the view models...
            ViewModelFactory.Current.SetHandler(typeof(IRegisterPageViewModel), typeof(RegisterPageViewModel));
            ViewModelFactory.Current.SetHandler(typeof(ILogonPageViewModel), typeof(LogonPageViewModel));
            ViewModelFactory.Current.SetHandler(typeof(IReportsPageViewModel), typeof(ReportsPageViewModel));
            ViewModelFactory.Current.SetHandler(typeof(IShareTargetPageViewModel), typeof(ShareTargetPageViewModel));
            ViewModelFactory.Current.SetHandler(typeof(ISearchResultsPageViewModel), typeof(SearchResultsPageViewModel));
            ViewModelFactory.Current.SetHandler(typeof(IMySettingsPaneViewModel), typeof(MySettingsPaneViewModel));
            ViewModelFactory.Current.SetHandler(typeof(IHelpPaneViewModel), typeof(HelpPaneViewModel));
            ViewModelFactory.Current.SetHandler(typeof(IReportPageViewModel), typeof(ReportPageViewModel));
            ViewModelFactory.Current.SetHandler(typeof(IEditReportPageViewModel), typeof(EditReportPageViewModel));

            // ...and then for the service proxies...
            ServiceProxyFactory.Current.SetHandler(typeof(IRegisterServiceProxy), typeof(RegisterServiceProxy));
            ServiceProxyFactory.Current.SetHandler(typeof(ILogonServiceProxy), typeof(LogonServiceProxy));
            ServiceProxyFactory.Current.SetHandler(typeof(IEnsureTestReportsServiceProxy), typeof(EnsureTestReportsServiceProxy));
            ServiceProxyFactory.Current.SetHandler(typeof(IGetReportsByUserServiceProxy), typeof(GetReportsByUserServiceProxy));
            ServiceProxyFactory.Current.SetHandler(typeof(IGetReportImageServiceProxy), typeof(GetReportImageServiceProxy));

            // initialize the system database... 
            var conn = GetSystemDatabase();
            await conn.CreateTableAsync<SettingItem>();
		}

        internal static bool HasLogonToken
        {
            get
            {
                return !(string.IsNullOrEmpty(LogonToken));
            }
        }

        internal static async Task LogonAsync(string username, string token)
        {
            // set the database to be a user specific one... (assumes the username doesn't have evil chars in it
            // - for production you may prefer to use a hash)...
            UserDatabaseConnectionString = string.Format("StreetFoo-user-{0}.db", username);

            // store the logon token...
            LogonToken = token;

            // initialize the database - has to be done async...
            var conn = GetUserDatabase();
            await conn.CreateTableAsync<ReportItem>();
        }

        internal static SQLiteAsyncConnection GetSystemDatabase()
        {
            return new SQLiteAsyncConnection(SystemDatabaseConnectionString);
        }

        internal static SQLiteAsyncConnection GetUserDatabase()
        {
            return new SQLiteAsyncConnection(UserDatabaseConnectionString);
        }

        internal static bool HasConnectivity
        {
            get
            {
                var profile = NetworkInformation.GetInternetConnectionProfile();
                return profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
            }
        }
    }
}

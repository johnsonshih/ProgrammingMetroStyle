﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;

namespace StreetFoo.Client
{
    // provides a route back from a view-model to a view...
    public interface IViewModelHost
    {
        // show messages...
        Task ShowAlertAsync(ErrorBucket errors);
        Task ShowAlertAsync(string message);

        // shows a view from a given view-model...
        void ShowView(Type viewModelInterfaceType);
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StreetFoo.Client
{
    public interface IMySettingsPaneViewModel : IViewModel, IDismissCommandSource
    {
        string Foobar { get; set; }
    }
}

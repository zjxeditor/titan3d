﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace CodeDomNode.Animation
{
    public partial class LAClipNodeControl
    {
        public LAClipNodeControl()
        {
            InitializeComponent();
            mCtrlValueLinkHandle = ValueLinkHandle;
        }
        partial void InitConstruction()
        {
            InitializeComponent();
            mCtrlValueLinkHandle = ValueLinkHandle;

        }


    }
}

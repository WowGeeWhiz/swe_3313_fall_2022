﻿using CoffeePointOfSale.Configuration;
using CoffeePointOfSale.Services.FormFactory;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoffeePointOfSale.Forms
{
    public partial class FormReceipt : Base.FormNoCloseBase
    {
        private IAppSettings? _appSettings;
        public FormReceipt(IAppSettings appSettings)
        {
            InitializeComponent();
            _appSettings = appSettings;
        }

        private void FormReceipt_Load(object sender, EventArgs e)
        {

        }

        private void RecceiptMainMenubtn_Click(object sender, EventArgs e)
        {
            Close();
            FormFactory.Get<FormMain>().Show();
        }
    }
}

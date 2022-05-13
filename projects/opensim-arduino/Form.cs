/*
 *    (C) Copyright 2013 Olivier Battini (https://olivierbattini.fr)
 *
 *    ALL RIGHTS RESERVED
 *
 *    This file is only published for showcase purposes and use in source and
 *    binary forms, with or without modification, are not permitted in any way.
 */
 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using RestSharp;

namespace Arduino0
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            SetLedState(Url.Text, (int)LedNumber.Value, (int)LedState.Value);
            this.Cursor = Cursors.Default;
        }

        private void K2000ModeButton_Click(object sender, EventArgs e)
        {
            int led;
            for (led = 0; led < 12; led++)
            {
                // Eteindre LED précédente
                if (led > 0)
                    SetLedState(Url.Text, led - 1, 0);

                // Allumer LED suivante
                SetLedState(Url.Text, led, 1);

                Thread.Sleep(1000);
            }

            for (led = 11; led >= 0; led--)
            {
                // Eteindre LED précédente
                if (led < 11)
                    SetLedState(Url.Text, led + 1, 0);

                // Allumer LED suivante
                SetLedState(Url.Text, led, 0);

                Thread.Sleep(1000);
            }
        }

        private void SetLedState(string url, int ledNumber, int ledState)
        {
            RestClient client = new RestClient(url);

            RestRequest request = new RestRequest();
            request.Method = Method.POST;
            request.AddParameter("Data", $"{ledNumber},{ledState}");

            IRestResponse response = client.Execute(request);
            //return response.Content;
        }
    }
}

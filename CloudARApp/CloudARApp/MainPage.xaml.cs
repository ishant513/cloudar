using CloudARApp.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CloudARApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        public void OnStartButtonClicked(object sender, EventArgs e)
        {
            DependencyService.Get<IAppController>().Start();
        }
    }
}

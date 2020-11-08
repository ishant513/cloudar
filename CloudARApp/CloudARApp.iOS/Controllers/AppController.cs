using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Essentials;
using UIKit;

using CloudARApp.Interfaces;
using CloudARApp.iOS.ViewControllers;
using CoreFoundation;

[assembly: Xamarin.Forms.Dependency(typeof(CloudARApp.iOS.Controllers.AppController))]
namespace CloudARApp.iOS.Controllers
{
    public class AppController : IAppController
    {
        private WebRTCController webRTCController;
        public void Start()
        {
            Permissions.RequestAsync<Permissions.Camera>();

            webRTCController = new WebRTCController();
            webRTCController.OnPeerConnected += () => { DispatchQueue.MainQueue.DispatchAsync(ShowView); };            
        }

        public void ShowView()
        {
            AppViewController viewController = new AppViewController(webRTCController);
            viewController.ModalPresentationStyle = UIModalPresentationStyle.FullScreen;
            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(viewController, true, null);
        }
    }
}
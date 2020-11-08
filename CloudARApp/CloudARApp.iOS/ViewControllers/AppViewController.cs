using ARKit;
using Foundation;
using Xamarin.Forms.Platform.iOS;

using UIKit;
using GLKit;
using CoreGraphics;

using CloudARApp.iOS.ViewControllers;
using CloudARApp.iOS.Controllers;
using CloudARApp.iOS.Views;

using WebRTC.iOS.Bindings;

[assembly: Xamarin.Forms.Dependency(typeof(AppViewController))]
namespace CloudARApp.iOS.ViewControllers
{
    [Register("AppViewController")]
    public class AppViewController : UIViewController
    {
        private ARSCNView sceneView;
        private ARVideoView remoteVideoView;
        private WebRTCController webRTCController;

        private AppView appView;

        public AppViewController(WebRTCController controller)
        {
            remoteVideoView = new ARVideoView();
            sceneView = new ARSCNView
            {
                AutoenablesDefaultLighting = false,
            };
            webRTCController = controller;
            webRTCController.SetRemoteViewRenderer(remoteVideoView);
            webRTCController.SetARSceneView(sceneView);
        }

        public override void LoadView()
        {
            base.LoadView();
            appView = new AppView(CGRect.Empty, ref remoteVideoView, ref sceneView);
            View = appView;

        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            sceneView.Session.Run(new ARWorldTrackingConfiguration
            {
                AutoFocusEnabled = true,
                LightEstimationEnabled = false,
                WorldAlignment = ARWorldAlignment.Gravity
            }, ARSessionRunOptions.ResetTracking | ARSessionRunOptions.RemoveExistingAnchors);
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            sceneView.Session.Pause();
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
        }
    }
}
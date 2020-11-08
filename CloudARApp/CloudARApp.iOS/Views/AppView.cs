using AVFoundation;
using CoreGraphics;
using GLKit;
using Foundation;
using System;
using ARKit;
using UIKit;
using CoreImage;
using Xamarin.Forms.Platform.iOS;
using System.Diagnostics;
using System.Threading;

using GPUImage;
using GPUImage.Sources;
using GPUImage.Outputs;
using GPUImage.Filters;
using GPUImage.Filters.Blends;
using GPUImage.Filters.ColorProcessing;

using WebRTC.iOS.Bindings;

namespace CloudARApp.iOS.Views
{
    [Register("AppView")]
    public class AppView : UIView, IRTCVideoViewDelegate
    {
        public ARSCNView sceneView;

        public UIView RemoteView;
        private CGSize remoteVideoSize;

        private GPUImageFilter filter;

        public AppView(CGRect frame, ref ARVideoView remoteView, ref ARSCNView sceneView) : base(frame)
        {
            BackgroundColor = UIColor.Green;

            this.sceneView = sceneView;
            AddSubview(sceneView);

            remoteView.Delegate = this;
            RemoteView = remoteView;
            RemoteView.BackgroundColor = UIColor.Clear;
            AddSubview(RemoteView);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            var bounds = Bounds;
            sceneView.Frame = Frame;

            if (remoteVideoSize.Width > 0 && remoteVideoSize.Height > 0)
            {
                var remoteVideoFrame = bounds.WithAspectRatio(remoteVideoSize);
                nfloat scale = 1f;
                scale = bounds.Size.Height / remoteVideoFrame.Size.Height;

                remoteVideoFrame.Size = new CGSize(remoteVideoFrame.Size.Width * scale, remoteVideoFrame.Size.Height * scale);
                RemoteView.Frame = remoteVideoFrame;
                RemoteView.Center = new CGPoint(bounds.GetMidX(), bounds.GetMidY());
            }
            else
            {
                RemoteView.Frame = bounds;
            }
        }

        [Export("videoView:didChangeVideoSize:")]
        public void DidChangeVideoSize(IRTCVideoRenderer videoView, CGSize size)
        {
            if (videoView == RemoteView as IRTCVideoRenderer)
            {
                remoteVideoSize = size;
            }
            SetNeedsLayout();
        }
    }
}
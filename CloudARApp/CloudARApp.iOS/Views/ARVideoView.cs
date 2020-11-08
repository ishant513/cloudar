using CoreGraphics;
using System.Diagnostics;
using System;
using CoreVideo;
using AVFoundation;
using ObjCRuntime;
using CoreImage;

using GPUImage;
using GPUImage.Filters;
using GPUImage.Sources;


using WebRTC.iOS.Bindings;
using Xamarin.Forms;
using MetalKit;
using UIKit;
using System.IO;
using Foundation;
using GLKit;
using OpenGLES;
using OpenTK.Graphics.ES20;
using CoreFoundation;
using Metal;

namespace CloudARApp.iOS.Views
{
    public class ARVideoView : UIImageView, IRTCVideoRenderer
    {
        private nint height = 812;
        private nint width = 375;

        public IRTCVideoViewDelegate? Delegate { get; set; }

        public ARVideoView() : base()
        {
        }

        public void RenderFrame(RTCVideoFrame frame)
        {
            DispatchQueue.MainQueue.DispatchAsync(() => DecodeAlpha(frame));
        }

        public void DecodeAlpha(RTCVideoFrame frame)
        {
            var frameBuffer = Runtime.GetNSObject<RTCCVPixelBuffer>(frame.Buffer.Handle);
            var pixelBuffer = frameBuffer.PixelBuffer;
            pixelBuffer.Lock(CVPixelBufferLock.None);

            CGRect leftRect = new CGRect(0, 0, width, height);
            CGRect rightRect = new CGRect(width, 0, width, height);

            CIImage frameImage = new CIImage(pixelBuffer);

            CIImage leftImage = (CIImage)frameImage.Copy(); 
            leftImage = leftImage.ImageByCroppingToRect(leftRect);

            CIImage rightImage = (CIImage)frameImage.Copy();
            rightImage = rightImage.ImageByCroppingToRect(rightRect);
            CGAffineTransform translate = CGAffineTransform.MakeTranslation(-rightImage.Extent.X, 0);
            CIImage rightImageTranslated = rightImage.ImageByApplyingTransform(translate);

            CIImage alphaMask = new CIMaskToAlpha()
            {
                InputImage = rightImageTranslated
            }.OutputImage;
            CIImage blendedImage = new CIBlendWithAlphaMask()
            {
                InputImage = leftImage,
                MaskImage = alphaMask,
            }.OutputImage;

            Image = new UIImage(blendedImage);
        }


        public void SetSize(CGSize size)
        {
            size.Width /= 2;
            DispatchQueue.MainQueue.DispatchAsync(() => Delegate?.DidChangeVideoSize(this, size));
        }
    }
}
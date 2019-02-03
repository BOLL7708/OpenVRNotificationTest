using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Valve.VR;

namespace OpenVRNotificationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initializing connection to OpenVR
            var error = EVRInitError.None;
            OpenVR.Init(ref error, EVRApplicationType.VRApplication_Background);
            if (error != EVRInitError.None) Utils.PrintError($"OpenVR initialization errored: {Enum.GetName(typeof(EVRInitError), error)}");
            else
            {
                Utils.PrintInfo("OpenVR initialized successfully.");

                // Initializing notification overlay
                ulong handle = 0;
                var overlayError = OpenVR.Overlay.CreateOverlay(Guid.NewGuid().ToString(), "Test Overlay", ref handle);
                if (overlayError == EVROverlayError.None) Utils.PrintInfo("Successfully initiated notification overlay.");
                else Utils.PrintError("Failed to initialize notification overlay.");
                
                // Loading bitmap from disk
                var currentDir = Directory.GetCurrentDirectory();
                var path = $"{currentDir}\\boll_alpha.png";
                Bitmap bitmap = null;
                try
                {
                    bitmap = new Bitmap(path);
                    Utils.PrintInfo("Successfully loaded image from disk.");
                } catch(FileNotFoundException e)
                {
                    Utils.PrintError($"Failed to load image from disk: {e.Message}");
                }

                // Flip R & B channels in bitmap so it displays correctly
                int bytesPerPixel = Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                Utils.PrintDebug($"Pixel format of input bitmap: {bitmap.PixelFormat}");
                BitmapData data = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadWrite,
                    bitmap.PixelFormat
                );
                int length = Math.Abs(data.Stride) * bitmap.Height;
                unsafe
                {
                    byte* rgbValues = (byte*)data.Scan0.ToPointer();
                    for (int i = 0; i < length; i += bytesPerPixel)
                    {
                        byte dummy = rgbValues[i];
                        rgbValues[i] = rgbValues[i + 2];
                        rgbValues[i + 2] = dummy;
                    }
                }
                bitmap.UnlockBits(data);

                // Preparing notification bitmap
                BitmapData bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb // Alawys the same format here.
                );
                NotificationBitmap_t notificationBitmap = new NotificationBitmap_t
                {
                    m_pImageData = bitmapData.Scan0,
                    m_nWidth = bitmapData.Width,
                    m_nHeight = bitmapData.Height,
                    m_nBytesPerPixel = 4
                };

                // Creating notification
                var rnd = new Random();
                var id = (uint) rnd.Next();
                var notificationError = OpenVR.Notifications.CreateNotification(handle, 0, EVRNotificationType.Transient, "This is a test.", EVRNotificationStyle.Application, ref notificationBitmap, ref id);
                if (notificationError == EVRNotificationError.OK) Utils.PrintInfo("Notification was displayed successfully.");
                else Utils.PrintError($"Error creating notification: {Enum.GetName(typeof(EVRNotificationError), notificationError)}");

                bitmap.UnlockBits(bitmapData);
            }
            Console.ReadLine();
            OpenVR.Shutdown();
        }
    }
}

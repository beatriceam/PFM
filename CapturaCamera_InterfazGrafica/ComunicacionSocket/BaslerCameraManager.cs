using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using Basler.Pylon;
using ComunicacionSocket;

namespace SocketCommunication
{


    public class BaslerCameraManager : IDisposable
    {
        private bool _disposed = false;

        private Camera _camera;
        private DateTime _lastFrameTime;
        private double _averageFrameRate;
        private int _frameCount;
        private readonly object _frameRateLock = new object();
        private Timer _frameRateUpdateTimer;

        static Version sfnc2_0_0 = new Version(2, 0, 0);
        public static EnumName userDefaultSelector;
        public static string userDefaultSelectorUserSet1;

        public event EventHandler<IGrabResult> ImageCaptured;
        public event EventHandler<double> FrameRateUpdated;

        public BaslerCameraManager()
        {
            _camera = new Camera(CameraSelectionStrategy.FirstFound);
            Console.WriteLine("Using camera {0}.", _camera.CameraInfo[CameraInfoKey.ModelName]);
        }

        public bool Initialize()
        {
            try
            {
                _camera.CameraOpened += Configuration.AcquireContinuous;
                _camera.Open();

                // Ajuste dos parâmetros
                bool exposureSet = SetExposureTime(8000.0); 
                bool gainSet = SetGain(2.0); 

                if (!exposureSet || !gainSet)
                {
                    Console.WriteLine("Failed to set exposure or gain.");
                    return false;
                }

                ListAvailablePixelFormats();
                SetPixelFormatToMono8();
                SetResolution(512, 512);
                SetAcquisitionFrameRate(100.0);

                _lastFrameTime = DateTime.Now;
                _averageFrameRate = 0;
                _frameCount = 0;

                _frameRateUpdateTimer = new Timer(UpdateFrameRate, null, 500, 500);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize the camera: {ex.Message}");
                return false;
            }
        }


        private void UpdateFrameRate(object state)
        {
            lock (_frameRateLock)
            {
                FrameRateUpdated?.Invoke(this, _averageFrameRate);
            }
        }
        public bool SetResolution(int desiredWidth, int desiredHeight)
        {
            try
            {
                var offsetXParam = _camera.Parameters[PLCamera.OffsetX] as IIntegerParameter;
                var offsetYParam = _camera.Parameters[PLCamera.OffsetY] as IIntegerParameter;
                var widthParam = _camera.Parameters[PLCamera.Width] as IIntegerParameter;
                var heightParam = _camera.Parameters[PLCamera.Height] as IIntegerParameter;

                // Ajustar os offsets para o mínimo, se possível
                offsetXParam?.TrySetToMinimum();
                offsetYParam?.TrySetToMinimum();

                // Ajustar a largura dentro dos limites e incrementos permitidos
                if (widthParam != null && widthParam.IsWritable)
                {
                    long adjustedWidth = AdjustToIncrement(desiredWidth, widthParam.GetMinimum(), widthParam.GetMaximum(), widthParam.GetIncrement());
                    widthParam.SetValue(adjustedWidth);
                    Console.WriteLine($"Width set to {adjustedWidth}.");
                }
                else
                {
                    Console.WriteLine("Failed to set width: parameter is not writable.");
                    return false;
                }

                // Ajustar a altura dentro dos limites e incrementos permitidos
                if (heightParam != null && heightParam.IsWritable)
                {
                    long adjustedHeight = AdjustToIncrement(desiredHeight, heightParam.GetMinimum(), heightParam.GetMaximum(), heightParam.GetIncrement());
                    heightParam.SetValue(adjustedHeight);
                    Console.WriteLine($"Height set to {adjustedHeight}.");
                }
                else
                {
                    Console.WriteLine("Failed to set height: parameter is not writable.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting resolution: {ex.Message}");
                return false;
            }
        }


        public void Configure(Camera camera)
        {
            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
            {
                userDefaultSelector = PLCamera.UserSetDefaultSelector;
                userDefaultSelectorUserSet1 = PLCamera.UserSetDefaultSelector.UserSet1;
            }
            else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
            {
                userDefaultSelector = PLCamera.UserSetDefault;
                userDefaultSelectorUserSet1 = PLCamera.UserSetDefault.UserSet1;
            }
        }





        public bool SetExposureTime(double exposureTime)
        {
            try
            {
                // Desativa o controle automático de exposição, se estiver habilitado
                if (_camera.Parameters[PLCamera.ExposureAuto].IsWritable)
                {
                    _camera.Parameters[PLCamera.ExposureAuto].SetValue(PLCamera.ExposureAuto.Off);
                }

                if (_camera.GetSfncVersion() < sfnc2_0_0 && _camera.Parameters[PLCamera.ExposureTimeRaw].IsWritable)
                {
                    _camera.Parameters[PLCamera.ExposureTimeRaw].SetValue((long)exposureTime);
                    Console.WriteLine($"Exposure Time set to {exposureTime} µs using ExposureTimeRaw.");
                    return true;
                }
                else if (_camera.Parameters[PLCamera.ExposureTime].IsWritable)
                {
                    _camera.Parameters[PLCamera.ExposureTime].SetValue(exposureTime);
                    Console.WriteLine($"Exposure Time set to {exposureTime} µs using ExposureTime.");
                    return true;
                }
                else
                {
                    Console.WriteLine("ExposureTime parameter is not writable or not found.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting exposure time: {ex.Message}");
                return false;
            }
        }

        public bool SetGain(double gain)
        {
            try
            {
                if (_camera.Parameters[PLCamera.GainAuto].IsWritable)
                {
                    _camera.Parameters[PLCamera.GainAuto].SetValue(PLCamera.GainAuto.Off);
                }

                if (_camera.GetSfncVersion() < sfnc2_0_0 && _camera.Parameters[PLCamera.GainRaw].IsWritable)
                {
                    _camera.Parameters[PLCamera.GainRaw].SetValue((long)gain);
                    Console.WriteLine($"Gain set to {gain} dB using GainRaw.");
                    return true;
                }
                else if (_camera.Parameters[PLCamera.Gain].IsWritable)
                {
                    _camera.Parameters[PLCamera.Gain].SetValue(gain);
                    Console.WriteLine($"Gain set to {gain} dB using Gain.");
                    return true;
                }
                else
                {
                    Console.WriteLine("Gain parameter is not writable or not found.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting gain: {ex.Message}");
                return false;
            }
        }






        private long AdjustToIncrement(long desiredValue, long minValue, long maxValue, long increment)
        {
            // Garante que o valor desejado esteja dentro dos limites
            long adjustedValue = Math.Max(minValue, Math.Min(maxValue, desiredValue));

            // Ajusta o valor para o incremento mais próximo
            long remainder = (adjustedValue - minValue) % increment;
            if (remainder != 0)
            {
                adjustedValue -= remainder; // Ajusta para o valor inferior alinhado
            }

            return adjustedValue;
        }



        public bool SetAcquisitionFrameRate(double frameRate)
        {
            try
            {
                // Habilitar o controle da taxa de quadros
                var frameRateEnableParam = _camera.Parameters[PLCamera.AcquisitionFrameRateEnable] as IBooleanParameter;

                if (frameRateEnableParam != null && frameRateEnableParam.IsWritable)
                {
                    frameRateEnableParam.SetValue(true);
                    Console.WriteLine("AcquisitionFrameRate control enabled.");
                }
                else
                {
                    Console.WriteLine("Failed to enable AcquisitionFrameRate control.");
                    return false;
                }

                var frameRateParam = _camera.Parameters[PLCamera.AcquisitionFrameRate] as IFloatParameter;

                if (frameRateParam != null && frameRateParam.IsWritable)
                {
                    frameRateParam.SetValue(frameRate);
                    Console.WriteLine($"Acquisition frame rate set to {frameRate} FPS.");
                    return true;
                }
                else
                {
                    Console.WriteLine("Failed to set AcquisitionFrameRate.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting acquisition frame rate: {ex.Message}");
                return false;
            }
        }


        public bool SetPixelFormatToMono8()
        {
            var pixelFormatParam = _camera.Parameters[PLCamera.PixelFormat] as IEnumParameter;

            if (pixelFormatParam != null && pixelFormatParam.IsWritable)
            {
                if (pixelFormatParam.CanSetValue(PLCamera.PixelFormat.Mono8))
                {
                    pixelFormatParam.SetValue(PLCamera.PixelFormat.Mono8);
                    Console.WriteLine("Pixel format set to Mono8.");
                    return true;
                }
                else
                {
                    Console.WriteLine("Mono8 pixel format is not available.");
                }
            }
            else
            {
                Console.WriteLine("PixelFormat parameter is not writable or not found.");
            }

            return false;
        }

        private void ListAvailablePixelFormats()
        {
            Console.WriteLine("Available Pixel Formats:");
            var pixelFormatParam = _camera.Parameters[PLCamera.PixelFormat] as IEnumParameter;

            if (pixelFormatParam != null)
            {
                foreach (string value in pixelFormatParam.GetAllValues())
                {
                    Console.WriteLine($" - {value}");
                }
            }
            else
            {
                Console.WriteLine("PixelFormat parameter not found.");
            }
        }

        private void OnImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            IGrabResult grabResult = e.GrabResult;

            if (grabResult.GrabSucceeded)
            {
                lock (_frameRateLock)
                {
                    DateTime currentFrameTime = DateTime.Now;
                    double elapsedMilliseconds = (currentFrameTime - _lastFrameTime).TotalMilliseconds;

                    if (_frameCount > 0)
                    {
                        double currentFrameRate = 1000.0 / elapsedMilliseconds;
                        _averageFrameRate = (_averageFrameRate * _frameCount + currentFrameRate) / (_frameCount + 1);
                    }

                    _frameCount++;
                    _lastFrameTime = currentFrameTime;
                }


                ImageCaptured?.Invoke(this, grabResult);  
            }
            else
            {
                Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
            }
        }

        public void StartCapture()
        {
            try
            {
                if (_camera == null)
                {
                    throw new InvalidOperationException("Camera is not initialized.");
                }

                if (!_camera.IsOpen)
                {
                    _camera.Open();
                }

                if (!_camera.StreamGrabber.IsGrabbing)
                {
                    _camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);
                }

                if (_camera.CanWaitForFrameTriggerReady)
                {
                    _camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;
                }
                else
                {
                    Console.WriteLine("This sample can only be used with cameras that can be queried whether they are ready to accept the next frame trigger.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start capture: {ex.Message}");
            }
        }

        public void StopCapture()
        {
            try
            {
                if (_camera.StreamGrabber.IsGrabbing)
                {
                    _camera.StreamGrabber.Stop();
                }

                if (_camera.IsOpen)
                {
                    _camera.Close();
                }
                _camera.StreamGrabber.ImageGrabbed -= OnImageGrabbed;  // Remover a assinatura do evento

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to stop capture: {ex.Message}");
            }
            finally
            {
                _frameRateUpdateTimer?.Dispose();
                _frameRateUpdateTimer = null; // Garantir que o timer seja limpo
            }
        }


        public void Close()
        {
            StopCapture();

            if (_camera != null)
            {
                _camera.Dispose();
                _camera = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Close();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

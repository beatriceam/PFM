using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ComunicacionSocket;
using Basler.Pylon;
using System.Timers;
using System.Windows.Media;
using System.Windows;


namespace SocketCommunication
{
    public class MainViewModel : ObservableObject
    {
        private readonly object _frameLock = new object(); 

        private Client _client;
        private BaslerCameraManager _cameraManager;
        private bool _isConnected;
        private bool _isCapturing;
        private string _ipAddress;
        private int _port;
        private string _message;
        private string _response;
        private string _connectButtonText = "Connect";
        private string _captureButtonText = "Capture Image";
        private BitmapSource _loadedImage;
        private BitmapSource _sentImage;
        private BitmapSource _responseImage;
        private int _bytesSent;
        private int _bytesReceived; 
        private byte[] _receivedDataBuffer;
        private int _receivedBytesCount;
        private int _expectedImageSize = 1;
        private int _expectedImageWidth = 1;
        private int _expectedImageHeight = 1;
        private double _frameRate;
        private Timer _updateUITimer;
        private Bitmap _latestLoadedImage;
        private Bitmap _latestSentImage;
        private Bitmap _latestReceivedImage;
        private string _latestResponse;
        private int _latestBytesReceived;
        private bool _isSettingsVisible;

        public string IPAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                OnPropertyChanged(nameof(IPAddress));
            }
        }

        public int Port
        {
            get => _port;
            set
            {
                _port = value;
                OnPropertyChanged(nameof(Port));
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        public string Response
        {
            get => _response;
            set
            {
                _response = value;
                OnPropertyChanged(nameof(Response));
            }
        }

        public BitmapSource LoadedImage
        {
            get => _loadedImage;
            set
            {
                _loadedImage = value;
                OnPropertyChanged(nameof(LoadedImage));
            }
        }

        public BitmapSource SentImage
        {
            get => _sentImage;
            set
            {
                _sentImage = value;
                OnPropertyChanged(nameof(SentImage));
            }
        }

        public BitmapSource ResponseImage
        {
            get => _responseImage;
            set
            {
                _responseImage = value;
                OnPropertyChanged(nameof(ResponseImage));
            }
        }


        public int BytesSent
        {
            get => _bytesSent;
            set
            {
                _bytesSent = value;
                OnPropertyChanged(nameof(BytesSent));
            }
        }

        public int BytesReceived
        {
            get => _bytesReceived;
            set
            {
                _bytesReceived = value;
                OnPropertyChanged(nameof(BytesReceived));
            }
        }
        public double FrameRate
        {
            get => Math.Round(_frameRate, 2);
            set
            {
                _frameRate = value;
                OnPropertyChanged(nameof(FrameRate));
            }
        }

        public bool IsSettingsVisible
        {
            get => _isSettingsVisible;
            set => SetProperty(ref _isSettingsVisible, value);
        }


        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                ConnectButtonText = value ? "Disconnect" : "Connect";
                SetProperty(ref _isConnected, value);
            }
        }

        public string ConnectButtonText
        {
            get => _connectButtonText;
            set
            {
                SetProperty(ref _connectButtonText, value);
            }
        }

        public bool IsCapturing
        {
            get => _isCapturing;
            set
            {
                CaptureButtonText = value ? "Stop Capturing" : "Capture Image";
                SetProperty(ref _isCapturing, value);
            }
        }

        public string CaptureButtonText
        {
            get => _captureButtonText;
            set
            {
                SetProperty(ref _captureButtonText, value);
            }
        }

        public ICommand ConnectCommand { get; }
        public ICommand SendCommand { get; }
        public ICommand LoadImageCommand { get; }
        public ICommand CaptureImageCommand { get; }
        public ICommand StopCaptureCommand { get; }
        public ICommand OpenSettingsCommand { get; }


        public MainViewModel()
        {
            ConnectCommand = new RelayCommand(ToggleConnection);
            SendCommand = new RelayCommand(SendMessage, CanSendMessage);
            LoadImageCommand = new RelayCommand(LoadImage);
            CaptureImageCommand = new RelayCommand(ToggleCaptureImage);
            OpenSettingsCommand = new RelayCommand(OpenSettings);

            // Default values
            _isConnected = false;
            _port = 7;
            _ipAddress = "192.168.1.210";

            _updateUITimer = new Timer(100); 
            _updateUITimer.Elapsed += OnUpdateUITimerElapsed;
            _updateUITimer.Start();
        }

        private void OnUpdateUITimerElapsed(object sender, ElapsedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (_latestLoadedImage != null)
                {
                    lock (_frameLock)
                    {
                        LoadedImage = BitmapConverter.ConvertBitmapToBitmapSource(_latestLoadedImage);
                    }
                }
                if (_latestSentImage != null)
                {
                    lock (_frameLock)
                    {
                        SentImage = BitmapConverter.ConvertBitmapToBitmapSource(_latestSentImage);
                    }
                }
                if (_latestReceivedImage != null)
                {
                    lock (_frameLock)
                    {
                        ResponseImage = BitmapConverter.ConvertBitmapToBitmapSource(_latestReceivedImage);
                    }
                }
                if (_latestResponse != null)
                {
                    lock (_frameLock)
                    {
                        Response = _latestResponse;
                    }
                }
                if (_latestBytesReceived > 0)
                {
                    lock (_frameLock)
                    {
                        BytesReceived = _latestBytesReceived;
                    }
                }
            });
        }


        private void OpenSettings(object parameter)
        {
            IsSettingsVisible = !IsSettingsVisible;  
        }


        private void ToggleConnection(object parameter)
        {
            if (IsConnected)
            {
                Disconnect(null);
            }
            else
            {
                Connect(null);
            }
        }

        private void ToggleCaptureImage(object parameter)
        {
            if (IsCapturing)
            {
                StopCapture(null);
            }
            else
            {
                CaptureImage(null);
            }
        }


        private bool CanConnect(object parameter) => !IsConnected;
        private void Connect(object parameter)
        {
            _client = new Client(_ipAddress, _port);

            _client.DataReceived += OnDataReceived;

            if (_client.ConnectToServer())
            {
                IsConnected = true;

                _receivedDataBuffer = new byte[_expectedImageSize];
                _receivedBytesCount = 0;
            }
            else
            {
                _client = null;
            }
        }
        private void OnImageCaptured(object sender, IGrabResult grabResult)
        {
            byte[] mono8Data = grabResult.PixelData as byte[];

            int imageWidth = grabResult.Width;
            int imageHeight = grabResult.Height;

            lock (_frameLock)
            {
                _latestLoadedImage = BitmapConverter.ConvertByteArrayToBitmap(mono8Data, imageWidth, imageHeight, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            }
        }


        private void OnFrameRateUpdated(object sender, double frameRate)
        {
            FrameRate = frameRate;
        }

        private void CaptureImage(object parameter)
        {
            _cameraManager = new BaslerCameraManager();
            _cameraManager.ImageCaptured += OnImageCaptured;
            _cameraManager.FrameRateUpdated += OnFrameRateUpdated;

            if (_cameraManager.Initialize())
            {
                _cameraManager.StartCapture();

                IsCapturing = true;
            }
        }


        private void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            Array.Copy(e.Data, 0, _receivedDataBuffer, _receivedBytesCount, e.Length);
            _receivedBytesCount += e.Length;

            _latestBytesReceived = _receivedBytesCount;

            if (_receivedBytesCount >= _expectedImageSize)
            {
                lock (_frameLock)
                {
                    _latestReceivedImage = BitmapConverter.ConvertByteArrayToBitmap(_receivedDataBuffer, _expectedImageWidth, _expectedImageHeight, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                    _latestResponse = BitConverter.ToString(BitmapConverter.ConvertBitmapToByteArray(_latestReceivedImage)).Replace("-", " ");
                }

                _receivedBytesCount = 0;
                Array.Clear(_receivedDataBuffer, 0, _receivedDataBuffer.Length);
            }
        }


        private bool CanSendMessage(object parameter) => _isConnected;
        private void SendMessage(object parameter)
        {
            //StopCapture(null);

            if (_client != null && _isConnected)
            {
                byte[] dataToSend;
                Bitmap imageToSend = null;

                if (_latestLoadedImage == null) return;

                imageToSend = _latestLoadedImage;

                if (imageToSend.Width != 512 || imageToSend.Height != 512)
                {
                    imageToSend = BitmapConverter.ResizeBitmap(imageToSend, 512, 512);
                }

                if (imageToSend.PixelFormat != System.Drawing.Imaging.PixelFormat.Format8bppIndexed) 
                {
                    imageToSend = BitmapConverter.ConvertToMono8(imageToSend);
                }

                dataToSend = BitmapConverter.ConvertBitmapToByteArray(imageToSend);

                _latestSentImage = (Bitmap)imageToSend.Clone();
                _expectedImageWidth = imageToSend.Width;
                _expectedImageHeight = imageToSend.Height;
                _expectedImageSize = imageToSend.Width * imageToSend.Height;


                int result = _client.SendData(dataToSend, dataToSend.Length);
                BytesSent = dataToSend.Length;

                if (result < 0)
                {

                }
            }
        }



        private void LoadImage(object parameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                Bitmap bitmap = new Bitmap(openFileDialog.FileName);
                _latestLoadedImage = bitmap;

                _expectedImageWidth = bitmap.Width;
                _expectedImageHeight = bitmap.Height;
                _expectedImageSize = _expectedImageWidth * _expectedImageHeight;

                _receivedDataBuffer = new byte[_expectedImageSize];

            }
        }

        private bool CanStopCapture(object parameter) => _cameraManager != null;
        private void StopCapture(object parameter)
        {
            if (_cameraManager != null)
            {
                _cameraManager.StopCapture();
                _cameraManager.ImageCaptured -= OnImageCaptured;
                _cameraManager.FrameRateUpdated -= OnFrameRateUpdated;
                _cameraManager = null;
                IsCapturing = false;
            }
        }

        private bool CanDisconnect(object parameter) => _isConnected;
        private void Disconnect(object parameter)
        {
            if (_client != null) _client.CloseClient();
            _client = null;
            IsConnected = false;
        }

        private void Exit(object parameter)
        {
            Disconnect(null);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Translation;
using CameraControl.Core.Wpf;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Timer = System.Timers.Timer;


namespace Macrophotography
{
    public class LiveViewViewModel : ViewModelBase
    {
        private bool _operInProgress = false;
        private Timer _timer = new Timer(1000 / 25.0);

        private ICameraDevice _cameraDevice;
        private BitmapSource _bitmap;
        private bool _settingArea;
        private CameraProperty _cameraProperty;

        public ICameraDevice SelectedCameraDevice
        {
            get { return _cameraDevice; }
            set
            {
                _cameraDevice = value;
                RaisePropertyChanged(() => SelectedCameraDevice);
            }
        }
        
        public bool ShowRuler
        {
            get { return CameraProperty.LiveviewSettings.ShowRuler; }
            set
            {
                CameraProperty.LiveviewSettings.ShowRuler = value;
                RaisePropertyChanged(() => ShowRuler);
            }
        }

        public bool SettingArea
        {
            get { return _settingArea; }
            set
            {
                _settingArea = value;
                RaisePropertyChanged(() => SettingArea);
                RaisePropertyChanged(() => NoSettingArea);
            }
        }

        public bool NoSettingArea
        {
            get { return !SettingArea; }
        }

        public Rect RullerRect
        {
            get { return new Rect(HorizontalMin, VerticalMin, HorizontalMax, VerticalMax); }
        }

        public int HorizontalMin
        {
            get { return CameraProperty.LiveviewSettings.HorizontalMin; }
            set
            {
                CameraProperty.LiveviewSettings.HorizontalMin = value;
                RaisePropertyChanged(() => RullerRect);
            }
        }

        public int HorizontalMax
        {
            get { return CameraProperty.LiveviewSettings.HorizontalMax; }
            set
            {
                CameraProperty.LiveviewSettings.HorizontalMax = value;
                RaisePropertyChanged(() => RullerRect);
            }
        }

        public int VerticalMin
        {
            get { return CameraProperty.LiveviewSettings.VerticalMin; }
            set
            {
                CameraProperty.LiveviewSettings.VerticalMin = value;
                RaisePropertyChanged(() => RullerRect);
            }
        }

        public CameraProperty CameraProperty
        {
            get { return _cameraProperty; }
            set
            {
                _cameraProperty = value;
                RaisePropertyChanged(() => CameraProperty);
            }
        }

        public int VerticalMax
        {
            get { return CameraProperty.LiveviewSettings.VerticalMax; }
            set
            {
                CameraProperty.LiveviewSettings.VerticalMax = value;
                RaisePropertyChanged(() => RullerRect);
            }
        }

        public BitmapSource Bitmap
        {
            get { return _bitmap; }
            set
            {
                _bitmap = value;
                RaisePropertyChanged(() => Bitmap);
            }
        }
        
        public bool IsActive { get; set; }
        public LiveViewData LiveViewData { get; set; }

        public RelayCommand StartLiveViewCommand { get; set; }
        public RelayCommand StopLiveViewCommand { get; set; }

        public RelayCommand SetAreaCommand { get; set; }
        public RelayCommand DoneSetAreaCommand { get; set; }

        public LiveViewViewModel()
        {
            StartLiveViewCommand = new RelayCommand(StartLiveView);
            StopLiveViewCommand = new RelayCommand(StopLiveView);
            SetAreaCommand = new RelayCommand(() => SettingArea = true);
            DoneSetAreaCommand = new RelayCommand(() => SettingArea = false);

            _timer.AutoReset = true;
            _timer.Elapsed += _timer_Elapsed;
            if (!IsInDesignMode)
            {
                SelectedCameraDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
                CameraProperty = SelectedCameraDevice.LoadProperties();
            }
        }

        void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Task.Factory.StartNew(GetLiveImage);
        }

        private void GetLiveImage()
        {
            if (_operInProgress)
                return;

            try
            {
                LiveViewData = SelectedCameraDevice.GetLiveViewImage();
            }
            catch (Exception ex)
            {
                Log.Error("Error geting lv", ex);
                _operInProgress = false;
                return;
            }

            if (LiveViewData == null)
            {
                _operInProgress = false;
                return;
            }

            try
            {
                if (LiveViewData != null && LiveViewData.ImageData != null)
                {
                    MemoryStream stream = new MemoryStream(LiveViewData.ImageData,
                        LiveViewData.
                            ImageDataPosition,
                        LiveViewData.ImageData.
                            Length -
                        LiveViewData.
                            ImageDataPosition);
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = stream;
                    bi.EndInit();
                    bi.Freeze();
                    WriteableBitmap bitmap = BitmapFactory.ConvertToPbgra32Format(bi);
                    DrawGrid(bitmap);
                    bitmap.Freeze();
                    Bitmap = bitmap;
                    _operInProgress = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error geting lv", ex);
                _operInProgress = false;
                return;
            }
        }

        private void DrawGrid(WriteableBitmap writeableBitmap)
        {
            System.Windows.Media.Color color = Colors.White;
            color.A = 50;

            if (ShowRuler && NoSettingArea)
            {
                int x1 = writeableBitmap.PixelWidth * (HorizontalMin) / 1000;
                int x2 = writeableBitmap.PixelWidth * (HorizontalMin + HorizontalMax) / 1000;
                int y2 = writeableBitmap.PixelHeight * (VerticalMin + VerticalMax) / 1000;
                int y1 = writeableBitmap.PixelHeight * VerticalMin / 1000;

                writeableBitmap.FillRectangle2(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight, System.Windows.Media.Color.FromArgb(128, 128, 128, 128));
                writeableBitmap.FillRectangleDeBlend(x1, y1, x2, y2, System.Windows.Media.Color.FromArgb(128, 128, 128, 128));
                writeableBitmap.DrawRectangle(x1, y1, x2, y2, color);

            }

        }

        private void StartLiveView()
        {
            //if (!IsActive)
            //    return;

            string resp = SelectedCameraDevice.GetProhibitionCondition(OperationEnum.LiveView);
            if (string.IsNullOrEmpty(resp))
            {
                Thread thread = new Thread(StartLiveViewThread);
                thread.Start();
                thread.Join();
            }
            else
            {
                Log.Error("Error starting live view " + resp);
                // in nikon case no show error message
                // if the images not transferd yet from SDRam
                if (resp != "LabelImageInRAM" && resp != "LabelCommandProcesingError")
                    ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.LiveViewWnd_Message,
                        TranslationStrings.LabelLiveViewError + "\n" +
                        TranslationManager.GetTranslation(resp));
            }
        }

        private void StartLiveViewThread()
        {
            try
            {
                bool retry = false;
                int retryNum = 0;
                Log.Debug("LiveView: Liveview started");
                do
                {
                    try
                    {
                        SelectedCameraDevice.StartLiveView();
                    }
                    catch (DeviceException deviceException)
                    {
                        if (deviceException.ErrorCode == ErrorCodes.ERROR_BUSY ||
                            deviceException.ErrorCode == ErrorCodes.MTP_Device_Busy)
                        {
                            Thread.Sleep(100);
                            Log.Debug("Retry live view :" + deviceException.ErrorCode.ToString("X"));
                            retry = true;
                            retryNum++;
                        }
                        else
                        {
                            throw;
                        }
                    }
                } while (retry && retryNum < 35);
                _timer.Start();
                _operInProgress = false;
                Log.Debug("LiveView: Liveview start done");
            }
            catch (Exception exception)
            {
                Log.Error("Unable to start liveview !", exception);
                StaticHelper.Instance.SystemMessage = "Unable to start liveview ! " + exception.Message;
            }
        }

        private void StopLiveView()
        {
            if (!SelectedCameraDevice.IsChecked)
                return;
            Thread thread = new Thread(StopLiveViewThread);
            thread.Start();
        }

        private void StopLiveViewThread()
        {
            try
            {
                bool retry = false;
                int retryNum = 0;
                Log.Debug("LiveView: Liveview stopping");
                do
                {
                    try
                    {
                        SelectedCameraDevice.StopLiveView();
                    }
                    catch (DeviceException deviceException)
                    {
                        if (deviceException.ErrorCode == ErrorCodes.ERROR_BUSY ||
                            deviceException.ErrorCode == ErrorCodes.MTP_Device_Busy)
                        {
                            Thread.Sleep(500);
                            Log.Debug("Retry live view stop:" + deviceException.ErrorCode.ToString("X"));
                            retry = true;
                            retryNum++;
                        }
                        else
                        {
                            throw;
                        }
                    }
                } while (retry && retryNum < 35);
            }
            catch (Exception exception)
            {
                Log.Error("Unable to stop liveview !", exception);
                StaticHelper.Instance.SystemMessage = "Unable to stop liveview ! " + exception.Message;
            }
        }
    }
}

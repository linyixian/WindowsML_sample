using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

//
using vegetables;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.System.Threading;
using Windows.Storage;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Graphics.Imaging;
using Windows.Devices.Enumeration;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace WindowsML_sample
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private MediaCapture mediaCapture;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1);                     //複数のスレッドで検出しないようにするためのsemaphore
        private ThreadPoolTimer timer;
        private VegetablesModel vegetableModel = new VegetablesModel();             //AIモデルオブジェクト
        private VegetablesModelInput inputData = new VegetablesModelInput();        //input用オブジェクト（VideoFrame）
        private VegetablesModelOutput outputData = new VegetablesModelOutput();     //output用オブジェクト（List）

        /// <summary>
        /// 
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await InitCameraAsync();

            LoadModel();
        }


        /// <summary>
        /// カメラの初期化及びタイマーの起動
        /// </summary>
        /// <returns></returns>
        private async Task InitCameraAsync()
        {
            try
            {
                //mediaCaptureオブジェクトが有効な時は一度Disposeする
                if (mediaCapture != null)
                {
                    mediaCapture.Dispose();
                    mediaCapture = null;
                }

                //キャプチャーの設定
                var captureInitSettings = new MediaCaptureInitializationSettings();
                captureInitSettings.VideoDeviceId = "";
                captureInitSettings.StreamingCaptureMode = StreamingCaptureMode.Video;

                //カメラデバイスの取得
                var cameraDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

                if (cameraDevices.Count() == 0)
                {
                    Debug.WriteLine("No Camera");
                    return;
                }
                else if (cameraDevices.Count() == 1)
                {
                    captureInitSettings.VideoDeviceId = cameraDevices[0].Id;
                }
                else
                {
                    captureInitSettings.VideoDeviceId = cameraDevices[1].Id;
                }

                //キャプチャーの準備
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync(captureInitSettings);

                VideoEncodingProperties vp = new VideoEncodingProperties();

                //RasperryPiでは解像度が高いと映像が乱れるので小さい解像度にしている
                vp.Height = 240;
                vp.Width = 320;
                vp.Subtype = "YUY2";

                await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, vp);

                capture.Source = mediaCapture;

                //キャプチャーの開始
                await mediaCapture.StartPreviewAsync();

                Debug.WriteLine("Camera Initialized");

                //15FPS毎にタイマーを起動する。
                TimeSpan timerInterval = TimeSpan.FromMilliseconds(66);
                timer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(CurrentVideoFrame), timerInterval);

            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timer"></param>
        private async void CurrentVideoFrame(ThreadPoolTimer timer)
        {
            //複数スレッドでの同時実行を抑制
            if (!semaphore.Wait(0))
            {
                return;
            }

            try
            {
                //AIモデルのインプットデータは解像度227x227,BGRA8にする必要がある。
                using (VideoFrame previewFrame =new VideoFrame(BitmapPixelFormat.Bgra8, 227, 227))
                {
                    await this.mediaCapture.GetPreviewFrameAsync(previewFrame);

                    if (previewFrame != null)
                    {
                        inputData.data = previewFrame;

                        //AIモデルにデータを渡すと推定値の入ったリストが返る
                        outputData = await vegetableModel.EvaluateAsync(inputData);

                        //UIスレッドに結果を表示
                        var ignored = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,  () =>
                           {
                               string result = "";

                               result = " cabbage: "+outputData.loss["cabbage"].ToString("P")+"\n";
                               result = result + " cucumber: " + outputData.loss["cucumber"].ToString("P") + "\n";
                               result = result + " onion: " + outputData.loss["onion"].ToString("P") + "\n";
                               result = result + " poteto: " + outputData.loss["poteto"].ToString("P") + "\n";
                               result = result + " tomato: " + outputData.loss["tomato"].ToString("P");
                          
                               this.msgTbk.Text = result;
                           });
                    }                    
                }
            }

            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// AIモデルをロード
        /// </summary>
        private async void LoadModel()
        {
            StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/vegetables.onnx"));
            vegetableModel = await VegetablesModel.CreateVegetablesModel(modelFile);
        }
        
    }
}

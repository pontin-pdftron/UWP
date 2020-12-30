using CompleteReader.ViewModels.Common;
using pdftron.PDF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompleteReader.ViewModels.Viewer.Helpers
{
    public class OptimizeFileViewModel : ViewModelBase
    {
        public enum State
        {
            Basic,
            Advanced,
        }

        private State _CurrentState = State.Basic;
        public State CurrentState
        {
            get { return _CurrentState; }
            set { Set(ref _CurrentState, value); }
        }

        private bool _IsPopupOpen = false;
        public bool IsPopupOpen
        {
            get { return _IsPopupOpen; }
            set
            {
                if (Set(ref _IsPopupOpen, value))
                {
                    if (!value)
                    {
                        PopupClosed();
                    }
                    else
                    {
                        SetDefaultValues();
                    }
                }
            }
        }

        public enum BasicFileSize
        {
            High,
            Balanced,
            Small
        }

        public enum CompressionColor
        {
            None,
            PNG,
            JPEG,
            JPEG2000,
        }

        public enum CompressionMonochrome
        {
            PNG,
            JBIG2
        }
        public enum CompressionQuality
        {
            Low,
            Medium,
            High,
            Max,
        }

        private BasicFileSize _FileSizeOption = BasicFileSize.Balanced;

        public BasicFileSize FileSizeOption
        {
            get { return _FileSizeOption; }
            set { Set(ref _FileSizeOption, value); }
        }

        private DpiOption _GreaterThanDpiOption;

        public DpiOption GreaterThanDpiOption
        {
            get { return _GreaterThanDpiOption; }
            set
            {
                Set(ref _GreaterThanDpiOption, value);
                RaisePropertyChanged("DownSampleDpiCollection");
                if (DownSampleDpiOption == null)
                {
                    DownSampleDpiOption = DownSampleDpiCollection.LastOrDefault();
                }
            }
        }

        private DpiOption _DownSampleDpiOption;

        public DpiOption DownSampleDpiOption
        {
            get { return _DownSampleDpiOption; }
            set { Set(ref _DownSampleDpiOption, value); }
        }

        public CompressionColorItem _CompressionColorOption;

        public CompressionColorItem CompressionColorOption
        {
            get { return _CompressionColorOption; }
            set
            {
                if (Set(ref _CompressionColorOption, value))
                {
                    RaisePropertyChanged("HasQuality");
                }
            }
        }
        public CompressionMonochromeItem _CompressionMonochromeOption;

        public CompressionMonochromeItem CompressionMonochromeOption
        {
            get { return _CompressionMonochromeOption; }
            set { Set(ref _CompressionMonochromeOption, value); }
        }
        public CompressionQualityItem _CompressionQualityOption;

        public CompressionQualityItem CompressionQualityOption
        {
            get { return _CompressionQualityOption; }
            set { Set(ref _CompressionQualityOption, value); }
        }

        public bool HasQuality
        {
            get { return CompressionColorOption != null && (CompressionColorOption.ColorOption == CompressionColor.JPEG || CompressionColorOption.ColorOption == CompressionColor.JPEG2000); }
        }

        private ObservableCollection<CompressionColorItem> _CompressionColorOptionCollection;
        public ObservableCollection<CompressionColorItem> CompressionColorOptionCollection { get { return _CompressionColorOptionCollection; } }

        private ObservableCollection<CompressionMonochromeItem> _CompressionMonochromeOptionCollection;
        public ObservableCollection<CompressionMonochromeItem> CompressionMonochromeOptionCollection { get { return _CompressionMonochromeOptionCollection; } }

        private ObservableCollection<CompressionQualityItem> _CompressionQualityOptionCollection;
        public ObservableCollection<CompressionQualityItem> CompressionQualityOptionCollection { get { return _CompressionQualityOptionCollection; } }

        private ObservableCollection<DpiOption> _GreaterThanDpiCollection;
        public ObservableCollection<DpiOption> GreaterThanDpiCollection { get { return _GreaterThanDpiCollection; } }

        public ObservableCollection<DpiOption> DownSampleDpiCollection
        {
            get
            {
                return new ObservableCollection<DpiOption>(GreaterThanDpiCollection.Where(x => (GreaterThanDpiOption != null && x.Dpi <= GreaterThanDpiOption.Dpi) && x.Dpi <= 225));
            }
        }

        public delegate void PopupClosedDelegate();
        public event PopupClosedDelegate PopupClosed;

        public delegate void OptimizeConfirmedDelegate();
        public event OptimizeConfirmedDelegate OptimizeConfirmed;

        public class CompressionColorItem
        {
            public string OptionName { get; private set; }

            public CompressionColor ColorOption { get; private set; }

            public CompressionColorItem(CompressionColor colorOption)
            {
                ColorOption = colorOption;
                OptionName = CompressionColorOptionToString(colorOption);
            }
        }

        public class CompressionMonochromeItem
        {
            public string OptionName { get; private set; }

            public CompressionMonochrome MonochromeOption { get; private set; }

            public CompressionMonochromeItem(CompressionMonochrome colorOption)
            {
                MonochromeOption = colorOption;
                OptionName = CompressionMonochromeOptionToString(colorOption);
            }
        }

        public class CompressionQualityItem
        {
            public string OptionName { get; private set; }

            public CompressionQuality QualityOption { get; private set; }

            public CompressionQualityItem(CompressionQuality colorOption)
            {
                QualityOption = colorOption;
                OptionName = CompressionQualityOptionToString(colorOption);
            }
        }

        public class DpiOption
        {
            public int Dpi { get; private set; }

            public string DpiString { get; private set; }

            public DpiOption(int dpi)
            {
                Dpi = dpi;
                DpiString = dpi + " DPI";
            }
        }

        public OptimizeFileViewModel()
        {
            Init();
        }

        private void Init()
        {
            AdvancedCommand = new RelayCommand(AdvancedCommandImpl);
            BasicCommand = new RelayCommand(BasicCommandImpl);
            BasicFileSizeCheckedCommand = new RelayCommand(BasicFileSizeCheckedCommandImpl);
            CancelCommand = new RelayCommand(CancelCommandImpl);
            OkCommand = new RelayCommand(OkCommandImpl);

            PrepareCompressionColorOptions();
            PrepareCompressionMonochromeOptions();
            PrepareCompressionQualityOptions();
            PrepareGreaterThanDpiOptions();
        }

        private void SetDefaultValues()
        {
            CurrentState = State.Basic;
            CompressionColorOption = CompressionColorOptionCollection.Where(x => x.ColorOption == CompressionColor.JPEG).FirstOrDefault();
            CompressionMonochromeOption = CompressionMonochromeOptionCollection.Where(x => x.MonochromeOption == CompressionMonochrome.JBIG2).FirstOrDefault();
            CompressionQualityOption = CompressionQualityOptionCollection.Where(x => x.QualityOption == CompressionQuality.High).FirstOrDefault();
            GreaterThanDpiOption = GreaterThanDpiCollection.Where(x => x.Dpi == 225).FirstOrDefault();
            DownSampleDpiOption = DownSampleDpiCollection.Where(x => x.Dpi == 150).FirstOrDefault();
        }

        public RelayCommand AdvancedCommand { get; private set; }
        public RelayCommand BasicCommand { get; private set; }
        public RelayCommand BasicFileSizeCheckedCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }
        public RelayCommand OkCommand { get; private set; }

        private void AdvancedCommandImpl(object command)
        {
            CurrentState = State.Advanced;
        }

        private void BasicCommandImpl(object command)
        {
            CurrentState = State.Basic;
        }

        private void BasicFileSizeCheckedCommandImpl(object command)
        {
            BasicFileSize option;
            bool result = Enum.TryParse(command.ToString(), out option);
            if (!result)
                return;
            
            switch (option)
            {
                case BasicFileSize.High:
                    FileSizeOption = BasicFileSize.High;
                    break;
                case BasicFileSize.Balanced:
                    FileSizeOption = BasicFileSize.Balanced;
                    break;
                case BasicFileSize.Small:
                    FileSizeOption = BasicFileSize.Small;
                    break;
            }
        }

        private void CancelCommandImpl(object command)
        {
            IsPopupOpen = false;
        }

        private void OkCommandImpl(object command)
        {
            _IsPopupOpen = false;
            RaisePropertyChanged("IsPopupOpen");
            OptimizeConfirmed();
        }

        public static string CompressionColorOptionToString(CompressionColor option)
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            switch (option)
            {
                case CompressionColor.None:
                    return loader.GetString("ViewerPage_Optimize_Dialog_Advanced_Compression_Color_None");
                case CompressionColor.PNG:
                    return loader.GetString("ViewerPage_Optimize_Dialog_Advanced_Compression_Color_PNG");
                case CompressionColor.JPEG:
                    return loader.GetString("ViewerPage_Optimize_Dialog_Advanced_Compression_Color_JPEG");
                default:
                    return loader.GetString("ViewerPage_Optimize_Dialog_Advanced_Compression_Color_JPEG2000");
            }
        }
        public static string CompressionMonochromeOptionToString(CompressionMonochrome option)
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            switch (option)
            {
                case CompressionMonochrome.PNG:
                    return loader.GetString("ViewerPage_Optimize_Dialog_Advanced_Compression_Monochrome_PNG");
                default:
                    return loader.GetString("ViewerPage_Optimize_Dialog_Advanced_Compression_Monochrome_JBIG2");
            }
        }
        public static string CompressionQualityOptionToString(CompressionQuality option)
        {
            Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            switch (option)
            {
                case CompressionQuality.Low:
                    return loader.GetString("ViewerPage_Optimize_Dialog_Advanced_Compression_Quality_Low");
                case CompressionQuality.Medium:
                    return loader.GetString("ViewerPage_Optimize_Dialog_Advanced_Compression_Quality_Medium");
                case CompressionQuality.High:
                    return loader.GetString("ViewerPage_Optimize_Dialog_Advanced_Compression_Quality_High");
                default:
                    return loader.GetString("ViewerPage_Optimize_Dialog_Advanced_Compression_Quality_Max");
            }
        }

        private void PrepareCompressionColorOptions()
        {
            _CompressionColorOptionCollection = new ObservableCollection<CompressionColorItem>();
            _CompressionColorOptionCollection.Add(new CompressionColorItem(CompressionColor.None));
            _CompressionColorOptionCollection.Add(new CompressionColorItem(CompressionColor.PNG));
            CompressionColorOption = new CompressionColorItem(CompressionColor.JPEG);
            _CompressionColorOptionCollection.Add(CompressionColorOption);
            _CompressionColorOptionCollection.Add(new CompressionColorItem(CompressionColor.JPEG2000));
        }

        private void PrepareCompressionMonochromeOptions()
        {
            _CompressionMonochromeOptionCollection = new ObservableCollection<CompressionMonochromeItem>();
            _CompressionMonochromeOptionCollection.Add(new CompressionMonochromeItem(CompressionMonochrome.PNG));
            CompressionMonochromeOption = new CompressionMonochromeItem(CompressionMonochrome.JBIG2);
            _CompressionMonochromeOptionCollection.Add(CompressionMonochromeOption);
        }

        private void PrepareCompressionQualityOptions()
        {
            _CompressionQualityOptionCollection = new ObservableCollection<CompressionQualityItem>();
            _CompressionQualityOptionCollection.Add(new CompressionQualityItem(CompressionQuality.Low));
            _CompressionQualityOptionCollection.Add(new CompressionQualityItem(CompressionQuality.Medium));
            CompressionQualityOption = new CompressionQualityItem(CompressionQuality.High);
            _CompressionQualityOptionCollection.Add(CompressionQualityOption);
            _CompressionQualityOptionCollection.Add(new CompressionQualityItem(CompressionQuality.Max));
        }

        private void PrepareGreaterThanDpiOptions()
        {
            _GreaterThanDpiCollection = new ObservableCollection<DpiOption>();
            _GreaterThanDpiCollection.Add(new DpiOption(50));
            _GreaterThanDpiCollection.Add(new DpiOption(72));
            _GreaterThanDpiCollection.Add(new DpiOption(96));
            _GreaterThanDpiCollection.Add(new DpiOption(120));
            DownSampleDpiOption = new DpiOption(150);
            _GreaterThanDpiCollection.Add(DownSampleDpiOption);
            GreaterThanDpiOption = new DpiOption(225);
            _GreaterThanDpiCollection.Add(GreaterThanDpiOption);
            _GreaterThanDpiCollection.Add(new DpiOption(300));
            _GreaterThanDpiCollection.Add(new DpiOption(600));
        }

        private OptimizerMonoImageSettingsCompressionMode GetPdfNetSettingFromCompressionMonochrome()
        {
            switch (CompressionMonochromeOption.MonochromeOption)
            {
                case CompressionMonochrome.PNG:
                    return OptimizerMonoImageSettingsCompressionMode.e_flate;
                default:
                    return OptimizerMonoImageSettingsCompressionMode.e_jbig2;
            }
        }

        private OptimizerImageSettingsCompressionMode GetPdfNetSettingFromCompressionColor()
        {
            switch (CompressionColorOption.ColorOption)
            {
                case CompressionColor.None:
                    return OptimizerImageSettingsCompressionMode.e_none;
                case CompressionColor.PNG:
                    return OptimizerImageSettingsCompressionMode.e_flate;
                case CompressionColor.JPEG:
                    return OptimizerImageSettingsCompressionMode.e_jpeg;
                default:
                    return OptimizerImageSettingsCompressionMode.e_jpeg2000;
            }
        }

        private int GetPdfNetSettingFromCompressionQuality()
        {
            switch (CompressionQualityOption.QualityOption)
            {
                case CompressionQuality.Low:
                    return 4;
                case CompressionQuality.Medium:
                    return 6;
                case CompressionQuality.High:
                    return 8;
                default:
                    return 10;
            }
        }

        public void Optimize(PDFDoc doc)
        {
            if (doc == null)
                return;

            bool shouldUnlock = false;
            try
            {
                doc.Lock();
                shouldUnlock = true;

                OptimizerOptimizerSettings optimizerSettings = new OptimizerOptimizerSettings();
                OptimizerImageSettings imageSettings = new OptimizerImageSettings();
                OptimizerMonoImageSettings monoSettings = new OptimizerMonoImageSettings();

                OptimizerImageSettingsDownsampleMode imageDownSettingMode = OptimizerImageSettingsDownsampleMode.e_off;
                OptimizerImageSettingsCompressionMode imageCompressionSettingMode = OptimizerImageSettingsCompressionMode.e_none;
                OptimizerMonoImageSettingsDownsampleMode monoDownSettingMode = OptimizerMonoImageSettingsDownsampleMode.e_off;
                OptimizerMonoImageSettingsCompressionMode monoCompressionSettingMode = OptimizerMonoImageSettingsCompressionMode.e_none;

                double imageMaxDpi = 0;
                double imageResampleDpi = 0;
                double monoMaxDpi = 0;
                double monoResampleDpi = 0;
                int quality = 0;

                bool forceRecompression = false;

                if (CurrentState == State.Basic)
                {
                    // set preset high, balanced, small values
                    switch (FileSizeOption)
                    {
                        case BasicFileSize.High:
                            imageDownSettingMode = OptimizerImageSettingsDownsampleMode.e_off;
                            imageCompressionSettingMode = OptimizerImageSettingsCompressionMode.e_retain;
                            quality = 10;
                            monoDownSettingMode = OptimizerMonoImageSettingsDownsampleMode.e_off;
                            monoCompressionSettingMode = OptimizerMonoImageSettingsCompressionMode.e_jbig2;
                            forceRecompression = false;
                            break;
                        case BasicFileSize.Balanced:
                            imageDownSettingMode = OptimizerImageSettingsDownsampleMode.e_default;
                            imageMaxDpi = 225.0;
                            imageResampleDpi = 150.0;
                            imageCompressionSettingMode = OptimizerImageSettingsCompressionMode.e_jpeg;
                            quality = 8;
                            monoDownSettingMode = OptimizerMonoImageSettingsDownsampleMode.e_default;
                            monoMaxDpi = imageMaxDpi * 2.0;
                            monoResampleDpi = imageResampleDpi * 2.0;
                            monoCompressionSettingMode = OptimizerMonoImageSettingsCompressionMode.e_jbig2;
                            forceRecompression = true;
                            break;
                        case BasicFileSize.Small:
                            imageDownSettingMode = OptimizerImageSettingsDownsampleMode.e_default;
                            imageMaxDpi = 120.0;
                            imageResampleDpi = 96.0;
                            imageCompressionSettingMode = OptimizerImageSettingsCompressionMode.e_jpeg;
                            quality = 6;
                            monoDownSettingMode = OptimizerMonoImageSettingsDownsampleMode.e_default;
                            monoMaxDpi = imageMaxDpi * 2.0;
                            monoResampleDpi = imageResampleDpi * 2.0;
                            monoCompressionSettingMode = OptimizerMonoImageSettingsCompressionMode.e_jbig2;
                            forceRecompression = true;
                            break;
                    }
                }
                // otherwise, get the user selected values from advanced 
                else
                {
                    imageDownSettingMode = OptimizerImageSettingsDownsampleMode.e_default;
                    imageCompressionSettingMode = GetPdfNetSettingFromCompressionColor();
                    monoDownSettingMode = OptimizerMonoImageSettingsDownsampleMode.e_default;
                    monoCompressionSettingMode = GetPdfNetSettingFromCompressionMonochrome();
                    imageMaxDpi = GreaterThanDpiOption.Dpi;
                    imageResampleDpi = DownSampleDpiOption.Dpi;
                    quality = GetPdfNetSettingFromCompressionQuality();
                    forceRecompression = true;
                    monoMaxDpi = GreaterThanDpiOption.Dpi * 2.0;
                    monoResampleDpi = DownSampleDpiOption.Dpi * 2.0;
                }

                // apply the values
                imageSettings.SetDownsampleMode(imageDownSettingMode);
                imageSettings.SetImageDPI(imageMaxDpi, imageResampleDpi);
                imageSettings.SetCompressionMode(imageCompressionSettingMode);
                imageSettings.SetQuality(quality);
                imageSettings.ForceChanges(false);
                imageSettings.ForceRecompression(forceRecompression);

                monoSettings.SetDownsampleMode(monoDownSettingMode);
                monoSettings.SetImageDPI(monoMaxDpi, monoResampleDpi);
                monoSettings.SetCompressionMode(monoCompressionSettingMode);
                monoSettings.ForceChanges(false);
                monoSettings.ForceRecompression(forceRecompression);

                optimizerSettings.SetColorImageSettings(imageSettings);
                optimizerSettings.SetGrayscaleImageSettings(imageSettings);
                optimizerSettings.SetMonoImageSettings(monoSettings);

                Optimizer.Optimize(doc, optimizerSettings);
            }

            catch (Exception)
            {
                // log error
            }

            finally
            {
                if (shouldUnlock)
                {
                    doc.Unlock();
                    shouldUnlock = false;
                }
            }
        } 
    }
}

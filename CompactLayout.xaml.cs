using Microsoft.Maps.MapControl.WPF;
using RemoteDriving.ClientProxy_gRPC;
using RemoteDriving.Tools;
using RemoteDriving.UI.Viewmodel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RemoteDriving.UI.View
{
    /// <summary>
    /// Interaction logic for CompactLayout.xaml
    /// </summary>
    public partial class CompactLayout : Page
    {
        private Pushpin pinCar = new Pushpin();
        private MapLayer mapLayer = new MapLayer();
        private MapLayer carLayer = new MapLayer();
        private List<Location> route = new List<Location>();
        private Image image = new Image();
        private bool _isDebugMode = Parameters.Instance.GetBool("isDebugMode");

        public CompactLayout()
        {
            InitializeComponent();
            myMap.PreviewMouseWheel += (s, e) => e.Handled = true;
            ClientProxy.GpsUpdate += (sender, args) => GpsPositionUpdate(args.latitude, args.longitude);
            myMap.Children.Add(carLayer);
            myMap.Children.Add(mapLayer);

            pinCar.Background = new ImageBrush(new BitmapImage(new Uri("../../../Images/CarIcon.png", UriKind.Relative)));
            image.Source = new BitmapImage(new Uri("../../../Images/carPin.png", UriKind.Relative));
            image.Width = 10;
            image.Height = 10;
        }
        private void LogRowDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LogGrid.SelectedItem == null) return;
            var selectedRow = LogGrid.SelectedItem;
            var parentViewModel = (MainViewModel)this.DataContext;
            parentViewModel.OnLogDataRowDoubleClick(selectedRow);
            LogErrorDscrpt.Visibility = Visibility.Visible;
            LogErrorDscrpt.IsOpen = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
        private void LoadRoute(object sender, RoutedEventArgs e)
        {
            myMap.Cursor = Cursors.Arrow;
            Pushpin pa = (Pushpin)myMap.Children[0];

            myMap.Children.Clear();
            route.Clear();

            myMap.Children.Add(pa);

            string fileSelected = "";
            using (System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog())
            {
                openFileDialog1.InitialDirectory = "c:\\";
                openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.RestoreDirectory = true;

                if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        fileSelected = openFileDialog1.FileName;

                        using (StreamReader sr = File.OpenText(fileSelected))
                        {
                            string s = "";
                            while ((s = sr.ReadLine()) != null)
                            {
                                var array3 = s.Split(',');

                                string latitude = array3[0];
                                string longitude = array3[1];

                                Location l = new Location();
                                NumberFormatInfo provider = new NumberFormatInfo();
                                provider.NumberDecimalSeparator = ".";
                                provider.NumberGroupSeparator = ",";
                                l.Latitude = Convert.ToDouble(latitude, provider);
                                l.Longitude = Convert.ToDouble(longitude, provider);

                                Pushpin p = new Pushpin();
                                p.Location = l;
                                p.Background = new SolidColorBrush(Color.FromArgb(255, 255, 83, 114));
                                Image im = new Image();
                                im.Source = new BitmapImage(new Uri("../../../Images/RedDot.png", UriKind.Relative));
                                im.Width = 8;
                                im.Height = 8;
                                p.Content = route.Count;
                                if (route.Count > 1)
                                {
                                    addNewCarPolygon(route[route.Count - 2], route[route.Count - 1]);

                                }
                                mapLayer.AddChild(im, p.Location, PositionOrigin.Center);
                                route.Add(l);
                            }
                            myMap.Children.Add(mapLayer);
                            ((MainViewModel)this.DataContext).SendRoute(route);
                            sr.Close();
                        }
                    }
                    catch (Exception Ex)
                    {
                        System.Windows.Forms.MessageBox.Show("Wrong file format");
                    }
                }
            }
        }
        private void GpsPositionUpdate(double latitude, double longitude)
        {
            try
            {
            Location locGps = new Location(latitude, longitude);
            this.Dispatcher.Invoke(() =>
            {
                if (carLayer.Children.Count > 0)
                {
                    carLayer.Children.Clear();
                }
            });
            pinCar.Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    pinCar.Location = locGps;
                    //TO DO
                    carLayer.AddChild(image, pinCar.Location, PositionOrigin.Center);
                    myMap.SetView(pinCar.Location, myMap.ZoomLevel);
                }
                catch (Exception ex)
                {
                    HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), "", ex);
                }
            }));
            }
            catch (Exception ex)
            {
                HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(),"",ex);
            }
        }
        private void addNewCarPolygon(Location first, Location second)
        {
            MapPolyline polygon = new MapPolyline();
            polygon.Fill = new SolidColorBrush(Color.FromArgb(255, 114, 83, 255));
            polygon.Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            polygon.StrokeThickness = 4;

            polygon.Locations = new LocationCollection() { first, second };

            carLayer.Children.Add(polygon);
        }
        private void PlayRoute(object sender, RoutedEventArgs e)
        {
            if (_isDebugMode == true)
            {
                ((MainViewModel)this.DataContext).ActivateGpsService();
            }
        }
        private void StopRoute(object sender, RoutedEventArgs e)
        {

        }
    }
}

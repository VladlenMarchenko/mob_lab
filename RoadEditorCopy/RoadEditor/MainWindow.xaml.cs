using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace RoadEditor
{
    public enum TileType { Grass, GrassWithBush, RoadHorizontal, RoadVertical, Intersection, CrosswalkHorizontal, CrosswalkVertical }

    public partial class MainWindow : Window
    {
        private const int TileSize = 100;
        private readonly Dictionary<TileType, BitmapSource> _tileSet = new();
        private readonly Random _random = new();
        private TileType[,] _logicalMap = new TileType[26, 16];
        private int _mapWidth = 26, _mapHeight = 16;
        private double _zoom = 1;
        private TileType _currentTool = TileType.Grass;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTileSet();
            CreateEmptyMap();
        }

        private void InitializeTileSet()
        {
            _tileSet[TileType.Grass] = TileGenerator.CreateGrass();
            _tileSet[TileType.GrassWithBush] = TileGenerator.CreateGrassWithBush();
            _tileSet[TileType.RoadHorizontal] = TileGenerator.CreateRoadHorizontal();
            _tileSet[TileType.RoadVertical] = TileGenerator.CreateRoadVertical();
            _tileSet[TileType.Intersection] = TileGenerator.CreateIntersection();
            _tileSet[TileType.CrosswalkHorizontal] = TileGenerator.CreateCrosswalkHorizontal();
            _tileSet[TileType.CrosswalkVertical] = TileGenerator.CreateCrosswalkVertical();
        }

        private void GenerateMap_Click(object sender, RoutedEventArgs e) { GenerateRandomLogicalMap(); RenderCurrentMap(); }
        private void ClearMap_Click(object sender, RoutedEventArgs e) { CreateEmptyMap(); }

        private void SavePng_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new() { Filter = "PNG изображение (*.png)|*.png", FileName = "road-map.png", AddExtension = true };
            if (dialog.ShowDialog() != true) return;
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(RenderMapBitmap()));
            using FileStream stream = File.Create(dialog.FileName);
            encoder.Save(stream);
        }

        private void MapSizeSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (MapSizeSelector.SelectedItem is not System.Windows.Controls.ComboBoxItem item || item.Tag is not string tag) return;
            string[] values = tag.Split(',');
            if (values.Length != 2 || !int.TryParse(values[0], out int width) || !int.TryParse(values[1], out int height)) return;
            _mapWidth = width;
            _mapHeight = height;
            if (IsLoaded) CreateEmptyMap();
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _zoom = e.NewValue;
            if (ZoomLabel != null) ZoomLabel.Text = $"{_zoom:P0}";
            if (IsLoaded) UpdateDisplayedSize();
        }

        private void Tool_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton button && Enum.TryParse(button.Tag?.ToString(), out TileType tool)) _currentTool = tool;
        }

        private void MapDisplay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DrawTileAtMousePosition(e.GetPosition(MapDisplay));
        private void MapDisplay_MouseMove(object sender, MouseEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DrawTileAtMousePosition(e.GetPosition(MapDisplay)); }

        private void DrawTileAtMousePosition(Point position)
        {
            int x = (int)(position.X / (_zoom * TileSize));
            int y = (int)(position.Y / (_zoom * TileSize));
            if (x < 0 || x >= _mapWidth || y < 0 || y >= _mapHeight || _logicalMap[x, y] == _currentTool) return;
            _logicalMap[x, y] = _currentTool;
            RenderCurrentMap();
        }

        private void CreateEmptyMap()
        {
            _logicalMap = new TileType[_mapWidth, _mapHeight];
            for (int x = 0; x < _mapWidth; x++) for (int y = 0; y < _mapHeight; y++) _logicalMap[x, y] = TileType.Grass;
            RenderCurrentMap();
        }

        private void GenerateRandomLogicalMap()
        {
            for (int x = 0; x < _mapWidth; x++) for (int y = 0; y < _mapHeight; y++) _logicalMap[x, y] = TileType.Grass;
            List<int> rows = ChooseRoadCoordinates(_mapHeight, Math.Max(2, _mapHeight / 5));
            List<int> columns = ChooseRoadCoordinates(_mapWidth, Math.Max(2, _mapWidth / 6));
            foreach (int y in rows) for (int x = 0; x < _mapWidth; x++) _logicalMap[x, y] = TileType.RoadHorizontal;
            foreach (int x in columns) for (int y = 0; y < _mapHeight; y++) _logicalMap[x, y] = _logicalMap[x, y] == TileType.RoadHorizontal ? TileType.Intersection : TileType.RoadVertical;
            AddCrosswalks(rows, columns);
            AddNaturalDetails();
        }

        private List<int> ChooseRoadCoordinates(int length, int count)
        {
            List<int> result = new();
            int minimumDistance = length >= 16 ? 4 : 3;
            for (int attempts = 0; result.Count < count && attempts < 1000; attempts++)
            {
                int value = _random.Next(1, length - 1);
                bool valid = true;
                foreach (int existing in result) if (Math.Abs(existing - value) < minimumDistance) valid = false;
                if (valid) result.Add(value);
            }
            result.Sort();
            return result;
        }

        private void AddCrosswalks(List<int> rows, List<int> columns)
        {
            foreach (int x in columns) foreach (int y in rows)
            {
                if (_logicalMap[x, y] != TileType.Intersection) continue;
                if (x > 0 && _logicalMap[x - 1, y] == TileType.RoadHorizontal) _logicalMap[x - 1, y] = TileType.CrosswalkHorizontal;
                if (x < _mapWidth - 1 && _logicalMap[x + 1, y] == TileType.RoadHorizontal) _logicalMap[x + 1, y] = TileType.CrosswalkHorizontal;
                if (y > 0 && _logicalMap[x, y - 1] == TileType.RoadVertical) _logicalMap[x, y - 1] = TileType.CrosswalkVertical;
                if (y < _mapHeight - 1 && _logicalMap[x, y + 1] == TileType.RoadVertical) _logicalMap[x, y + 1] = TileType.CrosswalkVertical;
            }
        }

        private void AddNaturalDetails()
        {
            for (int x = 0; x < _mapWidth; x++) for (int y = 0; y < _mapHeight; y++) if (_logicalMap[x, y] == TileType.Grass && _random.NextDouble() < 0.06) _logicalMap[x, y] = TileType.GrassWithBush;
        }

        private void RenderCurrentMap()
        {
            MapDisplay.Source = RenderMapBitmap();
            MapDisplay.Width = _mapWidth * TileSize * _zoom;
            MapDisplay.Height = _mapHeight * TileSize * _zoom;
        }

        private BitmapSource RenderMapBitmap()
        {
            RenderTargetBitmap bitmap = new(_mapWidth * TileSize, _mapHeight * TileSize, 96, 96, PixelFormats.Pbgra32);
            DrawingVisual visual = new();
            using (DrawingContext context = visual.RenderOpen())
                for (int x = 0; x < _mapWidth; x++) for (int y = 0; y < _mapHeight; y++) context.DrawImage(_tileSet[_logicalMap[x, y]], new Rect(x * TileSize, y * TileSize, TileSize, TileSize));
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }

        private void UpdateDisplayedSize()
        {
            MapDisplay.Width = _mapWidth * TileSize * _zoom;
            MapDisplay.Height = _mapHeight * TileSize * _zoom;
        }
    }
}

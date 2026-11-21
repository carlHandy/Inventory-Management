using InventoryManager.Models;
using Microsoft.Maui.Controls.Shapes;

namespace InventoryManager.Controls;

/// <summary>
/// Small rounded status chip with a coloured dot, e.g. "● In stock". The colours follow
/// <see cref="Level"/> (green / amber / red) and the light or dark theme.
/// </summary>
public class StatusPill : Border
{
    public static readonly BindableProperty LevelProperty = BindableProperty.Create(
        nameof(Level), typeof(StockLevel), typeof(StatusPill), StockLevel.InStock,
        propertyChanged: (bindable, _, _) => ((StatusPill)bindable).ApplyColors());

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(StatusPill), string.Empty,
        propertyChanged: (bindable, _, value) => ((StatusPill)bindable)._label.Text = (string)value);

    private readonly Label _label;
    private readonly BoxView _dot;

    public StatusPill()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 10 };
        Padding = new Thickness(8, 3);
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;

        _dot = new BoxView
        {
            WidthRequest = 6,
            HeightRequest = 6,
            CornerRadius = 3,
            VerticalOptions = LayoutOptions.Center
        };
        _label = new Label
        {
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center
        };

        Content = new HorizontalStackLayout { Spacing = 5, Children = { _dot, _label } };
        ApplyColors();
    }

    public StockLevel Level
    {
        get => (StockLevel)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private void ApplyColors()
    {
        var (fg, fgDark, bg, bgDark) = Level switch
        {
            StockLevel.OutOfStock => ("Danger", "DangerDark", "DangerSoftLight", "DangerSoftDark"),
            StockLevel.Low => ("Warning", "WarningDark", "WarningSoftLight", "WarningSoftDark"),
            _ => ("Success", "SuccessDark", "SuccessSoftLight", "SuccessSoftDark")
        };

        this.SetAppThemeColor(BackgroundColorProperty, Resource(bg), Resource(bgDark));
        _label.SetAppThemeColor(Label.TextColorProperty, Resource(fg), Resource(fgDark));
        _dot.SetAppThemeColor(BoxView.ColorProperty, Resource(fg), Resource(fgDark));
    }

    /// <summary>Looks up a colour defined in Resources/Styles/Colors.xaml.</summary>
    private static Color Resource(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Colors.Gray;
}

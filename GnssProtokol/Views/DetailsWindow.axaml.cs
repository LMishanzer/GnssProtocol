using Avalonia.Controls;
using Avalonia.Interactivity;
using GisProtocolLib.Models;

namespace PointAverager.Views;

public partial class DetailsWindow : Window
{
    public DetailsWindow(Details details)
    {
        InitializeComponent();

        SetFields(details);
    }
    
    public void ChangeAccuracy(object sender, SelectionChangedEventArgs e)
    {
        if (PrecisionInput == null)
            return;

        var item = e.AddedItems[0] as ComboBoxItem;

        if (item?.Content?.ToString() == "Lokální")
        {
            PrecisionInput.Minimum = 2;
            PrecisionInput.Maximum = 3;
            PrecisionInput.Value = 2;

            return;
        }

        PrecisionInput.Minimum = 7;
        PrecisionInput.Maximum = 10;
        PrecisionInput.Value = 9;
    }

    private void SetFields(Details details)
    {
        Sensor.Text = details.Sensor; 
        TransSoft.Text = details.TransSoft; 
        PolSoft.Text = details.PolSoft; 
        Projection.Text = details.Projection; 
        GeoModel.Text = details.GeoModel; 
        RealizationFrom.Text = details.RealizationFrom;
        Zhotovitel.Text = details.Zhotovitel; 
        Zpracoval.Text = details.Zpracoval; 
        Prijemace.Text = details.Prijemace; 
        Vyrobce.Text = details.Vyrobce; 
        Typ.Text = details.Typ; 
        Cislo.Text = details.Cislo; 
        Anteny.Text = details.Anteny; 
        PristupovyBod.Text = details.PristupovyBod; 
        IntervalZaznamu.Text = details.IntervalZaznamu; 
        ElevacniMaska.Text = details.ElevacniMaska; 
        VyskaAnteny.Text = details.VyskaAnteny; 
        PocetZameneniBodu.Text = details.PocetZameneniBodu; 
        ZpracovatelskyProgram.Text = details.ZpracovatelskyProgram; 
        SouradniceNepripojeny.Text = details.SouradniceNepripojeny; 
        KontrolaPripojeni.Text = details.KontrolaPripojeni; 
        TransformacniPostup.Text = details.TransformacniPostup; 
        TransformaceZpracovatelskyProgram.Text = details.TransformaceZpracovatelskyProgram; 
        PrecisionInput.Value = details.PrecisionInput;
        CoordinatesType.SelectedIndex = details.CoordinatesTypeIndex ?? 0;
        PouzitaStanice.SelectedIndex = details.PouzitaStaniceIndex ?? 0;
    }

    private void OnOkButtonClick(object? _1, RoutedEventArgs _2)
    {
        var ret = new Details
        {
            Sensor = Sensor.Text ?? string.Empty,
            TransSoft = TransSoft.Text ?? string.Empty,
            PolSoft = PolSoft.Text ?? string.Empty,
            Projection = Projection.Text ?? string.Empty,
            GeoModel = GeoModel.Text ?? string.Empty,
            RealizationFrom = RealizationFrom.Text ?? string.Empty,
            Zhotovitel = Zhotovitel.Text ?? string.Empty,
            Zpracoval = Zpracoval.Text ?? string.Empty,
            Prijemace = Prijemace.Text ?? string.Empty,
            Vyrobce = Vyrobce.Text ?? string.Empty,
            Typ = Typ.Text ?? string.Empty,
            Cislo = Cislo.Text ?? string.Empty,
            Anteny = Anteny.Text ?? string.Empty,
            PristupovyBod = PristupovyBod.Text ?? string.Empty,
            IntervalZaznamu = IntervalZaznamu.Text ?? string.Empty,
            ElevacniMaska = ElevacniMaska.Text ?? string.Empty,
            VyskaAnteny = VyskaAnteny.Text ?? string.Empty,
            PocetZameneniBodu = PocetZameneniBodu.Text ?? string.Empty,
            ZpracovatelskyProgram = ZpracovatelskyProgram.Text ?? string.Empty,
            SouradniceNepripojeny = SouradniceNepripojeny.Text ?? string.Empty,
            KontrolaPripojeni = KontrolaPripojeni.Text ?? string.Empty,
            TransformacniPostup = TransformacniPostup.Text ?? string.Empty,
            TransformaceZpracovatelskyProgram = TransformaceZpracovatelskyProgram.Text ?? string.Empty,
            PrecisionInput = (int?) PrecisionInput.Value,
            CoordinatesTypeIndex = CoordinatesType.SelectedIndex,
            CoordinatesType = (CoordinatesType.SelectionBoxItem as ComboBoxItem)?.Content?.ToString(),
            PouzitaStaniceIndex = PouzitaStanice.SelectedIndex,
            PouzitaStanice = (PouzitaStanice.SelectionBoxItem as ComboBoxItem)?.Content?.ToString()
        };
        
        Close(ret);
    }
}
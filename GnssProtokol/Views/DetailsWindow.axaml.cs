using Avalonia.Controls;
using Avalonia.Interactivity;
using GisProtocolLib.Models;

namespace GnssProtokol.Views;

public partial class DetailsWindow : Window
{
    public DetailsWindow() : this(new FormDetails()) { }
    
    public DetailsWindow(FormDetails formDetails)
    {
        InitializeComponent();

        SetFields(formDetails);
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

    private void SetFields(FormDetails formDetails)
    {
        Sensor.Text = formDetails.Sensor; 
        TransSoft.Text = formDetails.TransSoft; 
        PolSoft.Text = formDetails.PolSoft; 
        Projection.Text = formDetails.Projection; 
        GeoModel.Text = formDetails.GeoModel; 
        RealizationFrom.Text = formDetails.RealizationFrom;
        Zhotovitel.Text = formDetails.Zhotovitel; 
        Zpracoval.Text = formDetails.Zpracoval; 
        Prijemace.Text = formDetails.Prijemace; 
        Vyrobce.Text = formDetails.Vyrobce; 
        Typ.Text = formDetails.Typ; 
        Cislo.Text = formDetails.Cislo; 
        Anteny.Text = formDetails.Anteny; 
        PristupovyBod.Text = formDetails.PristupovyBod; 
        IntervalZaznamu.Text = formDetails.IntervalZaznamu; 
        ElevacniMaska.Text = formDetails.ElevacniMaska; 
        VyskaAnteny.Text = formDetails.VyskaAnteny; 
        PocetZameneniBodu.Text = formDetails.PocetZameneniBodu; 
        ZpracovatelskyProgram.Text = formDetails.ZpracovatelskyProgram; 
        SouradniceNepripojeny.Text = formDetails.SouradniceNepripojeny; 
        KontrolaPripojeni.Text = formDetails.KontrolaPripojeni; 
        TransformacniPostup.Text = formDetails.TransformacniPostup; 
        TransformaceZpracovatelskyProgram.Text = formDetails.TransformaceZpracovatelskyProgram; 
        PrecisionInput.Value = formDetails.PrecisionInput;
        CoordinatesType.SelectedIndex = formDetails.CoordinatesTypeIndex ?? 0;
        PouzitaStanice.SelectedIndex = formDetails.PouzitaStaniceIndex ?? 0;
    }

    private void OnOkButtonClick(object? _1, RoutedEventArgs _2)
    {
        var ret = new FormDetails
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
            PouzitaStanice = (PouzitaStanice.SelectionBoxItem as ComboBoxItem)?.Content?.ToString() ?? PouzitaStanice.SelectionBoxItem?.ToString() 
        };
        
        Close(ret);
    }
}
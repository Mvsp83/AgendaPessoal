using MudBlazor;

namespace AgendaPessoal;

/// <summary>
/// Identidade visual: preto profundo com amarelo (tema principal, escuro)
/// e variante clara com os mesmos acentos para quem usa o sistema no modo claro.
/// </summary>
public static class TemaDoApp
{
    public static readonly MudTheme Tema = new()
    {
        PaletteDark = new PaletteDark
        {
            Black = "#0A0A0A",
            Background = "#0A0A0A",
            BackgroundGray = "#101010",
            Surface = "#161616",
            DrawerBackground = "#101010",
            DrawerText = "#EDEDED",
            DrawerIcon = "#B8B8B8",
            AppbarBackground = "#141414",
            AppbarText = "#EDEDED",
            TextPrimary = "#EDEDED",
            TextSecondary = "#A8A8A8",
            TextDisabled = "#5C5C5C",
            Primary = "#FFC107",
            PrimaryContrastText = "#1A1300",
            Secondary = "#FFD54F",
            SecondaryContrastText = "#1A1300",
            Tertiary = "#FFE082",
            Info = "#64B5F6",
            Success = "#81C784",
            Warning = "#FFB74D",
            Error = "#E57373",
            LinesDefault = "#262626",
            TableLines = "#262626",
            Divider = "#262626",
            ActionDefault = "#B8B8B8",
            ActionDisabled = "#4A4A4A",
            HoverOpacity = 0.08
        },
        PaletteLight = new PaletteLight
        {
            Background = "#FAFAF7",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            AppbarBackground = "#1C1C1C",
            AppbarText = "#F5F5F5",
            Primary = "#F5A800",
            PrimaryContrastText = "#231A00",
            Secondary = "#FFB300",
            SecondaryContrastText = "#231A00",
            Info = "#1E88E5",
            Success = "#43A047",
            Warning = "#FB8C00",
            Error = "#E53935"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px"
        }
    };
}

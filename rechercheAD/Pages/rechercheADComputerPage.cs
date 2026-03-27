// RechercheAD - Page de recherche des ordinateurs Active Directory
// Auteur: Kylian

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace rechercheAD;

/// <summary>
/// Page de recherche AD dynamique pour les ordinateurs.
/// </summary>
internal sealed partial class rechercheADComputerPage : DynamicListPage
{
    public rechercheADComputerPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Recherche Ordinateurs AD";
        Name = "Ouvrir";
        PlaceholderText = "Rechercher un ordinateur (nom, DNS)...";
        ShowDetails = true;
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        RaiseItemsChanged(0);
    }

    public override IListItem[] GetItems()
    {
        try
        {
            string search = SearchText ?? string.Empty;

            if (search.Length < 2)
            {
                return
                [
                    new ListItem(new NoOpCommand())
                    {
                        Title = "Tapez au moins 2 caractères pour rechercher...",
                        Subtitle = "Ex : PC-KYLIAN, SRV-DC01",
                    },
                ];
            }

#pragma warning disable IL2026
            List<ADComputer> computers = ADSearcher.SearchComputers(search);
#pragma warning restore IL2026

            if (computers.Count == 0)
            {
                return
                [
                    new ListItem(new NoOpCommand())
                    {
                        Title = "Aucun ordinateur trouvé.",
                        Subtitle = $"Aucun résultat pour « {search} »",
                    },
                ];
            }

            var items = new List<IListItem>(computers.Count);

            foreach (ADComputer computer in computers)
            {
                if (computer.Name == "error")
                {
                    items.Add(new ListItem(new NoOpCommand())
                    {
                        Title = "Active Directory inaccessible",
                        Subtitle = computer.Description,
                    });
                    continue;
                }

                // Sous-titre : OS | DNS | OU
                var parts = new List<string>(3);
                if (!string.IsNullOrEmpty(computer.OperatingSystem)) parts.Add(computer.OperatingSystem);
                if (!string.IsNullOrEmpty(computer.DnsHostName)) parts.Add(computer.DnsHostName);
                if (!string.IsNullOrEmpty(computer.Location)) parts.Add(computer.Location);
                string subtitle = string.Join("  |  ", parts);

                // Panneau de détails
                var md = new StringBuilder();
                md.AppendLine(CultureInfo.CurrentCulture, $"## {computer.Name}");
                md.AppendLine();
                md.AppendLine("### Informations");
                if (!string.IsNullOrEmpty(computer.DnsHostName))
                    md.AppendLine(CultureInfo.CurrentCulture, $"- **DNS :** {computer.DnsHostName}");
                if (!string.IsNullOrEmpty(computer.OperatingSystem))
                    md.AppendLine(CultureInfo.CurrentCulture, $"- **OS :** {computer.OperatingSystem}");
                if (!string.IsNullOrEmpty(computer.OperatingSystemVersion))
                    md.AppendLine(CultureInfo.CurrentCulture, $"- **Version :** {computer.OperatingSystemVersion}");
                if (!string.IsNullOrEmpty(computer.Description))
                    md.AppendLine(CultureInfo.CurrentCulture, $"- **Description :** {computer.Description}");
                if (!string.IsNullOrEmpty(computer.Location))
                    md.AppendLine(CultureInfo.CurrentCulture, $"- **OU :** {computer.Location}");

                items.Add(new ListItem(new CopyTextCommand(computer.Name))
                {
                    Title = computer.Name,
                    Subtitle = subtitle,
                    Details = new Details
                    {
                        Body = md.ToString(),
                    },
                });
            }

            return [.. items];
        }
        catch (Exception ex)
        {
            return
            [
                new ListItem(new NoOpCommand())
                {
                    Title = "Erreur dans l'extension",
                    Subtitle = ex.Message,
                },
            ];
        }
    }
}

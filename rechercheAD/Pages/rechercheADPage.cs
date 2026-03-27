// RechercheAD - Page de recherche unifiée Active Directory (utilisateurs + ordinateurs)
// Auteur: Kylian

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace rechercheAD;

/// <summary>
/// Page de recherche AD unifiée — cherche utilisateurs ET ordinateurs en même temps.
/// </summary>
internal sealed partial class rechercheADPage : DynamicListPage
{
    public rechercheADPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Recherche Active Directory";
        Name = "Ouvrir";
        PlaceholderText = "Rechercher un utilisateur ou un ordinateur...";
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
                        Subtitle = "Recherche sur les utilisateurs et les ordinateurs AD",
                    },
                ];
            }

#pragma warning disable IL2026
            List<ADUser> users = ADSearcher.SearchUsers(search);
            List<ADComputer> computers = ADSearcher.SearchComputers(search);
#pragma warning restore IL2026

            var items = new List<IListItem>();

            // ── Utilisateurs ──────────────────────────────────────────
            foreach (ADUser user in users)
            {
                if (user.SamAccountName == "error")
                {
                    items.Add(new ListItem(new NoOpCommand())
                    {
                        Title = "Active Directory inaccessible",
                        Subtitle = user.DisplayName,
                    });
                    continue;
                }

                var parts = new List<string>(4);
                if (!string.IsNullOrEmpty(user.SamAccountName)) parts.Add(user.SamAccountName);
                if (!string.IsNullOrEmpty(user.Title)) parts.Add(user.Title);
                if (!string.IsNullOrEmpty(user.Department)) parts.Add(user.Department);
                if (!string.IsNullOrEmpty(user.Email)) parts.Add(user.Email);
                string subtitle = "Utilisateur  |  " + string.Join("  |  ", parts);

                string displayName = string.IsNullOrEmpty(user.DisplayName)
                    ? user.SamAccountName
                    : user.DisplayName;

                var md = new StringBuilder();
                md.AppendLine(CultureInfo.CurrentCulture, $"## {displayName}");
                md.AppendLine();
                md.AppendLine("### Informations");
                if (!string.IsNullOrEmpty(user.SamAccountName))
                    md.AppendLine(CultureInfo.CurrentCulture, $"- **Login :** {user.SamAccountName}");
                if (!string.IsNullOrEmpty(user.Email))
                    md.AppendLine(CultureInfo.CurrentCulture, $"- **Email :** {user.Email}");
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                    md.AppendLine(CultureInfo.CurrentCulture, $"- **Telephone :** {user.PhoneNumber}");
                if (!string.IsNullOrEmpty(user.Title))
                    md.AppendLine(CultureInfo.CurrentCulture, $"- **Poste :** {user.Title}");
                if (!string.IsNullOrEmpty(user.Department))
                    md.AppendLine(CultureInfo.CurrentCulture, $"- **Service :** {user.Department}");
                if (!string.IsNullOrEmpty(user.Office))
                    md.AppendLine(CultureInfo.CurrentCulture, $"- **Bureau :** {user.Office}");
                if (!string.IsNullOrEmpty(user.Manager))
                    md.AppendLine(CultureInfo.CurrentCulture, $"- **Manager :** {user.Manager}");

                if (user.Groups.Count > 0)
                {
                    md.AppendLine();
                    md.AppendLine(CultureInfo.CurrentCulture, $"### Groupes ({user.Groups.Count})");
                    foreach (string group in user.Groups)
                        md.AppendLine(CultureInfo.CurrentCulture, $"- {group}");
                }
                else
                {
                    md.AppendLine();
                    md.AppendLine("### Groupes");
                    md.AppendLine("*Aucun groupe*");
                }

                items.Add(new ListItem(new UserDetailPage(user))
                {
                    Title = displayName,
                    Subtitle = subtitle,
                    Details = new Details { Body = md.ToString() },
                });
            }

            // ── Ordinateurs ───────────────────────────────────────────
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

                var parts = new List<string>(3);
                if (!string.IsNullOrEmpty(computer.OperatingSystem)) parts.Add(computer.OperatingSystem);
                if (!string.IsNullOrEmpty(computer.DnsHostName)) parts.Add(computer.DnsHostName);
                if (!string.IsNullOrEmpty(computer.Location)) parts.Add(computer.Location);
                string subtitle = "Ordinateur  |  " + string.Join("  |  ", parts);

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
                    Details = new Details { Body = md.ToString() },
                });
            }

            if (items.Count == 0)
            {
                return
                [
                    new ListItem(new NoOpCommand())
                    {
                        Title = "Aucun résultat trouvé.",
                        Subtitle = $"Aucun utilisateur ni ordinateur pour « {search} »",
                    },
                ];
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

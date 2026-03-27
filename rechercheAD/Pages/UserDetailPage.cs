// RechercheAD - Page de détail d'un utilisateur AD
// Auteur: Kylian

using System.Collections.Generic;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace rechercheAD;

/// <summary>
/// Page affichant le détail complet d'un utilisateur AD.
/// Chaque champ est cliquable pour être copié dans le presse-papier.
/// </summary>
internal sealed partial class UserDetailPage : ListPage
{
    private readonly ADUser _user;

    public UserDetailPage(ADUser user)
    {
        _user = user;
        Title = string.IsNullOrEmpty(user.DisplayName) ? user.SamAccountName : user.DisplayName;
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        PlaceholderText = string.Empty;
    }

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>();

        // --- Informations de contact ---
        AddItem(items, _user.SamAccountName, "Login");
        AddItem(items, _user.Email, "Email");
        AddItem(items, _user.PhoneNumber, "Telephone");
        AddItem(items, _user.Title, "Poste");
        AddItem(items, _user.Department, "Service");
        AddItem(items, _user.Office, "Bureau");
        AddItem(items, _user.Manager, "Manager");

        // --- Groupes ---
        if (_user.Groups.Count > 0)
        {
            foreach (string group in _user.Groups)
            {
                items.Add(new ListItem(new CopyTextCommand(group))
                {
                    Title = group,
                    Subtitle = "Groupe",
                });
            }
        }
        else
        {
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = "Aucun groupe",
                Subtitle = "Groupes",
            });
        }

        return [.. items];
    }

    private static void AddItem(List<IListItem> items, string value, string label)
    {
        if (string.IsNullOrEmpty(value)) return;

        items.Add(new ListItem(new CopyTextCommand(value))
        {
            Title = value,
            Subtitle = label,
        });
    }
}

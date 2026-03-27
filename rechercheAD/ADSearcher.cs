// RechercheAD - Module de recherche Active Directory
// Auteur: Kylian

using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace rechercheAD;

/// <summary>
/// Représente un utilisateur Active Directory.
/// </summary>
public sealed class ADUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string SamAccountName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Office { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public List<string> Groups { get; set; } = [];
}

/// <summary>
/// Représente un ordinateur Active Directory.
/// </summary>
public sealed class ADComputer
{
    public string Name { get; set; } = string.Empty;
    public string DnsHostName { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string OperatingSystemVersion { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Fournit les méthodes de recherche dans l'Active Directory via LDAP.
/// </summary>
public static class ADSearcher
{
    private const int MaxResults = 25;

    // Tableau static readonly pour éviter l'avertissement CA1861
    private static readonly string[] LdapProperties =
    [
        "displayName",
        "sAMAccountName",
        "mail",
        "department",
        "title",
        "telephoneNumber",
        "physicalDeliveryOfficeName",
        "manager",
        "memberOf",
    ];

    /// <summary>
    /// Recherche des utilisateurs dans l'AD selon un terme de recherche.
    /// Effectue une recherche sur : nom affiché, login, email, prénom et nom de famille.
    /// </summary>
    public static List<ADUser> SearchUsers(string searchTerm)
    {
        var users = new List<ADUser>();

        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            return users;

        // Nettoyer le terme pour éviter l'injection LDAP
        string safe = EscapeLdap(searchTerm);

        try
        {
            // Connexion automatique au domaine courant via RootDSE
            using var rootEntry = new DirectoryEntry("LDAP://RootDSE");
            var namingContext = rootEntry.Properties["defaultNamingContext"][0]?.ToString();

            if (string.IsNullOrEmpty(namingContext))
                return users;

            using var domainEntry = new DirectoryEntry($"LDAP://{namingContext}");
            using var searcher = new DirectorySearcher(domainEntry)
            {
                // Filtre : utilisateurs actifs correspondant au terme de recherche
                Filter = $"(&(objectClass=user)(objectCategory=person)" +
                         $"(!(userAccountControl:1.2.840.113556.1.4.803:=2))" +
                         $"(|(displayName=*{safe}*)(sAMAccountName=*{safe}*)" +
                         $"(mail=*{safe}*)(givenName=*{safe}*)(sn=*{safe}*)))",
                SizeLimit = MaxResults,
                PageSize = MaxResults,
            };

            searcher.PropertiesToLoad.AddRange(LdapProperties);

            SearchResultCollection results = searcher.FindAll();

            foreach (SearchResult result in results)
            {
                string manager = GetProperty(result, "manager");

                // Le manager est un DN complet, on extrait juste le CN
                if (!string.IsNullOrEmpty(manager) && manager.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                {
                    int comma = manager.IndexOf(',');
                    manager = comma > 3 ? manager[3..comma] : manager[3..];
                }

                // Extraire les groupes depuis memberOf (liste de DNs)
                var groups = new List<string>();
                var memberOfProps = result.Properties["memberOf"];
                if (memberOfProps != null)
                {
                    foreach (object? dn in memberOfProps)
                    {
                        string dnStr = dn?.ToString() ?? string.Empty;
                        if (string.IsNullOrEmpty(dnStr)) continue;

                        // Extraire le CN du DN : "CN=GroupName,OU=..." → "GroupName"
                        if (dnStr.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                        {
                            int comma = dnStr.IndexOf(',');
                            string groupName = comma > 3 ? dnStr[3..comma] : dnStr[3..];
                            groups.Add(groupName);
                        }
                    }

                    groups.Sort(StringComparer.OrdinalIgnoreCase);
                }

                users.Add(new ADUser
                {
                    DisplayName = GetProperty(result, "displayName"),
                    SamAccountName = GetProperty(result, "sAMAccountName"),
                    Email = GetProperty(result, "mail"),
                    Department = GetProperty(result, "department"),
                    Title = GetProperty(result, "title"),
                    PhoneNumber = GetProperty(result, "telephoneNumber"),
                    Office = GetProperty(result, "physicalDeliveryOfficeName"),
                    Manager = manager,
                    Groups = groups,
                });
            }
        }
        catch (Exception ex)
        {
            // L'AD est inaccessible (machine hors domaine, VPN requis, etc.)
            // On retourne une entrée d'erreur pour informer l'utilisateur
            users.Add(new ADUser
            {
                DisplayName = $"Erreur de connexion AD : {ex.Message}",
                SamAccountName = "error",
            });
        }

        return users;
    }

    private static readonly string[] ComputerLdapProperties =
    [
        "cn",
        "dNSHostName",
        "operatingSystem",
        "operatingSystemVersion",
        "description",
        "distinguishedName",
    ];

    /// <summary>
    /// Recherche des ordinateurs dans l'AD selon un terme de recherche.
    /// </summary>
    public static List<ADComputer> SearchComputers(string searchTerm)
    {
        var computers = new List<ADComputer>();

        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            return computers;

        string safe = EscapeLdap(searchTerm);

        try
        {
            using var rootEntry = new DirectoryEntry("LDAP://RootDSE");
            var namingContext = rootEntry.Properties["defaultNamingContext"][0]?.ToString();

            if (string.IsNullOrEmpty(namingContext))
                return computers;

            using var domainEntry = new DirectoryEntry($"LDAP://{namingContext}");
            using var searcher = new DirectorySearcher(domainEntry)
            {
                Filter = $"(&(objectClass=computer)(|(cn=*{safe}*)(dNSHostName=*{safe}*)))",
                SizeLimit = MaxResults,
                PageSize = MaxResults,
            };

            searcher.PropertiesToLoad.AddRange(ComputerLdapProperties);

            SearchResultCollection results = searcher.FindAll();

            foreach (SearchResult result in results)
            {
                // Extraire le chemin OU depuis le distinguishedName
                string dn = GetProperty(result, "distinguishedName");
                string location = ExtractOuPath(dn);

                computers.Add(new ADComputer
                {
                    Name = GetProperty(result, "cn"),
                    DnsHostName = GetProperty(result, "dNSHostName"),
                    OperatingSystem = GetProperty(result, "operatingSystem"),
                    OperatingSystemVersion = GetProperty(result, "operatingSystemVersion"),
                    Description = GetProperty(result, "description"),
                    Location = location,
                });
            }
        }
        catch (Exception ex)
        {
            computers.Add(new ADComputer
            {
                Name = "error",
                Description = $"Erreur de connexion AD : {ex.Message}",
            });
        }

        return computers;
    }

    /// <summary>
    /// Extrait le chemin OU depuis un distinguishedName LDAP.
    /// Ex: "CN=PC01,OU=Informatique,OU=Paris,DC=corp,DC=fr" → "Informatique > Paris"
    /// </summary>
    private static string ExtractOuPath(string dn)
    {
        if (string.IsNullOrEmpty(dn)) return string.Empty;
        var parts = dn.Split(',');
        var ous = new List<string>();
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (trimmed.StartsWith("OU=", StringComparison.OrdinalIgnoreCase))
                ous.Add(trimmed[3..]);
        }
        return string.Join(" > ", ous);
    }

    private static string GetProperty(SearchResult result, string propertyName)
    {
        var props = result.Properties[propertyName];
        return props.Count > 0 ? props[0]?.ToString() ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// Échappe les caractères spéciaux LDAP pour prévenir l'injection.
    /// </summary>
    private static string EscapeLdap(string input)
    {
        return input
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00");
    }
}

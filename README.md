# AD Power Search — PowerToys Extension

<p align="center">
  <img src="rechercheAD/Assets/Wide310x150Logo.scale-200.png" alt="AD Power Search logo" width="400"/>
</p>

> A **PowerToys Command Palette** extension to search Active Directory users and computers instantly from your keyboard.

---

## What it does

**AD Power Search** lets you search your company's Active Directory directly from the PowerToys Command Palette (Win + Space).
Type a name, a login, an email address, or a computer name — results appear instantly as you type.

### Users
- Search by display name, login (sAMAccountName), email, first name, or last name
- Side panel shows: login, email, phone, job title, department, office, manager, and group memberships
- Press **Enter** to open a detail page where every field can be clicked to copy it

### Computers
- Search by computer name (CN) or DNS hostname
- Side panel shows: DNS name, operating system, OS version, description, and OU path

### Unified search
A single search box returns **both users and computers** at the same time.
Results are clearly labelled `Utilisateur` or `Ordinateur` in the subtitle.

---

## Screenshots

| Unified search | User detail panel | User detail page |
|---|---|---|
| *Search for "dupont" shows Jean Dupont (user) and PC-DUPONT (computer)* | *Side panel with all AD attributes* | *Full page with every field copyable* |

---

## Requirements

| Requirement | Version |
|---|---|
| Windows | 10 / 11 (x64) |
| PowerToys | 0.90 or later |
| .NET | 9.0 (included in build) |
| Windows SDK | 10.0.26100.0 |
| Active Directory | Domain-joined machine or VPN access to your domain |

---

## Installation

### Option 1 — Deploy from source (debug)

1. **Clone the repository**
   ```powershell
   git clone https://github.com/kylianvillard/ADPowerSearch.git
   cd ADPowerSearch
   ```

2. **Open `rechercheAD.sln`** in Visual Studio 2022

3. **Restore NuGet packages** (automatic on first build)

4. **Build** the solution: `Ctrl+Shift+B`

5. **Kill any running instance** (required before each redeploy):
   ```powershell
   Stop-Process -Name "rechercheAD" -Force -ErrorAction SilentlyContinue
   ```

6. **Deploy** the MSIX package:
   ```powershell
   Add-AppxPackage -Register `
     "rechercheAD\bin\x64\Debug\net9.0-windows10.0.26100.0\win-x64\AppxManifest.xml" `
     -ForceUpdateFromAnyVersion
   ```

7. **Open PowerToys** → Command Palette (`Win + Space`) → search **"Recherche Active Directory"**

---

## Usage

1. Open the Command Palette with `Win + Space`
2. Type **"Recherche"** or **"AD"** to find the extension
3. Press **Enter** to open it
4. Start typing — at least 2 characters — to search
5. **Arrow keys** to navigate results
6. **Hover** a result to see the details panel on the right
7. **Enter** on a user → opens the full detail page (every field is clickable to copy)
8. **Enter** on a computer → copies the computer name to clipboard

---

## Project structure

```
ADPowerSearch/
├── rechercheAD/
│   ├── Assets/                        # MSIX icons (all sizes)
│   ├── Pages/
│   │   ├── rechercheADPage.cs         # Main unified search page (users + computers)
│   │   ├── UserDetailPage.cs          # User detail page (opened on Enter)
│   │   └── rechercheADComputerPage.cs # Standalone computer search page
│   ├── ADSearcher.cs                  # LDAP queries (users + computers)
│   ├── rechercheAD.cs                 # COM server entry point
│   ├── rechercheADCommandsProvider.cs # Top-level command registration
│   └── rechercheAD.csproj
├── Directory.Packages.props           # Centralized NuGet versions
├── rechercheAD.sln
└── README.md
```

---

## Security

- **LDAP injection prevention** — all search terms are escaped before being used in LDAP filters (`\2a`, `\28`, `\29`, `\5c`, `\00`)
- **Read-only** — the extension only performs read queries on Active Directory; it never modifies any object
- **No credentials stored** — authentication uses the current Windows session (Kerberos / NTLM); no passwords are saved or transmitted
- **Local only** — the extension runs as a sandboxed COM server on the local machine; no data is sent externally
- **Domain-aware** — the domain is discovered automatically via `LDAP://RootDSE`; no manual configuration needed

---

## How it works

The extension uses `System.DirectoryServices` to perform LDAP queries against the domain controller.

**User search filter:**
```ldap
(&(objectClass=user)(objectCategory=person)
  (!(userAccountControl:1.2.840.113556.1.4.803:=2))
  (|(displayName=*term*)(sAMAccountName=*term*)(mail=*term*)(givenName=*term*)(sn=*term*)))
```

**Computer search filter:**
```ldap
(&(objectClass=computer)(|(cn=*term*)(dNSHostName=*term*)))
```

The extension is built on the **Microsoft.CommandPalette.Extensions** SDK (v0.9) and runs as a registered WinRT COM server that PowerToys discovers automatically.

---

## Development

Built with:
- **C# / .NET 9**
- **Microsoft.CommandPalette.Extensions.Toolkit** v0.9.260303001
- **System.DirectoryServices** v9.0.4
- **MSIX packaging** (single-project, `EnableMsixTooling=true`)

---

## Author

**Kylian Villard** — IT Infrastructure
Extension developed for internal use to speed up day-to-day AD lookups.

---

## License

MIT License — free to use, modify, and distribute.

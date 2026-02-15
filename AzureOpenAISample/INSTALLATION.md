# Getting Started - Installation Guide

## ⚠️ .NET 8 SDK Required

This project uses **.NET 8** (the latest Long-Term Support release). If you see the error `NETSDK1045`, you need to install the .NET 8 SDK.

---

## 🚀 Install .NET 8 SDK

### Option 1: Direct Download (Recommended)

**macOS:**
1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0
2. Download ".NET SDK 8.0.x" for macOS
3. Run the installer
4. Verify installation:
   ```bash
   dotnet --list-sdks
   # Should show: 8.0.xxx [/usr/local/share/dotnet/sdk]
   ```

**Windows:**
1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0
2. Download ".NET SDK 8.0.x" for Windows
3. Run the installer
4. Verify installation:
   ```bash
   dotnet --list-sdks
   ```

**Linux (Ubuntu/Debian):**
```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET 8 SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# Verify
dotnet --list-sdks
```

### Option 2: Using Homebrew (macOS)

```bash
brew install dotnet@8

# Verify
dotnet --list-sdks
```

---

## ✅ After Installation

1. **Verify .NET 8 is installed:**
   ```bash
   dotnet --version
   # Should show: 8.0.xxx
   ```

2. **Restore packages:**
   ```bash
   cd AzureOpenAISample
   dotnet restore
   ```

3. **Build the project:**
   ```bash
   dotnet build
   ```

4. **Run the application:**
   ```bash
   dotnet run
   ```

5. **Access Swagger UI:**
   - Open browser to: `https://localhost:7xxx` (check console output for exact port)
   - Swagger UI will be at the root URL

---

## 🔧 Alternative: Use .NET 6 (Not Recommended)

If you cannot install .NET 8 right now, you can temporarily use .NET 6:

1. **Change target framework in `AzureOpenAISample.csproj`:**
   ```xml
   <TargetFramework>net6.0</TargetFramework>
   ```

2. **Update package versions (some features may not be available):**
   ```xml
   <PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.12" />
   <PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.21.0" />
   ```

3. **Note:** Some .NET 8 features won't be available:
   - Performance improvements
   - Some new APIs
   - Native AOT compilation

**However, all the Azure concepts and patterns remain identical!**

---

## 🎯 Why .NET 8?

### For Interviews:
- **.NET 8 is the current LTS** (Long-Term Support until November 2026)
- Shows you're **up-to-date** with latest technologies
- **Performance improvements** (20-30% faster than .NET 6)
- **Better for production** workloads

### Key .NET 8 Features to Mention:
1. **Performance**: Faster startup, lower memory usage
2. **Native AOT**: Ahead-of-time compilation for faster cold starts
3. **Enhanced minimal APIs**: Better routing, improved performance
4. **Improved JSON serialization**: Source generators for better performance
5. **Blazor improvements**: Enhanced WebAssembly and Server modes

---

## 📋 Post-Installation Checklist

- [ ] .NET 8 SDK installed (`dotnet --version` shows 8.0.x)
- [ ] Project restores without errors (`dotnet restore`)
- [ ] Project builds successfully (`dotnet build`)
- [ ] Application runs (`dotnet run`)
- [ ] Swagger UI accessible in browser
- [ ] Azure resources created (OpenAI, Search, Storage, App Insights)
- [ ] Configuration updated in `appsettings.json`

---

## 🆘 Troubleshooting

### Issue: "Cannot find .NET SDK"
**Solution:** Restart terminal/IDE after installation to refresh PATH

### Issue: "Multiple SDKs installed, using wrong version"
**Solution:** 
```bash
# Create global.json to specify version
dotnet new globaljson --sdk-version 8.0.xxx

# Or check PATH
echo $PATH
```

### Issue: "Package restore fails"
**Solution:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore again
dotnet restore
```

### Issue: "Port already in use"
**Solution:**
```bash
# Check what's using port 5000/7000
lsof -i :5000
lsof -i :7000

# Kill process or change port in launchSettings.json
```

---

## 📚 Additional Tools (Optional)

### Visual Studio 2022
- Full IDE with debugging, IntelliSense, Azure integration
- Download: https://visualstudio.microsoft.com/

### VS Code with C# Extensions
- Lightweight, cross-platform
- Install C# Dev Kit extension

### Azure CLI
```bash
# macOS
brew install azure-cli

# Verify
az --version
az login
```

### Azure Storage Explorer
- GUI tool for managing blob storage
- Download: https://azure.microsoft.com/features/storage-explorer/

---

## 🚀 Quick Start Commands

Once .NET 8 is installed:

```bash
# Navigate to project
cd /Users/ramlanka/Projects/AzureOpenAISample/AzureOpenAISample

# Restore packages
dotnet restore

# Build project
dotnet build

# Run application
dotnet run

# Run with watch (auto-reload on changes)
dotnet watch run

# Access the application
# Browser: https://localhost:7xxx (check console for exact URL)
```

---

## 💡 Interview Tip

When asked about .NET versions:
- *.NET 8 is the current LTS* (released November 2023)
- *Even-numbered versions are LTS* (6, 8, 10...)
- *Odd-numbered are STS* (Short-Term Support) (7, 9...)
- *Pick LTS for production* - 3 years support
- *.NET 9 (STS) coming November 2024* - mention you're aware of the roadmap

---

## ✅ You're Ready!

Once you see this output after `dotnet run`:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7xxx
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**You're ready to start learning!** 🎉

Open your browser to the URL shown and explore the Swagger UI!

# ⚠️ Antivirus False Positive Warning

## Why is this flagged as a virus?

This application is **NOT a virus**. It's a false positive detection that commonly occurs with:

1. **Self-contained .NET applications** - The app bundles the .NET runtime, which some antivirus software flags as suspicious
2. **Auto-update functionality** - The app downloads and executes updates, which can trigger heuristic detection
3. **Unsigned executable** - The app is not code-signed with an expensive certificate

## Is it safe?

✅ **YES, it's completely safe!**

- **Open Source**: All source code is available at https://github.com/argonz-dev/FiveMConfigEditor
- **No Malicious Code**: You can review the entire codebase
- **Community Verified**: Used by the FiveM community
- **VirusTotal Scan**: Check the latest scan results

## How to use safely?

### Option 1: Add Exception to Windows Defender
1. Open **Windows Security**
2. Go to **Virus & threat protection**
3. Click **Manage settings** under "Virus & threat protection settings"
4. Scroll down to **Exclusions**
5. Click **Add or remove exclusions**
6. Click **Add an exclusion** → **File**
7. Select `FiveMConfigEditorWPF.exe`

### Option 2: Verify with VirusTotal
1. Go to https://www.virustotal.com/
2. Upload `FiveMConfigEditorWPF.exe`
3. Check the scan results
4. Most reputable antivirus engines will show it's clean

### Option 3: Build from Source
If you're still concerned, you can build the app yourself:

```bash
git clone https://github.com/argonz-dev/FiveMConfigEditor.git
cd FiveMConfigEditor
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Why not code-sign?

Code signing certificates cost $200-400 per year. As this is a free, open-source project, we currently don't have the budget for it. 

If you'd like to support code signing, consider:
- Sponsoring the project
- Contributing to a code signing fund
- Helping submit false positive reports to antivirus vendors

## Report False Positive

Help improve detection by reporting this as a false positive:

- **Windows Defender**: https://www.microsoft.com/en-us/wdsi/filesubmission
- **VirusTotal**: https://www.virustotal.com/gui/home/upload
- **Kaspersky**: https://opentip.kaspersky.com/
- **Avast**: https://www.avast.com/false-positive-file-form.php

## Still Concerned?

- Review the source code: https://github.com/argonz-dev/FiveMConfigEditor
- Check the commit history
- Ask questions in GitHub Issues
- Join our community discussions

---

**This is a legitimate tool for FiveM configuration management. The false positive is due to technical limitations, not malicious intent.**

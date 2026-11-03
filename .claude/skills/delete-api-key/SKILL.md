---
name: delete-api-key
description: Delete FZScout's saved API key from Windows Credential Manager, so the next launch asks for a new one. Use when asked to delete, clear, reset, or forget the saved API key.
---

FZScout saves its API key as a generic credential named `FZScout/ApiKey` (see `FZScout/Site/WindowsCredentialManagerApiKeyStore.cs`).

Delete it, then confirm it's gone:

```powershell
cmdkey /delete:FZScout/ApiKey; cmdkey /list:FZScout/ApiKey
```

The list should show `* NONE *`. If the delete says the element wasn't found, there was no saved key; that's fine.

Never print or read the key's value.

Tell the user the next launch will say "API key missing" and ask them to paste a new key from https://fangbreaker.zone/account.

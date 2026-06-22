Add a new EF Core migration named $ARGUMENTS and apply it to the database.

Run these commands in order from the project root (`c:\Users\svenh\Documents\BocceDocs\BocceManager`):

```powershell
dotnet ef migrations add $ARGUMENTS
dotnet ef database update
```

After running:
1. Report whether both commands succeeded.
2. Show the generated migration file name (under `Data/Migrations/`).
3. If `dotnet ef database update` fails, show the error and suggest a fix — do NOT silently retry.
4. If the migration was created but update failed, tell the user so they can decide whether to remove the migration with `dotnet ef migrations remove`.

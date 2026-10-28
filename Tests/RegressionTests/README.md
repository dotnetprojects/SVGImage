Run the automated WPF regression checks on Windows:

```powershell
dotnet run --project Tests/RegressionTests -p:TargetFrameworks=net8.0-windows -p:GeneratePackageOnBuild=false
```

The executable runs on an STA thread and returns a nonzero exit code if a check fails.

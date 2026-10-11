# Patient Vitals Trend Predictor — Simple Web UI

A single small ASP.NET Core app: one `Program.cs`, a couple of `.cs` files
for the ML.NET logic, and one plain HTML page (`wwwroot/index.html`) —
no Razor components, no folders-within-folders.

## Files in this folder

```
PatientVitalsPrediction.Simple.csproj   <- the project file
Program.cs                              <- starts the web server + the /api/predict endpoint
Models.cs                               <- data classes
Services.cs                             <- feature building / synthetic training data / insights
RiskPredictionService.cs                <- trains the ML.NET model once, runs predictions
wwwroot/index.html                      <- the entire UI (HTML+CSS+JS in one file)
sample-patient-input.json               <- example file to test the upload button
```

## Step 1 — Extract the zip properly (important)

Do NOT just double-click the `.zip` and work inside it — Windows lets you
browse a zip like a folder, but it's not a real folder on disk, and tools
like Visual Studio get confused by it.

1. Right-click the downloaded `.zip` file.
2. Choose **"Extract All..."**
3. Pick a destination like your Desktop and click **Extract**.
4. A new, real folder appears (its path will NOT contain `.zip` anywhere).
5. Open **that** folder — you should see the files listed above sitting
   directly inside it.

## Step 2 — Install the .NET 9 SDK (if you haven't already)

Check first:
```bash
dotnet --version
```
If that fails, install from: https://dotnet.microsoft.com/download/dotnet/9.0

## Step 3 — Run it

Open a terminal (Command Prompt / PowerPoint / Terminal) in that extracted
folder — the one containing `PatientVitalsPrediction.Simple.csproj` — and run:

```bash
dotnet run
```

You do **not** need to open Visual Studio at all for this — the terminal
is enough.

It will print something like:
```
Now listening on: http://localhost:5000
```

## Step 4 — Open it in your browser

Go to the address it printed (e.g. `http://localhost:5000`).

- Click **Load sample data**, then **Predict risk** to see it work immediately.
- Or click **Load from JSON file** and choose `sample-patient-input.json`
  from this folder.

## If you'd rather use Visual Studio

Double-click `PatientVitalsPrediction.Simple.csproj` from the **extracted**
folder (not from inside the zip) to open it, then press **F5** or the green
Run button.

## Notes

- The model trains once when the app starts, not on every prediction.
- This is a demo predictor on synthetic training data — not medical advice.

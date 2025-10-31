using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Records;
using Health.Platforms.Android.Callbacks;
using AndroidX.Health.Connect.Client.Records.Metadata;
using Java.Time;
using AndroidX.Health.Connect.Client.Permission;
using Java.Util;
using Health.Platforms.Android.Permissions;
using Android.Content;

namespace Health
{
    public partial class MainPage : ContentPage
    {
        private KotlinCallback _healthConnectClient;

        public MainPage()
        {
            InitializeComponent();
        }

        private Instant DateTimeToInstant(DateTime date)
        {
            long unixTimestamp = ((DateTimeOffset)date).ToUnixTimeSeconds();
            return Instant.OfEpochSecond(unixTimestamp);
        }

        private async void OnLoadDataClicked(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("[v0] OnLoadDataClicked iniciado");
                StatusLabel.Text = "Verificando disponibilidad...";
                
                string providerPackageName = "com.google.android.apps.healthdata";
                int availabilityStatus = HealthConnectClient.GetSdkStatus(Platform.CurrentActivity, providerPackageName);

                Console.WriteLine($"[v0] SDK Status: {availabilityStatus}");

                if (availabilityStatus == HealthConnectClient.SdkUnavailable)
                {
                    await DisplayAlert("No soportado", "Health Connect no está disponible en este dispositivo.", "OK");
                    StatusLabel.Text = "Health Connect no disponible";
                    return;
                }

                if (availabilityStatus == HealthConnectClient.SdkUnavailableProviderUpdateRequired)
                {
                    OpenProviderInstallOrWeb(providerPackageName);
                    StatusLabel.Text = "Se requiere actualización";
                    return;
                }

                if (OperatingSystem.IsAndroidVersionAtLeast(26) && availabilityStatus == HealthConnectClient.SdkAvailable)
                {
                    // Inicializar cliente
                    if (_healthConnectClient == null)
                    {
                        var client = HealthConnectClient.GetOrCreate(Android.App.Application.Context);
                        _healthConnectClient = new KotlinCallback(client);
                        Console.WriteLine("[v0] HealthConnectClient inicializado");
                    }

                    StatusLabel.Text = "Verificando permisos...";

                    var permissionsGranted = await RequestAllPermissions();

                    if (permissionsGranted)
                    {
                        StatusLabel.Text = "Cargando datos...";
                        await LoadAllHealthData();
                        StatusLabel.Text = "Datos actualizados correctamente";
                    }
                    else
                    {
                        StatusLabel.Text = "Permisos denegados. Ve a Configuración > Aplicaciones > Health > Permisos para concederlos manualmente.";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] Error en OnLoadDataClicked: {ex.Message}\n{ex.StackTrace}");
                await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
                StatusLabel.Text = $"Error: {ex.Message}";
            }
        }

        private async Task<bool> RequestAllPermissions()
        {
            try
            {
                var stepsRecord = new StepsRecord(
                    DateTimeToInstant(DateTime.Now.AddMinutes(-1)),
                    ZoneOffset.OfHours(0),
                    DateTimeToInstant(DateTime.Now),
                    ZoneOffset.OfHours(0),
                    1,
                    new Metadata()
                );

                var sleepRecord = new SleepSessionRecord(
                    DateTimeToInstant(DateTime.Now.AddHours(-8)),
                    ZoneOffset.OfHours(0),
                    DateTimeToInstant(DateTime.Now.AddHours(-1)),
                    ZoneOffset.OfHours(0),
                    null,
                    null,
                    new List<SleepSessionRecord.Stage>(),
                    new Metadata()
                );

                var heartRateRecord = new HeartRateRecord(
                    DateTimeToInstant(DateTime.Now.AddMinutes(-1)),
                    ZoneOffset.OfHours(0),
                    DateTimeToInstant(DateTime.Now),
                    ZoneOffset.OfHours(0),
                    new List<HeartRateRecord.Sample>(),
                    new Metadata()
                );

                var distanceRecord = new DistanceRecord(
                    DateTimeToInstant(DateTime.Now.AddMinutes(-1)),
                    ZoneOffset.OfHours(0),
                    DateTimeToInstant(DateTime.Now),
                    ZoneOffset.OfHours(0),
                    AndroidX.Health.Connect.Client.Units.Length.InvokeMeters(1),
                    new Metadata()
                );

                var permissionsToGrant = new Java.Util.HashSet();
                permissionsToGrant.Add(HealthPermission.GetReadPermission(Kotlin.Jvm.Internal.Reflection.GetOrCreateKotlinClass(stepsRecord.Class)));
                permissionsToGrant.Add(HealthPermission.GetReadPermission(Kotlin.Jvm.Internal.Reflection.GetOrCreateKotlinClass(sleepRecord.Class)));
                permissionsToGrant.Add(HealthPermission.GetReadPermission(Kotlin.Jvm.Internal.Reflection.GetOrCreateKotlinClass(heartRateRecord.Class)));
                permissionsToGrant.Add(HealthPermission.GetReadPermission(Kotlin.Jvm.Internal.Reflection.GetOrCreateKotlinClass(distanceRecord.Class)));

                Console.WriteLine($"[v0] Permisos a solicitar: {permissionsToGrant.Size()}");

                var grantedPermissions = await _healthConnectClient.GetGrantedPermissions();
                Console.WriteLine($"[v0] Permisos ya concedidos: {grantedPermissions?.Count ?? 0}");

                bool needsPermissions = false;
                if (grantedPermissions == null || grantedPermissions.Count == 0)
                {
                    needsPermissions = true;
                }
                else
                {
                    var iterator = permissionsToGrant.Iterator();
                    while (iterator.HasNext)
                    {
                        var permission = iterator.Next()?.ToString();
                        if (permission != null && !grantedPermissions.Contains(permission))
                        {
                            needsPermissions = true;
                            break;
                        }
                    }
                }

                Console.WriteLine($"[v0] Necesita solicitar permisos: {needsPermissions}");

                if (needsPermissions)
                {
                    var result = await PermissionHandler.Request(permissionsToGrant);
                    
                    if (result != null && result.Count > 0)
                    {
                        Console.WriteLine($"[v0] Permisos concedidos: {result.Count}");
                        grantedPermissions = result;
                    }
                    else
                    {
                        Console.WriteLine("[v0] No se concedieron permisos");
                        return false;
                    }
                }

                bool allGranted = true;
                if (grantedPermissions != null)
                {
                    var iterator = permissionsToGrant.Iterator();
                    while (iterator.HasNext)
                    {
                        var permission = iterator.Next()?.ToString();
                        if (permission != null && !grantedPermissions.Contains(permission))
                        {
                            allGranted = false;
                            break;
                        }
                    }
                }
                else
                {
                    allGranted = false;
                }

                Console.WriteLine($"[v0] Todos los permisos concedidos: {allGranted}");
                
                return allGranted;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] Error solicitando permisos: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        private async Task LoadAllHealthData()
        {
            DateTime now = DateTime.Now;
            DateTime startOfToday = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Local);
            DateTime startOfMonth = startOfToday.AddDays(-30);
            
            Instant startTimeMonth = DateTimeToInstant(startOfMonth);
            Instant endTimeNow = DateTimeToInstant(now);
            Instant startTimeToday = DateTimeToInstant(startOfToday);

            Console.WriteLine($"[v0] Cargando datos desde {startOfMonth} hasta {now}");

            var stepsTask = LoadStepsData(startTimeToday, endTimeNow);
            var sleepTask = LoadSleepData(startTimeMonth, endTimeNow);
            var heartRateTask = LoadHeartRateData(startTimeMonth, endTimeNow);
            var distanceTask = LoadDistanceData(startTimeToday, endTimeNow);

            await Task.WhenAll(stepsTask, sleepTask, heartRateTask, distanceTask);
        }

        private async Task LoadStepsData(Instant startTime, Instant endTime)
        {
            try
            {
                Console.WriteLine($"[v0] Cargando pasos...");
                var records = await _healthConnectClient.ReadStepsRecords(startTime, endTime);
                Console.WriteLine($"[v0] Registros de pasos encontrados: {records?.Count ?? 0}");
                
                long totalSteps = 0;
                if (records != null)
                {
                    foreach (var record in records)
                    {
                        Console.WriteLine($"[v0] Pasos: {record.Count} - Origen: {record.Metadata?.DataOrigin?.PackageName}");
                        totalSteps += record.Count;
                    }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StepsLabel.Text = totalSteps > 0 ? $"{totalSteps:N0}" : "0";
                    Console.WriteLine($"[v0] Total de pasos: {totalSteps}");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] Error cargando pasos: {ex.Message}\n{ex.StackTrace}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StepsLabel.Text = "Error";
                });
            }
        }

        private async Task LoadSleepData(Instant startTime, Instant endTime)
        {
            try
            {
                Console.WriteLine($"[v0] Cargando sueño...");
                var records = await _healthConnectClient.ReadSleepRecords(startTime, endTime);
                Console.WriteLine($"[v0] Registros de sueño encontrados: {records?.Count ?? 0}");
                
                double totalHours = 0;
                if (records != null && records.Count > 0)
                {
                    foreach (var record in records)
                    {
                        var duration = Java.Time.Duration.Between(record.StartTime, record.EndTime);
                        double hours = duration.ToMinutes() / 60.0;
                        Console.WriteLine($"[v0] Sueño: {hours:F2}h - Origen: {record.Metadata?.DataOrigin?.PackageName}");
                        totalHours += hours;
                    }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SleepLabel.Text = totalHours > 0 ? $"{totalHours:F1}h" : "0h";
                    Console.WriteLine($"[v0] Total de sueño: {totalHours:F1}h");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] Error cargando sueño: {ex.Message}\n{ex.StackTrace}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SleepLabel.Text = "Sin datos";
                });
            }
        }

        private async Task LoadHeartRateData(Instant startTime, Instant endTime)
        {
            try
            {
                Console.WriteLine($"[v0] Cargando ritmo cardíaco...");
                var records = await _healthConnectClient.ReadHeartRateRecords(startTime, endTime);
                Console.WriteLine($"[v0] Registros de ritmo cardíaco encontrados: {records?.Count ?? 0}");
                
                double averageBpm = 0;
                int totalSamples = 0;

                if (records != null && records.Count > 0)
                {
                    foreach (var record in records)
                    {
                        Console.WriteLine($"[v0] Ritmo cardíaco - Origen: {record.Metadata?.DataOrigin?.PackageName}");
                        if (record.Samples != null)
                        {
                            var samples = record.Samples;
                            if (samples is Java.Util.IList javaList)
                            {
                                for (int i = 0; i < javaList.Size(); i++)
                                {
                                    if (javaList.Get(i) is HeartRateRecord.Sample sample)
                                    {
                                        Console.WriteLine($"[v0] BPM: {sample.BeatsPerMinute}");
                                        averageBpm += sample.BeatsPerMinute;
                                        totalSamples++;
                                    }
                                }
                            }
                        }
                    }

                    if (totalSamples > 0)
                    {
                        averageBpm /= totalSamples;
                    }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HeartRateLabel.Text = averageBpm > 0 ? $"{averageBpm:F0}" : "--";
                    Console.WriteLine($"[v0] Promedio de BPM: {averageBpm:F0}");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] Error cargando ritmo cardíaco: {ex.Message}\n{ex.StackTrace}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HeartRateLabel.Text = "Sin datos";
                });
            }
        }

        private async Task LoadDistanceData(Instant startTime, Instant endTime)
        {
            try
            {
                Console.WriteLine($"[v0] Cargando distancia...");
                var records = await _healthConnectClient.ReadDistanceRecords(startTime, endTime);
                Console.WriteLine($"[v0] Registros de distancia encontrados: {records?.Count ?? 0}");
                
                double totalMeters = 0;
                if (records != null && records.Count > 0)
                {
                    foreach (var record in records)
                    {
                        double meters = record.Distance.Meters;
                        Console.WriteLine($"[v0] Distancia: {meters}m - Origen: {record.Metadata?.DataOrigin?.PackageName}");
                        totalMeters += meters;
                    }
                }

                double totalKm = totalMeters / 1000.0;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DistanceLabel.Text = totalKm > 0 ? $"{totalKm:F2}" : "0.00";
                    Console.WriteLine($"[v0] Total de distancia: {totalKm:F2}km");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] Error cargando distancia: {ex.Message}\n{ex.StackTrace}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DistanceLabel.Text = "Sin datos";
                });
            }
        }

        private void OpenProviderInstallOrWeb(string providerPackageName)
        {
            var activity = Platform.CurrentActivity;
            var pm = activity.PackageManager;

            var marketUri = Android.Net.Uri.Parse($"market://details?id={providerPackageName}&url=healthconnect%3A%2F%2Fonboarding");
            var marketIntent = new Intent(Intent.ActionView, marketUri)
                .SetPackage("com.android.vending")
                .PutExtra("overlay", true)
                .PutExtra("callerId", activity.PackageName);

            var handlers = pm.QueryIntentActivities(marketIntent, 0);
            if (handlers != null && handlers.Count > 0)
            {
                try
                {
                    activity.StartActivity(marketIntent);
                    return;
                }
                catch (ActivityNotFoundException)
                {
                    // fallback abajo
                }
            }

            var webUri = Android.Net.Uri.Parse($"https://play.google.com/store/apps/details?id={providerPackageName}&url=healthconnect%3A%2F%2Fonboarding");
            var webIntent = new Intent(Intent.ActionView, webUri);
            activity.StartActivity(webIntent);
        }
    }
}

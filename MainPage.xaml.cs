using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Aggregate;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Records;
using Health.Platforms.Android.Callbacks;
using AndroidX.Health.Connect.Client.Time;
using AndroidX.Health.Connect.Client.Records.Metadata;
using Java.Time;
using AndroidX.Health.Connect.Client.Permission;
using Android.Runtime;
using Java.Util;
using Health.Platforms.Android.Permissions;
using AndroidX.Health.Connect.Client.Units;
using Android.Content;

namespace Health
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private Instant DateTimeToInstant(DateTime date)
        {
            long unixTimestamp = ((DateTimeOffset)date).ToUnixTimeSeconds();
            return Instant.OfEpochSecond(unixTimestamp);
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            DateTime startOfDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, DateTimeKind.Local);
            Instant startTime = DateTimeToInstant(startOfDay);
            Instant endTime = DateTimeToInstant(DateTime.Now);

            StepsRecord stepsRecord = new StepsRecord(startTime, ZoneOffset.OfHours(0), endTime, ZoneOffset.OfHours(0), 1, new Metadata());

            // Permisos necesarios (lectura + escritura)
            List<string> PermissionsToGrant = new List<string>
            {
                HealthPermission.GetReadPermission(Kotlin.Jvm.Internal.Reflection.GetOrCreateKotlinClass(stepsRecord.Class)),
                HealthPermission.GetWritePermission(Kotlin.Jvm.Internal.Reflection.GetOrCreateKotlinClass(stepsRecord.Class))
            };

            string providerPackageName = "com.google.android.apps.healthdata";
            int availabilityStatus = HealthConnectClient.GetSdkStatus(Platform.CurrentActivity, providerPackageName);

            if (availabilityStatus == HealthConnectClient.SdkUnavailable)
            {
                await DisplayAlert("No soportado", "Health Connect no está disponible en este dispositivo.", "OK");
                return;
            }

            if (availabilityStatus == HealthConnectClient.SdkUnavailableProviderUpdateRequired)
            {
                OpenProviderInstallOrWeb(providerPackageName);
                return;
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(26) && availabilityStatus == HealthConnectClient.SdkAvailable)
            {
                try
                {
                    var healthConnectClient = new KotlinCallback(HealthConnectClient.GetOrCreate(Android.App.Application.Context));

                    // Verificar permisos
                    List<string> GrantedPermissions = await healthConnectClient.GetGrantedPermissions();
                    List<string> MissingPermissions = PermissionsToGrant.Except(GrantedPermissions).ToList();

                    if (MissingPermissions.Count > 0)
                    {
                        GrantedPermissions = await PermissionHandler.Request(new HashSet(PermissionsToGrant));
                    }

                    bool allPermissionsGranted = PermissionsToGrant.All(permission => GrantedPermissions.Contains(permission));
                    if (allPermissionsGranted)
                    {
                        await LoadStepsWithFallback(healthConnectClient, startTime, endTime);
                    }
                    else
                    {
                        await DisplayAlert("Permisos requeridos", "No se concedieron todos los permisos necesarios.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error al acceder a Health Connect: {ex.Message}", "OK");
                }
            }
        }

        private async Task LoadStepsWithFallback(KotlinCallback healthConnectClient, Instant startTime, Instant endTime) { }
        /*
        private async Task LoadStepsWithFallback(KotlinCallback healthConnectClient, Instant startTime, Instant endTime)
        {
            try
            {
                // Intentamos leer pasos directamente
                var request = new ReadRecordsRequest.Builder(StepsRecord.Class)
                    .SetTimeRangeFilter(TimeRangeFilter.Between(startTime, endTime))
                    .Build();

                var response = await healthConnectClient.ReadRecordsAsync(request);
                var totalSteps = response.Records.Sum(r => r.Count);

                if (totalSteps > 0)
                {
                    CounterBtn.Text = $"{totalSteps} pasos hoy";
                    SemanticScreenReader.Announce(CounterBtn.Text);
                    return;
                }

                // Si no hay pasos, insertamos pasos de prueba
                var testSteps = new StepsRecord(
                    startTime: DateTimeToInstant(DateTime.Now.AddMinutes(-30)),
                    startZoneOffset: ZoneOffset.OfHours(0),
                    endTime: DateTimeToInstant(DateTime.Now),
                    endZoneOffset: ZoneOffset.OfHours(0),
                    count: 1234,
                    metadata: new Metadata()
                );

                await healthConnectClient.InsertRecordsAsync(new List<StepsRecord> { testSteps });

                // Leemos nuevamente después de insertar
                var responseAfterInsert = await healthConnectClient.ReadRecordsAsync(request);
                var totalStepsAfterInsert = responseAfterInsert.Records.Sum(r => r.Count);

                if (totalStepsAfterInsert > 0)
                {
                    CounterBtn.Text = $"{totalStepsAfterInsert} pasos (insertados para prueba)";
                }
                else
                {
                    CounterBtn.Text = "No hay datos de pasos disponibles";
                }

                SemanticScreenReader.Announce(CounterBtn.Text);
            }
            catch (Exception ex)
            {
                CounterBtn.Text = "Error al leer pasos";
                Console.WriteLine($"Error: {ex.Message}");
            }
        }*/

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

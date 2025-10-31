using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Activity.Result.Contract;
using AndroidX.Activity.Result;
using Health.Platforms.Android.Callbacks;
using AndroidX.Health.Connect.Client;
using JObject = Java.Lang.Object;

namespace Health
{
    [IntentFilter(new[] { "androidx.health.ACTION_SHOW_PERMISSIONS_RATIONALE" })]
    [IntentFilter(new[] { "android.intent.action.VIEW_PERMISSION_USAGE" },
        Categories = new[] { "android.intent.category.HEALTH_PERMISSIONS" })]
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, 
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | 
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private ActivityResultLauncher _permissionRequestLauncher = null!;
        private TaskCompletionSource<JObject?> _permissionRequestCompletedSource;
        public static readonly TimeSpan MaxPermissionRequestDuration = TimeSpan.FromMinutes(1);

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            _permissionRequestLauncher = RegisterForActivityResult(
                PermissionController.CreateRequestPermissionResultContract(),
                new AndroidActivityResultCallback(result => {
                    Console.WriteLine($"[v0] Permission result received in MainActivity");
                    _permissionRequestCompletedSource?.TrySetResult(result);
                    _permissionRequestCompletedSource = null;
                }));
        }

        public Task RequestPermission(Java.Lang.Object permission, TaskCompletionSource<JObject?> whenCompletedSource)
        {
            Console.WriteLine($"[v0] RequestPermission called in MainActivity");
            _permissionRequestCompletedSource?.TrySetResult(null);
            _permissionRequestCompletedSource = whenCompletedSource;
            _permissionRequestLauncher.Launch(permission);
            return whenCompletedSource.Task;
        }
    }
}

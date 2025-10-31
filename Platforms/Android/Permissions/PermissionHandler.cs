using Android.App;
using Android.Util;
using AndroidX.Core.App;
using Health.Platforms.Android.Callbacks;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JObject = Java.Lang.Object;

namespace Health.Platforms.Android.Permissions
{
    internal class PermissionHandler
    {
        public static async Task<List<string>> Request(Java.Lang.Object permissions, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("[v0] PermissionHandler.Request iniciado");

                if (Platform.CurrentActivity is not MainActivity activity)
                {
                    Console.WriteLine("[v0] MainActivity no disponible");
                    return new List<string>();
                }

                var whenCompletedSource = new TaskCompletionSource<JObject?>();
                
                _ = Task.Delay(TimeSpan.FromSeconds(60), cancellationToken)
                    .ContinueWith(_ => whenCompletedSource.TrySetResult(null), TaskScheduler.Default);

                new AlertDialog.Builder(activity)
                    .SetTitle("Permisos de Health Connect")
                    .SetMessage("¿Deseas permitir que la app acceda a tus datos de salud?")
                    .SetNegativeButton("Rechazar", (_, _) => whenCompletedSource.TrySetResult(null))
                    .SetPositiveButton("Permitir", (_, _) => RequestPermission())
                    .Show();

                Console.WriteLine("[v0] Esperando respuesta del usuario...");
                JObject? result = await whenCompletedSource.Task.ConfigureAwait(false);
                
                if (result != null)
                {
                    Console.WriteLine("[v0] Permisos concedidos");
                    return KotlinCallback.ConvertISetToList((ISet)result);
                }
                else
                {
                    Console.WriteLine("[v0] Permisos rechazados o timeout");
                    return new List<string>();
                }

                void RequestPermission()
                {
                    Console.WriteLine("[v0] Lanzando solicitud de permisos...");
                    activity.RequestPermission(permissions.ToString(), whenCompletedSource);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] Exception en PermissionHandler: {ex.Message}\n{ex.StackTrace}");
                return new List<string>();
            }
        }
    }
}

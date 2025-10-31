using Android.Runtime;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Aggregate;
using AndroidX.Health.Connect.Client.Records;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Response;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Health.Platforms.Android.Callbacks
{
    internal class KotlinCallback
    {
        private IHealthConnectClient healthConnectClient;
        public KotlinCallback(IHealthConnectClient client)
        {
            healthConnectClient = client;
        }

        public enum MyCoroutineSingletons
        {
            COROUTINE_SUSPENDED,
            UNDECIDED,
            RESUMED
        }

        public async Task<List<AggregationResultGroupedByDuration>> AggregateGroupByDuration(global::AndroidX.Health.Connect.Client.Request.AggregateGroupByDurationRequest request)
        {
            var tcs = new TaskCompletionSource<Java.Lang.Object>();
            Java.Lang.Object result = healthConnectClient.AggregateGroupByDuration(request,new Continuation(tcs, default));

            if (result is Java.Lang.Enum CoroutineSingletons)
            {
                MyCoroutineSingletons checkedEnum = (MyCoroutineSingletons)Enum.Parse(typeof(MyCoroutineSingletons), CoroutineSingletons.ToString());
                if (checkedEnum == MyCoroutineSingletons.COROUTINE_SUSPENDED)
                {
                    result = await tcs.Task;
                }
            }
            
            if (result is JavaList javaList)
            {
                List<AggregationResultGroupedByDuration> dotNetList = new List<AggregationResultGroupedByDuration>();
                for (int i = 0; i < javaList.Size(); i++)
                {
                    if (javaList.Get(i) is AggregationResultGroupedByDuration item)
                    {
                        dotNetList.Add(item);
                    }
                }
                return dotNetList;
            }
            return null;
        }

        public async Task<List<string>> GetGrantedPermissions()
        {
            var tcs = new TaskCompletionSource<Java.Lang.Object>();
            Java.Lang.Object result = healthConnectClient.PermissionController.GetGrantedPermissions(new Continuation(tcs, default));

            if (result is Java.Lang.Enum CoroutineSingletons)
            {
                MyCoroutineSingletons checkedEnum = (MyCoroutineSingletons)Enum.Parse(typeof(MyCoroutineSingletons), CoroutineSingletons.ToString());
                if (checkedEnum == MyCoroutineSingletons.COROUTINE_SUSPENDED)
                {
                    result = await tcs.Task;
                }
            }
            
            if (result is Kotlin.Collections.AbstractMutableSet abstractMutableSet)
            {
                Java.Util.ISet javaSet = abstractMutableSet.JavaCast<Java.Util.ISet>();
                return ConvertISetToList(javaSet);
            }
            return null;
        }

        public static List<string> ConvertISetToList(ISet javaSet)
        {
            List<string> listOfStrings = new List<string>();
            var iterator = javaSet.Iterator();

            while (iterator.HasNext)
            {
                Java.Lang.Object element = iterator.Next();
                listOfStrings.Add((string)element.JavaCast<Java.Lang.String>());
            }

            return listOfStrings;
        }

        private ReadRecordsRequest CreateReadRecordsRequest(Type recordType, Java.Time.Instant startTime, Java.Time.Instant endTime)
        {
            try
            {
                var kClass = Kotlin.Jvm.Internal.Reflection.GetOrCreateKotlinClass(Java.Lang.Class.FromType(recordType));
                var timeFilter = AndroidX.Health.Connect.Client.Time.TimeRangeFilter.Between(startTime, endTime);
                
                // Usar reflexión para crear el request
                var requestClass = Java.Lang.Class.ForName("androidx.health.connect.client.request.ReadRecordsRequest");
                var constructor = requestClass.GetConstructors()[0];
                
                // Crear el request con los parámetros necesarios
                var request = constructor.NewInstance(
                    (Java.Lang.Object)kClass,
                    timeFilter,
                    new Java.Util.HashSet(), // dataOriginFilter
                    Java.Lang.Boolean.False, // ascendingOrder
                    Java.Lang.Integer.ValueOf(1000), // pageSize
                    null // pageToken
                ) as ReadRecordsRequest;
                
                return request;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creando ReadRecordsRequest: {ex.Message}");
                return null;
            }
        }

        public async Task<List<StepsRecord>> ReadStepsRecords(Java.Time.Instant startTime, Java.Time.Instant endTime)
        {
            var request = CreateReadRecordsRequest(typeof(StepsRecord), startTime, endTime);
            if (request == null) return new List<StepsRecord>();
            
            return await ReadRecordsGeneric<StepsRecord>(request);
        }

        public async Task<List<SleepSessionRecord>> ReadSleepRecords(Java.Time.Instant startTime, Java.Time.Instant endTime)
        {
            var request = CreateReadRecordsRequest(typeof(SleepSessionRecord), startTime, endTime);
            if (request == null) return new List<SleepSessionRecord>();
            
            return await ReadRecordsGeneric<SleepSessionRecord>(request);
        }

        public async Task<List<HeartRateRecord>> ReadHeartRateRecords(Java.Time.Instant startTime, Java.Time.Instant endTime)
        {
            var request = CreateReadRecordsRequest(typeof(HeartRateRecord), startTime, endTime);
            if (request == null) return new List<HeartRateRecord>();
            
            return await ReadRecordsGeneric<HeartRateRecord>(request);
        }

        public async Task<List<DistanceRecord>> ReadDistanceRecords(Java.Time.Instant startTime, Java.Time.Instant endTime)
        {
            var request = CreateReadRecordsRequest(typeof(DistanceRecord), startTime, endTime);
            if (request == null) return new List<DistanceRecord>();
            
            return await ReadRecordsGeneric<DistanceRecord>(request);
        }

        private async Task<List<T>> ReadRecordsGeneric<T>(ReadRecordsRequest request) where T : class
        {
            var tcs = new TaskCompletionSource<Java.Lang.Object>();
            Java.Lang.Object result = healthConnectClient.ReadRecords(request, new Continuation(tcs, default));

            if (result is Java.Lang.Enum coroutine)
            {
                var checkedEnum = (MyCoroutineSingletons)Enum.Parse(typeof(MyCoroutineSingletons), coroutine.ToString());
                if (checkedEnum == MyCoroutineSingletons.COROUTINE_SUSPENDED)
                    result = await tcs.Task;
            }

            if (result is AndroidX.Health.Connect.Client.Response.ReadRecordsResponse readResponse)
            {
                var list = new List<T>();
                var records = readResponse.Records;
                if (records is JavaList javaList)
                {
                    for (int i = 0; i < javaList.Size(); i++)
                    {
                        if (javaList.Get(i) is T record)
                            list.Add(record);
                    }
                }
                return list;
            }

            return new List<T>();
        }

        public async Task<List<StepsRecord>> ReadRecords(ReadRecordsRequest request)
        {
            var tcs = new TaskCompletionSource<Java.Lang.Object>();
            Java.Lang.Object result = healthConnectClient.ReadRecords(request, new Continuation(tcs, default));

            if (result is Java.Lang.Enum coroutine)
            {
                var checkedEnum = (MyCoroutineSingletons)Enum.Parse(typeof(MyCoroutineSingletons), coroutine.ToString());
                if (checkedEnum == MyCoroutineSingletons.COROUTINE_SUSPENDED)
                    result = await tcs.Task;
            }

            if (result is AndroidX.Health.Connect.Client.Response.ReadRecordsResponse readResponse)
            {
                var list = new List<StepsRecord>();
                var records = readResponse.Records;
                if (records is JavaList javaList)
                {
                    for (int i = 0; i < javaList.Size(); i++)
                    {
                        if (javaList.Get(i) is StepsRecord stepRecord)
                            list.Add(stepRecord);
                    }
                }
                return list;
            }

            return new List<StepsRecord>();
        }

    }
}

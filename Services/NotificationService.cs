using Grpc.Core;
using GrpcGreeter;
using CorePush.Google;
using GrpcGreeter.google.models;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using static GrpcGreeter.google.models.GoogleNotification;
using static GrpcGreeter.google.models.FcmNotificationSetting;

namespace GrpcGreeter.Services;

public class NotificationService : Notification.NotificationBase
{
    private readonly FcmNotificationSetting _fcmNotificationSetting;
    public NotificationService(IOptions<FcmNotificationSetting> settings)
    {
        _fcmNotificationSetting = settings.Value;
    }

    public async override Task<NotificationResponse> SendNotification(NotificationRequest request, Grpc.Core.ServerCallContext context)
    {
        NotificationResponse response = new NotificationResponse();
        try
        {
            if (request.IsAndroidDevice)
            {
                /* FCM Sender (Android Device) */
                FcmSettings settings = new FcmSettings()
                {
                    SenderId = _fcmNotificationSetting.SenderId,
                    ServerKey = _fcmNotificationSetting.ServerKey
                };
                HttpClient httpClient = new HttpClient();

                string authorizationKey = string.Format("keyy={0}", settings.ServerKey);
                string deviceToken = request.DeviceId;

                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorizationKey);
                httpClient.DefaultRequestHeaders.Accept
                        .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                DataPayload dataPayload = new DataPayload();
                dataPayload.Title = request.Title;
                dataPayload.Body = request.Body;

                GoogleNotification notification = new GoogleNotification();
                notification.Data = dataPayload;
                notification.Notification = dataPayload;

                var fcm = new FcmSender(settings, httpClient);
                var fcmSendResponse = await fcm.SendAsync(deviceToken, notification);

                if (fcmSendResponse.IsSuccess())
                {
                    response.IsSuccess = true;
                    response.Message = "Notification sent successfully";
                    return response;
                }
                else
                {
                    response.IsSuccess = false;
                    response.Message = fcmSendResponse.Results[0].Error;
                    return response;
                }
            }
            else
            {
                /* Code here for APN Sender (iOS Device) */
                //var apn = new ApnSender(apnSettings, httpClient);
                //await apn.SendAsync(notification, deviceToken);
            }
            return response;
        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.Message = "Something went wrong "+ex;
            return response;
        }

    }
}

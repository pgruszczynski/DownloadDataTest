package com.unity3d.backgrounddownload;

import android.app.Notification;
import android.app.NotificationManager;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.os.IBinder;
import android.support.annotation.Nullable;
import android.util.Log;

public class BackgroundDownloadKillService extends Service {

    private NotificationManager notificationManagerKiller;
    private String notificationString = Context.NOTIFICATION_SERVICE;

    @Nullable
    @Override
    public IBinder onBind(Intent intent){
        return null;
    }

    @Override
    public void onDestroy(){
        super.onDestroy();
        Log.d("BDK_SERVICE", "Destroying redundant service with notification ID "+BackgroundDownload.downloadNotificationId);
        /*
        DOPISĄĆ TUTAJ LOGIKE PRZY UBIJANIU APKIII
         */
        RemoveRedundantNotification();
        this.stopSelf();
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {

        Log.d("BDK_SERVICE", "Service started - remember to kill notification with id "+BackgroundDownload.downloadNotificationId);
        return Service.START_NOT_STICKY;
    }

    @Override
    public void onTaskRemoved(Intent rootIntent){
        Log.d("BDK_SERVICE", "Killing redundant services");
        super.onTaskRemoved(rootIntent);
        /*
        DOPISĄĆ TUTAJ LOGIKE PRZY UBIJANIU APKIII
         */
        RemoveRedundantNotification();
        this.stopSelf();
    }

    private void RemoveRedundantNotification(){
        notificationManagerKiller = (NotificationManager) getSystemService(notificationString);
        notificationManagerKiller.cancel(BackgroundDownload.downloadNotificationId);
    }
}

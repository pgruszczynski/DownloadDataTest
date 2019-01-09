package com.unity3d.backgrounddownload;

import android.app.Service;
import android.content.Intent;
import android.os.IBinder;
import android.support.annotation.Nullable;
import android.util.Log;

public class BackgroundDownloadKillService extends Service {

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
        BackgroundDownload.notificationManager.cancel(BackgroundDownload.downloadNotificationId);
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
        BackgroundDownload.notificationManager.cancel(BackgroundDownload.downloadNotificationId);
        this.stopSelf();
    }
}
